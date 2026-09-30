using CmsApi.Core.UseCases.ConsumeBatch;
using FluentAssertions;

namespace CmsApi.Core.Tests.UseCases.ConsumeBatch;

public sealed class BatchAttemptTests
{
    private const long BatchId = 42;
    private const int MaxAttempts = 5;

    [Test]
    public void Attempt_BeforeMaxAttempts()
    {
        new BatchAttempt(BatchId, MaxAttempts - 1, MaxAttempts).CanRetry.Should().BeTrue();
    }

    [Test]
    public void Attempt_AtMaxAttempts()
    {
        new BatchAttempt(BatchId, MaxAttempts, MaxAttempts).CanRetry.Should().BeFalse();
    }

    [Test]
    public void Delivery_AfterTwoFailures()
    {
        BatchAttempt
            .AfterFailures(BatchId, failures: 2, MaxAttempts)
            .Should()
            .Be(new BatchAttempt(BatchId, 3, MaxAttempts));
    }
}
