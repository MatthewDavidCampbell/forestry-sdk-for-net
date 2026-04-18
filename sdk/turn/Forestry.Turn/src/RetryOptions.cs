namespace Forestry.Turn
{
    /// <summary>
    /// Retry directive (pipe phases) options
    /// </summary>
    /// <remarks>Originate from client options alternatively testing</remarks>
    public class RetryOptions
    {
        /// <summary>
        /// Inheriting retry options when not null
        /// </summary>
        /// <param name="options"></param>
        internal RetryOptions(RetryOptions? options) { 
            if (options is not null)
            {
                MaximumRetries = options.MaximumRetries;
                PhaseDelay = options.PhaseDelay;
                MaximumDelay = options.MaximumDelay;
                DirectiveDelay = options.DirectiveDelay;
            }
        }

        /// <summary>
        /// Default maximum retries
        /// </summary>
        internal const int DefaultMaximumRetries = 3;

        /// <summary>
        /// Default maximum retry phase delay is one minute
        /// </summary>
        internal static readonly TimeSpan DefaultMaximumPhaseDelay = TimeSpan.FromMinutes(1);

        /// <summary>
        /// Default retry phase delay is 1 second
        /// </summary>
        internal static readonly TimeSpan DefaultPhaseDelay = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Default directive delay is 100 seconds
        /// </summary>
        internal static readonly TimeSpan DefaultDirectiveDelay = TimeSpan.FromSeconds(100);

        /// <summary>
        /// Maximum times the retry phase is processed before breaking
        /// </summary>
        public int MaximumRetries { get; set; } = DefaultMaximumRetries;

        /// <summary>
        /// Delay between each time the retry phase is processed
        /// </summary>
        public TimeSpan PhaseDelay { get; set; } = DefaultPhaseDelay;

        /// <summary>
        /// Maximum delay between each time the retry phase is processed
        /// </summary>
        public TimeSpan MaximumDelay {  get; set; } = DefaultMaximumPhaseDelay;

        /// <summary>
        /// Delay between each time a retry positioned directive is processed
        /// </summary>
        public TimeSpan DirectiveDelay { get; set; } = DefaultDirectiveDelay;
    }
}
