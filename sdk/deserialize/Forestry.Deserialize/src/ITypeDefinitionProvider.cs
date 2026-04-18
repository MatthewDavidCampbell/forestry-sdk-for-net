namespace Forestry.Deserialize
{
    public interface ITypeDefinitionProvider
    {
        TypeDefinition? GetTypeDefinition(Type type, DeserializeOptions options);
    }
}