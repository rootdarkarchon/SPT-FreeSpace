# Codex Handoff — SPT-FreeSpace

## 0. Mission

Implement a small client-only SPT mod named **SPT-FreeSpace** for the user's installed **SPT 4.0.13 / EFT 0.16.9.0.40087** environment.

For every eligible player-owned, grid-based container item rendered in the inventory UI, add a small corner overlay:

```text
available/total
```

The count must recursively include all nested grid containers.

When any container is opened, nested container item tiles inside that window must also show their own recursive values. Do not add a separate window-title or header display.

## 1. Execution rules

These are binding:

1. Read this file, `FEASIBILITY.md`, `SOURCE_NOTES.md`, and `MANUAL_TEST_MATRIX.md` before changing code.
2. Work through milestones M0–M4 sequentially without asking for routine confirmation.
3. Update `STATUS.md` after every milestone with:
   - completed work;
   - exact source symbols mapped;
   - build/test result;
   - remaining blockers;
   - next milestone.
4. Stop only for:
   - a concrete missing source/reference blocker;
   - a runtime behavior that cannot be verified without the user's manual test;
   - a requirement conflict that materially changes the metric.
5. Never invent an obfuscated method, field, or event name.
6. Use the user's matching SPT 4.0.13 source tree as the authority.
7. Do not decompile the installed game binaries. Installed assemblies may be used only as compiler references.
8. Every reflected private member or version-sensitive patch target must be recorded in `SOURCE_NOTES.md` with:
   - source file;
   - declaring type;
   - complete signature;
   - why it is needed;
   - fallback behavior if resolution fails.
9. Prefer public members and `nameof(...)` targets.
10. Use postfixes. Never skip or replace EFT's original item-view behavior.
11. Build after each milestone.
12. Do not deploy a failed or warning-ridden build.
13. Keep the implementation client-only. Do not add a server project, route, profile write, database edit, or synchronization protocol.
14. Keep version 1 intentionally small. Do not add speculative features.

## 2. Product decisions

### 2.1 Display scope

Show the overlay on a rendered item only when all conditions are true:

- the bound item is a `CompoundItem`;
- it owns at least one eligible grid;
- it belongs to the active local player's inventory;
- the plugin is enabled;
- the item view is active.

Cover:

- stash;
- player equipment/inventory;
- nested container grids;
- multiple opened `GridWindow`s;
- in-raid local player inventory if the same verified view path is used.

Exclude:

- root stash itself;
- trader stock;
- flea offers;
- mail/reward previews;
- handbook/inspect-only representations unless they are proven to be the same player-owned item tile;
- non-grid storage.

### 2.2 Capacity metric

Implement **net usable recursive grid capacity** only.

For each container:

```text
ownTotal
    = sum(grid.GridWidth * grid.GridHeight)

ownOccupied
    = sum(actual rotated cell footprint of every item directly in those grids)

nestedFootprint
    = sum(actual rotated footprint of each direct child that is an eligible
          grid container)

available
    = ownTotal - ownOccupied + sum(child.available)

total
    = ownTotal - nestedFootprint + sum(child.total)
```

Clamp malformed results:

```text
total     = max(0, total)
available = clamp(available, 0, total)
```

Interpretation:

- ordinary items reduce `available`, not `total`;
- nested containers reduce their parent's payload ceiling by their footprint and add their own recursive `total`;
- an otherwise empty hierarchy displays `total/total`;
- empty cells count even if fragmented or filtered;
- the value is not a fit solver.

Do not silently switch to gross cells.

### 2.3 Direct-child rule

A child contributes to a container only if the child's actual item address proves it is directly placed in one of that container's grids.

Do not use a recursively flattened enumeration as the direct-item list.

A valid implementation may begin with `GetNotMergedItems()` only if it then filters by exact direct grid parent/address. Prefer a direct grid-item API if the 4.0.13 source exposes one.

### 2.4 Cycle handling

