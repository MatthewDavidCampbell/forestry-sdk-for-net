using System;
using System.Diagnostics.Tracing;

namespace Forestry.Snowflake
{
    /// <summary>
    /// Diagnostics with creation rate overflow when creating a suffix
    /// </summary>
    public readonly partial struct Identity
    {
        /// <summary>
        /// Creation rate overflow event name
        /// </summary>
        public const string CreationRateOverflowEventName = "IdentityCreationRateOverflow";

        /// <summary>
        /// Creation rate overflow event counter name
        /// </summary>
        public const string IncrementingCreationRateEventCount = "creation-rate-overflow";

        /// <summary>
        /// Lifetime overflow event name
        /// </summary>
        public const string LifetimeOverflowEventName = "IdentityLifetimeOverflow";

        /// <summary>
        /// When the creation rate exceeds the current time window and overflows
        /// </summary>
        [EventSource(Name = CreationRateOverflowEventName)]
        private sealed class CreationRateOverflowEventSource: EventSource
        {
            public static readonly CreationRateOverflowEventSource Log = new();

            private readonly IncrementingEventCounter _overflow;

            private CreationRateOverflowEventSource()
            {
                _overflow = new IncrementingEventCounter(IncrementingCreationRateEventCount, this)
                {
                    DisplayName = "Creation Rate Overflow",
                    DisplayRateTimeScale = TimeSpan.FromSeconds(1)
                };
            }

            // Counter-only (for dashboards)
            [NonEvent]
            public void NewOverflow() { 
                _overflow.Increment(); 
            } 
            
            // Event with nodeId payload (for diagnostics)
            [Event(1, Level = EventLevel.Warning, Message = "Creation rate overflow on node {0}")] 
            public void NewOverflowNode(byte nodeId) { 
                WriteEvent(1, nodeId); 
            }
        }

        /// <summary>
        /// When the lifetime overflow
        /// </summary>
        [EventSource(Name = LifetimeOverflowEventName)]
        private sealed class LifetimeOverflowEventSource : EventSource {
            public static readonly LifetimeOverflowEventSource Log = new();

            // Event with nodeId payload (for diagnostics)
            [Event(1, Level = EventLevel.Warning, Message = "Lifetime overflow on node {0}")]
            public void NewOverflowNode(long now, long max, byte nodeId)
            {
                WriteEvent(1, now, max, nodeId);
            }
        }
    }
}