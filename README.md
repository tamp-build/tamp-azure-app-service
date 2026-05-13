# Tamp.AzureAppService

> Typed wrappers for Azure App Service slot orchestration + lifecycle verbs. Wraps `az webapp` so build/deploy scripts get readable slot-swap targets without re-deriving the CLI shape.

| Package | Status |
|---|---|
| `Tamp.AzureAppService` | 0.1.0 (initial) |

## Install

```bash
dotnet add package Tamp.AzureAppService
```

Multi-targets net8 / net9 / net10. Requires the `az` CLI on PATH (or resolved via Tamp.AzureCli.V2).

## Quick start — swap staging into production

```csharp
using Tamp;
using Tamp.AzureAppService;

class Build : TampBuild
{
    public static int Main(string[] args) => Execute<Build>(args);

    [FromPath("az")] readonly Tool Az = null!;

    Target SwapToProduction => _ => _
        .Description("Swap staging → production for strata-api-prod.")
        .Executes(() => AppService.SlotSwap(Az, s => s
            .SetResourceGroup("rg-strata-prod")
            .SetName("strata-api-prod")
            .SetSlot("staging")));   // TargetSlot defaults to "production"

    Target PreviewSwap => _ => _
        .Description("Stage the swap without performing it (dry-run for slot config).")
        .Executes(() => AppService.SlotSwap(Az, s => s
            .SetResourceGroup("rg-strata-prod")
            .SetName("strata-api-prod")
            .SetSlot("staging")
            .SetPreview()));
}
```

## Verb surface

| Tamp method | az command | Notes |
|---|---|---|
| `AppService.SlotSwap(...)` | `az webapp deployment slot swap` | Default target slot is `production`. `Preview` and `Reset` are mutually exclusive. |
| `AppService.SlotList(...)` | `az webapp deployment slot list` | |
| `AppService.SlotCreate(...)` | `az webapp deployment slot create` | Optional `ConfigurationSource` clones config from another slot. |
| `AppService.SlotDelete(...)` | `az webapp deployment slot delete` | |
| `AppService.Start(...)` | `az webapp start` | Optional `Slot` to target a non-production slot. |
| `AppService.Stop(...)` | `az webapp stop` | |
| `AppService.Restart(...)` | `az webapp restart` | |

Every verb requires `ResourceGroup` + `Name` (validated at plan-build time, not runtime). `Subscription` is optional when the `az` CLI default is correct.

## Auth

This package doesn't manage auth — it inherits from the `az` CLI's session. Use [`Tamp.AzureCli.V2`](https://github.com/tamp-build/tamp-azure-cli)'s `Login` verb earlier in your target graph if you need explicit credentials per-build (federated workload identity, service principal, etc.).

## JSON output

`JsonOutput` defaults to `true` so the CLI emits machine-readable JSON. Disable via `SetJsonOutput(false)` when you want the default table-format human output.

## Releasing

Releases follow the [Tamp dogfood pattern](MAINTAINERS.md): bump `<Version>` in `Directory.Build.props`, tag `v<X.Y.Z>`, GitHub Actions runs `dotnet tamp Ci` then `dotnet tamp Push`.

## License

MIT. See [LICENSE](LICENSE).
