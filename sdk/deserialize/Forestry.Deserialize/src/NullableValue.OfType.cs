namespace Forestry.Deserialize
{
    /// <summary>
    /// Nullable typed value
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class NullableValue<T>
    {
        private const string NoValue = "<null>";

        public abstract bool HasValue { get; }

        public abstract T? Deserialized { get; }

        public abstract Value GetValue();

        public override string ToString() => $"Value: {(HasValue ? Deserialized : NoValue)}";
    }
}