namespace Forestry.Turn
{
    /// <summary>
    /// Directive position in each pipe phase
    /// </summary>
    public enum PipelineDirectivePosition
    {
        /// <summary>
        /// Processed for each question either zero or one times
        /// </summary>
        EachQuestion,
        
        /// <summary>
        /// Processed during the retry phase either zero or multiple times
        /// </summary>
        EachRetry,

        /// <summary>
        /// Process before the transition phase either zero or multiple times
        /// </summary>
        BeforeTransition
    }
}
