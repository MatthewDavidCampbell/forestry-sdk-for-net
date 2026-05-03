using System.Diagnostics.Tracing;
using System.Fabric;

namespace Forestry.Raindrop.Tests
{
    /// <summary>
    /// Node Id example with Service Fabric that could be used 
    /// with Microsoft Options Monitor <see cref="https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.options.optionsmonitor-1?view=net-10.0-pp"/>
    /// </summary>
    internal static class ServiceFabric
    {
        /// <summary>
        /// Event when Service Fabric node id is resolved
        /// </summary>
        public const string NodeIdEventName = "Forestry-ServiceFabric-NodeId";

        /// <summary>
        /// Node id event source
        /// </summary>
        [EventSource(Name = NodeIdEventName)]
        private sealed class NodeIdEventSource : EventSource
        {
            public static readonly NodeIdEventSource Log = new();

            [Event(1, Level = EventLevel.Informational, Message = "Node id {0}")]
            public void NewNodeId(byte nodeId) => WriteEvent(1, nodeId);

            [Event(2, Level = EventLevel.Informational, Message = "Node name {0}")]
            public void NodeName(string name) => WriteEvent(2, name);
        }

        /// <summary>
        /// Listener <see cref="EventSource"/> with name <see cref="NodeIdEventName"/> to 
        /// log node id to a file in the Service Fabric log directory. 
        /// </summary>
        public  sealed class NodeIdListener : EventListener {

            private const string _logFileName = "node-id.log";

            private string _path = Path.Combine(FabricRuntime.GetActivationContext().LogDirectory, _logFileName);

            protected override void OnEventSourceCreated(EventSource eventSource)
            {
                if (eventSource.Name == NodeIdEventName)
                {
                    EnableEvents(eventSource, EventLevel.Warning); 
                }
            }

            protected override void OnEventWritten(EventWrittenEventArgs arguments)
            {
                if (arguments.EventSource.Name == NodeIdEventName && arguments is not null && arguments.Payload is not null && arguments.Payload.Count > 0)
                {
                    File.AppendAllText(_path, $"Node event [id: {arguments.Payload[0]}]");
                }
            }
        }

        /// <summary>
        /// Lazy evaluation of the last character of the Service Fabric 
        /// node which should be a number e.g. // Example: "_domain-sf-cluster-vmss_3"
        /// </summary>
        /// <remarks>Hard coded that the node id must be between 0-4 (max)</remarks>
        private static readonly Lazy<int> _nodeIndex = new(() =>
        {
            string name = FabricRuntime.GetNodeContext().NodeName;
            NodeIdEventSource.Log.NodeName(name);

            char last = name[^1];

            if (!char.IsDigit(last))
                throw new InvalidOperationException($"Node name does not end with a digit: {name}");

            int value = last - '0';  // as integer

            if (value < 0 || value > 4)
                throw new InvalidOperationException(
                    $"Node index must be between 0 and 4, but was {value} in name '{name}'");

            NodeIdEventSource.Log.NewNodeId((byte)value);
            return value;
        });

        /// <summary>
        /// Node identification (last character in Fabric node name)
        /// </summary>
        public static int NodeIndex => _nodeIndex.Value;
    }
}