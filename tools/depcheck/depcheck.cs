/*
  Copyright (C) 2009 Jeroen Frijters

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
using System.Reflection;

namespace depcheck
{
	class DependencyChecker
	{
		static int Main(string[] args)
		{
			var dependencies = new Dictionary<string, List<string>>();
			var dependency = string.Empty;
			foreach (var line in File.ReadAllLines(args[1]))
			{
				if (line.Trim().Length == 0 || line.StartsWith("#"))
				{
					// comment
				}
				else if (line.StartsWith("->"))
				{
					dependencies[dependency].Add(line.Substring(2));
				}
				else
				{
					dependency = line;
					dependencies.Add(dependency, new List<string>());
				}
			}
			var whitelist = new List<string>(new string[] { "mscorlib", "System", "IKVM.Runtime", "IKVM.OpenJDK.Core" });
			var fail = false;
			foreach (var line in File.ReadAllLines(args[0]))
			{
				if (line.Contains("-out:"))
				{
					var file = line.Trim().Substring(5);
					var assembly = Assembly.ReflectionOnlyLoadFrom(Path.Combine(Path.GetDirectoryName(args[0]), file));
					if (!dependencies.ContainsKey(assembly.GetName().Name ?? string.Empty))
					{
						fail = true;
						Console.WriteLine($"Dependencies does not contain {assembly.GetName().Name}");
						foreach (var assemblyName in assembly.GetReferencedAssemblies())
						{
							if (!whitelist.Contains(assemblyName.Name))
							{
								Console.WriteLine("->{0}", assemblyName.Name);
							}
						}
					}
					else
					{
						foreach (var assemblyName in assembly.GetReferencedAssemblies())
						{
							if (!whitelist.Contains(assemblyName.Name))
							{
								if (!dependencies[assembly.GetName().Name].Contains(assemblyName.Name))
								{
									fail = true;
									Console.WriteLine($"Error: Assembly {assembly.GetName().Name} has an undeclared dependency on {assemblyName.Name}");
								}
							}
						}
					}
				}
			}
			return fail ? 1 : 0;
		}
	}
}
