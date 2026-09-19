# SPT-FreeSpace Status

## Target

- SPT: 4.1.x (automated baseline 4.1.6)
- EFT executable file version: 0.16.9.40743
- FreeSpace: 1.1.1
- State: migration implemented and packaged; live-game acceptance pending. Not deployed or published.

## 1.1.1 — Startup version check fix (2026-09-19)

- Report: container counters missing in the hideout.
- Confirmed blocker: installed `BepInEx/LogOutput.log` line 1031 shows 1.1.0
  disabled during startup because the game runtime returned `0.16.9.4074`
  for the required `0.16.9.40743` executable. No FreeSpace rendering hook was
  installed; this is not evidence of a hideout-specific ownership/layout defect.
- Fix: read `FileMajorPart`, `FileMinorPart`, `FileBuildPart`, and
  `FilePrivatePart`, requiring exactly `0`, `16`, `9`, `40743`. Avoid the
  truncated `FileVersion` string without accepting a partial build match.
  Packaging now reads the same numeric fields. UI behavior is unchanged.
- Installed executable numeric fields independently verified in PowerShell:
  `0.16.9.40743`. Reading these fields inside the live game still needs restart
  verification; the original failure was observed in the game log.
- Build: 0 warnings, 0 errors against SPT 4.1.6. Tests: **50/50 passed**,
  including wrong/truncated build, other numeric components, missing version
  fields, and the existing SPT compatibility/capacity regressions.
- Files changed: plugin startup, compatibility policy/tests, project/release
  defaults, release version reader, README and validation documentation.
- Artifact: `artifacts/release/SPT-FreeSpace-1.1.1.zip` and `.zip.sha256`.
  Sole ZIP entry: `BepInEx/plugins/SPT-FreeSpace/SPT-FreeSpace.dll`.
- ZIP SHA-256: `33349e4bf830055f9b4272a34be18a6e74db7c40871650e12a10569c1338a935`.
- Not deployed. Close EFT, install 1.1.1 in the 4.1 client, restart, and verify
  the `Resolved item-view bind hook` and `SPT-FreeSpace 1.1.1 loaded for SPT ...`
  lines. Check player-owned containers in stash, hideout inventory, and opened
  nested windows; scroll them out of view and back. Expect one correct counter
  per eligible tile. Return warnings/errors and the exact screen if any remain
  missing. Live rendering and optional-mod acceptance remain pending.

## 1.1.0 — SPT 4.1 migration (2026-09-18)

- Mapped EFT types using the supplied 4.1 mapping and installed 40743 metadata.
  The ten-parameter item-view hook, native grid traversal/footprints, ownership
  predicate, folded-state handling, tag positioning, and current configuration
  remain intact. The font source is now `GridItemView.ItemInscription`.
- Preserved the existing uncommitted scrolling fix: disabled pooled overlays
  remain tracked; registration requests immediate refresh; rebind/destruction
  clears old state. Existing capacity formulas and display defaults are unchanged.
- Runtime dependency minimum is 4.1.0, with an explicit 4.1 major/minor check
  and exact EFT file-version check. Release validation uses the same policy.
  Fixed the old truncated executable-version constant. Hook resolution errors
  now enter the same guarded startup path as patch application failures.
- Files changed: client plugin/configuration, patch and inventory/UI adapters;
  project/release version defaults; compatibility-policy tests; README and docs.
  No server, network, profile, config schema, game source, or deployment changes.
- Build: `scripts/Build-Release.ps1 -SptPath D:\Tarkov-SPT-4.1 -Version 1.1.0`
  succeeded with **0 warnings, 0 errors** against SPT 4.1.6.
- Tests: **49/49 passed** (28 existing regressions plus 21 compatibility cases).
  Includes 4.1 patch acceptance, malformed/unsupported SPT rejection, and exact
  EFT version rejection. Existing formula/color/display/version tests still pass.
