# SPT-FreeSpace Feasibility Study

## 1. Executive verdict

**Verdict: highly feasible.**

**Estimated feasibility: 8.5/10.**

SPT-FreeSpace can be implemented as one small client-side BepInEx plugin with no server component, no profile writes, no custom item data, and no asset bundle. The capacity calculation is straightforward because EFT exposes grid-based compound items, grid dimensions, item grid addresses, item rotation-aware cell sizes, and ordinary Unity item views.

The main engineering risk is not the recursion or arithmetic. It is choosing and verifying the correct **SPT 4.0.13 item-view lifecycle hook** so overlays are attached exactly once and rebound correctly when EFT pools or refreshes item views.

A deliberately boring architecture—one postfix hook, one overlay component, one central low-frequency refresh scheduler, and one recursive calculator—is preferable to patching every inventory transaction path.

## 2. Requested behavior, made precise

SPT-FreeSpace shall display either:

```text
used/total (default)
available/total (configurable)
```

on every eligible, player-owned, grid-based container item shown in the inventory UI.

This includes containers:

- in the player's stash;
- in the player's equipped inventory;
- inside another container;
- visible inside one or more opened container windows;
- nested to arbitrary practical depth.

When a container is opened, every nested container item rendered inside it shall show its own recursively aggregated value. No special container-window header is required for version 1.

The root stash itself is not an item tile and therefore receives no overlay.

## 3. Capacity semantics

### 3.1 Recommended version-1 metric: net usable capacity

The overlay should answer:

> How many payload cells are currently free across this container and all nested grid containers, out of the maximum payload cells available with the current nested-container arrangement?

A nested container occupies cells in its parent, but those cells are structural overhead rather than stored payload. Its own usable capacity is added recursively.

For container `C`:

```text
ownTotal(C)
    = sum(width × height for every direct grid owned by C)

ownOccupied(C)
    = sum(actual rotated footprint of every direct item in C's grids)

nestedFootprint(C)
    = sum(actual rotated footprint of every direct child that is itself
          an eligible grid container)

available(C)
    = ownTotal(C)
      - ownOccupied(C)
      + sum(available(childContainer))

total(C)
    = ownTotal(C)
      - nestedFootprint(C)
      + sum(total(childContainer))
```

Results are clamped defensively so `0 <= available <= total`.

### 3.2 Example

An outer container has 20 cells. It contains:

- one empty 12-cell child container occupying 4 cells in the outer container;
- one ordinary item occupying 3 cells.

The result is:

```text
outer free cells      = 20 - 4 - 3 = 13
child free cells      = 12
available             = 13 + 12 = 25

outer payload ceiling = 20 - 4 = 16
child payload ceiling = 12
total                 = 16 + 12 = 28

display               = 25/28
```

If the ordinary 3-cell item is removed, the hierarchy displays `28/28`.

### 3.3 Explicit non-meaning

`available` is a count of empty cells. It is **not**:

- the largest contiguous rectangle;
- proof that an arbitrary item will fit;
- adjusted for an individual container's item filters;
- a packing-efficiency solver;
- a count of weapon slots, magazine cartridges, armor plate slots, or other non-grid storage.

This distinction matters because a container may have ten empty cells while still being unable to accept a particular `2×3` item due to fragmentation or filters.

### 3.4 Rejected default: gross grid cells

A simpler gross metric would sum every grid cell in the hierarchy and treat nested-container footprints as occupied. That makes a completely empty nested hierarchy appear partially full. It is mathematically defensible but less useful for the intended “how much usable room do I have?” display.

Do not add a metric-mode option in the initial implementation unless the actual user requests it. Keep version 1 unambiguous.

## 4. Technical evidence

Public SPT client mods already demonstrate the required building blocks:

