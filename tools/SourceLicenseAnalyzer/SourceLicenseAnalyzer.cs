/*
  Copyright (C) 2013-2014 Jeroen Frijters

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
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace SourceLicenseAnalyzer
{
	class Years
	{
		internal static Years Dummy = new Years();
		internal int Min = int.MaxValue;
		internal int Max = int.MinValue;
		internal string Name;
	}

	class Program
	{
		static Dictionary<string, string> Aliases = new Dictionary<string, string>();
		static Dictionary<string, Years> Copyrights = new Dictionary<string, Years>();
		static int errorCount;

		static void Def(string name, params string[] aliasesList)
		{
			Years y = new Years();
			y.Name = name;
			Copyrights.Add(name, y);
			Aliases.Add(name, name);
			foreach (string s in aliasesList)
			{
				Aliases.Add(s, name);
			}
		}

		static int Main(string[] args)
		{
			Def("Free Software Foundation", "Free Software   Foundation", "Free Software    Foundation", "Free Software Fonudation, Inc.");
			Def("Sun Microsystems, Inc.", "Sun Microsystems Inc");
			Def("Jeroen Frijters");
			Def("Thai Open Source Software Center Ltd");
			Def("World Wide Web Consortium");
			Def("International Business Machines, Inc.", "IBM Corp.", "IBM Corporation", "International Business Machines", "International Business Machines Corporation");
			Def("Wily Technology, Inc.");
			Def("Unicode, Inc.");
			Def("Colin Plumb");
			Def("Taligent, Inc.");
			Def("Red Hat, Inc.");
			Def("The Open Group Research Institute");
			Def("FundsXpress, INC.");
			Def("AT&T");
			Def("The Apache Software Foundation");
			Def("freebxml.org");
			Def("The Cryptix Foundation Limited");
			Def("Visual Numerics Inc.");
			Def("INRIA, France Telecom");
			Def("Oracle and/or its affiliates", "Oracle Corporation");
			Def("i-net software");
			Def("Google Inc.");
			Def("Stephen Colebourne & Michael Nascimento Santos");
			Def("Attila Szegedi");
			Def("Daniel Wilson");
			Def("http://stackoverflow.com/users/12048/finnw");


			// these are false positives
			Copyrights.Add("dummy", Years.Dummy);
			Aliases.Add("icSigCopyrightTag", "dummy");
			Aliases.Add("Copyright notice to stick into built-in-profile files.", "dummy");
			Aliases.Add("AssemblyCopyrightAttribute", "dummy");
			Aliases.Add("getVersionAndCopyrightInfo()", "dummy");
			Aliases.Add("Copyright by IBM and others and distributed under the * distributed under MIT/X", "dummy");
			Aliases.Add("*  Copyright Office. *", "dummy");
			Aliases.Add("* Copyright office. *", "dummy");
			Aliases.Add("Your Corporation", "dummy");
			Aliases.Add("but I wrote that code so I co-own the copyright", "dummy");
			Aliases.Add("identifying information: \"Portions Copyrighted [year] * [name of copyright owner]\"", "dummy");
			Aliases.Add("* \"Portions Copyright [year] [name of copyright owner]\" *", "dummy");

			using (StreamReader rdr = new StreamReader("allsources.gen.lst"))
			{
				string file;
				while ((file = rdr.ReadLine()) != null)
				{
					if (file != "AssemblyInfo.java")
					{
						ProcessFile(file);
					}
				}
			}

			var years = new Years[Copyrights.Count];
			Copyrights.Values.CopyTo(years, 0);

			Array.Sort(years, (x, y) => x.Name?.CompareTo(y.Name) ?? 0);

			var first = true;
			foreach (var year in years)
			{
				if (year != Years.Dummy)
				{
					if (!first)
					{
						Console.WriteLine("\\r\\n\" +");
					}
					first = false;
					Console.Write("    \"");
					if (year.Min != year.Max)
					{
						Console.Write($"{{year.Min}}-{year.Max}  {{{year.Name}}}");
					}
					else
					{
						Console.Write($"{year.Min}       {year.Name}");
					}
				}
			}
			Console.WriteLine("\"");

			return errorCount;
		}

		static void ProcessFile(string filePath)
		{
			var gpl = false;
			var classpathException = false;
			if (!File.Exists(filePath) && File.Exists(filePath + ".in"))
			{
				filePath += ".in";
			}

			using var rdr = new StreamReader(filePath);
			string line;
			string nextline = null;
			while ((line = rdr.ReadLine()) != null)
			{
				gpl |= line.Contains("GNU General Public License");
				classpathException |= line.Contains("subject to the \"Classpath\" exception") || line.Contains("permission to link this library with independent modules");
				while (line != null && line.IndexOf("Copyright") != -1)
				{
					Years y = null;
					foreach (KeyValuePair<string, string> kv in Aliases)
					{
						if (line.IndexOf(kv.Key) != -1)
						{
							y = Copyrights[kv.Value];
							break;
						}
					}
					if (y == null)
					{
						if (nextline == null)
						{
							nextline = rdr.ReadLine();
							if (nextline.IndexOf("Copyright") == -1)
							{
								line += nextline;
								continue;
							}
						}
						if (filePath.Contains("/jaxws/src/share/jaxws_classes/com/sun/xml/internal/rngom/")
						    && (line.Contains("* Copyright (C) 2004-2011 *") || line.Contains("* Copyright (C) 2004-2012 *")))
						{
							// HACK ignore bogus copyright line
						}
						else
						{
							Error(filePath + ":" + Environment.NewLine + line);
						}
					}
					else
					{
						foreach (Match m in Regex.Matches(line, "[^0-9]((19|20)[0-9][0-9]+)"))
						{
							if (m.Groups[1].Value.Length == 4)
							{
								var v = int.Parse(m.Groups[1].Value);
								y.Min = Math.Min(y.Min, v);
								y.Max = Math.Max(y.Max, v);
							}
						}
					}
					line = nextline;
					nextline = null;
				}
			}

			if (gpl && !classpathException)
			{
				Error("GPL without Classpath exception: {0}", filePath);
			}
		}

		static void Error(string message, params object[] args)
		{
			errorCount++;
			Console.Error.WriteLine(message, args);
		}
	}
}
