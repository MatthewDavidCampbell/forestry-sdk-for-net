using System.Buffers;
using System.Diagnostics;

namespace Forestry.Turn.Pipeline
{
    /// <summary>
    /// A pipe drives the pipeline of directives to transition the question into an answer
    /// and culture with three distinct phases: question creation, retry and transformation.
    /// </summary>
    public partial class Pipe
    {
  
        internal Pipe(
            Options options
        )
        {
            AnswerAnalyzer = options.AnswerAnalyzer ?? throw new ArgumentNullException(paramName: nameof(options.AnswerAnalyzer));

            _transition = options.Transition ?? throw new ArgumentNullException(paramName: nameof(options.Transition));
            _directives = options.Directives ?? throw new ArgumentNullException(paramName: nameof(options.Directives));

            Debug.Assert(options.Directives[^1] is TransitionDirective);

            _lastQuestionDirectiveIndex = options.LastQuestionDirectiveIndex;
            _lastRetryDirectiveIndex = options.LastRetryDirectiveIndex;
        }

        /// <summary>
        /// Adjacency pair transition
        /// </summary>
        private protected readonly Transition _transition;

        /// <summary>
        /// Directives constituting the pipe
        /// </summary>
        private readonly ReadOnlyMemory<Directive> _directives;

        /// <summary>
        /// Index of last directive for each question
        /// </summary>
        private readonly int _lastQuestionDirectiveIndex;

        /// <summary>
        /// Index of last directive for each retry
        /// </summary>
        private readonly int _lastRetryDirectiveIndex;

        /// <summary>
        /// Answer analyzer
        /// </summary>
        public AnswerAnalyzer AnswerAnalyzer { get; }

        /// <summary>
        /// Delegating the question creation to the <see cref="Transition"/>
        /// </summary>
        public Question CreateQuestion() => _transition.CreateQuestion();

        /// <summary>
        /// Delegating the adjancency pair creation to the <see cref="Transition"/> with this answer analyzer
        /// </summary>
        /// <returns></returns>
        public AdjacencyPair CreateAdjacencyPair() => new(CreateQuestion(), AnswerAnalyzer);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="conversationState"></param>
        /// <returns></returns>s
        public AdjacencyPair CreateAdjacencyPair(ConversationContext conversationState)
        {
            AdjacencyPair adjacencyPair = new(CreateQuestion(), AnswerAnalyzer);

            // TODO: dialog state 

            return adjacencyPair;
        }

        /// <summary>
        /// Transition the adjacency pair 
        /// </summary>
        /// <param name="adjacencyPair"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public ValueTask TransitionAsync(
            AdjacencyPair adjacencyPair,
            CancellationToken cancellationToken
        ) {
            adjacencyPair.CancellationToken = cancellationToken;
            adjacencyPair.ProcessStartTime = DateTime.UtcNow;
            // TODO: dimensions from turn conversation (scoped)

            if (adjacencyPair.PositionedDirectives is null || adjacencyPair.PositionedDirectives.Count == 0)
            {
                return _directives.Span[0].ProcessAsync(adjacencyPair, _directives.Slice(1));
            }

            return TransitionAsync(adjacencyPair);
        }

        /// <summary>
        /// Transition the adjacency pair appending contextual positioned target
        /// </summary>
        /// <param name="adjacencyPair"></param>
        /// <returns></returns>
        private async ValueTask TransitionAsync(
            AdjacencyPair adjacencyPair
        )
        {
            // Renting space for target associated with this pipeline and the context of adjacency pair keeping the turns clean from target
            int count = _directives.Length + adjacencyPair.PositionedDirectives!.Count;
            Directive[] rentedDirectives = ArrayPool<Directive>.Shared.Rent(count);

            try
            {
                ReadOnlyMemory<Directive> accessor = AddTransientDirectives(rentedDirectives, adjacencyPair.PositionedDirectives);
                await accessor.Span[0].ProcessAsync(adjacencyPair, accessor.Slice(1));
            } finally
            {
                ArrayPool<Directive>.Shared.Return(rentedDirectives);
            }
        }

        /// <summary>
        /// Add transient directives (source) with the pipe directives to target
        /// </summary>
        /// <param name="target"></param>
        /// <param name="source"></param>
        /// <returns></returns>
        private ReadOnlyMemory<Directive> AddTransientDirectives(
            Directive[] target,
            List<(PipelineDirectivePosition Position, Directive Directive)> source
        )
        {
            ReadOnlySpan<Directive> accessor = _directives.Span;  // no-cost and stack only
            int transitionDirectiveIndex = accessor.Length - 1;

            // Each question
            accessor[.._lastQuestionDirectiveIndex].CopyTo(target);

            int index = _lastQuestionDirectiveIndex;
            int count = MarkAddedTransientDirectives(target, source, PipelineDirectivePosition.EachQuestion, index);

            // Each retry
            index += count;
            count = _lastRetryDirectiveIndex - _lastQuestionDirectiveIndex;
            accessor.Slice(_lastQuestionDirectiveIndex, count).CopyTo(target.AsSpan(index, count));

            index += count;
            count = MarkAddedTransientDirectives(target, source, PipelineDirectivePosition.EachRetry, index);

            // Before transition
            index += count;
            count = transitionDirectiveIndex - _lastRetryDirectiveIndex;
            accessor.Slice(_lastRetryDirectiveIndex, count).CopyTo(target.AsSpan(index, count));

            index += count;
            count = MarkAddedTransientDirectives(target, source, PipelineDirectivePosition.BeforeTransition, index);

            // Transition
            index += count;
            target[index] = accessor[transitionDirectiveIndex];

            return new ReadOnlyMemory<Directive>(target, 0, index + 1);
        }

        /// <summary>
        /// Add transient directives (source) filtered by position to target then mark last
        /// </summary>
        /// <param name="target"></param>
        /// <param name="source"></param>
        /// <param name="position"></param>
        /// <param name="mark"></param>
        /// <returns></returns>
        private static int MarkAddedTransientDirectives(
            Directive[] target,
            List<(PipelineDirectivePosition Position, Directive Directive)> source,
            PipelineDirectivePosition position,
            int mark
        )
        {
            int count = 0;

            if (source is not null)
            {
                foreach((PipelineDirectivePosition Position, Directive Directive) value in source)
                {
                    if (value.Position == position)
                    {
                        target[mark + count] = value.Directive;
                        count++;
                    }
                }
            }

            return count;
        }
    }
}
