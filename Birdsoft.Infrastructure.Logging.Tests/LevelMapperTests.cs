using Birdsoft.Infrastructure.Logging;
using Birdsoft.Infrastructure.Logging.Abstractions;
using Serilog.Events;
using MsLogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Birdsoft.Infrastructure.Logging.Tests;

public sealed class LevelMapperTests
{
    [Theory]
    [InlineData(LogLevel.Trace, LogEventLevel.Verbose)]
    [InlineData(LogLevel.Debug, LogEventLevel.Debug)]
    [InlineData(LogLevel.Information, LogEventLevel.Information)]
    [InlineData(LogLevel.Warning, LogEventLevel.Warning)]
    [InlineData(LogLevel.Error, LogEventLevel.Error)]
    [InlineData(LogLevel.Critical, LogEventLevel.Fatal)]
    public void ToSerilogLevel_ShouldMapExpected(LogLevel appLevel, LogEventLevel expected)
    {
        Assert.Equal(expected, LevelMapper.ToSerilogLevel(appLevel));
    }

    [Fact]
    public void MicrosoftNone_ShouldMapToAppNone()
    {
        Assert.Equal(LogLevel.None, LevelMapper.ToAppLevel(MsLogLevel.None));
        Assert.Equal(MsLogLevel.None, LevelMapper.ToMicrosoftLevel(LogLevel.None));
    }
}