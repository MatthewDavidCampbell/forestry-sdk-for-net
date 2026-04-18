namespace Forestry.Deserialize
{
    /// <summary>
    /// Adjacency pair of <see cref="Value"/> and a deserialized object
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class DeserializedValue<T>(Value value, T deserialized) : Value<T>
    {
        private readonly Value _value = value;

        public override T Deserialized { get; } = deserialized;

        public override Value GetValue() => _value;
    }
}