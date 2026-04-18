namespace Forestry.Turn
{
    /// <summary>
    /// Transition in the turn pipeline sets an answer by processing the question made of content plus dimensions
    /// </summary>
    public abstract class Transition
    {
        /// <summary>
        /// Processes the question in the <paramref name="adjacencyPair"/> into an answer
        /// </summary>
        /// <param name="adjacencyPair"></param>
        public abstract void Process(AdjacencyPair adjacencyPair);

        /// <summary>
        /// Processes the question in the <paramref name="adjacencyPair"/> into an answer
        /// </summary>
        /// <param name="adjacencyPair"></param>
        /// <returns></returns>
        public abstract ValueTask ProcessAsync(AdjacencyPair adjacencyPair);

        /// <summary>
        /// Creates a question that this transition can turn
        /// </summary>
        /// <returns></returns>
        protected internal abstract Question CreateQuestion();
    }
}
