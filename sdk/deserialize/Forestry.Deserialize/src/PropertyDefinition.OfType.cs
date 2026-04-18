namespace Forestry.Deserialize
{
    internal sealed class PropertyDefinition<T>: PropertyDefinition
    {
        internal PropertyDefinition(Type declaringType, TypeDefinition declaringTypeDefinition, DeserializeOptions options): base(typeof(T), declaringType, declaringTypeDefinition, options)
        {
            
        }
    }
}