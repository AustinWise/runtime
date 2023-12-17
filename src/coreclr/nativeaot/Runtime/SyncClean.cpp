// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "SyncClean.hpp"

#include "CachedInterfaceDispatch.h"

void SyncClean::Terminate()
{
    CleanUp();
}

void SyncClean::CleanUp ()
{
#ifdef FEATURE_CACHED_INTERFACE_DISPATCH
    // Update any interface dispatch caches that were unsafe to modify outside of this GC.
    ReclaimUnusedInterfaceDispatchCaches();
#endif
}
