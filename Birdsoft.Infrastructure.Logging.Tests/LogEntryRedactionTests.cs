using Birdsoft.Infrastructure.Logging;

namespace Birdsoft.Infrastructure.Logging.Tests;

public sealed class LogEntryRedactionTests
{
    [Fact]
    public void Redact_ShouldMaskDefaultPatterns()
    {
        var input = "access_token=abc123 password=pwd456";

        var output = LogEntryRedactor.Redact(input);

        Assert.Contains("[REDACTED]", output);
        Assert.DoesNotContain("abc123", output);
        Assert.DoesNotContain("pwd456", output);
    }

    [Fact]
    public void ResetToDefaults_ShouldClearCustomPatterns()
    {
        LogEntryRedactor.AddPattern("(hello)(\\w+)");
        var redacted = LogEntryRedactor.Redact("helloworld");
        Assert.Equal("hello[REDACTED]", redacted);

        LogEntryRedactor.ResetToDefaults();
        var afterReset = LogEntryRedactor.Redact("helloworld");
        Assert.Equal("helloworld", afterReset);
    }
}