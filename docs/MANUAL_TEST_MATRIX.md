# SPT-FreeSpace Manual Test Matrix

## Test setup

Record:

```text
SPT version:
EFT build:
SPT-FreeSpace version:
Display mode:
Fika version:
UI Fixes version:
MoreCheckmarks version:
Other item-UI mods:
Profile used:
```

Enable debug logging only while diagnosing.

Use at least:

- one single-grid container;
- one multi-grid container, such as a rig with multiple grids;
- one container inside another container;
- three levels of nesting if the current item rules permit it;
- ordinary items with known cell footprints;
- one rotated non-square item;
- one tagged container;
- trader and flea screens.

## A. Loading and basic UI

| ID | Action | Expected |
|---|---|---|
| A1 | Launch SPT and reach the main menu | Plugin logs one successful load line; no patch-resolution error |
| A2 | Open stash | Eligible player-owned container tiles show one overlay each |
| A3 | Inspect ordinary non-container items | No overlay |
| A4 | Close and reopen stash five times | No duplicate overlay objects or duplicated text |
| A5 | Change UI scale/resolution if available | Overlay remains anchored inside the tile at top-left |
| A6 | Hover, click, drag, and context-click a container | Input behavior is unchanged |
| A7 | View an untagged container | Counter uses the top-left inset |
| A8 | Add a one-line or wrapped tag to the same container | Counter moves immediately below the visible tag without overlap |
| A9 | Compare the counter to native item text | Counter is visibly about 2 points smaller than the previous 12-point build and remains legible |

## B. Basic arithmetic

| ID | Setup/action | Expected |
|---|---|---|
| B1 | In default `UsedTotal` mode, empty a known `N`-cell container | `0/N` |
| B2 | Add an item occupying 1 cell | `1/N` |
| B3 | Add a non-square item occupying 6 cells | Used rises by exactly 6 |
| B4 | Rotate the 6-cell item | Count remains unchanged |
| B5 | Fill all cells | `N/N` |
| B6 | Remove all items | Returns to `0/N` within the configured interval |
| B7 | Change Display mode to `AvailableTotal` while the empty container remains visible | Existing overlay changes to `N/N` within the configured interval |
| B8 | Add an item occupying 1 cell in `AvailableTotal` mode | `(N-1)/N` |
| B9 | Enable `Fullness color scale`, then view an empty container | Counter is green |
| B10 | Fill the same container to approximately 50% | Counter passes through yellow at the midpoint |
| B11 | Fill the same container completely | Counter is red |
| B12 | Switch between `UsedTotal` and `AvailableTotal` without changing contents | Number changes mode; color does not change |
| B13 | Disable `Fullness color scale` | Counter returns to white within the configured interval |

## C. Recursive semantics

Use `AvailableTotal` for C1–C7 so the displayed numerator can be compared
directly with the recursive available-capacity formula.

| ID | Setup/action | Expected |
|---|---|---|
| C1 | With `Count nested containers as used space` enabled, put an empty child container inside an empty parent | Parent total includes all parent and child grid cells; used includes the child's parent-grid footprint |
| C2 | Put ordinary items in the child | Parent and child available counts both decrease correctly |
| C3 | Add a grandchild container | Parent, child, and grandchild show their own recursive totals |
| C4 | Move an item from parent into child | Parent aggregate available remains unchanged when footprint is equal and placement succeeds |
| C5 | Move the child container to another parent | Old parent, new parent, and child update |
| C6 | Remove the child from the hierarchy | Parent total drops by the child's net contribution and releases its parent footprint |
| C7 | Use two sibling child containers | Both contributions are included exactly once |
| C8 | Disable `Count nested containers as used space` while the hierarchy remains visible | Total drops by the sum of nested-container footprints; available is unchanged and used decreases by the same amount |
| C9 | Re-enable the setting | Default footprint-counted total and used values return within the configured interval |

For C8, verify the disabled/original formula manually:

```text
parent total grid cells
- child footprint in parent
+ child recursive total
```

## D. Multi-grid and opened windows

