namespace Forestry.Deserialize
{
    public sealed partial class TypeDefinition<T>: TypeDefinition
    {
        internal TypeDefinition(Deserializer deserializer, DeserializeOptions options): base(typeof(T), deserializer, options)
        {
            // TODO: Maybe an effective deserializer
        }

        private protected override PropertyDefinition AsPropertyDefinition()
        {
            return new PropertyDefinition<T>(declaringType: typeof(T), declaringTypeDefinition: this, Options)
            {
                TypeDefinition = this  
            };
        }

        private protected override PropertyDefinition CreatePropertyDefinition(TypeDefinition declaringTypeDefinition, Type? declaringType, DeserializeOptions options)
        {
            return new PropertyDefinition<T>(declaringType ?? declaringTypeDefinition.Type, declaringTypeDefinition, options)
            {
                TypeDefinition = this
            };
        }
    }
}