Use:

- a per-refresh memo dictionary keyed by stable item ID;
- an active-recursion set;
- a defensive maximum depth of 64.

When a cycle is detected:

- do not recurse into that child;
- retain its footprint as occupied/structural in the parent calculation;
- add no child capacity;
- log one throttled warning containing the involved item IDs;
- return a safe result rather than throwing.

## 3. Visual behavior

Create one child object per eligible `GridItemView`.

Required properties:

```text
name: SPT-FreeSpace.Overlay
anchor: bottom-right
pivot: bottom-right
inset: approximately 2–3 px
alignment: right
wrap: disabled
raycastTarget: false
render order: last sibling
content: invariant-culture integer "available/total"
```

Use `TextMeshProUGUI`.

Reuse a built-in EFT font from an existing label on the item view or a verified shared UI source. Do not ship an asset bundle.

Use a small outline/shadow or equivalent TMP styling so the text remains readable over item art. Avoid a large opaque background.

The overlay:

- must not resize the item view;
- must not join or alter its layout group;
- must not intercept input;
- must be hidden when unbound, disabled, non-player-owned, or non-container;
- must survive view pooling and rebinding without duplicate children or stale text.

Suggested visual defaults:

```text
font size: 11–12
auto-size minimum: 8
auto-size maximum: 12
single line
```

Do not abbreviate large values. Reduce font size instead.

## 4. Runtime architecture

Use the following responsibilities. Names may vary slightly, but keep the separation.

```text
src/SPT-FreeSpace/
├── Plugin.cs
├── Configuration/
│   └── FreeSpaceSettings.cs
├── Capacity/
│   ├── CapacityResult.cs
│   ├── ContainerCapacityCalculator.cs
│   └── ItemGridAdapter.cs
├── UI/
│   ├── FreeSpaceOverlay.cs
│   ├── FreeSpaceOverlayFactory.cs
│   └── FreeSpaceRefreshService.cs
├── Patches/
│   └── GridItemViewBindPatch.cs
└── Diagnostics/
    └── ThrottledLogger.cs

tests/SPT-FreeSpace.Tests/
├── CapacityFormulaTests.cs
└── CycleAndMemoizationTests.cs
```

Do not create interfaces or service layers that add no testability. The architecture should remain proportionate to a small plugin.

### 4.1 `CapacityResult`

Use an immutable value type:

```csharp
internal readonly record struct CapacityResult(int Available, int Total);
```

If the exact target/compiler setup makes records undesirable, use a readonly struct with value equality.

### 4.2 `ContainerCapacityCalculator`

Responsibilities:

- sum all direct grids;
- enumerate direct grid children;
- obtain rotation-aware footprints using the exact 4.0.13 API;
- recurse into direct grid containers;
- memoize;
- detect cycles/depth overflow;
- clamp malformed values;
- never mutate inventory state;
- run only on the Unity main thread.

Core pseudocode:

```text
Calculate(container, context):
    if memo contains container.Id:
        return memo value

    if container.Id is already active or depth > 64:
        signal cycle and return CycleFailure

    mark active

    ownTotal = sum grid dimensions
    ownOccupied = 0
    nestedFootprint = 0
    childAvailable = 0
    childTotal = 0

    for each item directly placed in container's grids:
        footprint = native rotation-aware footprint
        ownOccupied += footprint

        if item is eligible grid container:
            nestedFootprint += footprint
            childResult = Calculate(item, context)
            if childResult succeeded:
                childAvailable += childResult.Available
                childTotal += childResult.Total

    result.Available = ownTotal - ownOccupied + childAvailable
    result.Total = ownTotal - nestedFootprint + childTotal

    clamp result
    unmark active
    memoize result
    return result
```

Do not count an item more than once when a container has multiple grids.

### 4.3 `FreeSpaceOverlay`

Responsibilities:

