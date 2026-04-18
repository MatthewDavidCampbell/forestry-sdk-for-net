namespace Forestry.Deserialize
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract partial class Deserializer<T>: Deserializer
    {
        /// <summary>
        /// Default returns an empty value enumerator
        /// </summary>
        /// <param name="options"></param>
        /// <param name="cancellationToken"></param>
        /// <typeparam name="Value"></typeparam>
        /// <returns></returns>
        protected virtual IAsyncEnumerator<Value> ReadAsync(DeserializeOptions options, CancellationToken cancellationToken = default) => Constants.EmptyAsync<Value>();

        /// <summary>
        /// Default returns an empty value enumerator
        /// </summary>
        /// <param name="options"></param>
        /// <param name="cancellationToken"></param>
        /// <typeparam name="Value"></typeparam>
        /// <returns></returns>
        protected virtual IEnumerator<Value> Read(DeserializeOptions options, CancellationToken cancellationToken = default) => Constants.Empty<Value>();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public override bool CanDeserialize(Type type)
        {
            return type == typeof(T);
        }
    }
}