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
using System.Text;
using System.Linq;
using IKVM.Reflection.Reader;
using IKVM.Reflection.Emit;
using IKVM.Reflection.Metadata;

namespace IKVM.Reflection
{
	public sealed class CustomAttributeData
	{
		internal static readonly IList<CustomAttributeData> EmptyList = new List<CustomAttributeData>(0).AsReadOnly();

		/*
		 * There are several states a CustomAttributeData object can be in:
		 * 
		 * 1) Unresolved Custom Attribute
		 *    - customAttributeIndex >= 0
		 *    - declSecurityIndex == -1
		 *    - declSecurityBlob == null
		 *    - lazyConstructor = null
		 *    - lazyConstructorArguments = null
		 *    - lazyNamedArguments = null
		 * 
		 * 2) Resolved Custom Attribute
		 *    - customAttributeIndex >= 0
		 *    - declSecurityIndex == -1
		 *    - declSecurityBlob == null
		 *    - lazyConstructor != null
		 *    - lazyConstructorArguments != null
		 *    - lazyNamedArguments != null
		 *    
		 * 3) Pre-resolved Custom Attribute
		 *    - customAttributeIndex = -1
		 *    - declSecurityIndex == -1
		 *    - declSecurityBlob == null
		 *    - lazyConstructor != null
		 *    - lazyConstructorArguments != null
		 *    - lazyNamedArguments != null
		 *    
		 * 4) Pseudo Custom Attribute, .NET 1.x declarative security or result of CustomAttributeBuilder.ToData()
		 *    - customAttributeIndex = -1
		 *    - declSecurityIndex == -1
		 *    - declSecurityBlob == null
		 *    - lazyConstructor != null
		 *    - lazyConstructorArguments != null
		 *    - lazyNamedArguments != null
		 *    
		 * 5) Unresolved declarative security
		 *    - customAttributeIndex = -1
		 *    - declSecurityIndex >= 0
		 *    - declSecurityBlob != null
		 *    - lazyConstructor != null
		 *    - lazyConstructorArguments != null
		 *    - lazyNamedArguments == null
		 * 
		 * 6) Resolved declarative security
		 *    - customAttributeIndex = -1
		 *    - declSecurityIndex >= 0
		 *    - declSecurityBlob == null
		 *    - lazyConstructor != null
		 *    - lazyConstructorArguments != null
		 *    - lazyNamedArguments != null
		 * 
		 */
		private readonly Module module;
		private readonly int customAttributeIndex;
		private readonly int declSecurityIndex;
		private readonly byte[] declSecurityBlob;
		private ConstructorInfo lazyConstructor;
		private IList<CustomAttributeTypedArgument> lazyConstructorArguments;
		private IList<CustomAttributeNamedArgument> lazyNamedArguments;

		// 1) Unresolved Custom Attribute
		internal CustomAttributeData(Module modl, int index)
		{
			module = modl;
			customAttributeIndex = index;
			declSecurityIndex = -1;
		}

		// 4) Pseudo Custom Attribute, .NET 1.x declarative security
		internal CustomAttributeData(Module module, 
										ConstructorInfo constructor, 
										object[] args, 
										List<CustomAttributeNamedArgument> namedArguments)
			: this(module, 
					constructor, 
					WrapConstructorArgs(args, constructor.MethodSignature), namedArguments)
		{
		}
		
		// 4) Pseudo Custom Attribute, .NET 1.x declarative security or result of CustomAttributeBuilder.ToData()
		internal CustomAttributeData(Module module, 
										ConstructorInfo constructor, 
										List<CustomAttributeTypedArgument> constructorArgs, 
										List<CustomAttributeNamedArgument> namedArguments)
		{
			this.module = module;
			customAttributeIndex = -1;
			declSecurityIndex = -1;
			lazyConstructor = constructor;
			lazyConstructorArguments = constructorArgs.AsReadOnly();
			if (namedArguments == null)
			{
				lazyNamedArguments = Empty<CustomAttributeNamedArgument>.Array;
			}
			else
			{
				lazyNamedArguments = namedArguments.AsReadOnly();
			}
		}

		// 3) Pre-resolved Custom Attribute
		internal CustomAttributeData(Assembly asm, ConstructorInfo constructor, ByteReader br)
		{
			this.module = asm.ManifestModule;
			this.customAttributeIndex = -1;
			this.declSecurityIndex = -1;
			this.lazyConstructor = constructor;
			if (br.Length == 0)
			{
				// it's legal to have an empty blob
				lazyConstructorArguments = Empty<CustomAttributeTypedArgument>.Array;
				lazyNamedArguments = Empty<CustomAttributeNamedArgument>.Array;
			}
			else
			{
				if (br.ReadUInt16() != 1)
				{
					throw new BadImageFormatException();
				}
				lazyConstructorArguments = ReadConstructorArguments(module, br, constructor);
				lazyNamedArguments = ReadNamedArguments(module, br, br.ReadUInt16(), constructor.DeclaringType, true);
			}
		}
		
