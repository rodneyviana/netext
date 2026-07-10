/*=========================================================================================================
  Copyright (c) 2013-2015 Rodney Viana
  http://netext.codeplex.com/

  Distributed under GNU General Public License version 2 (GPLv2) (http://www.gnu.org/licenses/gpl-2.0.html)
============================================================================================================*/

using System;
using System.Text;

namespace Microsoft.Diagnostics.Runtime.Utilities
{
    /// <summary>
    /// Reads the GNU build-id (NT_GNU_BUILD_ID note, .note.gnu.build-id) out of a loaded Linux/macOS ELF
    /// module's memory. This is the key modern ClrMD/WinDbg use to locate the correct cross-OS DAC for a
    /// Linux target - an ELF module has no PE timestamp+filesize to match against.
    /// </summary>
    internal static class ElfBuildId
    {
        private const uint ElfMagic = 0x464c457f; // "\x7fELF" read as a little-endian uint
        private const byte ElfClass64 = 2;
        private const byte ElfDataLsb = 1; // little-endian; .NET never ships on a big-endian target
        private const uint PT_NOTE = 4;
        private const uint NT_GNU_BUILD_ID = 3;

        /// <summary>
        /// Attempts to read the build-id of the ELF module loaded at <paramref name="imageBase"/>.
        /// Returns false (harmlessly) for any non-ELF (e.g. PE) module.
        /// </summary>
        public static bool TryGetBuildId(IDataReader reader, ulong imageBase, out byte[] buildId)
        {
            buildId = null;

            byte[] header = new byte[64];
            if (!ReadFull(reader, imageBase, header))
                return false;

            uint magic = BitConverter.ToUInt32(header, 0);
            if (magic != ElfMagic)
                return false;

            if (header[4] != ElfClass64 || header[5] != ElfDataLsb)
                return false; // 32-bit / big-endian ELF targets are not something .NET ships on

            ulong phoff = BitConverter.ToUInt64(header, 32);
            ushort phentsize = BitConverter.ToUInt16(header, 54);
            ushort phnum = BitConverter.ToUInt16(header, 56);

            // The program header table lives inside the first PT_LOAD segment, which is mapped 1:1 from
            // file offset 0 to the module's load base - the same assumption glibc's own dl_iterate_phdr
            // relies on - so its runtime address is simply imageBase + phoff.
            for (int i = 0; i < phnum; i++)
            {
                byte[] phdr = new byte[56];
                if (!ReadFull(reader, imageBase + phoff + (ulong)(i * phentsize), phdr))
                    return false;

                uint pType = BitConverter.ToUInt32(phdr, 0);
                if (pType != PT_NOTE)
                    continue;

                ulong pVaddr = BitConverter.ToUInt64(phdr, 16);
                ulong pMemsz = BitConverter.ToUInt64(phdr, 40);

                if (TryReadBuildIdFromNoteSegment(reader, imageBase + pVaddr, pMemsz, out buildId))
                    return true;
            }

            return false;
        }

        private static bool TryReadBuildIdFromNoteSegment(IDataReader reader, ulong address, ulong size, out byte[] buildId)
        {
            buildId = null;
            if (size == 0 || size > 0x10000) // note segments are tiny; bail out on anything implausible
                return false;

            byte[] notes = new byte[size];
            if (!ReadFull(reader, address, notes))
                return false;

            int offset = 0;
            while (offset + 12 <= notes.Length)
            {
                uint nameSize = BitConverter.ToUInt32(notes, offset);
                uint descSize = BitConverter.ToUInt32(notes, offset + 4);
                uint type = BitConverter.ToUInt32(notes, offset + 8);
                offset += 12;

                int nameSizeAligned = Align4(nameSize);
                int descSizeAligned = Align4(descSize);

                if (offset + nameSizeAligned + descSizeAligned > notes.Length)
                    break;

                // Note types are only unique within a namespace (the note's name field): type 3 is
                // NT_GNU_BUILD_ID only when the name is "GNU" (namesz includes the terminating zero).
                if (type == NT_GNU_BUILD_ID && descSize > 0 && nameSize == 4 &&
                    notes[offset] == (byte)'G' && notes[offset + 1] == (byte)'N' && notes[offset + 2] == (byte)'U')
                {
                    buildId = new byte[descSize];
                    Array.Copy(notes, offset + nameSizeAligned, buildId, 0, (int)descSize);
                    return true;
                }

                offset += nameSizeAligned + descSizeAligned;
            }

            return false;
        }

