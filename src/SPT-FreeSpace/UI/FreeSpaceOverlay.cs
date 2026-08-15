using System;
using EFT.InventoryLogic;
using EFT.UI.DragAndDrop;
using SPTFreeSpace.Capacity;
using SPTFreeSpace.Configuration;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SPTFreeSpace.UI;

internal sealed class FreeSpaceOverlay : MonoBehaviour
{
    internal const string OverlayObjectName = "SPT-FreeSpace.Overlay";

    private TextMeshProUGUI _label = null!;
    private TextMeshProUGUI? _tagLabel;
    private Image? _tagBackground;
    private readonly Vector3[] _tagCorners = new Vector3[4];
    private GridItemView? _view;
    private CompoundItem? _container;
    private TraderControllerClass? _bindController;
    private IItemOwner? _bindOwner;
    private CapacityResult? _lastResult;
    private CapacityDisplayMode? _lastDisplayMode;

    internal void Initialize(
        TextMeshProUGUI label,
        TextMeshProUGUI? tagLabel,
        Image? tagBackground)
    {
        _label = label;
        _tagLabel = tagLabel;
        _tagBackground = tagBackground;
        HideAndClear();
    }

    internal void Bind(
        GridItemView view,
        CompoundItem container,
        TraderControllerClass bindController,
        IItemOwner bindOwner)
    {
        HideAndClear();

        if (!ItemGridAdapter.HasGridCapacity(container) ||
            !PlayerOwnership.IsPlayerOwned(bindController, bindOwner))
        {
            return;
        }

        _view = view;
        _container = container;
        _bindController = bindController;
        _bindOwner = bindOwner;
        _lastResult = null;
        _lastDisplayMode = null;
        _label.enabled = false;
        transform.SetAsLastSibling();
        UpdateLayout();
        Plugin.RefreshService?.Register(this);
    }

    internal void Refresh(
        ContainerCapacityCalculator<CompoundItem> calculator,
        CapacityCalculationContext context)
    {
        try
        {
            if (!IsCurrentBindingLive())
            {
                HideAndClear();
                return;
            }

            if (!Plugin.Settings.Enabled.Value)
            {
                HideLabelAndInvalidate();
                return;
            }

            if (ItemGridAdapter.IsFoldedContainer(_container))
            {
                HideLabelAndInvalidate();
                return;
            }

            CapacityResult result = calculator.Calculate(_container!, context);
            CapacityDisplayMode displayMode = Plugin.Settings.DisplayMode.Value;
            if (_lastResult != result || _lastDisplayMode != displayMode)
            {
                string displayText = CapacityDisplayFormatter.Format(result, displayMode);
                _label.SetText(displayText, true);
                _lastResult = result;
                _lastDisplayMode = displayMode;

                if (Plugin.Settings.DebugLogging.Value)
                {
                    Plugin.Log.LogInfo(
                        $"Item {_container!.Id}: {displayText} ({displayMode}; available/total {result})");
                }
            }

            ApplyColor(result);
            _label.enabled = true;
            transform.SetAsLastSibling();
            UpdateLayout();
        }
        catch (Exception exception)
        {
            string containerId = _container?.Id ?? "unknown";
            HideAndClear();
            Plugin.ThrottledLog.Error(
                $"overlay-refresh-{containerId}",
                $"Failed to refresh free-space overlay for item '{containerId}': {exception}");
        }
    }

    internal void HideAndClear()
    {
        Plugin.RefreshService?.Unregister(this);
        _view = null;
        _container = null;
        _bindController = null;
        _bindOwner = null;
        _lastResult = null;
        _lastDisplayMode = null;

        HideLabel();
    }

    private void HideLabelAndInvalidate()
    {
        _lastResult = null;
        _lastDisplayMode = null;
        HideLabel();
    }

    private void HideLabel()
    {
        if (_label == null)
        {
            return;
        }

        _label.text = string.Empty;
        _label.enabled = false;
        _label.color = Color.white;
    }

    private bool IsCurrentBindingLive()
    {
        return _view != null &&
               _view.gameObject.activeInHierarchy &&
               _container != null &&
               ReferenceEquals(_view.Item, _container) &&
               ItemGridAdapter.HasGridCapacity(_container) &&
               PlayerOwnership.IsPlayerOwned(_bindController, _bindOwner);
    }

    private void UpdateLayout()
    {
        if (_view == null)
        {
            return;
        }

        var rectTransform = (RectTransform)transform;
        float width = Mathf.Max(18f, _view.RectTransform.rect.width - 6f);
        float topOffset = GetTopOffset();

        if (!Mathf.Approximately(rectTransform.sizeDelta.x, width) ||
            !Mathf.Approximately(rectTransform.sizeDelta.y, 16f))
        {
            rectTransform.sizeDelta = new Vector2(width, 16f);
        }

        var position = new Vector2(3f, topOffset);
        if (rectTransform.anchoredPosition != position)
        {
            rectTransform.anchoredPosition = position;
        }
    }

    private float GetTopOffset()
    {
        if (_tagLabel == null ||
            _tagBackground == null ||
            !_tagLabel.enabled ||
            !_tagLabel.gameObject.activeInHierarchy ||
            !_tagBackground.enabled ||
            !_tagBackground.gameObject.activeInHierarchy ||
            string.IsNullOrWhiteSpace(_tagLabel.text))
        {
            return -3f;
        }

        RectTransform viewRect = _view!.RectTransform;
        _tagBackground.rectTransform.GetWorldCorners(_tagCorners);

        float bottom = float.PositiveInfinity;
        foreach (Vector3 corner in _tagCorners)
        {
            float localY = viewRect.InverseTransformPoint(corner).y;
            bottom = Mathf.Min(bottom, localY);
        }

        if (float.IsNaN(bottom) || float.IsInfinity(bottom))
        {
            return -3f;
        }

        float rawOffset = bottom - viewRect.rect.yMax - 2f;
        float lowestVisibleOffset = -Mathf.Max(3f, viewRect.rect.height - 16f);
        return Mathf.Clamp(rawOffset, lowestVisibleOffset, -3f);
    }

    private void ApplyColor(CapacityResult result)
    {
        if (!Plugin.Settings.FullnessColorScale.Value)
        {
            _label.color = Color.white;
            return;
        }

        CapacityColor color = CapacityFullnessColorScale.GetColor(result);
        _label.color = new Color(color.Red, color.Green, color.Blue, 1f);
    }

    private void OnDisable()
    {
        Plugin.RefreshService?.Unregister(this);
        HideLabelAndInvalidate();
    }

    private void OnEnable()
    {
        if (_view != null && _container != null)
        {
            Plugin.RefreshService?.Register(this);
        }
    }

    private void OnDestroy()
    {
        HideAndClear();
    }
}
