# SPT-FreeSpace

SPT-FreeSpace is a small client-only inventory UI mod for exactly:

- SPT `4.0.13`
- EFT `0.16.9.0.40087`

> **Development disclosure:** This project was generated with AI under human
> direction. Its behavior and compatibility have been tested by a human.

It adds a compact recursive-capacity label to player-owned grid-container item
tiles in the stash, player inventory, and opened container grids. The label
defaults to `used/total` and can be switched to `available/total`. The mod does
not add a server component, write profile data, change inventory operations, or
synchronize state.

## Install

1. Close EFT.
2. Open `SPT-FreeSpace-<version>.zip`.
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

The display is recursive grid capacity. By default, nested container footprints
count as used space:

```text
ownTotal
    = sum(grid width × grid height)

ownOccupied
    = sum(actual rotated footprint of every direct grid item)

nestedFootprint
    = sum(actual rotated footprint of each direct child grid container)

available
    = ownTotal - ownOccupied + sum(child.available)

total (default)
    = ownTotal + sum(child.total)

total (when nested containers do not count as used)
    = ownTotal - nestedFootprint + sum(child.total)
```

Results are clamped so `0 <= available <= total`.

For example, a 20-cell parent containing an empty 12-cell child that occupies
4 parent cells has `28` available cells out of a default recursive total of
`32`. It displays `4/32` in `UsedTotal` mode or `28/32` in `AvailableTotal`
mode. Disabling nested-container used-space counting applies the original net
usable formula and displays `0/28` or `28/28` instead.

This is an empty-cell count, not a packing or fit solver. Fragmentation and
item filters can still prevent a particular item from fitting.

## Scope

Shown:

- player stash and equipment/inventory containers;
- containers inside other containers;
- container tiles in multiple opened container windows;
- local-player in-raid inventory when EFT uses the same verified tile path.

The label sits at the top-left of the tile. When EFT shows an item tag, the
label moves below the native tag strip's actual lower edge; otherwise it uses
the normal top-left inset. The label uses a 10-point maximum font size.

Not shown:

- the root stash itself;
- trader stock, flea offers, mail/reward previews, or world/corpse inventory;
- non-grid storage such as slots, cartridges, armor plates, and attachments.
- Foldables containers while they are folded. Their grid capacity is excluded
  from parent totals until they are unfolded.

## Configuration

The config file is generated after the first successful load.

| Section | Setting | Default | Meaning |
|---|---|---:|---|
| General | Enabled | `true` | Show or hide existing overlays without reopening the screen. |
| General | Display mode | `UsedTotal` | Select `UsedTotal` or `AvailableTotal`; existing overlays update on the next refresh. |
| General | Count nested containers as used space | `true` | Count each nested container's footprint as used. Disable to remove those structural footprints from recursive total capacity. |
| General | Fullness color scale | `false` | When enabled, color the counter green when free, yellow at half full, and red when full. The color always uses fullness, independent of display mode. |
| General | Refresh interval | `0.25` | Seconds between refreshes; clamped to `0.10–2.00`. |
| Diagnostics | Debug logging | `false` | Log changed item values and a refresh summary every five seconds. |

Keep debug logging off during normal play. Refreshes slower than 10 ms and
cycle/depth guards emit throttled warnings automatically.

## Compatibility

SPT-FreeSpace uses one postfix on the exact 40087
`GridItemView.NewGridItemView(...)` bind/rebind method. EFT's original method
always runs. The overlay has no raycast target, does not participate in layout,
and does not modify existing captions, values, tags, checkmarks, or tooltips.
It reads the exact 40087 native tag text/background references only to position
its independent child below a visible tag.

Foldables `1.0.3` is supported without a hard plugin dependency. SPT-FreeSpace
reads the native EFT `FoldableComponent` that Foldables adds to compatible gear;
folded gear is treated as an ordinary occupied item rather than usable nested
capacity.

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
  -SptPath 'D:\Tarkov-SPT' `
  -Version '1.0.0'
```

The script validates the target versions, builds with project warnings treated
as errors, runs the unit suite, creates `dist`, produces the release ZIP, checks
its one-file layout, and writes a SHA-256 sidecar.

## Tagged releases

Pushing a three-part semantic version tag such as `v1.2.3` runs the GitHub
release workflow. The tag supplies `1.2.3` to the shared release script, which
sets the BepInEx plugin metadata, managed assembly/file versions, ZIP filename,
workflow artifact name, and GitHub Release name from that value.

The workflow intentionally uses a Windows self-hosted runner labeled
`spt-4.0.13-40087`. That runner must have the exact supported installation at
`D:\Tarkov-SPT` and GitHub Actions Runner `2.327.1` or newer. Proprietary EFT
and SPT runtime assemblies are not committed to this repository or downloaded
by the workflow.
