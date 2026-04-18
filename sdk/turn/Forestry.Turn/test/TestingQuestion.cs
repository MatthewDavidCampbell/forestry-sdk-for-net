
namespace Forestry.Turn.Tests
{
    /// <summary>
    /// Simple question for testing
    /// </summary>
    public class TestingQuestion : Question
    {
        private readonly Dimensions _dimensions = new();

        /// <summary>
        /// When has been disposed
        /// </summary>
        public bool AssertDisposed { get; private set; }

        /// <summary>
        /// Contenet stream
        /// </summary>
        public override QuestionContent? Content { 
            get => base.Content; 
            set => base.Content = value; 
        }

        #region dimensions
        protected override void AddDimension(string name, string value) => _dimensions.Add(name, value); 

        protected override bool ContainsDimension(string name) => _dimensions.Contains(name);

        protected override bool RemoveDimension(string name) => _dimensions.Remove(name);

        protected override bool TryGetDimension(string name, out string value) => _dimensions.TryGet(name, out value);

        protected override bool TryGetDimensionValues(string name, out IEnumerable<string> values) => _dimensions.TryGet(name, out values);

        protected override IEnumerable<Dimension> EnumerateDimensions() => _dimensions.Enumerate();
        #endregion

        public override void Dispose()
        {
            AssertDisposed = true;
        }
    }
}
