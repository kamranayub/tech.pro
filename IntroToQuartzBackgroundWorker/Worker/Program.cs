using System;
using System.Configuration;
using System.Net;
using System.Net.Mail;
using Quartz;
using Quartz.Impl;

namespace Worker {

    class WorkerOptions {

        /// <summary>
        /// Whether or not to run the job immediately at runtime
        /// </summary>
        public bool RunImmediately { get; set; }

        /// <summary>
        /// The email address to send a notification to
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// Our email username
        /// </summary>
        public string EmailUsername { get; set; }

        /// <summary>
        /// Our email password
        /// </summary>
        public string EmailPassword { get; set; }

        /// <summary>
        /// Our email server
        /// </summary>
        public string EmailServer { get; set; }

        /// <summary>
        /// Our email port
        /// </summary>
        public int EmailPort { get; set; }
    }

    class Program {
        private static readonly ISchedulerFactory SchedulerFactory;
        private static readonly IScheduler Scheduler;
        private static IJobDetail _emailJobDetail;
        private static WorkerOptions _options;

        static Program() {

            // Create a regular old Quartz scheduler
            SchedulerFactory = new StdSchedulerFactory();
            Scheduler = SchedulerFactory.GetScheduler();

        }

        static void Main(string[] args) {

            // Read our options from config (provided locally 
            // or via cloud host)
            ReadOptionsFromConfig();      
            
            // Now let's start our scheduler; you could perform
            // any processing or bootstrapping code here before
            // you start it but it must be started to schedule
            // any jobs
            Scheduler.Start();

            // Let's generate our email job detail now
            CreateJob();

            // And finally, schedule the job
            ScheduleJob();

            // Run immediately?
            if (_options.RunImmediately) {
                Scheduler.TriggerJob(new JobKey("SendToMyself"));
            }
        }

        private static void CreateJob() {

            // The job builder uses a fluent interface to
            // make it easier to build and generate an
            // IJobDetail object
            _emailJobDetail = JobBuilder.Create<EmailJob>()
                .WithIdentity("SendToMyself")   // Here we can assign a friendly name to our job        
                .Build();                       // And now we build the job detail

            // Put options into data map
            _emailJobDetail.JobDataMap.Put("Email", _options.Email);
            _emailJobDetail.JobDataMap.Put("Username", _options.EmailUsername);
            _emailJobDetail.JobDataMap.Put("Password", _options.EmailPassword);
            _emailJobDetail.JobDataMap.Put("Server", _options.EmailServer);
            _emailJobDetail.JobDataMap.Put("Port", _options.EmailPort);
        }

        private static void ScheduleJob() {

            // Let's create a trigger
            ITrigger trigger = TriggerBuilder.Create()

                // A description helps other people understand what you want
                .WithDescription("Every day at 3AM CST")

                // A daily time schedule gives you a
                // DailyTimeIntervalScheduleBuilder which provides
                // a fluent interface to build a schedule
                .WithDailyTimeIntervalSchedule(x => x

                    // Here we specify the interval
                    .WithIntervalInHours(24)

                    // And how often to repeat it
                    .OnEveryDay()

                    // Specify the time of day the trigger fires, in UTC (9am),
                    // since CST is UTC-0600
                    .StartingDailyAt(TimeOfDay.HourAndMinuteOfDay(9, 0))

                    // Specify the timezone
                    //
                    // I like to use UTC dates in my applications to make sure
                    // I stay consistent, especially when you never know what
                    // server you're on!
                    .InTimeZone(TimeZoneInfo.Utc))

                // Finally, we take the schedule and build a trigger
                .Build();
            
            // Ask the scheduler to schedule our EmailJob
            Scheduler.ScheduleJob(_emailJobDetail, trigger);
        }

        private static void ReadOptionsFromConfig() {

            // Make sure we have options to change
            if (_options == null)
                _options = new WorkerOptions();

            // Try to read the RunImmediately value from app.config
            string configRunImmediately = ConfigurationManager.AppSettings["RunImmediately"];
            bool runImmediately;

            if (Boolean.TryParse(configRunImmediately, out runImmediately)) {
                _options.RunImmediately = runImmediately;
            }

            // Try to read the Email value from app.config
            string configEmail = ConfigurationManager.AppSettings["Email"];

            if (!String.IsNullOrEmpty(configEmail)) {
                _options.Email = configEmail;
            }

            // Try to read the email username value from app.config
            string configEmailUsername = ConfigurationManager.AppSettings["MAILGUN_SMTP_LOGIN"];

            if (!String.IsNullOrEmpty(configEmailUsername)) {
                _options.EmailUsername = configEmailUsername;
            }

            // Try to read the email password value from app.config
            string configEmailPassword = ConfigurationManager.AppSettings["MAILGUN_SMTP_PASSWORD"];

            if (!String.IsNullOrEmpty(configEmailPassword)) {
                _options.EmailPassword = configEmailPassword;
            }

            // Try to read the email password value from app.config
            string configEmailServer = ConfigurationManager.AppSettings["MAILGUN_SMTP_SERVER"];

            if (!String.IsNullOrEmpty(configEmailServer)) {
                _options.EmailServer = configEmailServer;
            }

            // Try to read the email password value from app.config
            string configEmailPort = ConfigurationManager.AppSettings["MAILGUN_SMTP_PORT"];
            int emailPort;

            if (!String.IsNullOrEmpty(configEmailServer) && Int32.TryParse(configEmailPort, out emailPort)) {
                _options.EmailPort = emailPort;
            }
        }
    }

    /// <summary>
    /// Our email job, yet to be implemented
    /// </summary>
    public class EmailJob : IJob {
        public void Execute(IJobExecutionContext context) {

            // Read the values from our merged (final) data map
            var email = context.MergedJobDataMap["Email"] as string;
            var username = context.MergedJobDataMap["Username"] as string;
            var password = context.MergedJobDataMap["Password"] as string;
            var server = (context.MergedJobDataMap["Server"] as string) ?? "smtp.mailgun.org";
            int port;

            // Parse port
            if (!Int32.TryParse(context.MergedJobDataMap["Port"] as string, out port)) {

                // Default to 587
                port = 587;
            }
            
            // Ensure we have all data required
            if (email == null || username == null || password == null) return;

            // Create a new SmtpClient and dispose of it once we're done
            // Connect to Mailgun SMTP server (they also offer a REST API)
            // For more info, see: http://documentation.mailgun.net/quickstart.html#sending-messages
            using (var smtpClient = new SmtpClient(server, port)) {

                // Create credentials, specifying your user name and password.
                smtpClient.Credentials = new NetworkCredential(username, password);
                // Enable at least some security, for more info see:
                // http://msdn.microsoft.com/en-us/library/system.net.mail.smtpclient.enablessl.aspx
                smtpClient.EnableSsl = true;

                // Send the message
                // NOTE: We're using our Mailgun username which is also
                // the postmaster address as the From line
                smtpClient.Send(
                    from: username, 
                    recipients: email, 
                    subject: "Test email from background worker", 
                    body: "Hello World from our background worker!");
            }
        }
    }
}