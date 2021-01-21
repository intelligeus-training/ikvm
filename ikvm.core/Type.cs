/*
  Copyright (C) 2009-2015 Jeroen Frijters

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
using System.Runtime.InteropServices;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using IKVM.Reflection.Emit;

namespace IKVM.Reflection
{
	public interface IGenericContext
	{
		Type GetGenericTypeArgument(int index);
		Type GetGenericMethodArgument(int index);
	}

	public interface IGenericBinder
	{
		Type BindTypeParameter(Type type);
		Type BindMethodParameter(Type type);
	}

	public abstract class Type : MemberInfo, IGenericContext, IGenericBinder
	{
		public static readonly Type[] EmptyTypes = Empty<Type>.Array;
		protected readonly Type underlyingType;
		protected TypeFlags typeFlags;
		private byte sigElementType;	// only used if (__IsBuiltIn || HasElementType || __IsFunctionPointer || IsGenericParameter)

		[Flags]
		protected enum TypeFlags : ushort
		{
			// for use by TypeBuilder or TypeDefImpl
			IsGenericTypeDefinition = 1,

			// for use by TypeBuilder
			HasNestedTypes = 2,
			Baked = 4,

			// for use by IsValueType to cache result of IsValueTypeImpl
			ValueType = 8,
			NotValueType = 16,

			// for use by TypeDefImpl, TypeBuilder or MissingType
			PotentialEnumOrValueType = 32,
			EnumOrValueType = 64,

			// for use by TypeDefImpl
			NotGenericTypeDefinition = 128,

			// used to cache __ContainsMissingType
			ContainsMissingType_Unknown = 0,
			ContainsMissingType_Pending = 256,
			ContainsMissingType_Yes = 512,
			ContainsMissingType_No = 256 | 512,
			ContainsMissingType_Mask = 256 | 512,

			// built-in type support
			PotentialBuiltIn = 1024,
			BuiltIn = 2048,
		}

		// prevent subclassing by outsiders
		public Type()
		{ 
			underlyingType = this;
		}

		public Type(Type underlyingType)
		{
			System.Diagnostics.Debug.Assert(underlyingType.underlyingType == underlyingType);
			this.underlyingType = underlyingType;
			typeFlags = underlyingType.typeFlags;
		}

		public Type(byte sigElementType)
			: this()
		{
			this.sigElementType = sigElementType;
		}

		public static Binder DefaultBinder => new DefaultBinder();

		public sealed override MemberTypes MemberType => IsNested ? MemberTypes.NestedType : MemberTypes.TypeInfo;

		public virtual string AssemblyQualifiedName =>
			// NOTE the assembly name is not escaped here, only when used in a generic type instantiation
			this.FullName + ", " + this.Assembly.FullName;

		public abstract Type BaseType
		{
			get;
		}

		public abstract TypeAttributes Attributes
		{
			get;
		}

		public virtual Type GetElementType()
		{
			return null;
		}

		public virtual void CheckBaked()
		{
		}

		public virtual Type[] __GetDeclaredTypes()
		{
			return Type.EmptyTypes;
		}

		public virtual Type[] __GetDeclaredInterfaces()
		{
			return Type.EmptyTypes;
		}

		public virtual MethodBase[] __GetDeclaredMethods()
		{
			return Empty<MethodBase>.Array;
		}

		public virtual __MethodImplMap __GetMethodImplMap()
		{
			throw new NotSupportedException();
		}

		public virtual FieldInfo[] __GetDeclaredFields()
		{
			return Empty<FieldInfo>.Array;
		}

		public virtual EventInfo[] __GetDeclaredEvents()
		{
			return Empty<EventInfo>.Array;
		}

		public virtual PropertyInfo[] __GetDeclaredProperties()
		{
			return Empty<PropertyInfo>.Array;
		}

		public virtual CustomModifiers __GetCustomModifiers()
		{
			return new CustomModifiers();
		}

#if !NETSTANDARD
		[Obsolete("Please use __GetCustomModifiers() instead.")]
		public Type[] __GetRequiredCustomModifiers()
		{
			return __GetCustomModifiers().GetRequired();
		}

		[Obsolete("Please use __GetCustomModifiers() instead.")]
		public Type[] __GetOptionalCustomModifiers()
		{
			return __GetCustomModifiers().GetOptional();
		}
#endif

		public virtual __StandAloneMethodSig __MethodSignature => throw new InvalidOperationException();

		public bool HasElementType => IsArray || IsByRef || IsPointer;

		public bool IsArray => sigElementType == Signature.ELEMENT_TYPE_ARRAY 
		                       || sigElementType == Signature.ELEMENT_TYPE_SZARRAY;

		public bool __IsVector => sigElementType == Signature.ELEMENT_TYPE_SZARRAY;

		public bool IsByRef => sigElementType == Signature.ELEMENT_TYPE_BYREF;

		public bool IsPointer => sigElementType == Signature.ELEMENT_TYPE_PTR;

		public bool __IsFunctionPointer => sigElementType == Signature.ELEMENT_TYPE_FNPTR;

		public bool IsValueType
		{
			get
			{
				// MissingType sets both flags for WinRT projection types
				return (typeFlags & (TypeFlags.ValueType | TypeFlags.NotValueType)) switch
				{
					0 => IsValueTypeImpl,
					TypeFlags.ValueType | TypeFlags.NotValueType => IsValueTypeImpl,
					_ => (typeFlags & TypeFlags.ValueType) != 0
				};
			}
		}

		protected abstract bool IsValueTypeImpl
		{
			get;
		}

		public bool IsGenericParameter => sigElementType == Signature.ELEMENT_TYPE_VAR 
		                                  || sigElementType == Signature.ELEMENT_TYPE_MVAR;

		public virtual int GenericParameterPosition => throw new NotSupportedException();

		public virtual MethodBase DeclaringMethod => null;

		public Type UnderlyingSystemType
		{
			get { return underlyingType; }
		}

		public override Type DeclaringType => null;

		public virtual TypeName TypeName => throw new InvalidOperationException();

		public string __Name => TypeName.Name;

		public string __Namespace => TypeName.Namespace;

		public abstract override string Name
		{
			get;
		}

		public virtual string Namespace
		{
			get
			{
				if (IsNested)
				{
					return DeclaringType.Namespace;
				}
				return __Namespace;
			}
		}

		public virtual int GetModuleBuilderToken()
		{
			throw new InvalidOperationException();
		}

		public static bool operator ==(Type t1, Type t2)
		{
			// Casting to object results in smaller code than calling ReferenceEquals and makes
			// this method more likely to be inlined.
			// On CLR v2 x86, microbenchmarks show this to be faster than calling ReferenceEquals.
			return t1 == t2
				|| ((object)t1 != null && (object)t2 != null && t1.underlyingType == t2.underlyingType);
		}

		public static bool operator !=(Type t1, Type t2)
		{
			return !(t1 == t2);
		}

		public bool Equals(Type type)
		{
			return this == type;
		}

		public override bool Equals(object obj)
		{
			return Equals(obj as Type);
		}

		public override int GetHashCode()
		{
			var type = this.UnderlyingSystemType;
			return ReferenceEquals(type, this) ? base.GetHashCode() : type.GetHashCode();
		}

		public Type[] GenericTypeArguments => IsConstructedGenericType ? GetGenericArguments() : Type.EmptyTypes;

		public virtual Type[] GetGenericArguments()
		{
			return Type.EmptyTypes;
		}

		public virtual CustomModifiers[] __GetGenericArgumentsCustomModifiers()
		{
			return Empty<CustomModifiers>.Array;
		}

#if !NETSTANDARD
		[Obsolete("Please use __GetGenericArgumentsCustomModifiers() instead")]
		public Type[][] __GetGenericArgumentsRequiredCustomModifiers()
		{
			var customModifiers = __GetGenericArgumentsCustomModifiers();
			var types = new Type[customModifiers.Length][];
			for (var i = 0; i < types.Length; i++)
			{
				types[i] = customModifiers[i].GetRequired();
			}
			return types;
		}

		[Obsolete("Please use __GetGenericArgumentsCustomModifiers() instead")]
		public Type[][] __GetGenericArgumentsOptionalCustomModifiers()
		{
			var customModifiers = __GetGenericArgumentsCustomModifiers();
			var types = new Type[customModifiers.Length][];
			for (int i = 0; i < types.Length; i++)
			{
				types[i] = customModifiers[i].GetOptional();
			}
			return types;
		}
#endif

		public virtual Type GetGenericTypeDefinition()
		{
			throw new InvalidOperationException();
		}

		public StructLayoutAttribute StructLayoutAttribute
		{
			get
			{
				StructLayoutAttribute layout = (Attributes & TypeAttributes.LayoutMask) switch
				{
					TypeAttributes.AutoLayout => new StructLayoutAttribute(LayoutKind.Auto),
					TypeAttributes.SequentialLayout => new StructLayoutAttribute(LayoutKind.Sequential),
					TypeAttributes.ExplicitLayout => new StructLayoutAttribute(LayoutKind.Explicit),
					_ => throw new BadImageFormatException()
				};
				switch (Attributes & TypeAttributes.StringFormatMask)
				{
					case TypeAttributes.AnsiClass:
						layout.CharSet = CharSet.Ansi;
						break;
					case TypeAttributes.UnicodeClass:
						layout.CharSet = CharSet.Unicode;
						break;
					case TypeAttributes.AutoClass:
#if NETSTANDARD
						layout.CharSet = (CharSet)4;
#else
						layout.CharSet = CharSet.Auto;
#endif
						break;
					default:
#if NETSTANDARD
						layout.CharSet = (CharSet)1;
#else
						layout.CharSet = CharSet.None;
#endif
						break;
				}
				if (!__GetLayout(out layout.Pack, out layout.Size))
				{
					// compatibility with System.Reflection
					layout.Pack = 8;
				}
				return layout;
			}
		}

		public virtual bool __GetLayout(out int packingSize, out int typeSize)
		{
			packingSize = 0;
			typeSize = 0;
			return false;
		}

		public virtual bool IsGenericType => false;

		public virtual bool IsGenericTypeDefinition => false;

		// .NET 4.5 API
		public virtual bool IsConstructedGenericType => false;

		public virtual bool ContainsGenericParameters
		{
			get
			{
				if (IsGenericParameter)
				{
					return true;
				}
				foreach (var arg in GetGenericArguments())
				{
					if (arg.ContainsGenericParameters)
					{
						return true;
					}
				}
				return false;
			}
		}

		public virtual Type[] GetGenericParameterConstraints()
		{
			throw new InvalidOperationException();
		}

		public virtual CustomModifiers[] __GetGenericParameterConstraintCustomModifiers()
		{
			throw new InvalidOperationException();
		}

		public virtual GenericParameterAttributes GenericParameterAttributes => throw new InvalidOperationException();

		public virtual int GetArrayRank()
		{
			throw new NotSupportedException();
		}

		public virtual int[] __GetArraySizes()
		{
			throw new NotSupportedException();
		}

		public virtual int[] __GetArrayLowerBounds()
		{
			throw new NotSupportedException();
		}

		// .NET 4.0 API
		public virtual Type GetEnumUnderlyingType()
		{
			if (!IsEnum)
			{
				throw new ArgumentException();
			}
			CheckBaked();
			return GetEnumUnderlyingTypeImpl();
		}

		public Type GetEnumUnderlyingTypeImpl()
		{
			foreach (var fieldInfo in __GetDeclaredFields())
			{
				if (!fieldInfo.IsStatic)
				{
					// the CLR assumes that an enum has only one instance field, so we can do the same
					return fieldInfo.FieldType;
				}
			}
			throw new InvalidOperationException();
		}

		public string[] GetEnumNames()
		{
			if (!IsEnum)
			{
				throw new ArgumentException();
			}
			var names = new List<string>();
			foreach (var fieldInfo in __GetDeclaredFields())
			{
				if (fieldInfo.IsLiteral)
				{
					names.Add(fieldInfo.Name);
				}
			}
			return names.ToArray();
		}

		public string GetEnumName(object value)
		{
			if (!IsEnum)
			{
				throw new ArgumentException();
			}
			if (value == null)
			{
				throw new ArgumentNullException();
			}
			try
			{
				value = Convert.ChangeType(value, __GetSystemType(GetTypeCode(GetEnumUnderlyingType())));
			}
			catch (FormatException)
			{
				throw new ArgumentException();
			}
			catch (OverflowException)
			{
				return null;
			}
			catch (InvalidCastException)
			{
				return null;
			}
			foreach (FieldInfo field in __GetDeclaredFields())
			{
				if (field.IsLiteral && field.GetRawConstantValue().Equals(value))
				{
					return field.Name;
				}
			}
			return null;
		}

		public bool IsEnumDefined(object value)
		{
			if (value is string)
			{
				return Array.IndexOf(GetEnumNames(), value) != -1;
			}
			if (!IsEnum)
			{
				throw new ArgumentException();
			}
			if (value == null)
			{
				throw new ArgumentNullException();
			}
			if (value.GetType() != __GetSystemType(GetTypeCode(GetEnumUnderlyingType())))
			{
				throw new ArgumentException();
			}
			foreach (var fieldInfo in __GetDeclaredFields())
			{
				if (fieldInfo.IsLiteral && fieldInfo.GetRawConstantValue().Equals(value))
				{
					return true;
				}
			}
			return false;
		}

		public override string ToString()
		{
			return FullName;
		}

		public abstract string FullName
		{
			get;
		}

		protected string GetFullName()
		{
			var ns = TypeNameParser.Escape(this.__Namespace);
			var declaringType = this.DeclaringType;
			if (declaringType == null)
			{
				if (ns == null)
				{
					return Name;
				}
				
				return ns + "." + this.Name;
			}
		
			if (ns == null)
			{
				return declaringType.FullName + "+" + Name;
			}
			
			return declaringType.FullName + "+" + ns + "." + this.Name;
		}

		public virtual bool IsModulePseudoType => false;

		public virtual Type GetGenericTypeArgument(int index)
		{
			throw new InvalidOperationException();
		}

		public MemberInfo[] GetDefaultMembers()
		{
			var defaultMemberAttribute = this.Module.universe.Import(typeof(System.Reflection.DefaultMemberAttribute));
			foreach (var customAttributeData in CustomAttributeData.GetCustomAttributes(this))
			{
				if (customAttributeData.Constructor.DeclaringType.Equals(defaultMemberAttribute))
				{
					return GetMember((string)customAttributeData.ConstructorArguments[0].Value);
				}
			}
			return Empty<MemberInfo>.Array;
		}

		public MemberInfo[] GetMember(string name)
		{
			return GetMember(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
		}

		public MemberInfo[] GetMember(string name, BindingFlags bindingAttr)
		{
			return GetMember(name, MemberTypes.All, bindingAttr);
		}

		public MemberInfo[] GetMembers()
		{
			return GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
		}

		public MemberInfo[] GetMembers(BindingFlags bindingAttr)
		{
			List<MemberInfo> members = new List<MemberInfo>();
			members.AddRange(GetConstructors(bindingAttr));
			members.AddRange(GetMethods(bindingAttr));
			members.AddRange(GetFields(bindingAttr));
			members.AddRange(GetProperties(bindingAttr));
			members.AddRange(GetEvents(bindingAttr));
			members.AddRange(GetNestedTypes(bindingAttr));
			return members.ToArray();
		}

		public MemberInfo[] GetMember(string name, MemberTypes type, BindingFlags bindingAttr)
		{
			MemberFilter filter;
			if ((bindingAttr & BindingFlags.IgnoreCase) != 0)
			{
				name = name.ToLowerInvariant();
				filter = delegate(MemberInfo member, object filterCriteria) { return member.Name.ToLowerInvariant().Equals(filterCriteria); };
			}
			else
			{
				filter = delegate(MemberInfo member, object filterCriteria) { return member.Name.Equals(filterCriteria); };
			}
			return FindMembers(type, bindingAttr, filter, name);
		}

		private static void AddMembers(List<MemberInfo> list, MemberFilter filter, object filterCriteria, MemberInfo[] members)
		{
			foreach (var member in members)
			{
				if (filter == null || filter(member, filterCriteria))
				{
					list.Add(member);
				}
			}
		}

		public MemberInfo[] FindMembers(MemberTypes memberType, BindingFlags bindingAttr, MemberFilter filter, object filterCriteria)
		{
			var members = new List<MemberInfo>();
			if ((memberType & MemberTypes.Constructor) != 0)
			{
				AddMembers(members, filter, filterCriteria, GetConstructors(bindingAttr));
			}
			if ((memberType & MemberTypes.Method) != 0)
			{
				AddMembers(members, filter, filterCriteria, GetMethods(bindingAttr));
			}
			if ((memberType & MemberTypes.Field) != 0)
			{
				AddMembers(members, filter, filterCriteria, GetFields(bindingAttr));
			}
			if ((memberType & MemberTypes.Property) != 0)
			{
				AddMembers(members, filter, filterCriteria, GetProperties(bindingAttr));
			}
			if ((memberType & MemberTypes.Event) != 0)
			{
				AddMembers(members, filter, filterCriteria, GetEvents(bindingAttr));
			}
			if ((memberType & MemberTypes.NestedType) != 0)
			{
				AddMembers(members, filter, filterCriteria, GetNestedTypes(bindingAttr));
			}
			return members.ToArray();
		}

		private MemberInfo[] GetMembers<T>()
		{
			if (typeof(T) == typeof(ConstructorInfo) || typeof(T) == typeof(MethodInfo))
			{
				return __GetDeclaredMethods();
			}
			
			if (typeof(T) == typeof(FieldInfo))
			{
				return __GetDeclaredFields();
			}
			
			if (typeof(T) == typeof(PropertyInfo))
			{
				return __GetDeclaredProperties();
			}
			
			if (typeof(T) == typeof(EventInfo))
			{
				return __GetDeclaredEvents();
			}
			
			if (typeof(T) == typeof(Type))
			{
				return __GetDeclaredTypes();
			}
			
			throw new InvalidOperationException();
			
		}

		private T[] GetMembers<T>(BindingFlags flags)
			where T : MemberInfo
		{
			CheckBaked();
			var memberInfos = new List<T>();
			foreach (MemberInfo member in GetMembers<T>())
			{
				if (member is T && member.BindingFlagsMatch(flags))
				{
					memberInfos.Add((T)member);
				}
			}
			if ((flags & BindingFlags.DeclaredOnly) == 0)
			{
				for (var type = BaseType; type != null; type = type.BaseType)
				{
					type.CheckBaked();
					foreach (var member in type.GetMembers<T>())
					{
						if (member is T && member.BindingFlagsMatchInherited(flags))
						{
							memberInfos.Add((T)member.SetReflectedType(this));
						}
					}
				}
			}
			return memberInfos.ToArray();
		}

		private T GetMemberByName<T>(string name, BindingFlags flags, Predicate<T> filter) where T : MemberInfo
		{
			CheckBaked();
			if ((flags & BindingFlags.IgnoreCase) != 0)
			{
				name = name.ToLowerInvariant();
			}
			T found = null;
			foreach (var member in GetMembers<T>())
			{
				if (member is T && member.BindingFlagsMatch(flags))
				{
					var memberName = member.Name;
					if ((flags & BindingFlags.IgnoreCase) != 0)
					{
						memberName = memberName.ToLowerInvariant();
					}
					if (memberName == name && (filter == null || filter((T)member)))
					{
						if (found != null)
						{
							throw new AmbiguousMatchException();
						}
						found = (T)member;
					}
				}
			}
			if ((flags & BindingFlags.DeclaredOnly) == 0)
			{
				for (var type = BaseType; (found == null || typeof(T) == typeof(MethodInfo)) 
				                          && type != null; type = type.BaseType)
				{
					type.CheckBaked();
					foreach (var member in type.GetMembers<T>())
					{
						if (member is T && member.BindingFlagsMatchInherited(flags))
						{
							string memberName = member.Name;
							if ((flags & BindingFlags.IgnoreCase) != 0)
							{
								memberName = memberName.ToLowerInvariant();
							}
							if (memberName == name && (filter == null || filter((T)member)))
							{
								if (found != null)
								{
									MethodInfo mi;
									// TODO does this depend on HideBySig vs HideByName?
									if ((mi = found as MethodInfo) != null
										&& mi.MethodSignature.MatchParameterTypes(((MethodBase)member).MethodSignature))
									{
										continue;
									}
									throw new AmbiguousMatchException();
								}
								found = (T)member.SetReflectedType(this);
							}
						}
					}
				}
			}
			return found;
		}

		private T GetMemberByName<T>(string name, BindingFlags flags)
			where T : MemberInfo
		{
			return GetMemberByName<T>(name, flags, null);
		}

		public EventInfo GetEvent(string name)
		{
			return GetEvent(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
		}

		public EventInfo GetEvent(string name, BindingFlags bindingAttr)
		{
			return GetMemberByName<EventInfo>(name, bindingAttr);
		}

		public EventInfo[] GetEvents()
		{
			return GetEvents(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
		}

		public EventInfo[] GetEvents(BindingFlags bindingAttr)
		{
			return GetMembers<EventInfo>(bindingAttr);
		}

		public FieldInfo GetField(string name)
		{
			return GetField(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
		}

		public FieldInfo GetField(string name, BindingFlags bindingAttr)
		{
			return GetMemberByName<FieldInfo>(name, bindingAttr);
		}

		public FieldInfo[] GetFields()
		{
			return GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
		}

		public FieldInfo[] GetFields(BindingFlags bindingAttr)
		{
			return GetMembers<FieldInfo>(bindingAttr);
		}

		public Type[] GetInterfaces()
		{
			var types = new List<Type>();
			for (var type = this; type != null; type = type.BaseType)
			{
				AddInterfaces(types, type);
			}
			return types.ToArray();
		}

		private static void AddInterfaces(List<Type> list, Type type)
		{
			foreach (var iface in type.__GetDeclaredInterfaces())
			{
				if (!list.Contains(iface))
				{
					list.Add(iface);
					AddInterfaces(list, iface);
				}
			}
		}

		public MethodInfo[] GetMethods(BindingFlags bindingAttr)
		{
			CheckBaked();
			var methodInfos = new List<MethodInfo>();
			foreach (var methodBase in __GetDeclaredMethods())
			{
				var methodInfo = methodBase as MethodInfo;
				if (methodInfo != null && methodInfo.BindingFlagsMatch(bindingAttr))
				{
					methodInfos.Add(methodInfo);
				}
			}
			if ((bindingAttr & BindingFlags.DeclaredOnly) == 0)
			{
				var baseMethods = new List<MethodInfo>();
				foreach (var methodInfo in methodInfos)
				{
					if (methodInfo.IsVirtual)
					{
						baseMethods.Add(methodInfo.GetBaseDefinition());
					}
				}
				for (var type = BaseType; type != null; type = type.BaseType)
				{
					type.CheckBaked();
					foreach (var methodBase in type.__GetDeclaredMethods())
					{
						var methodInfo = methodBase as MethodInfo;
						if (methodInfo != null && methodInfo.BindingFlagsMatchInherited(bindingAttr))
						{
							if (methodInfo.IsVirtual)
							{
								if (baseMethods.Contains(methodInfo.GetBaseDefinition()))
								{
									continue;
								}
								baseMethods.Add(methodInfo.GetBaseDefinition());
							}
							methodInfos.Add((MethodInfo)methodInfo.SetReflectedType(this));
						}
					}
				}
			}
			return methodInfos.ToArray();
		}

		public MethodInfo[] GetMethods()
		{
			return GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
		}

		public MethodInfo GetMethod(string name)
		{
			return GetMethod(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public);
		}

		public MethodInfo GetMethod(string name, BindingFlags bindingAttr)
		{
			return GetMemberByName<MethodInfo>(name, bindingAttr);
		}

		public MethodInfo GetMethod(string name, Type[] types)
		{
			return GetMethod(name, types, null);
		}

		public MethodInfo GetMethod(string name, Type[] types, ParameterModifier[] modifiers)
		{
			return GetMethod(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public, null, types, modifiers);
		}

		public MethodInfo GetMethod(string name, BindingFlags bindingAttr, Binder binder, Type[] types, ParameterModifier[] modifiers)
		{
			// first we try an exact match and only if that fails we fall back to using the binder
			return GetMemberByName<MethodInfo>(name, bindingAttr,
				delegate(MethodInfo method) { return method.MethodSignature.MatchParameterTypes(types); })
				?? GetMethodWithBinder<MethodInfo>(name, bindingAttr, binder ?? DefaultBinder, types, modifiers);
		}

		private T GetMethodWithBinder<T>(string name, BindingFlags bindingAttr, Binder binder, Type[] types, ParameterModifier[] modifiers)
			where T : MethodBase
		{
			var methodBases = new List<MethodBase>();
			GetMemberByName<T>(name, bindingAttr, delegate(T method) {
				methodBases.Add(method);
				return false;
			});
			return (T)binder.SelectMethod(bindingAttr, methodBases.ToArray(), types, modifiers);
		}

		public MethodInfo GetMethod(string name, BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
		{
			// FXBUG callConvention seems to be ignored
			return GetMethod(name, bindingAttr, binder, types, modifiers);
		}

		public ConstructorInfo[] GetConstructors()
		{
			return GetConstructors(BindingFlags.Public | BindingFlags.Instance);
		}

		public ConstructorInfo[] GetConstructors(BindingFlags bindingAttr)
		{
			return GetMembers<ConstructorInfo>(bindingAttr | BindingFlags.DeclaredOnly);
		}

		public ConstructorInfo GetConstructor(Type[] types)
		{
			return GetConstructor(BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Standard, types, null);
		}

		public ConstructorInfo GetConstructor(BindingFlags bindingAttr, Binder binder, Type[] types, ParameterModifier[] modifiers)
		{
			ConstructorInfo ci1 = null;
			if ((bindingAttr & BindingFlags.Instance) != 0)
			{
				ci1 = GetConstructorImpl(ConstructorInfo.ConstructorName, bindingAttr, binder, types, modifiers);
			}
			if ((bindingAttr & BindingFlags.Static) != 0)
			{
				var ci2 = GetConstructorImpl(ConstructorInfo.TypeConstructorName, bindingAttr, binder, types, modifiers);
				if (ci2 != null)
				{
					if (ci1 != null)
					{
						throw new AmbiguousMatchException();
					}
					return ci2;
				}
			}
			return ci1;
		}

		private ConstructorInfo GetConstructorImpl(string name, BindingFlags bindingAttr, Binder binder, Type[] types, ParameterModifier[] modifiers)
		{
			// first we try an exact match and only if that fails we fall back to using the binder
			return GetMemberByName<ConstructorInfo>(name, bindingAttr | BindingFlags.DeclaredOnly,
				delegate(ConstructorInfo ctor) { return ctor.MethodSignature.MatchParameterTypes(types); })
				?? GetMethodWithBinder<ConstructorInfo>(name, bindingAttr, binder ?? DefaultBinder, types, modifiers);
		}

		public ConstructorInfo GetConstructor(BindingFlags bindingAttr, Binder binder, CallingConventions callingConvention, Type[] types, ParameterModifier[] modifiers)
		{
			// FXBUG callConvention seems to be ignored
			return GetConstructor(bindingAttr, binder, types, modifiers);
		}

		public Type ResolveNestedType(Module requester, TypeName typeName)
		{
			return FindNestedType(typeName) ?? Module.universe.GetMissingTypeOrThrow(requester, Module, this, typeName);
		}

		// unlike the public API, this takes the namespace and name into account
		public virtual Type FindNestedType(TypeName name)
		{
			foreach (var type in __GetDeclaredTypes())
			{
				if (type.TypeName == name)
				{
					return type;
				}
			}
			return null;
		}

		public virtual Type FindNestedTypeIgnoreCase(TypeName lowerCaseName)
		{
			foreach (var type in __GetDeclaredTypes())
			{
				if (type.TypeName.ToLowerInvariant() == lowerCaseName)
				{
					return type;
				}
			}
			return null;
		}

		public Type GetNestedType(string name)
		{
			return GetNestedType(name, BindingFlags.Public);
		}

		public Type GetNestedType(string name, BindingFlags bindingAttr)
		{
			// FXBUG the namespace is ignored, so we can use GetMemberByName
			return GetMemberByName<Type>(name, bindingAttr | BindingFlags.DeclaredOnly);
		}

		public Type[] GetNestedTypes()
		{
			return GetNestedTypes(BindingFlags.Public);
		}

		public Type[] GetNestedTypes(BindingFlags bindingAttr)
		{
			// FXBUG the namespace is ignored, so we can use GetMember
			return GetMembers<Type>(bindingAttr | BindingFlags.DeclaredOnly);
		}

		public PropertyInfo[] GetProperties()
		{
			return GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
		}

		public PropertyInfo[] GetProperties(BindingFlags bindingAttr)
		{
			return GetMembers<PropertyInfo>(bindingAttr);
		}

		public PropertyInfo GetProperty(string name)
		{
			return GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
		}

		public PropertyInfo GetProperty(string name, BindingFlags bindingAttr)
		{
			return GetMemberByName<PropertyInfo>(name, bindingAttr);
		}

		public PropertyInfo GetProperty(string name, Type returnType)
		{
			const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static;
			return GetMemberByName<PropertyInfo>(name, flags, prop => prop.PropertyType.Equals(returnType))
				?? GetPropertyWithBinder(name, flags, DefaultBinder, returnType, null, null);
		}

		public PropertyInfo GetProperty(string name, Type[] types)
		{
			const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static;
			return GetMemberByName<PropertyInfo>(name, flags, delegate(PropertyInfo prop) { return prop.PropertySignature.MatchParameterTypes(types); })
				?? GetPropertyWithBinder(name, flags, DefaultBinder, null, types, null);
		}

		public PropertyInfo GetProperty(string name, Type returnType, Type[] types)
		{
			return GetProperty(name, returnType, types, null);
		}

		public PropertyInfo GetProperty(string name, Type returnType, Type[] types, ParameterModifier[] modifiers)
		{
			return GetProperty(name,
							BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static, 
							null, 
							returnType, 
							types, 
							modifiers);
		}

		public PropertyInfo GetProperty(string name, 
										BindingFlags bindingAttr, 
										Binder binder, 
										Type returnType, 
										Type[] types, 
										ParameterModifier[] modifiers)
		{
			return GetMemberByName<PropertyInfo>(name, bindingAttr,
				       prop => prop.PropertyType.Equals(returnType) &&
				               prop.PropertySignature.MatchParameterTypes(types))
				?? GetPropertyWithBinder(name, bindingAttr, binder ?? DefaultBinder, returnType, types, modifiers);
		}

		private PropertyInfo GetPropertyWithBinder(string name, 
													BindingFlags bindingAttr, 
													Binder binder, 
													Type returnType, 
													Type[] types, 
													ParameterModifier[] modifiers)
		{
			var propertyInfos = new List<PropertyInfo>();
			GetMemberByName<PropertyInfo>(name, bindingAttr, delegate(PropertyInfo property) {
				propertyInfos.Add(property);
				return false;
			});
			return binder.SelectProperty(bindingAttr, propertyInfos.ToArray(), returnType, types, modifiers);
		}

		public Type GetInterface(string name)
		{
			return GetInterface(name, false);
		}

		public Type GetInterface(string name, bool ignoreCase)
		{
			if (ignoreCase)
			{
				name = name.ToLowerInvariant();
			}
			Type found = null;
			foreach (var type in GetInterfaces())
			{
				var typeName = type.FullName;
				if (ignoreCase)
				{
					typeName = typeName.ToLowerInvariant();
				}
				if (typeName == name)
				{
					if (found != null)
					{
						throw new AmbiguousMatchException();
					}
					found = type;
				}
			}
			return found;
		}

		public Type[] FindInterfaces(TypeFilter filter, object filterCriteria)
		{
			var types = new List<Type>();
			foreach (var type in GetInterfaces())
			{
				if (filter(type, filterCriteria))
				{
					types.Add(type);
				}
			}
			return types.ToArray();
		}

		public ConstructorInfo TypeInitializer => GetConstructor(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);

		public bool IsPrimitive =>
			__IsBuiltIn
			&& ((sigElementType >= Signature.ELEMENT_TYPE_BOOLEAN && sigElementType <= Signature.ELEMENT_TYPE_R8)
			    || sigElementType == Signature.ELEMENT_TYPE_I
			    || sigElementType == Signature.ELEMENT_TYPE_U);

		public bool __IsBuiltIn =>
			(typeFlags & (TypeFlags.BuiltIn | TypeFlags.PotentialBuiltIn)) != 0
			&& ((typeFlags & TypeFlags.BuiltIn) != 0 || ResolvePotentialBuiltInType());

		public byte SigElementType
		{
			get
			{
				// this property can only be called after __IsBuiltIn, HasElementType, __IsFunctionPointer or IsGenericParameter returned true
				System.Diagnostics.Debug.Assert((typeFlags & TypeFlags.BuiltIn) != 0 || HasElementType || __IsFunctionPointer || IsGenericParameter);
				return sigElementType;
			}
		}

		private bool ResolvePotentialBuiltInType()
		{
			// [ECMA 335] 8.2.2 Built-in value and reference types
			typeFlags &= ~TypeFlags.PotentialBuiltIn;
			Universe u = this.Universe;
			return __Name switch
			{
				"Boolean" => ResolvePotentialBuiltInType(u.System_Boolean, Signature.ELEMENT_TYPE_BOOLEAN),
				"Char" => ResolvePotentialBuiltInType(u.System_Char, Signature.ELEMENT_TYPE_CHAR),
				"Object" => ResolvePotentialBuiltInType(u.System_Object, Signature.ELEMENT_TYPE_OBJECT),
				"String" => ResolvePotentialBuiltInType(u.System_String, Signature.ELEMENT_TYPE_STRING),
				"Single" => ResolvePotentialBuiltInType(u.System_Single, Signature.ELEMENT_TYPE_R4),
				"Double" => ResolvePotentialBuiltInType(u.System_Double, Signature.ELEMENT_TYPE_R8),
				"SByte" => ResolvePotentialBuiltInType(u.System_SByte, Signature.ELEMENT_TYPE_I1),
				"Int16" => ResolvePotentialBuiltInType(u.System_Int16, Signature.ELEMENT_TYPE_I2),
				"Int32" => ResolvePotentialBuiltInType(u.System_Int32, Signature.ELEMENT_TYPE_I4),
				"Int64" => ResolvePotentialBuiltInType(u.System_Int64, Signature.ELEMENT_TYPE_I8),
				"IntPtr" => ResolvePotentialBuiltInType(u.System_IntPtr, Signature.ELEMENT_TYPE_I),
				"UIntPtr" => ResolvePotentialBuiltInType(u.System_UIntPtr, Signature.ELEMENT_TYPE_U),
				"TypedReference" => ResolvePotentialBuiltInType(u.System_TypedReference,
					Signature.ELEMENT_TYPE_TYPEDBYREF),
				"Byte" => ResolvePotentialBuiltInType(u.System_Byte, Signature.ELEMENT_TYPE_U1),
				"UInt16" => ResolvePotentialBuiltInType(u.System_UInt16, Signature.ELEMENT_TYPE_U2),
				"UInt32" => ResolvePotentialBuiltInType(u.System_UInt32, Signature.ELEMENT_TYPE_U4),
				"UInt64" => ResolvePotentialBuiltInType(u.System_UInt64, Signature.ELEMENT_TYPE_U8),
				"Void" => // [LAMESPEC] missing from ECMA list for some reason
					ResolvePotentialBuiltInType(u.System_Void, Signature.ELEMENT_TYPE_VOID),
				_ => throw new InvalidOperationException()
			};
		}

		private bool ResolvePotentialBuiltInType(Type builtIn, byte elementType)
		{
			if (this == builtIn)
			{
				typeFlags |= TypeFlags.BuiltIn;
				sigElementType = elementType;
				return true;
			}
			return false;
		}

		public bool IsEnum
		{
			get
			{
				var baseType = BaseType;
				return baseType != null
					&& baseType.IsEnumOrValueType
					&& baseType.__Name[0] == 'E';
			}
		}

		public bool IsSealed => (Attributes & TypeAttributes.Sealed) != 0;

		public bool IsAbstract => (Attributes & TypeAttributes.Abstract) != 0;

		private bool CheckVisibility(TypeAttributes access)
		{
			return (Attributes & TypeAttributes.VisibilityMask) == access;
		}

		public bool IsPublic => CheckVisibility(TypeAttributes.Public);

		public bool IsNestedPublic => CheckVisibility(TypeAttributes.NestedPublic);

		public bool IsNestedPrivate => CheckVisibility(TypeAttributes.NestedPrivate);

		public bool IsNestedFamily => CheckVisibility(TypeAttributes.NestedFamily);

		public bool IsNestedAssembly => CheckVisibility(TypeAttributes.NestedAssembly);

		public bool IsNestedFamANDAssem => CheckVisibility(TypeAttributes.NestedFamANDAssem);

		public bool IsNestedFamORAssem => CheckVisibility(TypeAttributes.NestedFamORAssem);

		public bool IsNotPublic => CheckVisibility(TypeAttributes.NotPublic);

		public bool IsImport => (Attributes & TypeAttributes.Import) != 0;

		public bool IsCOMObject => IsClass && IsImport;

		public bool IsContextful => IsSubclassOf(this.Module.universe.System_ContextBoundObject);

		public bool IsMarshalByRef => IsSubclassOf(this.Module.universe.System_MarshalByRefObject);

		public virtual bool IsVisible => IsPublic || (IsNestedPublic && this.DeclaringType.IsVisible);

		public bool IsAnsiClass => (Attributes & TypeAttributes.StringFormatMask) == TypeAttributes.AnsiClass;

		public bool IsUnicodeClass => (Attributes & TypeAttributes.StringFormatMask) == TypeAttributes.UnicodeClass;

		public bool IsAutoClass => (Attributes & TypeAttributes.StringFormatMask) == TypeAttributes.AutoClass;

		public bool IsAutoLayout => (Attributes & TypeAttributes.LayoutMask) == TypeAttributes.AutoLayout;

		public bool IsLayoutSequential => (Attributes & TypeAttributes.LayoutMask) == TypeAttributes.SequentialLayout;

		public bool IsExplicitLayout => (Attributes & TypeAttributes.LayoutMask) == TypeAttributes.ExplicitLayout;

		public bool IsSpecialName => (Attributes & TypeAttributes.SpecialName) != 0;

		public bool IsSerializable => (Attributes & TypeAttributes.Serializable) != 0;

		public bool IsClass => !IsInterface && !IsValueType;

		public bool IsInterface => (Attributes & TypeAttributes.Interface) != 0;

		public bool IsNested =>
			// FXBUG we check the declaring type (like .NET) and this results
			// in IsNested returning true for a generic type parameter
			DeclaringType != null;

		public bool __ContainsMissingType
		{
			get
			{
				if ((typeFlags & TypeFlags.ContainsMissingType_Mask) == TypeFlags.ContainsMissingType_Unknown)
				{
					// Generic parameter constraints can refer back to the type parameter they are part of,
					// so to prevent infinite recursion, we set the Pending flag during computation.
					typeFlags |= TypeFlags.ContainsMissingType_Pending;
					typeFlags = (typeFlags & ~TypeFlags.ContainsMissingType_Mask) 
					            | (ContainsMissingTypeImpl 
						            ? TypeFlags.ContainsMissingType_Yes : TypeFlags.ContainsMissingType_No);
				}
				return (typeFlags & TypeFlags.ContainsMissingType_Mask) == TypeFlags.ContainsMissingType_Yes;
			}
		}

		public static bool ContainsMissingType(Type[] types)
		{
			if (types == null)
			{
				return false;
			}
			foreach (var type in types)
			{
				if (type.__ContainsMissingType)
				{
					return true;
				}
			}
			return false;
		}

		protected virtual bool ContainsMissingTypeImpl =>
			__IsMissing
			|| ContainsMissingType(GetGenericArguments())
			|| __GetCustomModifiers().ContainsMissingType;

		public Type MakeArrayType()
		{
			return ArrayType.Make(this, new CustomModifiers());
		}

		public Type __MakeArrayType(CustomModifiers customModifiers)
		{
			return ArrayType.Make(this, customModifiers);
		}

#if !NETSTANDARD
		[Obsolete("Please use __MakeArrayType(CustomModifiers) instead.")]
		public Type __MakeArrayType(Type[] requiredCustomModifiers, Type[] optionalCustomModifiers)
		{
			return __MakeArrayType(CustomModifiers.FromReqOpt(requiredCustomModifiers, optionalCustomModifiers));
		}
#endif

		public Type MakeArrayType(int rank)
		{
			return __MakeArrayType(rank, new CustomModifiers());
		}

		public Type __MakeArrayType(int rank, CustomModifiers customModifiers)
		{
			return MultiArrayType.Make(this, rank, Empty<int>.Array, new int[rank], customModifiers);
		}

#if !NETSTANDARD
		[Obsolete("Please use __MakeArrayType(int, CustomModifiers) instead.")]
		public Type __MakeArrayType(int rank, Type[] requiredCustomModifiers, Type[] optionalCustomModifiers)
		{
			return __MakeArrayType(rank, CustomModifiers.FromReqOpt(requiredCustomModifiers, optionalCustomModifiers));
		}
#endif

		public Type __MakeArrayType(int rank, int[] sizes, int[] lobounds, CustomModifiers customModifiers)
		{
			return MultiArrayType.Make(this, rank, sizes ?? Empty<int>.Array, lobounds ?? Empty<int>.Array, customModifiers);
		}

#if !NETSTANDARD
		[Obsolete("Please use __MakeArrayType(int, int[], int[], CustomModifiers) instead.")]
		public Type __MakeArrayType(int rank, 
									int[] sizes, 
									int[] lobounds, 
									Type[] requiredCustomModifiers, 
									Type[] optionalCustomModifiers)
		{
			return __MakeArrayType(rank, 
									sizes, 
									lobounds, 
									CustomModifiers.FromReqOpt(requiredCustomModifiers, optionalCustomModifiers));
		}
#endif

		public Type MakeByRefType()
		{
			return ByRefType.Make(this, new CustomModifiers());
		}

		public Type __MakeByRefType(CustomModifiers customModifiers)
		{
			return ByRefType.Make(this, customModifiers);
		}

#if !NETSTANDARD
		[Obsolete("Please use __MakeByRefType(CustomModifiers) instead.")]
		public Type __MakeByRefType(Type[] requiredCustomModifiers, Type[] optionalCustomModifiers)
		{
			return __MakeByRefType(CustomModifiers.FromReqOpt(requiredCustomModifiers, optionalCustomModifiers));
		}
#endif

		public Type MakePointerType()
		{
			return PointerType.Make(this, new CustomModifiers());
		}

		public Type __MakePointerType(CustomModifiers customModifiers)
		{
			return PointerType.Make(this, customModifiers);
		}

#if !NETSTANDARD
		[Obsolete("Please use __MakeByRefType(CustomModifiers) instead.")]
		public Type __MakePointerType(Type[] requiredCustomModifiers, Type[] optionalCustomModifiers)
		{
			return __MakePointerType(CustomModifiers.FromReqOpt(requiredCustomModifiers, optionalCustomModifiers));
		}
#endif

		public Type MakeGenericType(params Type[] typeArguments)
		{
			return __MakeGenericType(typeArguments, null);
		}

		public Type __MakeGenericType(Type[] typeArguments, CustomModifiers[] customModifiers)
		{
			if (!__IsMissing && !IsGenericTypeDefinition)
			{
				throw new InvalidOperationException();
			}
			return GenericTypeInstance.Make(this, Util.Copy(typeArguments), customModifiers == null ? null : (CustomModifiers[])customModifiers.Clone());
		}

#if !NETSTANDARD
		[Obsolete("Please use __MakeGenericType(Type[], CustomModifiers[]) instead.")]
		public Type __MakeGenericType(Type[] typeArguments, Type[][] requiredCustomModifiers, Type[][] optionalCustomModifiers)
		{
			if (!__IsMissing && !this.IsGenericTypeDefinition)
			{
				throw new InvalidOperationException();
			}
			CustomModifiers[] mods = null;
			if (requiredCustomModifiers != null || optionalCustomModifiers != null)
			{
				mods = new CustomModifiers[typeArguments.Length];
				for (int i = 0; i < mods.Length; i++)
				{
					mods[i] = CustomModifiers.FromReqOpt(Util.NullSafeElementAt(requiredCustomModifiers, i), Util.NullSafeElementAt(optionalCustomModifiers, i));
				}
			}
			return GenericTypeInstance.Make(this, Util.Copy(typeArguments), mods);
		}
#endif

		public static System.Type __GetSystemType(TypeCode typeCode)
		{
			switch (typeCode)
			{
				case TypeCode.Boolean:
					return typeof(System.Boolean);
				case TypeCode.Byte:
					return typeof(System.Byte);
				case TypeCode.Char:
					return typeof(System.Char);
#if NETSTANDARD
				case (TypeCode)2:
					return System.Type.GetType("System.DBNull", true);
#else
				case TypeCode.DBNull:
					return typeof(System.DBNull);
#endif
				case TypeCode.DateTime:
					return typeof(System.DateTime);
				case TypeCode.Decimal:
					return typeof(System.Decimal);
				case TypeCode.Double:
					return typeof(System.Double);
				case TypeCode.Empty:
					return null;
				case TypeCode.Int16:
					return typeof(System.Int16);
				case TypeCode.Int32:
					return typeof(System.Int32);
				case TypeCode.Int64:
					return typeof(System.Int64);
				case TypeCode.Object:
					return typeof(System.Object);
				case TypeCode.SByte:
					return typeof(System.SByte);
				case TypeCode.Single:
					return typeof(System.Single);
				case TypeCode.String:
					return typeof(System.String);
				case TypeCode.UInt16:
					return typeof(System.UInt16);
				case TypeCode.UInt32:
					return typeof(System.UInt32);
				case TypeCode.UInt64:
					return typeof(System.UInt64);
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		public static TypeCode GetTypeCode(Type type)
		{
			if (type == null)
			{
				return TypeCode.Empty;
			}
			
			if (!type.__IsMissing && type.IsEnum)
			{
				type = type.GetEnumUnderlyingType();
			}
			
			var universe = type.Module.universe;
			
			if (type == universe.System_Boolean)
			{
				return TypeCode.Boolean;
			}
			
			if (type == universe.System_Char)
			{
				return TypeCode.Char;
			}
			
			if (type == universe.System_SByte)
			{
				return TypeCode.SByte;
			}
			
			if (type == universe.System_Byte)
			{
				return TypeCode.Byte;
			}
			
			if (type == universe.System_Int16)
			{
				return TypeCode.Int16;
			}
			
			if (type == universe.System_UInt16)
			{
				return TypeCode.UInt16;
			}
			
			if (type == universe.System_Int32)
			{
				return TypeCode.Int32;
			}
			
			if (type == universe.System_UInt32)
			{
				return TypeCode.UInt32;
			}
			
			if (type == universe.System_Int64)
			{
				return TypeCode.Int64;
			}
			
			if (type == universe.System_UInt64)
			{
				return TypeCode.UInt64;
			}
			
			if (type == universe.System_Single)
			{
				return TypeCode.Single;
			}
			
			if (type == universe.System_Double)
			{
				return TypeCode.Double;
			}
			
			if (type == universe.System_DateTime)
			{
				return TypeCode.DateTime;
			}
			
			if (type == universe.System_DBNull)
			{
#if NETSTANDARD
				return (TypeCode)2;
#else
				return TypeCode.DBNull;
#endif
			}
			
			if (type == universe.System_Decimal)
			{
				return TypeCode.Decimal;
			}
			
			if (type == universe.System_String)
			{
				return TypeCode.String;
			}
			
			if (type.__IsMissing)
			{
				throw new MissingMemberException(type);
			}
			
			return TypeCode.Object;
			
		}

		public Assembly Assembly => Module.Assembly;

		public bool IsAssignableFrom(Type type)
		{
			if (Equals(type))
			{
				return true;
			}
			
			if (type == null)
			{
				return false;
			}
			
			if (IsArray && type.IsArray)
			{
				if (GetArrayRank() != type.GetArrayRank())
				{
					return false;
				}
				if (__IsVector && !type.__IsVector)
				{
					return false;
				}
				var e1 = GetElementType();
				var e2 = type.GetElementType();
				return e1.IsValueType == e2.IsValueType && e1.IsAssignableFrom(e2);
			}
			
			if (IsCovariant(type))
			{
				return true;
			}
			
			if (IsSealed)
			{
				return false;
			}
			
			if (IsInterface)
			{
				foreach (var iface in type.GetInterfaces())
				{
					if (Equals(iface) || IsCovariant(iface))
					{
						return true;
					}
				}
				return false;
			}
			
			if (type.IsInterface)
			{
				return this == this.Module.universe.System_Object;
			}
			
			if (type.IsPointer)
			{
				return this == this.Module.universe.System_Object || this == this.Module.universe.System_ValueType;
			}
			
			return type.IsSubclassOf(this);
			
		}

		private bool IsCovariant(Type other)
		{
			if (IsConstructedGenericType
				&& other.IsConstructedGenericType
				&& GetGenericTypeDefinition() == other.GetGenericTypeDefinition())
			{
				var typeParameters = GetGenericTypeDefinition().GetGenericArguments();
				for (var i = 0; i < typeParameters.Length; i++)
				{
					var t1 = GetGenericTypeArgument(i);
					var t2 = other.GetGenericTypeArgument(i);
					if (t1.IsValueType != t2.IsValueType)
					{
						return false;
					}
					switch (typeParameters[i].GenericParameterAttributes & GenericParameterAttributes.VarianceMask)
					{
						case GenericParameterAttributes.Covariant:
							if (!t1.IsAssignableFrom(t2))
							{
								return false;
							}
							break;
						case GenericParameterAttributes.Contravariant:
							if (!t2.IsAssignableFrom(t1))
							{
								return false;
							}
							break;
						case GenericParameterAttributes.None:
							if (t1 != t2)
							{
								return false;
							}
							break;
					}
				}
				return true;
			}
			return false;
		}

		public bool IsSubclassOf(Type type)
		{
			var thisType = BaseType;
			while (thisType != null)
			{
				if (thisType.Equals(type))
				{
					return true;
				}
				thisType = thisType.BaseType;
			}
			return false;
		}

		// This returns true if this type directly (i.e. not inherited from the base class) implements the interface.
		// Note that a complicating factor is that the interface itself can be implemented by an interface that extends it.
		private bool IsDirectlyImplementedInterface(Type interfaceType)
		{
			foreach (var iface in __GetDeclaredInterfaces())
			{
				if (interfaceType.IsAssignableFrom(iface))
				{
					return true;
				}
			}
			return false;
		}

		public InterfaceMapping GetInterfaceMap(Type interfaceType)
		{
			CheckBaked();
			InterfaceMapping map;
			map.InterfaceMethods = interfaceType.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public);
			map.InterfaceType = interfaceType;
			map.TargetMethods = new MethodInfo[map.InterfaceMethods.Length];
			map.TargetType = this;
			FillInInterfaceMethods(interfaceType, map.InterfaceMethods, map.TargetMethods);
			return map;
		}

		private void FillInInterfaceMethods(Type interfaceType, 
											MethodInfo[] interfaceMethods, 
											MethodInfo[] targetMethods)
		{
			FillInExplicitInterfaceMethods(interfaceMethods, targetMethods);
			var direct = IsDirectlyImplementedInterface(interfaceType);
			if (direct)
			{
				FillInImplicitInterfaceMethods(interfaceMethods, targetMethods);
			}
			var baseType = BaseType;
			if (baseType != null)
			{
				baseType.FillInInterfaceMethods(interfaceType, interfaceMethods, targetMethods);
				ReplaceOverriddenMethods(targetMethods);
			}
			if (direct)
			{
				for (var type = this.BaseType; type != null && type.Module == Module; type = type.BaseType)
				{
					type.FillInImplicitInterfaceMethods(interfaceMethods, targetMethods);
				}
			}
		}

		private void FillInImplicitInterfaceMethods(MethodInfo[] interfaceMethods, MethodInfo[] targetMethods)
		{
			MethodBase[] methods = null;
			for (var i = 0; i < targetMethods.Length; i++)
			{
				if (targetMethods[i] == null)
				{
					if (methods == null)
					{
						methods = __GetDeclaredMethods();
					}
					for (var j = 0; j < methods.Length; j++)
					{
						if (methods[j].IsVirtual
							&& methods[j].Name == interfaceMethods[i].Name
							&& methods[j].MethodSignature.Equals(interfaceMethods[i].MethodSignature))
						{
							targetMethods[i] = (MethodInfo)methods[j];
							break;
						}
					}
				}
			}
		}

		private void ReplaceOverriddenMethods(MethodInfo[] baseMethods)
		{
			var impl = __GetMethodImplMap();
			for (var i = 0; i < baseMethods.Length; i++)
			{
				if (baseMethods[i] != null && !baseMethods[i].IsFinal)
				{
					var baseDefinition = baseMethods[i].GetBaseDefinition();
					for (var j = 0; j < impl.MethodDeclarations.Length; j++)
					{
						for (var k = 0; k < impl.MethodDeclarations[j].Length; k++)
						{
							if (impl.MethodDeclarations[j][k].GetBaseDefinition() == baseDefinition)
							{
								baseMethods[i] = impl.MethodBodies[j];
								goto next;
							}
						}
					}
					MethodInfo candidate = FindMethod(baseDefinition.Name, baseDefinition.MethodSignature) as MethodInfo;
					if (candidate != null && candidate.IsVirtual && !candidate.IsNewSlot)
					{
						baseMethods[i] = candidate;
					}
				}
			next: ;
			}
		}

		public void FillInExplicitInterfaceMethods(MethodInfo[] interfaceMethods, MethodInfo[] targetMethods)
		{
			var implMap = __GetMethodImplMap();
			for (var i = 0; i < implMap.MethodDeclarations.Length; i++)
			{
				for (var j = 0; j < implMap.MethodDeclarations[i].Length; j++)
				{
					var index = Array.IndexOf(interfaceMethods, implMap.MethodDeclarations[i][j]);
					if (index != -1 && targetMethods[index] == null)
					{
						targetMethods[index] = implMap.MethodBodies[i];
					}
				}
			}
		}

		Type IGenericContext.GetGenericTypeArgument(int index)
		{
			return GetGenericTypeArgument(index);
		}

		Type IGenericContext.GetGenericMethodArgument(int index)
		{
			throw new BadImageFormatException();
		}

		Type IGenericBinder.BindTypeParameter(Type type)
		{
			return GetGenericTypeArgument(type.GenericParameterPosition);
		}

		Type IGenericBinder.BindMethodParameter(Type type)
		{
			throw new BadImageFormatException();
		}

		public virtual Type BindTypeParameters(IGenericBinder binder)
		{
			if (IsGenericTypeDefinition)
			{
				var args = GetGenericArguments();
				InplaceBindTypeParameters(binder, args);
				return GenericTypeInstance.Make(this, args, null);
			}
			
			return this;
			
		}

		private static void InplaceBindTypeParameters(IGenericBinder binder, Type[] types)
		{
			for (var i = 0; i < types.Length; i++)
			{
				types[i] = types[i].BindTypeParameters(binder);
			}
		}

		public virtual MethodBase FindMethod(string name, MethodSignature signature)
		{
			foreach (var methodBase in __GetDeclaredMethods())
			{
				if (methodBase.Name == name && methodBase.MethodSignature.Equals(signature))
				{
					return methodBase;
				}
			}
			return null;
		}

		public virtual FieldInfo FindField(string name, FieldSignature signature)
		{
			foreach (var fieldInfo in __GetDeclaredFields())
			{
				if (fieldInfo.Name == name && fieldInfo.FieldSignature.Equals(signature))
				{
					return fieldInfo;
				}
			}
			return null;
		}

		public bool IsAllowMultipleCustomAttribute
		{
			get
			{
				var customAttributeDatas = CustomAttributeData.__GetCustomAttributes(this, 
																		Module.universe.System_AttributeUsageAttribute,
																		false);
				if (customAttributeDatas.Count == 1)
				{
					foreach (var arg in customAttributeDatas[0].NamedArguments)
					{
						if (arg.MemberInfo.Name == "AllowMultiple")
						{
							return (bool)arg.TypedValue.Value;
						}
					}
				}
				return false;
			}
		}

		public Type MarkNotValueType()
		{
			typeFlags |= TypeFlags.NotValueType;
			return this;
		}

		public Type MarkValueType()
		{
			typeFlags |= TypeFlags.ValueType;
			return this;
		}

		public ConstructorInfo GetPseudoCustomAttributeConstructor(params Type[] parameterTypes)
		{
			var universe = Module.universe;
			var methodSig = MethodSignature.MakeFromBuilder(universe.System_Void, 
																parameterTypes, 
																new PackedCustomModifiers(), 
																CallingConventions.Standard | CallingConventions.HasThis, 
																0);
			var methodBase =
				FindMethod(".ctor", methodSig) ??
				universe.GetMissingMethodOrThrow(null, this, ".ctor", methodSig);
			return (ConstructorInfo)methodBase;
		}

		public MethodBase __CreateMissingMethod(string name, CallingConventions callingConvention, Type returnType, CustomModifiers returnTypeCustomModifiers, Type[] parameterTypes, CustomModifiers[] parameterTypeCustomModifiers)
		{
			return CreateMissingMethod(name, 
										callingConvention, 
										returnType, 
										parameterTypes, 
										PackedCustomModifiers.CreateFromExternal(returnTypeCustomModifiers, 
																					parameterTypeCustomModifiers, 
																					parameterTypes.Length));
		}

		private MethodBase CreateMissingMethod(string name, 
												CallingConventions callingConvention, 
												Type returnType, 
												Type[] parameterTypes, 
												PackedCustomModifiers customModifiers)
		{
			var methodSignature = new MethodSignature(
				returnType ?? Module.universe.System_Void,
				Util.Copy(parameterTypes),
				customModifiers,
				callingConvention,
				0);
			var methodInfo = new MissingMethod(this, name, methodSignature);
			if (name == ".ctor" || name == ".cctor")
			{
				return new ConstructorInfoImpl(methodInfo);
			}
			return methodInfo;
		}

#if !NETSTANDARD
		[Obsolete("Please use __CreateMissingMethod(string, CallingConventions, Type, CustomModifiers, Type[], CustomModifiers[]) instead")]
		public MethodBase __CreateMissingMethod(string name, 
												CallingConventions callingConvention, 
												Type returnType, 
												Type[] returnTypeRequiredCustomModifiers, 
												Type[] returnTypeOptionalCustomModifiers, 
												Type[] parameterTypes, 
												Type[][] parameterTypeRequiredCustomModifiers, 
												Type[][] parameterTypeOptionalCustomModifiers)
		{
			return CreateMissingMethod(name, 
										callingConvention, 
										returnType, 
										parameterTypes, 
										PackedCustomModifiers.CreateFromExternal(returnTypeOptionalCustomModifiers, 
																			returnTypeRequiredCustomModifiers, 
																			parameterTypeOptionalCustomModifiers, 
																			parameterTypeRequiredCustomModifiers,
																			parameterTypes.Length));
		}
#endif

		public FieldInfo __CreateMissingField(string name, Type fieldType, CustomModifiers customModifiers)
		{
			return new MissingField(this, name, FieldSignature.Create(fieldType, customModifiers));
		}

#if !NETSTANDARD
		[Obsolete("Please use __CreateMissingField(string, Type, CustomModifiers) instead")]
		public FieldInfo __CreateMissingField(string name, 
												Type fieldType, 
												Type[] requiredCustomModifiers, 
												Type[] optionalCustomModifiers)
		{
			return __CreateMissingField(name, 
										fieldType, 
										CustomModifiers.FromReqOpt(requiredCustomModifiers, optionalCustomModifiers));
		}
#endif

		public PropertyInfo __CreateMissingProperty(string name, 
													CallingConventions callingConvention, 
													Type propertyType, 
													CustomModifiers propertyTypeCustomModifiers, 
													Type[] parameterTypes, 
													CustomModifiers[] parameterTypeCustomModifiers)
		{
			var propertySignature = PropertySignature.Create(callingConvention,
				propertyType,
				parameterTypes,
				PackedCustomModifiers.CreateFromExternal(propertyTypeCustomModifiers, 
															parameterTypeCustomModifiers, 
															Util.NullSafeLength(parameterTypes)));
			return new MissingProperty(this, name, propertySignature);
		}

		public virtual Type SetMetadataTokenForMissing(int token, int flags)
		{
			return this;
		}

		public virtual Type SetCyclicTypeForwarder()
		{
			return this;
		}

		public virtual Type SetCyclicTypeSpec()
		{
			return this;
		}

		protected void MarkKnownType(string typeNamespace, string typeName)
		{
			// we assume that mscorlib won't have nested types with these names,
			// so we don't check that we're not a nested type
			if (typeNamespace == "System")
			{
				switch (typeName)
				{
					case "Boolean":
					case "Char":
					case "Object":
					case "String":
					case "Single":
					case "Double":
					case "SByte":
					case "Int16":
					case "Int32":
					case "Int64":
					case "IntPtr":
					case "UIntPtr":
					case "TypedReference":
					case "Byte":
					case "UInt16":
					case "UInt32":
					case "UInt64":
					case "Void":
						typeFlags |= TypeFlags.PotentialBuiltIn;
						break;
					case "Enum":
					case "ValueType":
						typeFlags |= TypeFlags.PotentialEnumOrValueType;
						break;
				}
			}
		}

		private bool ResolvePotentialEnumOrValueType()
		{
			if (Assembly == this.Universe.Mscorlib
				|| Assembly.GetName().Name.Equals("mscorlib", StringComparison.OrdinalIgnoreCase)
				// check if mscorlib forwards the type (.NETCore profile reference mscorlib forwards System.Enum and System.ValueType to System.Runtime.dll)
				|| Universe.Mscorlib.FindType(TypeName) == this)
			{
				typeFlags = (typeFlags & ~TypeFlags.PotentialEnumOrValueType) | TypeFlags.EnumOrValueType;
				return true;
			}
			
			typeFlags &= ~TypeFlags.PotentialEnumOrValueType;
			return false;
			
		}

		public bool IsEnumOrValueType =>
			(typeFlags & (TypeFlags.EnumOrValueType | TypeFlags.PotentialEnumOrValueType)) != 0
			&& ((typeFlags & TypeFlags.EnumOrValueType) != 0 || ResolvePotentialEnumOrValueType());

		public virtual Universe Universe => Module.universe;

		public sealed override bool BindingFlagsMatch(BindingFlags flags)
		{
			return BindingFlagsMatch(IsNestedPublic, flags, BindingFlags.Public, BindingFlags.NonPublic);
		}

		public sealed override MemberInfo SetReflectedType(Type type)
		{
			throw new InvalidOperationException();
		}

		public override int GetCurrentToken()
		{
			return MetadataToken;
		}

		public sealed override List<CustomAttributeData> GetPseudoCustomAttributes(Type attributeType)
		{
			// types don't have pseudo custom attributes
			return null;
		}

		// in .NET this is an extension method, but we target .NET 2.0, so we have an instance method
		public TypeInfo GetTypeInfo()
		{
			var typeInfo = this as TypeInfo;
			if (typeInfo == null)
			{
				throw new MissingMemberException(this);
			}
			return typeInfo;
		}

		public virtual bool __IsTypeForwarder => false;

		public virtual bool __IsCyclicTypeForwarder => false;

		public virtual bool __IsCyclicTypeSpec => false;
	}

	abstract class ElementHolderType : TypeInfo
	{
		protected readonly Type elementType;
		private int token;
		private readonly CustomModifiers mods;

		protected ElementHolderType(Type elementType, CustomModifiers mods, byte sigElementType)
			: base(sigElementType)
		{
			this.elementType = elementType;
			this.mods = mods;
		}

		protected bool EqualsHelper(ElementHolderType other)
		{
			return other != null
				&& other.elementType.Equals(elementType)
				&& other.mods.Equals(mods);
		}

		public override CustomModifiers __GetCustomModifiers()
		{
			return mods;
		}

		public sealed override string Name => elementType.Name + GetSuffix();

		public sealed override string Namespace => elementType.Namespace;

		public sealed override string FullName => elementType.FullName + GetSuffix();

		public sealed override string ToString()
		{
			return elementType.ToString() + GetSuffix();
		}

		public sealed override Type GetElementType()
		{
			return elementType;
		}

		public sealed override Module Module => elementType.Module;

		public sealed override int GetModuleBuilderToken()
		{
			if (token == 0)
			{
				token = ((ModuleBuilder)elementType.Module).ImportType(this);
			}
			return token;
		}

		public sealed override bool ContainsGenericParameters
		{
			get
			{
				var type = elementType;
				while (type.HasElementType)
				{
					type = type.GetElementType();
				}
				return type.ContainsGenericParameters;
			}
		}

		protected sealed override bool ContainsMissingTypeImpl
		{
			get
			{
				var type = elementType;
				while (type.HasElementType)
				{
					type = type.GetElementType();
				}
				return type.__ContainsMissingType || mods.ContainsMissingType;
			}
		}

		protected sealed override bool IsValueTypeImpl => false;

		public sealed override Type BindTypeParameters(IGenericBinder binder)
		{
			var type = elementType.BindTypeParameters(binder);
			var mods = this.mods.Bind(binder);
			if (ReferenceEquals(type, elementType)
				&& mods.Equals(this.mods))
			{
				return this;
			}
			return Wrap(type, mods);
		}

		public override void CheckBaked()
		{
			elementType.CheckBaked();
		}

		public sealed override Universe Universe => elementType.Universe;

		public sealed override bool IsBaked => elementType.IsBaked;

		public sealed override int GetCurrentToken()
		{
			// we don't have a token, so we return 0 (which is never a valid token)
			return 0;
		}

		public abstract string GetSuffix();

		protected abstract Type Wrap(Type type, CustomModifiers mods);
	}

	sealed class ArrayType : ElementHolderType
	{
		public static Type Make(Type type, CustomModifiers mods)
		{
			return type.Universe.CanonicalizeType(new ArrayType(type, mods));
		}

		private ArrayType(Type type, CustomModifiers mods)
			: base(type, mods, Signature.ELEMENT_TYPE_SZARRAY)
		{
		}

		public override Type BaseType => elementType.Module.universe.System_Array;

		public override Type[] __GetDeclaredInterfaces()
		{
			return new [] {
				Module.universe.Import(typeof(IList<>)).MakeGenericType(elementType),
				Module.universe.Import(typeof(ICollection<>)).MakeGenericType(elementType),
				Module.universe.Import(typeof(IEnumerable<>)).MakeGenericType(elementType)
			};
		}

		public override MethodBase[] __GetDeclaredMethods()
		{
			var int32 = new [] { Module.universe.System_Int32 };
			var methodBases = new List<MethodBase>();
			methodBases.Add(new BuiltinArrayMethod(this.Module, this, "Set", CallingConventions.Standard | CallingConventions.HasThis, this.Module.universe.System_Void, new Type[] { this.Module.universe.System_Int32, elementType }));
			methodBases.Add(new BuiltinArrayMethod(this.Module, this, "Address", CallingConventions.Standard | CallingConventions.HasThis, elementType.MakeByRefType(), int32));
			methodBases.Add(new BuiltinArrayMethod(this.Module, this, "Get", CallingConventions.Standard | CallingConventions.HasThis, elementType, int32));
			methodBases.Add(new ConstructorInfoImpl(new BuiltinArrayMethod(this.Module, this, ".ctor", CallingConventions.Standard | CallingConventions.HasThis, this.Module.universe.System_Void, int32)));
			for (Type type = elementType; type.__IsVector; type = type.GetElementType())
			{
				Array.Resize(ref int32, int32.Length + 1);
				int32[int32.Length - 1] = int32[0];
				methodBases.Add(new ConstructorInfoImpl(new BuiltinArrayMethod(this.Module, this, ".ctor", CallingConventions.Standard | CallingConventions.HasThis, this.Module.universe.System_Void, int32)));
			}
			return methodBases.ToArray();
		}

		public override TypeAttributes Attributes
		{
			get { return TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.Serializable; }
		}

		public override int GetArrayRank()
		{
			return 1;
		}

		public override bool Equals(object o)
		{
			return EqualsHelper(o as ArrayType);
		}

		public override int GetHashCode()
		{
			return elementType.GetHashCode() * 5;
		}

		public override string GetSuffix()
		{
			return "[]";
		}

		protected override Type Wrap(Type type, CustomModifiers mods)
		{
			return Make(type, mods);
		}
	}

	public sealed class MultiArrayType : ElementHolderType
	{
		private readonly int rank;
		private readonly int[] sizes;
		private readonly int[] lobounds;

		public static Type Make(Type type, int rank, int[] sizes, int[] lobounds, CustomModifiers mods)
		{
			return type.Universe.CanonicalizeType(new MultiArrayType(type, rank, sizes, lobounds, mods));
		}

		private MultiArrayType(Type type, int rank, int[] sizes, int[] lobounds, CustomModifiers mods)
			: base(type, mods, Signature.ELEMENT_TYPE_ARRAY)
		{
			this.rank = rank;
			this.sizes = sizes;
			this.lobounds = lobounds;
		}

		public override Type BaseType => elementType.Module.universe.System_Array;

		public override MethodBase[] __GetDeclaredMethods()
		{
			var int32 = this.Module.universe.System_Int32;
			var setArgs = new Type[rank + 1];
			var getArgs = new Type[rank];
			var ctorArgs = new Type[rank * 2];
			for (var i = 0; i < rank; i++)
			{
				setArgs[i] = int32;
				getArgs[i] = int32;
				ctorArgs[i * 2 + 0] = int32;
				ctorArgs[i * 2 + 1] = int32;
			}
			setArgs[rank] = elementType;
			return new MethodBase[] {
				new ConstructorInfoImpl(new BuiltinArrayMethod(this.Module, this, ".ctor", CallingConventions.Standard | CallingConventions.HasThis, this.Module.universe.System_Void, getArgs)),
				new ConstructorInfoImpl(new BuiltinArrayMethod(this.Module, this, ".ctor", CallingConventions.Standard | CallingConventions.HasThis, this.Module.universe.System_Void, ctorArgs)),
				new BuiltinArrayMethod(this.Module, this, "Set", CallingConventions.Standard | CallingConventions.HasThis, this.Module.universe.System_Void, setArgs),
				new BuiltinArrayMethod(this.Module, this, "Address", CallingConventions.Standard | CallingConventions.HasThis, elementType.MakeByRefType(), getArgs),
				new BuiltinArrayMethod(this.Module, this, "Get", CallingConventions.Standard | CallingConventions.HasThis, elementType, getArgs),
			};
		}

		public override TypeAttributes Attributes => TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.Serializable;

		public override int GetArrayRank()
		{
			return rank;
		}

		public override int[] __GetArraySizes()
		{
			return Util.Copy(sizes);
		}

		public override int[] __GetArrayLowerBounds()
		{
			return Util.Copy(lobounds);
		}

		public override bool Equals(object o)
		{
			var multiArrayType = o as MultiArrayType;
			return EqualsHelper(multiArrayType)
				&& multiArrayType.rank == rank
				&& ArrayEquals(multiArrayType.sizes, sizes)
				&& ArrayEquals(multiArrayType.lobounds, lobounds);
		}

		private static bool ArrayEquals(int[] i1, int[] i2)
		{
			if (i1.Length == i2.Length)
			{
				for (var i = 0; i < i1.Length; i++)
				{
					if (i1[i] != i2[i])
					{
						return false;
					}
				}
				return true;
			}
			return false;
		}

		public override int GetHashCode()
		{
			return elementType.GetHashCode() * 9 + rank;
		}

		public override string GetSuffix()
		{
			if (rank == 1)
			{
				return "[*]";
			}
			else
			{
				return "[" + new String(',', rank - 1) + "]";
			}
		}

		protected override Type Wrap(Type type, CustomModifiers mods)
		{
			return Make(type, rank, sizes, lobounds, mods);
		}
	}

	public sealed class BuiltinArrayMethod : ArrayMethod
	{
		public BuiltinArrayMethod(Module module, 
									Type arrayClass, 
									string methodName, 
									CallingConventions callingConvention, 
									Type returnType, 
									Type[] parameterTypes)
			: base(module, arrayClass, methodName, callingConvention, returnType, parameterTypes)
		{
		}

		public override MethodAttributes Attributes => this.Name == ".ctor" ? MethodAttributes.RTSpecialName | MethodAttributes.Public : MethodAttributes.Public;

		public override MethodImplAttributes GetMethodImplementationFlags()
		{
			return MethodImplAttributes.IL;
		}

		public override int MetadataToken => 0x06000000;

		public override MethodBody GetMethodBody()
		{
			return null;
		}

		public override ParameterInfo[] GetParameters()
		{
			var parameterInfos = new ParameterInfo[parameterTypes.Length];
			for (var i = 0; i < parameterInfos.Length; i++)
			{
				parameterInfos[i] = new ParameterInfoImpl(this, parameterTypes[i], i);
			}
			return parameterInfos;
		}

		public override ParameterInfo ReturnParameter => new ParameterInfoImpl(this, this.ReturnType, -1);

		private sealed class ParameterInfoImpl : ParameterInfo
		{
			private readonly MethodInfo method;
			private readonly Type type;
			private readonly int pos;

			public ParameterInfoImpl(MethodInfo method, Type type, int pos)
			{
				this.method = method;
				this.type = type;
				this.pos = pos;
			}

			public override Type ParameterType => type;

			public override string Name => null;

			public override ParameterAttributes Attributes => ParameterAttributes.None;

			public override int Position => pos;

			public override object RawDefaultValue => null;

			public override CustomModifiers __GetCustomModifiers()
			{
				return new CustomModifiers();
			}

			public override bool __TryGetFieldMarshal(out FieldMarshal fieldMarshal)
			{
				fieldMarshal = new FieldMarshal();
				return false;
			}

			public override MemberInfo Member => method.IsConstructor ? (MethodBase)new ConstructorInfoImpl(method) : method;

			public override int MetadataToken
			{
				get { return 0x08000000; }
			}

			public override Module Module => method.Module;
		}
	}

	public sealed class ByRefType : ElementHolderType
	{
		public static Type Make(Type type, CustomModifiers mods)
		{
			return type.Universe.CanonicalizeType(new ByRefType(type, mods));
		}

		private ByRefType(Type type, CustomModifiers mods)
			: base(type, mods, Signature.ELEMENT_TYPE_BYREF)
		{
		}

		public override bool Equals(object o)
		{
			return EqualsHelper(o as ByRefType);
		}

		public override int GetHashCode()
		{
			return elementType.GetHashCode() * 3;
		}

		public override Type BaseType => null;

		public override TypeAttributes Attributes => 0;

		public override string GetSuffix()
		{
			return "&";
		}

		protected override Type Wrap(Type type, CustomModifiers mods)
		{
			return Make(type, mods);
		}
	}

	public sealed class PointerType : ElementHolderType
	{
		public static Type Make(Type type, CustomModifiers mods)
		{
			return type.Universe.CanonicalizeType(new PointerType(type, mods));
		}

		private PointerType(Type type, CustomModifiers mods)
			: base(type, mods, Signature.ELEMENT_TYPE_PTR)
		{
		}

		public override bool Equals(object o)
		{
			return EqualsHelper(o as PointerType);
		}

		public override int GetHashCode()
		{
			return elementType.GetHashCode() * 7;
		}

		public override Type BaseType => null;

		public override TypeAttributes Attributes => 0;

		public override string GetSuffix()
		{
			return "*";
		}

		protected override Type Wrap(Type type, CustomModifiers mods)
		{
			return Make(type, mods);
		}
	}

	public sealed class GenericTypeInstance : TypeInfo
	{
		private readonly Type type;
		private readonly Type[] args;
		private readonly CustomModifiers[] mods;
		private Type baseType;
		private int token;

		public static Type Make(Type type, Type[] typeArguments, CustomModifiers[] mods)
		{
			var identity = true;
			if (type is TypeBuilder || type is BakedType || type.__IsMissing)
			{
				// a TypeBuiler identity must be instantiated
				identity = false;
			}
			else
			{
				// we must not instantiate the identity instance, because typeof(Foo<>).MakeGenericType(typeof(Foo<>).GetGenericArguments()) == typeof(Foo<>)
				for (var i = 0; i < typeArguments.Length; i++)
				{
					if (typeArguments[i] != type.GetGenericTypeArgument(i)
						|| !IsEmpty(mods, i))
					{
						identity = false;
						break;
					}
				}
			}
			if (identity)
			{
				return type;
			}
			
			return type.Universe.CanonicalizeType(new GenericTypeInstance(type, typeArguments, mods));
		
		}

		private static bool IsEmpty(CustomModifiers[] mods, int i)
		{
			// we need to be extra careful, because mods doesn't not need to be in canonical format
			// (Signature.ReadGenericInst() calls Make() directly, without copying the modifier arrays)
			return mods == null || mods[i].IsEmpty;
		}

		private GenericTypeInstance(Type type, Type[] args, CustomModifiers[] mods)
		{
			this.type = type;
			this.args = args;
			this.mods = mods;
		}

		public override bool Equals(object o)
		{
			var genericTypeInstance = o as GenericTypeInstance;
			return genericTypeInstance != null && genericTypeInstance.type.Equals(type) 
			                                   && Util.ArrayEquals(genericTypeInstance.args, args) 
			                                   && Util.ArrayEquals(genericTypeInstance.mods, mods);
		}

		public override int GetHashCode()
		{
			return type.GetHashCode() * 3 ^ Util.GetHashCode(args);
		}

		public override string AssemblyQualifiedName
		{
			get
			{
				var fn = FullName;
				return fn == null ? null : fn + ", " + type.Assembly.FullName;
			}
		}

		public override Type BaseType
		{
			get
			{
				if (baseType == null)
				{
					var rawBaseType = type.BaseType;
					if (rawBaseType == null)
					{
						baseType = rawBaseType;
					}
					else
					{
						baseType = rawBaseType.BindTypeParameters(this);
					}
				}
				return baseType;
			}
		}

		protected override bool IsValueTypeImpl => type.IsValueType;
		
		public override bool IsVisible
		{
			get
			{
				if (base.IsVisible)
				{
					return args.All(arg => arg.IsVisible);
				}
				return false;
			}
		}

		public override Type DeclaringType => type.DeclaringType;

		public override TypeAttributes Attributes => type.Attributes;

		public override void CheckBaked()
		{
			type.CheckBaked();
		}

		public override FieldInfo[] __GetDeclaredFields()
		{
			var fields = type.__GetDeclaredFields();
			for (var i = 0; i < fields.Length; i++)
			{
				fields[i] = fields[i].BindTypeParameters(this);
			}
			return fields;
		}

		public override Type[] __GetDeclaredInterfaces()
		{
			var interfaces = type.__GetDeclaredInterfaces();
			for (var i = 0; i < interfaces.Length; i++)
			{
				interfaces[i] = interfaces[i].BindTypeParameters(this);
			}
			return interfaces;
		}

		public override MethodBase[] __GetDeclaredMethods()
		{
			var methodBases = type.__GetDeclaredMethods();
			for (var i = 0; i < methodBases.Length; i++)
			{
				methodBases[i] = methodBases[i].BindTypeParameters(this);
			}
			return methodBases;
		}

		public override Type[] __GetDeclaredTypes()
		{
			return type.__GetDeclaredTypes();
		}

		public override EventInfo[] __GetDeclaredEvents()
		{
			var eventInfos = type.__GetDeclaredEvents();
			for (var i = 0; i < eventInfos.Length; i++)
			{
				eventInfos[i] = eventInfos[i].BindTypeParameters(this);
			}
			return eventInfos;
		}

		public override PropertyInfo[] __GetDeclaredProperties()
		{
			var propertyInfos = type.__GetDeclaredProperties();
			for (var i = 0; i < propertyInfos.Length; i++)
			{
				propertyInfos[i] = propertyInfos[i].BindTypeParameters(this);
			}
			return propertyInfos;
		}

		public override __MethodImplMap __GetMethodImplMap()
		{
			var map = type.__GetMethodImplMap();
			map.TargetType = this;
			for (var i = 0; i < map.MethodBodies.Length; i++)
			{
				map.MethodBodies[i] = (MethodInfo)map.MethodBodies[i].BindTypeParameters(this);
				for (var j = 0; j < map.MethodDeclarations[i].Length; j++)
				{
					var interfaceType = map.MethodDeclarations[i][j].DeclaringType;
					if (interfaceType.IsGenericType)
					{
						map.MethodDeclarations[i][j] = (MethodInfo)map.MethodDeclarations[i][j].BindTypeParameters(this);
					}
				}
			}
			return map;
		}

		public override string Namespace => type.Namespace;

		public override string Name => type.Name;

		public override string FullName
		{
			get
			{
				if (!__ContainsMissingType && this.ContainsGenericParameters)
				{
					return null;
				}
				var stringBuilder = new StringBuilder(this.type.FullName);
				stringBuilder.Append('[');
				var sep = "";
				foreach (var type in args)
				{
					stringBuilder.Append(sep)
									.Append('[')
									.Append(type.FullName)
									.Append(", ")
									.Append(type.Assembly.FullName.Replace("]", "\\]"))
									.Append(']');
					sep = ",";
				}
				stringBuilder.Append(']');
				return stringBuilder.ToString();
			}
		}

		public override string ToString()
		{
			var stringBuilder = new StringBuilder(type.FullName);
			stringBuilder.Append('[');
			var sep = "";
			foreach (var arg in args)
			{
				stringBuilder.Append(sep);
				stringBuilder.Append(arg);
				sep = ",";
			}
			stringBuilder.Append(']');
			return stringBuilder.ToString();
		}

		public override Module Module => type.Module;

		public override bool IsGenericType => true;

		public override bool IsConstructedGenericType => true;

		public override Type GetGenericTypeDefinition()
		{
			return type;
		}

		public override Type[] GetGenericArguments()
		{
			return Util.Copy(args);
		}

		public override CustomModifiers[] __GetGenericArgumentsCustomModifiers()
		{
			return mods != null ? (CustomModifiers[])mods.Clone() : new CustomModifiers[args.Length];
		}

		public override Type GetGenericTypeArgument(int index)
		{
			return args[index];
		}

		public override bool ContainsGenericParameters
		{
			get
			{
				foreach (var type in args)
				{
					if (type.ContainsGenericParameters)
					{
						return true;
					}
				}
				return false;
			}
		}

		protected override bool ContainsMissingTypeImpl => type.__ContainsMissingType || ContainsMissingType(args);

		public override bool __GetLayout(out int packingSize, out int typeSize)
		{
			return type.__GetLayout(out packingSize, out typeSize);
		}

		public override int GetModuleBuilderToken()
		{
			if (token == 0)
			{
				token = ((ModuleBuilder)type.Module).ImportType(this);
			}
			return token;
		}

		public override Type BindTypeParameters(IGenericBinder binder)
		{
			for (var i = 0; i < args.Length; i++)
			{
				var xarg = args[i].BindTypeParameters(binder);
				if (!ReferenceEquals(xarg, args[i]))
				{
					var xargs = new Type[args.Length];
					Array.Copy(args, xargs, i);
					xargs[i++] = xarg;
					for (; i < args.Length; i++)
					{
						xargs[i] = args[i].BindTypeParameters(binder);
					}
					return Make(type, xargs, null);
				}
			}
			return this;
		}

		public override int GetCurrentToken()
		{
			return type.GetCurrentToken();
		}

		public override bool IsBaked => type.IsBaked;
	}

	public sealed class FunctionPointerType : TypeInfo
	{
		private readonly Universe universe;
		private readonly __StandAloneMethodSig sig;

		public static Type Make(Universe universe, __StandAloneMethodSig sig)
		{
			return universe.CanonicalizeType(new FunctionPointerType(universe, sig));
		}

		private FunctionPointerType(Universe universe, __StandAloneMethodSig sig)
			: base(Signature.ELEMENT_TYPE_FNPTR)
		{
			this.universe = universe;
			this.sig = sig;
		}

		public override bool Equals(object obj)
		{
			FunctionPointerType other = obj as FunctionPointerType;
			return other != null
				&& other.universe == universe
				&& other.sig.Equals(sig);
		}

		public override int GetHashCode()
		{
			return sig.GetHashCode();
		}

		public override __StandAloneMethodSig __MethodSignature => sig;

		public override Type BaseType => null;

		public override TypeAttributes Attributes => 0;

		public override string Name => throw new InvalidOperationException();

		public override string FullName => throw new InvalidOperationException();

		public override Module Module
		{
			get { throw new InvalidOperationException(); }
		}

		public override Universe Universe => universe;

		public override string ToString()
		{
			return "<FunctionPtr>";
		}

		protected override bool ContainsMissingTypeImpl => sig.ContainsMissingType;

		public override bool IsBaked => true;

		protected override bool IsValueTypeImpl => false;
	}

	public sealed class MarkerType : Type
	{
		// used by CustomModifiers and SignatureHelper
		public static readonly Type ModOpt = new MarkerType(Signature.ELEMENT_TYPE_CMOD_OPT);
		public static readonly Type ModReq = new MarkerType(Signature.ELEMENT_TYPE_CMOD_REQD);
		// used by SignatureHelper
		public static readonly Type Sentinel = new MarkerType(Signature.SENTINEL);
		public static readonly Type Pinned = new MarkerType(Signature.ELEMENT_TYPE_PINNED);
		// used by ModuleReader.LazyForwardedType and TypeSpec resolution
		public static readonly Type LazyResolveInProgress = new MarkerType(0xFF);

		private MarkerType(byte sigElementType)
			: base(sigElementType)
		{
		}

		public override Type BaseType => throw new InvalidOperationException();

		public override TypeAttributes Attributes => throw new InvalidOperationException();

		public override string Name => throw new InvalidOperationException();

		public override string FullName => throw new InvalidOperationException();

		public override Module Module => throw new InvalidOperationException();

		public override bool IsBaked => throw new InvalidOperationException();

		public override bool __IsMissing => false;

		protected override bool IsValueTypeImpl => throw new InvalidOperationException();
	}
}
