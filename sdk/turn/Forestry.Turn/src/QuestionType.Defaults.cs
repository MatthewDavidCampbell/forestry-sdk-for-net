namespace Forestry.Turn
{
    /// <summary>
    /// Question type defaults helpful for interaction
    /// </summary>
    public readonly partial struct QuestionType : IEquatable<QuestionType>
    {
        /// <summary>
        /// Single-turn interactions yield an answer solely based on the question
        /// </summary>
        public static readonly QuestionType SingleTurn = new QuestionType("single-turn");

        /// <summary>
        /// Multi-turn signales ongoing dialogues with multiple exchanges
        /// </summary>
        public static readonly QuestionType MultiTurn = new QuestionType("multi-turn");
    }
}
