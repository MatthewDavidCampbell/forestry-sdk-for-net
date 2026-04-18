namespace Biometria.Deserialize.Xml
{
    /// <summary>
    /// Deserialize XML options
    /// </summary>
    public sealed class DeserializeXmlOptions: DeserializeOptions
    {
        /// <summary>
        /// Default options instance
        /// </summary>
        public static readonly DeserializeXmlOptions Default = new DeserializeXmlOptions();

        /// <summary>
        /// Type definition reflective instantiator for XML deserialization
        /// </summary>
        internal override Func<Type, Deserializer, DeserializeOptions, TypeDefinition> TypeDefinitionReflectiveInstantiator => (type, deserializer, options) => new XmlTypeDefinition(type, deserializer, options);

        /// <summary>
        /// Property definition reflective instantiator for XML deserialization
        /// </summary>
        internal override Func<Type, TypeDefinition, DeserializeOptions, PropertyDefinition> PropertyDefinitionReflectiveInstantiator => (type, typeDefinition, options) => new XmlPropertyDefinition(type, typeDefinition, options);

        /// <summary>
        /// Deserializer provider for XML deserialization
        /// </summary>
        internal override IDeserializerProvider DeserializerProvider => XmlDeserializerProvider.Instance;
    }
}