        private static int Align4(uint value) => ((int)value + 3) & ~3;

        private static bool ReadFull(IDataReader reader, ulong address, byte[] buffer)
        {
            int read;
            return reader.ReadMemory(address, buffer, buffer.Length, out read) && read == buffer.Length;
        }

        public static string ToHexString(byte[] buildId)
        {
            StringBuilder sb = new StringBuilder(buildId.Length * 2);
            foreach (byte b in buildId)
                sb.Append(b.ToString("x2"));
            return sb.ToString();
        }
    }

    /// <summary>
    /// Resolves an exported (dynamic) symbol of a loaded ELF module directly from target memory, via the
    /// module's PT_DYNAMIC segment and GNU hash table. Used to find "DotNetRuntimeContractDescriptor"
    /// (the cDAC contract descriptor export, .NET 9+) in libcoreclr.so - dbghelp cannot resolve ELF
    /// dynamic symbols for us, and there is no PE export directory to walk.
    /// Kept in this file on purpose: adding new files to the old-style csproj has proven error-prone.
    /// </summary>
    internal static class ElfExportSymbol
    {
        private const uint PT_DYNAMIC = 2;

        // Elf64_Dyn tags we care about
        private const ulong DT_NULL = 0;
        private const ulong DT_STRTAB = 5;
        private const ulong DT_SYMTAB = 6;
        private const ulong DT_GNU_HASH = 0x6ffffef5;

        private const int MaxChainIterations = 100000; // defensive cap against corrupt hash chains

