using Microsoft.Extensions.Configuration;
using System.Net.Mime;
using Azure;
using Azure.Communication.Email;
using Serilog;

class AzComm {
    public async static void Main(IConfiguration config) {
        Log.Information("In Azure Communication Services, parsing config");
        // Read the passed in config settings for Azure Communication Services settings and Global Settings
        IConfigurationSection azAppSetting = config.GetSection("AzCommSettings");
        IConfiguration globalAppSetting = config.GetSection("GlobalSettings");

        var fromAddress = azAppSetting["FromAddress"]; //config["AppSettings:FromAddress"]; //"DoNotReply@site-right.com";
        var connectionString = azAppSetting["ConnectionString"]; //config["AppSettings//"";

        var emailSubject = globalAppSetting["Subject"]; // "Remedy Available";
        var pathToHTMLEmail = globalAppSetting["PathToHTMLEmail"];
        var pathToPlaintextEmail = globalAppSetting["PathToPlaintextEmail"];
        var sendAttachment = bool.Parse(globalAppSetting["SendAttachment"]);
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

        Log.Information($"Sending from user {fromAddress}");

        var emailHTMLContent = File.ReadAllText(pathToHTMLEmail);
        var emailPlainTextContent = File.ReadAllText(pathToPlaintextEmail);

        // EmailClient emailClient; 
        // if (connectionString.StartsWith("endpoint")) {
        //     // config file has hardcoded endpoint id
        //     emailClient = new EmailClient(connectionString);
        // }
        // else {
        //     // use environment variable call to find connection string
        //     connectionString = Environment.GetEnvironmentVariable("COMMUNICATION_SERVICES_CONNECTION_STRING");
        //     emailClient = new EmailClient(connectionString);
        // }

        var emailClient = new EmailClient(connectionString);


        // Take the list of email address, and send out one email, to one person, during defined working hours
        List<EmailAddress> emailAddrs = new List<EmailAddress>();
        var lastEmail = recipientLines.Last();

        foreach (string email in recipientLines) {
            while (true) {
                    if (TurtleMailer.IsWorkingTime(config))
                        {
                            // Turn each individual recipient list item into an email address object
                            emailAddrs.Add(new EmailAddress(email));
                            Log.Information($"Starting email process for {email}");

                            // Now turn them into an email recipients object
                            // can optionally be ..pients(emailAddrs, ccAddrs, bccAddrs);
                            var emRecip = new EmailRecipients(emailAddrs, null, null);

                            // Define the content of the email in it's own object
                            var emContent = new EmailContent(emailSubject);
                            emContent.Html = emailHTMLContent;
                            emContent.PlainText = emailPlainTextContent;

                            // build the message
                            var emailMessage = new EmailMessage(fromAddress, emRecip, emContent);

                            // Attachment data -- TBD how to more dynamically determine content types of an attachment
                            string attachmentContentType = MediaTypeNames.Application.Pdf;

                            if (sendAttachment) {
                                EmailAttachment ematt;
                                try {
                                    BinaryData attachmentData = new BinaryData(File.ReadAllBytes(pathToAttachment));
                                    ematt = new EmailAttachment(attachmentName, attachmentContentType, attachmentData);
                                    emailMessage.Attachments.Add(ematt);
                                }
                                catch {
                                    Log.Error("Caught an error trying to get the attachment. Press Ctrl C to exit if this isn't okay, or any other key to continue");
                                    // Log.Information(" * Ignoring for now and auto-continuing until rewrite");
                                    //Console.ReadLine();
                                }
                                                
                            }

                            // Send Email
                            EmailSendOperation emailSendOperation = emailClient.Send(
                                WaitUntil.Completed,
                                emailMessage);

                            // Call UpdateStatus on the email send operation to poll for the status manually
                            try
                            {
                                while (true)
                                {
                                    await emailSendOperation.UpdateStatusAsync();
                                    if (emailSendOperation.HasCompleted)
                                    {
                                        break;
                                    }
                                    await Task.Delay(100);
                                }

                                if (emailSendOperation.HasValue)
                                {
                                    Log.Information($"Email queued for delivery. Status = {emailSendOperation.Value.Status}");
                                }
                            }
                            catch (RequestFailedException ex)
                            {
                                Log.Error($"Email send failed with Code = {ex.ErrorCode} and Message = {ex.Message}");
                            }

                            /// Get the OperationId so that it can be used for tracking the message for troubleshooting
                            string operationId = emailSendOperation.Id;
                            Log.Information($"Email operation id = {operationId}");

                            // Remove previous email address, only emailing one person at a time
                            emailAddrs.Clear(); 

                            // Add Sleep timer to limit frequency of delivery
                            if (email == lastEmail) {
                                break;
                            }
                            else {
                                TurtleMailer.RandomizedWaitTimer(minWait, maxWait);
                            }
                            // break out of the while loop and hit the next email address in the for loop
                            break; 
                        }
                        else {
                            TurtleMailer.WaitForWorkingTime();
                        }
            }
        }
    }
}

