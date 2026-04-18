
namespace Forestry.Turn.Tests
{
    public class TestingTransition : Transition
    {
        private readonly Func<AdjacencyPair, TestingAnswer> _transition;

        private readonly object _answering = new();

        /// <summary>
        /// Turn each answer blindly
        /// </summary>
        /// <param name="answers"></param>
        public TestingTransition(params TestingAnswer[] answers)
        {
            int index = 0;
            _transition = _ =>
            {
                lock (_answering)
                {
                    return answers[index++];
                }
            };
        }

        /// <summary>
        /// Turn question into an answer
        /// </summary>
        /// <param name="transition"></param>
        public TestingTransition(Func<TestingQuestion, TestingAnswer> transition): this(turn => transition((TestingQuestion)turn.Question)) { }

        /// <summary>
        /// Turn adjacency pair into an answer
        /// </summary>
        /// <param name="transition"></param>
        private TestingTransition(Func<AdjacencyPair, TestingAnswer> transition)
        {
            _transition = transition;
        }

        /// <summary>
        /// Set asynchronous processing
        /// </summary>
        public bool? IsAsynchronously { get; set; }

        public override void Process(AdjacencyPair adjacencyPair)
        {
            if (IsAsynchronously == true)
            {
                throw new InvalidOperationException("Expecting sychronous processing");
            }

            InternalProccessAsync(adjacencyPair).GetAwaiter().GetResult();
        }

        public override async ValueTask ProcessAsync(AdjacencyPair adjacencyPair)
        {
            if (IsAsynchronously == false)
            {
                throw new InvalidOperationException("Expecting asychronous processing");
            }

            await InternalProccessAsync(adjacencyPair);
        }

        protected override Question CreateQuestion()
        {
            return new TestingQuestion();
        }

        private Task InternalProccessAsync(AdjacencyPair adjacencyPair)
        {
            return ValueTask.CompletedTask.AsTask();
        }
    }
}
