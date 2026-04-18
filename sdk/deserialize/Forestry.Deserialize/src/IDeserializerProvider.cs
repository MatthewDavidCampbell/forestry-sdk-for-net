namespace Forestry.Deserialize
{
    public interface IDeserializerProvider {
        Dictionary<Type, Deserializer> GetSimpleDeserializers();

        DeserializerFactory[] GetFactoryDeserializers();
    }
}