- own references to its TMP object and currently bound item;
- `Bind(GridItemView view, Item item)`;
- hide immediately when not eligible;
- register with the refresh service while active;
- apply a new result only when it differs from the last result;
- clear item references on disable/destroy;
- tolerate destroyed Unity objects.

It must not calculate recursively by itself.

### 4.4 `FreeSpaceRefreshService`

Create one persistent main-thread service owned by the plugin.

Default refresh interval:

```text
0.25 seconds, using unscaled time
```

At each refresh:

1. snapshot live registered overlays;
2. discard destroyed/inactive entries;
3. create one calculation context with memoization;
4. refresh all eligible overlays;
5. assign text only when values changed;
6. optionally measure elapsed time when debug logging is enabled.

Do not create one coroutine per overlay.

Do not refresh every Unity frame.

### 4.5 `GridItemViewBindPatch`

M0 must identify the exact 4.0.13 method that runs whenever a `GridItemView` has a valid current `Item`, including pooled/rebound views.

Strong source lead:

```text
GridItemView.UpdateItemName
```

Existing mods patch it successfully, but verify it against the target source.

The postfix shall:

1. ensure exactly one `FreeSpaceOverlay` exists beneath the view;
2. bind the current item;
3. hide/clear for ineligible items.

If source analysis proves one method does not cover creation and rebinding, use one additional verified hook, such as the exact `NewGridItemView` factory result. Do not scatter patches across unrelated methods.

Patch-resolution failure must log a clear fatal error and disable this plugin's feature rather than breaking EFT startup.

## 5. Ownership filter

Implement:

```csharp
bool IsPlayerOwned(Item item)
```

Resolve this from the exact source.

Preferred approach:

- compare the item's owning inventory/controller with the active `ItemUiContext` inventory controller;
- or prove the item's root belongs to active player inventory roots.

Defensively exclude known non-player owner types, but do not guess that a single enum value covers all contexts.

Test player-owned and non-player-owned views on:

- stash screen;
- trader screen;
- flea screen;
- insurance/mail/reward screen if accessible;
- in-raid inventory.

## 6. Configuration

Keep configuration minimal:

```text
General / Enabled
    bool, default true

General / Refresh interval
    float seconds, default 0.25, acceptable range 0.10–2.00

Diagnostics / Debug logging
    bool, default false
```

Clamp invalid interval values.

Do not add colors, corner selection, gross/net mode, localization, or per-container filters in version 1.

The displayed string contains only numbers and `/`, so localization is unnecessary.

## 7. Build and packaging

Target the exact SPT 4.0.13 client environment.

M0 must determine the correct target framework and reference layout from a known-good 4.0.13 client plugin or the local source/build setup. Do not copy a 4.1 project's dependency version blindly.

Expected references include only what is actually needed:

- `Assembly-CSharp`;
- `BepInEx`;
- `0Harmony`;
- `spt-common`;
- `spt-reflection`;
- `UnityEngine.CoreModule`;
- `UnityEngine.UI`;
- `Unity.TextMeshPro`;
- any exact additional Unity module required by the source.

Use an MSBuild `SPTPath` property or local untracked props file. Do not commit the user's absolute path.

Fail the build early with a readable message when required references are missing.

Release output:

```text
dist/
└── BepInEx/
    └── plugins/
        └── SPT-FreeSpace/
            └── SPT-FreeSpace.dll
```

Create:

```text
artifacts/release/SPT-FreeSpace-<version>.zip
```

The ZIP must contain the `BepInEx` folder at its root.

Do not include:

- reference DLLs;
- PDBs unless explicitly requested;
- user-specific config files;
- source-tree paths;
- server files.

## 8. Milestones

### M0 — Source reconnaissance and build skeleton

Deliver:

- solution/project skeleton;
- exact 4.0.13 references;
- `Plugin.cs` loading successfully;
- `SOURCE_NOTES.md` updated with exact signatures for:
  - container grids;
  - direct item enumeration/address;
  - rotated cell size;
  - item-view bind/update hook;
  - player ownership test;
  - built-in TMP font source;
