using Birdsoft.Infrastructure.Logging;

namespace Birdsoft.Infrastructure.Logging.Tests;

public sealed class MessageTemplateParserTests
{
    [Fact]
    public void Parse_ShouldMapNamedProperties_AndRenderMessage()
    {
        var result = MessageTemplateParser.Parse("Error {Code} from {Service}", 500, "Auth");

        Assert.Equal("Error 500 from Auth", result.RenderedMessage);
        Assert.Equal(500, result.Properties["Code"]);
        Assert.Equal("Auth", result.Properties["Service"]);
    }
}