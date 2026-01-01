using System;
using System.Threading.Tasks;
using Xunit;

namespace Brevit.NET.Tests
{
    public class BrevitClientTests
    {
        [Fact]
        public async Task Optimize_FlattenJson_ReturnsFlattenedString()
        {
            // Arrange
            var config = new BrevitConfig(JsonMode: JsonOptimizationMode.Flatten);
            var jsonOptimizer = new DefaultJsonOptimizer();
            var textOptimizer = new DefaultTextOptimizer();
            var imageOptimizer = new DefaultImageOptimizer();
            var client = new BrevitClient(config, jsonOptimizer, textOptimizer, imageOptimizer);

            var testObject = new
            {
                user = new
                {
                    name = "Javian",
                    email = "support@javianpicardo.com"
                }
            };

            // Act
            var result = await client.OptimizeAsync(testObject);

            // Assert
            Assert.Contains("user.name: Javian", result);
            Assert.Contains("user.email: support@javianpicardo.com", result);
        }

        [Fact]
        public async Task Optimize_JsonString_ReturnsFlattenedString()
        {
            // Arrange
            var config = new BrevitConfig(JsonMode: JsonOptimizationMode.Flatten);
            var jsonOptimizer = new DefaultJsonOptimizer();
            var textOptimizer = new DefaultTextOptimizer();
            var imageOptimizer = new DefaultImageOptimizer();
            var client = new BrevitClient(config, jsonOptimizer, textOptimizer, imageOptimizer);

            var jsonString = @"{""order"": {""orderId"": ""o-456"", ""status"": ""SHIPPED""}}";

            // Act
            var result = await client.OptimizeAsync(jsonString);

            // Assert
            Assert.Contains("order.orderId: o-456", result);
            Assert.Contains("order.status: SHIPPED", result);
        }

        [Fact]
        public async Task Optimize_ShortText_ReturnsAsIs()
        {
            // Arrange
            var config = new BrevitConfig(TextMode: TextOptimizationMode.Clean);
            var jsonOptimizer = new DefaultJsonOptimizer();
            var textOptimizer = new DefaultTextOptimizer();
            var imageOptimizer = new DefaultImageOptimizer();
            var client = new BrevitClient(config, jsonOptimizer, textOptimizer, imageOptimizer);

            var shortText = "Hello World";

            // Act
            var result = await client.OptimizeAsync(shortText);

            // Assert
            Assert.Equal("Hello World", result);
        }
    }
}

