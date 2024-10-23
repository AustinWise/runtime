// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#define NATIVEAOT_DLL

#include "main.h"

#ifdef TARGET_WINDOWS
#define DLL_EXPORT extern "C" __declspec(dllexport)
#else
#define DLL_EXPORT extern "C" __attribute((visibility("default")))
#endif

// Returns non-zero on failure.
DLL_EXPORT int InitializeRuntimeBase()
{
    if (!RhInitialize(
        /* isDll */ true
        ))
        return -1;

    void * osModule = PalGetModuleHandleFromPointer((void*)&NATIVEAOT_ENTRYPOINT);

    // TODO: pass struct with parameters instead of the large signature of RhRegisterOSModule
    if (!RhRegisterOSModule(
        osModule,
        (void*)&__managedcode_a, (uint32_t)((char *)&__managedcode_z - (char*)&__managedcode_a),
        (void*)&__unbox_a, (uint32_t)((char *)&__unbox_z - (char*)&__unbox_a),
        /* classlibFunction */ (void **)nullptr, 0))
    {
        return -1;
    }

    InitializeModules(osModule, __modules_a, (int)((__modules_z - __modules_a)), (void **)nullptr, 0);

    // Run startup method immediately for a native library
    __managed__Startup();

    return 0;
}
