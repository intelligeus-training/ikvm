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
using System.Collections.Generic;
using CallingConvention = System.Runtime.InteropServices.CallingConvention;
using IKVM.Reflection.Reader;
using IKVM.Reflection.Emit;
using IKVM.Reflection.Writer;
using IKVM.Reflection.Metadata;

namespace IKVM.Reflection
{
	public abstract class Signature
	{
		public const byte DEFAULT = 0x00;
		public const byte VARARG = 0x05;
		public const byte GENERIC = 0x10;
		public const byte HASTHIS = 0x20;
		public const byte EXPLICITTHIS = 0x40;
		public const byte FIELD = 0x06;
		public const byte LOCAL_SIG = 0x07;
		public const byte PROPERTY = 0x08;
		public const byte GENERICINST = 0x0A;
		public const byte SENTINEL = 0x41;
		public const byte ELEMENT_TYPE_VOID = 0x01;
		public const byte ELEMENT_TYPE_BOOLEAN = 0x02;
		public const byte ELEMENT_TYPE_CHAR = 0x03;
		public const byte ELEMENT_TYPE_I1 = 0x04;
		public const byte ELEMENT_TYPE_U1 = 0x05;
		public const byte ELEMENT_TYPE_I2 = 0x06;
		public const byte ELEMENT_TYPE_U2 = 0x07;
		public const byte ELEMENT_TYPE_I4 = 0x08;
		public const byte ELEMENT_TYPE_U4 = 0x09;
		public const byte ELEMENT_TYPE_I8 = 0x0a;
		public const byte ELEMENT_TYPE_U8 = 0x0b;
		public const byte ELEMENT_TYPE_R4 = 0x0c;
		public const byte ELEMENT_TYPE_R8 = 0x0d;
		public const byte ELEMENT_TYPE_STRING = 0x0e;
		public const byte ELEMENT_TYPE_PTR = 0x0f;
		public const byte ELEMENT_TYPE_BYREF = 0x10;
		public const byte ELEMENT_TYPE_VALUETYPE = 0x11;
		public const byte ELEMENT_TYPE_CLASS = 0x12;
		public const byte ELEMENT_TYPE_VAR = 0x13;
		public const byte ELEMENT_TYPE_ARRAY = 0x14;
		public const byte ELEMENT_TYPE_GENERICINST = 0x15;
		public const byte ELEMENT_TYPE_TYPEDBYREF = 0x16;
		public const byte ELEMENT_TYPE_I = 0x18;
		public const byte ELEMENT_TYPE_U = 0x19;
		public const byte ELEMENT_TYPE_FNPTR = 0x1b;
		public const byte ELEMENT_TYPE_OBJECT = 0x1c;
		public const byte ELEMENT_TYPE_SZARRAY = 0x1d;
		public const byte ELEMENT_TYPE_MVAR = 0x1e;
		public const byte ELEMENT_TYPE_CMOD_REQD = 0x1f;
		public const byte ELEMENT_TYPE_CMOD_OPT = 0x20;
		public const byte ELEMENT_TYPE_PINNED = 0x45;

		public abstract void WriteSig(ModuleBuilder module, ByteBuffer byteBuffer);

		private static Type ReadGenericInst(ModuleReader module, ByteReader br, IGenericContext context)
		{
			Type type = br.ReadByte() switch
			{
				ELEMENT_TYPE_CLASS => ReadTypeDefOrRefEncoded(module, br, context).MarkNotValueType(),
				ELEMENT_TYPE_VALUETYPE => ReadTypeDefOrRefEncoded(module, br, context).MarkValueType(),
				_ => throw new BadImageFormatException()
			};
			if (!type.__IsMissing && !type.IsGenericTypeDefinition)
			{
				throw new BadImageFormatException();
			}
			var genArgCount = br.ReadCompressedUInt();
			var args = new Type[genArgCount];
			CustomModifiers[] mods = null;
			for (var i = 0; i < genArgCount; i++)
			{
				// LAMESPEC the Type production (23.2.12) doesn't include CustomMod* for genericinst, but C++ uses it, the verifier allows it and ildasm also supports it
				CustomModifiers cm = CustomModifiers.Read(module, br, context);
				if (!cm.IsEmpty)
				{
					if (mods == null)
					{
						mods = new CustomModifiers[genArgCount];
					}
					mods[i] = cm;
				}
				args[i] = ReadType(module, br, context);
			}
			return GenericTypeInstance.Make(type, args, mods);
		}