		private static List<CustomAttributeTypedArgument> WrapConstructorArgs(object[] args, MethodSignature sig)
		{
			return args.Select((t, i) => new CustomAttributeTypedArgument(sig.GetParameterType(i), t)).ToList();
		}

		public override string ToString()
		{
			var stringBuilder = new StringBuilder();
			stringBuilder.Append('[');
			stringBuilder.Append(Constructor.DeclaringType.FullName);
			stringBuilder.Append('(');
			var sep = "";
			var parameters = Constructor.GetParameters();
			var args = ConstructorArguments;
			
			for (var i = 0; i < parameters.Length; i++)
			{
				stringBuilder.Append(sep);
				sep = ", ";
				AppendValue(stringBuilder, parameters[i].ParameterType, args[i]);
			}
			foreach (CustomAttributeNamedArgument named in NamedArguments)
			{
				stringBuilder.Append(sep);
				sep = ", ";
				stringBuilder.Append(named.MemberInfo.Name);
				stringBuilder.Append(" = ");
				FieldInfo fi = named.MemberInfo as FieldInfo;
				Type type = fi != null ? fi.FieldType : ((PropertyInfo)named.MemberInfo).PropertyType;
				AppendValue(stringBuilder, type, named.TypedValue);
			}
			stringBuilder.Append(')');
			stringBuilder.Append(']');
			return stringBuilder.ToString();
		}

		private static void AppendValue(StringBuilder stringBuilder, Type type, CustomAttributeTypedArgument arg)
		{
			if (arg.ArgumentType == arg.ArgumentType.Module.universe.System_String)
			{
				stringBuilder.Append('"').Append(arg.Value).Append('"');
			}
			else if (arg.ArgumentType.IsArray)
			{
				Type elementType = arg.ArgumentType.GetElementType();
				string elementTypeName;
				if (elementType.IsPrimitive
					|| elementType == type.Module.universe.System_Object
					|| elementType == type.Module.universe.System_String
					|| elementType == type.Module.universe.System_Type)
				{
					elementTypeName = elementType.Name;
				}
				else
				{
					elementTypeName = elementType.FullName;
				}
				stringBuilder.Append("new ").Append(elementTypeName).Append("[").Append(((Array)arg.Value).Length).Append("] { ");
				var sep = "";
				foreach (CustomAttributeTypedArgument elem in (CustomAttributeTypedArgument[])arg.Value)
				{
					stringBuilder.Append(sep);
					sep = ", ";
					AppendValue(stringBuilder, elementType, elem);
				}
				stringBuilder.Append(" }");
			}
			else
			{
				if (arg.ArgumentType != type || (type.IsEnum && !arg.Value.Equals(0)))
				{
					stringBuilder.Append('(');
					stringBuilder.Append(arg.ArgumentType.FullName);
					stringBuilder.Append(')');
				}
				stringBuilder.Append(arg.Value);
			}
		}

		internal static void ReadDeclarativeSecurity(Module module, int index, List<CustomAttributeData> list)
		{
			var universe = module.universe;
			var assembly = module.Assembly;
			var action = module.DeclSecurity.records[index].Action;
			var byteReader = module.GetBlob(module.DeclSecurity.records[index].PermissionSet);
			if (byteReader.PeekByte() == '.')
			{
				byteReader.ReadByte();
				var count = byteReader.ReadCompressedUInt();
				for (var j = 0; j < count; j++)
				{
					var type = ReadType(module, byteReader);
					var constructorInfo = 
						type.GetPseudoCustomAttributeConstructor(universe.System_Security_Permissions_SecurityAction);
					// LAMESPEC there is an additional length here (probably of the named argument list)
					var blob = byteReader.ReadBytes(byteReader.ReadCompressedUInt());
					list.Add(new CustomAttributeData(assembly, constructorInfo, action, blob, index));
				}
			}
			else
			{
				// .NET 1.x format (xml)
				var buffer = new char[byteReader.Length / 2];
				for (var i = 0; i < buffer.Length; i++)
				{
					buffer[i] = byteReader.ReadChar();
				}
				var xml = new String(buffer);
				var constructorInfo = universe.System_Security_Permissions_PermissionSetAttribute.GetPseudoCustomAttributeConstructor(universe.System_Security_Permissions_SecurityAction);
				var namedArguments = new List<CustomAttributeNamedArgument>();
				namedArguments.Add(
					new CustomAttributeNamedArgument(
						GetProperty(null, 
											universe.System_Security_Permissions_PermissionSetAttribute, 
											"XML", 
											universe.System_String),
					new CustomAttributeTypedArgument(universe.System_String, xml)));
				
				list.Add(new CustomAttributeData(assembly.ManifestModule, constructorInfo, new object[] { action }, namedArguments));
			}
		}

