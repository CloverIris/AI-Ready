package main

import (
	"runtime"

	"github.com/shirou/gopsutil/v3/cpu"
	"github.com/shirou/gopsutil/v3/disk"
	"github.com/shirou/gopsutil/v3/host"
	"github.com/shirou/gopsutil/v3/mem"
)

// SystemCollector collects system information
type SystemCollector struct{}

// NewSystemCollector creates a new system collector
func NewSystemCollector() *SystemCollector {
	return &SystemCollector{}
}

// Collect gathers all system information
func (c *SystemCollector) Collect() (*SystemInfo, error) {
	info := &SystemInfo{
		Platform: runtime.GOOS + "/" + runtime.GOARCH,
	}

	// Hostname
	hostname, err := host.Hostname()
	if err == nil {
		info.Hostname = hostname
	}

	// CPU info
	if cpuInfo, err := cpu.Info(); err == nil && len(cpuInfo) > 0 {
		info.CPU.Model = cpuInfo[0].ModelName
		info.CPU.Cores = int(cpuInfo[0].Cores)
	}
	
	if cpuCount, err := cpu.Counts(true); err == nil {
		info.CPU.Threads = cpuCount
	}
	
	if cpuPercent, err := cpu.Percent(0, false); err == nil && len(cpuPercent) > 0 {
		info.CPU.UsagePercent = cpuPercent[0]
	}

	// Memory info
	if vmStat, err := mem.VirtualMemory(); err == nil {
		info.Memory.Total = vmStat.Total
		info.Memory.Used = vmStat.Used
		info.Memory.Free = vmStat.Free
		info.Memory.UsedPercent = vmStat.UsedPercent
	}

	// Disk info (root partition)
	if diskStat, err := disk.Usage("/"); err == nil {
		info.Disk.Total = diskStat.Total
		info.Disk.Used = diskStat.Used
		info.Disk.Free = diskStat.Free
		info.Disk.UsedPercent = diskStat.UsedPercent
	}

	// GPU info (placeholder - would need nvidia-smi or similar)
	info.GPUs = c.collectGPUInfo()

	return info, nil
}

// collectGPUInfo attempts to collect GPU information
// This is a placeholder - actual implementation would use nvidia-smi or ROCm
func (c *SystemCollector) collectGPUInfo() []GPUInfo {
	gpus := []GPUInfo{}
	
	// TODO: Implement GPU detection using:
	// - nvidia-smi for NVIDIA GPUs
	// - rocm-smi for AMD GPUs
	// - Intel GPU tools for Intel GPUs
	
	return gpus
}
