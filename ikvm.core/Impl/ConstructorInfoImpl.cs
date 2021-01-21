namespace IKVM.Reflection
{
    public sealed class ConstructorInfoImpl : ConstructorInfo
    {
        private readonly MethodInfo method;

        public ConstructorInfoImpl(MethodInfo method)
        {
            this.method = method;
        }

        public override bool Equals(object obj)
        {
            var other = obj as ConstructorInfoImpl;
            return other != null && other.method.Equals(method);
        }

        public override int GetHashCode()
        {
            return method.GetHashCode();
        }

        public override MethodInfo GetMethodInfo()
        {
            return method;
        }

        public override MethodInfo GetMethodOnTypeDefinition()
        {
            return method.GetMethodOnTypeDefinition();
        }
    }
}