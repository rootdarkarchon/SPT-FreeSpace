using System;
using EFT.InventoryLogic;
using EFT.UI.DragAndDrop;
using SPTFreeSpace.Capacity;
using SPTFreeSpace.Configuration;
using TMPro;
using UnityEngine;

namespace SPTFreeSpace.UI;

internal sealed class FreeSpaceOverlay : MonoBehaviour
{
    internal const string OverlayObjectName = "SPT-FreeSpace.Overlay";

    private TextMeshProUGUI _label = null!;
    private GridItemView? _view;
    private CompoundItem? _container;
    private TraderControllerClass? _bindController;
    private CapacityResult? _lastResult;
    private CapacityDisplayMode? _lastDisplayMode;

    internal void Initialize(TextMeshProUGUI label)
    {
        _label = label;
        HideAndClear();
    }

    internal void Bind(
        GridItemView view,
        CompoundItem container,
        TraderControllerClass bindController)
    {
        HideAndClear();

        if (!ItemGridAdapter.IsEligibleContainer(container) ||
            !PlayerOwnership.IsPlayerOwned(container, bindController))
        {
            return;
        }

        _view = view;
        _container = container;
        _bindController = bindController;
        _lastResult = null;
        _lastDisplayMode = null;
        _label.enabled = false;
        transform.SetAsLastSibling();
        UpdateWidth();
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
                HideLabel();
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

            _label.enabled = true;
            transform.SetAsLastSibling();
            UpdateWidth();
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
    }

    private bool IsCurrentBindingLive()
    {
        return _view != null &&
               _view.gameObject.activeInHierarchy &&
               _container != null &&
               ReferenceEquals(_view.Item, _container) &&
               ItemGridAdapter.IsEligibleContainer(_container) &&
               PlayerOwnership.IsPlayerOwned(_container, _bindController);
    }

    private void UpdateWidth()
    {
        if (_view == null)
        {
            return;
        }

        var rectTransform = (RectTransform)transform;
        float width = Mathf.Max(18f, _view.RectTransform.rect.width - 6f);
        if (!Mathf.Approximately(rectTransform.sizeDelta.x, width))
        {
            rectTransform.sizeDelta = new Vector2(width, 18f);
        }
    }

    private void OnDisable()
    {
        HideAndClear();
    }

    private void OnDestroy()
    {
        HideAndClear();
    }
}
