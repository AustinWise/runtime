// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Runtime.Versioning;
using System.Text;
using System.Threading;

// TODO: remove
#pragma warning disable CA1822

namespace System.Diagnostics
{
    public partial class Process : IDisposable
    {
        /// <summary>
        /// Creates an array of <see cref="Process"/> components that are associated with process resources on a
        /// remote computer. These process resources share the specified process name.
        /// </summary>
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [SupportedOSPlatform("maccatalyst")]
        public static Process[] GetProcessesByName(string? processName, string machineName)
        {
            ProcessManager.ThrowIfRemoteMachine(machineName);

            processName ??= "";

            ArrayBuilder<Process> processes = default;
            foreach (int pid in ProcessManager.EnumerateProcessIds())
            {
                if (Interop.procfs.TryReadProcessStatusInfo(pid, out Interop.procfs.ProcessStatusInfo status, out string? shortProcessName))
                {
                    string actualProcessName = GetUntruncatedProcessName(pid, shortProcessName);
                    if ((processName == "" || string.Equals(processName, actualProcessName, StringComparison.OrdinalIgnoreCase)))
                    {
                        ProcessInfo processInfo = ProcessManager.CreateProcessInfo(ref status, shortProcessName, actualProcessName);
                        processes.Add(new Process(machineName, isRemoteMachine: false, pid, processInfo));
                    }
                }
            }

            return processes.ToArray();
        }

        /// <summary>Gets the amount of time the process has spent running code inside the operating system core.</summary>
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [SupportedOSPlatform("maccatalyst")]
        public TimeSpan PrivilegedProcessorTime
        {
            get
            {
                throw new PlatformNotSupportedException();
            }
        }

        /// <summary>Gets the time the associated process was started.</summary>
        internal DateTime StartTimeCore
        {
            get
            {
                Interop.procfs.ProcessStatusInfo status = GetStatus();
                return DateTime.UnixEpoch.AddSeconds(status.StartTime.TvSec).AddTicks(status.StartTime.TvNsec / 100);
            }
        }

        /// <summary>Gets the parent process ID</summary>
        private int ParentProcessId => GetStatus().ParentPid;

        /// <summary>Gets execution path</summary>
        private static string? GetPathToOpenFile()
        {
            return FindProgramInPath("xdg-open");
        }

        /// <summary>
        /// Gets the amount of time the associated process has spent utilizing the CPU.
        /// It is the sum of the <see cref='System.Diagnostics.Process.UserProcessorTime'/> and
        /// <see cref='System.Diagnostics.Process.PrivilegedProcessorTime'/>.
        /// </summary>
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [SupportedOSPlatform("maccatalyst")]
        public TimeSpan TotalProcessorTime
        {
            get
            {
                throw new PlatformNotSupportedException();
            }
        }

        /// <summary>
        /// Gets the amount of time the associated process has spent running code
        /// inside the application portion of the process (not the operating system core).
        /// </summary>
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [SupportedOSPlatform("maccatalyst")]
        public TimeSpan UserProcessorTime
        {
            get
            {
                throw new PlatformNotSupportedException();
            }
        }

        partial void EnsureHandleCountPopulated()
        {
            // TODO: remove this method if not needed
        }

        /// <summary>
        /// Gets or sets which processors the threads in this process can be scheduled to run on.
        /// </summary>
        private IntPtr ProcessorAffinityCore
        {
            get
            {
                throw new PlatformNotSupportedException();
            }
            set
            {
                throw new PlatformNotSupportedException();
            }
        }

        /// <summary>
        /// Make sure we have obtained the min and max working set limits.
        /// </summary>
        private void GetWorkingSetLimits(out IntPtr minWorkingSet, out IntPtr maxWorkingSet)
        {
            throw new PlatformNotSupportedException();
        }

