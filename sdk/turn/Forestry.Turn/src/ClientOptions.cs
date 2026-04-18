using Forestry.Turn.Pipeline;

namespace Forestry.Turn
{
    /// <summary>
    /// Client options for phases of the turn pipeline, the transition and 
    /// an answer analyser i.e. the basics
    /// </summary>
    public abstract class ClientOptions
    {
        public ClientOptions(Transition transition) : this(null, transition) {}

        internal ClientOptions(ClientOptions? options, Transition transition)
        {
            ArgumentNullException.ThrowIfNull(transition, nameof(transition));
            _transition = transition;

            if (options is not null) {
                RetryOptions = new RetryOptions(options.RetryOptions);
            } else
            {
                RetryOptions = new RetryOptions(null);
            }
        }

        /// <summary>
        /// Transition that turns a question to an answer
        /// </summary>
        public Transition Transition {
            get => _transition;
        }

        private Transition _transition;

        /// <summary>
        /// Answer analyzer falling back on a default (very simple) when not set
        /// </summary>
        public AnswerAnalyzer AnswerAnalyzer
        {
            get => _answerAnalyzer;
            set
            {
                _answerAnalyzer = value ?? throw new ArgumentNullException(nameof(value));
            }
        }

        private AnswerAnalyzer _answerAnalyzer = AnswerAnalyzer.Shared;

        /// <summary>
        /// Retry phase options
        /// </summary>
        public RetryOptions RetryOptions { get; }

        /// <summary>
        /// Dimension delimeter for values as collections
        /// </summary>
        /// <remarks>Default == comma</remarks>
        public string DimensionDelimeter { get; set; } = Dimension.DefaultDelimeter;

        /// <summary>
        /// Add directive to the pipe
        /// </summary>
        /// <param name="directive"></param>
        /// <param name="position"></param>
        /// <remarks>
        /// TODO: Restrict duplicates or same directive at different phases
        /// </remarks>
        public void AddDirective(
            Directive directive,
            PipelineDirectivePosition position
        ) {
            if (
                position != PipelineDirectivePosition.EachQuestion &&
                position != PipelineDirectivePosition.EachRetry &&
                position != PipelineDirectivePosition.BeforeTransition
            )
            {
                throw new ArgumentOutOfRangeException(nameof(position), position, null);
            }

            PositionedDirectives ??= [];
            PositionedDirectives.Add((position, directive));
        }

        internal List<(PipelineDirectivePosition Position, Directive Guidance)>? PositionedDirectives { get; private set; }
    }
}
