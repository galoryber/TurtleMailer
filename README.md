
# Turtle Mailer

![Turtle Mailer](images/image.png)

## Description
Turtle Mailer is a Red Team tool to build phishing emails and slowly schedule the send of that email through Azure Communication Services and other Email send providers. 

Features include:
- Sends emails based on a list of email addresses provided
- Emails are sent 1 every 40 to 75 minutes (Definable numbers)
- Emails are only sent during defined working hours (Definable : M - F, 9 - 5)

## Visuals

While running, output will indicate timing and actions taken. This output will be captured in the future and written as a log file (someday). 

![In and Out of Working Hours](images/workingImage.png)

## Installation

The tool is written in, and therefore requires, [.NET 8.0](https://dotnet.microsoft.com/en-us/download). 

## Usage
First, edit the appsettings.json file to fit your needs. This includes defining what provider you'll use to send emails, and drafting up your email message in the HTML template (AND plaintext).

![Config File](images/appsettings.png)

Then, edit both your HTML and the Plaintext version of the email contents. Templates are provided as an example, but any valid EMAIL HTML should be fine. 

There is certainly HTML that is not valid for email clients, so consider testing against something like this [phishing email linter](https://github.com/mgeeky/Penetration-Testing-Tools/blob/master/phishing/phishing-HTML-linter.py).

![Editing the templates](images/htmlTemplate.png)

### Compile and Run
To compile, clone the repo and then, 

```dotnet build .```

This will produce all needed files in **bin/Debug/net8.0** including
- TurtleMail.exe and all supporting DLLs
- appsettings.json - templateEmail.html - recipients.txt - etc.

Finally, start running the program -> 

```./TurtleMailer.exe```

**Note** 

The template files and appsettings files **are copied** to the output directory when compiled. If you change them, you should save the files and **recompile** to make sure you're not editing or running the wrong template files. 

As in... appsettings.json will be in the root directory, AND ALSO the output directory once compiled. The compiled binary reads the adjacent configs that were copied to the output directory.

This was convenient while testing the app, but may change in the future to avoid duplicate config files existing. 

## Testing Emails

I would recommened drafting your email and sending to several DIFFERENT email addresses. The goal should be identifying which techniques are most likely to be delivered. 
- gmail address
- M365 address
- target behind ProofPoint address
- etc...

For example, a phishing email with a maliciously redirected link might get blocked, but sending a maliciously redirected link that utilizes Google AMP page redirection will likely land in a gmail inbox just fine, but get blocked to other services like M365. 

## Providers

The Turtle Mailer has been tested using:
- Azure Communication Services ```AzureCommunicationServices```
- Generic SMTP (using Office 365) ```GenericSMTP```

The generic smtp client option should allow us to use any service. Bought a domain for $2 on NameCheap, then bought their SMTP Email subscription? That should work, just plug in their SMTP server into the config and compile. 

As we come accross additional mailing services, we can utilize them in this tool or craft an additional Provider specific config. 

## Roadmap

- Adapt to other utilities, SES? Other providers? 
- Reload config on each loop - could hot swap certain elements then. Mid email list, change the from address, change HTML, change attachment, etc. 
- Attachments - not currently handling content-type automatically, needs work, review, implmentation