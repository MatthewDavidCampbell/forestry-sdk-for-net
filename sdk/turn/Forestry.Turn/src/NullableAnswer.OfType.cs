namespace Forestry.Turn
{
    /// <summary>
    /// Nullable typed value wrapping an answer 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class NullableAnswer<T>
    {
        /// <summary>
        /// Asserts when a value exists
        /// </summary>
        public abstract bool HasValue { get; }

        /// <summary>
        /// Typed value
        /// </summary>
        public abstract T? Value { get; }

        /// <summary>
        /// Underlying answer
        /// </summary>
        /// <returns></returns>
        public abstract Answer GetAnswer();

        /// <summary>
        /// Errors + Value asserts as string
        /// </summary>
        /// <returns></returns>
        public override string ToString() => $"Errors: {GetAnswer()?.HasErrors}, Value: {HasValue}";
    }
}
