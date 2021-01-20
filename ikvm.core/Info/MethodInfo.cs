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
using System.Diagnostics;
using System.Text;

namespace IKVM.Reflection
{
	public abstract class MethodInfo : MethodBase, IGenericContext, IGenericBinder
	{
		// prevent external subclasses
		internal MethodInfo()
		{
		}

		public sealed override MemberTypes MemberType => MemberTypes.Method;

		public abstract Type ReturnType { get; }
		public abstract ParameterInfo ReturnParameter { get; }

		public virtual MethodInfo MakeGenericMethod(params Type[] typeArguments)
		{
			throw new NotSupportedException(this.GetType().FullName);
		}

		public virtual MethodInfo GetGenericMethodDefinition()
		{
			throw new NotSupportedException(GetType().FullName);
		}

		public override string ToString()
		{
			var stringBuilder = new StringBuilder();
			stringBuilder.Append(ReturnType.Name).Append(' ').Append(Name);
			string sep;
			if (IsGenericMethod)
			{
				stringBuilder.Append('[');
				sep = "";
				foreach (Type arg in GetGenericArguments())
				{
					stringBuilder.Append(sep).Append(arg);
					sep = ", ";
				}
				stringBuilder.Append(']');
			}
			stringBuilder.Append('(');
			sep = "";
			foreach (var parameterInfo in GetParameters())
			{
				stringBuilder.Append(sep).Append(parameterInfo.ParameterType);
				sep = ", ";
			}
			stringBuilder.Append(')');
			return stringBuilder.ToString();
		}

		internal bool IsNewSlot => (Attributes & MethodAttributes.NewSlot) != 0;

		public MethodInfo GetBaseDefinition()
		{
			MethodInfo match = this;
			if (match.IsVirtual)
			{
				for (var type = DeclaringType.BaseType; type != null && !match.IsNewSlot; type = type.BaseType)
				{ 
					var methodInfo = type.FindMethod(Name, MethodSignature) as MethodInfo;
					if (methodInfo != null && methodInfo.IsVirtual)
					{
						match = methodInfo;
					}
				}
			}
			return match;
		}

		public virtual MethodInfo[] __GetMethodImpls()
		{
			throw new NotSupportedException();
		}

		public bool __TryGetImplMap(out ImplMapFlags mappingFlags, out string importName, out string importScope)
		{
			return Module.__TryGetImplMap(GetCurrentToken(), out mappingFlags, out importName, out importScope);
		}

		public ConstructorInfo __AsConstructorInfo()
		{
			return new ConstructorInfoImpl(this);
		}

		Type IGenericContext.GetGenericTypeArgument(int index)
		{
			return DeclaringType.GetGenericTypeArgument(index);
		}

		Type IGenericContext.GetGenericMethodArgument(int index)
		{
			return GetGenericMethodArgument(index);
		}

		internal virtual Type GetGenericMethodArgument(int index)
		{
			throw new InvalidOperationException();
		}

		internal virtual int GetGenericMethodArgumentCount()
		{
			throw new InvalidOperationException();
		}

		internal override MethodInfo GetMethodOnTypeDefinition()
		{
			return this;
		}

		Type IGenericBinder.BindTypeParameter(Type type)
		{
			return this.DeclaringType.GetGenericTypeArgument(type.GenericParameterPosition);
		}

		Type IGenericBinder.BindMethodParameter(Type type)
		{
			return GetGenericMethodArgument(type.GenericParameterPosition);
		}

		internal override MethodBase BindTypeParameters(Type type)
		{
			return new GenericMethodInstance(this.DeclaringType.BindTypeParameters(type), this, null);
		}

		// This method is used by ILGenerator and exists to allow ArrayMethod to override it,
		// because ArrayMethod doesn't have a working MethodAttributes property, so it needs
		// to base the result of this on the CallingConvention.
		internal virtual bool HasThis => !IsStatic;

		internal sealed override MemberInfo SetReflectedType(Type type)
		{
			return new MethodInfoWithReflectedType(type, this);
		}

