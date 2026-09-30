namespace AndreaTV.Api.Configuration;

public sealed class SecurityOptions
{
    public const string SectionName = "Security";

    public bool RequireRegisteredDevice { get; init; }

    public int MaxDevicesPerUser { get; init; }
}
