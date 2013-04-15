using Quartz;
using Quartz.Impl;

namespace Worker {
    class Program {
        private static readonly ISchedulerFactory SchedulerFactory;
        private static readonly IScheduler Scheduler;

        static Program() {

            // Create a regular old Quartz scheduler
            SchedulerFactory = new StdSchedulerFactory();
            Scheduler = SchedulerFactory.GetScheduler();

        }

        static void Main(string[] args) {
            
        }
    }
}
