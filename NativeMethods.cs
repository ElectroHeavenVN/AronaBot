using System.Runtime.InteropServices;

namespace ZaloBot
{
    internal static class NativeMethods
    {
        [StructLayout(LayoutKind.Sequential)]
        struct PsApiPerformanceInformation
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

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetPhysicallyInstalledSystemMemory(out long TotalMemoryInKilobytes);

        [DllImport("psapi.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetPerformanceInfo([Out] out PsApiPerformanceInformation PerformanceInformation, [In] int Size);

        public static PerfomanceInfoData GetPerformanceInfo()
        {
            PerfomanceInfoData data = new PerfomanceInfoData();
            PsApiPerformanceInformation perfInfo = new PsApiPerformanceInformation();
            if (GetPerformanceInfo(out perfInfo, Marshal.SizeOf(perfInfo)))
            {
                /// data in pages
                data.CommitTotalPages = perfInfo.CommitTotal.ToInt64();
                data.CommitLimitPages = perfInfo.CommitLimit.ToInt64();
                data.CommitPeakPages = perfInfo.CommitPeak.ToInt64();
                /// data in bytes
                long pageSize = perfInfo.PageSize.ToInt64();
                data.PhysicalTotalBytes = perfInfo.PhysicalTotal.ToInt64() * pageSize;
                data.PhysicalAvailableBytes = perfInfo.PhysicalAvailable.ToInt64() * pageSize;
                data.SystemCacheBytes = perfInfo.SystemCache.ToInt64() * pageSize;
                data.KernelTotalBytes = perfInfo.KernelTotal.ToInt64() * pageSize;
                data.KernelPagedBytes = perfInfo.KernelPaged.ToInt64() * pageSize;
                data.KernelNonPagedBytes = perfInfo.KernelNonPaged.ToInt64() * pageSize;
                data.PageSizeBytes = pageSize;
                /// counters
                data.HandlesCount = perfInfo.HandlesCount;
                data.ProcessCount = perfInfo.ProcessCount;
                data.ThreadCount = perfInfo.ThreadCount;
            }
            return data;
        }

        public class PerfomanceInfoData
        {
            public long CommitTotalPages;
            public long CommitLimitPages;
            public long CommitPeakPages;
            public long PhysicalTotalBytes;
            public long PhysicalAvailableBytes;
            public long SystemCacheBytes;
            public long KernelTotalBytes;
            public long KernelPagedBytes;
            public long KernelNonPagedBytes;
            public long PageSizeBytes;
            public int HandlesCount;
            public int ProcessCount;
            public int ThreadCount;
        }
    }
}
