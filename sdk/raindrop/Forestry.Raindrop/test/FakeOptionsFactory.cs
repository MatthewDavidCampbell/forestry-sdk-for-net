using Microsoft.Extensions.Options;

namespace Forestry.Raindrop.Tests
{
    /// <summary>
    /// Fake <see cref="IOptionsFactory"/>
    /// </summary>
    /// <param name="value"></param>
    internal class FakeOptionsFactory(NodeId value) : IOptionsFactory<NodeId>
    {
        public static readonly Func<byte, IOptionsMonitor<NodeId>> OptionsMonitor = value => new OptionsMonitor<NodeId>(
            new FakeOptionsFactory(new() { Value = value }),
            [],
            new OptionsCache<NodeId>()
        );

        private readonly NodeId _value = value;

        public NodeId Create(string name) => _value;
    }

    /// <summary>
    /// Node id options
    /// </summary>
    public class NodeId
    {
        public const string Name = "NodeId";

        public byte Value { get; set; }
    };
}