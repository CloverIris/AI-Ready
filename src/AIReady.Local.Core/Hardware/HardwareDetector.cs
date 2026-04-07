#nullable enable

using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;
using AIReady.Shared.Models;


namespace AIReady.Local.Core.Hardware;

/// <summary>
/// Detects local hardware information including CPU, GPU, memory, and disk
/// </summary>
public class HardwareDetector
{
    /// <summary>
    /// Gets comprehensive hardware information
    /// </summary>
    public async Task<HardwareInfo> GetHardwareInfoAsync(CancellationToken cancellationToken = default)
    {
        var info = new HardwareInfo();

        var tasks = new List<Task>
        {
            Task.Run(() => DetectCpu(info), cancellationToken),
            Task.Run(() => DetectMemory(info), cancellationToken),
            Task.Run(() => DetectGpus(info), cancellationToken),
            Task.Run(() => DetectDisks(info), cancellationToken),
            Task.Run(() => DetectOs(info), cancellationToken)
        };

        await Task.WhenAll(tasks);

        return info;
    }

    private void DetectCpu(HardwareInfo info)
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Processor");
            foreach (ManagementObject obj in searcher.Get())
            {
                info.CpuModel = obj["Name"]?.ToString()?.Trim();
                info.CpuCores = Convert.ToInt32(obj["NumberOfCores"]);
                info.CpuThreads = Convert.ToInt32(obj["NumberOfLogicalProcessors"]);
                break;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error detecting CPU: {ex.Message}");
        }
    }

    private void DetectMemory(HardwareInfo info)
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PhysicalMemory");
            ulong totalMemory = 0;
            foreach (ManagementObject obj in searcher.Get())
            {
                totalMemory += Convert.ToUInt64(obj["Capacity"]);
            }
            info.TotalMemoryBytes = totalMemory;

            // Get available memory
            var memoryStatus = new MEMORYSTATUSEX();
            if (GlobalMemoryStatusEx(memoryStatus))
            {
                info.AvailableMemoryBytes = memoryStatus.ullAvailPhys;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error detecting memory: {ex.Message}");
        }
    }

    private void DetectGpus(HardwareInfo info)
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController");
            foreach (ManagementObject obj in searcher.Get())
            {
                var gpu = new GPUInfo
                {
                    Name = obj["Name"]?.ToString()?.Trim(),
                    DriverVersion = obj["DriverVersion"]?.ToString()
                };

                // Try to get VRAM from AdapterRAM property (may be inaccurate for >4GB)
                try
                {
                    var adapterRam = obj["AdapterRAM"];
                    if (adapterRam != null)
                    {
                        uint vramBytes = (uint)adapterRam;
                        // AdapterRAM is uint and wraps around for >4GB
                        if (vramBytes < uint.MaxValue)
                        {
                            gpu.VramBytes = vramBytes;
                        }
                    }
                }
                catch { }

                // For NVIDIA GPUs, try to get accurate VRAM from nvidia-smi
                if (gpu.IsNvidia)
                {
                    TryGetNvidiaGpuInfo(gpu);
                }

                // Only add if it looks like a real GPU (not a basic display adapter)
                if (!string.IsNullOrEmpty(gpu.Name) && 
                    !gpu.Name.Contains("Microsoft Basic Display", StringComparison.OrdinalIgnoreCase) &&
                    !gpu.Name.Contains("VMware", StringComparison.OrdinalIgnoreCase))
                {
                    info.GPUs.Add(gpu);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error detecting GPUs: {ex.Message}");
        }
    }

    private void TryGetNvidiaGpuInfo(GPUInfo gpu)
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "nvidia-smi",
                    Arguments = "--query-gpu=name,memory.total,driver_version --format=csv,noheader,nounits",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(5000);

            if (process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output))
            {
                var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    var parts = line.Split(',');
                    if (parts.Length >= 3)
                    {
                        var name = parts[0].Trim();
                        if (gpu.Name?.Contains(name, StringComparison.OrdinalIgnoreCase) == true ||
                            name.Contains(gpu.Name ?? "", StringComparison.OrdinalIgnoreCase))
                        {
                            // Parse VRAM (in MiB)
                            if (double.TryParse(parts[1].Trim(), out var vramMiB))
                            {
                                gpu.VramBytes = (ulong)(vramMiB * 1024 * 1024);
                            }
                            gpu.DriverVersion = parts[2].Trim();
                            break;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error getting NVIDIA GPU info: {ex.Message}");
        }
    }

    private void DetectDisks(HardwareInfo info)
    {
        try
        {
            foreach (DriveInfo drive in DriveInfo.GetDrives())
            {
                if (drive.IsReady && drive.DriveType == DriveType.Fixed)
                {
                    info.Disks.Add(new DiskInfo
                    {
                        DriveLetter = drive.Name,
                        TotalBytes = (ulong)drive.TotalSize,
                        AvailableBytes = (ulong)drive.AvailableFreeSpace,
                        FileSystem = drive.DriveFormat
                    });
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error detecting disks: {ex.Message}");
        }
    }

    private void DetectOs(HardwareInfo info)
    {
        try
        {
            info.OSVersion = RuntimeInformation.OSDescription;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error detecting OS: {ex.Message}");
        }
    }

    /// <summary>
    /// Detects CUDA version from registry
    /// </summary>
    public string? GetCudaVersion()
    {
        try
        {
            // Try to find CUDA path from PATH environment variable
            var path = Environment.GetEnvironmentVariable("PATH");
            if (!string.IsNullOrEmpty(path))
            {
                var paths = path.Split(';');
                foreach (var p in paths)
                {
                    if (p.Contains("CUDA", StringComparison.OrdinalIgnoreCase) && 
                        p.Contains("bin", StringComparison.OrdinalIgnoreCase))
                    {
                        // Extract version from path like "C:\Program Files\NVIDIA GPU Computing Toolkit\CUDA\v11.8\bin"
                        var parts = p.Split('\\', '/');
                        foreach (var part in parts)
                        {
                            if (part.StartsWith("v", StringComparison.OrdinalIgnoreCase) && 
                                char.IsDigit(part[1]))
                            {
                                return part.TrimStart('v', 'V');
                            }
                        }
                    }
                }
            }


        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error detecting CUDA version: {ex.Message}");
        }

        return null;
    }

    [StructLayout(LayoutKind.Sequential)]
    private class MEMORYSTATUSEX
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;

        public MEMORYSTATUSEX()
        {
            dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
        }
    }

    [DllImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);
}
