using System;
using System.Diagnostics;
using System.Threading;

namespace EHVN.AronaBot.Miscellaneous
{
    internal static class ProcessInfo
    {
        internal static string Get(Process process)
        {
            PerformanceCounter counter = new PerformanceCounter("Process", "Working Set - Private", process.ProcessName);
            double currentMem = counter.RawValue / 1024d / 1024d;
            counter.Dispose();
            double currentMemPaged = process.PrivateMemorySize64 / 1024f / 1024f;
            counter = new PerformanceCounter("Process", "% Processor Time", process.ProcessName);
            counter.NextValue();
            Thread.Sleep(200);
            double cpuUsage = counter.NextValue();
            counter.Dispose();
            TimeSpan timeSpan = DateTime.UtcNow - process.StartTime.ToUniversalTime();
            string uptime = Math.Floor(timeSpan.TotalHours).ToString("00") + ":" + timeSpan.Minutes.ToString("00") + ":" + timeSpan.Seconds.ToString("00");
            return
                $"""
                - [b]Tiến trình:[/b] {process.ProcessName} ({process.Id})
                - [b]CPU sử dụng:[/b] {cpuUsage:00.00}%
                - [b]RAM tiêu thụ:[/b] {currentMem:00.00}MB
                - [b]Phân trang:[/b] {currentMemPaged:00.00}MB
                - [b]Thời gian chạy:[/b] {uptime}
                """;
        }
    }
}
