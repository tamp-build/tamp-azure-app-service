# Changelog

All notable changes to **Tamp.AzureAppService** are recorded here.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/);
versions follow [SemVer](https://semver.org/spec/v2.0.0.html).

## [0.1.0] - 2026-05-13

### Added

- Initial release. Slot orchestration (`SlotSwap`, `SlotList`, `SlotCreate`, `SlotDelete`)
  plus the `Start` / `Stop` / `Restart` lifecycle verbs. Filed under TAM-176.

- `SlotSwap.Preview` and `SlotSwap.Reset` map to `az`'s `--action preview` / `--action reset`
  modes — `Preview` stages the swap without performing it (useful as a dry-run for slot config
  changes); `Reset` discards a staged preview. The two are mutually exclusive at plan-build time.

- Default `TargetSlot` is `production`, since that's the overwhelming majority case for
  `SlotSwap`. Override via `SetTargetSlot(...)` for non-production swaps.

- `JsonOutput` defaults to `true` so callers get machine-readable output without opting in
  per-verb.

### Notes

- Driven by Strata's adoption-wave gap list 2026-05-13. P2 priority (deferable), shipping
  now because the slot-swap surface is small and the surrounding `az webapp` family was
  already authored.
