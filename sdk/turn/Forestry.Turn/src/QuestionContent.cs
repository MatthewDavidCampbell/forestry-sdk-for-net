using System.Buffers;
using System.Text;

namespace Forestry.Turn
{
    /// <summary>
    /// Question content that maybe be copied 
    /// </summary>
    public abstract class QuestionContent : IDisposable
    {
        /// <summary>
        /// Create question content from a stream
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static QuestionContent Create(Stream stream, int copyBufferLength = 81920) => new StreamContent(stream, copyBufferLength);

        /// <summary>
        /// Create question content from a byte array
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="startingPosition"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static QuestionContent Create(byte[] bytes) => new ArrayContent(bytes, 0, bytes.Length);

        /// <summary>
        /// Create question content from a string
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static QuestionContent Create(string text) => Create(_defaultStringEncoding.GetBytes(text));

        /// <summary>
        /// Default UTF8 string encoding
        /// </summary>
        private static readonly Encoding _defaultStringEncoding = new UTF8Encoding(false);

        /// <summary>
        /// Copy context to the passed stream
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public abstract Task CopyToAsync(Stream stream, CancellationToken cancellation);

        /// <summary>
        /// Copy context to the passed stream
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="cancellation"></param>
        public abstract void CopyTo(Stream stream, CancellationToken cancellation);

        /// <summary>
        /// Try get content length
        /// </summary>
        /// <param name="length"></param>
        /// <returns></returns>
        public abstract bool TryGetLength(out long length);

        /// <summary>
        /// Dispose 
        /// </summary>
        public abstract void Dispose();

        /// <summary>
        /// Stream content
        /// </summary>
        private sealed class StreamContent: QuestionContent
        {
            /// <summary>
            /// Passed stream must be seekable
            /// </summary>
            /// <param name="stream"></param>
            /// <exception cref="ArgumentException"></exception>
            public StreamContent(
                Stream stream,
                int copyBufferLength
            ) {
                ArgumentNullException.ThrowIfNull(stream);

                if (stream.CanSeek)
                {
                    throw new ArgumentException("Stream must support seeking", nameof(stream));
                }

                if (copyBufferLength <= 0)
                {
                    throw new ArgumentException("Copy buffer length must be positive", nameof(copyBufferLength));
                }

                _stream = stream;
                _startingPosition = _stream.Position;

                _copyBufferLength = copyBufferLength;
            }

            private readonly Stream _stream;

            private readonly long _startingPosition;

            private int _copyBufferLength = 81920;

            public override async Task CopyToAsync(
                Stream stream, 
                CancellationToken cancellation
            ) {
                _stream.Seek(_startingPosition, SeekOrigin.Begin);
                await _stream.CopyToAsync(stream, _copyBufferLength, cancellation).ConfigureAwait(false);
            }

            public override void CopyTo(
                Stream stream, 
                CancellationToken cancellation
            ) {
                _stream.Seek(_startingPosition, SeekOrigin.Begin);

                byte[] rented = ArrayPool<byte>.Shared.Rent(_copyBufferLength);
                try
                {
                    while (true)
                    {
                        // throw before read when cancelled
                        cancellation.ThrowIfCancellationRequested();

                        int length = _stream.Read(rented, 0, rented.Length);
                        if (length == 0)
                        {
                            break;
                        }

                        // throw before write when cancelled
                        cancellation.ThrowIfCancellationRequested();
                        stream.Write(rented, 0, length);
                    }
                }
                finally
                {
                    stream.Flush();
                    ArrayPool<byte>.Shared.Return(rented, true);
                }
            }

            public override bool TryGetLength(out long length)
            {
                if (_stream.CanSeek)
                {
                    length = _stream.Length - _startingPosition;
                    return true;
                }

                length = 0;
                return false;
            }

            public override void Dispose()
            {
                _stream.Dispose();
            }
        }

        /// <summary>
        /// Byte array content
        /// </summary>
        private sealed class ArrayContent : QuestionContent
        {
            public ArrayContent(
                byte[] bytes, 
                int startingPosition, 
                int length
            )
            {
                _bytes = bytes;
                _startingPosition = startingPosition;
                _length = length;
            }

            private readonly byte[] _bytes;

            private readonly int _startingPosition;

            private readonly int _length;

            public override async Task CopyToAsync(
                Stream stream, 
                CancellationToken cancellation
            )
            {
                await stream.WriteAsync(_bytes.AsMemory(_startingPosition, _length), cancellation).ConfigureAwait(false);
            }

            public override void CopyTo(
                Stream stream, 
                CancellationToken cancellation
            ) {
                stream.Write(_bytes, _startingPosition, _length);
            }

            public override bool TryGetLength(out long length)
            {
                length = _length;
                return true;
            }

            public override void Dispose() { }
        }
    }
}
