using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Timers;

namespace Servant.Web.Performance
{
    public class PerformanceData
    {
        public static float CpuUsage { get; private set; }
        public static float TotalGetRequestsSec { get; private set; }
        public static long PhysicalAvailableMemory { get; private set; }
        public static long TotalMemory { get; private set; }
        public static TimeSpan SystemUpTime { get; set; }

        private static BackgroundWorker _backgroundWorker;

        public static void Run()
        {
            var cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            var getRequestsSecTotal = new PerformanceCounter("Web Service", "Get Requests/sec", "_Total");
            var systemUpTime = new PerformanceCounter("System", "System Up Time");

            _backgroundWorker = new BackgroundWorker { WorkerReportsProgress = true };
            _backgroundWorker.RunWorkerCompleted += (sender, args) =>
                                                    {
                                                        if (args.Error != null)
                                                        {
                                                            return;
                                                        }

                                                        dynamic data = args.Result;
                                                        CpuUsage = data.CpuUsage;
                                                        TotalGetRequestsSec = data.TotalGetRequestsSec;
                                                        PhysicalAvailableMemory = data.PhysicalAvailableMemory;
                                                        TotalMemory = data.TotalMemory;
                                                        SystemUpTime = data.SystemUpTime; 
                                                    };
            _backgroundWorker.DoWork += (sender, args) =>
                                        {
                                            args.Result =
                                                new
                                                {
                                                    CpuUsage = cpuCounter.NextValue(),
                                                    TotalGetRequestsSec = getRequestsSecTotal.NextValue(),
                                                    PhysicalAvailableMemory = SystemInfoWrapper.GetPhysicalAvailableMemory(),
                                                    TotalMemory = SystemInfoWrapper.GetTotalMemory(),
                                                    SystemUpTime = TimeSpan.FromSeconds(systemUpTime.NextValue())
                                                };
                                        };

            var timer = new Timer(1000);
            timer.Elapsed += UpdateDataCallback;
            timer.Start();
        }

        private static void UpdateDataCallback(object state, ElapsedEventArgs elapsedEventArgs)
        {
            if (_backgroundWorker.IsBusy)
            {
                return;
            }

            _backgroundWorker.RunWorkerAsync();
        }
    }
}