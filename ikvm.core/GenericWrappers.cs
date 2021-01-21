/*
  Copyright (C) 2009, 2010 Jeroen Frijters

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
using System.Linq;


namespace IKVM.Reflection
{
	// this represents both generic method instantiations and non-generic methods on generic type instantations
	// (this means that it can be a generic method declaration as well as a generic method instance)
	public sealed class GenericMethodInstance : MethodInfo
	{
		private readonly Type declaringType;
		private readonly MethodInfo method;
		private readonly Type[] methodArgs;
		private MethodSignature lazyMethodSignature;

		public GenericMethodInstance(Type declaringType, MethodInfo method, Type[] methodArgs)
		{
			System.Diagnostics.Debug.Assert(!(method is GenericMethodInstance));
			this.declaringType = declaringType;
			this.method = method;
			this.methodArgs = methodArgs;
		}

		public override bool Equals(object obj)
		{
			var other = obj as GenericMethodInstance;
			return other != null
				&& other.method.Equals(method)
				&& other.declaringType.Equals(declaringType)
				&& Util.ArrayEquals(other.methodArgs, methodArgs);
		}

		public override int GetHashCode()
		{
			return declaringType.GetHashCode() * 33 ^ method.GetHashCode() ^ Util.GetHashCode(methodArgs);
		}

		public override Type ReturnType => method.ReturnType.BindTypeParameters(this);

		public override ParameterInfo ReturnParameter => new GenericParameterInfoImpl(this, method.ReturnParameter);

		public override ParameterInfo[] GetParameters()
		{
			ParameterInfo[] parameters = method.GetParameters();
			for (int i = 0; i < parameters.Length; i++)
			{
				parameters[i] = new GenericParameterInfoImpl(this, parameters[i]);
			}
			return parameters;
		}

		public override int ParameterCount => method.ParameterCount;

		public override CallingConventions CallingConvention => method.CallingConvention;

		public override MethodAttributes Attributes => method.Attributes;

		public override MethodImplAttributes GetMethodImplementationFlags()
		{
			return method.GetMethodImplementationFlags();
		}

		public override string Name => method.Name;

		public override Type DeclaringType => declaringType.IsModulePseudoType ? null : declaringType;

		public override Module Module => method.Module;

		public override int MetadataToken => method.MetadataToken;

		public override MethodBody GetMethodBody()
		{
			var methodDefImpl = method as IKVM.Reflection.Reader.MethodDefImpl;
			if (methodDefImpl != null)
			{
				return methodDefImpl.GetMethodBody(this);
			}
			throw new NotSupportedException();
		}

		public override int __MethodRVA => method.__MethodRVA;

		public override MethodInfo MakeGenericMethod(params Type[] typeArguments)
		{
			return new GenericMethodInstance(declaringType, method, typeArguments);
		}

		public override bool IsGenericMethod => method.IsGenericMethod;

		public override bool IsGenericMethodDefinition => method.IsGenericMethodDefinition && methodArgs == null;

		public override bool ContainsGenericParameters
		{
			get
			{
				if (declaringType.ContainsGenericParameters)
				{
					return true;
				}
				if (methodArgs != null)
				{
					foreach (Type type in methodArgs)
					{
						if (type.ContainsGenericParameters)
						{
							return true;
						}
					}
				}
				return false;
			}
		}

		public override MethodInfo GetGenericMethodDefinition()
		{
			if (IsGenericMethod)
			{
				if (IsGenericMethodDefinition)
				{
					return this;
				}
				
				if (declaringType.IsConstructedGenericType)
				{
					return new GenericMethodInstance(declaringType, method, null);
				}
				
				return method;
				
			}
			throw new InvalidOperationException();
		}

		public override MethodBase __GetMethodOnTypeDefinition()
		{
			return method;
		}

		public override Type[] GetGenericArguments()
		{
			if (methodArgs == null)
			{
				return method.GetGenericArguments();
			}
			
			return (Type[])methodArgs.Clone();

		}

		public override Type GetGenericMethodArgument(int index)
		{
			if (methodArgs == null)
			{
				return method.GetGenericMethodArgument(index);
			}
			
			return methodArgs[index];
			
		}

		public override int GetGenericMethodArgumentCount()
		{
			return method.GetGenericMethodArgumentCount();
		}

		public override MethodInfo GetMethodOnTypeDefinition()
		{
			return method.GetMethodOnTypeDefinition();
		}

		public override int ImportTo(Emit.ModuleBuilder module)
		{
			if (methodArgs == null)
			{
				return module.ImportMethodOrField(declaringType, method.Name, method.MethodSignature);
			}
		
			return module.ImportMethodSpec(declaringType, method, methodArgs);
			
		}

		public override MethodSignature MethodSignature => lazyMethodSignature ??= method.MethodSignature.Bind(declaringType, methodArgs);

		public override MethodBase BindTypeParameters(Type type)
		{
			System.Diagnostics.Debug.Assert(methodArgs == null);
			return new GenericMethodInstance(declaringType.BindTypeParameters(type), method, null);
		}

		public override bool HasThis => method.HasThis;

		public override MethodInfo[] __GetMethodImpls()
		{
			MethodInfo[] methods = method.__GetMethodImpls();
			for (int i = 0; i < methods.Length; i++)
			{
				methods[i] = (MethodInfo)methods[i].BindTypeParameters(declaringType);
			}
			return methods;
		}

		public override int GetCurrentToken()
		{
			return method.GetCurrentToken();
		}

		public override bool IsBaked => method.IsBaked;
	}

	public sealed class GenericFieldInstance : FieldInfo
	{
		private readonly Type declaringType;
		private readonly FieldInfo field;

		public GenericFieldInstance(Type declaringType, FieldInfo field)
		{
			this.declaringType = declaringType;
			this.field = field;
		}

		public override bool Equals(object obj)
		{
			GenericFieldInstance other = obj as GenericFieldInstance;
			return other != null && other.declaringType.Equals(declaringType) && other.field.Equals(field);
		}

		public override int GetHashCode()
		{
			return declaringType.GetHashCode() * 3 ^ field.GetHashCode();
		}

		public override FieldAttributes Attributes => field.Attributes;

		public override string Name => field.Name;

		public override Type DeclaringType => declaringType;

		public override Module Module => declaringType.Module;

		public override int MetadataToken => field.MetadataToken;

		public override object GetRawConstantValue()
		{
			return field.GetRawConstantValue();
		}

		public override void __GetDataFromRVA(byte[] data, int offset, int length)
		{
			field.__GetDataFromRVA(data, offset, length);
		}

		public override int __FieldRVA => field.__FieldRVA;

		public override bool __TryGetFieldOffset(out int offset)
		{
			return field.__TryGetFieldOffset(out offset);
		}

		public override FieldInfo __GetFieldOnTypeDefinition()
		{
			return field;
		}

		public override FieldSignature FieldSignature => field.FieldSignature.ExpandTypeParameters(declaringType);

		public override int ImportTo(Emit.ModuleBuilder module)
		{
			return module.ImportMethodOrField(declaringType, field.Name, field.FieldSignature);
		}

		public override FieldInfo BindTypeParameters(Type type)
		{
			return new GenericFieldInstance(declaringType.BindTypeParameters(type), field);
		}

		public override int GetCurrentToken()
		{
			return field.GetCurrentToken();
		}

		public override bool IsBaked => field.IsBaked;
	}

	public sealed class GenericParameterInfoImpl : ParameterInfo
	{
		private readonly GenericMethodInstance method;
		private readonly ParameterInfo parameterInfo;

		public GenericParameterInfoImpl(GenericMethodInstance method, ParameterInfo parameterInfo)
		{
			this.method = method;
			this.parameterInfo = parameterInfo;
		}

		public override string Name => parameterInfo.Name;

		public override Type ParameterType => parameterInfo.ParameterType.BindTypeParameters(method);

		public override ParameterAttributes Attributes => parameterInfo.Attributes;

		public override int Position => parameterInfo.Position;

		public override object RawDefaultValue => parameterInfo.RawDefaultValue;

		public override CustomModifiers __GetCustomModifiers()
		{
			return parameterInfo.__GetCustomModifiers().Bind(method);
		}

		public override bool __TryGetFieldMarshal(out FieldMarshal fieldMarshal)
		{
			return parameterInfo.__TryGetFieldMarshal(out fieldMarshal);
		}

		public override MemberInfo Member => method;

		public override int MetadataToken => parameterInfo.MetadataToken;

		public override Module Module => method.Module;
	}

	public sealed class GenericPropertyInfo : PropertyInfo
	{
		private readonly Type typeInstance;
		private readonly PropertyInfo property;

		public GenericPropertyInfo(Type typeInstance, PropertyInfo property)
		{
			this.typeInstance = typeInstance;
			this.property = property;
		}

		public override bool Equals(object obj)
		{
			GenericPropertyInfo other = obj as GenericPropertyInfo;
			return other != null && other.typeInstance == typeInstance && other.property == property;
		}

		public override int GetHashCode()
		{
			return typeInstance.GetHashCode() * 537 + property.GetHashCode();
		}

		public override PropertyAttributes Attributes => property.Attributes;

		public override bool CanRead => property.CanRead;

		public override bool CanWrite => property.CanWrite;

		private MethodInfo Wrap(MethodInfo method)
		{
			return method == null ? null : new GenericMethodInstance(typeInstance, method, null);
		}

		public override MethodInfo GetGetMethod(bool nonPublic)
		{
			return Wrap(property.GetGetMethod(nonPublic));
		}

		public override MethodInfo GetSetMethod(bool nonPublic)
		{
			return Wrap(property.GetSetMethod(nonPublic));
		}

		public override MethodInfo[] GetAccessors(bool nonPublic)
		{
			var accessors = property.GetAccessors(nonPublic);
			for (var i = 0; i < accessors.Length; i++)
			{
				accessors[i] = Wrap(accessors[i]);
			}
			return accessors;
		}

		public override object GetRawConstantValue()
		{
			return property.GetRawConstantValue();
		}

		public override bool IsPublic => property.IsPublic;

		public override bool IsNonPrivate => property.IsNonPrivate;

		public override bool IsStatic => property.IsStatic;

		public override PropertySignature PropertySignature => property.PropertySignature.ExpandTypeParameters(typeInstance);

		public override string Name => property.Name;

		public override Type DeclaringType => typeInstance;

		public override Module Module => typeInstance.Module;

		public override int MetadataToken => property.MetadataToken;

		public override PropertyInfo BindTypeParameters(Type type)
		{
			return new GenericPropertyInfo(typeInstance.BindTypeParameters(type), property);
		}

		public override bool IsBaked => property.IsBaked;

		public override int GetCurrentToken()
		{
			return property.GetCurrentToken();
		}
	}

	public sealed class GenericEventInfo : EventInfo
	{
		private readonly Type typeInstance;
		private readonly EventInfo eventInfo;

		public GenericEventInfo(Type typeInstance, EventInfo eventInfo)
		{
			this.typeInstance = typeInstance;
			this.eventInfo = eventInfo;
		}

		public override bool Equals(object obj)
		{
			var other = obj as GenericEventInfo;
			return other != null && other.typeInstance == typeInstance && other.eventInfo == eventInfo;
		}

		public override int GetHashCode()
		{
			return typeInstance.GetHashCode() * 777 + eventInfo.GetHashCode();
		}

		public override EventAttributes Attributes => eventInfo.Attributes;

		private MethodInfo Wrap(MethodInfo method)
		{
			return method == null ? null : new GenericMethodInstance(typeInstance, method, null);
		}

		public override MethodInfo GetAddMethod(bool nonPublic)
		{
			return Wrap(eventInfo.GetAddMethod(nonPublic));
		}

		public override MethodInfo GetRaiseMethod(bool nonPublic)
		{
			return Wrap(eventInfo.GetRaiseMethod(nonPublic));
		}

		public override MethodInfo GetRemoveMethod(bool nonPublic)
		{
			return Wrap(eventInfo.GetRemoveMethod(nonPublic));
		}

		public override MethodInfo[] GetOtherMethods(bool nonPublic)
		{
			var otherMethods = eventInfo.GetOtherMethods(nonPublic);
			for (var i = 0; i < otherMethods.Length; i++)
			{
				otherMethods[i] = Wrap(otherMethods[i]);
			}
			return otherMethods;
		}

		public override MethodInfo[] __GetMethods()
		{
			var methods = eventInfo.__GetMethods();
			
			return methods.Select(Wrap).ToArray();
		}

		public override Type EventHandlerType => eventInfo.EventHandlerType.BindTypeParameters(typeInstance);

		public override string Name => eventInfo.Name;

		public override Type DeclaringType => typeInstance;

		public override Module Module => eventInfo.Module;

		public override int MetadataToken => eventInfo.MetadataToken;

		public override EventInfo BindTypeParameters(Type type)
		{
			return new GenericEventInfo(typeInstance.BindTypeParameters(type), eventInfo);
		}

		public override bool IsPublic => eventInfo.IsPublic;

		public override bool IsNonPrivate => eventInfo.IsNonPrivate;

		public override bool IsStatic => eventInfo.IsStatic;

		public override bool IsBaked => eventInfo.IsBaked;

		public override int GetCurrentToken()
		{
			return eventInfo.GetCurrentToken();
		}
	}
}
