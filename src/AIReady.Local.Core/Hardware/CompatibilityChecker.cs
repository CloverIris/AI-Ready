#nullable enable

using System.Diagnostics.CodeAnalysis;
using AIReady.Shared.Models;

namespace AIReady.Local.Core.Hardware;

/// <summary>
/// Checks hardware compatibility for running AI workloads
/// </summary>
public class CompatibilityChecker
{
    // VRAM thresholds in GB
    private const int MIN_VRAM_GB = 4;
    private const int RECOMMENDED_VRAM_GB = 8;
    private const int OPTIMAL_VRAM_GB = 16;

    // RAM thresholds in GB
    private const int MIN_RAM_GB = 8;
    private const int RECOMMENDED_RAM_GB = 16;

    // Disk space threshold in GB
    private const int MIN_DISK_GB = 20;

    /// <summary>
    /// Assesses hardware compatibility for AI workloads
    /// </summary>
    public CompatibilityResult AssessCompatibility(HardwareInfo hardware)
    {
        var result = new CompatibilityResult();
        var issues = new List<CompatibilityIssue>();
        var recommendations = new List<string>();

        // Check GPU
        var gpuResult = AssessGpuCompatibility(hardware, issues, recommendations);
        
        // Check RAM
        var ramResult = AssessRamCompatibility(hardware, issues, recommendations);
        
        // Check Disk
        var diskResult = AssessDiskCompatibility(hardware, issues, recommendations);

        // Determine overall compatibility level
        result.Level = DetermineCompatibilityLevel(gpuResult, ramResult, diskResult, hardware);
        result.IsCompatible = result.Level != CompatibilityLevel.NotCompatible;
        result.Issues = issues;
        result.Recommendations = recommendations;

        return result;
    }

    /// <summary>
    /// Gets recommended workflows based on hardware capabilities
    /// </summary>
    public List<string> GetRecommendedWorkflows(HardwareInfo hardware)
    {
        var recommendations = new List<string>();
        
        if (hardware.GPUs.Count == 0)
        {
            recommendations.Add("纯 CPU 工作流（速度较慢）");
            return recommendations;
        }

        var primaryGpu = hardware.GPUs.FirstOrDefault();
        if (primaryGpu == null) return recommendations;

        var vramGb = primaryGpu.VramBytes / (1024.0 * 1024 * 1024);

        if (vramGb >= 16)
        {
            recommendations.Add("Stable Diffusion XL（高质量图像生成）");
            recommendations.Add("大型语言模型（LLaMA 2 70B）");
            recommendations.Add("ComfyUI（高级工作流）");
        }
        else if (vramGb >= 8)
        {
            recommendations.Add("Stable Diffusion 1.5（标准图像生成）");
            recommendations.Add("中型语言模型（LLaMA 2 13B）");
            recommendations.Add("Ollama + OpenWebUI");
        }
        else if (vramGb >= 4)
        {
            recommendations.Add("Stable Diffusion（优化模式）");
            recommendations.Add("小型语言模型（LLaMA 2 7B）");
            recommendations.Add("Ollama（轻量模式）");
        }
        else
        {
            recommendations.Add("纯 CPU 推理工作流");
        }

        return recommendations;
    }

    private (bool hasGpu, double vramGb, CompatibilityLevel level) AssessGpuCompatibility(
        HardwareInfo hardware, 
        List<CompatibilityIssue> issues, 
        List<string> recommendations)
    {
        if (hardware.GPUs.Count == 0)
        {
            issues.Add(new CompatibilityIssue
            {
                Component = "GPU",
                Description = "未检测到独立显卡，AI 推理将使用 CPU，速度会很慢",
                Severity = IssueSeverity.Error
            });
            recommendations.Add("建议安装 NVIDIA RTX 系列显卡以获得最佳体验");
            return (false, 0, CompatibilityLevel.NotCompatible);
        }

        var primaryGpu = hardware.GPUs.FirstOrDefault();
        if (primaryGpu == null)
        {
            return (false, 0, CompatibilityLevel.NotCompatible);
        }

        var vramGb = primaryGpu.VramBytes / (1024.0 * 1024 * 1024);
        bool isNvidia = primaryGpu.IsNvidia;

        // Check if NVIDIA
        if (!isNvidia)
        {
            if (primaryGpu.IsAmd)
            {
                issues.Add(new CompatibilityIssue
                {
                    Component = "GPU",
                    Description = $"检测到 AMD 显卡: {primaryGpu.Name}。AMD 显卡支持有限，部分功能可能无法使用",
                    Severity = IssueSeverity.Warning
                });
                recommendations.Add("对于最佳兼容性，建议使用 NVIDIA RTX 系列显卡");
            }
            else if (primaryGpu.IsIntel)
            {
                issues.Add(new CompatibilityIssue
                {
                    Component = "GPU",
                    Description = $"检测到 Intel 显卡: {primaryGpu.Name}。Intel 显卡支持有限",
                    Severity = IssueSeverity.Warning
                });
                recommendations.Add("建议使用 NVIDIA RTX 系列显卡以获得最佳体验");
            }
        }
        else
        {
            // Check VRAM for NVIDIA
            if (vramGb < MIN_VRAM_GB)
            {
                issues.Add(new CompatibilityIssue
                {
                    Component = "GPU",
                    Description = $"显存不足: {vramGb:F1} GB。最低需要 {MIN_VRAM_GB} GB",
                    Severity = IssueSeverity.Error
                });
                recommendations.Add("建议升级到 8GB 或以上显存的显卡");
            }
            else if (vramGb < RECOMMENDED_VRAM_GB)
            {
                issues.Add(new CompatibilityIssue
                {
                    Component = "GPU",
                    Description = $"显存较低: {vramGb:F1} GB。推荐 {RECOMMENDED_VRAM_GB} GB 或以上",
                    Severity = IssueSeverity.Warning
                });
            }
        }

        // Determine GPU compatibility level
        var level = vramGb switch
        {
            >= OPTIMAL_VRAM_GB => CompatibilityLevel.Optimal,
            >= RECOMMENDED_VRAM_GB => CompatibilityLevel.Recommended,
            >= MIN_VRAM_GB => CompatibilityLevel.Minimum,
            _ => CompatibilityLevel.NotCompatible
        };

        return (true, vramGb, level);
    }

