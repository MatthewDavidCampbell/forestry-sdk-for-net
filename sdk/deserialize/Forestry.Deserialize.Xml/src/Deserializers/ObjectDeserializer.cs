namespace Biometria.Deserialize.Xml.Deserializers
{
    public class ObjectDeserializer : Deserializer
    {
        public ObjectDeserializer(DeserializeOptions options) : base(options)
        {
        }

        protected override object Deserialize(Type type, object value)
        {
            var typeDefinition = Options.TypeDefinitionReflectiveInstantiator(type, this, Options);
            return typeDefinition.Deserialize(value);
        }
    }    
}
    