		public static Type ReadTypeSpec(ModuleReader module, ByteReader br, IGenericContext context)
		{
			// LAMESPEC a TypeSpec can contain custom modifiers (C++/CLI generates "newarr (TypeSpec with custom modifiers)")
			CustomModifiers.Skip(br);
			// LAMESPEC anything can be adorned by (useless) custom modifiers
			// also, VAR and MVAR are also used in TypeSpec (contrary to what the spec says)
			return ReadType(module, br, context);
		}

		private static Type ReadFunctionPointer(ModuleReader module, ByteReader br, IGenericContext context)
		{
			__StandAloneMethodSig sig = MethodSignature.ReadStandAloneMethodSig(module, br, context);
			if (module.universe.EnableFunctionPointers)
			{
				return FunctionPointerType.Make(module.universe, sig);
			}
			else
			{
				// by default, like .NET we return System.IntPtr here
				return module.universe.System_IntPtr;
			}
		}

		public static Type[] ReadMethodSpec(ModuleReader module, ByteReader byteReader, IGenericContext context)
		{
			if (byteReader.ReadByte() != GENERICINST)
			{
				throw new BadImageFormatException();
			}
			var args = new Type[byteReader.ReadCompressedUInt()];
			for (var i = 0; i < args.Length; i++)
			{
				CustomModifiers.Skip(byteReader);
				args[i] = ReadType(module, byteReader, context);
			}
			return args;
		}

		private static int[] ReadArraySizes(ByteReader br)
		{
			var num = br.ReadCompressedUInt();
			if (num == 0)
			{
				return null;
			}
			var arr = new int[num];
			for (int i = 0; i < num; i++)
			{
				arr[i] = br.ReadCompressedUInt();
			}
			return arr;
		}

		private static int[] ReadArrayBounds(ByteReader br)
		{
			var num = br.ReadCompressedUInt();
			if (num == 0)
			{
				return null;
			}
			var arr = new int[num];
			for (int i = 0; i < num; i++)
			{
				arr[i] = br.ReadCompressedInt();
			}
			return arr;
		}

		private static Type ReadTypeOrVoid(ModuleReader module, ByteReader byteReader, IGenericContext context)
		{
			if (byteReader.PeekByte() == ELEMENT_TYPE_VOID)
			{
				byteReader.ReadByte();
				return module.universe.System_Void;
			}
			
			return ReadType(module, byteReader, context);
			
		}

		// see ECMA 335 CLI spec June 2006 section 23.2.12 for this production
		protected static Type ReadType(ModuleReader module, ByteReader byteReader, IGenericContext context)
		{
			CustomModifiers mods;
			switch (byteReader.ReadByte())
			{
				case ELEMENT_TYPE_CLASS:
					return ReadTypeDefOrRefEncoded(module, byteReader, context).MarkNotValueType();
				case ELEMENT_TYPE_VALUETYPE:
					return ReadTypeDefOrRefEncoded(module, byteReader, context).MarkValueType();
				case ELEMENT_TYPE_BOOLEAN:
					return module.universe.System_Boolean;
				case ELEMENT_TYPE_CHAR:
					return module.universe.System_Char;
				case ELEMENT_TYPE_I1:
					return module.universe.System_SByte;
				case ELEMENT_TYPE_U1:
					return module.universe.System_Byte;
				case ELEMENT_TYPE_I2:
					return module.universe.System_Int16;
				case ELEMENT_TYPE_U2:
					return module.universe.System_UInt16;
				case ELEMENT_TYPE_I4:
					return module.universe.System_Int32;
				case ELEMENT_TYPE_U4:
					return module.universe.System_UInt32;
				case ELEMENT_TYPE_I8:
					return module.universe.System_Int64;
				case ELEMENT_TYPE_U8:
					return module.universe.System_UInt64;
				case ELEMENT_TYPE_R4:
					return module.universe.System_Single;
				case ELEMENT_TYPE_R8:
					return module.universe.System_Double;
				case ELEMENT_TYPE_I:
					return module.universe.System_IntPtr;
				case ELEMENT_TYPE_U:
					return module.universe.System_UIntPtr;
				case ELEMENT_TYPE_STRING:
					return module.universe.System_String;
				case ELEMENT_TYPE_OBJECT:
					return module.universe.System_Object;
				case ELEMENT_TYPE_VAR:
					return context.GetGenericTypeArgument(byteReader.ReadCompressedUInt());
				case ELEMENT_TYPE_MVAR:
					return context.GetGenericMethodArgument(byteReader.ReadCompressedUInt());
				case ELEMENT_TYPE_GENERICINST:
					return ReadGenericInst(module, byteReader, context);
				case ELEMENT_TYPE_SZARRAY:
					mods = CustomModifiers.Read(module, byteReader, context);
					return ReadType(module, byteReader, context).__MakeArrayType(mods);
				case ELEMENT_TYPE_ARRAY:
					mods = CustomModifiers.Read(module, byteReader, context);
					return ReadType(module, byteReader, context).__MakeArrayType(byteReader.ReadCompressedUInt(), 
																				ReadArraySizes(byteReader), 
																				ReadArrayBounds(byteReader),
																				mods);
				case ELEMENT_TYPE_PTR:
					mods = CustomModifiers.Read(module, byteReader, context);
					return ReadTypeOrVoid(module, byteReader, context).__MakePointerType(mods);
				case ELEMENT_TYPE_FNPTR:
					return ReadFunctionPointer(module, byteReader, context);
				default:
					throw new BadImageFormatException();
			}
		}

