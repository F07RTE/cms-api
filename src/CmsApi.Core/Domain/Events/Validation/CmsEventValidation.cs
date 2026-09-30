namespace CmsApi.Core.Domain.Events.Validation;

public abstract record CmsEventValidation;

public sealed record ValidCmsEvent(CmsEvent Event) : CmsEventValidation;

public sealed record FailedCmsEvent(string? Id, string RawEvent, string Reason)
    : CmsEventValidation;
