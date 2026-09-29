using System.Text;
using CmsApi.Core.Batches;
using CmsApi.Core.Exceptions;
using FluentAssertions;

namespace CmsApi.UnitTests.Batches;

public sealed class BatchBodyValidatorTests
{
    [Test]
    public void Body_WithArrayOfEvents()
    {
        var body = BatchBodyValidator.Validate(Utf8(TwoEvents));

        body.Should().Be(new ValidBatchBody(TwoEvents, EventCount: 2));
    }

    [TestCase("[]")]
    [TestCase("""{ "type": "publish" }""")]
    [TestCase("not json")]
    [TestCase("")]
    public void Body_WithMalformedJson(string json) => ShouldReject(Utf8(json));

    [Test]
    public void Body_WithTooManyEvents() =>
        ShouldReject(
            Utf8($"[{string.Join(',', Enumerable.Repeat("{}", BatchLimits.MaxEvents + 1))}]")
        );

    [Test]
    public void Body_WithTooDeepNesting() =>
        ShouldReject(
            Utf8(
                new string('[', BatchLimits.MaxDepth + 1)
                    + new string(']', BatchLimits.MaxDepth + 1)
            )
        );

    [Test]
    public void Body_WithInvalidUtf8InString() => ShouldReject([.. "[\""u8, 0x80, .. "\"]"u8]);

    private const string TwoEvents = """[ {"type":"delete"},  {"type":"delete"} ]""";

    private static void ShouldReject(byte[] body)
    {
        var validate = () => BatchBodyValidator.Validate(body);

        validate.Should().Throw<InvalidBatchException>().Which.BodyBytes.Should().Be(body.Length);
    }

    private static byte[] Utf8(string text) => Encoding.UTF8.GetBytes(text);
}