		public static void ReadLocalVarSig(ModuleReader module, 
												ByteReader byteReader, 
												IGenericContext context, 
												List<LocalVariableInfo> list)
		{
			if (byteReader.Length < 2 || byteReader.ReadByte() != LOCAL_SIG)
			{
				throw new BadImageFormatException("Invalid local variable signature");
			}
			var count = byteReader.ReadCompressedUInt();
			for (var i = 0; i < count; i++)
			{
				if (byteReader.PeekByte() == ELEMENT_TYPE_TYPEDBYREF)
				{
					byteReader.ReadByte();
					list.Add(new LocalVariableInfo(i, 
														module.universe.System_TypedReference, 
														false, 
														new CustomModifiers()));
				}
				else
				{
					var customModifiers = CustomModifiers.Read(module, byteReader, context);
					var pinned = false;
					if (byteReader.PeekByte() == ELEMENT_TYPE_PINNED)
					{
						byteReader.ReadByte();
						pinned = true;
					}
					var mods2 = CustomModifiers.Read(module, byteReader, context);
					var type = ReadTypeOrByRef(module, byteReader, context);
					list.Add(new LocalVariableInfo(i, type, pinned, CustomModifiers.Combine(customModifiers, mods2)));
				}
			}
		}

		private static Type ReadTypeOrByRef(ModuleReader module, ByteReader byteReader, IGenericContext context)
		{
			if (byteReader.PeekByte() == ELEMENT_TYPE_BYREF)
			{
				byteReader.ReadByte();
				// LAMESPEC it is allowed (by C++/CLI, ilasm and peverify) to have custom modifiers after the BYREF
				// (which makes sense, as it is analogous to pointers)
				var mods = CustomModifiers.Read(module, byteReader, context);
				// C++/CLI generates void& local variables, so we need to use ReadTypeOrVoid here
				return ReadTypeOrVoid(module, byteReader, context).__MakeByRefType(mods);
			}
	
			return ReadType(module, byteReader, context);
			
		}

		protected static Type ReadRetType(ModuleReader module, ByteReader byteReader, IGenericContext context)
		{
			switch (byteReader.PeekByte())
			{
				case ELEMENT_TYPE_VOID:
					byteReader.ReadByte();
					return module.universe.System_Void;
				case ELEMENT_TYPE_TYPEDBYREF:
					byteReader.ReadByte();
					return module.universe.System_TypedReference;
				default:
					return ReadTypeOrByRef(module, byteReader, context);
			}
		}

		protected static Type ReadParam(ModuleReader module, ByteReader byteReader, IGenericContext context)
		{
			switch (byteReader.PeekByte())
			{
				case ELEMENT_TYPE_TYPEDBYREF:
					byteReader.ReadByte();
					return module.universe.System_TypedReference;
				default:
					return ReadTypeOrByRef(module, byteReader, context);
			}
		}

