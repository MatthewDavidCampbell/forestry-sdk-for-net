using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Forestry.Turn
{
    /// <summary>
    /// Mutable dimensions used when transitioning to an answer
    /// </summary>
    public readonly struct QuestionDimensions: IEnumerable<Dimension>
    {
        /// <summary>
        /// Operations on dimensions are delegated to the concrete question which 
        /// will likely get help from the transition e.g. formatting
        /// </summary>
        /// <param name="question"></param>
        internal QuestionDimensions(
            Question question
        ) {
            ArgumentNullException.ThrowIfNull(question);

            _question = question;
        }

        private readonly Question _question;

        public IEnumerator<Dimension> GetEnumerator()
        {
            return _question.EnumerateDimensions().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _question.EnumerateDimensions().GetEnumerator();
        }

        /// <summary>
        /// Add dimension
        /// </summary>
        /// <param name="dimension"></param>
        public void Add(Dimension dimension)
        {
            _question.AddDimension(dimension.Name, dimension.Value);
        }

        /// <summary>
        /// Add dimension from a name and value pair
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public void Add(string name, string value) { 
            _question.AddDimension(name, value);
        }

        /// <summary>
        /// Try get dimension value by name
        /// </summary>
        /// <remarks>Dimensions with the same name are concatenated with a delimeter</remarks> 
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool TryGetValue(string name, [NotNullWhen(true)] out string? value)
        {
            return _question.TryGetDimension(name, out value);
        }

        /// <summary>
        /// Try get values by name
        /// </summary>
        /// <param name="name"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        public bool TryGetValues(string name, [NotNullWhen(true)] out IEnumerable<string>? values)
        {
            return _question.TryGetDimensionValues(name, out values);
        }

        /// <summary>
        /// Dimension with name argument exists
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool Contains(string name)
        {
            return _question.ContainsDimension(name);   
        }

        /// <summary>
        /// Set value by update when name exists else add
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public void SetValue(string name, string value)
        {
            _question.SetDimension(name, value);
        }

        /// <summary>
        /// True when dimension is removed by name otherwise fale if dimension does not exist
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool Remove(string name)
        {
            return _question.RemoveDimension(name);
        }
    }
}
