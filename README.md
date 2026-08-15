# SPT-FreeSpace

SPT-FreeSpace is a small client-only inventory UI mod for exactly:

- SPT `4.0.13`
- EFT `0.16.9.0.40087`

It adds a compact recursive-capacity label to player-owned grid-container item
tiles in the stash, player inventory, and opened container grids. The label
defaults to `used/total` and can be switched to `available/total`. The mod does
not add a server component, write profile data, change inventory operations, or
synchronize state.

## Install

1. Close EFT.
2. Open `SPT-FreeSpace-1.0.0.zip`.
3. Extract the `BepInEx` folder into the root of the matching SPT installation.
4. Start SPT and EFT normally.

The installed file is:

```text
BepInEx/plugins/SPT-FreeSpace/SPT-FreeSpace.dll
```

The plugin fails closed with a fatal log message on a different SPT/EFT build
or if its exact item-view hook cannot be resolved.

## Uninstall

Close EFT, then remove:

```text
BepInEx/plugins/SPT-FreeSpace/SPT-FreeSpace.dll
```

The generated config can also be removed if desired:

```text
BepInEx/config/com.rootdarkarchon.spt-freespace.cfg
```

No profile or server cleanup is necessary.

## What the numbers mean

The display is **net usable recursive grid capacity**:

```text
ownTotal
    = sum(grid width × grid height)

ownOccupied
    = sum(actual rotated footprint of every direct grid item)

nestedFootprint
    = sum(actual rotated footprint of each direct child grid container)

available
    = ownTotal - ownOccupied + sum(child.available)

total
    = ownTotal - nestedFootprint + sum(child.total)
```

Results are clamped so `0 <= available <= total`.

For example, a 20-cell parent containing an empty 12-cell child that occupies
4 parent cells has a recursive total of 28. In the default `UsedTotal` mode it
displays `0/28`, changing to `3/28` after adding a 3-cell ordinary item. In
`AvailableTotal` mode the same states display `28/28` and `25/28`.

This is an empty-cell count, not a packing or fit solver. Fragmentation and
item filters can still prevent a particular item from fitting.

## Scope

Shown:

- player stash and equipment/inventory containers;
- containers inside other containers;
- container tiles in multiple opened container windows;
- local-player in-raid inventory when EFT uses the same verified tile path.

Not shown:

- the root stash itself;
- trader stock, flea offers, mail/reward previews, or world/corpse inventory;
- non-grid storage such as slots, cartridges, armor plates, and attachments.

## Configuration

The config file is generated after the first successful load.

| Section | Setting | Default | Meaning |
|---|---|---:|---|
| General | Enabled | `true` | Show or hide existing overlays without reopening the screen. |
| General | Display mode | `UsedTotal` | Select `UsedTotal` or `AvailableTotal`; existing overlays update on the next refresh. |
| General | Refresh interval | `0.25` | Seconds between refreshes; clamped to `0.10–2.00`. |
| Diagnostics | Debug logging | `false` | Log changed item values and a refresh summary every five seconds. |

Keep debug logging off during normal play. Refreshes slower than 10 ms and
cycle/depth guards emit throttled warnings automatically.

## Compatibility

SPT-FreeSpace uses one postfix on the exact 40087
`GridItemView.NewGridItemView(...)` bind/rebind method. EFT's original method
always runs. The overlay has no raycast target, does not participate in layout,
and does not modify existing captions, values, tags, checkmarks, or tooltips.

UI Fixes 5.3.11 and MoreCheckmarks 2.2.0 were present for static compatibility
inspection. Runtime coexistence, ownership filtering, visual placement, and
pooling behavior require the manual test matrix in
[`docs/MANUAL_TEST_MATRIX.md`](docs/MANUAL_TEST_MATRIX.md).

Fika requires no synchronization. Each client that wants the display installs
the DLL locally.

## Build and test

With the exact target installation available:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File .\scripts\Build-Release.ps1 `
  -SptPath 'D:\Tarkov-SPT'
```

The script validates the target versions, builds with project warnings treated
as errors, runs the unit suite, creates `dist`, produces the release ZIP, checks
its one-file layout, and writes a SHA-256 sidecar.
