namespace Tamp.AzureAppService;

/// <summary>
/// Typed wrappers for the <c>az webapp</c> subset Strata + adopters use most: slot
/// orchestration plus the start / stop / restart lifecycle verbs. Resolve the
/// <c>az</c> tool via <c>[FromPath("az")]</c> or via <c>Tamp.AzureCli.V2</c>'s
/// existing resolution.
/// </summary>
/// <remarks>
/// <code>
/// [FromPath("az")] readonly Tool Az = null!;
///
/// Target SwapToProduction => _ => _.Executes(() => AppService.SlotSwap(Az, s => s
///     .SetResourceGroup("rg-strata-dev")
///     .SetName("strata-api-dev")
///     .SetSlot("staging")));
/// </code>
/// </remarks>
public static class AppService
{
    /// <summary><c>az webapp deployment slot swap</c> — swap a staging slot into production.</summary>
    public static CommandPlan SlotSwap(Tool tool, Action<SlotSwapSettings> configure)
        => Build<SlotSwapSettings>(tool, configure);

    /// <summary><c>az webapp deployment slot list</c> — enumerate slots on an App Service.</summary>
    public static CommandPlan SlotList(Tool tool, Action<SlotListSettings> configure)
        => Build<SlotListSettings>(tool, configure);

    /// <summary><c>az webapp deployment slot create</c> — provision a new slot.</summary>
    public static CommandPlan SlotCreate(Tool tool, Action<SlotCreateSettings> configure)
        => Build<SlotCreateSettings>(tool, configure);

    /// <summary><c>az webapp deployment slot delete</c> — remove a slot.</summary>
    public static CommandPlan SlotDelete(Tool tool, Action<SlotDeleteSettings> configure)
        => Build<SlotDeleteSettings>(tool, configure);

    /// <summary><c>az webapp start</c>.</summary>
    public static CommandPlan Start(Tool tool, Action<LifecycleSettings> configure)
        => BuildLifecycle(tool, LifecycleSettings.Verb.Start, configure);

    /// <summary><c>az webapp stop</c>.</summary>
    public static CommandPlan Stop(Tool tool, Action<LifecycleSettings> configure)
        => BuildLifecycle(tool, LifecycleSettings.Verb.Stop, configure);

    /// <summary><c>az webapp restart</c>.</summary>
    public static CommandPlan Restart(Tool tool, Action<LifecycleSettings> configure)
        => BuildLifecycle(tool, LifecycleSettings.Verb.Restart, configure);

    // ---- Object-init overloads (TAM-161) ----
    // Parallel surface to the fluent Slot* verbs above. Both styles produce
    // identical CommandPlans; fluent stays canonical in docs and `tamp init`
    // templates.
    //
    //     AppService.SlotSwap(Az, new() { ResourceGroup = "rg", Name = "api", Slot = "staging" });
    //
    // is equivalent to:
    //
    //     AppService.SlotSwap(Az, s => s.SetResourceGroup("rg").SetName("api").SetSlot("staging"));
    //
    // Lifecycle verbs (Start / Stop / Restart) intentionally stay fluent-only:
    // their shared LifecycleSettings carries an internal verb selector — users
    // can't construct it object-init style without choosing a verb, and the
    // current fluent shape (`AppService.Start(tool, s => ...)`) already pins
    // the verb at the call site.
    public static CommandPlan SlotSwap(Tool tool, SlotSwapSettings settings) => Plan(tool, settings);
    public static CommandPlan SlotList(Tool tool, SlotListSettings settings) => Plan(tool, settings);
    public static CommandPlan SlotCreate(Tool tool, SlotCreateSettings settings) => Plan(tool, settings);
    public static CommandPlan SlotDelete(Tool tool, SlotDeleteSettings settings) => Plan(tool, settings);

    private static CommandPlan Build<T>(Tool tool, Action<T> configure)
        where T : AzureAppServiceSettingsBase, new()
    {
        if (tool is null) throw new ArgumentNullException(nameof(tool));
        if (configure is null) throw new ArgumentNullException(nameof(configure));
        var s = new T();
        configure(s);
        return s.ToCommandPlan(tool);
    }

    private static CommandPlan BuildLifecycle(Tool tool, LifecycleSettings.Verb verb, Action<LifecycleSettings> configure)
    {
        if (tool is null) throw new ArgumentNullException(nameof(tool));
        if (configure is null) throw new ArgumentNullException(nameof(configure));
        var s = new LifecycleSettings(verb);
        configure(s);
        return s.ToCommandPlan(tool);
    }

    private static CommandPlan Plan<T>(Tool tool, T settings)
        where T : AzureAppServiceSettingsBase
    {
        if (tool is null) throw new ArgumentNullException(nameof(tool));
        if (settings is null) throw new ArgumentNullException(nameof(settings));
        return settings.ToCommandPlan(tool);
    }
}
