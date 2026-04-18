
using System.Diagnostics;
using System.Runtime.ExceptionServices;

namespace Forestry.Turn.Pipeline
{
    /// <summary>
    /// Processes other directives multiple times positioned either as retriable or 
    /// before transitioning
    /// </summary>
    /// <remarks>Default retry pipeline phase</remarks>
    public class RetryDirective : Directive
    {
        public RetryDirective(
            int maximumRetries = RetryOptions.DefaultMaximumRetries,
            Delaying? delayPolicy = default
        ) {
            _maximumRetries = maximumRetries;
            _delayPolicy = delayPolicy ?? Delaying.CreateExponential();
        }

        private readonly int _maximumRetries;

        private readonly Delaying _delayPolicy;

        /// <summary>
        /// Process adjancency pair with remaining directives
        /// </summary>
        /// <param name="adjacencyPair"></param>
        /// <param name="directives"></param>
        /// <exception cref="NotImplementedException"></exception>
        public override void Process(
            AdjacencyPair adjacencyPair, 
            ReadOnlyMemory<Directive> directives
        ) {
            ValueTask task = InternalProcessAsync(adjacencyPair, directives, false);

            if (task.IsCompleted!)
            {
                throw new InvalidOperationException("Retry synchronous value task is not completed");
            }

            task.GetAwaiter().GetResult();
        }

        /// <summary>
        /// Process adjancency pair with remaining directives
        /// </summary>
        /// <param name="adjacencyPair"></param>
        /// <param name="directives"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public override async ValueTask ProcessAsync(
            AdjacencyPair adjacencyPair, 
            ReadOnlyMemory<Directive> directives
        ) {
            await InternalProcessAsync(adjacencyPair, directives, true);
        }

