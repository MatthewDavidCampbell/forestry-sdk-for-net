namespace Forestry.Turn
{
    /// <summary>
    /// Stream extensions
    /// </summary>
    public static class StreamExtensions
    {
        /// <summary>
        /// Throws if the stream is not a memory stream (i.e. complete) otherwise
        /// translates to transformation friendly byte array from Microsoft
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static BinaryData ToBinaryData(this Stream stream)
        {
            if (stream == null)
            {
                return BinaryData.Empty;
            }

            MemoryStream? content = stream as MemoryStream ?? throw new InvalidOperationException($"Stream not buffered");

            if (content.TryGetBuffer(out ArraySegment<byte> segment))
            {
                return new BinaryData(segment.AsMemory());
            }
            else
            {
                return new BinaryData(content.ToArray());
            }
        }
    }
}