		protected static void WriteType(ModuleBuilder module, ByteBuffer byteBuffer, Type type)
		{
			while (type.HasElementType)
			{
				var sigElementType = type.SigElementType;
				byteBuffer.Write(sigElementType);
				if (sigElementType == ELEMENT_TYPE_ARRAY)
				{
					// LAMESPEC the Type production (23.2.12) doesn't include CustomMod* for arrays, but the verifier allows it and ildasm also supports it
					WriteCustomModifiers(module, byteBuffer, type.__GetCustomModifiers());
					WriteType(module, byteBuffer, type.GetElementType());
					byteBuffer.WriteCompressedUInt(type.GetArrayRank());
					var sizes = type.__GetArraySizes();
					byteBuffer.WriteCompressedUInt(sizes.Length);
					for (var i = 0; i < sizes.Length; i++)
					{
						byteBuffer.WriteCompressedUInt(sizes[i]);
					}
					var lobounds = type.__GetArrayLowerBounds();
					byteBuffer.WriteCompressedUInt(lobounds.Length);
					for (var i = 0; i < lobounds.Length; i++)
					{
						byteBuffer.WriteCompressedInt(lobounds[i]);
					}
					return;
				}
				WriteCustomModifiers(module, byteBuffer, type.__GetCustomModifiers());
				type = type.GetElementType();
			}
			if (type.__IsBuiltIn)
			{
				byteBuffer.Write(type.SigElementType);
			}
			else if (type.IsGenericParameter)
			{
				byteBuffer.Write(type.SigElementType);
				byteBuffer.WriteCompressedUInt(type.GenericParameterPosition);
			}
			else if (!type.__IsMissing && type.IsGenericType)
			{
				WriteGenericSignature(module, byteBuffer, type);
			}
			else if (type.__IsFunctionPointer)
			{
				byteBuffer.Write(ELEMENT_TYPE_FNPTR);
				WriteStandAloneMethodSig(module, byteBuffer, type.__MethodSignature);
			}
			else
			{
				if (type.IsValueType)
				{
					byteBuffer.Write(ELEMENT_TYPE_VALUETYPE);
				}
				else
				{
					byteBuffer.Write(ELEMENT_TYPE_CLASS);
				}
				byteBuffer.WriteTypeDefOrRefEncoded(module.GetTypeToken(type).Token);
			}
		}

		private static void WriteGenericSignature(ModuleBuilder module, ByteBuffer byteBuffer, Type type)
		{
			var typeArguments = type.GetGenericArguments();
			var customModifiers = type.__GetGenericArgumentsCustomModifiers();
			if (!type.IsGenericTypeDefinition)
			{
				type = type.GetGenericTypeDefinition();
			}
			byteBuffer.Write(ELEMENT_TYPE_GENERICINST);
			if (type.IsValueType)
			{
				byteBuffer.Write(ELEMENT_TYPE_VALUETYPE);
			}
			else
			{
				byteBuffer.Write(ELEMENT_TYPE_CLASS);
			}
			byteBuffer.WriteTypeDefOrRefEncoded(module.GetTypeToken(type).Token);
			byteBuffer.WriteCompressedUInt(typeArguments.Length);
			for (var i = 0; i < typeArguments.Length; i++)
			{
				WriteCustomModifiers(module, byteBuffer, customModifiers[i]);
				WriteType(module, byteBuffer, typeArguments[i]);
			}
		}

		protected static void WriteCustomModifiers(ModuleBuilder module, ByteBuffer byteBuffer, CustomModifiers modifiers)
		{
			foreach (var entry in modifiers)
			{
				byteBuffer.Write(entry.IsRequired ? ELEMENT_TYPE_CMOD_REQD : ELEMENT_TYPE_CMOD_OPT);
				byteBuffer.WriteTypeDefOrRefEncoded(module.GetTypeTokenForMemberRef(entry.Type));
			}
		}

		public static Type ReadTypeDefOrRefEncoded(ModuleReader module, ByteReader br, IGenericContext context)
		{
			var encoded = br.ReadCompressedUInt();
			return (encoded & 3) switch
			{
				0 => module.ResolveType((TypeDefTable.Index << 24) + (encoded >> 2), null, null),
				1 => module.ResolveType((TypeRefTable.Index << 24) + (encoded >> 2), null, null),
				2 => module.ResolveType((TypeSpecTable.Index << 24) + (encoded >> 2), context),
				_ => throw new BadImageFormatException()
			};
		}

