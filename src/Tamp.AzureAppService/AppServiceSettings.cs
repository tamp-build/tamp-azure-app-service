namespace Tamp.AzureAppService;

/// <summary>
/// Common knobs shared by every <c>az webapp</c> verb. Working dir + env-var overlay,
/// plus the subscription / resource group / webapp name triple every command needs.
/// </summary>
public abstract class AzureAppServiceSettingsBase
{
    /// <summary>Working directory for the spawned <c>az</c> process. Typically the repo root.</summary>
    public string? WorkingDirectory { get; set; }

    /// <summary>Per-invocation environment variables on top of the inherited environment.</summary>
    public Dictionary<string, string> EnvironmentVariables { get; } = new();

    /// <summary>Subscription id or name (<c>--subscription</c>). Optional when the CLI default is correct.</summary>
    public string? Subscription { get; set; }

    /// <summary>Resource group containing the App Service (<c>--resource-group</c>). Required.</summary>
    public string? ResourceGroup { get; set; }

    /// <summary>App Service name (<c>--name</c>). Required.</summary>
    public string? Name { get; set; }

    /// <summary>Emit machine-readable JSON output (<c>--output json</c>). Default true so
    /// downstream parsers don't have to opt in per call.</summary>
    public bool JsonOutput { get; set; } = true;

    /// <summary>Subclasses produce the per-verb argument list AFTER the common <c>webapp</c> token.</summary>
    protected abstract IEnumerable<string> BuildVerbArguments();

    /// <summary>Subclasses can override to add secrets to the redaction list.</summary>
    protected virtual IEnumerable<Secret> CollectSecrets() => Array.Empty<Secret>();

    internal CommandPlan ToCommandPlan(Tool tool)
    {
        if (string.IsNullOrEmpty(ResourceGroup))
            throw new InvalidOperationException("ResourceGroup is required (set via SetResourceGroup).");
        if (string.IsNullOrEmpty(Name))
            throw new InvalidOperationException("Name is required (set via SetName).");

        var args = new List<string> { "webapp" };
        args.AddRange(BuildVerbArguments());
        args.Add("--resource-group"); args.Add(ResourceGroup!);
        args.Add("--name"); args.Add(Name!);
        if (!string.IsNullOrEmpty(Subscription)) { args.Add("--subscription"); args.Add(Subscription!); }
        if (JsonOutput) { args.Add("--output"); args.Add("json"); }

        return new CommandPlan
        {
            Executable = tool.Executable.Value,
            Arguments = args,
            Environment = new Dictionary<string, string>(EnvironmentVariables),
            WorkingDirectory = WorkingDirectory ?? tool.WorkingDirectory,
            Secrets = CollectSecrets().ToList(),
        };
    }
}

/// <summary>Fluent setters for the common knobs.</summary>
public static class AzureAppServiceSettingsBaseExtensions
{
    public static T SetWorkingDirectory<T>(this T s, string? cwd) where T : AzureAppServiceSettingsBase { s.WorkingDirectory = cwd; return s; }
    public static T SetSubscription<T>(this T s, string? subscription) where T : AzureAppServiceSettingsBase { s.Subscription = subscription; return s; }
    public static T SetResourceGroup<T>(this T s, string resourceGroup) where T : AzureAppServiceSettingsBase { s.ResourceGroup = resourceGroup; return s; }
    public static T SetName<T>(this T s, string name) where T : AzureAppServiceSettingsBase { s.Name = name; return s; }
    public static T SetJsonOutput<T>(this T s, bool v = true) where T : AzureAppServiceSettingsBase { s.JsonOutput = v; return s; }
    public static T SetEnvironmentVariable<T>(this T s, string name, string value) where T : AzureAppServiceSettingsBase { s.EnvironmentVariables[name] = value; return s; }
}

/// <summary>Settings for <c>az webapp deployment slot swap</c>.</summary>
public sealed class SlotSwapSettings : AzureAppServiceSettingsBase
{
    /// <summary>Source slot. Required. (<c>--slot</c>)</summary>
    public string? Slot { get; set; }
    /// <summary>Target slot. Default <c>production</c>. (<c>--target-slot</c>)</summary>
    public string? TargetSlot { get; set; } = "production";
    /// <summary>Preview swap (<c>--action preview</c>) — stages the swap without performing it.</summary>
    public bool Preview { get; set; }
    /// <summary>Reset slot configuration (<c>--action reset</c>) — discards the staged preview.</summary>
    public bool Reset { get; set; }

