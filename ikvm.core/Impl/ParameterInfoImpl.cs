using System;
using System.Collections.Generic;
using IKVM.Reflection.Metadata;

namespace IKVM.Reflection.Reader
{
    public sealed class ParameterInfoImpl : ParameterInfo
    {
        private readonly MethodDefImpl method;
        private readonly int position;
        private readonly int index;

        public ParameterInfoImpl(MethodDefImpl method, int position, int index)
        {
            this.method = method;
            this.position = position;
            this.index = index;
        }

        public override string Name => index == -1 ? null : ((ModuleReader)this.Module).GetString(this.Module.Param.records[index].Name);

        public override Type ParameterType => position == -1 ? method.MethodSignature.GetReturnType(method) : method.MethodSignature.GetParameterType(method, position);

        public override ParameterAttributes Attributes => index == -1 ? ParameterAttributes.None : (ParameterAttributes)this.Module.Param.records[index].Flags;

        public override int Position => position;

        public override object RawDefaultValue
        {
            get
            {
                if ((this.Attributes & ParameterAttributes.HasDefault) != 0)
                {
                    return this.Module.Constant.GetRawConstantValue(this.Module, this.MetadataToken);
                }
                Universe universe = this.Module.universe;
                if (this.ParameterType == universe.System_Decimal)
                {
                    Type attr = universe.System_Runtime_CompilerServices_DecimalConstantAttribute;
                    if (attr != null)
                    {
                        foreach (CustomAttributeData cad in CustomAttributeData.__GetCustomAttributes(this, attr, false))
                        {
                            IList<CustomAttributeTypedArgument> args = cad.ConstructorArguments;
                            if (args.Count == 5)
                            {
                                if (args[0].ArgumentType == universe.System_Byte
                                    && args[1].ArgumentType == universe.System_Byte
                                    && args[2].ArgumentType == universe.System_Int32
                                    && args[3].ArgumentType == universe.System_Int32
                                    && args[4].ArgumentType == universe.System_Int32)
                                {
                                    return new Decimal((int)args[4].Value, (int)args[3].Value, (int)args[2].Value, (byte)args[1].Value != 0, (byte)args[0].Value);
                                }
                                else if (args[0].ArgumentType == universe.System_Byte
                                         && args[1].ArgumentType == universe.System_Byte
                                         && args[2].ArgumentType == universe.System_UInt32
                                         && args[3].ArgumentType == universe.System_UInt32
                                         && args[4].ArgumentType == universe.System_UInt32)
                                {
                                    return new Decimal(unchecked((int)(uint)args[4].Value), unchecked((int)(uint)args[3].Value), unchecked((int)(uint)args[2].Value), (byte)args[1].Value != 0, (byte)args[0].Value);
                                }
                            }
                        }
                    }
                }
                if ((this.Attributes & ParameterAttributes.Optional) != 0)
                {
                    return Missing.Value;
                }
                return null;
            }
        }

        public override CustomModifiers __GetCustomModifiers()
        {
            return position == -1
                ? method.MethodSignature.GetReturnTypeCustomModifiers(method)
                : method.MethodSignature.GetParameterCustomModifiers(method, position);
        }

        public override bool __TryGetFieldMarshal(out FieldMarshal fieldMarshal)
        {
            return FieldMarshal.ReadFieldMarshal(this.Module, this.MetadataToken, out fieldMarshal);
        }

        public override MemberInfo Member
        {
            get
            {
                // return the right ConstructorInfo wrapper
                return method.Module.ResolveMethod(method.MetadataToken);
            }
        }

        public override int MetadataToken
        {
            get
            {
                // for parameters that don't have a row in the Param table, we return 0x08000000 (because index is -1 in that case),
                // just like .NET
                return (ParamTable.Index << 24) + index + 1;
            }
        }

        public override Module Module
        {
            get { return method.Module; }
        }
    }
}