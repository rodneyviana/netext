# NetExt Version History

## 3.0.0.5000

### Linux .NET core dump support (new)

NetExt can now debug Linux .NET core dumps (ELF format) opened in WinDbg on a Windows host:

- Runtime detection now recognizes `libcoreclr.so` (surfaced by dbgeng as module `libcoreclr`), both in the extension (`INIT_API`) and in the managed shim's `DataTarget` version discovery. New `isLinuxTarget` global reflects the target environment.
- The DAC for a Linux target is located by ELF build-id instead of PE timestamp/size: the build-id is read directly from the target's `libcoreclr.so` memory (GNU build-id note) and used to fetch the matching cross-OS `mscordaccore.dll` from the symbol server (`mscordaccore.dll/elf-buildid-coreclr-<hash>/`), mirroring WinDbg's own `.cordll` behavior.
- DAC loading now performs the PAL bootstrap (`DAC_PAL_InitializeDLL`/`PAL_InitializeDLL` + manual `DllMain`) required by PAL-based DAC binaries; classic Windows DACs are unaffected.
- The DAC data target now answers `ICLRRuntimeLocator` (runtime base address) and `ICLRContractLocator` (cDAC contract descriptor, resolved from the ELF dynamic symbol table for .NET 9+ targets).
- `!wstack` works without a TEB: on Linux targets stack bounds are derived from the stack pointer's memory region (with page-probing fallback).
- Memory validation (`IsValidMemory`) falls back to direct memory probing when dbgeng cannot answer region queries for ELF dumps, and range end checks no longer probe one byte past the mapping.

### GC heap fixes for modern .NET (affects Windows dumps too)

- **GC regions (default since .NET 7):** heap enumeration previously walked only the gen2 and LOH segment chains, silently missing every gen0/gen1 region — undercounting objects by an order of magnitude on regions-enabled dumps. All generation chains are now walked, and each region reports its correct generation.
- **Pinned Object Heap (.NET 5+):** POH segments are now enumerated via `ISOSDacInterface8`, previously missed entirely.
- Segment walking is hardened with cycle/duplicate protection.
- Older runtimes (.NET Framework 2.0–4.8, .NET Core 3.1, .NET Native) use the exact same code paths as before — segment-mode behavior is unchanged.

### Toolchain

- Projects and solutions updated for Visual Studio 2026.
- Boost.Spirit now consumed as a Boost NuGet package instead of vendored sources.
