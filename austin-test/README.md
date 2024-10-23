# Building a split NativeAOT runtime

The goal is try to split off a chunk of the NativeAOT runtime into a DLL that can be referenced by
NativeAOT apps (and DLLs). The reason why you would want this are:

-   Binary Size: the native code (mostly the GC I think) sets a 1MB floor on every NativeAOT app.
-   GC behavior: the GC allocates heaps, threads, and virtual memory like it owns the place.
    If many NativeAOT DLLs are loaded into one process, these resources could be strained.
    I don't know if anyone has this problem, which leads to the last reason.
-   To see if it is possible.

The first step is that is in progress is to make a version of the runtime that is DLL that can only
be loaded once into a process. This would still be useful for the binary size goal, as multiple different
EXEs could share one runtime DLL.

Later the runtime will be modified so that multiple managed DLLs can be loaded, each with their own
managed world. That is, each will have their own definition of `System.Object` and the type hierarchy.

How to build runtime:

```batch
build.cmd -s clr.aot+libs -rc debug -lc release
```

# Basic design

Runtime.Base exports `RuntimeExport` functions from a DLL. Applications `RuntimeImport` functions
import the functions from Runtime.Base.

# TODO

Figure out how to trigger initialization when building libraries. Currently initialization relies on
a slow path in `RhpReversePInvoke` that is trigger when the a thread first enters the runtime. I guess
I could make it so that `DllMain` registers something and then adds a check in `RhpReversePInvoke` to
see if anything is registered? Things to consider:

* Try not add too much overhead. I'm not sure how bad having every entry point checking a global variable is.
* Avoid adding more ways of compiling the main runtime static library. (related to DllExport problem below)

Also thread local storage needs to take into account multiple managed worlds. Probably with an extra
layer of indirection, e.g. `RhGetThreadStaticStorage()` returns an `object[][][]`.

## ILC

`--runtimeknob` contains a mix of things used by managed code (which might not belong in the shared runtime)
and GC config which has to be in the shared runtime.

Somehow export the entrypoints defined in native code (defined with `FCIMPL` or `QCALLTYPE` or
random things like `g_cpuFeatures`) in a systematic manner.

## LINK

Add these back for a release build.

```
/NODEFAULTLIB:libucrt.lib
/DEFAULTLIB:ucrt.lib
/OPT:REF
/OPT:ICF
```

## Bug Reporting

Report that changes to `AsmOffsets.h` does not cause assembler files to be rebuilt.

Specially after rebasing over the [`ee_alloc_context` change](https://github.com/dotnet/runtime/pull/104851),
C++ files got rebuilt, but the assembler files (including `RhpPInvoke`) did not get rebuilt and had
stale offsets in them.