        /// <summary>Sets one or both of the minimum and maximum working set limits.</summary>
        /// <param name="newMin">The new minimum working set limit, or null not to change it.</param>
        /// <param name="newMax">The new maximum working set limit, or null not to change it.</param>
        /// <param name="resultingMin">The resulting minimum working set limit after any changes applied.</param>
        /// <param name="resultingMax">The resulting maximum working set limit after any changes applied.</param>
        private static void SetWorkingSetLimitsCore(IntPtr? newMin, IntPtr? newMax, out IntPtr resultingMin, out IntPtr resultingMax)
        {
            throw new PlatformNotSupportedException();
        }

        // ----------------------------------
        // ---- Unix PAL layer ends here ----
        // ----------------------------------

        /// <summary>Gets the name that was used to start the process, or null if it could not be retrieved.</summary>
        /// <param name="pid">The pid of the target process.</param>
        /// <param name="processNameStart">The start of the process name of the ProcessStatusInfo struct.</param>
        internal static string GetUntruncatedProcessName(int pid, string processNameStart)
        {
            string cmdLineFilePath = Interop.procfs.GetCmdLinePathForProcess(pid);

            byte[]? rentedArray = null;
            try
            {
                // bufferSize == 1 used to avoid unnecessary buffer in FileStream
                using (var fs = new FileStream(cmdLineFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 1, useAsync: false))
                {
                    Span<byte> buffer = stackalloc byte[512];
                    int bytesRead = 0;
                    while (true)
                    {
                        // Resize buffer if it was too small.
                        if (bytesRead == buffer.Length)
                        {
                            uint newLength = (uint)buffer.Length * 2;

                            byte[] tmp = ArrayPool<byte>.Shared.Rent((int)newLength);
                            buffer.CopyTo(tmp);
                            byte[]? toReturn = rentedArray;
                            buffer = rentedArray = tmp;
                            if (toReturn != null)
                            {
                                ArrayPool<byte>.Shared.Return(toReturn);
                            }
                        }

                        Debug.Assert(bytesRead < buffer.Length);
                        int n = fs.Read(buffer.Slice(bytesRead));
                        bytesRead += n;

                        // cmdline contains the argv array separated by '\0' bytes.
                        // processNameStart contains a possibly truncated version of the process name.
                        // When the program is a native executable, the process name will be in argv[0].
                        // When the program is a script, argv[0] contains the interpreter, and argv[1] contains the script name.
                        Span<byte> argRemainder = buffer.Slice(0, bytesRead);
                        int argEnd = argRemainder.IndexOf((byte)'\0');
                        if (argEnd != -1)
                        {
                            // Check if argv[0] has the process name.
                            string? name = GetUntruncatedNameFromArg(argRemainder.Slice(0, argEnd), prefix: processNameStart);
                            if (name != null)
                            {
                                return name;
                            }

                            // Check if argv[1] has the process name.
                            argRemainder = argRemainder.Slice(argEnd + 1);
                            argEnd = argRemainder.IndexOf((byte)'\0');
                            if (argEnd != -1)
                            {
                                name = GetUntruncatedNameFromArg(argRemainder.Slice(0, argEnd), prefix: processNameStart);
                                return name ?? processNameStart;
                            }
                        }

                        if (n == 0)
                        {
                            return processNameStart;
                        }
                    }
                }
            }
            catch (IOException)
            {
                return processNameStart;
            }
            finally
            {
                if (rentedArray != null)
                {
                    ArrayPool<byte>.Shared.Return(rentedArray);
                }
            }

            static string? GetUntruncatedNameFromArg(Span<byte> arg, string prefix)
            {
                // Strip directory names from arg.
                int nameStart = arg.LastIndexOf((byte)'/') + 1;
                string argString = Encoding.UTF8.GetString(arg.Slice(nameStart));

                if (argString.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    return argString;
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>Reads the stats information for this process from the procfs file system.</summary>
        private Interop.procfs.ProcessStatusInfo GetStatus()
        {
            EnsureState(State.HaveNonExitedId);
            Interop.procfs.ProcessStatusInfo status;
            if (Interop.procfs.TryReadProcessStatusInfo(_processId, out status, out string? _))
            {
                throw new Win32Exception(SR.ProcessInformationUnavailable);
            }
            return status;
        }
    }
}
