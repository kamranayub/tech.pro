using System;
using Quartz;
using Quartz.Impl;

namespace Worker {
    class Program {
        private static readonly ISchedulerFactory SchedulerFactory;
        private static readonly IScheduler Scheduler;
        private static IJobDetail _emailJobDetail;

        static Program() {

            // Create a regular old Quartz scheduler
            SchedulerFactory = new StdSchedulerFactory();
            Scheduler = SchedulerFactory.GetScheduler();

        }

        static void Main(string[] args) {
            
            // Now let's start our scheduler; you could perform
            // any processing or bootstrapping code here before
            // you start it but it must be started to schedule
            // any jobs
            Scheduler.Start();

            // Let's generate our email job detail now
            CreateJob();

            // And finally, schedule the job
            ScheduleJob();
        }

        private static void CreateJob() {

            // The job builder uses a fluent interface to
            // make it easier to build and generate an
            // IJobDetail object
            _emailJobDetail = JobBuilder.Create<EmailJob>()
                .WithIdentity("SendToMyself")   // Here we can assign a friendly name to our job        
                .Build();                       // And now we build the job detail

        }

        private static void ScheduleJob() {

            // Let's create a trigger that fires immediately
            ITrigger trigger = TriggerBuilder.Create()
                .StartNow()     // Starts the trigger immediately when scheduled
                .Build();       // Builds a trigger to assign to a job

            // Ask the scheduler to schedule our EmailJob
            Scheduler.ScheduleJob(_emailJobDetail, trigger);
        }
    }

    /// <summary>
    /// Our email job, yet to be implemented
    /// </summary>
    public class EmailJob : IJob {
        public void Execute(IJobExecutionContext context) {

            // Let's start simple, write to the console
            Console.WriteLine("Hello World!");

        }
    }
}