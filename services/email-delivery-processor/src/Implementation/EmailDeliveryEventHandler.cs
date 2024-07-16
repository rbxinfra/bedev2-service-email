namespace Roblox.EmailDelivery.Processor;

using Roblox.Processor.Core;

using Prometheus;

using SendGrid;
using SendGrid.Helpers.Mail;

using Newtonsoft.Json.Linq;

using EventLog;
using Configuration;
using Threading.Extensions;

using Platform.Email;
using Platform.Email.Delivery;

using Email = Roblox.Email.Email;


/// <summary>
/// Email delivery event handler.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="EmailDeliveryEventHandler"/> class.
/// </remarks>
/// <param name="logger">The <see cref="ILogger"/> instance.</param>
/// <param name="sendGridClient">The <see cref="ISendGridClient"/> instance.</param>
/// <param name="emailDomainFactories">The <see cref="EmailDomainFactories"/> instance.</param>
/// <exception cref="ArgumentNullException">
/// - <paramref name="logger"/> is <see langword="null"/>.
/// - <paramref name="emailDomainFactories"/> is <see langword="null"/>.
/// </exception>
public class EmailDeliveryEventHandler(ILogger logger, ISendGridClient sendGridClient, EmailDomainFactories emailDomainFactories) : IMessageProcessor<EmailDeliveryEvent>
{
    private readonly ILogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly ISendGridClient _sendGridClient = sendGridClient;
    private readonly EmailDomainFactories _EmailDomainFactories = emailDomainFactories ?? throw new ArgumentNullException(nameof(emailDomainFactories));

    private static HashSet<string> _sendGridEmailTypes = [];

    private const string _InfoEmailName = "Roblox";
    private const string _NoReplyEmailName = "Roblox no-reply";

    private static readonly Counter _totalEmailDeliveryEvents = Metrics.CreateCounter(
        "email_delivery_events_total",
        "The total number of email delivery events for the specified type.",
        "email_type"
    );

    private static readonly Counter _totalSendGridEmailDeliveryEvents = Metrics.CreateCounter(
        "email_delivery_sendgrid_emails_total",
        "The total number of email delivery events with types that use SendGrid",
        "email_type"
    );

    private static readonly Counter _totalBlacklistedEmailAttempts = Metrics.CreateCounter(
        "email_delivery_blacklisted_emails_total",
        "The total number of email delivery events for black listed emails",
        "email"
    );


    static EmailDeliveryEventHandler()
    {
        Settings.Singleton.ReadValueAndMonitorChanges(s => s.SendGridEmailTypesCSV, s => _sendGridEmailTypes = MultiValueSettingParser.ParseCommaDelimitedListString(s));
    }

    /// <inheritdoc cref="IMessageProcessor{TMessage}.ProcessMessage(TMessage)"/>
    public void ProcessMessage(EmailDeliveryEvent message)
    {
        if (!_EmailDomainFactories.EmailAddressValidator.IsValidEmail(message.To))
        {
            _logger.Warning("Skipping message because message.To is not a valid email address");

            return;
        }

        if (_EmailDomainFactories.EmailAddressValidator.IsShadyProvider(message.To))
        {
            _logger.Warning("Skipping message because message.To is a shady provider");

            return;
        }

        if (_EmailDomainFactories.EmailAddressValidator.IsBlacklisted(message.To))
        {
            _totalBlacklistedEmailAttempts.WithLabels(message.To).Inc();
            _logger.Warning("Skipping message because sending to blacklisted emails is prohibited");

            return;
        }

        _totalEmailDeliveryEvents.WithLabels(message.EmailType).Inc();

        _logger.Information(
            "Sending email. To = {0}, From = {1}, Subject = {2}, Type = {3}, BodyType = {4}",
            message.To,
            message.From,
            message.Subject,
            message.EmailType,
            message.EmailBodyType
        );

        var fromName = DetermineFromName(message.From);

        if (!string.IsNullOrEmpty(message.EmailType) && _sendGridEmailTypes.Contains(message.EmailType) && _sendGridClient != null)
        {
            _logger.Warning("Email type {0} is in the list for SendGrid email types!", message.EmailType);
            _totalSendGridEmailDeliveryEvents.WithLabels(message.EmailType).Inc();

            var sendGridMessage = new SendGridMessage();
            sendGridMessage.SetSubject(message.Subject);
            sendGridMessage.SetFrom(message.From, fromName);
            sendGridMessage.AddTo(message.To);

            switch (message.EmailBodyType)
            {
                case EmailBodyType.Plain:
                    sendGridMessage.AddContent(MimeType.Text, message.PlainTextBody);
                    break;
                case EmailBodyType.Html:
                    sendGridMessage.AddContent(MimeType.Html, message.HtmlBody);
                    break;
                case EmailBodyType.Mime:
                    sendGridMessage.AddContent(MimeType.Text, message.PlainTextBody);
                    sendGridMessage.AddContent(MimeType.Html, message.HtmlBody);
                    break;
            }

            var result = _sendGridClient.SendEmailAsync(sendGridMessage).Sync();

            if (!result.IsSuccessStatusCode)
                HandleSendGridError(result);

            return;
        }

        if (!string.IsNullOrEmpty(fromName))
            message.From = $"{fromName} <{message.From}>";

        if (message.EmailBodyType == EmailBodyType.Mime)
            Email.SendMimeEmail(message.To, message.From, message.Subject, message.PlainTextBody, message.HtmlBody);
        else
            Email.SendEmail(
                message.To,
                message.From,
                message.Subject,
                message.EmailBodyType == EmailBodyType.Html
                    ? message.HtmlBody
                    : message.PlainTextBody,
                message.EmailBodyType == EmailBodyType.Html
            );
    }

    private static string DetermineFromName(string from)
    {
        if (from == EmailAddresses.NoReplyEmailAddress) return _NoReplyEmailName;
        if (from == EmailAddresses.InfoEmailAddress) return _InfoEmailName;

        return null;
    }

    private void HandleSendGridError(Response result)
    {
        var body = result.DeserializeResponseBodyAsync().Sync();

        if (body.TryGetValue("errors", out var v) && v is JArray arr)
        {
            var errorString = "Error when sending email via SendGrid: ";

            foreach (JObject error in arr.Cast<JObject>())
            {
                var errorMessage = error.GetValue("message")?.ToString();
                var field = error.GetValue("field")?.ToString();
                var help = error.GetValue("help")?.ToString();

                errorString += errorMessage;

                if (!string.IsNullOrEmpty(field)) errorString += $" (field: {field})";
                if (!string.IsNullOrEmpty(help)) errorString += $" (help: {help})";

                errorString += "\n";
            }

            errorString = errorString.TrimEnd('\n');

            _logger.Error(errorString);
        }
    }
}
