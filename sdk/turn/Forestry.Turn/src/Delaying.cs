namespace Forestry.Turn
{
    /// <summary>
    /// Delaying inside turns i.e. operating on adjacency pairs
    /// </summary>
    public abstract class Delaying
    {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="maximumPhaseDelay"></param>
        protected Delaying(
            TimeSpan? maximumPhaseDelay = default
        ) {
            _maximumPhaseDelay = maximumPhaseDelay ?? RetryOptions.DefaultMaximumPhaseDelay;
        }

        private readonly TimeSpan _maximumPhaseDelay;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="answer"></param>
        /// <param name="policyCall"></param>
        /// <returns></returns>
        public TimeSpan GetDelay(
            Answer? answer,
            int policyCall
        )
        {
            // TODO: Answer dimensions
            TimeSpan calculatedDelay = CalculateDelay(answer, policyCall);
            return calculatedDelay;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="answer"></param>
        /// <param name="policyCall"></param>
        /// <returns></returns>
        protected abstract TimeSpan CalculateDelay(
            Answer? answer,
            int policyCall
        );

        /// <summary>
        /// Create fixed delay policy
        /// </summary>
        /// <param name="delay"></param>
        /// <returns></returns>
        public static Delaying CreateFixed(TimeSpan? delay = default)
        {
            return new FixedDelaying(delay ?? RetryOptions.DefaultPhaseDelay);
        }

        /// <summary>
        /// Create exponential delay policy
        /// </summary>
        /// <param name="phaseDelay"></param>
        /// <param name="maximumPhaseDelay"></param>
        /// <returns></returns>
        public static Delaying CreateExponential(TimeSpan? phaseDelay = default, TimeSpan? maximumPhaseDelay = default) { 
            phaseDelay ??= RetryOptions.DefaultPhaseDelay;
            maximumPhaseDelay ??= RetryOptions.DefaultMaximumPhaseDelay;

            return new ExponentialDelaying(phaseDelay, maximumPhaseDelay);
        }
    }

    /// <summary>
    /// Fixed delaying
    /// </summary>
    internal class FixedDelaying : Delaying
    {
        public FixedDelaying(TimeSpan delay): base(TimeSpan.FromMicroseconds(delay.TotalMilliseconds))
        {
            _delay = delay;
        }

        private readonly TimeSpan _delay;

        protected override TimeSpan CalculateDelay(
            Answer? answer,
            int policyCall
        ) => _delay;
    }

    /// <summary>
    /// Exponential delaying
    /// </summary>
    internal class ExponentialDelaying: Delaying
    {
        public ExponentialDelaying(TimeSpan? delay = default, TimeSpan? maximumDelay = default): base(maximumDelay)
        {
            _delay = delay ?? RetryOptions.DefaultPhaseDelay;
        }

        private readonly TimeSpan _delay;

        protected override TimeSpan CalculateDelay(
            Answer? answer,
            int policyCall
        ) => TimeSpan.FromMilliseconds((1 << (policyCall - 1)) * _delay.TotalMilliseconds);
    }
}
