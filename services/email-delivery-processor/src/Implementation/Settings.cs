namespace Roblox.EmailDelivery.Processor;

using Configuration;
using Roblox.Processor.Core;

/// <summary>
/// Default details for processor settings.
/// </summary>
internal class Settings : BaseSettingsProvider<Settings>, IProcessorSettings
{
    /// <inheritdoc cref="BaseSettingsProvider.ChildPath"/>
    protected override string ChildPath => SettingsProvidersDefaults.EmailDeliveryProcessorSettingsPath;

    /// <inheritdoc cref="IProcessorSettings.NumberOfThreads"/>
    public int NumberOfThreads => GetOrDefault(nameof(NumberOfThreads), 1);

    /// <inheritdoc cref="IProcessorSettings.SqsQueueUrl"/>
    public string SqsQueueUrl => GetOrDefault(nameof(SqsQueueUrl), string.Empty);

    /// <inheritdoc cref="IProcessorSettings.AwsAccessKeyAndSecretKey"/>
    public string AwsAccessKeyAndSecretKey => GetOrDefault(nameof(AwsAccessKeyAndSecretKey), string.Empty);

    /// <inheritdoc cref="IProcessorSettings.IsThroughputThrottlingEnabled"/>
    public bool IsThroughputThrottlingEnabled => GetOrDefault(nameof(IsThroughputThrottlingEnabled), false);

    /// <inheritdoc cref="IProcessorSettings.ThroughputThrottlePeriod"/>
    public TimeSpan ThroughputThrottlePeriod => GetOrDefault(nameof(ThroughputThrottlePeriod), TimeSpan.FromSeconds(1));

    /// <inheritdoc cref="IProcessorSettings.ThroughputThrottleLimit"/>
    public int ThroughputThrottleLimit => GetOrDefault(nameof(ThroughputThrottleLimit), 10000);

    /// <summary>
    /// Gets the SendGrid API Key.
    /// </summary>
    public string SendGridApiKey => GetOrDefault<string>(nameof(SendGridApiKey), string.Empty);

    /// <summary>
    /// Gets a CSV list of email types to use SendGrid for.
    /// </summary>
    public string SendGridEmailTypesCSV => GetOrDefault(nameof(SendGridEmailTypesCSV), string.Empty);
}
