namespace CmsApi.Core.Events.Validation;

/// <summary>What <see cref="CmsEventValidator"/> made of one element of a Batch.</summary>
public abstract record CmsEventValidation;

public sealed record ValidCmsEvent(CmsEvent Event) : CmsEventValidation;

/// <summary>An invalid CMS Event. <see cref="Id"/> is null when the element had no usable id.</summary>
public sealed record FailedCmsEvent(string? Id, string RawEvent, string Reason)
    : CmsEventValidation;
