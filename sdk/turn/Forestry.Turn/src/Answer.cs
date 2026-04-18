using System.Diagnostics.CodeAnalysis;

namespace Forestry.Turn
{
    /// <summary>
    /// An answer in the turn-taking model is a responsive turn completing 
    /// an adjacency pair to a question made up of content, dimensions plus 
    /// essential flags like when errors exist
    /// </summary>
    public abstract class Answer: IDisposable
    {
        /// <summary>
        /// Content stream
        /// </summary>
        public abstract Stream? Content { get; set; }

        /// <summary>
        /// Try get dimension value by name
        /// </summary>
        /// <remarks>Dimensions with the same name are concatenated with a delimeter</remarks>
        /// <param name="name"></param>
        /// <param name="dimension"></param>
        /// <returns></returns>
        protected internal abstract bool TryGetDimension(string name, [NotNullWhen(true)] out string? dimension);

        /// <summary>
        /// Try get dimensionvalues by name
        /// </summary>
        /// <param name="name"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        protected internal abstract bool TryGetDimensionValues(string name, [NotNullWhen(true)] out IEnumerable<string>? values);

        /// <summary>
        /// Assert true when has dimension by name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        protected internal abstract bool ContainsDimension(string name);

        /// <summary>
        /// Dimensions iterator
        /// </summary>
        /// <returns></returns>
        protected internal abstract IEnumerable<Dimension> EnumerateDimensions();

        /// <summary>
        /// Has enough errors that the question failed to transition
        /// </summary>
        public virtual bool HasErrors { get; internal set; }

        /// <summary>
        /// Typed value wrapping an answer where the value is the streamed content
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <param name="answer"></param>
        /// <returns></returns>
        public static Answer<T> FromValue<T>(T value, Answer answer) => new ValueAnswer<T>(answer, value);

        /// <summary>
        /// Delegating dispose
        /// </summary>
        public abstract void Dispose();
    }
}
