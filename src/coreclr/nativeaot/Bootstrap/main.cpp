// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "main.h"

#if defined(HOST_X86) && defined(HOST_WINDOWS)
#define STRINGIFY(s) #s
#define MANAGED_RUNTIME_EXPORT_ALTNAME(_method) STRINGIFY(/alternatename:_##_method=_method)
#define MANAGED_RUNTIME_EXPORT(_name) \
    __pragma(comment (linker, MANAGED_RUNTIME_EXPORT_ALTNAME(_name))) \
    extern "C" void __cdecl _name();
#define MANAGED_RUNTIME_EXPORT_NAME(_name) _name
#define CDECL __cdecl
#else
#define MANAGED_RUNTIME_EXPORT(_name) \
    extern "C" void _name();
#define MANAGED_RUNTIME_EXPORT_NAME(_name) _name
#define CDECL
#endif

MANAGED_RUNTIME_EXPORT(GetRuntimeException)
MANAGED_RUNTIME_EXPORT(RuntimeFailFast)
MANAGED_RUNTIME_EXPORT(AppendExceptionStackFrame)
MANAGED_RUNTIME_EXPORT(GetSystemArrayEEType)
MANAGED_RUNTIME_EXPORT(OnFirstChanceException)
MANAGED_RUNTIME_EXPORT(OnUnhandledException)
MANAGED_RUNTIME_EXPORT(IDynamicCastableIsInterfaceImplemented)
MANAGED_RUNTIME_EXPORT(IDynamicCastableGetInterfaceImplementation)
#ifdef FEATURE_OBJCMARSHAL
MANAGED_RUNTIME_EXPORT(ObjectiveCMarshalTryGetTaggedMemory)
MANAGED_RUNTIME_EXPORT(ObjectiveCMarshalGetIsTrackedReferenceCallback)
MANAGED_RUNTIME_EXPORT(ObjectiveCMarshalGetOnEnteredFinalizerQueueCallback)
MANAGED_RUNTIME_EXPORT(ObjectiveCMarshalGetUnhandledExceptionPropagationHandler)
#endif

typedef void (CDECL *pfn)();

static const pfn c_classlibFunctions[] = {
    &MANAGED_RUNTIME_EXPORT_NAME(GetRuntimeException),
    &MANAGED_RUNTIME_EXPORT_NAME(RuntimeFailFast),
    nullptr, // &UnhandledExceptionHandler,
    &MANAGED_RUNTIME_EXPORT_NAME(AppendExceptionStackFrame),
    nullptr, // &CheckStaticClassConstruction,
    &MANAGED_RUNTIME_EXPORT_NAME(GetSystemArrayEEType),
    &MANAGED_RUNTIME_EXPORT_NAME(OnFirstChanceException),
    &MANAGED_RUNTIME_EXPORT_NAME(OnUnhandledException),
    &MANAGED_RUNTIME_EXPORT_NAME(IDynamicCastableIsInterfaceImplemented),
    &MANAGED_RUNTIME_EXPORT_NAME(IDynamicCastableGetInterfaceImplementation),
#ifdef FEATURE_OBJCMARSHAL
    &MANAGED_RUNTIME_EXPORT_NAME(ObjectiveCMarshalTryGetTaggedMemory),
    &MANAGED_RUNTIME_EXPORT_NAME(ObjectiveCMarshalGetIsTrackedReferenceCallback),
    &MANAGED_RUNTIME_EXPORT_NAME(ObjectiveCMarshalGetOnEnteredFinalizerQueueCallback),
    &MANAGED_RUNTIME_EXPORT_NAME(ObjectiveCMarshalGetUnhandledExceptionPropagationHandler),
#else
    nullptr,
    nullptr,
    nullptr,
    nullptr,
#endif
};

#ifdef TARGET_WINDOWS
#define DLL_IMPORT extern "C" __declspec(dllimport)
#else
#define DLL_EXPORT extern "C"
#endif

#ifdef NATIVEAOT_CONSUME_OUTPLACE
DLL_IMPORT int InitializeRuntimeBase();
#endif

static int InitializeRuntime()
{
#ifdef NATIVEAOT_CONSUME_OUTPLACE
    if (InitializeRuntimeBase())
        return -1;
#else
    if (!RhInitialize(
#ifdef NATIVEAOT_DLL
        /* isDll */ true
#else
        /* isDll */ false
#endif
        ))
        return -1;
#endif

    void * osModule = PalGetModuleHandleFromPointer((void*)&NATIVEAOT_ENTRYPOINT);

    // TODO: pass struct with parameters instead of the large signature of RhRegisterOSModule
    if (!RhRegisterOSModule(
        osModule,
        (void*)&__managedcode_a, (uint32_t)((char *)&__managedcode_z - (char*)&__managedcode_a),
        (void*)&__unbox_a, (uint32_t)((char *)&__unbox_z - (char*)&__unbox_a),
        (void **)&c_classlibFunctions, _countof(c_classlibFunctions)))
    {
        return -1;
    }

    InitializeModules(osModule, __modules_a, (int)((__modules_z - __modules_a)), (void **)&c_classlibFunctions, _countof(c_classlibFunctions));

#ifdef NATIVEAOT_DLL
    // Run startup method immediately for a native library
    __managed__Startup();
#endif // NATIVEAOT_DLL

    return 0;
}

#ifndef NATIVEAOT_DLL

#if defined(_WIN32)
int CDECL wmain(int argc, wchar_t* argv[])
#else
int main(int argc, char* argv[])
#endif
{
    int initval = InitializeRuntime();
    if (initval != 0)
        return initval;

    return __managed__Main(argc, argv);
}

#ifdef HAS_ADDRESS_SANITIZER
// We need to build the bootstrapper as a single object file, to ensure
// the linker can detect that we have ASAN components early enough in the build.
// Include our asan support sources for executable projects here to ensure they
// are compiled into the bootstrapper object.
#include "minipal/sansupport.c"
#endif // HAS_ADDRESS_SANITIZER

#endif // !NATIVEAOT_DLL

#ifdef NATIVEAOT_DLL
static struct InitializeRuntimePointerHelper
{
    InitializeRuntimePointerHelper()
    {
        RhSetRuntimeInitializationCallback(&InitializeRuntime);
    }
} initializeRuntimePointerHelper;
#endif // NATIVEAOT_DLL
