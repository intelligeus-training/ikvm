/*
  Copyright (C) 2013 Jeroen Frijters

  This software is provided 'as-is', without any express or implied
  warranty.  In no event will the authors be held liable for any damages
  arising from the use of this software.

  Permission is granted to anyone to use this software for any purpose,
  including commercial applications, and to alter it and redistribute it
  freely, subject to the following restrictions:

  1. The origin of this software must not be misrepresented; you must not
     claim that you wrote the original software. If you use this software
     in a product, an acknowledgment in the product documentation would be
     appreciated but is not required.
  2. Altered source versions must be plainly marked as such, and must not be
     misrepresented as being the original software.
  3. This notice may not be removed or altered from any source distribution.

  Jeroen Frijters
  jeroen@frijters.net
  
*/
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using IKVM.Reflection;
using IKVM.Reflection.Emit;
using Type = IKVM.Reflection.Type;

static class ImpLib
{
    static readonly Regex Definition = new Regex(@"^\s*(.+)=\[([^\]]+)\](.+)::([^\s]+)\s+@(\d+)$");
    static readonly Universe Universe = new Universe();

    static int Main(string[] args)
    {
        var options = new Options();
        var exports = new List<Export>();
        if (!ParseArgs(args, options) || !ParseDefFile(options.DefinitionFile, exports))
        {
            return 1;
        }
        var assemblyName = new AssemblyName(Path.GetFileNameWithoutExtension(options.OutputFile));
        assemblyName.Version = options.Version;
        assemblyName.KeyPair = options.KeyPair;
        var assemblyBuilder = Universe.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Save);
        var moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name, options.OutputFile);
        foreach (var export in exports)
        {
            ExportMethod(moduleBuilder, export);
        }
        moduleBuilder.CreateGlobalFunctions();
        if (options.Win32Res != null)
        {
            assemblyBuilder.DefineUnmanagedResource(options.Win32Res);
        }
        else
        {
            if (options.Description != null)
            {
                assemblyBuilder.SetCustomAttribute(new CustomAttributeBuilder(Universe.Import(typeof(System.Reflection.AssemblyTitleAttribute)).GetConstructor(new Type[] { Universe.Import(typeof(string)) }), new object[] { options.Description }));
            }
            assemblyBuilder.DefineVersionInfoResource(options.Product, options.Version.ToString(), options.Company, options.Copyright, null);
        }
        assemblyBuilder.Save(options.OutputFile, options.PortableExecutableKind, options.Machine);
        return 0;
    }

    static bool ParseArgs(string[] args, Options options)
    {
        options.PortableExecutableKind = PortableExecutableKinds.Required32Bit;
        options.Machine = ImageFileMachine.I386;
        foreach (var arg in args)
        {
            if (arg.StartsWith("-r:", StringComparison.Ordinal) || arg.StartsWith("-reference:", StringComparison.Ordinal))
            {
                Universe.LoadFile(arg.Substring(arg.IndexOf(':') + 1));
            }
            else if (arg.StartsWith("-out:", StringComparison.Ordinal))
            {
                options.OutputFile = arg.Substring(5);
            }
            else switch (arg)
            {
                case "-platform:x86":
                    options.PortableExecutableKind = PortableExecutableKinds.Required32Bit;
                    options.Machine = ImageFileMachine.I386;
                    break;
                case "-platform:x64":
                    options.PortableExecutableKind = PortableExecutableKinds.PE32Plus;
                    options.Machine = ImageFileMachine.AMD64;
                    break;
                case "-platform:arm":
                    options.PortableExecutableKind = PortableExecutableKinds.Unmanaged32Bit;
                    options.Machine = ImageFileMachine.ARM;
                    break;
                default:
                {
                    if (arg.StartsWith("-win32res:", StringComparison.Ordinal))
                    {
                        options.Win32Res = arg.Substring(10);
                    }
                    else if (arg.StartsWith("-key:", StringComparison.Ordinal))
                    {
                        options.KeyPair = new StrongNameKeyPair(arg.Substring(5));
                    }
                    else if (arg.StartsWith("-keyfile:", StringComparison.Ordinal))
                    {
                        using (FileStream fs = File.OpenRead(arg.Substring(9)))
                        {
                            options.KeyPair = new StrongNameKeyPair(fs);
                        }
                    }
                    else if (arg.StartsWith("-version:", StringComparison.Ordinal))
                    {
                        options.Version = new Version(arg.Substring(9));
                    }
                    else if (arg.StartsWith("-product:", StringComparison.Ordinal))
                    {
                        options.Product = arg.Substring(9);
                    }
                    else if (arg.StartsWith("-company:", StringComparison.Ordinal))
                    {
                        options.Company = arg.Substring(9);
                    }
                    else if (arg.StartsWith("-copyright:", StringComparison.Ordinal))
                    {
                        options.Copyright = arg.Substring(11);
                    }
                    else if (arg.StartsWith("-description:", StringComparison.Ordinal))
                    {
                        options.Description = arg.Substring(13);
                    }
                    else if (options.DefinitionFile == null)
                    {
                        options.DefinitionFile = arg;
                    }
                    else
                    {
                        Console.WriteLine($"Unknown option: {arg}");
                        return false;
                    }

                    break;
                }
            }
        }

        if (options.DefinitionFile == null || options.OutputFile == null)
        {
            Console.WriteLine("Usage: implib <exports.def> -out:<outputAssembly.dll> -r:<inputAssembly.dll> [-platform:<x86|x64|arm>] [-win32res:<file>] [-key:<keycontainer>] [-version:<M.m.b.r>]");
            return false;
        }

        return true;
    }

    static bool ParseDefFile(string fileName, List<Export> exports)
    {
        using var streamReader = new StreamReader(fileName);
        string line;
        while ((line = streamReader.ReadLine()) != null)
        {
            var match = Definition.Match(line);
            if (match.Groups.Count == 6)
            {
                Export exp;
                exp.Name = match.Groups[1].Value;
                exp.Ordinal = Int32.Parse(match.Groups[5].Value);
                exp.MethodInfo = GetMethod(match.Groups[2].Value, match.Groups[3].Value, match.Groups[4].Value);
                if (exp.MethodInfo == null)
                {
                    Console.WriteLine($"Unable to find {exp.Name}");
                    return false;
                }
                exports.Add(exp);
            }
        }

        return true;
    }

    static MethodInfo GetMethod(string assemblyName, string typeName, string method)
    {
        foreach (var assembly in Universe.GetAssemblies())
        {
            if (assembly.GetName().Name.Equals(assemblyName, StringComparison.OrdinalIgnoreCase))
            {
                var type = assembly.GetType(typeName);
                if (type != null)
                {
                    return type.GetMethod(method, BindingFlags.Public | BindingFlags.Static);
                }
            }
        }
        return null;
    }

    static void ExportMethod(ModuleBuilder modb, Export exp)
    {
        var parameters = exp.MethodInfo.GetParameters();
        var types = new Type[parameters.Length];
        for (var i = 0; i < types.Length; i++)
        {
            types[i] = parameters[i].ParameterType;
        }
        var methodBuilder = modb.DefineGlobalMethod(exp.Name, 
                                                MethodAttributes.Public | MethodAttributes.Static, 
                                                exp.MethodInfo.ReturnType, 
                                                types);
        var ilGenerator = methodBuilder.GetILGenerator();
        for (var i = 0; i < types.Length; i++)
        {
            ilGenerator.Emit(OpCodes.Ldarg_S, (byte)i);
        }
        ilGenerator.Emit(OpCodes.Call, exp.MethodInfo);
        ilGenerator.Emit(OpCodes.Ret);
        methodBuilder.__AddUnmanagedExport(methodBuilder.Name, exp.Ordinal);
    }

    sealed class Options
    {
        internal PortableExecutableKinds PortableExecutableKind;
        internal ImageFileMachine Machine;
        internal string DefinitionFile;
        internal string OutputFile;
        internal string Win32Res;
        internal StrongNameKeyPair KeyPair;
        internal Version Version;
        internal string Product;
        internal string Company;
        internal string Copyright;
        internal string Description;
    }

    struct Export
    {
        internal string Name;
        internal int Ordinal;
        internal MethodInfo MethodInfo;
    }
}