- `CompoundItem.Grids` and each grid's `GridWidth`, `GridHeight`, `ID`, and `ParentItem` are used by UI Fixes.
- Rotation-aware item footprints are obtained through `Item.CalculateRotatedSize(rotation)`.
- Grid placement is represented by `GridItemAddress` and `LocationInGrid`.
- `CompoundItem.GetNotMergedItems()` and parent-container relationships are used by AutoDeposit for recursive inventory work.
- `GridItemView` is a standard patchable item tile with an accessible `Item`.
- Existing mods patch `GridItemView.UpdateItemName`, `ItemShortName`, `NewGridItemView`, `UpdateTag`, pointer methods, and tooltip methods.
- Opened containers are represented by `GridWindow` instances associated with a `CompoundItem`.
- Existing client mods create and position Unity UI children at runtime, use `TextMeshProUGUI`, and disable raycasts on decorative overlays.

These examples establish the feasibility of both the data traversal and the UI injection. They are API leads, not substitutes for checking the exact local SPT 4.0.13 source.

## 5. Proposed implementation

### 5.1 One client plugin

Package only:

```text
BepInEx/
└── plugins/
    └── SPT-FreeSpace/
        └── SPT-FreeSpace.dll
```

No server mod, HTTP route, profile migration, JSON database edit, or custom asset is required.

### 5.2 Capacity calculator

A dedicated calculator should:

1. accept a `CompoundItem`;
2. enumerate only items directly placed in that container's grids;
3. calculate each direct item's actual rotated footprint;
4. recursively calculate direct child `CompoundItem`s with one or more usable grids;
5. support multi-grid containers;
6. memoize results by item ID for the duration of one refresh pass;
7. use an active-recursion set to prevent cycles in corrupted or unusual modded inventories;
8. log malformed states once rather than throwing through EFT's UI.

It must not recursively enumerate all descendants and then recurse again, because that would double-count deep items.

### 5.3 Overlay component

Each eligible `GridItemView` receives one uniquely named child, for example:

```text
SPT-FreeSpace.Overlay
```

Recommended visual defaults:

- bottom-right anchor;
- 2–3 pixel inset;
- single-line `TextMeshProUGUI`;
- right alignment;
- no wrapping;
- auto-sizing within a conservative range;
- existing EFT/TMP font reused from the view or a nearby built-in label;
- subtle outline or shadow for readability;
- `raycastTarget = false`;
- last sibling so it renders above the item icon;
- no layout component that can resize the parent item view.

Containers are not stackable in normal use, so the bottom-right stack-count area is the least disruptive default. Runtime testing still needs to check tags and other UI mods.

### 5.4 Binding hook

Preferred strategy:

- patch a stable `GridItemView` bind/update method with a postfix;
- verify the exact method and signature in the SPT 4.0.13 source tree;
- ensure the overlay exists;
- bind it to `__instance.Item`;
- hide and clear it for non-container or non-player-owned items.

`GridItemView.UpdateItemName` is a strong candidate because existing mods patch it and it supplies a live `GridItemView`, but it is not binding until verified against the exact target source.

`GridItemView.NewGridItemView` may be used as a secondary creation hook only if the source inspection shows that the chosen update hook does not cover all pooled/rebound views.

Do not replace EFT prefabs and do not suppress original methods.

### 5.5 Refresh strategy

Use a central main-thread refresh service rather than patching every possible inventory mutation:

- active overlays register on enable/bind and unregister on disable/destroy;
- every 0.25 seconds of unscaled time, refresh all active eligible overlays;
- create one per-tick memoization dictionary keyed by item ID;
- calculate each unique container subtree at most once per tick;
- update TMP text only if `available` or `total` changed;
- discard destroyed or invalid Unity objects safely.

Advantages:

- resilient to drag/drop, quick-move, sorting, auto-deposit, transaction callbacks, and mod-added operations;
- avoids fragile version-specific patches on every operation type;
- deterministic maximum staleness of roughly one refresh interval;
- no work every frame;
- memoization prevents repeated traversal when both a parent and its visible nested containers have overlays.

A later optimization can add event-driven invalidation, but it is unnecessary for version 1 unless profiling proves the interval scheduler too costly.

## 6. Player-owned scope

