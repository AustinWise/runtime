// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class @procfs
    {
        /// <summary>
        /// Attempts to get status info for the specified process ID.
        /// </summary>
        /// <param name="pid">PID of the process to read status info for.</param>
        /// <param name="processStatus">The pointer to processStatus instance.</param>
        /// <param name="nameBuf">Buffer in which to place the process name.</param>
        /// <param name="nameBufSize">Size of <paramref name="nameBuf"/> in bytes.</param>
        /// <returns>
        /// true if the process status was read; otherwise, false.
        /// </returns>
        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_ReadProcessStatusInfo", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static unsafe partial bool TryReadProcessStatusInfo(int pid, ProcessStatusInfo* processStatus, byte* nameBuf, int nameBufSize);

        internal struct ProcessStatusInfo
        {
            internal int Pid;
            internal int ParentPid;
            internal int SessionId;
            internal nuint ResidentSetSize;
            internal Interop.Sys.TimeSpec StartTime;
            // add more fields when needed.
        }

        internal static unsafe bool TryReadSelfProcessStatusInfo(out ProcessStatusInfo statusInfo, [NotNullWhen(true)] out string? processName)
        {
            // PID 0 is special cased to mean "self".
            return TryReadProcessStatusInfo(0, out statusInfo, out processName);
        }

        internal static unsafe bool TryReadProcessStatusInfo(int pid, out ProcessStatusInfo statusInfo, [NotNullWhen(true)] out string? processName)
        {
            statusInfo = default;
            processName = null;
            Span<byte> buf = stackalloc byte[16];
            fixed (ProcessStatusInfo* pStatusInfo = &statusInfo)
            fixed (byte* pBuf = buf)
            {
                if (TryReadProcessStatusInfo(pid, pStatusInfo, pBuf, buf.Length))
                {
                    int terminator = buf.IndexOf((byte)0);
                    processName = Marshal.PtrToStringUTF8((IntPtr)pBuf, (terminator >= 0) ? terminator : buf.Length);
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
    }
}
