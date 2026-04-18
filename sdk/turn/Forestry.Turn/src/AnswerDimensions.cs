using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net.Http.Headers;

namespace Forestry.Turn
{
    /// <summary>
    /// Read-only dimensions
    /// </summary>
    public readonly struct AnswerDimensions : IEnumerable<Dimension>
    {
        /// <summary>
        /// Delegates back to the concrete answer with gets help likely
        /// from the transition
        /// </summary>
        /// <param name="answer"></param>
        internal AnswerDimensions(Answer answer)
        {
            _answer = answer;
        }

        private readonly Answer _answer;

        /// <summary>
        /// Content length
        /// </summary>
        public int? ContentLength
        {
            get
            {
                if (!TryGetValue(Dimension.Names.ContentLength, out string? dimension))
                {
                    return null;
                }

                if (!int.TryParse(dimension, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
                {
                    throw new OverflowException($"Failed parsing '{Dimension.Names.ContentLength}' header: '{value}' e.g. when value exceeds {int.MaxValue}");
                }

                return value;
            }
        }

        /// <summary>
        /// Content length
        /// </summary>
        public long? ContentLengthLong => TryGetValue(Dimension.Names.ContentLength, out string? dimension) ? long.Parse(dimension, CultureInfo.InvariantCulture) : null;

        /// <summary>
        /// Try get dimension by name
        /// </summary>
        /// <remarks>Dimensions with the same name are concatenated with a delimeter</remarks>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool TryGetValue(string name, [NotNullWhen(true)] out string? value)
        {
            return _answer.TryGetDimension(name, out value);
        }

        /// <summary>
        /// Try get dimension values by name
        /// </summary>
        /// <param name="name"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        public bool TryGetValues(string name, [NotNullWhen(true)] out IEnumerable<string>? values)
        {
            return _answer.TryGetDimensionValues(name, out values);
        }

        /// <summary>
        /// Assert true when has dimension by name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool Contains(string name)
        {
            return _answer.ContainsDimension(name);
        }

        /// <summary>
        /// Dimensions enumerator
        /// </summary>
        /// <returns></returns>
        public IEnumerator<Dimension> GetEnumerator()
        {
            return _answer.EnumerateDimensions().GetEnumerator();
        }

        /// <summary>
        /// Dimensions enumerator
        /// </summary>
        /// <returns></returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return _answer.EnumerateDimensions().GetEnumerator();
        }
    }
}