- Read-only Cecil metadata checks passed: unique public instance
  `NewGridItemView` with all ten exact parameter types, return type and named
  postfix arguments; both public TMP getters; both instance tag-field types.
  Installed tag fields are public; exact-field reflection is deliberately retained.
- Compiled metadata checks passed: BepInEx SPT dependency `4.1.0`, plugin version
  `1.1.0`, assembly version `1.1.0.0`. The release script rejected the old
  `D:\Tarkov-SPT` installation before building or packaging.
- Artifact: `artifacts/release/SPT-FreeSpace-1.1.0.zip` and `.zip.sha256`;
  sole ZIP entry verified as `BepInEx/plugins/SPT-FreeSpace/SPT-FreeSpace.dll`.
- ZIP SHA-256: `25308d7d79ae5cd50acf2bf619a2002a5eb429038b22d7b444e43a85082e4a40`.
- Remaining gate: install the package into a matching client after closing EFT,
  restart, and execute `MANUAL_TEST_MATRIX.md` A–H. Prioritize F1/F9 scrolling,
  D2–D7 nested windows, A7–A9 tags, E1–E7 ownership/raid inventory, live settings,
  and G6–G7 fold/unfold. Record matching optional-mod versions for G1/G2/G5.
- Return: successful load and resolved-hook lines, any FreeSpace warnings or
  exceptions, failed test IDs, and screenshots for visual failures. Expected:
  one correct counter per eligible tile, prompt updates, no stale/duplicate
  overlays or excluded-owner leakage, and unchanged input behavior.
- Known uncertainty: no EFT launch, live Harmony installation, UI/raid observation,
  or matching 4.1 optional-mod coexistence run was performed. API metadata and
  unit tests do not establish those behaviors or validate other 4.1 patch releases.

## Historical 4.0.13 / EFT 40087 milestones

The results below describe earlier releases and are not 4.1 acceptance evidence.

## M0 — Source reconnaissance and build skeleton

- State: Complete (source mapping and automated validation)
- Exact symbols mapped: `CompoundItem.Grids`; `StashGridClass.GridWidth`, `GridHeight`, `Items`, and `GetItemLocation(Item)`; `LocationInGrid.r`; `Item.CalculateRotatedSize(ItemRotation)`, `Item.Id`, and `Item.Owner`; `ItemView.Item`; `GridItemView.NewGridItemView(...)`, `TextMeshProUGUI_0`, `ItemValue`, `TagName`, `_tagColor`, and `Kill()`; bind argument `TraderControllerClass itemController` plus `InventoryController` ownership type.
- Files changed: `SPT-FreeSpace.slnx`; `src/SPT-FreeSpace/SPT-FreeSpace.csproj`; `src/SPT-FreeSpace/Plugin.cs`; `src/SPT-FreeSpace/Patches/GridItemViewBindPatch.cs`; `docs/SOURCE_NOTES.md`; `docs/STATUS.md`.
- Build: `dotnet build SPT-FreeSpace.slnx -c Release -p:SPTPath=D:\Tarkov-SPT` — succeeded, 0 warnings, 0 errors.
- Runtime load: Exact SPT/EFT version guard and exact target-resolution fatal fallback implemented. Menu/load verification is deferred to the final precisely specified manual run; it does not block automated milestones.
- Blockers: None. Installed build 40087 has no `GridItemView.UpdateItemName`; the exact replacement hook is the verified public `NewGridItemView(...)` bind/rebind boundary.
- Next: M1 — recursive capacity core, direct-grid adapter, memoization/cycle guards, and required unit cases.

## M1 — Recursive capacity core

