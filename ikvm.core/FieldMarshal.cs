/*
  Copyright (C) 2008-2012 Jeroen Frijters

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
using System.Runtime.InteropServices;
using System.Text;
using IKVM.Reflection.Emit;
using IKVM.Reflection.Reader;
using IKVM.Reflection.Writer;
using IKVM.Reflection.Metadata;

#if NETSTANDARD
// VarEnum, UnmanagedType.IDispatch and UnmanagedType.SafeArray are obsolete
#pragma warning disable 618
#endif

namespace IKVM.Reflection
{
	public struct FieldMarshal
	{
		private const UnmanagedType UnmanagedType_CustomMarshaler = (UnmanagedType)0x2c;
		private const UnmanagedType NATIVE_TYPE_MAX = (UnmanagedType)0x50;
		public UnmanagedType UnmanagedType;
		public UnmanagedType? ArraySubType;
		public short? SizeParamIndex;
		public int? SizeConst;
		public VarEnum? SafeArraySubType;
		public Type SafeArrayUserDefinedSubType;
		public int? IidParameterIndex;
		public string MarshalType;
		public string MarshalCookie;
		public Type MarshalTypeRef;

		internal static bool ReadFieldMarshal(Module module, int token, out FieldMarshal fieldMarshal)
		{
			fieldMarshal = new FieldMarshal();
			foreach (var i in module.FieldMarshal.Filter(token))
			{
				var byteReader = module.GetBlob(module.FieldMarshal.records[i].NativeType);
				fieldMarshal.UnmanagedType = (UnmanagedType)byteReader.ReadCompressedUInt();
				if (fieldMarshal.UnmanagedType == UnmanagedType.LPArray)
				{
					fieldMarshal.ArraySubType = (UnmanagedType)byteReader.ReadCompressedUInt();
					if (fieldMarshal.ArraySubType == NATIVE_TYPE_MAX)
					{
						fieldMarshal.ArraySubType = null;
					}
					if (byteReader.Length != 0)
					{
						fieldMarshal.SizeParamIndex = (short)byteReader.ReadCompressedUInt();
						if (byteReader.Length != 0)
						{
							fieldMarshal.SizeConst = byteReader.ReadCompressedUInt();
							if (byteReader.Length != 0 && byteReader.ReadCompressedUInt() == 0)
							{
								fieldMarshal.SizeParamIndex = null;
							}
						}
					}
				}
				else if (fieldMarshal.UnmanagedType == UnmanagedType.SafeArray)
				{
					if (byteReader.Length != 0)
					{
						fieldMarshal.SafeArraySubType = (VarEnum)byteReader.ReadCompressedUInt();
						if (byteReader.Length != 0)
						{
							fieldMarshal.SafeArrayUserDefinedSubType = ReadType(module, byteReader);
						}
					}
				}
				else if (fieldMarshal.UnmanagedType == UnmanagedType.ByValArray)
				{
					fieldMarshal.SizeConst = byteReader.ReadCompressedUInt();
					if (byteReader.Length != 0)
					{
						fieldMarshal.ArraySubType = (UnmanagedType)byteReader.ReadCompressedUInt();
					}
				}
				else if (fieldMarshal.UnmanagedType == UnmanagedType.ByValTStr)
				{
					fieldMarshal.SizeConst = byteReader.ReadCompressedUInt();
				}
				else if (fieldMarshal.UnmanagedType == UnmanagedType.Interface
					|| fieldMarshal.UnmanagedType == UnmanagedType.IDispatch
					|| fieldMarshal.UnmanagedType == UnmanagedType.IUnknown)
				{
					if (byteReader.Length != 0)
					{
						fieldMarshal.IidParameterIndex = byteReader.ReadCompressedUInt();
					}
				}
				else if (fieldMarshal.UnmanagedType == UnmanagedType_CustomMarshaler)
				{
					byteReader.ReadCompressedUInt();
					byteReader.ReadCompressedUInt();
					fieldMarshal.MarshalType = ReadString(byteReader);
					fieldMarshal.MarshalCookie = ReadString(byteReader);

					var typeNameParser = TypeNameParser.Parse(fieldMarshal.MarshalType, false);
					if (!typeNameParser.Error)
					{
						fieldMarshal.MarshalTypeRef = typeNameParser.GetType(module.universe, 
																				module, 
																				false, 
																				fieldMarshal.MarshalType, 
																				false, 
																				false);
					}
				}
				return true;
			}
			return false;
		}

		internal static void SetMarshalAsAttribute(ModuleBuilder module, int token, CustomAttributeBuilder attributeBuilder)
		{
			attributeBuilder = attributeBuilder.DecodeBlob(module.Assembly);
			var record = new FieldMarshalTable.Record();
			record.Parent = token;
			record.NativeType = WriteMarshallingDescriptor(module, attributeBuilder);
			module.FieldMarshal.AddRecord(record);
		}

		private static int WriteMarshallingDescriptor(ModuleBuilder module, CustomAttributeBuilder attributeBuilder)
		{
			UnmanagedType unmanagedType;
			var val = attributeBuilder.GetConstructorArgument(0);
			if (val is short)
			{
				unmanagedType = (UnmanagedType)(short)val;
			}
			else if (val is int)
			{
				unmanagedType = (UnmanagedType)(int)val;
			}
			else
			{
				unmanagedType = (UnmanagedType)val;
			}

			var byteBuffer = new ByteBuffer(5);
			byteBuffer.WriteCompressedUInt((int)unmanagedType);

			if (unmanagedType == UnmanagedType.LPArray)
			{
				var arraySubType = attributeBuilder.GetFieldValue<UnmanagedType>("ArraySubType") ?? NATIVE_TYPE_MAX;
				byteBuffer.WriteCompressedUInt((int)arraySubType);
				var sizeParamIndex = attributeBuilder.GetFieldValue<short>("SizeParamIndex");
				var sizeConst = attributeBuilder.GetFieldValue<int>("SizeConst");
				if (sizeParamIndex != null)
				{
					byteBuffer.WriteCompressedUInt(sizeParamIndex.Value);
					if (sizeConst != null)
					{
						byteBuffer.WriteCompressedUInt(sizeConst.Value);
						byteBuffer.WriteCompressedUInt(1); // flag that says that SizeParamIndex was specified
					}
				}
				else if (sizeConst != null)
				{
					byteBuffer.WriteCompressedUInt(0); // SizeParamIndex
					byteBuffer.WriteCompressedUInt(sizeConst.Value);
					byteBuffer.WriteCompressedUInt(0); // flag that says that SizeParamIndex was not specified
				}
			}
			else if (unmanagedType == UnmanagedType.SafeArray)
			{
				var safeArraySubType = attributeBuilder.GetFieldValue<VarEnum>("SafeArraySubType");
				if (safeArraySubType != null)
				{
					byteBuffer.WriteCompressedUInt((int)safeArraySubType);
					var safeArrayUserDefinedSubType 
						= (Type)attributeBuilder.GetFieldValue("SafeArrayUserDefinedSubType");
					if (safeArrayUserDefinedSubType != null)
					{
						WriteType(module, byteBuffer, safeArrayUserDefinedSubType);
					}
				}
			}
			else if (unmanagedType == UnmanagedType.ByValArray)
			{
				byteBuffer.WriteCompressedUInt(attributeBuilder.GetFieldValue<int>("SizeConst") ?? 1);
				var arraySubType = attributeBuilder.GetFieldValue<UnmanagedType>("ArraySubType");
				if (arraySubType != null)
				{
					byteBuffer.WriteCompressedUInt((int)arraySubType);
				}
			}
			else if (unmanagedType == UnmanagedType.ByValTStr)
			{
				byteBuffer.WriteCompressedUInt(attributeBuilder.GetFieldValue<int>("SizeConst").Value);
			}
			else if (unmanagedType == UnmanagedType.Interface
				|| unmanagedType == UnmanagedType.IDispatch
				|| unmanagedType == UnmanagedType.IUnknown)
			{
				var iidParameterIndex = attributeBuilder.GetFieldValue<int>("IidParameterIndex");
				if (iidParameterIndex != null)
				{
					byteBuffer.WriteCompressedUInt(iidParameterIndex.Value);
				}
			}
			else if (unmanagedType == UnmanagedType_CustomMarshaler)
			{
				byteBuffer.WriteCompressedUInt(0);
				byteBuffer.WriteCompressedUInt(0);
				var marshalType = (string)attributeBuilder.GetFieldValue("MarshalType");
				if (marshalType != null)
				{
					WriteString(byteBuffer, marshalType);
				}
				else
				{
					WriteType(module, byteBuffer, (Type)attributeBuilder.GetFieldValue("MarshalTypeRef"));
				}
				WriteString(byteBuffer, (string)attributeBuilder.GetFieldValue("MarshalCookie") ?? "");
			}

			return module.Blobs.Add(byteBuffer);
		}

		private static Type ReadType(Module module, ByteReader br)
		{
			var str = ReadString(br);
			if (str == "")
			{
				return null;
			}
			return module.Assembly.GetType(str) ?? module.universe.GetType(str, true);
		}

		private static void WriteType(Module module, ByteBuffer bb, Type type)
		{
			WriteString(bb, type.Assembly == module.Assembly ? type.FullName : type.AssemblyQualifiedName);
		}

		private static string ReadString(ByteReader br)
		{
			return Encoding.UTF8.GetString(br.ReadBytes(br.ReadCompressedUInt()));
		}

		private static void WriteString(ByteBuffer byteBuffer, string str)
		{
			var bytes = Encoding.UTF8.GetBytes(str);
			byteBuffer.WriteCompressedUInt(bytes.Length);
			byteBuffer.Write(bytes);
		}
	}
}