The overlay must not appear on trader stock, flea offers, mail rewards, handbook previews, or arbitrary loot previews.

Codex must inspect the exact 4.0.13 ownership model and implement one reliable `IsPlayerOwned(Item)` check. Preferred evidence, in order:

1. compare the item's owner/controller with the active `ItemUiContext` inventory controller;
2. otherwise verify ancestry against the active player inventory roots;
3. use `EOwnerType` exclusions only as defensive support, not as the sole guessed criterion.

The same code may naturally work in the in-raid inventory UI. That is acceptable as long as it remains limited to the local player's items.

## 7. Edge cases

The implementation must handle:

- one-grid and multi-grid containers;
- rotated direct items;
- empty containers;
- nested containers three or more levels deep;
- the same nested container tile visible in an opened window while its ancestors remain visible;
- pooled `GridItemView` instances rebound to a different item;
- moving a nested container between parents;
- container sorting;
- container opening and closing;
- containers with item filters;
- custom/modded containers with unusually large grids;
- corrupted cycles or repeated IDs without freezing the UI;
- null/destroyed Unity objects during screen transitions.

Initial scope excludes:

- non-grid slots;
- magazine cartridge capacity;
- weapon attachment slots;
- armor plate slots;
- largest-fit calculations;
- server synchronization;
- window-title overlays;
- total stash-root capacity;
- changing inventory behavior.

## 8. Performance

The expected workload is small.

With a 0.25-second interval and per-tick memoization, one refresh pass is approximately linear in the number of unique items beneath currently visible container overlays. Even an unusually dense stash should remain trivial compared with EFT's normal inventory rendering.

The implementation must still include optional debug timing so a pathological modded inventory can be measured. Do not log every tick by default.

Suggested warning threshold:

```text
log one throttled warning when a refresh pass exceeds 10 ms
```

This threshold is diagnostic, not a release acceptance target.

## 9. Compatibility

The design is deliberately cooperative:

- postfix patches only;
- original methods always run;
- no prefab replacement;
- no modification of existing caption/tag/checkmark objects;
- one uniquely named child object;
- no raycast interception;
- no mutation of item state;
- no network or profile writes.

This should coexist with UI Fixes, MoreCheckmarks, Wishlist extensions, value tooltips, and tag mods. Runtime compatibility still needs manual validation because several mods touch `GridItemView`.

Fika does not need shared state for this feature. Every client that wants the overlay installs the DLL locally.

## 10. Risks and mitigations

| Risk | Probability | Impact | Mitigation |
|---|---:|---:|---|
| Wrong 4.0.13 item-view lifecycle hook | Medium | Medium | Source-map first; patch a verified bind/update method; test view pooling and all inventory screens |
| Overlay appears on trader/flea items | Medium | Low | Implement and test an explicit player-ownership predicate |
| Direct-child enumeration double-counts descendants | Medium | High | Require direct `GridItemAddress` parent matching; cover with unit tests |
| Rotated/folded footprint counted incorrectly | Low–Medium | Medium | Use EFT's native rotation-aware size API from the exact source |
| UI overlap with tags or another mod | Medium | Low | Small bottom-right overlay, last sibling, no layout/raycast; manual compatibility tests |
| Refresh traversal causes menu hitching | Low | Medium | 0.25 s scheduler, per-pass memoization, update text only on change, optional timing |
| Corrupt/modded cycle causes recursion loop | Low | High | Active recursion set, depth guard, throttled warning, fail closed |
| 4.1 source assumptions leak into 4.0.13 build | Medium | High | Build only against exact 4.0.13 references and source; record every mapped symbol |

## 11. Recommendation

Proceed.

The mod is small enough to build cleanly without a server component, but it should not be treated as a ten-line Harmony patch. The correct implementation is a compact, source-mapped plugin with explicit capacity semantics, pooled-view handling, and a central refresh cache.

The first working release should stay narrow: net usable recursive grid capacity, player-owned item tiles, one visual style, and one exact SPT target.