- State: Complete
- Files changed: `src/SPT-FreeSpace/Capacity/CapacityResult.cs`; `CapacityGraph.cs`; `CapacityCalculationContext.cs`; `ContainerCapacityCalculator.cs`; `ItemGridAdapter.cs`; `src/SPT-FreeSpace/Properties/AssemblyInfo.cs`; `tests/SPT-FreeSpace.Tests/SPT-FreeSpace.Tests.csproj`; `CapacityFormulaTests.cs`; `CycleAndMemoizationTests.cs`; solution and status files.
- Tests: 14/14 passed. Covers all 12 required cases plus the depth guard and both item orientations. Exact formula cases include empty/occupied/multi-grid, nested child payload, three levels, siblings, cycle retention, per-pass memoization, and malformed over-occupancy clamping.
- Build: Release succeeded, 0 warnings, 0 errors.
- Blockers: None.
- Next: M2 — apply the verified bind postfix, create exactly one pooled overlay child, and enforce exact-controller player ownership.

## M2 — Item-view overlay

- State: Complete (implementation and automated validation)
- Files changed: `src/SPT-FreeSpace/Configuration/FreeSpaceSettings.cs`; `src/SPT-FreeSpace/UI/PlayerOwnership.cs`; `FreeSpaceOverlay.cs`; `FreeSpaceOverlayFactory.cs`; `src/SPT-FreeSpace/Patches/GridItemViewBindPatch.cs`; `src/SPT-FreeSpace/Plugin.cs`; `docs/STATUS.md`.
- Build: Release succeeded, 0 warnings, 0 errors; 14/14 regression tests passed.
- Runtime validation: Verified statically against the exact bind signature. The postfix never skips/replaces original behavior; it creates one uniquely named tag-aware top-left TMP child only for an eligible player-owned container, reuses the tile font, uses a UI `Shadow`, and disables raycasts/wrapping. Ineligible rebind/destroy clears state; temporary Unity disable hides/unregisters while retaining the binding for re-enable. Click/drag, visual overlap, trader/flea filtering, and live pool behavior remain in the final manual matrix.
- Blockers: None.
- Next: M3 — central unscaled timer, shared per-pass context, cleanup, setting transitions, and throttled diagnostics.

## M3 — Refresh and nested-window behavior

- State: Complete (implementation and automated validation)
- Files changed: `src/SPT-FreeSpace/UI/FreeSpaceRefreshService.cs`; `src/SPT-FreeSpace/Diagnostics/ThrottledLogger.cs`; `FreeSpaceOverlay.cs`; `FreeSpaceOverlayFactory.cs`; `CapacityCalculationContext.cs`; `Plugin.cs`; patch and status files.
- Build: Release succeeded, 0 warnings, 0 errors; 14/14 regression tests passed.
- Runtime validation: Generic `NewGridItemView(...)` coverage means opened/nested `GridWindow` tiles register without window enumeration. One service snapshots live overlays, purges destroyed/inactive entries, accepts re-registration when a pooled view becomes active, and refreshes every 0.10–2.00 seconds of unscaled time with one shared memo context. Live drag/drop/sort/window behavior remains in the final manual matrix.
- Performance: No per-overlay coroutine and no every-frame traversal; the only per-frame work is the timer comparison. Snapshot storage is reused. Text changes only when the result changes. Debug summaries are limited to once per 5 seconds; refreshes over 10 ms warn at most once per 30 seconds.
- Blockers: None.
- Next: M4 — docs, compatibility/static checks, packaging, hash, final clean gate, and the precise manual runtime protocol.

## M4 — Compatibility, documentation, and release