		// 5) Unresolved declarative security
		internal CustomAttributeData(Assembly asm, 
										ConstructorInfo constructor, 
										int securityAction, 
										byte[] blob, 
										int index)
		{
			module = asm.ManifestModule;
			customAttributeIndex = -1;
			declSecurityIndex = index;
			var universe = constructor.Module.universe;
			lazyConstructor = constructor;
			var attributeTypedArguments = new List<CustomAttributeTypedArgument>();
			attributeTypedArguments.Add(
				new CustomAttributeTypedArgument(universe.System_Security_Permissions_SecurityAction, 
														securityAction));
			lazyConstructorArguments =  attributeTypedArguments.AsReadOnly();
			declSecurityBlob = blob;
		}

		private static Type ReadFieldOrPropType(Module context, ByteReader br)
		{
			var universe = context.universe;
			return br.ReadByte() switch
			{
				Signature.ELEMENT_TYPE_BOOLEAN => universe.System_Boolean,
				Signature.ELEMENT_TYPE_CHAR => universe.System_Char,
				Signature.ELEMENT_TYPE_I1 => universe.System_SByte,
				Signature.ELEMENT_TYPE_U1 => universe.System_Byte,
				Signature.ELEMENT_TYPE_I2 => universe.System_Int16,
				Signature.ELEMENT_TYPE_U2 => universe.System_UInt16,
				Signature.ELEMENT_TYPE_I4 => universe.System_Int32,
				Signature.ELEMENT_TYPE_U4 => universe.System_UInt32,
				Signature.ELEMENT_TYPE_I8 => universe.System_Int64,
				Signature.ELEMENT_TYPE_U8 => universe.System_UInt64,
				Signature.ELEMENT_TYPE_R4 => universe.System_Single,
				Signature.ELEMENT_TYPE_R8 => universe.System_Double,
				Signature.ELEMENT_TYPE_STRING => universe.System_String,
				Signature.ELEMENT_TYPE_SZARRAY => ReadFieldOrPropType(context, br).MakeArrayType(),
				0x55 => ReadType(context, br),
				0x50 => universe.System_Type,
				0x51 => universe.System_Object,
				_ => throw new BadImageFormatException()
			};
		}

		private static CustomAttributeTypedArgument ReadFixedArg(Module context, ByteReader br, Type type)
		{
			var universe = context.universe;
			if (type == universe.System_String)
			{
				return new CustomAttributeTypedArgument(type, br.ReadString());
			}
			if (type == universe.System_Boolean)
			{
				return new CustomAttributeTypedArgument(type, br.ReadByte() != 0);
			}
			if (type == universe.System_Char)
			{
				return new CustomAttributeTypedArgument(type, br.ReadChar());
			}
			if (type == universe.System_Single)
			{
				return new CustomAttributeTypedArgument(type, br.ReadSingle());
			}
			if (type == universe.System_Double)
			{
				return new CustomAttributeTypedArgument(type, br.ReadDouble());
			}
			if (type == universe.System_SByte)
			{
				return new CustomAttributeTypedArgument(type, br.ReadSByte());
			}
			if (type == universe.System_Int16)
			{
				return new CustomAttributeTypedArgument(type, br.ReadInt16());
			}
			if (type == universe.System_Int32)
			{
				return new CustomAttributeTypedArgument(type, br.ReadInt32());
			}
			if (type == universe.System_Int64)
			{
				return new CustomAttributeTypedArgument(type, br.ReadInt64());
			}
			if (type == universe.System_Byte)
			{
				return new CustomAttributeTypedArgument(type, br.ReadByte());
			}
			if (type == universe.System_UInt16)
			{
				return new CustomAttributeTypedArgument(type, br.ReadUInt16());
			}
			if (type == universe.System_UInt32)
			{
				return new CustomAttributeTypedArgument(type, br.ReadUInt32());
			}
			if (type == universe.System_UInt64)
			{
				return new CustomAttributeTypedArgument(type, br.ReadUInt64());
			}
			if (type == universe.System_Type)
			{
				return new CustomAttributeTypedArgument(type, ReadType(context, br));
			}
			if (type == universe.System_Object)
			{
				return ReadFixedArg(context, br, ReadFieldOrPropType(context, br));
			}
			if (type.IsArray)
			{
				var length = br.ReadInt32();
				if (length == -1)
				{
					return new CustomAttributeTypedArgument(type, null);
				}
				var elementType = type.GetElementType();
				var attributeTypedArguments = new CustomAttributeTypedArgument[length];
				for (var i = 0; i < length; i++)
				{
					attributeTypedArguments[i] = ReadFixedArg(context, br, elementType);
				}
				return new CustomAttributeTypedArgument(type, attributeTypedArguments);
			}
			if (type.IsEnum)
			{
				return new CustomAttributeTypedArgument(type, 
														ReadFixedArg(context, 
																		br, 
																		type.GetEnumUnderlyingTypeImpl()).Value);
			}
			
			throw new InvalidOperationException();
			
		}

		private static Type ReadType(Module context, ByteReader br)
		{
			var typeName = br.ReadString();
			if (typeName == null)
			{
				return null;
			}
			if (typeName.Length > 0 && typeName[typeName.Length - 1] == 0)
			{
				// there are broken compilers that emit an extra NUL character after the type name
				typeName = typeName.Substring(0, typeName.Length - 1);
			}
			return TypeNameParser.Parse(typeName, true)
								.GetType(context.universe, 
											context, 
											true, 
											typeName, 
											true, 
											false);
		}

