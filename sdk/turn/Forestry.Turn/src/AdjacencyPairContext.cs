namespace Forestry.Turn
{
    /// <summary>
    /// Adjacency pair context in a turn
    /// </summary>
    public readonly struct AdjacencyPairContext
    {
        internal AdjacencyPairContext(AdjacencyPair adjacencyPair)
        {
            _adjacencyPair = adjacencyPair;
        }


        private readonly AdjacencyPair _adjacencyPair;

        /// <summary>
        /// Turn start time
        /// </summary>
        public DateTimeOffset StartTime { 
            get => _adjacencyPair.ProcessStartTime;
            internal set => _adjacencyPair.ProcessStartTime = value;
        }

        /// <summary>
        /// Retry count
        /// </summary>
        public int RetryCount
        {
            get => _adjacencyPair.RetryCount;
            internal set => _adjacencyPair.RetryCount = value;
        }
    }
}

