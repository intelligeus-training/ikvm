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
using System.Text;
using IKVM.Reflection.Emit;
using IKVM.Reflection.Writer;
using IKVM.Reflection.Reader;

namespace IKVM.Reflection
{
	public sealed class FieldSignature : Signature
	{
		private readonly Type fieldType;
		private readonly CustomModifiers mods;

		public static FieldSignature Create(Type fieldType, CustomModifiers customModifiers)
		{
			return new FieldSignature(fieldType, customModifiers);
		}

		private FieldSignature(Type fieldType, CustomModifiers mods)
		{
			this.fieldType = fieldType;
			this.mods = mods;
		}

		public override bool Equals(object obj)
		{
			return obj is FieldSignature other
			       && other.fieldType.Equals(fieldType)
			       && other.mods.Equals(mods);
		}

		public override int GetHashCode()
		{
			return fieldType.GetHashCode() ^ mods.GetHashCode();
		}

		public Type FieldType => fieldType;

		public CustomModifiers GetCustomModifiers()
		{
			return mods;
		}

		public FieldSignature ExpandTypeParameters(Type declaringType)
		{
			return new FieldSignature(
				fieldType.BindTypeParameters(declaringType),
				mods.Bind(declaringType));
		}

		public static FieldSignature ReadSig(ModuleReader module, ByteReader byteReader, IGenericContext context)
		{
			if (byteReader.ReadByte() != FIELD)
			{
				throw new BadImageFormatException();
			}
			var mods = CustomModifiers.Read(module, byteReader, context);
			var fieldType = ReadType(module, byteReader, context);
			return new FieldSignature(fieldType, mods);
		}

		public override void WriteSig(ModuleBuilder module, ByteBuffer byteBuffer)
		{
			byteBuffer.Write(FIELD);
			WriteCustomModifiers(module, byteBuffer, mods);
			WriteType(module, byteBuffer, fieldType);
		}
	}
}