		private static IList<CustomAttributeTypedArgument> ReadConstructorArguments(Module context, 
																					ByteReader br, 
																					ConstructorInfo constructor)
		{
			var methodSignature = constructor.MethodSignature;
			var count = methodSignature.GetParameterCount();
			var attributeTypedArguments = new List<CustomAttributeTypedArgument>(count);
			for (var i = 0; i < count; i++)
			{
				attributeTypedArguments.Add(ReadFixedArg(context, br, methodSignature.GetParameterType(i)));
			}
			return attributeTypedArguments.AsReadOnly();
		}

		private static IList<CustomAttributeNamedArgument> ReadNamedArguments(Module context, 
																				ByteReader br, 
																				int named, 
																				Type type, 
																				bool required)
		{
			var attributeNamedArguments = new List<CustomAttributeNamedArgument>(named);
			for (var i = 0; i < named; i++)
			{
				var fieldOrProperty = br.ReadByte();
				var fieldOrPropertyType = ReadFieldOrPropType(context, br);
				if (fieldOrPropertyType.__IsMissing && !required)
				{
					return null;
				}
				var name = br.ReadString();
				var customAttributeTypedArgument = ReadFixedArg(context, br, fieldOrPropertyType);
				MemberInfo member = fieldOrProperty switch
				{
					0x53 => GetField(context, type, name, fieldOrPropertyType),
					0x54 => GetProperty(context, type, name, fieldOrPropertyType),
					_ => throw new BadImageFormatException()
				};
				attributeNamedArguments.Add(new CustomAttributeNamedArgument(member, customAttributeTypedArgument));
			}
			return attributeNamedArguments.AsReadOnly();
		}

		private static FieldInfo GetField(Module context, Type type, string name, Type fieldType)
		{
			var org = type;
			for (; type != null && !type.__IsMissing; type = type.BaseType)
			{
				foreach (var fieldInfo in type.__GetDeclaredFields())
				{
					if (fieldInfo.IsPublic && !fieldInfo.IsStatic && fieldInfo.Name == name)
					{
						return fieldInfo;
					}
				}
			}
			// if the field is missing, we stick the missing field on the first missing base type
			if (type == null)
			{
				type = org;
			}
			var fieldSignature = FieldSignature.Create(fieldType, new CustomModifiers());
			return type.FindField(name, fieldSignature)
				?? type.Module.universe.GetMissingFieldOrThrow(context, type, name, fieldSignature);
		}

		private static PropertyInfo GetProperty(Module context, Type type, string name, Type propertyType)
		{
			var org = type;
			for (; type != null && !type.__IsMissing; type = type.BaseType)
			{
				foreach (var propertyInfo in type.__GetDeclaredProperties())
				{
					if (propertyInfo.IsPublic && !propertyInfo.IsStatic && propertyInfo.Name == name)
					{
						return propertyInfo;
					}
				}
			}
			// if the property is missing, we stick the missing property on the first missing base type
			if (type == null)
			{
				type = org;
			}
			return type.Module.universe.GetMissingPropertyOrThrow(context, type, name,
				PropertySignature.Create(CallingConventions.Standard | CallingConventions.HasThis, 
										propertyType, 
										null, 
										new PackedCustomModifiers()));
		}

#if !NETSTANDARD
		[Obsolete("Use AttributeType property instead.")]
		internal bool __TryReadTypeName(out string ns, out string name)
		{
			if (Constructor.DeclaringType.IsNested)
			{
				ns = null;
				name = null;
				return false;
			}
			var typeName = AttributeType.TypeName;
			ns = typeName.Namespace;
			name = typeName.Name;
			return true;
		}
#endif

		public byte[] __GetBlob()
		{
			if (declSecurityBlob != null)
			{
				return (byte[])declSecurityBlob.Clone();
			} 
			if (customAttributeIndex == -1)
			{
				return __ToBuilder().GetBlob(module.Assembly);
			}
			
			return ((ModuleReader)module).GetBlobCopy(module.CustomAttribute.records[customAttributeIndex].Value);
			
		}

		public int __Parent =>
			customAttributeIndex >= 0
				? module.CustomAttribute.records[customAttributeIndex].Parent
				: declSecurityIndex >= 0
					? module.DeclSecurity.records[declSecurityIndex].Parent
					: 0;

		// .NET 4.5 API
		public Type AttributeType => Constructor.DeclaringType;

		public ConstructorInfo Constructor
		{
			get
			{
				if (lazyConstructor == null)
				{
					lazyConstructor 
						= (ConstructorInfo)module.ResolveMethod(module.CustomAttribute.records[customAttributeIndex].Type);
				}
				return lazyConstructor;
			}
		}