- State: Automated work complete; stopped at the required manual Unity/EFT runtime gate.
- Files changed: root `README.md`; `scripts/Build-Release.ps1`; client source under `src/SPT-FreeSpace`; unit tests under `tests/SPT-FreeSpace.Tests`; `docs/SOURCE_NOTES.md`; `docs/MANUAL_TEST_MATRIX.md`; `docs/STATUS.md`; solution/project metadata; generated `dist` and release artifacts.
- Build: Final Release build succeeded against `D:\Tarkov-SPT`, 0 warnings, 0 errors.
- Tests: 28/28 passed, including tag-driven build-version metadata, both nested-container footprint policies, folded-child exclusion, `UsedTotal`, `AvailableTotal`, invalid-mode fallback formatting, and the green/yellow/red fullness scale with malformed-input clamps.
- Manual matrix: Automated/static evidence is recorded in `MANUAL_TEST_MATRIX.md`. Sections A–H remain live runtime checks. Installed compatibility targets detected: UI Fixes `5.3.11`, MoreCheckmarks `2.2.0`, Fika `2.3.9`.
- Artifact: `artifacts/release/SPT-FreeSpace-1.0.0.zip`; verified sole entry `BepInEx/plugins/SPT-FreeSpace/SPT-FreeSpace.dll` (no PDB/reference/config/server files).
- SHA-256: `b0d71e0d5a834703b05302cf56d6dc4fefa1888f6e37667f69253677683dd3d3`; matching `.sha256` sidecar generated.
- Known issues: The reported placement, Foldables, and standalone-window defects are addressed in source and automated validation. Their live Unity behavior remains pending the precise A7–A9, D2–D7, and G6–G7 manual checks.
- Post-gate adjustment: The counter now uses a tag-aware top-left anchor: directly below a visible native item tag, or at the normal top-left inset when untagged.
- Post-gate adjustment: Added `General / Display mode` with enum values `UsedTotal` and `AvailableTotal`, defaulting to `UsedTotal`. Existing overlays reformat on the next refresh when the setting changes; the recursive capacity formula itself is unchanged.
- Post-gate adjustment: Foldables `1.0.3` compatibility uses its source-backed native `FoldableComponent`; folded containers retain their parent footprint but expose no usable nested capacity. Standalone `GridWindow` tiles now use EFT's explicit `itemOwner` bind argument at the existing postfix.
- Post-gate adjustment: Added `General / Fullness color scale`, default `false`. When enabled it derives fullness only from `(total - available) / total`, giving green at empty, yellow at 50%, and red at full regardless of display mode.
- Post-gate adjustment: Corrected tag placement to use build 40087's actual `TagName` / `_tagColor` strip and its transformed lower edge; reduced the maximum font size from 12 to 10 points.
- Post-gate adjustment: Disabled pooled item views now retain their valid binding and re-register on enable, closing the lifecycle gap for tiles created under inactive standalone-window parents. Rebind/destroy still clears state.
- Post-gate adjustment: Added `General / Count nested containers as used space`, default `true`. Enabled keeps child footprints in recursive `total`, so they appear in `used`; disabled subtracts those footprints from `total` and restores the exact original net-usable handoff formula. `available` is identical between the two policies.
- Post-gate adjustment: Added a tag-triggered GitHub release workflow for strict `vMAJOR.MINOR.PATCH` tags. The tag version now drives BepInEx metadata, assembly/file versions, package names, workflow artifacts, and GitHub Releases through the shared release script. CI deliberately requires the exact local game references on a labeled Windows self-hosted runner.
- Post-gate adjustment: Bound overlays now remain tracked while their pooled item view is temporarily disabled. Re-enabling requests an immediate central refresh, while ineligible rebinds and destruction still unregister and clear the overlay. This closes the intermittent missing-counter path observed while scrolling the stash.

## 1.0.1 scrolling lifecycle fix

- Build: Release succeeded against `D:\Tarkov-SPT`, 0 warnings and 0 errors.
- Tests: 28/28 passed.
- Artifact: `artifacts/release/SPT-FreeSpace-1.0.1.zip`; verified sole entry `BepInEx/plugins/SPT-FreeSpace/SPT-FreeSpace.dll`.
- SHA-256: `7d0bec09532f4d5df3d259c51777649469004d240adcd45a7a3480f8e47cb4a8`.
- Runtime validation: Pending a game restart and repeated stash-scroll check F9.