        /// <summary>
        /// Attempts to resolve the absolute address of an exported symbol in the ELF module loaded at
        /// <paramref name="imageBase"/>. Returns false for non-ELF modules or when the symbol is absent.
        /// </summary>
        public static bool TryGetExportSymbol(IDataReader reader, ulong imageBase, string symbolName, out ulong address)
        {
            address = 0;

            byte[] header = new byte[64];
            if (!ReadFull(reader, imageBase, header))
                return false;

            if (BitConverter.ToUInt32(header, 0) != 0x464c457f /* \x7fELF */)
                return false;

            if (header[4] != 2 /* ELFCLASS64 */ || header[5] != 1 /* little-endian */)
                return false;

            ulong phoff = BitConverter.ToUInt64(header, 32);
            ushort phentsize = BitConverter.ToUInt16(header, 54);
            ushort phnum = BitConverter.ToUInt16(header, 56);

            // Find PT_DYNAMIC
            ulong dynVaddr = 0, dynSize = 0;
            for (int i = 0; i < phnum; i++)
            {
                byte[] phdr = new byte[56];
                if (!ReadFull(reader, imageBase + phoff + (ulong)(i * phentsize), phdr))
                    return false;

                if (BitConverter.ToUInt32(phdr, 0) == PT_DYNAMIC)
                {
                    dynVaddr = BitConverter.ToUInt64(phdr, 16);
                    dynSize = BitConverter.ToUInt64(phdr, 40);
                    break;
                }
            }

            if (dynVaddr == 0 || dynSize == 0 || dynSize > 0x100000)
                return false;

            // Walk Elf64_Dyn entries (16 bytes each: tag + val) collecting the tables we need.
            ulong strTab = 0, symTab = 0, gnuHash = 0;
            byte[] dyn = new byte[dynSize];
            if (!ReadFull(reader, imageBase + dynVaddr, dyn))
                return false;

            for (int off = 0; off + 16 <= dyn.Length; off += 16)
            {
                ulong tag = BitConverter.ToUInt64(dyn, off);
                ulong val = BitConverter.ToUInt64(dyn, off + 8);

                if (tag == DT_NULL)
                    break;
                else if (tag == DT_STRTAB)
                    strTab = val;
                else if (tag == DT_SYMTAB)
                    symTab = val;
                else if (tag == DT_GNU_HASH)
                    gnuHash = val;
            }

            if (strTab == 0 || symTab == 0 || gnuHash == 0)
                return false;

            // In a live image glibc's loader has already relocated d_ptr entries to absolute addresses
            // (that is what RELRO protects after relocation), which is also what modern ClrMD assumes.
            // Hedge anyway: an unrelocated value is module-relative and thus far below the load base.
            strTab = Rebase(strTab, imageBase);
            symTab = Rebase(symTab, imageBase);
            gnuHash = Rebase(gnuHash, imageBase);

            // GNU hash header: nbuckets, symoffset, bloomsize, bloomshift (4 x int32),
            // then bloomsize x uint64 bloom words, then nbuckets x int32 buckets, then the chain array.
            byte[] hashHeader = new byte[16];
            if (!ReadFull(reader, gnuHash, hashHeader))
                return false;

            int bucketCount = BitConverter.ToInt32(hashHeader, 0);
            int symOffset = BitConverter.ToInt32(hashHeader, 4);
            int bloomSize = BitConverter.ToInt32(hashHeader, 8);

            if (bucketCount <= 0 || bucketCount >= 0x10000000 || symOffset <= 0 || bloomSize < 0)
                return false;

            ulong bucketsAddr = gnuHash + 16 + (ulong)(8 * bloomSize);
            ulong chainsAddr = bucketsAddr + (ulong)(4 * bucketCount);

            uint hash = GnuHash(symbolName);

            byte[] intBuf = new byte[4];
            if (!ReadFull(reader, bucketsAddr + (ulong)(4 * (hash % (uint)bucketCount)), intBuf))
                return false;

            int index = BitConverter.ToInt32(intBuf, 0) - symOffset;
            if (index < 0)
                return false;

            for (int iter = 0; iter < MaxChainIterations; iter++, index++)
            {
                if (!ReadFull(reader, chainsAddr + (ulong)(4 * index), intBuf))
                    return false;

                uint chainVal = BitConverter.ToUInt32(intBuf, 0);
                if ((chainVal & 0xfffffffe) == (hash & 0xfffffffe))
                {
                    // Candidate: read the Elf64_Sym (24 bytes) and compare the name.
                    byte[] sym = new byte[24];
                    if (!ReadFull(reader, symTab + (ulong)((index + symOffset) * 24), sym))
                        return false;

                    uint nameOffset = BitConverter.ToUInt32(sym, 0);
                    if (ReadNullTerminated(reader, strTab + nameOffset) == symbolName)
                    {
                        ulong value = BitConverter.ToUInt64(sym, 8);
                        if (value == 0)
                            return false;

                        address = Rebase(value, imageBase);
                        return true;
                    }
                }

                if ((chainVal & 1) == 1)
                    break; // end of this bucket's chain
            }

            return false;
        }

        private static ulong Rebase(ulong value, ulong imageBase)
        {
            return value < imageBase ? value + imageBase : value;
        }

        // The GNU hash function (djb2/x33 over the UTF-8 symbol name).
        private static uint GnuHash(string name)
        {
            uint h = 5381;
            foreach (byte b in Encoding.UTF8.GetBytes(name))
                h = unchecked((h << 5) + h + b);
            return h;
        }

        private static string ReadNullTerminated(IDataReader reader, ulong address)
        {
            StringBuilder sb = new StringBuilder(64);
            byte[] chunk = new byte[64];
            for (int i = 0; i < 8; i++) // symbol names of interest are well under 512 chars
            {
                int read;
                if (!reader.ReadMemory(address + (ulong)(i * chunk.Length), chunk, chunk.Length, out read) || read == 0)
                    break;

                for (int j = 0; j < read; j++)
                {
                    if (chunk[j] == 0)
                        return sb.ToString();
                    sb.Append((char)chunk[j]);
                }
            }

            return sb.ToString();
        }

        private static bool ReadFull(IDataReader reader, ulong address, byte[] buffer)
        {
            int read;
            return reader.ReadMemory(address, buffer, buffer.Length, out read) && read == buffer.Length;
        }
    }
}
