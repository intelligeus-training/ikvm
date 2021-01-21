/*
  Copyright (C) 2011-2012 Jeroen Frijters

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

namespace IKVM.Reflection
{
#if !NETSTANDARD
	[Serializable]
#endif
	public sealed class MissingAssemblyException : InvalidOperationException
	{
#if !NETSTANDARD
		[NonSerialized]
#endif
		private readonly MissingAssembly assembly;

		public MissingAssemblyException(MissingAssembly assembly)
			: base("Assembly '" + assembly.FullName +
			       "' is a missing assembly and does not support the requested operation.")
		{
			this.assembly = assembly;
		}

#if !NETSTANDARD
		private MissingAssemblyException(System.Runtime.Serialization.SerializationInfo info, 
											System.Runtime.Serialization.StreamingContext context)
			: base(info, context)
		{
		}
#endif

		public Assembly Assembly => assembly;
	}

#if !NETSTANDARD
	[Serializable]
#endif
	public sealed class MissingModuleException : InvalidOperationException
	{
#if !NETSTANDARD
		[NonSerialized]
#endif
		private readonly MissingModule module;

		public MissingModuleException(MissingModule module)
			: base("Module from missing assembly '" + module.Assembly.FullName + 
			       "' does not support the requested operation.")
		{
			this.module = module;
		}

#if !NETSTANDARD
		private MissingModuleException(System.Runtime.Serialization.SerializationInfo info, 
										System.Runtime.Serialization.StreamingContext context)
			: base(info, context)
		{
		}
#endif

		public Module Module => module;
	}

#if !NETSTANDARD
	[Serializable]
#endif
	public sealed class MissingMemberException : InvalidOperationException
	{
#if !NETSTANDARD
		[NonSerialized]
#endif
		private readonly MemberInfo member;

		public MissingMemberException(MemberInfo member)
			: base("Member '" + member + "' is a missing member and does not support the requested operation.")
		{
			this.member = member;
		}

#if !NETSTANDARD
		private MissingMemberException(System.Runtime.Serialization.SerializationInfo info, 
										System.Runtime.Serialization.StreamingContext context)
			: base(info, context)
		{
		}
#endif

		public MemberInfo MemberInfo => member;
	}

	public struct MissingGenericMethodBuilder
	{
		private readonly MissingMethod method;

		public MissingGenericMethodBuilder(Type declaringType, 
											CallingConventions callingConvention, 
											string name, 
											int genericParameterCount)
		{
			method = new MissingMethod(declaringType, 
										name, 
										new MethodSignature(null, 
											null, 
											new PackedCustomModifiers(), 
											callingConvention, 
											genericParameterCount));
		}

		public Type[] GetGenericArguments()
		{
			return method.GetGenericArguments();
		}

		public void SetSignature(Type returnType, CustomModifiers returnTypeCustomModifiers, Type[] parameterTypes, CustomModifiers[] parameterTypeCustomModifiers)
		{
			method.signature = new MethodSignature(
				returnType ?? method.Module.universe.System_Void,
				Util.Copy(parameterTypes),
				PackedCustomModifiers.CreateFromExternal(returnTypeCustomModifiers, parameterTypeCustomModifiers, parameterTypes.Length),
				method.signature.CallingConvention,
				method.signature.GenericParameterCount);
		}

#if !NETSTANDARD
		[Obsolete("Please use SetSignature(Type, CustomModifiers, Type[], CustomModifiers[]) instead.")]
		public void SetSignature(Type returnType, 
									Type[] returnTypeRequiredCustomModifiers, 
									Type[] returnTypeOptionalCustomModifiers, 
									Type[] parameterTypes, 
									Type[][] parameterTypeRequiredCustomModifiers, 
									Type[][] parameterTypeOptionalCustomModifiers)
		{
			method.signature = new MethodSignature(
				returnType ?? method.Module.universe.System_Void,
				Util.Copy(parameterTypes),
				PackedCustomModifiers.CreateFromExternal(returnTypeOptionalCustomModifiers, returnTypeRequiredCustomModifiers, parameterTypeOptionalCustomModifiers, parameterTypeRequiredCustomModifiers, parameterTypes.Length),
				method.signature.CallingConvention,
				method.signature.GenericParameterCount);
		}
#endif

		public MethodInfo Finish()
		{
			return method;
		}
	}

	public sealed class MissingAssembly : Assembly
	{
		private readonly MissingModule module;

		public MissingAssembly(Universe universe, string name)
			: base(universe)
		{
			module = new MissingModule(this, -1);
			fullName = name;
		}

		public override Type[] GetTypes()
		{
			throw new MissingAssemblyException(this);
		}

		public override AssemblyName GetName()
		{
			return new AssemblyName(fullName);
		}

		public override string ImageRuntimeVersion
		{
			get { throw new MissingAssemblyException(this); }
		}

		public override Module ManifestModule
		{
			get { return module; }
		}

		public override MethodInfo EntryPoint
		{
			get { throw new MissingAssemblyException(this); }
		}

		public override string Location
		{
			get { throw new MissingAssemblyException(this); }
		}

		public override AssemblyName[] GetReferencedAssemblies()
		{
			throw new MissingAssemblyException(this);
		}

		public override Module[] GetModules(bool getResourceModules)
		{
			throw new MissingAssemblyException(this);
		}

		public override Module[] GetLoadedModules(bool getResourceModules)
		{
			throw new MissingAssemblyException(this);
		}

		public override Module GetModule(string name)
		{
			throw new MissingAssemblyException(this);
		}

		public override string[] GetManifestResourceNames()
		{
			throw new MissingAssemblyException(this);
		}

		public override ManifestResourceInfo GetManifestResourceInfo(string resourceName)
		{
			throw new MissingAssemblyException(this);
		}

		public override System.IO.Stream GetManifestResourceStream(string resourceName)
		{
			throw new MissingAssemblyException(this);
		}

		public override bool __IsMissing
		{
			get { return true; }
		}

		public override Type FindType(TypeName typeName)
		{
			return null;
		}

		public override Type FindTypeIgnoreCase(TypeName lowerCaseName)
		{
			return null;
		}

		public override IList<CustomAttributeData> GetCustomAttributesData(Type attributeType)
		{
			throw new MissingAssemblyException(this);
		}
	}

	public sealed class MissingModule : NonPEModule
	{
		private readonly Assembly assembly;
		private readonly int index;

		public MissingModule(Assembly assembly, int index)
			: base(assembly.universe)
		{
			this.assembly = assembly;
			this.index = index;
		}

		public override int MDStreamVersion => throw new MissingModuleException(this);

		public override Assembly Assembly => assembly;

		public override string FullyQualifiedName => throw new MissingModuleException(this);

		public override string Name
		{
			get
			{
				if (index == -1)
				{
					throw new MissingModuleException(this);
				}
				return assembly.ManifestModule.GetString(assembly.ManifestModule.File.records[index].Name);
			}
		}

		public override Guid ModuleVersionId
		{
			get { throw new MissingModuleException(this); }
		}

		public override string ScopeName => throw new MissingModuleException(this);

		public override Type FindType(TypeName typeName)
		{
			return null;
		}

		public override Type FindTypeIgnoreCase(TypeName lowerCaseName)
		{
			return null;
		}

		public override void GetTypesImpl(System.Collections.Generic.List<Type> list)
		{
			throw new MissingModuleException(this);
		}

		public override void __GetDataDirectoryEntry(int index, out int rva, out int length)
		{
			throw new MissingModuleException(this);
		}

		public override IList<CustomAttributeData> __GetPlaceholderAssemblyCustomAttributes(bool multiple, 
																							bool security)
		{
			throw new MissingModuleException(this);
		}

		public override long __RelativeVirtualAddressToFileOffset(int rva)
		{
			throw new MissingModuleException(this);
		}

		public override __StandAloneMethodSig __ResolveStandAloneMethodSig(int metadataToken, 
																			Type[] genericTypeArguments, 
																			Type[] genericMethodArguments)
		{
			throw new MissingModuleException(this);
		}

		public override int __Subsystem => throw new MissingModuleException(this);

		public override void ExportTypes(int fileToken, IKVM.Reflection.Emit.ModuleBuilder manifestModule)
		{
			throw new MissingModuleException(this);
		}

		public override void GetPEKind(out PortableExecutableKinds peKind, out ImageFileMachine machine)
		{
			throw new MissingModuleException(this);
		}

		public override bool __IsMissing => true;

		protected override Exception InvalidOperationException()
		{
			return new MissingModuleException(this);
		}

		protected override Exception NotSupportedException()
		{
			return new MissingModuleException(this);
		}

		protected override Exception ArgumentOutOfRangeException()
		{
			return new MissingModuleException(this);
		}

		public override byte[] __ModuleHash
		{
			get
			{
				if (index == -1)
				{
					throw new MissingModuleException(this);
				}
				if (assembly.ManifestModule.File.records[index].HashValue == 0)
				{
					return null;
				}
				IKVM.Reflection.Reader.ByteReader br = assembly.ManifestModule.GetBlob(assembly.ManifestModule.File.records[index].HashValue);
				return br.ReadBytes(br.Length);
			}
		}
	}

	public sealed class MissingType : Type
	{
		private readonly Module module;
		private readonly Type declaringType;
		private readonly string ns;
		private readonly string name;
		private Type[] typeArgs;
		private int token;
		private int flags;
		private bool cyclicTypeForwarder;
		private bool cyclicTypeSpec;

		public MissingType(Module module, Type declaringType, string ns, string name)
		{
			this.module = module;
			this.declaringType = declaringType;
			this.ns = ns;
			this.name = name;
			MarkKnownType(ns, name);

			// HACK we need to handle the Windows Runtime projected types that change from ValueType to Class or v.v.
			if (WindowsRuntimeProjection.IsProjectedValueType(ns, name, module))
			{
				typeFlags |= TypeFlags.ValueType;
			}
			else if (WindowsRuntimeProjection.IsProjectedReferenceType(ns, name, module))
			{
				typeFlags |= TypeFlags.NotValueType;
			}
		}

		public override MethodBase FindMethod(string name, MethodSignature signature)
		{
			var missingMethod = new MissingMethod(this, name, signature);
			if (name == ".ctor")
			{
				return new ConstructorInfoImpl(missingMethod);
			}
			return missingMethod;
		}

		public override FieldInfo FindField(string name, FieldSignature signature)
		{
			return new MissingField(this, name, signature);
		}

		public override Type FindNestedType(TypeName name)
		{
			return null;
		}

		public override Type FindNestedTypeIgnoreCase(TypeName lowerCaseName)
		{
			return null;
		}

		public override bool __IsMissing => true;

		public override Type DeclaringType => declaringType;

		public override TypeName TypeName => new TypeName(ns, name);

		public override string Name => TypeNameParser.Escape(name);

		public override string FullName
		{
			get { return GetFullName(); }
		}

		public override Module Module => module;

		public override int MetadataToken => token;

		protected override bool IsValueTypeImpl
		{
			get
			{
				switch (typeFlags & (TypeFlags.ValueType | TypeFlags.NotValueType))
				{
					case TypeFlags.ValueType:
						return true;
					case TypeFlags.NotValueType:
						return false;
					case TypeFlags.ValueType | TypeFlags.NotValueType:
						if (WindowsRuntimeProjection.IsProjectedValueType(ns, name, module))
						{
							typeFlags &= ~TypeFlags.NotValueType;
							return true;
						}
						if (WindowsRuntimeProjection.IsProjectedReferenceType(ns, name, module))
						{
							typeFlags &= ~TypeFlags.ValueType;
							return false;
						}
						goto default;
					default:
						if (module.universe.ResolveMissingTypeIsValueType(this))
						{
							typeFlags |= TypeFlags.ValueType;
						}
						else
						{
							typeFlags |= TypeFlags.NotValueType;
						}
						return (typeFlags & TypeFlags.ValueType) != 0;
				}
			}
		}

		public override Type BaseType => throw new MissingMemberException(this);

		public override TypeAttributes Attributes => throw new MissingMemberException(this);

		public override Type[] __GetDeclaredTypes()
		{
			throw new MissingMemberException(this);
		}

		public override Type[] __GetDeclaredInterfaces()
		{
			throw new MissingMemberException(this);
		}

		public override MethodBase[] __GetDeclaredMethods()
		{
			throw new MissingMemberException(this);
		}

		public override __MethodImplMap __GetMethodImplMap()
		{
			throw new MissingMemberException(this);
		}

		public override FieldInfo[] __GetDeclaredFields()
		{
			throw new MissingMemberException(this);
		}

		public override EventInfo[] __GetDeclaredEvents()
		{
			throw new MissingMemberException(this);
		}

		public override PropertyInfo[] __GetDeclaredProperties()
		{
			throw new MissingMemberException(this);
		}

		public override CustomModifiers __GetCustomModifiers()
		{
			throw new MissingMemberException(this);
		}

		public override Type[] GetGenericArguments()
		{
			throw new MissingMemberException(this);
		}

		public override CustomModifiers[] __GetGenericArgumentsCustomModifiers()
		{
			throw new MissingMemberException(this);
		}

		public override bool __GetLayout(out int packingSize, out int typeSize)
		{
			throw new MissingMemberException(this);
		}

		public override bool IsGenericType => throw new MissingMemberException(this);

		public override bool IsGenericTypeDefinition => throw new MissingMemberException(this);

		public override Type GetGenericTypeArgument(int index)
		{
			if (typeArgs == null)
			{
				typeArgs = new Type[index + 1];
			}
			else if (typeArgs.Length <= index)
			{
				Array.Resize(ref typeArgs, index + 1);
			}
			return typeArgs[index] ?? (typeArgs[index] = new MissingTypeParameter(this, index));
		}

		public override Type BindTypeParameters(IGenericBinder binder)
		{
			return this;
		}

		public override Type SetMetadataTokenForMissing(int token, int flags)
		{
			this.token = token;
			this.flags = flags;
			return this;
		}

		public override Type SetCyclicTypeForwarder()
		{
			cyclicTypeForwarder = true;
			return this;
		}

		public override Type SetCyclicTypeSpec()
		{
			cyclicTypeSpec = true;
			return this;
		}

		public override bool IsBaked => throw new MissingMemberException(this);

		public override bool __IsTypeForwarder
		{
			// CorTypeAttr.tdForwarder
			get { return (flags & 0x00200000) != 0; }
		}

		public override bool __IsCyclicTypeForwarder => cyclicTypeForwarder;

		public override bool __IsCyclicTypeSpec => cyclicTypeSpec;
	}

	public sealed class MissingTypeParameter : IKVM.Reflection.Reader.TypeParameterType
	{
		private readonly MemberInfo owner;
		private readonly int index;

		public MissingTypeParameter(Type owner, int index)
			: this(owner, index, Signature.ELEMENT_TYPE_VAR)
		{
		}

		public MissingTypeParameter(MethodInfo owner, int index)
			: this(owner, index, Signature.ELEMENT_TYPE_MVAR)
		{
		}

		private MissingTypeParameter(MemberInfo owner, int index, byte sigElementType)
			: base(sigElementType)
		{
			this.owner = owner;
			this.index = index;
		}

		public override Module Module => owner.Module;

		public override string Name => null;

		public override int GenericParameterPosition
		{
			get { return index; }
		}

		public override MethodBase DeclaringMethod => owner as MethodBase;

		public override Type DeclaringType => owner as Type;

		public override Type BindTypeParameters(IGenericBinder binder)
		{
			if (owner is MethodBase)
			{
				return binder.BindMethodParameter(this);
			}
		
			return binder.BindTypeParameter(this);
			
		}

		public override bool IsBaked => owner.IsBaked;
	}

	public sealed class MissingMethod : MethodInfo
	{
		private readonly Type declaringType;
		private readonly string name;
		public MethodSignature signature;
		private MethodInfo forwarder;
		private Type[] typeArgs;

		public MissingMethod(Type declaringType, string name, MethodSignature signature)
		{
			this.declaringType = declaringType;
			this.name = name;
			this.signature = signature;
		}

		private MethodInfo Forwarder
		{
			get
			{
				var methodInfo = TryGetForwarder();
				if (methodInfo == null)
				{
					throw new MissingMemberException(this);
				}
				return methodInfo;
			}
		}

		private MethodInfo TryGetForwarder()
		{
			if (forwarder == null && !declaringType.__IsMissing)
			{
				var methodBase = declaringType.FindMethod(name, signature);
				var constructorInfo = methodBase as ConstructorInfo;
				if (constructorInfo != null)
				{
					forwarder = constructorInfo.GetMethodInfo();
				}
				else
				{
					forwarder = (MethodInfo)methodBase;
				}
			}
			return forwarder;
		}

		public override bool __IsMissing => TryGetForwarder() == null;

		public override Type ReturnType
		{
			get { return signature.GetReturnType(this); }
		}

		public override ParameterInfo ReturnParameter => new ParameterInfoImpl(this, -1);

		public override MethodSignature MethodSignature => signature;

		public override int ParameterCount => signature.GetParameterCount();

		private sealed class ParameterInfoImpl : ParameterInfo
		{
			private readonly MissingMethod method;
			private readonly int index;

			public ParameterInfoImpl(MissingMethod method, int index)
			{
				this.method = method;
				this.index = index;
			}

			private ParameterInfo Forwarder => index == -1 ? method.Forwarder.ReturnParameter : method.Forwarder.GetParameters()[index];

			public override string Name => Forwarder.Name;

			public override Type ParameterType => index == -1 ? method.signature.GetReturnType(method) : method.signature.GetParameterType(method, index);

			public override ParameterAttributes Attributes => Forwarder.Attributes;

			public override int Position => index;

			public override object RawDefaultValue => Forwarder.RawDefaultValue;

			public override CustomModifiers __GetCustomModifiers()
			{
				return index == -1
					? method.signature.GetReturnTypeCustomModifiers(method)
					: method.signature.GetParameterCustomModifiers(method, index);
			}

			public override bool __TryGetFieldMarshal(out FieldMarshal fieldMarshal)
			{
				return Forwarder.__TryGetFieldMarshal(out fieldMarshal);
			}

			public override MemberInfo Member => method;

			public override int MetadataToken => Forwarder.MetadataToken;

			public override Module Module => method.Module;

			public override string ToString()
			{
				return Forwarder.ToString();
			}
		}

		public override ParameterInfo[] GetParameters()
		{
			var parameterInfos = new ParameterInfo[signature.GetParameterCount()];
			for (var i = 0; i < parameterInfos.Length; i++)
			{
				parameterInfos[i] = new ParameterInfoImpl(this, i);
			}
			return parameterInfos;
		}

		public override MethodAttributes Attributes => Forwarder.Attributes;

		public override MethodImplAttributes GetMethodImplementationFlags()
		{
			return Forwarder.GetMethodImplementationFlags();
		}

		public override MethodBody GetMethodBody()
		{
			return Forwarder.GetMethodBody();
		}

		public override int __MethodRVA
		{
			get { return Forwarder.__MethodRVA; }
		}

		public override CallingConventions CallingConvention => signature.CallingConvention;

		public override int ImportTo(IKVM.Reflection.Emit.ModuleBuilder module)
		{
			var methodInfo = TryGetForwarder();
			if (methodInfo != null)
			{
				return methodInfo.ImportTo(module);
			}
			return module.ImportMethodOrField(declaringType, Name, MethodSignature);
		}

		public override string Name => name;

		public override Type DeclaringType => declaringType.IsModulePseudoType ? null : declaringType;

		public override Module Module => declaringType.Module;

		public override bool Equals(object obj)
		{
			var other = obj as MissingMethod;
			return other != null
				&& other.declaringType == declaringType
				&& other.name == name
				&& other.signature.Equals(signature);
		}

		public override int GetHashCode()
		{
			return declaringType.GetHashCode() ^ name.GetHashCode() ^ signature.GetHashCode();
		}

		public override MethodBase BindTypeParameters(Type type)
		{
			var forwarder = TryGetForwarder();
			if (forwarder != null)
			{
				return forwarder.BindTypeParameters(type);
			}
			return new GenericMethodInstance(type, this, null);
		}

		public override bool ContainsGenericParameters => Forwarder.ContainsGenericParameters;

		public override Type[] GetGenericArguments()
		{
			var method = TryGetForwarder();
			if (method != null)
			{
				return Forwarder.GetGenericArguments();
			}
			if (typeArgs == null)
			{
				typeArgs = new Type[signature.GenericParameterCount];
				for (var i = 0; i < typeArgs.Length; i++)
				{
					typeArgs[i] = new MissingTypeParameter(this, i);
				}
			}
			return Util.Copy(typeArgs);
		}

		public override Type GetGenericMethodArgument(int index)
		{
			return GetGenericArguments()[index];
		}

		public override int GetGenericMethodArgumentCount()
		{
			return Forwarder.GetGenericMethodArgumentCount();
		}

		public override MethodInfo GetGenericMethodDefinition()
		{
			return Forwarder.GetGenericMethodDefinition();
		}

		public override MethodInfo GetMethodOnTypeDefinition()
		{
			return Forwarder.GetMethodOnTypeDefinition();
		}

		public override bool HasThis => (signature.CallingConvention 
		                                   & (CallingConventions.HasThis 
		                                      | CallingConventions.ExplicitThis)) == CallingConventions.HasThis;

		public override bool IsGenericMethod => IsGenericMethodDefinition;

		public override bool IsGenericMethodDefinition => signature.GenericParameterCount != 0;

		public override MethodInfo MakeGenericMethod(params Type[] typeArguments)
		{
			MethodInfo method = TryGetForwarder();
			if (method != null)
			{
				return method.MakeGenericMethod(typeArguments);
			}
			return new GenericMethodInstance(declaringType, this, typeArguments);
		}

		public override int MetadataToken => Forwarder.MetadataToken;

		public override int GetCurrentToken()
		{
			return Forwarder.GetCurrentToken();
		}

		public override bool IsBaked => Forwarder.IsBaked;
	}

	public sealed class MissingField : FieldInfo
	{
		private readonly Type declaringType;
		private readonly string name;
		private readonly FieldSignature signature;
		private FieldInfo forwarder;

		public MissingField(Type declaringType, string name, FieldSignature signature)
		{
			this.declaringType = declaringType;
			this.name = name;
			this.signature = signature;
		}

		private FieldInfo Forwarder
		{
			get
			{
				var fieldInfo = TryGetForwarder();
				if (fieldInfo == null)
				{
					throw new MissingMemberException(this);
				}
				return fieldInfo;
			}
		}

		private FieldInfo TryGetForwarder()
		{
			if (forwarder == null && !declaringType.__IsMissing)
			{
				forwarder = declaringType.FindField(name, signature);
			}
			return forwarder;
		}

		public override bool __IsMissing => TryGetForwarder() == null;

		public override FieldAttributes Attributes => Forwarder.Attributes;

		public override void __GetDataFromRVA(byte[] data, int offset, int length)
		{
			Forwarder.__GetDataFromRVA(data, offset, length);
		}

		public override int __FieldRVA => Forwarder.__FieldRVA;

		public override bool __TryGetFieldOffset(out int offset)
		{
			return Forwarder.__TryGetFieldOffset(out offset);
		}

		public override object GetRawConstantValue()
		{
			return Forwarder.GetRawConstantValue();
		}

		public override FieldSignature FieldSignature => signature;

		public override int ImportTo(IKVM.Reflection.Emit.ModuleBuilder module)
		{
			var fieldInfo = TryGetForwarder();
			if (fieldInfo != null)
			{
				return fieldInfo.ImportTo(module);
			}
			return module.ImportMethodOrField(declaringType, this.Name, this.FieldSignature);
		}

		public override string Name => name;

		public override Type DeclaringType => declaringType.IsModulePseudoType ? null : declaringType;

		public override Module Module => declaringType.Module;

		public override FieldInfo BindTypeParameters(Type type)
		{
			var forwarder = TryGetForwarder();
			if (forwarder != null)
			{
				return forwarder.BindTypeParameters(type);
			}
			return new GenericFieldInstance(type, this);
		}

		public override int MetadataToken => Forwarder.MetadataToken;

		public override bool Equals(object obj)
		{
			var other = obj as MissingField;
			return other != null
				&& other.declaringType == declaringType
				&& other.name == name
				&& other.signature.Equals(signature);
		}

		public override int GetHashCode()
		{
			return declaringType.GetHashCode() ^ name.GetHashCode() ^ signature.GetHashCode();
		}

		public override string ToString()
		{
			return $"{FieldType.Name} {Name}";
		}

		public override int GetCurrentToken()
		{
			return Forwarder.GetCurrentToken();
		}

		public override bool IsBaked => Forwarder.IsBaked;
	}

	// NOTE this is currently only used by CustomAttributeData (because there is no other way to refer to a property)
	public sealed class MissingProperty : PropertyInfo
	{
		private readonly Type declaringType;
		private readonly string name;
		private readonly PropertySignature signature;

		public MissingProperty(Type declaringType, string name, PropertySignature signature)
		{
			this.declaringType = declaringType;
			this.name = name;
			this.signature = signature;
		}

		public override PropertyAttributes Attributes => throw new MissingMemberException(this);

		public override bool CanRead => throw new MissingMemberException(this);

		public override bool CanWrite => throw new MissingMemberException(this);

		public override MethodInfo GetGetMethod(bool nonPublic)
		{
			throw new MissingMemberException(this);
		}

		public override MethodInfo GetSetMethod(bool nonPublic)
		{
			throw new MissingMemberException(this);
		}

		public override MethodInfo[] GetAccessors(bool nonPublic)
		{
			throw new MissingMemberException(this);
		}

		public override object GetRawConstantValue()
		{
			throw new MissingMemberException(this);
		}

		public override bool IsPublic => throw new MissingMemberException(this);

		public override bool IsNonPrivate => throw new MissingMemberException(this);

		public override bool IsStatic => throw new MissingMemberException(this);

		public override PropertySignature PropertySignature => signature;

		public override string Name => name;

		public override Type DeclaringType => declaringType;

		public override Module Module => declaringType.Module;

		public override bool IsBaked => declaringType.IsBaked;

		public override int GetCurrentToken()
		{
			throw new MissingMemberException(this);
		}
	}
}