| ID | Action | Expected |
|---|---|---|
| D1 | View a multi-grid container tile | Total is the sum of all direct grid dimensions, adjusted for nested containers |
| D2 | Open the container | Nested container tiles inside show overlays |
| D3 | Open a nested child in a second window | Both windows remain correct |
| D4 | Open three container windows | No missing or duplicate overlays |
| D5 | Sort an opened container | Values remain correct after sorting |
| D6 | Close windows in different orders | No stale references, exceptions, or orphan UI |
| D7 | In a standalone container window, inspect an untagged and tagged nested container | Both show their own values at the correct top-left/tag-relative position |

## E. Ownership filtering

| ID | Screen/item | Expected |
|---|---|---|
| E1 | Player stash side of trader screen | Player containers show |
| E2 | Trader stock side | Trader containers do not show |
| E3 | Flea offer list | Offer containers do not show |
| E4 | Player item-selection side for flea/trader | Player-owned containers show only if rendered as ordinary player inventory tiles |
| E5 | Mail/reward preview | Non-player preview containers do not show |
| E6 | In-raid local inventory | Local player containers show if in-raid support is reached through the same verified path |
| E7 | Loot container/corpse UI | World/corpse-owned containers do not show unless they are already transferred to the player |

## F. View pooling and live updates

| ID | Action | Expected |
|---|---|---|
| F1 | Scroll a long stash so item views are reused | No stale value from a previously displayed item |
| F2 | Move an item repeatedly between two containers | Both values update within interval plus one frame |
| F3 | Quick-move items | Values update |
| F4 | Use auto-sort | Values update without duplicates |
| F5 | Use a compatible auto-deposit/quick-move mod | Values update without needing a dedicated operation patch |
| F6 | Open/close inventory rapidly | No `MissingReferenceException`, `NullReferenceException`, or growing registry |
| F7 | Disable plugin setting | Existing overlays hide promptly |
| F8 | Re-enable plugin setting | Existing eligible overlays return without reopening the screen |

## G. Compatibility

| ID | Setup | Expected |
|---|---|---|
| G1 | UI Fixes enabled | Both mods function |
| G2 | MoreCheckmarks enabled | Checkmarks/tooltips and capacity overlays function |
| G3 | Tagged container | Tag and free-space text remain legible |
| G4 | Value/tooltip mod enabled | No tooltip/input regression |
| G5 | Fika client enabled | No network/session error; feature remains local |
| G6 | Fold an empty Foldables backpack/vest that is inside another container | Folded tile hides its counter; parent retains the folded item's footprint but excludes its internal capacity |
| G7 | Unfold the same item without reopening the screen | Its counter returns and the parent total includes its recursive capacity within the configured interval |

## H. Performance and logs

| ID | Action | Expected |
|---|---|---|
| H1 | Open a densely populated stash | No perceptible recurring hitch |
| H2 | Leave stash open for five minutes | No log spam and no steadily rising memory/registry count |
| H3 | Enable debug timing temporarily | One refresh statistic at the documented cadence, not per item/per frame |
| H4 | Return to main menu and reopen stash | Registry recovers cleanly |
| H5 | Exit game | No shutdown exception from destroyed UI objects |

## Release evidence to capture

- clean Release build output;
- unit-test summary;
- screenshot of a parent and opened nested child;
- screenshot showing trader stock without overlays;
- relevant BepInEx log excerpt;
- release ZIP file listing;
- SHA-256.

## Current implementation evidence (2026-08-15)

Completed without launching EFT:

- exact installed 40087 ABI/signature mapping and target-resolution guard;
- 28/28 build-version, recursive formula, cycle, depth, memoization, rotation, multi-grid,
  sibling, malformed-state, folded-child, display-mode, and fullness-color unit
  tests, including both nested-container footprint policies;
- clean Release solution build with 0 warnings and 0 errors;
- client-only dependency and source scan: no server project, route, network,
  profile, or inventory-mutation implementation;
- static coexistence inspection with installed UI Fixes `5.3.11`,
  MoreCheckmarks `2.2.0`, Fika `2.3.9`, and official Foldables `1.0.3`
  source commit `6a954353f396eee8830a5112181b1bbc5a20d609`;
- one-file ZIP layout and SHA-256 verification.

All checks in sections A through H exercise live Unity/EFT behavior and remain
manual. No runtime result is claimed until those checks are performed against
the packaged DLL.
