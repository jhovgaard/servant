using System;
using System.Runtime.InteropServices;

namespace Servant.Web.Performance
{
    public static class SystemInfoWrapper
    {
        [DllImport("psapi.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetPerformanceInfo([Out] out PerformanceInformation performanceInformation, [In] int size);

        [StructLayout(LayoutKind.Sequential)]
        public struct PerformanceInformation
        {
            public int Size;
            public IntPtr CommitTotal;
            public IntPtr CommitLimit;
            public IntPtr CommitPeak;
            public IntPtr PhysicalTotal;
            public IntPtr PhysicalAvailable;
            public IntPtr SystemCache;
            public IntPtr KernelTotal;
            public IntPtr KernelPaged;
            public IntPtr KernelNonPaged;
            public IntPtr PageSize;
            public int HandlesCount;
            public int ProcessCount;
            public int ThreadCount;
        }

        public static Int64 GetPhysicalAvailableMemory()
        {
            var performanceInformation = new PerformanceInformation();

            if (GetPerformanceInfo(out performanceInformation, Marshal.SizeOf(performanceInformation)))
            {
                return performanceInformation.PhysicalAvailable.ToInt64()*performanceInformation.PageSize.ToInt64();
            }

            return -1;
        }

        public static Int64 GetTotalMemory()
        {
            var performanceInformation = new PerformanceInformation();

            if (GetPerformanceInfo(out performanceInformation, Marshal.SizeOf(performanceInformation)))
            {
                return performanceInformation.PhysicalTotal.ToInt64()*performanceInformation.PageSize.ToInt64();
            }

            return -1;
        }
    }
}