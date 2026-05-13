using System.Linq;
using Tamp;
using Tamp.AzureAppService;
using Xunit;

namespace Tamp.AzureAppService.Tests;

public sealed class AppServiceTests
{
    private static Tool FakeTool() => new(AbsolutePath.Create("/fake/az"));

    private static int IndexOf(IReadOnlyList<string> args, string token)
    {
        for (var i = 0; i < args.Count; i++) if (args[i] == token) return i;
        return -1;
    }

    // ---- Slot swap ----

    [Fact]
    public void SlotSwap_Builds_Expected_Arguments()
    {
        var plan = AppService.SlotSwap(FakeTool(), s => s
            .SetResourceGroup("rg-strata-dev")
            .SetName("strata-api-dev")
            .SetSlot("staging"));
        Assert.Equal(new[] { "webapp", "deployment", "slot", "swap" }, plan.Arguments.Take(4));
        Assert.Equal("staging", plan.Arguments[IndexOf(plan.Arguments, "--slot") + 1]);
        Assert.Equal("production", plan.Arguments[IndexOf(plan.Arguments, "--target-slot") + 1]);
        Assert.Equal("rg-strata-dev", plan.Arguments[IndexOf(plan.Arguments, "--resource-group") + 1]);
        Assert.Equal("strata-api-dev", plan.Arguments[IndexOf(plan.Arguments, "--name") + 1]);
        Assert.Contains("--output", plan.Arguments);
        Assert.Equal("json", plan.Arguments[IndexOf(plan.Arguments, "--output") + 1]);
    }

    [Fact]
    public void SlotSwap_Preview_Adds_Action_Flag()
    {
        var plan = AppService.SlotSwap(FakeTool(), s => s
            .SetResourceGroup("rg").SetName("app").SetSlot("staging").SetPreview());
        var actionIdx = IndexOf(plan.Arguments, "--action");
        Assert.True(actionIdx >= 0);
        Assert.Equal("preview", plan.Arguments[actionIdx + 1]);
    }

    [Fact]
    public void SlotSwap_Reset_Adds_Action_Flag()
    {
        var plan = AppService.SlotSwap(FakeTool(), s => s
            .SetResourceGroup("rg").SetName("app").SetSlot("staging").SetReset());
        var actionIdx = IndexOf(plan.Arguments, "--action");
        Assert.Equal("reset", plan.Arguments[actionIdx + 1]);
    }

    [Fact]
    public void SlotSwap_Preview_And_Reset_Are_Mutually_Exclusive()
    {
        Assert.Throws<InvalidOperationException>(() =>
            AppService.SlotSwap(FakeTool(), s => s
                .SetResourceGroup("rg").SetName("app").SetSlot("staging")
                .SetPreview().SetReset()).Arguments.ToList());
    }

    [Fact]
    public void SlotSwap_Requires_Slot()
    {
        Assert.Throws<InvalidOperationException>(() =>
            AppService.SlotSwap(FakeTool(), s => s
                .SetResourceGroup("rg").SetName("app")).Arguments.ToList());
    }

    // ---- Common validation ----

    [Fact]
    public void Missing_ResourceGroup_Throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            AppService.SlotList(FakeTool(), s => s.SetName("app")).Arguments.ToList());
    }

    [Fact]
    public void Missing_Name_Throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            AppService.SlotList(FakeTool(), s => s.SetResourceGroup("rg")).Arguments.ToList());
    }

    // ---- Slot list / create / delete ----

    [Fact]
    public void SlotList_Has_Expected_Verb()
    {
        var plan = AppService.SlotList(FakeTool(), s => s.SetResourceGroup("rg").SetName("app"));
        Assert.Equal(new[] { "webapp", "deployment", "slot", "list" }, plan.Arguments.Take(4));
    }

    [Fact]
    public void SlotCreate_With_ConfigurationSource()
    {
        var plan = AppService.SlotCreate(FakeTool(), s => s
            .SetResourceGroup("rg").SetName("app").SetSlot("staging").SetConfigurationSource("production"));
        Assert.Equal(new[] { "webapp", "deployment", "slot", "create" }, plan.Arguments.Take(4));
        Assert.Equal("staging", plan.Arguments[IndexOf(plan.Arguments, "--slot") + 1]);
        Assert.Equal("production", plan.Arguments[IndexOf(plan.Arguments, "--configuration-source") + 1]);
    }

    [Fact]
    public void SlotDelete_Builds_Expected_Verb()
    {
        var plan = AppService.SlotDelete(FakeTool(), s => s
            .SetResourceGroup("rg").SetName("app").SetSlot("old-slot"));
        Assert.Equal(new[] { "webapp", "deployment", "slot", "delete" }, plan.Arguments.Take(4));
        Assert.Equal("old-slot", plan.Arguments[IndexOf(plan.Arguments, "--slot") + 1]);
    }

    // ---- Lifecycle ----

    [Theory]
    [InlineData("start")]
    [InlineData("stop")]
    [InlineData("restart")]
    public void Lifecycle_Verbs_Build_Expected_Command(string verb)
    {
        var plan = verb switch
        {
            "start" => AppService.Start(FakeTool(), s => s.SetResourceGroup("rg").SetName("app")),
            "stop" => AppService.Stop(FakeTool(), s => s.SetResourceGroup("rg").SetName("app")),
            "restart" => AppService.Restart(FakeTool(), s => s.SetResourceGroup("rg").SetName("app")),
            _ => throw new InvalidOperationException(),
        };
        Assert.Equal(new[] { "webapp", verb }, plan.Arguments.Take(2));
    }

    [Fact]
    public void Lifecycle_With_Slot_Adds_Slot_Flag()
    {
        var plan = AppService.Restart(FakeTool(), s => s
            .SetResourceGroup("rg").SetName("app").SetSlot("staging"));
        Assert.Equal("staging", plan.Arguments[IndexOf(plan.Arguments, "--slot") + 1]);
    }

    // ---- JSON output toggle + subscription ----

    [Fact]
    public void JsonOutput_Default_Adds_Output_Json_Flag()
    {
        var plan = AppService.SlotList(FakeTool(), s => s.SetResourceGroup("rg").SetName("app"));
        Assert.Equal("json", plan.Arguments[IndexOf(plan.Arguments, "--output") + 1]);
    }

    [Fact]
    public void JsonOutput_Disable_Removes_Output_Flag()
    {
        var plan = AppService.SlotList(FakeTool(), s => s
            .SetResourceGroup("rg").SetName("app").SetJsonOutput(false));
        Assert.DoesNotContain("--output", plan.Arguments);
    }

    [Fact]
    public void Subscription_When_Set_Adds_Subscription_Flag()
    {
        var plan = AppService.SlotList(FakeTool(), s => s
            .SetResourceGroup("rg").SetName("app").SetSubscription("00000000-0000-0000-0000-000000000000"));
        Assert.Equal("00000000-0000-0000-0000-000000000000",
            plan.Arguments[IndexOf(plan.Arguments, "--subscription") + 1]);
    }

    [Fact]
    public void Executable_Matches_Tool_Path()
    {
        // AbsolutePath normalization differs by OS — assert basename only.
        var plan = AppService.SlotList(FakeTool(), s => s.SetResourceGroup("rg").SetName("app"));
        Assert.EndsWith("az", plan.Executable.TrimEnd(System.IO.Path.DirectorySeparatorChar));
    }
}
