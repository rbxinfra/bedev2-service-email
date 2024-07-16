var emailSender = new EmailSender(Logger.NoopSingleton, StaticCounterRegistry.Instance);

var emails = new List<IEmail>();

Console.WriteLine("Enter the number of mail messages to send:");

if (!uint.TryParse(Console.ReadLine(), out var count))
    count = 1;

Console.WriteLine();

for (uint i = 1; i <= count; i++)
{
    Console.WriteLine("Mail message #{0}", i);
    Console.WriteLine();

    Console.WriteLine("From address (default: {0}):", EmailAddresses.NoReplyEmailAddress);
    var fromAddress = Console.ReadLine();
    if (string.IsNullOrEmpty(fromAddress)) fromAddress = EmailAddresses.NoReplyEmailAddress;

    Console.WriteLine("To address:");
    var toAddress = Console.ReadLine();
    if (string.IsNullOrEmpty(toAddress)) throw new ApplicationException("Required argument not specified: To");

    Console.WriteLine("Subject:");
    var subject = Console.ReadLine();
    if (string.IsNullOrEmpty(subject)) throw new ApplicationException("Required argument not specified: Subject");

    Console.WriteLine("Body type (default: {0}):", nameof(EmailBodyType.Plain));
    var bodyType = EnumUtils.StrictTryParseEnum<EmailBodyType>(Console.ReadLine()) ?? EmailBodyType.Plain;

    Console.WriteLine("Body:");
    var body = Console.ReadLine();

    Console.WriteLine("Email Type (default: TestEmail):");
    var emailType = Console.ReadLine();
    if (string.IsNullOrEmpty(emailType)) emailType = "TestEmail";

    var email = new Email(
        fromAddress,
        toAddress,
        subject,
        bodyType,
        emailType,
        body,
        body
    );

    emails.Add(email);

    Console.WriteLine();
}

foreach (var email in emails)
{
    Console.WriteLine("Sending email '{0}' to '{1}'", email.Subject, email.To);

    emailSender.SendEmail(email);
}


Console.WriteLine("Press any key to exit...");
Console.ReadKey();
