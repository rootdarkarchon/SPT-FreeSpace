# Source Notes and Reconnaissance Leads

## Purpose

These references establish that the required APIs and UI patterns exist. They are **not** authorization to assume that current repository HEAD symbols are identical to the user's SPT 4.0.13 source.

Before implementation, replace every “lead” below with an exact mapping from the user's matching source tree.

Use the matching source tree as authority. Native EFT binary inspection is
limited to the exact installed build when the corresponding native declaration
is absent from that tree; do not use the stale `B:\source\eft` or
`B:\source\tarkov2` trees.

## Target baseline

Official SPT build release:

- SPT 4.0.13, build 40087
- EFT 0.16.9.0.40087
- Release page: https://github.com/sp-tarkov/build/releases/tag/4.0.13

Official compatibility guidance states that 4.0.x client mods must be built for the 4.0 line:

- https://github.com/sp-tarkov/wiki/blob/main/Known_Mod_Issues_40.md
- https://github.com/sp-tarkov/wiki/blob/main/How_SPT_Works.md

## API and implementation leads

### 1. Container grids and dimensions

Repository:

- https://github.com/tyfon7/UIFixes
- inspected commit: `dc5af6b37370b31b6937e0ff15c48ac47234361e`

Relevant files:

- `src/Multiselect/MultiGrid.cs`
- `src/R.cs`

Observed leads:

- `CompoundItem.Grids`
- `Grid.ID`
- `Grid.GridWidth`
- `Grid.GridHeight`
- `Grid.ParentItem`
- multi-grid detection through `compoundItem.Grids.Length > 1`
- `GridView.ItemViews` as a dictionary of rendered item views

Verify exact declarations and nullability in 4.0.13.

### 2. Rotation-aware item footprint

Repository/file:

- https://github.com/tyfon7/UIFixes/blob/dc5af6b37370b31b6937e0ff15c48ac47234361e/src/Patches/SwapPatches.cs

Observed leads:

- `Item.CalculateCellSize()`
- `Item.CalculateRotatedSize(ItemRotation)`
- `GridItemAddress`
- `GridItemAddress.LocationInGrid`
- `LocationInGrid.r`

Use the rotation from the item's actual grid address, not a display transform.

### 3. Recursive inventory enumeration and parent relationships

Repository/file:

- https://github.com/tyfon7/AutoDeposit/blob/e56c8f3be12ca1431ef6cb76bba875e646b5730a/AutoDepositPanel.cs

Observed leads:

- `CompoundItem.GetNotMergedItems()`
- `item.Parent.Container.ParentItem`
- `InventoryController.Inventory.Stash`

Warning:

`GetNotMergedItems()` is recursive. It cannot be used directly as the current container's direct children without exact parent/address filtering.

### 4. Open container windows

Repository/file:

- https://github.com/DrakiaXYZ/SPT-QuickMoveToContainer/blob/039132f3a4db809417e57174b7c11412ce49710e/QuickMovePlugin.cs

Observed leads:

- `ItemUiContext._windows`
- `GridWindow`
- `GridWindow._item`
- opened window item as `CompoundItem`

SPT-FreeSpace should not need to enumerate windows if the generic item-view hook is correct. Use this source only to understand the UI model or diagnose missing coverage.

The inspected commit currently declares an SPT 4.1 dependency. Do not copy its dependency declaration into the 4.0.13 project.

### 5. Grid item-view hooks

Repositories/files:

- https://github.com/slpf/CaliberUnderName/blob/0d964ce35a6d3afb6a02ca7f17fed741fc501b9b4/src/Patches/CaliberInNamePatch.cs
- https://github.com/danx91/WishlistExtended/blob/f209b8c22cfbb1aff62e7132e508b3653a4ba9b4/WishlistExtendedClient/Patches/WishlistIconPatches.cs
- https://github.com/IhanaMies/LootValue/blob/1ab49605845284eee639ebaae999e0b7ab20009d/Plugin.cs

Observed leads:

- `GridItemView.Item`
- `GridItemView.UpdateItemName`
- `GridItemView.ItemShortName`
- `GridItemView.NewGridItemView`
- `GridItemView.ValidateWishlistView`
- pointer and tooltip lifecycle methods

Preferred first target to inspect:

```text
GridItemView.UpdateItemName
```

Required M0 answer:

- Is it called after `Item` is assigned?
- Is it called on every new view?
- Is it called on pooled view rebinding?
- Does it cover stash, equipment, and opened `GridWindow` item tiles?
- Does it also run for trader/flea subclasses?
- Is a second creation hook actually necessary?

### 6. Runtime-created item-tile UI

Repository/file:

- https://github.com/ShaneeexD/SPT-TheQuartermaster/blob/10232c84e6ce68664a6aafdb0680d95da7859d87/Client/Patches/ScavengedTagPatch.cs