    public SlotSwapSettings SetSlot(string slot) { Slot = slot; return this; }
    public SlotSwapSettings SetTargetSlot(string targetSlot) { TargetSlot = targetSlot; return this; }
    public SlotSwapSettings SetPreview(bool v = true) { Preview = v; return this; }
    public SlotSwapSettings SetReset(bool v = true) { Reset = v; return this; }

    protected override IEnumerable<string> BuildVerbArguments()
    {
        if (string.IsNullOrEmpty(Slot)) throw new InvalidOperationException("Slot is required for slot swap (set via SetSlot).");
        if (Preview && Reset) throw new InvalidOperationException("Preview and Reset are mutually exclusive.");
        yield return "deployment";
        yield return "slot";
        yield return "swap";
        yield return "--slot"; yield return Slot!;
        if (!string.IsNullOrEmpty(TargetSlot)) { yield return "--target-slot"; yield return TargetSlot!; }
        if (Preview) { yield return "--action"; yield return "preview"; }
        if (Reset) { yield return "--action"; yield return "reset"; }
    }
}

/// <summary>Settings for <c>az webapp deployment slot list</c>.</summary>
public sealed class SlotListSettings : AzureAppServiceSettingsBase
{
    protected override IEnumerable<string> BuildVerbArguments()
    {
        yield return "deployment";
        yield return "slot";
        yield return "list";
    }
}

/// <summary>Settings for <c>az webapp deployment slot create</c>.</summary>
public sealed class SlotCreateSettings : AzureAppServiceSettingsBase
{
    /// <summary>Slot name to create. Required. (<c>--slot</c>)</summary>
    public string? Slot { get; set; }
    /// <summary>Configuration source slot to clone from (<c>--configuration-source</c>). Optional.</summary>
    public string? ConfigurationSource { get; set; }

    public SlotCreateSettings SetSlot(string slot) { Slot = slot; return this; }
    public SlotCreateSettings SetConfigurationSource(string? source) { ConfigurationSource = source; return this; }

    protected override IEnumerable<string> BuildVerbArguments()
    {
        if (string.IsNullOrEmpty(Slot)) throw new InvalidOperationException("Slot is required for slot create (set via SetSlot).");
        yield return "deployment";
        yield return "slot";
        yield return "create";
        yield return "--slot"; yield return Slot!;
        if (!string.IsNullOrEmpty(ConfigurationSource)) { yield return "--configuration-source"; yield return ConfigurationSource!; }
    }
}

/// <summary>Settings for <c>az webapp deployment slot delete</c>.</summary>
public sealed class SlotDeleteSettings : AzureAppServiceSettingsBase
{
    /// <summary>Slot name to delete. Required.</summary>
    public string? Slot { get; set; }
    public SlotDeleteSettings SetSlot(string slot) { Slot = slot; return this; }

    protected override IEnumerable<string> BuildVerbArguments()
    {
        if (string.IsNullOrEmpty(Slot)) throw new InvalidOperationException("Slot is required for slot delete (set via SetSlot).");
        yield return "deployment";
        yield return "slot";
        yield return "delete";
        yield return "--slot"; yield return Slot!;
    }
}

/// <summary>Settings for <c>az webapp start</c> / <c>stop</c> / <c>restart</c>.</summary>
public sealed class LifecycleSettings : AzureAppServiceSettingsBase
{
    internal enum Verb { Start, Stop, Restart }
    private readonly Verb _verb;
    /// <summary>Optional slot. When set, the operation targets the slot instead of the main app.</summary>
    public string? Slot { get; set; }

    internal LifecycleSettings(Verb verb) { _verb = verb; }
    public LifecycleSettings SetSlot(string? slot) { Slot = slot; return this; }

    protected override IEnumerable<string> BuildVerbArguments()
    {
        yield return _verb switch
        {
            Verb.Start => "start",
            Verb.Stop => "stop",
            Verb.Restart => "restart",
            _ => throw new InvalidOperationException("Unknown verb."),
        };
        if (!string.IsNullOrEmpty(Slot)) { yield return "--slot"; yield return Slot!; }
    }
}