		public IList<CustomAttributeTypedArgument> ConstructorArguments
		{
			get
			{
				if (lazyConstructorArguments == null)
				{
					LazyParseArguments(false);
				}
				return lazyConstructorArguments;
			}
		}

		public IList<CustomAttributeNamedArgument> NamedArguments
		{
			get
			{
				if (lazyNamedArguments == null)
				{
					if (customAttributeIndex >= 0)
					{
						// 1) Unresolved Custom Attribute
						LazyParseArguments(true);
					}
					else
					{
						// 5) Unresolved declarative security
						var byteReader = new ByteReader(declSecurityBlob, 0, declSecurityBlob.Length);
						// LAMESPEC the count of named arguments is a compressed integer (instead of UInt16 as NumNamed
						// in custom attributes)
						lazyNamedArguments 
							= ReadNamedArguments(module, 
												byteReader, 
												byteReader.ReadCompressedUInt(), 
												Constructor.DeclaringType, 
												true);
					}
				}
				return lazyNamedArguments;
			}
		}

		private void LazyParseArguments(bool requireNameArguments)
		{
			var byteReader = module.GetBlob(module.CustomAttribute.records[customAttributeIndex].Value);
			if (byteReader.Length == 0)
			{
				// it's legal to have an empty blob
				lazyConstructorArguments = Empty<CustomAttributeTypedArgument>.Array;
				lazyNamedArguments = Empty<CustomAttributeNamedArgument>.Array;
			}
			else
			{
				if (byteReader.ReadUInt16() != 1)
				{
					throw new BadImageFormatException();
				}
				lazyConstructorArguments = ReadConstructorArguments(module, byteReader, Constructor);
				lazyNamedArguments = ReadNamedArguments(module, 
														byteReader, 
														byteReader.ReadUInt16(), 
														Constructor.DeclaringType, 
														requireNameArguments);
			}
		}

		public CustomAttributeBuilder __ToBuilder()
		{
			var parameterInfos = Constructor.GetParameters();
			var args = new object[ConstructorArguments.Count];
			for (var i = 0; i < args.Length; i++)
			{
				args[i] = RewrapArray(parameterInfos[i].ParameterType, ConstructorArguments[i]);
			}
			var namedProperties = new List<PropertyInfo>();
			var propertyValues = new List<object>();
			var namedFields = new List<FieldInfo>();
			var fieldValues = new List<object>();
			foreach (var namedArgument in NamedArguments)
			{
				var propertyInfo = namedArgument.MemberInfo as PropertyInfo;
				if (propertyInfo != null)
				{
					namedProperties.Add(propertyInfo);
					propertyValues.Add(RewrapArray(propertyInfo.PropertyType, namedArgument.TypedValue));
				}
				else
				{
					var fieldInfo = (FieldInfo)namedArgument.MemberInfo;
					namedFields.Add(fieldInfo);
					fieldValues.Add(RewrapArray(fieldInfo.FieldType, namedArgument.TypedValue));
				}
			}
			return new CustomAttributeBuilder(Constructor, 
												args, 
												namedProperties.ToArray(), 
												propertyValues.ToArray(), 
												namedFields.ToArray(), 
												fieldValues.ToArray());
		}

		private static object RewrapArray(Type type, CustomAttributeTypedArgument arg)
		{
			if (arg.Value is IList<CustomAttributeTypedArgument> attributeTypedArguments)
			{
				var elementType = arg.ArgumentType.GetElementType();
				var arr = new object[attributeTypedArguments.Count];
				for (var i = 0; i < arr.Length; i++)
				{
					arr[i] = RewrapArray(elementType, attributeTypedArguments[i]);
				}
				if (type == type.Module.universe.System_Object)
				{
					return CustomAttributeBuilder.__MakeTypedArgument(arg.ArgumentType, arr);
				}
				return arr;
			}
			else
			{
				return arg.Value;
			}
		}

		public static IList<CustomAttributeData> GetCustomAttributes(MemberInfo member)
		{
			return __GetCustomAttributes(member, null, false);
		}

		public static IList<CustomAttributeData> GetCustomAttributes(Assembly assembly)
		{
			return assembly.GetCustomAttributesData(null);
		}

		public static IList<CustomAttributeData> GetCustomAttributes(Module module)
		{
			return __GetCustomAttributes(module, null, false);
		}

		public static IList<CustomAttributeData> GetCustomAttributes(ParameterInfo parameter)
		{
			return __GetCustomAttributes(parameter, null, false);
		}

		public static IList<CustomAttributeData> __GetCustomAttributes(Assembly assembly, 
																		Type attributeType, 
																		bool inherit)
		{
			return assembly.GetCustomAttributesData(attributeType);
		}

		public static IList<CustomAttributeData> __GetCustomAttributes(Module module, 
																		Type attributeType, 
																		bool inherit)
		{
			if (module.__IsMissing)
			{
				throw new MissingModuleException((MissingModule)module);
			}
			return GetCustomAttributesImpl(null, module, 0x00000001, attributeType) ?? EmptyList;
		}

