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
using IKVM.Reflection.Metadata;

namespace IKVM.Reflection.Reader
{
	public sealed class MethodDefImpl : MethodInfo
	{
		private readonly ModuleReader module;
		private readonly int index;
		private readonly TypeDefImpl declaringType;
		private MethodSignature lazyMethodSignature;
		private ParameterInfo returnParameter;
		private ParameterInfo[] parameters;
		private Type[] typeArgs;

		public MethodDefImpl(ModuleReader module, TypeDefImpl declaringType, int index)
		{
			this.module = module;
			this.index = index;
			this.declaringType = declaringType;
		}

		public override MethodBody GetMethodBody()
		{
			return GetMethodBody(this);
		}

		public MethodBody GetMethodBody(IGenericContext context)
		{
			if ((GetMethodImplementationFlags() & MethodImplAttributes.CodeTypeMask) != MethodImplAttributes.IL)
			{
				// method is not IL
				return null;
			}
			int rva = module.MethodDef.records[index].RVA;
			return rva == 0 ? null : new MethodBody(module, rva, context);
		}

		public override int __MethodRVA
		{
			get { return module.MethodDef.records[index].RVA; }
		}

		public override CallingConventions CallingConvention
		{
			get { return this.MethodSignature.CallingConvention; }
		}

		public override MethodAttributes Attributes
		{
			get { return (MethodAttributes)module.MethodDef.records[index].Flags; }
		}

		public override MethodImplAttributes GetMethodImplementationFlags()
		{
			return (MethodImplAttributes)module.MethodDef.records[index].ImplFlags;
		}

		public override ParameterInfo[] GetParameters()
		{
			PopulateParameters();
			return (ParameterInfo[])parameters.Clone();
		}

		private void PopulateParameters()
		{
			if (parameters == null)
			{
				MethodSignature methodSignature = this.MethodSignature;
				parameters = new ParameterInfo[methodSignature.GetParameterCount()];
				int parameter = module.MethodDef.records[index].ParamList - 1;
				int end = module.MethodDef.records.Length > index + 1 ? module.MethodDef.records[index + 1].ParamList - 1 : module.Param.records.Length;
				for (; parameter < end; parameter++)
				{
					int seq = module.Param.records[parameter].Sequence - 1;
					if (seq == -1)
					{
						returnParameter = new ParameterInfoImpl(this, seq, parameter);
					}
					else
					{
						parameters[seq] = new ParameterInfoImpl(this, seq, parameter);
					}
				}
				for (int i = 0; i < parameters.Length; i++)
				{
					if (parameters[i] == null)
					{
						parameters[i] = new ParameterInfoImpl(this, i, -1);
					}
				}
				if (returnParameter == null)
				{
					returnParameter = new ParameterInfoImpl(this, -1, -1);
				}
			}
		}

		public override int ParameterCount
		{
			get { return this.MethodSignature.GetParameterCount(); }
		}

		public override ParameterInfo ReturnParameter
		{
			get
			{
				PopulateParameters();
				return returnParameter;
			}
		}

		public override Type ReturnType
		{
			get
			{
				return this.MethodSignature.GetReturnType(this);
			}
		}

		public override Type DeclaringType
		{
			get { return declaringType.IsModulePseudoType ? null : declaringType; }
		}

		public override string Name
		{
			get { return module.GetString(module.MethodDef.records[index].Name); }
		}

		public override int MetadataToken
		{
			get { return (MethodDefTable.Index << 24) + index + 1; }
		}

		public override bool IsGenericMethodDefinition
		{
			get
			{
				PopulateGenericArguments();
				return typeArgs.Length > 0;
			}
		}

		public override bool IsGenericMethod
		{
			get { return IsGenericMethodDefinition; }
		}

		public override Type[] GetGenericArguments()
		{
			PopulateGenericArguments();
			return Util.Copy(typeArgs);
		}

		private void PopulateGenericArguments()
		{
			if (typeArgs == null)
			{
				int token = this.MetadataToken;
				int first = module.GenericParam.FindFirstByOwner(token);
				if (first == -1)
				{
					typeArgs = Type.EmptyTypes;
				}
				else
				{
					List<Type> list = new List<Type>();
					int len = module.GenericParam.records.Length;
					for (int i = first; i < len && module.GenericParam.records[i].Owner == token; i++)
					{
						list.Add(new GenericTypeParameter(module, i, Signature.ELEMENT_TYPE_MVAR));
					}
					typeArgs = list.ToArray();
				}
			}
		}

		public override Type GetGenericMethodArgument(int index)
		{
			PopulateGenericArguments();
			return typeArgs[index];
		}

		public override int GetGenericMethodArgumentCount()
		{
			PopulateGenericArguments();
			return typeArgs.Length;
		}

		public override MethodInfo GetGenericMethodDefinition()
		{
			if (this.IsGenericMethodDefinition)
			{
				return this;
			}
			throw new InvalidOperationException();
		}

		public override MethodInfo MakeGenericMethod(params Type[] typeArguments)
		{
			return new GenericMethodInstance(declaringType, this, typeArguments);
		}

		public override Module Module => module;

		public override MethodSignature MethodSignature => lazyMethodSignature ?? (lazyMethodSignature = MethodSignature.ReadSig(module, module.GetBlob(module.MethodDef.records[index].Signature), this));

		public override int ImportTo(Emit.ModuleBuilder module)
		{
			return module.ImportMethodOrField(declaringType, this.Name, this.MethodSignature);
		}

		public override MethodInfo[] __GetMethodImpls()
		{
			Type[] typeArgs = null;
			List<MethodInfo> list = null;
			foreach (int i in module.MethodImpl.Filter(declaringType.MetadataToken))
			{
				if (module.MethodImpl.records[i].MethodBody == this.MetadataToken)
				{
					if (typeArgs == null)
					{
						typeArgs = declaringType.GetGenericArguments();
					}
					if (list == null)
					{
						list = new List<MethodInfo>();
					}
					list.Add((MethodInfo)module.ResolveMethod(module.MethodImpl.records[i].MethodDeclaration, typeArgs, null));
				}
			}
			return Util.ToArray(list, Empty<MethodInfo>.Array);
		}

		public override int GetCurrentToken()
		{
			return MetadataToken;
		}

		public override bool IsBaked => true;
	}
}