        /// <summary>
        /// Process adjancency pair with remaining directives either synchronous or asynchronous
        /// </summary>
        /// <param name="adjacencyPair"></param>
        /// <param name="directives"></param>
        /// <param name="isAsynchronous"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        private async ValueTask InternalProcessAsync(
            AdjacencyPair adjacencyPair,
            ReadOnlyMemory<Directive> directives,
            bool isAsynchronous
        ) {
            List<Exception>? exceptions = null;

            while (true)
            {
                Exception? actualException = null;
                long beforeTicks = Stopwatch.GetTimestamp();

                // Before processing
                if (isAsynchronous)
                {
                    await BeforeProcessAsync(adjacencyPair);
                }
                else
                {
                     BeforeProcess(adjacencyPair);
                }

                // Process with exception capturing
                try
                {
                    if (isAsynchronous)
                    {
                        await ProcessNextAsync(adjacencyPair, directives);
                    }
                    else
                    {
                        ProcessNext(adjacencyPair, directives);
                    }
                } catch (Exception e)
                {
                    exceptions ??= [];
                    exceptions.Add(e);

                    actualException = e;
                }

                // After processing
                if (isAsynchronous)
                {
                    await AfterProcessAsync(adjacencyPair);
                } else
                {
                    AfterProcess(adjacencyPair);
                }

                long afterTicks = Stopwatch.GetTimestamp();
                double duration = (afterTicks - beforeTicks) / (double)Stopwatch.Frequency;

                // Assert retry with continue else throw last exception
                bool assertRetryable = false;
                if (actualException is not null || (adjacencyPair.HasAnswer && adjacencyPair.Answer.HasErrors))
                {
                    assertRetryable = isAsynchronous ? await AssertRetryableAsync(adjacencyPair, actualException) : AsserRetryable(adjacencyPair, actualException);
                }

                if (assertRetryable)
                {
                    TimeSpan delay = isAsynchronous ? await GetDelayAsync(adjacencyPair) : GetDelay(adjacencyPair);
                    if (delay > TimeSpan.Zero)
                    {
                        if (isAsynchronous)
                        {
                            await WaitAsync(delay, adjacencyPair.CancellationToken);
                        }
                        else
                        {
                            Wait(delay, adjacencyPair.CancellationToken);
                        }
                    }

                    if (adjacencyPair.HasAnswer)
                    {
                        adjacencyPair.Answer.Dispose();
                    }

                    adjacencyPair.RetryCount++;
                    continue;
                }

                // When actual exception after maximum retries then throw
                if (actualException is not null)
                {
                    if (exceptions!.Count == 1)
                    {
                        ExceptionDispatchInfo.Capture(actualException).Throw();
                    }

                    throw new AggregateException("Retry directive failure", exceptions);
                }

                break;
            }
        }

        /// <summary>
        /// Get delay
        /// </summary>
        /// <param name="adjacencyPair"></param>
        /// <returns></returns>
        protected TimeSpan GetDelay(AdjacencyPair adjacencyPair) => InternalGetDelay(adjacencyPair);

        /// <summary>
        /// Get delay
        /// </summary>
        /// <param name="adjacencyPair"></param>
        /// <returns></returns>
        protected ValueTask<TimeSpan> GetDelayAsync(AdjacencyPair adjacencyPair) => new(InternalGetDelay(adjacencyPair));

        /// <summary>
        /// Delegate to delay strategy
        /// </summary>
        /// <param name="adjacencyPair"></param>
        /// <returns></returns>
        private TimeSpan InternalGetDelay(AdjacencyPair adjacencyPair) => _delayPolicy.GetDelay(adjacencyPair.HasAnswer ? adjacencyPair.Answer : default, adjacencyPair.RetryCount + 1);

        /// <summary>
        /// Delegate waiting to <see cref="CancellationToken.WaitHandle"/>
        /// </summary>
        /// <param name="time"></param>
        /// <param name="cancellationToken"></param>
        protected virtual void Wait(TimeSpan time, CancellationToken cancellationToken)
        {
            cancellationToken.WaitHandle.WaitOne(time);
        }

        /// <summary>
        /// Delegate waiting to <see cref="Task.Delay(TimeSpan, CancellationToken)"/>
        /// </summary>
        /// <param name="time"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected virtual async Task WaitAsync(TimeSpan time, CancellationToken cancellationToken)
        {
            await Task.Delay(time, cancellationToken).ConfigureAwait(false);
        }        

        /// <summary>
        /// 
        /// </summary>
        /// <param name="adjacencyPair"></param>
        /// <param name="exception"></param>
        /// <returns></returns>
        protected internal virtual ValueTask<bool> AssertRetryableAsync(AdjacencyPair adjacencyPair, Exception? exception) => new(InternalAssertRetryable(adjacencyPair, exception));

        /// <summary>
        /// 
        /// </summary>
        /// <param name="adjacencyPair"></param>
        /// <param name="exception"></param>
        /// <returns></returns>
        protected internal virtual bool AsserRetryable(AdjacencyPair adjacencyPair, Exception? exception) => InternalAssertRetryable(adjacencyPair, exception);

        /// <summary>
        /// Delegate to the answer analyzer
        /// </summary>
        /// <param name="adjacencyPair"></param>
        /// <param name="exception"></param>
        /// <returns></returns>
        private bool InternalAssertRetryable(
            AdjacencyPair adjacencyPair,
            Exception? exception
        )
        {
            if (adjacencyPair.RetryCount < _maximumRetries)
            {
                if (exception is not null)
                {
                    return adjacencyPair.AnswerAnalyzer.AssertRetryAllowed(exception, adjacencyPair);
                }

                return adjacencyPair.AnswerAnalyzer.AssertRetryAllowed(adjacencyPair);
            } 

            return false;
        }

        /// <summary>
        /// Before asynchronously processing adjacency pair
        /// </summary>
        /// <param name="adjacencyPair"></param>
        /// <remarks>Exceptions bleed</remarks>
        /// <returns></returns>
        protected internal virtual ValueTask BeforeProcessAsync(AdjacencyPair adjacencyPair) => default;

        /// <summary>
        /// Before synchronously processing adjacency pair
        /// </summary>
        /// <param name="adjacencyPair"></param>
        /// <remarks>Exceptions bleed</remarks>
        protected internal virtual void BeforeProcess(AdjacencyPair adjacencyPair) { }

        /// <summary>
        /// After asynchronously processing adjacency pair
        /// </summary>
        /// <param name="adjacencyPair"></param>
        /// <remarks>Exceptions bleed</remarks>
        /// <returns></returns>
        protected internal virtual ValueTask AfterProcessAsync(AdjacencyPair adjacencyPair) => default;

        /// <summary>
        /// After synchronously processing adjacency pair
        /// </summary>
        /// <param name="adjacencyPair"></param>
        /// <remarks>Exceptions bleed</remarks>
        protected internal virtual void AfterProcess(AdjacencyPair adjacencyPair) { }
    }
}
