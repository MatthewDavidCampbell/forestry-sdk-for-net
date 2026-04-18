namespace Forestry.Turn
{
    /// <summary>
    /// Answer analyzer to assert if an answer 
    /// allows retring or has errors
    /// </summary>
    public class AnswerAnalyzer
    {
        /// <summary>
        /// Default dimension name denoting errors
        /// </summary>
        public const string DefaultHasErrosDimensionName = "has-errors";

        /// <summary>
        /// Shared (non-derived)
        /// </summary>
        internal static AnswerAnalyzer Shared { get; } = new();

        /// <summary>
        /// Asserts when retries are allowed for the answer in the adjancency pair
        /// </summary>
        /// <param name="adjacencyPair"></param>
        /// <returns></returns>
        public virtual bool AssertRetryAllowed(
            AdjacencyPair adjacencyPair
        ) {
            return false;
        }

        /// <summary>
        /// Asserts when retries are allowed for the exception
        /// </summary>
        /// <param name="exception"></param>
        /// <returns></returns>
        public virtual bool AssertRetryAllowed(
            Exception exception
        ) {
            return exception is IOException;
        }

        /// <summary>
        /// Asserts when retries are allowed for the exception with the adacency pair
        /// </summary>
        /// <param name="exception"></param>
        /// <param name="adjacencyPair"></param>
        /// <returns></returns>
        public virtual bool AssertRetryAllowed(
            Exception exception, 
            AdjacencyPair adjacencyPair
        ) {
            return 
                AssertRetryAllowed(exception) || 
                (exception is OperationCanceledException && !adjacencyPair.CancellationToken.IsCancellationRequested);
        }

        /// <summary>
        /// Assert when answer has errors
        /// </summary>
        /// <param name="adjacencyPair"></param>
        /// <returns></returns>
        public virtual bool AssertHasErrors(AdjacencyPair adjacencyPair)
        {
            return adjacencyPair.HasAnswer && adjacencyPair.Answer.TryGetDimension(DefaultHasErrosDimensionName, out _);            
        }
    }
}
