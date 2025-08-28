using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using static EHVN.ZaloBot.Miscellaneous.NativeMethods;

namespace EHVN.ZaloBot.Miscellaneous
{
    internal static class SystemInfo
    {
        static string GetVendorId()
        {
            var cpuInfo = X86Base.CpuId(0, 0);
            return (ConvertToString(cpuInfo.Ebx) + ConvertToString(cpuInfo.Edx) + ConvertToString(cpuInfo.Ecx)).Trim();
        }

        static int GetPhysicalCores()
        {
            GetLogicalProcessorInformationEx(0, IntPtr.Zero, out uint returnedLength);
            IntPtr bufferPtr = Marshal.AllocHGlobal((int)returnedLength);
            GetLogicalProcessorInformationEx(0, bufferPtr, out returnedLength);
            Marshal.FreeHGlobal(bufferPtr);
            return (int)returnedLength / 48;
        }

        static Dictionary<string, string> RunSystemInfo()
        {
            var systemInfo = new Dictionary<string, string>();
            using var process = new Process();
            process.StartInfo.FileName = "systeminfo";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.Start();
            using var reader = new StreamReader(process.StandardOutput.BaseStream);
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                string[] parts = line.Split(":  ", 2);
                if (parts.Length == 2)
                    systemInfo[parts[0].Trim()] = parts[1].Trim();
                else if (systemInfo.Count > 0 && !line.EndsWith(':'))
                    systemInfo[systemInfo.Last().Key] += Environment.NewLine + line.Trim();
                else if (!string.IsNullOrWhiteSpace(line))
                    systemInfo.Add(parts[0].Trim(), "");
            }
            process.WaitForExit();
            return systemInfo;
        }

        static string GetCpuModel() => (ConvertToString(X86Base.CpuId(unchecked((int)0x80000002), 0)) + ConvertToString(X86Base.CpuId(unchecked((int)0x80000003), 0)) + ConvertToString(X86Base.CpuId(unchecked((int)0x80000004), 0))).Trim();

        static int GetLogicalCores() => X86Base.CpuId(0xB, 1).Ebx & 0xFF;

        static string ConvertToString((int Eax, int Ebx, int Ecx, int Edx) value) => ConvertToString(value.Eax) + ConvertToString(value.Ebx) + ConvertToString(value.Ecx) + ConvertToString(value.Edx);

        static string ConvertToString(int value) => new([.. BitConverter.GetBytes(value).Select(i => (char)i)]);

        internal static string Get()
        {
            string result = "";
            if (X86Base.IsSupported)
            {
                var dict = RunSystemInfo();
                int pCores = GetPhysicalCores();
                int lCores = GetLogicalCores();
                result += $"""
                    [b]Vendor ID:[/b] {GetVendorId()}
                    [b]CPU:[/b] {GetCpuModel()}
                    [b]Lõi/Bộ xử lý logic:[/b] {pCores}/{Math.Max(pCores, lCores)}
                    [b]Tên HĐH:[/b] {dict["OS Name"]} (Build {dict["OS Version"].Split("Build ").Last()})
                    """;
            }
            else
                result += "[b]X86Base không được hỗ trợ[/b]";
            result += Environment.NewLine;
            var pInfo = GetPerformanceInfo();
            GetPhysicallyInstalledSystemMemory(out long totalMem);
            totalMem /= 1024;
            double totalMemPaged = pInfo.CommitTotalPages * pInfo.PageSizeBytes / 1024f / 1024f + totalMem;
            double usedMem = (pInfo.PhysicalTotalBytes - pInfo.PhysicalAvailableBytes) / 1024f / 1024f;
            result +=
                $"""
                [b]RAM:[/b] {usedMem:00.00}MB/{totalMem}MB
                [b]Phân trang:[/b] {totalMemPaged:00.00}MB
                """;
            return result;
        }
    }
}
