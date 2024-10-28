using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Configuration;
using MimeKit;
using Serilog;

class TurtleMailer {
    public static void Main() {

        // Setup
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()   
            .WriteTo.File("turtle.log")
            .CreateLogger();
        
        Log.Information("TurtleMailer is starting");

        IConfiguration config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional:false, reloadOnChange: true)
            .Build();

        IConfiguration providerCheck = config.GetSection("Provider");
        var provider = providerCheck["Provider"];
        Log.Information("Provider is " + provider.ToString());

        switch (provider)
        {
            case "AzureCommunicationServices":
                AzComm.Main(config);
                break;
            case "GenericSMTP":
                GenericMail.Main(config);
                break;
            default:
                Log.Error("Hit Default Case, broke");
                break;
        }
        Log.Information("Application finished");
        Log.CloseAndFlush();
    }
    public static bool IsWorkingTime(IConfiguration config)
        {
            DateTime now = DateTime.Now;
            //Log.Information("Time now is " + now);
            // Not needed if logger timestamps file and output //Log.Information(now);
            IConfiguration globalAppSetting = config.GetSection("GlobalSettings");

            // Get days of operation from config file
            var dayStartConfig = globalAppSetting["WorkingDayStart"];
            var dayEndConfig = globalAppSetting["WorkingDayEnd"];
            System.DayOfWeek startDay = (DayOfWeek)Enum.Parse(typeof(DayOfWeek), dayStartConfig);
            System.DayOfWeek endDay = (DayOfWeek)Enum.Parse(typeof(DayOfWeek), dayEndConfig);
            
            // Check if the current day is between Monday (DayOfWeek.Monday) and Friday (DayOfWeek.Friday)
            bool isWeekday = now.DayOfWeek >= startDay && now.DayOfWeek <= endDay;

            // Get time of operation from config file
            var timeStartConfig = globalAppSetting["WorkingTimeStart"];
            var timeEndConfig = globalAppSetting["WorkingTimeEnd"];
            int startHour = int.Parse(timeStartConfig.Split(':')[0]);
            int startMinute = int.Parse(timeStartConfig.Split(':')[1]);            
            int endHour = int.Parse(timeEndConfig.Split(':')[0]);
            int endMinute = int.Parse(timeEndConfig.Split(':')[1]);
            // Check if the current time is between 6:30 AM and 4 PM
            bool isWorkingHours = now.TimeOfDay >= new TimeSpan(startHour, startMinute, 0) && now.TimeOfDay <= new TimeSpan(endHour, endMinute, 0);

            return isWeekday && isWorkingHours;
        }
    
    public static void RandomizedWaitTimer(int minimumWaitTime, int maximumWaitTime)
    {
        Random randomMinutes = new Random();
        var numOfMinutes = 60000 * randomMinutes.Next(minimumWaitTime, maximumWaitTime);
        Log.Information($"Sleeping for {numOfMinutes / 60000} more minutes");
        Thread.Sleep(numOfMinutes);
    }

    public static void WaitForWorkingTime(){
        // Hardcoded wait of 30 minutes, can convert to config entry if desired
        var numOfMinutes = 60000 * 30; 
        Log.Information($"Not working hours, sleeping for {numOfMinutes/60000} minutes");
        Thread.Sleep(numOfMinutes);
    }
    public static string[] ReadInRecipients(string PathToRecipientFile){
        Log.Information("Reading in recipients file at {0}", PathToRecipientFile);
        var recipientLines = File.ReadAllLines(PathToRecipientFile);
        Log.Information($"Read {recipientLines.Count()} addresses");
        foreach (string line in recipientLines){
            Log.Debug("\t" + line);
        }
        return recipientLines;
    }

}               