- patch target resolution test/logging;
- clean Release build.

Do not implement speculative reflection before source mapping.

Acceptance:

- plugin DLL builds against exact target;
- game reaches menu with plugin loaded;
- no overlay yet is acceptable;
- no server project exists.

### M1 — Recursive capacity core

Deliver:

- `CapacityResult`;
- calculator;
- direct-child adapter;
- memoization;
- cycle/depth guard;
- formula unit tests;
- all tests green;
- no Unity UI code inside the formula tests.

Required unit cases:

1. empty 10-cell container → `10/10`;
2. ordinary 3-cell item in 10 cells → `7/10`;
3. multi-grid `4×4 + 2×3` empty container → `22/22`;
4. rotated `2×3` item occupies 6 cells regardless orientation;
5. empty 12-cell child occupying 4 cells in a 20-cell parent → `28/28`;
6. same hierarchy plus a 3-cell ordinary parent item → `25/28`;
7. child with 5 occupied payload cells → `23/28`;
8. three nested levels;
9. two sibling containers;
10. cycle detection terminates and reports safely;
11. memoized child calculation is performed once per refresh context;
12. malformed over-occupancy clamps rather than returning negatives.

### M2 — Item-view overlay

Deliver:

- verified `GridItemView` lifecycle patch;
- overlay factory/component;
- visual style;
- exact-one-child behavior;
- hide/rebind behavior;
- player-ownership filter;
- temporary manual logging showing item ID and result only when debug is enabled.

Acceptance:

- player-owned containers display;
- ordinary items do not;
- trader/flea items do not;
- no click/drag behavior is intercepted;
- no duplicate overlays after closing/reopening screens;
- no stale text when a pooled view binds another item.

### M3 — Refresh and nested-window behavior

Deliver:

- central refresh service;
- one timer, not one timer per overlay;
- per-pass calculator memoization;
- configurable interval;
- opened/nested windows covered automatically;
- screen-transition cleanup;
- throttled performance diagnostics.

Acceptance:

- drag/drop updates within `refresh interval + one frame`;
- moving a nested container updates old parent, new parent, and the moved container;
- sorting does not duplicate or lose overlays;
- opening a container shows recursively calculated values on child-container tiles;
- opening several windows remains correct;
- no visible per-frame allocation pattern in ordinary use.

### M4 — Compatibility, documentation, and release

Deliver:

- execute all feasible manual tests;
- document tests requiring the user;
- validate with UI Fixes and MoreCheckmarks installed if available;
- `README.md` with install/uninstall/config/semantics;
- version metadata;
- clean Release build;
- release ZIP;
- SHA-256;
- final `STATUS.md`.

Release gate:

- zero compiler errors;
- zero compiler warnings attributable to this project;
- all unit tests pass;
- no known duplicate/stale overlay bug;
- no trader/flea leakage;
- no server/profile mutation;
- ZIP layout verified.

## 9. Manual-test blocker protocol

Codex should continue through all code and automated validation before stopping for a manual game test.

When a manual test is required, report exactly:

```text
Build:
Artifact:
SHA-256:
Files changed:
Exact test steps:
Expected result:
Log lines to return:
Known uncertainty:
```

Do not ask the user to “see whether it works” without precise steps.

## 10. Definition of done

SPT-FreeSpace is done when:

- a single client DLL installs under `BepInEx/plugins/SPT-FreeSpace`;
- every player-owned grid-container tile in stash/inventory/opened containers displays correct net recursive `available/total`;
- values refresh promptly after inventory changes;
- deep nesting, multi-grid containers, and rotation are correct;
- non-player inventory views remain untouched;
- view pooling causes neither duplicates nor stale values;
- the plugin makes no inventory, profile, network, or server changes;
- the exact SPT 4.0.13 source mappings are documented;
- build, tests, package, and hash are reproducible.
