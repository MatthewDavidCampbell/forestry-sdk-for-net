namespace Forestry.Deserialize
{
    public static partial class Deserialization
    {
        public static IEnumerable<Value> Deserialize<T>(string media, DeserializeOptions options)
        {
             ArgumentNullException.ThrowIfNull(media);

             TypeDefinition typeDefinition = GetTypeDefinition(typeof(T), options);
        }
    }
}