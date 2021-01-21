/*
  Copyright (C) 2009-2011 Jeroen Frijters

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
using System.Text;
using IKVM.Reflection.Emit;
using IKVM.Reflection.Writer;
using IKVM.Reflection.Reader;

namespace IKVM.Reflection
{
	public sealed class PropertySignature : Signature
	{
		private CallingConventions callingConvention;
		private readonly Type propertyType;
		private readonly Type[] parameterTypes;
		private readonly PackedCustomModifiers customModifiers;

		public static PropertySignature Create(CallingConventions callingConvention, 
													Type propertyType, 
													Type[] parameterTypes, 
													PackedCustomModifiers customModifiers)
		{
			return new PropertySignature(callingConvention, 
											propertyType, 
											Util.Copy(parameterTypes), 
											customModifiers);
		}

		private PropertySignature(CallingConventions callingConvention, 
									Type propertyType, 
									Type[] parameterTypes, 
									PackedCustomModifiers customModifiers)
		{
			this.callingConvention = callingConvention;
			this.propertyType = propertyType;
			this.parameterTypes = parameterTypes;
			this.customModifiers = customModifiers;
		}

		public override bool Equals(object obj)
		{
			var other = obj as PropertySignature;
			return other != null
				&& other.propertyType.Equals(propertyType)
				&& other.customModifiers.Equals(customModifiers);
		}

		public override int GetHashCode()
		{
			return propertyType.GetHashCode() ^ customModifiers.GetHashCode();
		}

		public int ParameterCount => parameterTypes.Length;

		public bool HasThis
		{
			set
			{
				if (value)
				{
					callingConvention |= CallingConventions.HasThis;
				}
				else
				{
					callingConvention &= ~CallingConventions.HasThis;
				}
			}
		}

		public Type PropertyType => propertyType;

		public CustomModifiers GetCustomModifiers()
		{
			return customModifiers.GetReturnTypeCustomModifiers();
		}

		public PropertySignature ExpandTypeParameters(Type declaringType)
		{
			return new PropertySignature(
				callingConvention,
				propertyType.BindTypeParameters(declaringType),
				BindTypeParameters(declaringType, parameterTypes),
				customModifiers.Bind(declaringType));
		}

		public override void WriteSig(ModuleBuilder module, ByteBuffer byteBuffer)
		{
			var flags = PROPERTY;
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
			byteBuffer.WriteCompressedUInt(parameterTypes == null ? 0 : parameterTypes.Length);
			WriteCustomModifiers(module, byteBuffer, customModifiers.GetReturnTypeCustomModifiers());
			WriteType(module, byteBuffer, propertyType);
			if (parameterTypes != null)
			{
				for (var i = 0; i < parameterTypes.Length; i++)
				{
					WriteCustomModifiers(module, byteBuffer, customModifiers.GetParameterCustomModifiers(i));
					WriteType(module, byteBuffer, parameterTypes[i]);
				}
			}
		}

		public Type GetParameter(int parameter)
		{
			return parameterTypes[parameter];
		}

		public CustomModifiers GetParameterCustomModifiers(int parameter)
		{
			return customModifiers.GetParameterCustomModifiers(parameter);
		}

		public CallingConventions CallingConvention => callingConvention;

		public bool MatchParameterTypes(Type[] types)
		{
			return Util.ArrayEquals(types, parameterTypes);
		}

		public static PropertySignature ReadSig(ModuleReader module, ByteReader byteReader, IGenericContext context)
		{
			var flags = byteReader.ReadByte();
			if ((flags & PROPERTY) == 0)
			{
				throw new BadImageFormatException();
			}
			var callingConvention = CallingConventions.Standard;
			if ((flags & HASTHIS) != 0)
			{
				callingConvention |= CallingConventions.HasThis;
			}
			if ((flags & EXPLICITTHIS) != 0)
			{
				callingConvention |= CallingConventions.ExplicitThis;
			}
			Type returnType;
			Type[] parameterTypes;
			var paramCount = byteReader.ReadCompressedUInt();
			CustomModifiers[] mods = null;
			PackedCustomModifiers.Pack(ref mods, 
									0, 
									CustomModifiers.Read(module, byteReader, context), 
									paramCount + 1);
			
			returnType = ReadRetType(module, byteReader, context);
			parameterTypes = new Type[paramCount];
			
			for (var i = 0; i < parameterTypes.Length; i++)
			{
				PackedCustomModifiers.Pack(ref mods, 
										i + 1, 
										CustomModifiers.Read(module, byteReader, context), 
										paramCount + 1);
				parameterTypes[i] = ReadParam(module, byteReader, context);
			}
			return new PropertySignature(callingConvention, returnType, parameterTypes, PackedCustomModifiers.Wrap(mods));
		}
	}
}
