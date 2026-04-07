#nullable enable

using AIReady.Local.Core.Hardware;
using AIReady.Local.Core.Miniconda;
using AIReady.Local.Core.Workflows;

namespace AIReady.Local.Core;

/// <summary>
/// Extension methods for registering Local.Core services
/// </summary>
public static class LocalCoreServices
{
    /// <summary>
    /// Creates a new HardwareDetector instance
    /// </summary>
    public static HardwareDetector CreateHardwareDetector()
    {
        return new HardwareDetector();
    }

    /// <summary>
    /// Creates a new CompatibilityChecker instance
    /// </summary>
    public static CompatibilityChecker CreateCompatibilityChecker()
    {
        return new CompatibilityChecker();
    }

    /// <summary>
    /// Creates a new MinicondaManager instance
    /// </summary>
    public static MinicondaManager CreateMinicondaManager()
    {
        return new MinicondaManager();
    }

    /// <summary>
    /// Creates a new WorkflowRegistry instance
    /// </summary>
    public static WorkflowRegistry CreateWorkflowRegistry()
    {
        return new WorkflowRegistry();
    }

    /// <summary>
    /// Creates a new WorkflowInstaller instance
    /// </summary>
    public static WorkflowInstaller CreateWorkflowInstaller(MinicondaManager minicondaManager)
    {
        return new WorkflowInstaller(minicondaManager);
    }
}
