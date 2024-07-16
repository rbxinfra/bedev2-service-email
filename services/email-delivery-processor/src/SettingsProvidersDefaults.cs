namespace Roblox.EmailDelivery.Processor;

/// <summary>
/// Default details for the settings providers.
/// </summary>
internal static class SettingsProvidersDefaults
{
    /// <summary>
    /// The path prefix for the coordination platform.
    /// </summary>
    public const string ProviderPathPrefix = "coordination-platform";

    /// <summary>
    /// The path to the email delivery processor settings.
    /// </summary>
    public const string EmailDeliveryProcessorSettingsPath = $"{ProviderPathPrefix}/processors/email-delivery-processor";
}
