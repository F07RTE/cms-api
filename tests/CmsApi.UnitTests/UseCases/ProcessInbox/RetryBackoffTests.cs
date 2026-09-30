using CmsApi.Core.UseCases.ProcessInbox;
using FluentAssertions;

namespace CmsApi.UnitTests.UseCases.ProcessInbox;

public sealed class RetryBackoffTests
{
    [TestCase(1, 2)]
    [TestCase(5, 32)]
    [TestCase(8, 256)]
    public void Attempts_BelowFiveMinuteCap(int attempts, int expectedSeconds)
    {
        RetryBackoff.After(attempts).Should().Be(TimeSpan.FromSeconds(expectedSeconds));
    }

    [TestCase(9)]
    [TestCase(100)]
    public void Attempts_PastFiveMinuteCap(int attempts)
    {
        RetryBackoff.After(attempts).Should().Be(TimeSpan.FromMinutes(5));
    }
}