		public static void WriteStandAloneMethodSig(ModuleBuilder module, 
														ByteBuffer byteBuffer, 
														__StandAloneMethodSig signature)
		{
			if (signature.IsUnmanaged)
			{
				switch (signature.UnmanagedCallingConvention)
				{
					case CallingConvention.Cdecl:
						byteBuffer.Write((byte)0x01);	// C
						break;
					case CallingConvention.StdCall:
					case CallingConvention.Winapi:
						byteBuffer.Write((byte)0x02);	// STDCALL
						break;
					case CallingConvention.ThisCall:
						byteBuffer.Write((byte)0x03);	// THISCALL
						break;
#if NETSTANDARD
					case (CallingConvention)5:
#else
					case CallingConvention.FastCall:
#endif
						byteBuffer.Write((byte)0x04);	// FASTCALL
						break;
					default:
						throw new ArgumentOutOfRangeException("callingConvention");
				}
			}
			else
			{
				var callingConvention = signature.CallingConvention;
				var flags = 0;
				if ((callingConvention & CallingConventions.HasThis) != 0)
				{
					flags |= HASTHIS;
				}
				if ((callingConvention & CallingConventions.ExplicitThis) != 0)
				{
					flags |= EXPLICITTHIS;
				}
				if ((callingConvention & CallingConventions.VarArgs) != 0)
				{
					flags |= VARARG;
				}
				byteBuffer.Write(flags);
			}
			var parameterTypes = signature.ParameterTypes;
			var optionalParameterTypes = signature.OptionalParameterTypes;
			byteBuffer.WriteCompressedUInt(parameterTypes.Length + optionalParameterTypes.Length);
			WriteCustomModifiers(module, byteBuffer, signature.GetReturnTypeCustomModifiers());
			WriteType(module, byteBuffer, signature.ReturnType);
			var index = 0;
			foreach (var type in parameterTypes)
			{
				WriteCustomModifiers(module, byteBuffer, signature.GetParameterCustomModifiers(index++));
				WriteType(module, byteBuffer, type);
			}
			// note that optional parameters are only allowed for managed signatures (but we don't enforce that)
			if (optionalParameterTypes.Length > 0)
			{
				byteBuffer.Write(SENTINEL);
				foreach (var type in optionalParameterTypes)
				{
					WriteCustomModifiers(module, byteBuffer, signature.GetParameterCustomModifiers(index++));
					WriteType(module, byteBuffer, type);
				}
			}
		}

		public static void WriteTypeSpec(ModuleBuilder module, ByteBuffer byteBuffer, Type type)
		{
			WriteType(module, byteBuffer, type);
		}

		public static void WriteMethodSpec(ModuleBuilder module, ByteBuffer byteBuffer, Type[] genArgs)
		{
			byteBuffer.Write(GENERICINST);
			byteBuffer.WriteCompressedUInt(genArgs.Length);
			foreach (var arg in genArgs)
			{
				WriteType(module, byteBuffer, arg);
			}
		}

		// this reads just the optional parameter types, from a MethodRefSig
		public static Type[] ReadOptionalParameterTypes(ModuleReader module, 
															ByteReader byteReader, 
															IGenericContext context, 
															out CustomModifiers[] customModifiers)
		{
			byteReader.ReadByte();
			var paramCount = byteReader.ReadCompressedUInt();
			CustomModifiers.Skip(byteReader);
			ReadRetType(module, byteReader, context);
			for (var i = 0; i < paramCount; i++)
			{
				if (byteReader.PeekByte() == SENTINEL)
				{
					byteReader.ReadByte();
					var types = new Type[paramCount - i];
					customModifiers = new CustomModifiers[types.Length];
					for (var j = 0; j < types.Length; j++)
					{
						customModifiers[j] = CustomModifiers.Read(module, byteReader, context);
						types[j] = ReadType(module, byteReader, context);
					}
					return types;
				}
				CustomModifiers.Skip(byteReader);
				ReadType(module, byteReader, context);
			}
			customModifiers = Empty<CustomModifiers>.Array;
			return Type.EmptyTypes;
		}

		protected static Type[] BindTypeParameters(IGenericBinder binder, Type[] types)
		{
			if (types == null || types.Length == 0)
			{
				return Type.EmptyTypes;
			}
			var expanded = new Type[types.Length];
			for (var i = 0; i < types.Length; i++)
			{
				expanded[i] = types[i].BindTypeParameters(binder);
			}
			return expanded;
		}

		public static void WriteSignatureHelper(ModuleBuilder module, 
													ByteBuffer byteBuffer, 
													byte flags, 
													ushort paramCount, 
													List<Type> args)
		{
			byteBuffer.Write(flags);
			if (flags != FIELD)
			{
				byteBuffer.WriteCompressedUInt(paramCount);
			}
			foreach (var type in args)
			{
				if (type == null)
				{
					byteBuffer.Write(ELEMENT_TYPE_VOID);
				}
				else if (type is MarkerType)
				{
					byteBuffer.Write(type.SigElementType);
				}
				else
				{
					WriteType(module, byteBuffer, type);
				}
			}
		}
	}
}
