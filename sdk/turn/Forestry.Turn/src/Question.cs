using System.Diagnostics.CodeAnalysis;

namespace Forestry.Turn
{
    /// <summary>
    /// A question in the turn-talking model is an expectation of an answer making 
    /// up the first half of an adjacency pair.  A transition is responsible for 
    /// creating concrete questions along side handling context and headers in a 
    /// suitable format.
    /// </summary>
    public abstract class Question: IDisposable
    {
        /// <summary>
        /// Question type confers conversational expectations when transitioning
        /// </summary>
        public virtual QuestionType QuestionType { get; set; }

        /// <summary>
        /// Question content
        /// </summary>
        public virtual QuestionContent? Content { get; set; } 

        /// <summary>
        /// Mutable dimensions used when transitioning to an answer
        /// </summary>
        public QuestionDimensions Dimensions => new(this);

        /// <summary>
        /// Try get dimension
        /// </summary>
        /// <remarks>Dimensions with the same name are concatenated with a delimeter</remarks> 
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        protected internal abstract bool TryGetDimension(string name, [NotNullWhen(true)] out string? value);

        /// <summary>
        /// Try get dimension values by name
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
        /// Add dimension to the dimensions collection
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        protected internal abstract void AddDimension(string name, string value);

        /// <summary>
        /// Remove dimension by name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        protected internal abstract bool RemoveDimension(string name);

        /// <summary>
        /// Set dimension value
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        protected internal virtual void SetDimension(string name, string value)
        {
            RemoveDimension(name);
            AddDimension(name, value);
        }

        /// <summary>
        /// Dimensions iterator
        /// </summary>
        /// <returns></returns>
        protected internal abstract IEnumerable<Dimension> EnumerateDimensions();

        /// <summary>
        /// Dispose primarily dimensions and content 
        /// </summary
        public abstract void Dispose();
    }
}
