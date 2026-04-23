namespace Forestry.Deserialize
{
    /// <summary>
    /// Reader implementations are driven by concrete deserializers 
    /// using the stack and stack frames to parse a particular media
    /// </summary>
    public interface IReader
    {
        /// <summary>
        /// Read only when yielding true
        /// </summary>
        public bool Read();

        /// <summary>
        /// Skip
        /// </summary>
        public void Skip();

        /// <summary> 
        /// Try skip
        /// </summary>
        public bool TrySkip();

        /// <summary>
        /// Get <see cref="Value"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetValue<T>() where T: Value<T>;

        /// <summary>
        /// Try get <see cref="Value"/>
        /// </summary>
        /// <param name="value"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public bool TryGetValue<T>(out T value) where T : Value<T>;
    }
}