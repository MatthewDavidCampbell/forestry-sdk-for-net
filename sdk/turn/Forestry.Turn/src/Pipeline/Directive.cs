namespace Forestry.Turn.Pipeline
{
    /// <summary>
    /// Directive in the turn pipline helps the pipeline voluntarily adhere timing, overlap and culture to the 
    /// question then react to the answer 
    /// </summary>
    public abstract class Directive
    {
        /// <summary>
        /// Processes the adjacency pair only altering the question before the next directive
        /// </summary>
        /// <param name="adjacencyPair"></param>
        /// <param name="directives"></param>
        /// <returns></returns>
        public abstract ValueTask ProcessAsync(AdjacencyPair adjacencyPair, ReadOnlyMemory<Directive> directives);

        /// <summary>
        /// Processes the adjacency pair only altering the question before the next directive
        /// </summary>
        /// <param name="adjacencyPair"></param>
        /// <param name="directives"></param>
        public abstract void Process(AdjacencyPair adjacencyPair, ReadOnlyMemory<Directive> directives);

        /// <summary>
        /// Processes the next directive 
        /// </summary>
        /// <param name="adjacencyPair"></param>
        /// <param name="directives"></param>
        /// <returns></returns>
        protected static ValueTask ProcessNextAsync(AdjacencyPair adjacencyPair, ReadOnlyMemory<Directive> directives)
        {
            return directives.Span[0].ProcessAsync(adjacencyPair, directives.Slice(1));
        }

        /// <summary>
        /// Processes the next directive
        /// </summary>
        /// <param name="adjacencyPair"></param>
        /// <param name="directives"></param>
        protected static void ProcessNext(AdjacencyPair adjacencyPair, ReadOnlyMemory<Directive> directives)
        {
            directives.Span[0].Process(adjacencyPair, directives.Slice(1));
        }
    }
}
