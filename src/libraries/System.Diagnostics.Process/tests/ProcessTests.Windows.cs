// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.IO;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.Win32.SafeHandles;
using Xunit;

namespace System.Diagnostics.Tests
{
    public partial class ProcessTests
    {
        private string WriteScriptFile(string directory, string name, int returnValue)
        {
            string filename = Path.Combine(directory, name);
            filename += ".bat";
            File.WriteAllText(filename, $"exit {returnValue}");
            return filename;
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(true, false)]
        [InlineData(false, false)]
        [InlineData(true, true)]
        [InlineData(false, true)]
        public void ProcessStart_InheritHandles_InfluencesTheInheritanceOfHandles(bool inheritHandles, bool redirectStreams)
        {
            using (var tmpFile = File.Open(GetTestFilePath(), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite | FileShare.Inheritable))
            {
                tmpFile.WriteByte(42);
                tmpFile.Flush();
                tmpFile.Position = 0;

                var options = new RemoteInvokeOptions();
                options.StartInfo.InheritHandles = inheritHandles;
                if (redirectStreams)
                {
                    options.StartInfo.RedirectStandardInput = true;
                    options.StartInfo.RedirectStandardOutput = true;
                    options.StartInfo.RedirectStandardError = true;
                }
                string handleStr = tmpFile.SafeFileHandle.DangerousGetHandle().ToString(CultureInfo.InvariantCulture);

                using (RemoteInvokeHandle handle = RemoteExecutor.Invoke(static (string handleStr, string inheritHandlesStr, string redirectStreamsStr) =>
                {
                    nint handle = nint.Parse(handleStr);
                    bool inheritHandles = bool.Parse(inheritHandlesStr);
                    bool redirectStreams = bool.Parse(redirectStreamsStr);
                    var fileHandle = new SafeFileHandle(handle, ownsHandle: true);

                    if (redirectStreams)
                    {
                        Assert.Equal("input", Console.ReadLine());
                        Console.WriteLine("output");
                        Console.Error.WriteLine("error");
                    }

                    // To avoid asserts when trying to read from an invalid handle,
                    // ensure that what we have is at least a normal file.
                    int fileType = Interop.Kernel32.GetFileType(fileHandle);
                    if (inheritHandles)
                    {
                        Assert.Equal(Interop.Kernel32.FileTypes.FILE_TYPE_DISK, fileType);
                    }
                    else if (fileType != Interop.Kernel32.FileTypes.FILE_TYPE_DISK)
                    {
                        return RemoteExecutor.SuccessExitCode;
                    }

                    byte[] buf = new byte[100];
                    long bytesRead;
                    try
                    {
                        bytesRead = RandomAccess.Read(fileHandle, buf.AsSpan(), 0);
                    }
                    catch (Exception) when (!inheritHandles)
                    {
                        // It it hard to predict what could go wrong when we are reading from a random
                        // file handle.
                        return RemoteExecutor.SuccessExitCode;
                    }

                    if (inheritHandles)
                    {
                        Assert.Equal(1, bytesRead);
                        Assert.Equal(42, buf[0]);
                    }
                    else
                    {
                        // Hypothetically there could be a handle with the same value in this process
                        // as in the parent process. Make sure that we are looking at a different file.
                        Assert.False(bytesRead == 1 && buf[0] == 42);
                    }

                    return RemoteExecutor.SuccessExitCode;
                }, handleStr, inheritHandles.ToString(), redirectStreams.ToString(), options))
                {
                    if (redirectStreams)
                    {
                        handle.Process.StandardInput.WriteLine("input");
                        Assert.Equal("output", handle.Process.StandardOutput.ReadLine());
                        Assert.Equal("error", handle.Process.StandardError.ReadLine());
                    }
                }
            }
        }
    }
}
