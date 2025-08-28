using System;
using System.Runtime.InteropServices;

namespace EHVN.ZaloBot.Miscellaneous
{
    internal static class NativeMethods
    {
        [StructLayout(LayoutKind.Sequential)]
        struct PsApiPerformanceInformation
        {
            public int Size;
            public nint CommitTotal;
            public nint CommitLimit;
            public nint CommitPeak;
            public nint PhysicalTotal;
            public nint PhysicalAvailable;
            public nint SystemCache;
            public nint KernelTotal;
            public nint KernelPaged;
            public nint KernelNonPaged;
            public nint PageSize;
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

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool GetLogicalProcessorInformationEx(int relationshipType, nint buffer, out uint returnedLength);

        [DllImport("user32.dll")]
        public static extern nint GetForegroundWindow();

        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        public static extern nint GetDesktopWindow();

        [DllImport("user32.dll")]
        public static extern nint GetWindowRect(nint hWnd, ref Rect rect);

        [StructLayout(LayoutKind.Sequential)]
        public struct Rect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

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
