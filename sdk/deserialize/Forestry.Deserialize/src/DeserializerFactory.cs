namespace Forestry.Deserialize
{
    /// <summary>
    /// A deserializer factory enables generics e.g. generic lists
    /// </summary>
    public abstract class DeserializerFactory: Deserializer
    {
        protected internal override DeserializerKind GetDeserializerKind() => DeserializerKind.None;

        public abstract Deserializer? CreateDeserializer(Type type, DeserializeOptions options);
    }
}