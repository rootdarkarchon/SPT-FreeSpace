using System.Reflection;
using EFT.InventoryLogic;
using EFT.UI.DragAndDrop;
using HarmonyLib;
using SPTFreeSpace.Capacity;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SPTFreeSpace.UI;

internal static class FreeSpaceOverlayFactory
{
    private static readonly FieldInfo? TagNameField =
        AccessTools.Field(typeof(GridItemView), "TagName");
    private static readonly FieldInfo? TagColorField =
        AccessTools.Field(typeof(GridItemView), "_tagColor");

    private static bool _missingFontLogged;
    private static bool _invalidNamedChildLogged;
    private static bool _missingTagFieldsLogged;

    internal static void Bind(
        GridItemView view,
        Item? item,
        TraderControllerClass? bindController,
        IItemOwner? bindOwner)
    {
        FreeSpaceOverlay? existing = FindExisting(view, out bool hasNamedChild);
        if (item is not CompoundItem container)
        {
            existing?.HideAndClear();
            return;
        }

        if (bindController == null ||
            bindOwner == null ||
            !ItemGridAdapter.HasGridCapacity(container) ||
            !PlayerOwnership.IsPlayerOwned(bindController, bindOwner))
        {
            existing?.HideAndClear();
            return;
        }

        if (existing == null && hasNamedChild)
        {
            if (!_invalidNamedChildLogged)
            {
                _invalidNamedChildLogged = true;
                Plugin.ThrottledLog.Error(
                    "invalid-overlay-child",
                    "A child named 'SPT-FreeSpace.Overlay' exists without the expected " +
                    "component; no duplicate overlay was created.");
            }

            return;
        }

        FreeSpaceOverlay? overlay = existing ?? Create(view);
        overlay?.Bind(view, container, bindController, bindOwner);
    }

    private static FreeSpaceOverlay? FindExisting(
        GridItemView view,
        out bool hasNamedChild)
    {
        Transform child = view.transform.Find(FreeSpaceOverlay.OverlayObjectName);
        hasNamedChild = child != null;
        return child == null ? null : child.GetComponent<FreeSpaceOverlay>();
    }

    private static FreeSpaceOverlay? Create(GridItemView view)
    {
        var overlayObject = new GameObject(
            FreeSpaceOverlay.OverlayObjectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI),
            typeof(Shadow),
            typeof(FreeSpaceOverlay));
        overlayObject.transform.SetParent(view.transform, false);

        var rectTransform = (RectTransform)overlayObject.transform;
        rectTransform.anchorMin = new Vector2(0f, 1f);
        rectTransform.anchorMax = new Vector2(0f, 1f);
        rectTransform.pivot = new Vector2(0f, 1f);
        rectTransform.anchoredPosition = new Vector2(3f, -3f);
        rectTransform.sizeDelta = new Vector2(
            Mathf.Max(18f, view.RectTransform.rect.width - 6f),
            18f);

        TextMeshProUGUI label = overlayObject.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI? fontSource = GetFontSource(view);
        if (fontSource == null)
        {
            UnityEngine.Object.Destroy(overlayObject);
            if (!_missingFontLogged)
            {
                _missingFontLogged = true;
                Plugin.ThrottledLog.Error(
                    "missing-grid-font",
                    "SPT-FreeSpace could not reuse a font from GridItemView labels; " +
                    "the overlay was not created.");
            }

            return null;
        }

        label.font = fontSource.font;

        label.fontSize = 10f;
        label.enableAutoSizing = true;
        label.fontSizeMin = 7f;
        label.fontSizeMax = 10f;
        label.alignment = TextAlignmentOptions.TopLeft;
        label.enableWordWrapping = false;
        label.overflowMode = TextOverflowModes.Overflow;
        label.richText = false;
        label.raycastTarget = false;
        label.color = Color.white;
        label.margin = Vector4.zero;

        Shadow shadow = overlayObject.GetComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.9f);
        shadow.effectDistance = new Vector2(1f, -1f);
        shadow.useGraphicAlpha = true;

        overlayObject.transform.SetAsLastSibling();
        FreeSpaceOverlay overlay = overlayObject.GetComponent<FreeSpaceOverlay>();
        overlay.Initialize(label, GetTagName(view), GetTagBackground(view));
        return overlay;
    }

    private static TextMeshProUGUI? GetTagName(GridItemView view)
    {
        return GetTagFieldValue<TextMeshProUGUI>(TagNameField, view);
    }

    private static Image? GetTagBackground(GridItemView view)
    {
        return GetTagFieldValue<Image>(TagColorField, view);
    }

    private static T? GetTagFieldValue<T>(FieldInfo? field, GridItemView view)
        where T : class
    {
        if (field != null)
        {
            return field.GetValue(view) as T;
        }

        if (!_missingTagFieldsLogged)
        {
            _missingTagFieldsLogged = true;
            Plugin.ThrottledLog.Warning(
                "missing-native-tag-fields",
                "The exact EFT item-tag fields were not found; counters will use the " +
                "normal top-left inset.");
        }

        return null;
    }

    private static TextMeshProUGUI? GetFontSource(GridItemView view)
    {
        TextMeshProUGUI? inscription = view.TextMeshProUGUI_0;
        if (inscription != null && inscription.font != null)
        {
            return inscription;
        }

        TextMeshProUGUI? itemValue = view.ItemValue;
        return itemValue != null && itemValue.font != null ? itemValue : null;
    }
}
