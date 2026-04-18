namespace Forestry.Deserialize
{
    /// <summary>
    /// Reader
    /// </summary>
    public interface IReader
    {
        /// <summary>
        /// Read
        /// </summary>
        /// <returns></returns>
        public bool Read();

        /// <summary>
        /// Ignore
        /// </summary>
        public void Ignore();

        /// <summary>
        /// Get <see cref="Value"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetValue<T>() where T: Value<T>;
    }
}