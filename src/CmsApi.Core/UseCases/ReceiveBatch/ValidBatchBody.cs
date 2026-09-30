namespace CmsApi.Core.UseCases.ReceiveBatch;

/// <summary>A body that passed the whole-body rules. <see cref="Text"/> is the original body, decoded losslessly.</summary>
public sealed record ValidBatchBody(string Text, int EventCount);