		public static IList<CustomAttributeData> __GetCustomAttributes(ParameterInfo parameter, 
																		Type attributeType, 
																		bool inherit)
		{
			var module = parameter.Module;
			var customAttributeData = new List<CustomAttributeData>(); // TODO set this rather than check in loop
			if (module.universe.ReturnPseudoCustomAttributes)
			{
				if (attributeType == null || attributeType.IsAssignableFrom(parameter.Module.universe.System_Runtime_InteropServices_MarshalAsAttribute))
				{
					FieldMarshal spec;
					if (parameter.__TryGetFieldMarshal(out spec))
					{
						customAttributeData.Add(CustomAttributeData.CreateMarshalAsPseudoCustomAttribute(parameter.Module, spec));
					}
				}
			}
			var moduleBuilder = module as ModuleBuilder;
			var token = parameter.MetadataToken;
			if (moduleBuilder != null && moduleBuilder.IsSaved && ModuleBuilder.IsPseudoToken(token))
			{
				token = moduleBuilder.ResolvePseudoToken(token);
			}
			return GetCustomAttributesImpl(customAttributeData, module, token, attributeType) ?? EmptyList;
		}

		public static IList<CustomAttributeData> __GetCustomAttributes(MemberInfo member, Type attributeType, bool inherit)
		{
			if (!member.IsBaked)
			{
				// like .NET we we don't return custom attributes for unbaked members
				throw new NotImplementedException();
			}
			if (!inherit || !IsInheritableAttribute(attributeType))
			{
				return GetCustomAttributesImpl(null, member, attributeType) ?? EmptyList;
			}
			var customAttributes = new List<CustomAttributeData>();
			for (; ; )
			{
				GetCustomAttributesImpl(customAttributes, member, attributeType);
				var type = member as Type;
				if (type != null)
				{
					type = type.BaseType;
					if (type == null)
					{
						return customAttributes;
					}
					member = type;
					continue;
				}
				var methodInfo = member as MethodInfo;
				if (methodInfo != null)
				{
					var prevMemberInfo = member;
					methodInfo = methodInfo.GetBaseDefinition();
					if (methodInfo == null || methodInfo == prevMemberInfo)
					{
						return customAttributes;
					}
					member = methodInfo;
					continue;
				}
				return customAttributes;
			}
		}

		private static List<CustomAttributeData> GetCustomAttributesImpl(List<CustomAttributeData> list, MemberInfo member, Type attributeType)
		{
			if (member.Module.universe.ReturnPseudoCustomAttributes)
			{
				var pseudoCustomAttributes = member.GetPseudoCustomAttributes(attributeType);
				if (list == null)
				{
					list = pseudoCustomAttributes;
				}
				else if (pseudoCustomAttributes != null)
				{
					list.AddRange(pseudoCustomAttributes);
				}
			}
			return GetCustomAttributesImpl(list, member.Module, member.GetCurrentToken(), attributeType);
		}

		internal static List<CustomAttributeData> GetCustomAttributesImpl(List<CustomAttributeData> list, Module module, int token, Type attributeType)
		{
			foreach (var i in module.CustomAttribute.Filter(token))
			{
				if (attributeType == null)
				{
					list ??= new List<CustomAttributeData>();
					list.Add(new CustomAttributeData(module, i));
				}
				else
				{
					if (attributeType.IsAssignableFrom(module.ResolveMethod(module.CustomAttribute.records[i].Type).DeclaringType))
					{
						list ??= new List<CustomAttributeData>();
						list.Add(new CustomAttributeData(module, i));
					}
				}
			}
			return list;
		}

		public static IList<CustomAttributeData> __GetCustomAttributes(Type type, 
																		Type interfaceType, 
																		Type attributeType, 
																		bool inherit)
		{
			var module = type.Module;
			foreach (var i in module.InterfaceImpl.Filter(type.MetadataToken))
			{
				if (module.ResolveType(module.InterfaceImpl.records[i].Interface, type) == interfaceType)
				{
					return GetCustomAttributesImpl(null, 
													module, 
													(InterfaceImplTable.Index << 24) | (i + 1), attributeType) ?? EmptyList;
				}
			}
			return EmptyList;
		}

		public static IList<CustomAttributeData> __GetDeclarativeSecurity(Assembly assembly)
		{
			if (assembly.__IsMissing)
			{
				throw new MissingAssemblyException((MissingAssembly)assembly);
			}
			return assembly.ManifestModule.GetDeclarativeSecurity(0x20000001);
		}

		public static IList<CustomAttributeData> __GetDeclarativeSecurity(Type type)
		{
			return (type.Attributes & TypeAttributes.HasSecurity) != 0 
				? type.Module.GetDeclarativeSecurity(type.MetadataToken) : EmptyList;
		}

		public static IList<CustomAttributeData> __GetDeclarativeSecurity(MethodBase method)
		{
			return (method.Attributes & MethodAttributes.HasSecurity) != 0 
				? method.Module.GetDeclarativeSecurity(method.MetadataToken) : EmptyList;
		}

