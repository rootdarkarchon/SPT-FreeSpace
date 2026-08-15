# SPT-FreeSpace Status

## Target

- SPT: 4.0.13
- EFT: 0.16.9.0.40087
- Current milestone: M4
- State: M0–M3 complete; M4 automated deliverables complete; precise manual runtime validation required

## M0 — Source reconnaissance and build skeleton

- State: Complete (source mapping and automated validation)
- Exact symbols mapped: `CompoundItem.Grids`; `StashGridClass.GridWidth`, `GridHeight`, `Items`, and `GetItemLocation(Item)`; `LocationInGrid.r`; `Item.CalculateRotatedSize(ItemRotation)`, `Item.Id`, and `Item.Owner`; `ItemView.Item`; `GridItemView.NewGridItemView(...)`, `TextMeshProUGUI_0`, `ItemValue`, and `Kill()`; bind argument `TraderControllerClass itemController` plus `InventoryController` ownership type.
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
- Runtime validation: Verified statically against the exact bind signature. The postfix never skips/replaces original behavior; it creates one uniquely named bottom-left TMP child only for an eligible player-owned container, reuses the tile font, uses a UI `Shadow`, disables raycasts/wrapping, and hides/clears on ineligible rebind or Unity disable/destroy. Click/drag, visual overlap, trader/flea filtering, and live pool behavior remain in the final manual matrix.
- Blockers: None.
- Next: M3 — central unscaled timer, shared per-pass context, cleanup, setting transitions, and throttled diagnostics.

## M3 — Refresh and nested-window behavior

- State: Complete (implementation and automated validation)
- Files changed: `src/SPT-FreeSpace/UI/FreeSpaceRefreshService.cs`; `src/SPT-FreeSpace/Diagnostics/ThrottledLogger.cs`; `FreeSpaceOverlay.cs`; `FreeSpaceOverlayFactory.cs`; `CapacityCalculationContext.cs`; `Plugin.cs`; patch and status files.
- Build: Release succeeded, 0 warnings, 0 errors; 14/14 regression tests passed.
- Runtime validation: Generic `NewGridItemView(...)` coverage means opened/nested `GridWindow` tiles register without window enumeration. One service snapshots live overlays, purges destroyed/inactive entries, and refreshes every 0.10–2.00 seconds of unscaled time with one shared memo context. Live drag/drop/sort/window behavior remains in the final manual matrix.
- Performance: No per-overlay coroutine and no every-frame traversal; the only per-frame work is the timer comparison. Snapshot storage is reused. Text changes only when the result changes. Debug summaries are limited to once per 5 seconds; refreshes over 10 ms warn at most once per 30 seconds.
- Blockers: None.
- Next: M4 — docs, compatibility/static checks, packaging, hash, final clean gate, and the precise manual runtime protocol.

## M4 — Compatibility, documentation, and release

- State: Automated work complete; stopped at the required manual Unity/EFT runtime gate.
- Files changed: root `README.md`; `scripts/Build-Release.ps1`; client source under `src/SPT-FreeSpace`; unit tests under `tests/SPT-FreeSpace.Tests`; `docs/SOURCE_NOTES.md`; `docs/MANUAL_TEST_MATRIX.md`; `docs/STATUS.md`; solution/project metadata; generated `dist` and release artifacts.
- Build: Final Release build succeeded against `D:\Tarkov-SPT`, 0 warnings, 0 errors.
- Tests: 17/17 passed, including `UsedTotal`, `AvailableTotal`, and invalid-mode fallback formatting.
- Manual matrix: Automated/static evidence is recorded in `MANUAL_TEST_MATRIX.md`. Sections A–H remain live runtime checks. Installed compatibility targets detected: UI Fixes `5.3.11`, MoreCheckmarks `2.2.0`, Fika `2.3.9`.
- Artifact: `artifacts/release/SPT-FreeSpace-1.0.0.zip`; verified sole entry `BepInEx/plugins/SPT-FreeSpace/SPT-FreeSpace.dll` (no PDB/reference/config/server files).
- SHA-256: `883880557fd44bb82c7b1b68864d0bffa1aa4b83109a0be28050be98c9abd546`; matching `.sha256` sidecar generated.
- Known issues: No automated/static defect is known. Actual plugin load, overlay placement, ownership filtering, view pooling, live updates, and compatibility/performance are deliberately not claimed until the precisely specified manual run.
- Post-gate adjustment: The counter was moved from the bottom-right to the bottom-left at user request; anchor, pivot, inset, and TMP alignment were changed together. Release validation was rerun afterward.
- Post-gate adjustment: Added `General / Display mode` with enum values `UsedTotal` and `AvailableTotal`, defaulting to `UsedTotal`. Existing overlays reformat on the next refresh when the setting changes; the recursive capacity formula itself is unchanged.