Observed pattern:

- create a uniquely named `GameObject`;
- set parent with `worldPositionStays: false`;
- add/configure `RectTransform`;
- add Unity UI/TMP components;
- set anchors/pivot/position/size;
- set `raycastTarget = false`;
- use `SetAsLastSibling()`.

Do not reuse or reposition EFT's tag object for SPT-FreeSpace. Create an independent child.

### 7. TextMeshPro and Harmony client UI patches

Repository/file:

- https://github.com/TommySoucy/MoreCheckmarks/blob/96c260884003d97ce06d6d09f2aaed54fc7fbc01/Client/Patches.cs

Observed leads:

- client-side Harmony patching against named EFT UI classes;
- `TextMeshProUGUI`;
- Unity UI components;
- item-aware UI updates.

SPT-FreeSpace should reuse an existing font and does not need MoreCheckmarks' asset bundle.

### 8. Typical client project references

Repositories/files:

- https://github.com/tyfon7/UIFixes/blob/dc5af6b37370b31b6937e0ff15c48ac47234361e/UIFixes.csproj
- https://github.com/DrakiaXYZ/SPT-QuickMoveToContainer/blob/039132f3a4db809417e57174b7c11412ce49710e/DrakiaXYZ-QuickMoveToContainer.csproj
- https://github.com/TommySoucy/MoreCheckmarks/blob/96c260884003d97ce06d6d09f2aaed54fc7fbc01/Client/MoreCheckmarks.csproj

Observed patterns vary between `netstandard2.1` and .NET Framework-style projects.

Binding rule:

- determine the correct target from the exact user's 4.0.13 environment;
- reference local game/SPT assemblies with `Private=false`;
- do not package runtime/game dependencies;
- include `spt-reflection` if using `ModulePatch`.

## M0 exact source mapping

Native EFT declarations below were mapped from the installed
`D:\Tarkov-SPT\EscapeFromTarkov_Data\Managed\Assembly-CSharp.dll` for build
`0.16.9.0.40087`. Only the listed target types and call sites were inspected.
The potentially stale trees at `B:\source\eft` and `B:\source\tarkov2` were
explicitly excluded. SPT metadata was checked against the read-only official
4.0.13 source tree and installed `spt-core.dll`.

