using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Forestry.Turn.Tests
{
    public class TestingAnswer : Answer
    {
        private readonly Dimensions _dimensions = new();

        private bool? _hasErrors;

        /// <summary>
        /// When has been disposed
        /// </summary>
        public bool AssertDisposed { get; private set; }

        public override Stream? Content { get; set; }

        public override bool HasErrors { get => _hasErrors ?? base.HasErrors; }

        #region dimensions

        protected override bool ContainsDimension(string name) => _dimensions.Contains(name);

        protected override bool TryGetDimension(string name, out string value) => _dimensions.TryGet(name, out value);

        protected override bool TryGetDimensionValues(string name, out IEnumerable<string> values) => _dimensions.TryGet(name, out values);

        protected override IEnumerable<Dimension> EnumerateDimensions() => _dimensions.Enumerate();
        #endregion

        #region fluent
        public TestingAnswer WithDimension(Dimension dimension)
        {
            _dimensions.Add(dimension.Name, dimension.Value);
            return this;
        }

        public TestingAnswer WithDimension(string name, string value)
        {
            _dimensions.Add(name, value);
            return this;
        }

        public TestingAnswer WithContent(string content)
        {
            if (content is not null)
            {
                Content = new MemoryStream(Encoding.UTF8.GetBytes(content), 0, content.Length, false, true);
                _dimensions.Add(Dimension.Names.ContentLength, $"{content.Length}");
            }

            return this;
        }

        public TestingAnswer WithErrors()
        {
            _hasErrors = true;
            return this;
        }
        #endregion

        public override void Dispose()
        {
            AssertDisposed = true;
        }
    }
}
