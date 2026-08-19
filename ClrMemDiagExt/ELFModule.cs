using Microsoft.Diagnostics.Runtime.Interop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NetExt.Shim
{
    public sealed class ELFHeader
    {
        public byte Class { get; internal set; }
        public byte Endianness { get; internal set; }
        public ushort Type { get; internal set; }
        public ushort Machine { get; internal set; }
        public ulong Entry { get; internal set; }
        public ulong ProgramHeaderOffset { get; internal set; }
        public ulong SectionHeaderOffset { get; internal set; }
        public ushort ProgramHeaderEntrySize { get; internal set; }
        public ushort ProgramHeaderCount { get; internal set; }
        public ushort SectionHeaderEntrySize { get; internal set; }
        public ushort SectionHeaderCount { get; internal set; }
        public ushort SectionNameIndex { get; internal set; }
    }

    public sealed class ELFProgramHeader
    {
        public uint Type { get; internal set; }
        public uint Flags { get; internal set; }
        public ulong FileOffset { get; internal set; }
        public ulong VirtualAddress { get; internal set; }
        public ulong FileSize { get; internal set; }
        public ulong MemorySize { get; internal set; }
        public ulong Alignment { get; internal set; }
    }

    public class ELFModule : Module
    {
        private const uint ELFMagic = 0x464c457f;
        private const byte ELFClass64 = 2;
        private const byte ELFLittleEndian = 1;
        private const ushort EMX86_64 = 62;
        private const uint PTLoad = 1;
        private const uint PTDynamic = 2;
        private const uint PTInterp = 3;
        private const uint PTNote = 4;
        private const uint PTTls = 7;
        private const uint PTGnuEhFrame = 0x6474e550;
        private const uint PTGnuRelro = 0x6474e552;
        private const long DTNull = 0;
        private const long DTPltRelSize = 2;
        private const long DTPltGot = 3;
        private const long DTHash = 4;
        private const long DTStrTab = 5;
        private const long DTSymTab = 6;
        private const long DTRela = 7;
        private const long DTRelaSize = 8;
        private const long DTRelaEntry = 9;
        private const long DTStrSize = 10;
        private const long DTSymEntry = 11;
        private const long DTInit = 12;
        private const long DTFini = 13;
        private const long DTDebug = 21;
        private const long DTJumpRel = 23;
        private const long DTInitArray = 25;
        private const long DTFiniArray = 26;
        private const long DTInitArraySize = 27;
        private const long DTFiniArraySize = 28;
        private const long DTGnuHash = 0x6ffffef5;
        private const long DTVerSym = 0x6ffffff0;
        private const long DTVerNeed = 0x6ffffffe;
        private const long DTVerNeedNum = 0x6fffffff;
        private const uint RX86_64_64 = 1;
        private const uint RX86_64GlobDat = 6;
        private const uint RX86_64JumpSlot = 7;
        private const uint RX86_64Relative = 8;
        private const uint RX86_64IRelative = 37;

        private const uint SHTNull = 0;
        private const uint SHTProgBits = 1;
        private const uint SHTStrTab = 3;
        private const uint SHTRela = 4;
        private const uint SHTHash = 5;
        private const uint SHTDynamic = 6;
        private const uint SHTNote = 7;
        private const uint SHTNoBits = 8;
        private const uint SHTDynSym = 11;
        private const uint SHTInitArray = 14;
        private const uint SHTFiniArray = 15;
        private const uint SHTGnuHash = 0x6ffffff6;
        private const uint SHTGnuVerNeed = 0x6ffffffe;
        private const uint SHTGnuVerSym = 0x6fffffff;

        private const ulong SHFWrite = 0x1;
        private const ulong SHFAlloc = 0x2;
        private const ulong SHFExecInstr = 0x4;
        private const ulong SHFInfoLink = 0x40;
        private const ulong SHFTls = 0x400;

        private const ulong SectionHeaderSize = 64;

        private const uint PFExecute = 1;
        private const uint PFWrite = 2;

        private delegate bool MemoryReader(ulong address, byte[] buffer);

        private sealed class ELFSection
        {
            public string Name;
            public uint Type;
            public ulong Flags;
            public ulong Address;
            public ulong FileOffset;
            public ulong Size;
            public bool SizeKnown;
            public ulong EntrySize;
            public ulong Alignment;
            public uint Info;
            public string LinkName;
            public string InfoName;
        }

        public ELFHeader Header { get; private set; }
        public IList<ELFProgramHeader> ProgramHeaders { get; private set; }

        public ELFModule(ulong offset)
            : base(offset)
        {
            LoadMetadata(ReadTargetMemory);
        }

        public ELFModule(string moduleName)
            : base(moduleName)
        {
            LoadMetadata(ReadTargetMemory);
        }

        public override bool SaveToStream(Stream moduleStream)
        {
            return SaveFromTarget(BaseAddress, moduleStream);
        }

        public static bool SaveToStream(Stream loadedImage, ulong baseAddress, Stream moduleStream)
        {
            if (loadedImage == null || !loadedImage.CanRead || !loadedImage.CanSeek)
                throw new ArgumentException("The loaded ELF image must be a readable, seekable stream.", "loadedImage");

            MemoryReader reader = delegate(ulong address, byte[] buffer)
            {
                if (address < baseAddress)
                    return false;

                ulong offset = address - baseAddress;
                if (offset > (ulong)loadedImage.Length || (ulong)buffer.Length > (ulong)loadedImage.Length - offset)
                    return false;

                loadedImage.Position = (long)offset;
                int total = 0;
                while (total < buffer.Length)
                {
                    int read = loadedImage.Read(buffer, total, buffer.Length - total);
                    if (read == 0)
                        return false;
                    total += read;
                }
                return true;
            };

            return SaveImage(baseAddress, moduleStream, reader);
        }

        internal static bool IsELFImage(ulong baseAddress)
        {
            byte[] magic = new byte[4];
            return baseAddress != 0 && ReadTargetMemory(baseAddress, magic) && ReadUInt32(magic, 0) == ELFMagic;
        }

        internal static bool SaveFromTarget(ulong baseAddress, Stream moduleStream)
        {
            return SaveImage(baseAddress, moduleStream, ReadTargetMemory);
        }

        private void LoadMetadata(MemoryReader reader)
        {
            ELFHeader header;
            List<ELFProgramHeader> programHeaders;
            ulong loadBias;
            if (TryReadMetadata(BaseAddress, reader, out header, out programHeaders, out loadBias))
            {
                Header = header;
                ProgramHeaders = programHeaders.AsReadOnly();
            }
            else
            {
                ProgramHeaders = new List<ELFProgramHeader>().AsReadOnly();
            }
        }

        private static bool SaveImage(ulong baseAddress, Stream moduleStream, MemoryReader reader)
        {
            if (moduleStream == null || !moduleStream.CanWrite || !moduleStream.CanSeek)
                throw new ArgumentException("The output stream must be writable and seekable.", "moduleStream");

            ELFHeader header;
            List<ELFProgramHeader> programHeaders;
            ulong loadBias;
            if (!TryReadMetadata(baseAddress, reader, out header, out programHeaders, out loadBias))
                return false;

            if (header.Class != ELFClass64 || header.Endianness != ELFLittleEndian || header.Machine != EMX86_64)
            {
                DebugApi.WriteLine("Only little-endian x86-64 ELF images are currently supported");
                return false;
            }

            List<ELFProgramHeader> loadSegments = programHeaders.FindAll(delegate(ELFProgramHeader item) { return item.Type == PTLoad; });
            if (loadSegments.Count == 0)
                return false;

            ulong fileSize = 0;
            foreach (ELFProgramHeader segment in loadSegments)
            {
                if (segment.FileOffset > ulong.MaxValue - segment.FileSize)
                    return false;
                fileSize = Math.Max(fileSize, segment.FileOffset + segment.FileSize);
            }

            if (fileSize > Int32.MaxValue)
            {
                DebugApi.WriteLine("ELF images larger than 2 GB are not currently supported");
                return false;
            }

            byte[] image = new byte[(int)fileSize];
            foreach (ELFProgramHeader segment in loadSegments)
            {
                if (segment.FileSize > Int32.MaxValue)
                    return false;

                byte[] content = new byte[(int)segment.FileSize];
                if (!reader(loadBias + segment.VirtualAddress, content))
                {
                    DebugApi.WriteLine("Unable to read ELF segment at %p", loadBias + segment.VirtualAddress);
                    return false;
                }
                Buffer.BlockCopy(content, 0, image, (int)segment.FileOffset, content.Length);
            }

            ELFProgramHeader dynamicSegment = programHeaders.Find(delegate(ELFProgramHeader item) { return item.Type == PTDynamic; });
            Dictionary<long, ulong> tags = new Dictionary<long, ulong>();
            List<ulong> pltStubs = new List<ulong>();
            if (dynamicSegment != null && !RestoreDynamicState(image, loadBias, loadSegments, dynamicSegment, reader, tags, pltStubs))
                return false;

            image = AppendSectionHeaders(image, programHeaders, loadSegments, tags, loadBias, pltStubs);

            moduleStream.SetLength(0);
            moduleStream.Position = 0;
            moduleStream.Write(image, 0, image.Length);
            return true;
        }

        private static bool RestoreDynamicState(byte[] image, ulong loadBias, List<ELFProgramHeader> loadSegments,
            ELFProgramHeader dynamicSegment, MemoryReader reader, Dictionary<long, ulong> tags, List<ulong> pltStubs)
        {
            if (dynamicSegment.FileSize > Int32.MaxValue || dynamicSegment.FileSize % 16 != 0)
                return false;

            byte[] dynamicBytes = new byte[(int)dynamicSegment.FileSize];
            if (!reader(loadBias + dynamicSegment.VirtualAddress, dynamicBytes))
                return false;

            for (int offset = 0; offset + 16 <= dynamicBytes.Length; offset += 16)
            {
                long tag = ReadInt64(dynamicBytes, offset);
                ulong value = ReadUInt64(dynamicBytes, offset + 8);
                tags[tag] = value;

                ulong originalValue = value;
                if (tag == DTDebug)
                    originalValue = 0;
                else if (IsRuntimeAddress(value, loadBias, loadSegments))
                    originalValue -= loadBias;

                ulong entryAddress = dynamicSegment.VirtualAddress + (ulong)offset + 8;
                int fileOffset;
                if (!TryVirtualAddressToFileOffset(entryAddress, 8, loadSegments, out fileOffset))
                    return false;
                WriteUInt64(image, fileOffset, originalValue);

                if (tag == DTNull)
                    break;
            }

            ulong relaAddress;
            ulong relaSize;
            ulong relaEntrySize;
            if (tags.TryGetValue(DTRela, out relaAddress) && tags.TryGetValue(DTRelaSize, out relaSize))
            {
                if (!tags.TryGetValue(DTRelaEntry, out relaEntrySize))
                    relaEntrySize = 24;
                if (!RestoreRelocations(image, loadBias, loadSegments, reader, relaAddress, relaSize, relaEntrySize, pltStubs))
                    return false;
            }

            ulong jumpRelAddress;
            ulong pltRelSize;
            if (tags.TryGetValue(DTJumpRel, out jumpRelAddress) && tags.TryGetValue(DTPltRelSize, out pltRelSize))
            {
                if (!RestoreRelocations(image, loadBias, loadSegments, reader, jumpRelAddress, pltRelSize, 24, pltStubs))
                    return false;
            }

            return true;
        }

        private static bool RestoreRelocations(byte[] image, ulong loadBias, List<ELFProgramHeader> loadSegments,
            MemoryReader reader, ulong runtimeAddress, ulong size, ulong entrySize, List<ulong> pltStubs)
        {
            if (entrySize < 24 || size > Int32.MaxValue || size % entrySize != 0)
                return false;

            if (!IsRuntimeAddress(runtimeAddress, loadBias, loadSegments))
                runtimeAddress += loadBias;

            byte[] relocations = new byte[(int)size];
            if (!reader(runtimeAddress, relocations))
                return false;

            for (ulong offset = 0; offset < size; offset += entrySize)
            {
                int entryOffset = (int)offset;
                ulong targetAddress = ReadUInt64(relocations, entryOffset);
                uint type = (uint)ReadUInt64(relocations, entryOffset + 8);
                int targetFileOffset;
                if (!TryVirtualAddressToFileOffset(targetAddress, 8, loadSegments, out targetFileOffset))
                    continue;

                if (type == RX86_64JumpSlot)
                {
                    ulong pltAddress;
                    if (!TryFindPltTarget(image, targetAddress, loadSegments, out pltAddress))
                    {
                        DebugApi.WriteLine("Unable to restore the ELF PLT slot at {0:x}", targetAddress);
                        return false;
                    }
                    WriteUInt64(image, targetFileOffset, pltAddress);
                    pltStubs.Add(pltAddress - 6);
                }
                else if (type == RX86_64_64 || type == RX86_64GlobDat || type == RX86_64Relative || type == RX86_64IRelative)
                {
                    WriteUInt64(image, targetFileOffset, 0);
                }
                else if (type != 0)
                {
                    DebugApi.WriteLine("Unsupported x86-64 ELF relocation type {0} at {1:x}", type, targetAddress);
                    return false;
                }
            }

            return true;
        }

        private static bool TryFindPltTarget(byte[] image, ulong relocationAddress, List<ELFProgramHeader> loadSegments,
            out ulong pltAddress)
        {
            foreach (ELFProgramHeader segment in loadSegments)
            {
                if ((segment.Flags & 1) == 0 || segment.FileSize < 6)
                    continue;

                int start = (int)segment.FileOffset;
                int end = start + (int)segment.FileSize - 6;
                for (int offset = start; offset <= end; offset++)
                {
                    if (image[offset] != 0xff || image[offset + 1] != 0x25)
                        continue;

                    long instructionAddress = (long)segment.VirtualAddress + offset - start;
                    long target = instructionAddress + 6 + ReadInt32(image, offset + 2);
                    if (target >= 0 && (ulong)target == relocationAddress)
                    {
                        pltAddress = (ulong)instructionAddress + 6;
                        return true;
                    }
                }
            }

            pltAddress = 0;
            return false;
        }

        private static bool TryReadMetadata(ulong baseAddress, MemoryReader reader, out ELFHeader header,
            out List<ELFProgramHeader> programHeaders, out ulong loadBias)
        {
            header = null;
            programHeaders = new List<ELFProgramHeader>();
            loadBias = 0;

            byte[] headerBytes = new byte[64];
            if (baseAddress == 0 || !reader(baseAddress, headerBytes) || ReadUInt32(headerBytes, 0) != ELFMagic)
                return false;

            header = new ELFHeader();
            header.Class = headerBytes[4];
            header.Endianness = headerBytes[5];
            header.Type = ReadUInt16(headerBytes, 0x10);
            header.Machine = ReadUInt16(headerBytes, 0x12);
            header.Entry = ReadUInt64(headerBytes, 0x18);
            header.ProgramHeaderOffset = ReadUInt64(headerBytes, 0x20);
            header.SectionHeaderOffset = ReadUInt64(headerBytes, 0x28);
            header.ProgramHeaderEntrySize = ReadUInt16(headerBytes, 0x36);
            header.ProgramHeaderCount = ReadUInt16(headerBytes, 0x38);
            header.SectionHeaderEntrySize = ReadUInt16(headerBytes, 0x3a);
            header.SectionHeaderCount = ReadUInt16(headerBytes, 0x3c);
            header.SectionNameIndex = ReadUInt16(headerBytes, 0x3e);

            if (header.Class != ELFClass64 || header.Endianness != ELFLittleEndian ||
                header.ProgramHeaderEntrySize < 56 || header.ProgramHeaderCount == 0)
                return false;

            ulong tableSize = (ulong)header.ProgramHeaderEntrySize * header.ProgramHeaderCount;
            if (tableSize > Int32.MaxValue)
                return false;

            byte[] table = new byte[(int)tableSize];
            if (!reader(baseAddress + header.ProgramHeaderOffset, table))
                return false;

            ELFProgramHeader firstLoad = null;
            for (int index = 0; index < header.ProgramHeaderCount; index++)
            {
                int offset = index * header.ProgramHeaderEntrySize;
                ELFProgramHeader item = new ELFProgramHeader();
                item.Type = ReadUInt32(table, offset);
                item.Flags = ReadUInt32(table, offset + 4);
                item.FileOffset = ReadUInt64(table, offset + 8);
                item.VirtualAddress = ReadUInt64(table, offset + 16);
                item.FileSize = ReadUInt64(table, offset + 32);
                item.MemorySize = ReadUInt64(table, offset + 40);
                item.Alignment = ReadUInt64(table, offset + 48);
                programHeaders.Add(item);

                if (item.Type == PTLoad && item.FileOffset == 0 && firstLoad == null)
                    firstLoad = item;
            }

            if (firstLoad == null || baseAddress < firstLoad.VirtualAddress)
                return false;

            loadBias = baseAddress - firstLoad.VirtualAddress;
            return true;
        }

        private static bool IsRuntimeAddress(ulong address, ulong loadBias, List<ELFProgramHeader> loadSegments)
        {
            foreach (ELFProgramHeader segment in loadSegments)
            {
                ulong start = loadBias + segment.VirtualAddress;
                if (address >= start && address - start < segment.MemorySize)
                    return true;
            }
            return false;
        }

        private static bool TryVirtualAddressToFileOffset(ulong address, ulong size,
            List<ELFProgramHeader> loadSegments, out int fileOffset)
        {
            foreach (ELFProgramHeader segment in loadSegments)
            {
                if (address < segment.VirtualAddress)
                    continue;
                ulong relative = address - segment.VirtualAddress;
                if (relative <= segment.FileSize && size <= segment.FileSize - relative)
                {
                    ulong offset = segment.FileOffset + relative;
                    if (offset <= Int32.MaxValue)
                    {
                        fileOffset = (int)offset;
                        return true;
                    }
                }
            }

            fileOffset = 0;
            return false;
        }

        // The loader never maps the section header table, so it is rebuilt from PT_DYNAMIC and the program
        // headers. Sizes only the original table recorded are inferred from the start of the next section.
        private static byte[] AppendSectionHeaders(byte[] image, List<ELFProgramHeader> programHeaders,
            List<ELFProgramHeader> loadSegments, Dictionary<long, ulong> tags, ulong loadBias, List<ulong> pltStubs)
        {
            List<ELFSection> sections = BuildSections(image, programHeaders, loadSegments, tags, loadBias, pltStubs);

            ELFSection names = MakeSection(".shstrtab", SHTStrTab, 0, 0, 0, true, 0, 1);
            sections.Add(names);

            byte[] stringTable;
            Dictionary<string, uint> nameOffsets;
            BuildStringTable(sections, out stringTable, out nameOffsets);

            names.FileOffset = (ulong)image.Length;
            names.Size = (ulong)stringTable.Length;

            ulong tableOffset = Align(names.FileOffset + names.Size, 8);
            ulong total = tableOffset + SectionHeaderSize * (ulong)sections.Count;
            if (total > Int32.MaxValue)
                return image;

            Array.Resize(ref image, (int)total);
            Buffer.BlockCopy(stringTable, 0, image, (int)names.FileOffset, stringTable.Length);

            for (int index = 0; index < sections.Count; index++)
            {
                ELFSection section = sections[index];
                int offset = (int)(tableOffset + SectionHeaderSize * (ulong)index);
                WriteUInt32(image, offset, nameOffsets[section.Name]);
                WriteUInt32(image, offset + 4, section.Type);
                WriteUInt64(image, offset + 8, section.Flags);
                WriteUInt64(image, offset + 16, section.Address);
                WriteUInt64(image, offset + 24, section.FileOffset);
                WriteUInt64(image, offset + 32, section.Size);
                WriteUInt32(image, offset + 40, IndexOfSection(sections, section.LinkName));
                WriteUInt32(image, offset + 44, section.InfoName != null ? IndexOfSection(sections, section.InfoName) : section.Info);
                WriteUInt64(image, offset + 48, section.Alignment);
                WriteUInt64(image, offset + 56, section.EntrySize);
            }

            WriteUInt64(image, 0x28, tableOffset);
            WriteUInt16(image, 0x3a, (ushort)SectionHeaderSize);
            WriteUInt16(image, 0x3c, (ushort)sections.Count);
            WriteUInt16(image, 0x3e, (ushort)(sections.Count - 1));
            return image;
        }

        private static List<ELFSection> BuildSections(byte[] image, List<ELFProgramHeader> programHeaders,
            List<ELFProgramHeader> loadSegments, Dictionary<long, ulong> tags, ulong loadBias, List<ulong> pltStubs)
        {
            List<ELFSection> allocated = new List<ELFSection>();
            List<ELFSection> noBits = new List<ELFSection>();

            foreach (ELFProgramHeader segment in programHeaders)
            {
                switch (segment.Type)
                {
                    case PTInterp:
                        allocated.Add(MakeSection(".interp", SHTProgBits, SHFAlloc, segment.VirtualAddress, segment.FileSize, true, 0, 1));
                        break;

                    case PTNote:
                        AddNoteSections(allocated, image, segment);
                        break;

                    case PTGnuEhFrame:
                        allocated.Add(MakeSection(".eh_frame_hdr", SHTProgBits, SHFAlloc, segment.VirtualAddress, segment.FileSize, true, 0, 4));
                        break;

                    case PTDynamic:
                    {
                        ELFSection dynamic = MakeSection(".dynamic", SHTDynamic, SHFAlloc | SHFWrite, segment.VirtualAddress, segment.FileSize, true, 16, 8);
                        dynamic.LinkName = ".dynstr";
                        allocated.Add(dynamic);
                        break;
                    }

                    case PTTls:
                        if (segment.MemorySize > segment.FileSize)
                            noBits.Add(MakeSection(".tbss", SHTNoBits, SHFAlloc | SHFWrite | SHFTls, segment.VirtualAddress, segment.MemorySize, true, 0, 8));
                        break;
                }
            }

            AddDynamicSections(allocated, tags, loadBias, loadSegments);
            AddPltSection(allocated, loadSegments, image, pltStubs);
            ResolveSizesAndGaps(allocated, programHeaders, loadSegments, image);
            AddNoBitsSections(noBits, programHeaders, loadSegments);

            List<ELFSection> ordered = new List<ELFSection>(allocated);
            ordered.AddRange(noBits);
            ordered.Sort(CompareSections);
            ordered.Insert(0, MakeSection("", SHTNull, 0, 0, 0, true, 0, 0));

            foreach (ELFSection section in ordered)
            {
                if (section.Type != SHTNull)
                    section.FileOffset = AddressToFileOffset(section.Address, loadSegments);
            }

            return ordered;
        }

        private static void AddDynamicSections(List<ELFSection> allocated, Dictionary<long, ulong> tags,
            ulong loadBias, List<ELFProgramHeader> loadSegments)
        {
            ulong address;
            ulong size;

            if (TryGetAddress(tags, DTHash, loadBias, loadSegments, out address))
                AddLinked(allocated, ".hash", SHTHash, SHFAlloc, address, 0, false, 4, 8, ".dynsym");

            if (TryGetAddress(tags, DTGnuHash, loadBias, loadSegments, out address))
                AddLinked(allocated, ".gnu.hash", SHTGnuHash, SHFAlloc, address, 0, false, 0, 8, ".dynsym");

            if (TryGetAddress(tags, DTSymTab, loadBias, loadSegments, out address))
            {
                ulong entrySize;
                if (!tags.TryGetValue(DTSymEntry, out entrySize))
                    entrySize = 24;
                AddLinked(allocated, ".dynsym", SHTDynSym, SHFAlloc, address, 0, false, entrySize, 8, ".dynstr").Info = 1;
            }

            if (TryGetAddress(tags, DTStrTab, loadBias, loadSegments, out address))
            {
                bool known = tags.TryGetValue(DTStrSize, out size);
                allocated.Add(MakeSection(".dynstr", SHTStrTab, SHFAlloc, address, known ? size : 0, known, 0, 1));
            }

            if (TryGetAddress(tags, DTVerSym, loadBias, loadSegments, out address))
                AddLinked(allocated, ".gnu.version", SHTGnuVerSym, SHFAlloc, address, 0, false, 2, 2, ".dynsym");

            if (TryGetAddress(tags, DTVerNeed, loadBias, loadSegments, out address))
            {
                ELFSection needed = AddLinked(allocated, ".gnu.version_r", SHTGnuVerNeed, SHFAlloc, address, 0, false, 0, 8, ".dynstr");
                ulong count;
                if (tags.TryGetValue(DTVerNeedNum, out count))
                    needed.Info = (uint)count;
            }

            if (TryGetAddress(tags, DTRela, loadBias, loadSegments, out address) && tags.TryGetValue(DTRelaSize, out size))
                AddLinked(allocated, ".rela.dyn", SHTRela, SHFAlloc, address, size, true, 24, 8, ".dynsym");

            if (TryGetAddress(tags, DTJumpRel, loadBias, loadSegments, out address) && tags.TryGetValue(DTPltRelSize, out size))
                AddLinked(allocated, ".rela.plt", SHTRela, SHFAlloc | SHFInfoLink, address, size, true, 24, 8, ".dynsym").InfoName = ".got.plt";

            if (TryGetAddress(tags, DTInit, loadBias, loadSegments, out address))
                allocated.Add(MakeSection(".init", SHTProgBits, SHFAlloc | SHFExecInstr, address, 0, false, 0, 4));

            if (TryGetAddress(tags, DTFini, loadBias, loadSegments, out address))
                allocated.Add(MakeSection(".fini", SHTProgBits, SHFAlloc | SHFExecInstr, address, 0, false, 0, 4));

            if (TryGetAddress(tags, DTInitArray, loadBias, loadSegments, out address) && tags.TryGetValue(DTInitArraySize, out size))
                allocated.Add(MakeSection(".init_array", SHTInitArray, SHFAlloc | SHFWrite, address, size, true, 8, 8));

            if (TryGetAddress(tags, DTFiniArray, loadBias, loadSegments, out address) && tags.TryGetValue(DTFiniArraySize, out size))
                allocated.Add(MakeSection(".fini_array", SHTFiniArray, SHFAlloc | SHFWrite, address, size, true, 8, 8));

            if (TryGetAddress(tags, DTPltGot, loadBias, loadSegments, out address))
                allocated.Add(MakeSection(".got.plt", SHTProgBits, SHFAlloc | SHFWrite, address, 0, false, 8, 8));
        }

        private static void AddPltSection(List<ELFSection> allocated, List<ELFProgramHeader> loadSegments,
            byte[] image, List<ulong> pltStubs)
        {
            if (pltStubs.Count == 0)
                return;

            ulong start = pltStubs[0];
            ulong end = pltStubs[0];
            foreach (ulong stub in pltStubs)
            {
                start = Math.Min(start, stub);
                end = Math.Max(end, stub);
            }

            start &= ~15UL;
            end = Align(end + 6, 16);

            // A lazy-capable PLT begins with a header entry (push GOT+8; jmp *GOT+16) before the first stub.
            int headerOffset;
            if (start >= 16 && TryVirtualAddressToFileOffset(start - 16, 2, loadSegments, out headerOffset) &&
                image[headerOffset] == 0xff && image[headerOffset + 1] == 0x35)
                start -= 16;

            allocated.Add(MakeSection(".plt", SHTProgBits, SHFAlloc | SHFExecInstr, start, end - start, true, 16, 16));
        }

        private static void AddNoteSections(List<ELFSection> allocated, byte[] image, ELFProgramHeader segment)
        {
            ulong offset = segment.FileOffset;
            ulong end = offset + segment.FileSize;
            if (end > (ulong)image.Length)
                return;

            while (offset + 12 <= end)
            {
                uint nameSize = ReadUInt32(image, (int)offset);
                uint descriptionSize = ReadUInt32(image, (int)offset + 4);
                uint noteType = ReadUInt32(image, (int)offset + 8);
                ulong total = 12 + Align(nameSize, 4) + Align(descriptionSize, 4);
                if (total <= 12 || offset + total > end)
                    break;

                string owner = nameSize > 1 ? Encoding.ASCII.GetString(image, (int)offset + 12, (int)nameSize - 1) : String.Empty;
                string name = ".note";
                if (owner == "GNU" && noteType == 1)
                    name = ".note.ABI-tag";
                else if (owner == "GNU" && noteType == 3)
                    name = ".note.gnu.build-id";
                else if (owner.Length > 0)
                    name = ".note." + owner.ToLowerInvariant();

                allocated.Add(MakeSection(name, SHTNote, SHFAlloc,
                    segment.VirtualAddress + (offset - segment.FileOffset), total, true, 0, 4));
                offset += total;
            }
        }

        private static void ResolveSizesAndGaps(List<ELFSection> allocated, List<ELFProgramHeader> programHeaders,
            List<ELFProgramHeader> loadSegments, byte[] image)
        {
            List<ELFSection> gaps = new List<ELFSection>();

            foreach (ELFProgramHeader segment in loadSegments)
            {
                ulong segmentEnd = segment.VirtualAddress + segment.FileSize;
                List<ELFSection> inSegment = new List<ELFSection>();
                foreach (ELFSection section in allocated)
                {
                    if (section.Address >= segment.VirtualAddress && section.Address < segmentEnd)
                        inSegment.Add(section);
                }
                inSegment.Sort(CompareSections);

                for (int index = 0; index < inSegment.Count; index++)
                {
                    ELFSection section = inSegment[index];
                    ulong limit = index + 1 < inSegment.Count ? inSegment[index + 1].Address : segmentEnd;
                    if (!section.SizeKnown)
                    {
                        section.Size = limit > section.Address ? limit - section.Address : 0;
                        section.SizeKnown = true;
                    }
                    else if (section.Address + section.Size > segmentEnd)
                        section.Size = segmentEnd - section.Address;
                }

                // The first segment also maps the ELF header and program headers, which own no section.
                ulong cursor = segment.FileOffset == 0
                    ? (inSegment.Count > 0 ? inSegment[0].Address : segmentEnd)
                    : segment.VirtualAddress;
                string previous = null;

                for (int index = 0; index < inSegment.Count; index++)
                {
                    ELFSection section = inSegment[index];
                    if (section.Address > cursor)
                        AddGap(gaps, image, programHeaders, segment, cursor, section.Address, previous, section.Name);
                    cursor = Math.Max(cursor, section.Address + section.Size);
                    previous = section.Name;
                }

                if (segmentEnd > cursor)
                    AddGap(gaps, image, programHeaders, segment, cursor, segmentEnd, previous, null);
            }

            allocated.AddRange(gaps);
        }

        private static void AddNoBitsSections(List<ELFSection> noBits, List<ELFProgramHeader> programHeaders,
            List<ELFProgramHeader> loadSegments)
        {
            foreach (ELFProgramHeader segment in loadSegments)
            {
                if (segment.MemorySize <= segment.FileSize)
                    continue;

                ulong address = segment.VirtualAddress + segment.FileSize;
                bool relro = programHeaders.Exists(delegate(ELFProgramHeader item)
                {
                    return item.Type == PTGnuRelro && address >= item.VirtualAddress &&
                        address < item.VirtualAddress + item.MemorySize;
                });

                ulong flags = SHFAlloc | ((segment.Flags & PFWrite) != 0 ? SHFWrite : 0);
                noBits.Add(MakeSection(relro ? ".relro_padding" : ".bss", SHTNoBits, flags, address,
                    segment.MemorySize - segment.FileSize, true, 0, relro ? 1UL : 16UL));
            }
        }

        private static void AddGap(List<ELFSection> gaps, byte[] image, List<ELFProgramHeader> programHeaders,
            ELFProgramHeader segment, ulong start, ulong end, string previous, string next)
        {
            // Skip linker alignment padding, but never in writable data, whose leading bytes are
            // legitimately zero once the relocations have been reversed.
            ulong offset = segment.FileOffset + (start - segment.VirtualAddress);
            ulong limit = (segment.Flags & PFWrite) != 0 ? start : Math.Min(start + 15, end);
            while (start < limit && offset < (ulong)image.Length && image[(int)offset] == 0)
            {
                start++;
                offset++;
            }

            if (end - start < 16)
                return;

            bool executable = (segment.Flags & PFExecute) != 0;
            bool writable = (segment.Flags & PFWrite) != 0;
            ulong address = start;
            bool relro = programHeaders.Exists(delegate(ELFProgramHeader item)
            {
                return item.Type == PTGnuRelro && address >= item.VirtualAddress &&
                    address < item.VirtualAddress + item.MemorySize;
            });

            string name;
            if (previous == ".eh_frame_hdr")
                name = ".eh_frame";
            else if (next == ".got.plt")
                name = ".got";
            else if (executable)
                name = ".text";
            else if (writable)
                name = relro ? ".data.rel.ro" : ".data";
            else
                name = ".rodata";

            ulong flags = SHFAlloc | (executable ? SHFExecInstr : 0) | (writable ? SHFWrite : 0);
            gaps.Add(MakeSection(name, SHTProgBits, flags, start, end - start, true, 0, executable ? 16UL : 8UL));
        }

        private static ELFSection AddLinked(List<ELFSection> allocated, string name, uint type, ulong flags, ulong address,
            ulong size, bool sizeKnown, ulong entrySize, ulong alignment, string linkName)
        {
            ELFSection section = MakeSection(name, type, flags, address, size, sizeKnown, entrySize, alignment);
            section.LinkName = linkName;
            allocated.Add(section);
            return section;
        }

        private static ELFSection MakeSection(string name, uint type, ulong flags, ulong address, ulong size,
            bool sizeKnown, ulong entrySize, ulong alignment)
        {
            ELFSection section = new ELFSection();
            section.Name = name;
            section.Type = type;
            section.Flags = flags;
            section.Address = address;
            section.Size = size;
            section.SizeKnown = sizeKnown;
            section.EntrySize = entrySize;
            section.Alignment = alignment;
            return section;
        }

        private static int CompareSections(ELFSection left, ELFSection right)
        {
            if (left.Address != right.Address)
                return left.Address < right.Address ? -1 : 1;
            if (left.Type == SHTNoBits && right.Type != SHTNoBits)
                return 1;
            if (right.Type == SHTNoBits && left.Type != SHTNoBits)
                return -1;
            return 0;
        }

        private static void BuildStringTable(List<ELFSection> sections, out byte[] table, out Dictionary<string, uint> offsets)
        {
            offsets = new Dictionary<string, uint>();
            offsets[String.Empty] = 0;

            List<byte> bytes = new List<byte>();
            bytes.Add(0);

            foreach (ELFSection section in sections)
            {
                if (offsets.ContainsKey(section.Name))
                    continue;
                offsets[section.Name] = (uint)bytes.Count;
                bytes.AddRange(Encoding.ASCII.GetBytes(section.Name));
                bytes.Add(0);
            }

            table = bytes.ToArray();
        }

        private static uint IndexOfSection(List<ELFSection> sections, string name)
        {
            if (name == null)
                return 0;

            for (int index = 0; index < sections.Count; index++)
            {
                if (sections[index].Name == name)
                    return (uint)index;
            }
            return 0;
        }

        private static bool TryGetAddress(Dictionary<long, ulong> tags, long tag, ulong loadBias,
            List<ELFProgramHeader> loadSegments, out ulong address)
        {
            if (!tags.TryGetValue(tag, out address) || address == 0)
                return false;

            if (IsRuntimeAddress(address, loadBias, loadSegments))
                address -= loadBias;
            return true;
        }

        private static ulong AddressToFileOffset(ulong address, List<ELFProgramHeader> loadSegments)
        {
            foreach (ELFProgramHeader segment in loadSegments)
            {
                if (address >= segment.VirtualAddress && address < segment.VirtualAddress + segment.MemorySize)
                    return segment.FileOffset + Math.Min(address - segment.VirtualAddress, segment.FileSize);
            }

            // A TLS or padding section can sit exactly at the end of its segment.
            foreach (ELFProgramHeader segment in loadSegments)
            {
                if (address == segment.VirtualAddress + segment.MemorySize)
                    return segment.FileOffset + segment.FileSize;
            }

            return 0;
        }

        private static ulong Align(ulong value, ulong alignment)
        {
            ulong remainder = value % alignment;
            return remainder == 0 ? value : value + alignment - remainder;
        }

        private static bool ReadTargetMemory(ulong address, byte[] buffer)
        {
            IDebugDataSpaces3 data = (IDebugDataSpaces3)DebugApi.Client;
            uint read;
            return data.ReadVirtual(address, buffer, (uint)buffer.Length, out read) == (int)HRESULT.S_OK && read == buffer.Length;
        }

        private static ushort ReadUInt16(byte[] buffer, int offset)
        {
            return (ushort)(buffer[offset] | buffer[offset + 1] << 8);
        }

        private static uint ReadUInt32(byte[] buffer, int offset)
        {
            return (uint)(buffer[offset] | buffer[offset + 1] << 8 | buffer[offset + 2] << 16 | buffer[offset + 3] << 24);
        }

        private static int ReadInt32(byte[] buffer, int offset)
        {
            return unchecked((int)ReadUInt32(buffer, offset));
        }

        private static ulong ReadUInt64(byte[] buffer, int offset)
        {
            return ReadUInt32(buffer, offset) | (ulong)ReadUInt32(buffer, offset + 4) << 32;
        }

        private static long ReadInt64(byte[] buffer, int offset)
        {
            return unchecked((long)ReadUInt64(buffer, offset));
        }

        private static void WriteUInt16(byte[] buffer, int offset, ushort value)
        {
            buffer[offset] = (byte)value;
            buffer[offset + 1] = (byte)(value >> 8);
        }

        private static void WriteUInt32(byte[] buffer, int offset, uint value)
        {
            for (int index = 0; index < 4; index++)
                buffer[offset + index] = (byte)(value >> (index * 8));
        }

        private static void WriteUInt64(byte[] buffer, int offset, ulong value)
        {
            for (int index = 0; index < 8; index++)
                buffer[offset + index] = (byte)(value >> (index * 8));
        }
    }
}