| Concern | Exact source/assembly | Exact declaring type | Exact member/signature | Public/reflected | Why / fallback |
|---|---|---|---|---|---|
| Container grid list | Installed `Assembly-CSharp.dll` | `EFT.InventoryLogic.CompoundItem` | `public StashGridClass[] Grids` | Public | Direct array of grids owned by the compound item. Null/empty means ineligible. |
| Grid width/height | Installed `Assembly-CSharp.dll` | `StashGridClass` | `public int GridWidth { get; set; }`; `public int GridHeight { get; set; }` | Public | Dimensions are multiplied with checked/clamped arithmetic. Malformed dimensions contribute zero. |
| Direct grid children | Installed `Assembly-CSharp.dll` | `StashGridClass` | `public IEnumerable<EFT.InventoryLogic.Item> Items { get; }` | Public | This is the grid's direct collection, not a flattened descendant enumeration. Items are de-duplicated by stable item ID across multiple grids. |
| Item grid address/location | Installed `Assembly-CSharp.dll` | `StashGridClass` | `public LocationInGrid GetItemLocation(EFT.InventoryLogic.Item item)` | Public | Supplies the direct grid placement. Null/missing location skips that malformed entry and emits a throttled warning. |
| Actual rotation | Installed `Assembly-CSharp.dll` | `LocationInGrid` | `public ItemRotation r` | Public | Rotation comes from the item's real grid placement, never the tile transform. |
| Rotation-aware cell size | Installed `Assembly-CSharp.dll` | `EFT.InventoryLogic.Item` | `public XYCellSizeStruct CalculateRotatedSize(ItemRotation rotation)` | Public | Native footprint calculation; malformed/non-positive results contribute zero. |
| Item-view bind/update | Installed `Assembly-CSharp.dll` | `EFT.UI.DragAndDrop.GridItemView` | `public GridItemView NewGridItemView(Item item, ItemContextAbstractClass sourceContext, ItemRotation rotation, TraderControllerClass itemController, IItemOwner itemOwner, FilterPanel filterPanel, global::IContainer container, ItemUiContext itemUiContext, InsuranceCompanyClass insurance, GClass2067 wishlistManger = null)` | Public Harmony postfix | `UpdateItemName` does not exist in build 40087. `NewGridItemView` calls `ItemView.NewItemView(...)` before returning and is called by the base pool factory and all inspected grid-view subclasses. Resolution failure logs fatal and disables the feature. |
| Current item on view | Installed `Assembly-CSharp.dll` | `EFT.UI.DragAndDrop.ItemView` | `public EFT.InventoryLogic.Item Item { get; set; }` | Public | Set by `NewItemView` before the selected postfix runs. Null/destroyed views are hidden safely. |
| Player ownership | Installed `Assembly-CSharp.dll` | `EFT.InventoryLogic.InventoryController`; `EFT.UI.DragAndDrop.GridView` | `NewGridItemView(...)` bind arguments `TraderControllerClass itemController` and `IItemOwner itemOwner`; `GridView.Show(...)` resolves and retains its grid owner | Public | Require the explicit bind owner and bind controller to be the same profile `InventoryController`. Using the bind owner is significant for nested `GridWindow` tiles and still excludes trader/flea/mail/preview/world owners without relying on `EOwnerType` alone. Failure hides the overlay. |
| Reusable TMP font and tag | Installed `Assembly-CSharp.dll` | `EFT.UI.DragAndDrop.GridItemView` | `public TextMeshProUGUI TextMeshProUGUI_0 { get; }`; `public TextMeshProUGUI ItemValue { get; }`; private non-obfuscated fields `TextMeshProUGUI TagName` and `Image _tagColor` | Public font fallback; narrow exact-field reflection for tag geometry | Copy the first available built-in font from the public labels. The earlier `TextMeshProUGUI_0` assumption was wrong: it is the inscription, while `TagName` and `_tagColor` are the actual native item-tag text/background used by build 40087. The independent overlay reads their visibility and the background rectangle's lower edge without modifying either object. Missing fields fail back to the normal top-left inset. |
| Folded container state | Installed `Assembly-CSharp.dll` and `ItemComponent.Types.dll`; Foldables `1.0.3` source commit `6a954353f396eee8830a5112181b1bbc5a20d609` | `EFT.InventoryLogic.Item`; `EFT.InventoryLogic.FoldableComponent` | `GetItemComponent<FoldableComponent>()`; `FoldableComponent.Folded` | Public | Foldables' backpack/vest classes create a native `FoldableComponent` and add it to `Item.Components`. A folded child retains its occupied parent footprint but contributes no nested available/total capacity; its own overlay remains registered but hidden so unfolding restores it. No Foldables assembly reference or dependency is required. |
| View cleanup/rebind | Installed `Assembly-CSharp.dll` | `EFT.UI.DragAndDrop.GridItemView`; Unity component lifecycle | `public override void Kill()`; `NewGridItemView(...)` above; overlay `OnEnable()` / `OnDisable()` / `OnDestroy()` | Public; no patch needed for `Kill` | The single bind postfix resets pooled views. A disabled view unregisters and hides but retains its binding so a separately created inactive window can re-register on enable. Rebind/destroy clears completely, and a failed rebind hides immediately, preventing stale text. |

### Lifecycle coverage evidence

Installed build 40087 call-site inspection found `NewGridItemView(...)` used by:

- `GridItemView.Create` after `ItemViewFactory.CreateFromPool<GridItemView>(...)`;
- selectable, slot, hideout, transfer, quest, and fast-access grid views;
- trading, ragfair offer/new-offer, mail-transfer, and insurance grid views.

The standalone-container path is `GridWindow.Show(...)` →
`ContainedGridsView.CreateGrids(...)` → `GridView.Show(...)` →
`ItemUiContext.CreateItemView(...)` → `GridItemView.Create(...)` →
`NewGridItemView(...)`. `GridView` passes its separately resolved `itemOwner`
through that chain; the postfix must use that bind argument rather than
re-deriving ownership from the item.

Standalone windows can construct their pooled item tiles under a disabled
parent. Therefore `OnDisable()` cannot discard a valid binding permanently;
`OnEnable()` re-registers it with the central refresh service. This fixes the
window-specific lifecycle gap without enumerating `ItemUiContext._windows` or
adding another Harmony target.

This is both the pool creation/rebind boundary and the shared boundary for the
non-player screens that must be filtered. One postfix is sufficient; no
secondary creation hook is required.

### SPT and build ABI

- Official read-only source: `B:\source\SPT-QuestMap\reference\spt-4.0.13-sources`;
  `Build.props` declares `SptVersion` `4.0.13`.
- Installed `spt-core.dll`: assembly version `4.0.13.0`, target framework
  `.NETStandard,Version=v2.1`, plugin GUID `com.SPT.core`.
- Known-good installed 4.0.13 client plugins also target `netstandard2.1`.
- SPT-FreeSpace uses direct Harmony and public EFT inventory/view members. Its
  only private-member access is the exact, non-obfuscated build-40087
  `GridItemView.TagName` / `_tagColor` pair used read-only for tag-relative
  geometry. It does not require `spt-common` or `spt-reflection`.