    private CompatibilityLevel AssessRamCompatibility(
        HardwareInfo hardware, 
        List<CompatibilityIssue> issues, 
        List<string> recommendations)
    {
        var ramGb = hardware.TotalMemoryBytes / (1024.0 * 1024 * 1024);

        if (ramGb < MIN_RAM_GB)
        {
            issues.Add(new CompatibilityIssue
            {
                Component = "内存",
                Description = $"内存不足: {ramGb:F1} GB。最低需要 {MIN_RAM_GB} GB",
                Severity = IssueSeverity.Error
            });
            recommendations.Add("建议升级内存至 16GB 或以上");
            return CompatibilityLevel.NotCompatible;
        }
        else if (ramGb < RECOMMENDED_RAM_GB)
        {
            issues.Add(new CompatibilityIssue
            {
                Component = "内存",
                Description = $"内存较低: {ramGb:F1} GB。推荐 {RECOMMENDED_RAM_GB} GB 或以上",
                Severity = IssueSeverity.Warning
            });
            return CompatibilityLevel.Minimum;
        }

        return ramGb >= 32 ? CompatibilityLevel.Optimal : CompatibilityLevel.Recommended;
    }

    private CompatibilityLevel AssessDiskCompatibility(
        HardwareInfo hardware, 
        List<CompatibilityIssue> issues, 
        List<string> recommendations)
    {
        if (hardware.Disks.Count == 0)
        {
            return CompatibilityLevel.NotCompatible;
        }

        // Find the disk with most available space
        var largestDisk = hardware.Disks.OrderByDescending(d => d.AvailableBytes).First();
        var availableGb = largestDisk.AvailableBytes / (1024.0 * 1024 * 1024);

        if (availableGb < MIN_DISK_GB)
        {
            issues.Add(new CompatibilityIssue
            {
                Component = "磁盘",
                Description = $"磁盘空间不足: {largestDisk.DriveLetter} 仅剩 {availableGb:F1} GB。建议至少保留 {MIN_DISK_GB} GB",
                Severity = IssueSeverity.Error
            });
            recommendations.Add("请清理磁盘空间或更换更大容量的硬盘");
            return CompatibilityLevel.NotCompatible;
        }
        else if (availableGb < 50)
        {
            issues.Add(new CompatibilityIssue
            {
                Component = "磁盘",
                Description = $"磁盘空间有限: {largestDisk.DriveLetter} 仅剩 {availableGb:F1} GB",
                Severity = IssueSeverity.Warning
            });
            return CompatibilityLevel.Minimum;
        }

        return availableGb >= 100 ? CompatibilityLevel.Optimal : CompatibilityLevel.Recommended;
    }

    private CompatibilityLevel DetermineCompatibilityLevel(
        (bool hasGpu, double vramGb, CompatibilityLevel level) gpu,
        CompatibilityLevel ram,
        CompatibilityLevel disk,
        HardwareInfo hardware)
    {
        // If any critical component is not compatible, return NotCompatible
        if (gpu.level == CompatibilityLevel.NotCompatible || 
            ram == CompatibilityLevel.NotCompatible || 
            disk == CompatibilityLevel.NotCompatible)
        {
            return CompatibilityLevel.NotCompatible;
        }

        // Find the minimum level across all components
        var levels = new[] { gpu.level, ram, disk };
        return levels.Min();
    }

    /// <summary>
    /// Gets a human-readable compatibility level description
    /// </summary>
    public static string GetCompatibilityLevelDescription(CompatibilityLevel level)
    {
        return level switch
        {
            CompatibilityLevel.NotCompatible => "不兼容",
            CompatibilityLevel.Minimum => "最低配置",
            CompatibilityLevel.Recommended => "推荐配置",
            CompatibilityLevel.Optimal => "最佳配置",
            _ => "未知"
        };
    }

    /// <summary>
    /// Gets a human-readable issue severity description
    /// </summary>
    public static string GetSeverityDescription(IssueSeverity severity)
    {
        return severity switch
        {
            IssueSeverity.Info => "信息",
            IssueSeverity.Warning => "警告",
            IssueSeverity.Error => "错误",
            _ => "未知"
        };
    }
}