		private static bool IsInheritableAttribute(Type attribute)
		{
			var attributeUsageAttribute = attribute.Module.universe.System_AttributeUsageAttribute;
			var customAttributes = __GetCustomAttributes(attribute, attributeUsageAttribute, false);
			if (customAttributes.Count != 0)
			{
				foreach (var namedArgument in customAttributes[0].NamedArguments)
				{
					if (namedArgument.MemberInfo.Name == "Inherited")
					{
						return (bool)namedArgument.TypedValue.Value;
					}
				}
			}
			return true;
		}

		internal static CustomAttributeData CreateDllImportPseudoCustomAttribute(Module module, ImplMapFlags flags, string entryPoint, string dllName, MethodImplAttributes attr)
		{
			var type = module.universe.System_Runtime_InteropServices_DllImportAttribute;
			var constructorInfo = type.GetPseudoCustomAttributeConstructor(module.universe.System_String);
			var attributeNamedArguments = new List<CustomAttributeNamedArgument>();
			System.Runtime.InteropServices.CharSet charSet = System.Runtime.InteropServices.CharSet.None;
			switch (flags & ImplMapFlags.CharSetMask)
			{
				case ImplMapFlags.CharSetAnsi:
					charSet = System.Runtime.InteropServices.CharSet.Ansi;
					break;
				case ImplMapFlags.CharSetUnicode:
					charSet = System.Runtime.InteropServices.CharSet.Unicode;
					break;
				case ImplMapFlags.CharSetAuto:
#if NETSTANDARD
					charSet = (System.Runtime.InteropServices.CharSet)4;
#else
					charSet = System.Runtime.InteropServices.CharSet.Auto;
#endif
					break;
				case ImplMapFlags.NoMangle:
					break;
				case ImplMapFlags.SupportsLastError:
					break;
				case ImplMapFlags.CallConvMask:
					break;
				case ImplMapFlags.CallConvWinapi:
					break;
				case ImplMapFlags.CallConvCdecl:
					break;
				case ImplMapFlags.CallConvStdcall:
					break;
				case ImplMapFlags.CallConvThiscall:
					break;
				case ImplMapFlags.CallConvFastcall:
					break;
				case ImplMapFlags.BestFitOn:
					break;
				case ImplMapFlags.BestFitOff:
					break;
				case ImplMapFlags.CharMapErrorOn:
					break;
				case ImplMapFlags.CharMapErrorOff:
					break;
				case ImplMapFlags.CharSetNotSpec:
				default:
#if NETSTANDARD
					charSet = (System.Runtime.InteropServices.CharSet)1;
#else
					charSet = System.Runtime.InteropServices.CharSet.None;
#endif
					break;
			}
			System.Runtime.InteropServices.CallingConvention callingConvention;
			switch (flags & ImplMapFlags.CallConvMask)
			{
				case ImplMapFlags.CallConvCdecl:
					callingConvention = System.Runtime.InteropServices.CallingConvention.Cdecl;
					break;
				case ImplMapFlags.CallConvFastcall:
#if NETSTANDARD
					callingConvention = (System.Runtime.InteropServices.CallingConvention)5;
#else
					callingConvention = System.Runtime.InteropServices.CallingConvention.FastCall;
#endif
					break;
				case ImplMapFlags.CallConvStdcall:
					callingConvention = System.Runtime.InteropServices.CallingConvention.StdCall;
					break;
				case ImplMapFlags.CallConvThiscall:
					callingConvention = System.Runtime.InteropServices.CallingConvention.ThisCall;
					break;
				case ImplMapFlags.CallConvWinapi:
					callingConvention = System.Runtime.InteropServices.CallingConvention.Winapi;
					break;
				default:
					callingConvention = 0;
					break;
			}
			AddNamedArgument(attributeNamedArguments, type, "EntryPoint", entryPoint);
			
			AddNamedArgument(attributeNamedArguments, 
					type, 
					"CharSet", 
					module.universe.System_Runtime_InteropServices_CharSet, 
					(int)charSet);
			
			AddNamedArgument(attributeNamedArguments, 
								type, 
								"ExactSpelling", 
								(int)flags, 
								(int)ImplMapFlags.NoMangle);
			
			AddNamedArgument(attributeNamedArguments, 
								type, 
								"SetLastError", 
								(int)flags, 
								(int)ImplMapFlags.SupportsLastError);
			
			AddNamedArgument(attributeNamedArguments, 
								type, 
								"PreserveSig", 
								(int)attr, 
								(int)MethodImplAttributes.PreserveSig);
			
			AddNamedArgument(attributeNamedArguments, 
						type, 
						"CallingConvention", 
						module.universe.System_Runtime_InteropServices_CallingConvention, 
						(int)callingConvention);
			
			AddNamedArgument(attributeNamedArguments, 
								type, 
								"BestFitMapping", 
								(int)flags, 
								(int)ImplMapFlags.BestFitOn);
			AddNamedArgument(attributeNamedArguments, 
								type, 
								"ThrowOnUnmappableChar", 
								(int)flags, 
								(int)ImplMapFlags.CharMapErrorOn);
			
			return new CustomAttributeData(module, 
											constructorInfo, 
											new object[] { dllName }, 
											attributeNamedArguments);
		}