		internal sealed override List<CustomAttributeData> GetPseudoCustomAttributes(Type attributeType)
		{
			var module = Module;
			var customAttributeDatas = new List<CustomAttributeData>();
			if ((Attributes & MethodAttributes.PinvokeImpl) != 0
				&& (attributeType == null 
				    || attributeType.IsAssignableFrom(module.universe.System_Runtime_InteropServices_DllImportAttribute)))
			{
				ImplMapFlags flags;
				if (__TryGetImplMap(out flags, out var importName, out var importScope))
				{
					customAttributeDatas.Add(CustomAttributeData.CreateDllImportPseudoCustomAttribute(module, 
																										flags, 
																										importName, 
																										importScope, 
																										GetMethodImplementationFlags()));
				}
			}
			if ((GetMethodImplementationFlags() & MethodImplAttributes.PreserveSig) != 0
				&& (attributeType == null 
				    || attributeType.IsAssignableFrom(module.universe.System_Runtime_InteropServices_PreserveSigAttribute)))
			{
				customAttributeDatas.Add(CustomAttributeData.CreatePreserveSigPseudoCustomAttribute(module));
			}
			return customAttributeDatas;
		}
	}

	sealed class MethodInfoWithReflectedType : MethodInfo
	{
		private readonly Type reflectedType;
		private readonly MethodInfo method;

		internal MethodInfoWithReflectedType(Type reflectedType, MethodInfo method)
		{
			Debug.Assert(reflectedType != method.DeclaringType);
			this.reflectedType = reflectedType;
			this.method = method;
		}

		public override bool Equals(object obj)
		{
			var other = obj as MethodInfoWithReflectedType;
			return other != null
				&& other.reflectedType == reflectedType
				&& other.method == method;
		}

		public override int GetHashCode()
		{
			return reflectedType.GetHashCode() ^ method.GetHashCode();
		}

		internal override MethodSignature MethodSignature => method.MethodSignature;

		internal override int ParameterCount => method.ParameterCount;

		public override ParameterInfo[] GetParameters()
		{
			var parameterInfos = method.GetParameters();
			for (var i = 0; i < parameterInfos.Length; i++)
			{
				parameterInfos[i] = new ParameterInfoWrapper(this, parameterInfos[i]);
			}
			return parameterInfos;
		}

		public override MethodAttributes Attributes => method.Attributes;

		public override MethodImplAttributes GetMethodImplementationFlags()
		{
			return method.GetMethodImplementationFlags();
		}

		public override MethodBody GetMethodBody()
		{
			return method.GetMethodBody();
		}

		public override CallingConventions CallingConvention => method.CallingConvention;

		public override int __MethodRVA => method.__MethodRVA;

		public override Type ReturnType => method.ReturnType;

		public override ParameterInfo ReturnParameter => new ParameterInfoWrapper(this, method.ReturnParameter);

		public override MethodInfo MakeGenericMethod(params Type[] typeArguments)
		{
			return SetReflectedType(method.MakeGenericMethod(typeArguments), reflectedType);
		}

		public override MethodInfo GetGenericMethodDefinition()
		{
			return method.GetGenericMethodDefinition();
		}

		public override string ToString()
		{
			return method.ToString();
		}

		public override MethodInfo[] __GetMethodImpls()
		{
			return method.__GetMethodImpls();
		}

		internal override Type GetGenericMethodArgument(int index)
		{
			return method.GetGenericMethodArgument(index);
		}

		internal override int GetGenericMethodArgumentCount()
		{
			return method.GetGenericMethodArgumentCount();
		}

		internal override MethodInfo GetMethodOnTypeDefinition()
		{
			return method.GetMethodOnTypeDefinition();
		}

		internal override bool HasThis => method.HasThis;

		public override Module Module => method.Module;

		public override Type DeclaringType => method.DeclaringType;

		public override Type ReflectedType => reflectedType;

		public override string Name => method.Name;

		internal override int ImportTo(IKVM.Reflection.Emit.ModuleBuilder module)
		{
			return method.ImportTo(module);
		}

		public override MethodBase __GetMethodOnTypeDefinition()
		{
			return method.__GetMethodOnTypeDefinition();
		}

		public override bool __IsMissing => method.__IsMissing;

		internal override MethodBase BindTypeParameters(Type type)
		{
			return method.BindTypeParameters(type);
		}

		public override bool ContainsGenericParameters => method.ContainsGenericParameters;

		public override Type[] GetGenericArguments()
		{
			return method.GetGenericArguments();
		}

		public override bool IsGenericMethod => method.IsGenericMethod;

		public override bool IsGenericMethodDefinition => method.IsGenericMethodDefinition;

		public override int MetadataToken => method.MetadataToken;

		internal override int GetCurrentToken()
		{
			return method.GetCurrentToken();
		}

		internal override bool IsBaked => method.IsBaked;
	}
}
