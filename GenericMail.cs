using System;
using System.Xml;
using MailKit;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Configuration;
using MimeKit;
using Serilog;

class GenericMail {
    public static void Main(IConfiguration config) {

        // Setup - read configs - gather template file info
        IConfigurationSection GenericMailAppSetting = config.GetSection("GenericSMTPClientSettings");
        IConfiguration globalAppSetting = config.GetSection("GlobalSettings");

        var smtpServer = GenericMailAppSetting["Server"]; //config["AppSettings//"";
        var smtpPort = int.Parse(GenericMailAppSetting["Port"]);
        var startTLS = bool.Parse(GenericMailAppSetting["StartTLS"]);
        var DisplayName = GenericMailAppSetting["FromAddressDisplayName"];
        var SMTPUser = GenericMailAppSetting["SMTPUser"]; //config["AppSettings:FromAddress"]; //"DoNotReply@site-right.com";
        var SMTPFromAddress = GenericMailAppSetting["SMTPFromAddress"]; //The SMTPUser might be the same as this, but in some cases (AWS SES) the credentials are different from the sending address
        var SMTPPass = GenericMailAppSetting["SMTPPass"];

        var emailSubject = globalAppSetting["Subject"]; // "Remedy Available";
        var pathToHTMLEmail = globalAppSetting["PathToHTMLEmail"];
        var pathToPlaintextEmail = globalAppSetting["PathToPlaintextEmail"];
        var sendAttachment = globalAppSetting["SendAttachment"];
        var pathToAttachment = globalAppSetting["PathToAttachment"];
        var attachmentName = globalAppSetting["AttachmentName"];
        var minWait = int.Parse(globalAppSetting["SendEmailMinWait"]);
        var maxWait = int.Parse(globalAppSetting["SendEmailMaxWait"]);

        string recipFile = globalAppSetting["RecipientsFile"];
        string[] recipientLines;
        if (string.IsNullOrEmpty(recipFile)) {
            Log.Error("Recipient File appears null or empty");
            return;
        }
        else {       
            recipientLines = TurtleMailer.ReadInRecipients(recipFile);
        }
        
        var emailHTMLContent = File.ReadAllText(pathToHTMLEmail);
        var emailPlainTextContent = File.ReadAllText(pathToPlaintextEmail);

        Log.Information($"SMTP Server is {smtpServer} for user {SMTPFromAddress}");
        // Start building SMTP Client objects
        var lastEmail = recipientLines.Last();

        foreach (string email in recipientLines) {
            while (true) {
                if (TurtleMailer.IsWorkingTime(config))
                {
                    Log.Information($"Starting email process for {email}");

                    // Create the email
                    var message = new MimeMessage();
                    message.From.Add(new MailboxAddress(DisplayName, SMTPFromAddress));
                    message.To.Add(new MailboxAddress(email, email));
                    message.Subject = emailSubject;

                    // Always send HTML and Plaintext versions
                    var bodyBuilder = new BodyBuilder();
                    bodyBuilder.HtmlBody = emailHTMLContent;
                    bodyBuilder.TextBody = emailPlainTextContent;
                    message.Body = bodyBuilder.ToMessageBody(); // new TextPart("html") { Text = emailHTMLContent };

                    // attachment section - also shows changing body to include multipart plaintext and html
                    // todo - mimekit.net/docs/html/Creating-Messages.htm

                    using (var client = new SmtpClient())
                    {
                        client.Connect(smtpServer, smtpPort, startTLS);
                        client.Authenticate(SMTPUser, SMTPPass);
                        client.Send(message);
                        Log.Information("Email was sent to " + email);
                        client.Disconnect(true);
                    }

                    // Wait to send the next email, unless it was the last email, then just stop
                    if (email == lastEmail) {
                        break;
                    }
                    else {
                        TurtleMailer.RandomizedWaitTimer(minWait, maxWait);
                    }
                    break;
                }
                else {
                    TurtleMailer.WaitForWorkingTime();
                }
            }
        }
    }


}