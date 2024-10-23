// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;

using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

namespace ILCompiler
{
    public class ExportsFileWriter
    {
        private readonly string _exportsFile;
        private readonly string[] _exportSymbols;
        private readonly List<EcmaMethod> _methods;
        private readonly TypeSystemContext _context;
        private readonly bool _exportSystemModuleFunction;

        public ExportsFileWriter(TypeSystemContext context, string exportsFile, string[] exportSymbols, bool exportSystemModuleFunction)
        {
            _exportsFile = exportsFile;
            _exportSymbols = exportSymbols;
            _context = context;
            _methods = new List<EcmaMethod>();
            _exportSystemModuleFunction = exportSystemModuleFunction;
        }

        public void AddExportedMethods(IEnumerable<EcmaMethod> methods)
        {
            if (!_exportSystemModuleFunction)
                methods = methods.Where(m => m.Module != _context.SystemModule);
            _methods.AddRange(methods);
        }

        public void EmitExportedMethods()
        {
            FileStream fileStream = new FileStream(_exportsFile, FileMode.Create);
            using (StreamWriter streamWriter = new StreamWriter(fileStream))
            {
                if (_context.Target.IsWindows)
                {
                    streamWriter.WriteLine("EXPORTS");
                    foreach (string symbol in _exportSymbols)
                        streamWriter.WriteLine($"   {symbol.Replace(',', ' ')}");
                    foreach (var method in _methods)
                    {
                        streamWriter.WriteLine($"   {GetExportedName(method)}");
                    }
                }
                else if(_context.Target.IsApplePlatform)
                {
                    foreach (string symbol in _exportSymbols)
                        streamWriter.WriteLine($"_{symbol}");
                    foreach (var method in _methods)
                        streamWriter.WriteLine($"_{GetExportedName(method)}");
                }
                else
                {
                    streamWriter.WriteLine("V1.0 {");
                    if (_exportSymbols.Length != 0 || _methods.Count != 0)
                    {
                        streamWriter.WriteLine("    global:");
                        foreach (string symbol in _exportSymbols)
                            streamWriter.WriteLine($"        {symbol};");
                        foreach (var method in _methods)
                            streamWriter.WriteLine($"        {GetExportedName(method)};");
                    }
                    streamWriter.WriteLine("    local: *;");
                    streamWriter.WriteLine("};");
                }
            }
        }

        private static string GetExportedName(EcmaMethod method)
        {
            if (method.IsUnmanagedCallersOnly)
            {
                return method.GetUnmanagedCallersOnlyExportName();
            }
            else
            {
                return method.GetRuntimeExportName();
            }
        }
    }
}
