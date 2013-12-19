using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Timers;

namespace Servant.Web.Performance
{
    public class PerformanceData
    {
        public static float TotalGetRequestsSec { get; private set; }
        public static long PhysicalAvailableMemory { get; private set; }
        public static long TotalMemory { get; private set; }
        public static TimeSpan SystemUpTime { get; set; }
        private static BackgroundWorker _backgroundWorker;
        private static LimitedSizeStack<float> GetRequests = new LimitedSizeStack<float>(60);
        private static LimitedSizeStack<float> CpuUsage = new LimitedSizeStack<float>(60);
        public static float CurrentConnections { get; private set; }

        public static float AverageGetRequestPerSecond { get { return GetRequests.Any() ? GetRequests.Average() : 0; }}
        public static float AverageCpuUsage { get { return CpuUsage.Any() ? CpuUsage.Average() : 0; } }

        private static readonly PerformanceCounter CpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        private static readonly PerformanceCounter SystemUpTimeCounter = new PerformanceCounter("System", "System Up Time");
        private static readonly PerformanceCounter GetRequestsSecTotalCounter = new PerformanceCounter("Web Service", "Get Requests/sec", "_Total");
        private static readonly PerformanceCounter CurrentConnectionsCounter = new PerformanceCounter("Web Service", "Current Connections", "_Total");

        public static void Run()
        {
            _backgroundWorker = new BackgroundWorker { WorkerReportsProgress = true };
            _backgroundWorker.RunWorkerCompleted += (sender, args) =>
                                                    {
                                                        if (args.Error != null)
                                                        {
                                                            return;
                                                        }

                                                        dynamic data = args.Result;
                                                        TotalGetRequestsSec = data.TotalGetRequestsSec;
                                                        PhysicalAvailableMemory = data.PhysicalAvailableMemory;
                                                        TotalMemory = data.TotalMemory;
                                                        SystemUpTime = data.SystemUpTime;
                                                        CurrentConnections = data.CurrentConnections;

                                                        GetRequests.Push(data.TotalGetRequestsSec);
                                                        CpuUsage.Push(data.CurrentCpuUsage);
                                                    };
            _backgroundWorker.DoWork += (sender, args) =>
                                        {
                                            args.Result =
                                                new
                                                {
                                                    CurrentCpuUsage = CpuCounter.NextValue(),
                                                    TotalGetRequestsSec = GetRequestsSecTotalCounter.NextValue(),
                                                    PhysicalAvailableMemory = SystemInfoWrapper.GetPhysicalAvailableMemory(),
                                                    TotalMemory = SystemInfoWrapper.GetTotalMemory(),
                                                    SystemUpTime = TimeSpan.FromSeconds(SystemUpTimeCounter.NextValue()),
                                                    CurrentConnections = CurrentConnectionsCounter.NextValue()
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