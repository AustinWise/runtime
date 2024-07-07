// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class @procfs
    {
        internal const string RootPath = "/proc/";
        // As of July 2024, this file only exists on systems that have LX support.
        private const string CmdLineFileName = "/cmdline";
        internal static string GetCmdLinePathForProcess(int pid) => string.Create(null, stackalloc char[256], $"{RootPath}{(uint)pid}{CmdLineFileName}");
    }
}
