using EFT.InventoryLogic;
using EFT.UI.DragAndDrop;
using SPTFreeSpace.Capacity;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SPTFreeSpace.UI;

internal static class FreeSpaceOverlayFactory
{
    private static bool _missingFontLogged;
    private static bool _invalidNamedChildLogged;

    internal static void Bind(
        GridItemView view,
        Item? item,
        TraderControllerClass? bindController)
    {
        FreeSpaceOverlay? existing = FindExisting(view, out bool hasNamedChild);
        if (item is not CompoundItem container)
        {
            existing?.HideAndClear();
            return;
        }

        if (bindController == null ||
            !ItemGridAdapter.IsEligibleContainer(container) ||
            !PlayerOwnership.IsPlayerOwned(container, bindController))
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
        overlay?.Bind(view, container, bindController);
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
        rectTransform.anchorMin = new Vector2(0f, 0f);
        rectTransform.anchorMax = new Vector2(0f, 0f);
        rectTransform.pivot = new Vector2(0f, 0f);
        rectTransform.anchoredPosition = new Vector2(3f, 3f);
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

        label.fontSize = 12f;
        label.enableAutoSizing = true;
        label.fontSizeMin = 8f;
        label.fontSizeMax = 12f;
        label.alignment = TextAlignmentOptions.BottomLeft;
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
        overlay.Initialize(label);
        return overlay;
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
