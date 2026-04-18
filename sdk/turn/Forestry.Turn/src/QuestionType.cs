using System.Diagnostics.CodeAnalysis;

namespace Forestry.Turn
{
    /// <summary>
    /// Question type confers conversational expectations when transitioning
    /// </summary>
    public readonly partial struct QuestionType : IEquatable<QuestionType>
    {
        public QuestionType(
            string type
        ) {
            Type = type;
        }

        public string Type { get; }

        /// <summary>
        /// Equivalent <see cref="Type"/> string as bytes to another
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(QuestionType other)
        {
            return string.Equals(Type, other.Type, StringComparison.Ordinal);
        }

        /// <summary>
        /// Object is an equivalent <see cref="QuestionType"/>
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public override bool Equals([NotNullWhen(true)] object? other)
        {
            return other is QuestionType type && Equals(type);
        }

        public static bool operator ==(QuestionType left, QuestionType right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(QuestionType left, QuestionType right)
        {
            return !left.Equals(right);
        }

        /// <summary>
        /// Hash of <see cref="Type"/> string as bytes
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {            
            return Type is null ? 0 : StringComparer.Ordinal.GetHashCode(Type);
        }

        public override string ToString()
        {
            return Type ?? "<null>";
        }
    }
}
