using Xunit;

namespace Forestry.Turn.Tests
{
    public class QuestionContentTests
    {
        [Fact]
        public async void When_AsyncCopyingString_ItShould_CreateByteArray()
        {
            // Arrange
            QuestionContent value = QuestionContent.Create("value");
            MemoryStream stream = new();

            // Act
            await value.CopyToAsync(stream, CancellationToken.None);
            stream.Position = 0; // else position == end

            StreamReader reader = new(stream);

            // Assert
            Assert.Equal("value", reader.ReadToEnd());
        }

        [Fact]
        public void When_CopyingString_ItShould_CreateByteArray()
        {
            // Arrange
            QuestionContent value = QuestionContent.Create("value");
            MemoryStream stream = new();

            // Act
            value.CopyTo(stream, CancellationToken.None);
            stream.Position = 0;

            StreamReader reader = new(stream);

            // Assert
            Assert.Equal("value", reader.ReadToEnd());
        }

        [Fact]
        public void When_HasStringContent_ItShould_HaveLength()
        {
            // Arrange + Act
            QuestionContent value = QuestionContent.Create("value");

            // Assert
            Assert.True(value.TryGetLength(out long _));
        }
    }
}
