namespace Forestry.Deserialize
{
    /// <summary>
    /// Internal <see cref="Value"/> enumerator 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IValueAsyncEnumerator<T>: IAsyncDisposable where T: Value
    {
        /// <summary>
        /// Current <see cref="Value"/>
        /// </summary>
        /// <value></value>
        T Current { get; }

        /// <summary>
        /// Move next with the stack and reader targeting the 
        /// passed type getting help from options
        /// </summary>
        /// <param name="readStack"></param>
        /// <param name="reader"></param>
        /// <param name="type"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        ValueTask<bool> MoveNextAsync(scoped ref IReadStack readStack, ref IReader reader, Type type, DeserializeOptions options);
    }
}