		internal static CustomAttributeData CreateMarshalAsPseudoCustomAttribute(Module module, FieldMarshal fm)
		{
			var typeofMarshalAs = module.universe.System_Runtime_InteropServices_MarshalAsAttribute;
			var typeofUnmanagedType = module.universe.System_Runtime_InteropServices_UnmanagedType;
			var typeofVarEnum = module.universe.System_Runtime_InteropServices_VarEnum;
			var typeofType = module.universe.System_Type;
			var attributeNamedArguments = new List<CustomAttributeNamedArgument>();
			AddNamedArgument(attributeNamedArguments, 
					typeofMarshalAs, 
					"ArraySubType", 
					typeofUnmanagedType, 
					(int)(fm.ArraySubType ?? 0));
			AddNamedArgument(attributeNamedArguments, 
					typeofMarshalAs, 
					"SizeParamIndex", 
					module.universe.System_Int16, 
					fm.SizeParamIndex ?? 0);
			AddNamedArgument(attributeNamedArguments, 
					typeofMarshalAs, 
					"SizeConst", module.universe.System_Int32, fm.SizeConst ?? 0);
			AddNamedArgument(attributeNamedArguments, 
					typeofMarshalAs, 
					"IidParameterIndex", 
					module.universe.System_Int32, 
					fm.IidParameterIndex ?? 0);
			AddNamedArgument(attributeNamedArguments, 
					typeofMarshalAs, 
					"SafeArraySubType", 
					typeofVarEnum, 
					(int)(fm.SafeArraySubType ?? 0));
			
			if (fm.SafeArrayUserDefinedSubType != null)
			{
				AddNamedArgument(attributeNamedArguments, 
						typeofMarshalAs, 
						"SafeArrayUserDefinedSubType", 
						typeofType, 
						fm.SafeArrayUserDefinedSubType);
			}
			
			if (fm.MarshalType != null)
			{
				AddNamedArgument(attributeNamedArguments, 
						typeofMarshalAs, 
						"MarshalType", 
						module.universe.System_String, 
						fm.MarshalType);
			}
			
			if (fm.MarshalTypeRef != null)
			{
				AddNamedArgument(attributeNamedArguments, 
						typeofMarshalAs, 
						"MarshalTypeRef", 
						module.universe.System_Type, 
						fm.MarshalTypeRef);
			}
			if (fm.MarshalCookie != null)
			{
				AddNamedArgument(attributeNamedArguments, 
						typeofMarshalAs, 
						"MarshalCookie", 
						module.universe.System_String, 
						fm.MarshalCookie);
			}
			var constructorInfo = typeofMarshalAs.GetPseudoCustomAttributeConstructor(typeofUnmanagedType);
			return new CustomAttributeData(module, 
											constructorInfo, 
											new object[] { (int)fm.UnmanagedType }, attributeNamedArguments);
		}

		private static void AddNamedArgument(List<CustomAttributeNamedArgument> list, 
												Type type, 
												string fieldName, 
												string value)
		{
			AddNamedArgument(list, type, fieldName, type.Module.universe.System_String, value);
		}

		private static void AddNamedArgument(List<CustomAttributeNamedArgument> list, 
												Type type, 
												string fieldName, 
												int flags, 
												int flagMask)
		{
			AddNamedArgument(list, 
					type, 
								fieldName, 
					type.Module.universe.System_Boolean, 
					(flags & flagMask) != 0);
		}

		private static void AddNamedArgument(List<CustomAttributeNamedArgument> list, 
												Type attributeType, 
												string fieldName, 
												Type valueType, 
												object value)
		{
			// some fields are not available on the .NET Compact Framework version of
			// DllImportAttribute/MarshalAsAttribute
			var fieldInfo = attributeType.FindField(fieldName, FieldSignature.Create(valueType, new CustomModifiers()));
			if (fieldInfo != null)
			{
				list.Add(
					new CustomAttributeNamedArgument(fieldInfo, 
						new CustomAttributeTypedArgument(valueType, value)));
			}
		}

		internal static CustomAttributeData CreateFieldOffsetPseudoCustomAttribute(Module module, int offset)
		{
			var type = module.universe.System_Runtime_InteropServices_FieldOffsetAttribute;
			var constructorInfo = type.GetPseudoCustomAttributeConstructor(module.universe.System_Int32);
			return new CustomAttributeData(module, constructorInfo, new object[] { offset }, null);
		}

		internal static CustomAttributeData CreatePreserveSigPseudoCustomAttribute(Module module)
		{
			var type = module.universe.System_Runtime_InteropServices_PreserveSigAttribute;
			var constructorInfo = type.GetPseudoCustomAttributeConstructor();
			return new CustomAttributeData(module, constructorInfo, Empty<object>.Array, null);
		}
	}
}
