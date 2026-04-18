using Forestry.Turn.Pipeline;

namespace Forestry.Turn
{
    /// <summary>
    /// Paired discourses starting with a question and ending with an answer
    /// </summary>
    public sealed class AdjacencyPair: IDisposable
    {
        /// <summary>
        /// Creates a new instanceof <see cref="AdjacencyPair"/>
        /// </summary>
        /// <param name="question"></param>
        /// <param name="answerAnalyser"></param>
        public AdjacencyPair(
            Question question,
            AnswerAnalyzer answerAnalyser
        ) {
            ArgumentNullException.ThrowIfNull(question, nameof(question));

            Question = question;
            AnswerAnalyzer = answerAnalyser;
        }

        /// <summary>
        /// Get <see cref="Question"/> turn
        /// </summary>
        public Question Question { get; }

        /// <summary>
        /// Answer analyzer used by the turn pipeline
        /// </summary>
        public AnswerAnalyzer AnswerAnalyzer { get; set; }

        /// <summary>
        /// Get <see cref="Answer"/> turn throwing an exception when not set
        /// </summary>
        public Answer Answer { 
            get
            {
                if (_answer is null)
                {
                    throw new InvalidOperationException("Transition never set answer");   
                }

                return _answer;
            }
            set => _answer = value;
        }

        private Answer? _answer;

        /// <summary>
        /// Flagging true when answer turn exists
        /// </summary>
        public bool HasAnswer => _answer is not null;

        /// <summary>
        /// Cancellation token used when processing the turns
        /// </summary>
        public CancellationToken CancellationToken { get; internal set; }

        #region Adjacency pair context in a turn
        public AdjacencyPairContext AdjacencyPairContext => new(this);

        /// <summary>
        /// Process start time by directive pipe
        /// </summary>
        internal DateTimeOffset ProcessStartTime { get; set; }

        /// <summary>
        /// Retry count
        /// </summary>
        internal int RetryCount { get; set; }
        #endregion

        /// <summary>
        /// Positioned directives derived from question context (TODO: explain cooralation=
        /// </summary>
        internal List<(PipelineDirectivePosition Position, Directive Directive)>? PositionedDirectives { get; set; }

        /// <summary>
        /// Disposes the question and answer turns
        /// </summary>
        public void Dispose()
        {
            Question.Dispose();

            var answer = Interlocked.Exchange(ref _answer, null);  // avoids multiple threads disposing at the same time with a local reference
            answer?.Dispose();
        }
    }
}
