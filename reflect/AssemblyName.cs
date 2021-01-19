/*
  Copyright (C) 2009-2012 Jeroen Frijters

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
using System.Globalization;
#if !NETSTANDARD
using System.Configuration.Assemblies;
#endif
using System.IO;
using System.Security.Cryptography;
using System.Text;
using IKVM.Reflection.Reader;

namespace IKVM.Reflection
{
	public sealed class AssemblyName
#if !NETSTANDARD
		: ICloneable
#endif
	{
		private Version version;
		private byte[] publicKeyToken;
		private byte[] publicKey;
		internal byte[] hash;

		public AssemblyName()
		{
		}

		public AssemblyName(string assemblyName)
		{
			switch (assemblyName)
			{
				case null:
					throw new ArgumentNullException("assemblyName");
				case "":
					throw new ArgumentException();
			}

			ParsedAssemblyName parsed;
			switch (Fusion.ParseAssemblyName(assemblyName, out parsed))
			{
				case ParseAssemblyResult.GenericError:
				case ParseAssemblyResult.DuplicateKey:
					throw new FileLoadException();
				case ParseAssemblyResult.OK:
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
			if (!ParseVersion(parsed.Version, parsed.Retargetable.HasValue, out version))
			{
				throw new FileLoadException();
			}
			Name = parsed.Name;
			if (parsed.Culture != null)
			{
				if (parsed.Culture.Equals("neutral", StringComparison.OrdinalIgnoreCase))
				{
					CultureName = "";
				}
				else if (parsed.Culture == "")
				{
					throw new FileLoadException();
				}
				else
				{
					CultureName = new CultureInfo(parsed.Culture).Name;
				}
			}
			if (parsed.PublicKeyToken != null)
			{
				if (parsed.PublicKeyToken.Equals("null", StringComparison.OrdinalIgnoreCase))
				{
					publicKeyToken = Empty<byte>.Array;
				}
				else if (parsed.PublicKeyToken.Length != 16)
				{
					throw new FileLoadException();
				}
				else
				{
					publicKeyToken = ParseKey(parsed.PublicKeyToken);
				}
			}
			if (parsed.Retargetable.HasValue)
			{
				if (parsed.Culture == null || parsed.PublicKeyToken == null || version == null)
				{
					throw new FileLoadException();
				}
				if (parsed.Retargetable.Value)
				{
					RawFlags |= AssemblyNameFlags.Retargetable;
				}
			}
			ProcessorArchitecture = parsed.ProcessorArchitecture;
			if (parsed.WindowsRuntime)
			{
				ContentType = AssemblyContentType.WindowsRuntime;
			}
		}

		private static byte[] ParseKey(string key)
		{
			if ((key.Length & 1) != 0)
			{
				throw new FileLoadException();
			}
			byte[] buf = new byte[key.Length / 2];
			for (int i = 0; i < buf.Length; i++)
			{
				buf[i] = (byte)(ParseHexDigit(key[i * 2]) * 16 + ParseHexDigit(key[i * 2 + 1]));
			}
			return buf;
		}

		private static int ParseHexDigit(char digit)
		{
			if (digit >= '0' && digit <= '9')
			{
				return digit - '0';
			}
		
			digit |= (char)0x20;
			if (digit >= 'a' && digit <= 'f')
			{
				return 10 + digit - 'a';
			}

			throw new FileLoadException();
			
		}

		public override string ToString()
		{
			return FullName;
		}

		public string Name { get; set; }

		public CultureInfo CultureInfo
		{
			get => CultureName == null ? null : new CultureInfo(CultureName);
			set => CultureName = value?.Name;
		}

		public string CultureName { get; set; }

		public Version Version
		{
			get => version;
			set => version = value;
		}

		public StrongNameKeyPair KeyPair { get; set; }

		public string CodeBase { get; set; }

#if !NETSTANDARD
		public string EscapedCodeBase
		{
			get
			{
				// HACK use the real AssemblyName to escape the codebase
				System.Reflection.AssemblyName tmp = new System.Reflection.AssemblyName();
				tmp.CodeBase = CodeBase;
				return tmp.EscapedCodeBase;
			}
		}
#endif

		public ProcessorArchitecture ProcessorArchitecture
		{
			get => (ProcessorArchitecture)(((int)RawFlags & 0x70) >> 4);
			set
			{
				if (value >= ProcessorArchitecture.None && value <= ProcessorArchitecture.Arm)
				{
					RawFlags = (RawFlags & ~(AssemblyNameFlags)0x70) | (AssemblyNameFlags)((int)value << 4);
				}
			}
		}

		public AssemblyNameFlags Flags
		{
			get => RawFlags & (AssemblyNameFlags)~0xEF0;
			set => RawFlags = (RawFlags & (AssemblyNameFlags)0xEF0) | (value & (AssemblyNameFlags)~0xEF0);
		}

		public AssemblyVersionCompatibility VersionCompatibility { get; set; } = AssemblyVersionCompatibility.SameMachine;

		public AssemblyContentType ContentType
		{
			get => (AssemblyContentType)(((int)RawFlags & 0xE00) >> 9);
			set
			{
				if (value >= AssemblyContentType.Default && value <= AssemblyContentType.WindowsRuntime)
				{
					RawFlags = (RawFlags & ~(AssemblyNameFlags)0xE00) | (AssemblyNameFlags)((int)value << 9);
				}
			}
		}

		public byte[] GetPublicKey()
		{
			return publicKey;
		}

		public void SetPublicKey(byte[] publicKey)
		{
			this.publicKey = publicKey;
			RawFlags = (RawFlags & ~AssemblyNameFlags.PublicKey) | (publicKey == null ? 0 : AssemblyNameFlags.PublicKey);
		}

		public byte[] GetPublicKeyToken()
		{
			if (publicKeyToken == null && publicKey != null)
			{
				// note that GetPublicKeyToken() has a side effect in this case, because we retain this token even after the public key subsequently gets changed
				publicKeyToken = ComputePublicKeyToken(publicKey);
			}
			return publicKeyToken;
		}

		public void SetPublicKeyToken(byte[] publicKeyToken)
		{
			this.publicKeyToken = publicKeyToken;
		}

		public AssemblyHashAlgorithm HashAlgorithm { get; set; }

		public byte[] __Hash => hash;

		public string FullName
		{
			get
			{
				if (Name == null)
				{
					return "";
				}
				ushort versionMajor = 0xFFFF;
				ushort versionMinor = 0xFFFF;
				ushort versionBuild = 0xFFFF;
				ushort versionRevision = 0xFFFF;
				if (version != null)
				{
					versionMajor = (ushort)version.Major;
					versionMinor = (ushort)version.Minor;
					versionBuild = (ushort)version.Build;
					versionRevision = (ushort)version.Revision;
				}
				var publicKeyToken = this.publicKeyToken;
				if ((publicKeyToken == null || publicKeyToken.Length == 0) && publicKey != null)
				{
					publicKeyToken = ComputePublicKeyToken(publicKey);
				}
				return GetFullName(Name, versionMajor, versionMinor, versionBuild, versionRevision, CultureName, publicKeyToken, (int)RawFlags);
			}
		}

		internal static string GetFullName(string name, ushort versionMajor, ushort versionMinor, ushort versionBuild, ushort versionRevision, string culture, byte[] publicKeyToken, int flags)
		{
			var stringBuilder = new StringBuilder();
			var doubleQuotes = name.StartsWith(" ") || name.EndsWith(" ") || name.IndexOf('\'') != -1;
			var singleQuotes = name.IndexOf('"') != -1;
			if (singleQuotes)
			{
				stringBuilder.Append('\'');
			}
			else if (doubleQuotes)
			{
				stringBuilder.Append('"');
			}
			if (name.IndexOf(',') != -1 || name.IndexOf('\\') != -1 || name.IndexOf('=') != -1 || (singleQuotes && name.IndexOf('\'') != -1))
			{
				for (var i = 0; i < name.Length; i++)
				{
					var c = name[i];
					if (c == ',' || c == '\\' || c == '=' || (singleQuotes && c == '\''))
					{
						stringBuilder.Append('\\');
					}
					stringBuilder.Append(c);
				}
			}
			else
			{
				stringBuilder.Append(name);
			}
			if (singleQuotes)
			{
				stringBuilder.Append('\'');
			}
			else if (doubleQuotes)
			{
				stringBuilder.Append('"');
			}
			if (versionMajor != 0xFFFF)
			{
				stringBuilder.Append(", Version=").Append(versionMajor);
				if (versionMinor != 0xFFFF)
				{
					stringBuilder.Append('.').Append(versionMinor);
					if (versionBuild != 0xFFFF)
					{
						stringBuilder.Append('.').Append(versionBuild);
						if (versionRevision != 0xFFFF)
						{
							stringBuilder.Append('.').Append(versionRevision);
						}
					}
				}
			}
			if (culture != null)
			{
				stringBuilder.Append(", Culture=").Append(culture == "" ? "neutral" : culture);
			}
			if (publicKeyToken != null)
			{
				stringBuilder.Append(", PublicKeyToken=");
				if (publicKeyToken.Length == 0)
				{
					stringBuilder.Append("null");
				}
				else
				{
					AppendPublicKey(stringBuilder, publicKeyToken);
				}
			}
			if ((flags & (int)AssemblyNameFlags.Retargetable) != 0)
			{
				stringBuilder.Append(", Retargetable=Yes");
			}
			if ((AssemblyContentType)((flags & 0xE00) >> 9) == AssemblyContentType.WindowsRuntime)
			{
				stringBuilder.Append(", ContentType=WindowsRuntime");
			}
			return stringBuilder.ToString();
		}

		internal static byte[] ComputePublicKeyToken(byte[] publicKey)
		{
			if (publicKey.Length == 0)
			{
				return publicKey;
			}
			byte[] hash;
			using var sha1 = SHA1.Create();
			hash = sha1.ComputeHash(publicKey);
			var token = new byte[8];
			for (var i = 0; i < token.Length; i++)
			{
				token[i] = hash[hash.Length - 1 - i];
			}
			return token;
		}

		internal static string ComputePublicKeyToken(string publicKey)
		{
			var stringBuilder = new StringBuilder(16);
			AppendPublicKey(stringBuilder, ComputePublicKeyToken(ParseKey(publicKey)));
			return stringBuilder.ToString();
		}

		private static void AppendPublicKey(StringBuilder sb, byte[] publicKey)
		{
			for (var i = 0; i < publicKey.Length; i++)
			{
				sb.Append("0123456789abcdef"[publicKey[i] >> 4]);
				sb.Append("0123456789abcdef"[publicKey[i] & 0x0F]);
			}
		}

		public override bool Equals(object obj)
		{
			return obj is AssemblyName other && other.FullName == FullName;
		}

		public override int GetHashCode()
		{
			return FullName.GetHashCode();
		}

		public object Clone()
		{
			var copy = (AssemblyName)MemberwiseClone();
			copy.publicKey = Copy(publicKey);
			copy.publicKeyToken = Copy(publicKeyToken);
			return copy;
		}

		private static byte[] Copy(byte[] b)
		{
			return b == null || b.Length == 0 ? b : (byte[])b.Clone();
		}

#if !NETSTANDARD
		public static bool ReferenceMatchesDefinition(AssemblyName reference, AssemblyName definition)
		{
			// HACK use the real AssemblyName to implement the (broken) ReferenceMatchesDefinition method
			return System.Reflection.AssemblyName.ReferenceMatchesDefinition(new System.Reflection.AssemblyName(reference.FullName), new System.Reflection.AssemblyName(definition.FullName));
		}
#endif

		public static AssemblyName GetAssemblyName(string path)
		{
			try
			{
				path = Path.GetFullPath(path);
				using var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
				var moduleReader = new ModuleReader(null, null, fileStream, path, false);
				if (moduleReader.Assembly == null)
				{
					throw new BadImageFormatException("Module does not contain a manifest");
				}
				return moduleReader.Assembly.GetName();
			}
			catch (IOException x)
			{
				throw new FileNotFoundException(x.Message, x);
			}
			catch (UnauthorizedAccessException x)
			{
				throw new FileNotFoundException(x.Message, x);
			}
		}

		internal AssemblyNameFlags RawFlags { get; set; }

		private static bool ParseVersion(string str, bool mustBeComplete, out Version version)
		{
			if (str == null)
			{
				version = null;
				return true;
			}
			var parts = str.Split('.');
			if (parts.Length < 2 || parts.Length > 4)
			{
				version = null;
				ushort dummy;
				// if the version consists of a single integer, it is invalid, but not invalid enough to fail
				// the parse of the whole assembly name
				return parts.Length == 1 && ushort.TryParse(parts[0], NumberStyles.Integer, null, out dummy);
			}
			if (parts[0] == "" || parts[1] == "")
			{
				// this is a strange scenario, the version is invalid, but not invalid enough to fail the
				// parse of the whole assembly name
				version = null;
				return true;
			}
			ushort build = 65535, revision = 65535;
			if (ushort.TryParse(parts[0], NumberStyles.Integer, null, out var major)
				&& ushort.TryParse(parts[1], NumberStyles.Integer, null, out var minor)
				&& (parts.Length <= 2 || parts[2] == "" || ushort.TryParse(parts[2], NumberStyles.Integer, null, out build))
				&& (parts.Length <= 3 || parts[3] == "" || (parts[2] != "" && ushort.TryParse(parts[3], NumberStyles.Integer, null, out revision))))
			{
				if (mustBeComplete && (parts.Length < 4 || parts[2] == "" || parts[3] == ""))
				{
					version = null;
				}
				else if (major == 65535 || minor == 65535)
				{
					version = null;
				}
				else
				{
					version = new Version(major, minor, build, revision);
				}
				return true;
			}
			version = null;
			return false;
		}
	}
}
