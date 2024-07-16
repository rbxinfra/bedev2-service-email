namespace Roblox.EmailDelivery.Processor;

using DnsClient;

using Microsoft.Extensions.DependencyInjection;

using SendGrid;

using Roblox.Processor.Core;

using Platform.Email;
using Platform.Email.Delivery;

/// <summary>
/// Email delivery processor.
/// </summary>
public class EmailDeliveryProcessor : ProcessorBase<EmailDeliveryEvent>
{
    /// <inheritdoc cref="ProcessorBase{TMessage}.ProcessorSettings"/>
    protected override IProcessorSettings ProcessorSettings => Settings.Singleton;

    /// <inheritdoc cref="ProcessorBase{TMessage}.SetupServices"/>
    protected override void SetupServices(IServiceCollection services)
    {
        var dnsClient = new LookupClient();
        var emailDomainFactories = new EmailDomainFactories(Logger, dnsClient);

        if (!string.IsNullOrEmpty(Settings.Singleton.SendGridApiKey))
        {
            var sendGridApiClient = new SendGridClient(Settings.Singleton.SendGridApiKey);

            services.AddSingleton<ISendGridClient>(sendGridApiClient);
        }
        else
            services.AddSingleton<ISendGridClient>(_ => null);

        services.AddSingleton(emailDomainFactories);
        services.AddSingleton<IMessageProcessor<EmailDeliveryEvent>, EmailDeliveryEventHandler>();

        base.SetupServices(services);
    }

    /// <summary>
    /// The main entry point for the email delivery processor.
    /// </summary>
    public static void Main()
    {
        new EmailDeliveryProcessor().Run();
    }
}
