namespace Forestry.Turn
{
    /// <summary>
    /// Typed value wrapping an answer
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class Answer<T>: NullableAnswer<T>
    {
        /// <summary>
        /// Asserts allow with value
        /// </summary>
        public override bool HasValue => true;

        /// <summary>
        /// Typed value
        /// </summary>
        public override T Value => Value;

        /// <summary>
        /// Implicit value cast
        /// </summary>
        /// <param name="answer"></param>
        public static implicit operator T(Answer<T> answer)
        {
            ArgumentNullException.ThrowIfNull(answer, nameof(answer));

            return answer.Value;
        }
    }

    /// <summary>
    /// Typed value wrapping an answer 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class ValueAnswer<T>: Answer<T>
    {
        public ValueAnswer(Answer answer, T value)
        {
            _answer = answer;
            Value = value;
        }

        private readonly Answer _answer;

        public override T Value { get; }

        public override Answer GetAnswer() => _answer;
    }
}
