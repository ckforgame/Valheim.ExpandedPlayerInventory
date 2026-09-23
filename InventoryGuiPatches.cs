using HarmonyLib;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace ExpandedPlayerInventory
{
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Awake))]
    public static class InventoryGui_Awake_Patch
    {
        public static void Postfix(InventoryGui __instance)
        {
            try
            {
                InventoryGui_Show_Patch.ResetSessionState();
                InventoryGui_Show_Patch.SetupScrollUI(__instance);
            }
            catch (Exception e)
            {
                ExpandedPlayerInventoryPlugin.Log.LogError($"InventoryGui_Awake_Patch error: {e}");
            }
        }
    }

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Hide))]
    public static class InventoryGui_Hide_Patch
    {
        public static void Prefix(InventoryGui __instance)
        {
            try
            {
                if (__instance != null && __instance.m_playerGrid != null)
                {
                    ScrollRect sr = __instance.m_playerGrid.GetComponent<ScrollRect>();
                    if (sr != null)
                    {
                        InventoryGui_Show_Patch._savedScrollNormalizedPosition = Mathf.Clamp01(sr.verticalNormalizedPosition);
                    }
                }
            }
            catch (Exception e)
            {
                ExpandedPlayerInventoryPlugin.Log.LogError($"InventoryGui_Hide_Patch error: {e}");
            }
        }
    }

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Update))]
    public static class InventoryGui_Update_Patch
    {
        private const int MaxOpeningFrames = 30;

        public static void Postfix(InventoryGui __instance)
        {
            try
            {
                if (__instance == null) return;

                var playerGrid = __instance.m_playerGrid;
                if (playerGrid == null) return;

                var playerGridGo = playerGrid.gameObject;
                var gridRect = playerGridGo.GetComponent<RectTransform>();
                if (gridRect == null) return;

                // Failsafe: If mask is ever enabled while viewport height is non-positive or tiny,
                // disable mask immediately so item slots are NEVER culled as invisible
                RectMask2D mask = playerGridGo.GetComponent<RectMask2D>();
                if (mask != null && mask.enabled && gridRect.rect.height <= 10f)
                {
                    mask.enabled = false;
                }

                bool isVisible = __instance.m_animator != null && __instance.m_animator.GetBool("visible");
                if (!isVisible) return;

                // Screen resolution or UI scale change detection
                if (InventoryGui_Show_Patch._hasRecordedBaseOffsets)
                {
                    int screenW = Screen.width;
                    int screenH = Screen.height;
                    if (InventoryGui_Show_Patch._lastScreenWidth != 0 &&
                        (InventoryGui_Show_Patch._lastScreenWidth != screenW || InventoryGui_Show_Patch._lastScreenHeight != screenH))
                    {
                        InventoryGui_Show_Patch._lastScreenWidth = screenW;
                        InventoryGui_Show_Patch._lastScreenHeight = screenH;
                        InventoryGui_Show_Patch._hasRecordedBaseOffsets = false;
                        InventoryGui_Show_Patch.SetupScrollUI(__instance);
                    }
                    else
                    {
                        InventoryGui_Show_Patch._lastScreenWidth = screenW;
                        InventoryGui_Show_Patch._lastScreenHeight = screenH;
                    }
                }

                ScrollRect sr = playerGridGo.GetComponent<ScrollRect>();

                // Track and remember scroll position continuously while inventory is open
                if (sr != null && !InventoryGui_Show_Patch._needsClippingRefresh)
                {
                    InventoryGui_Show_Patch._savedScrollNormalizedPosition = Mathf.Clamp01(sr.verticalNormalizedPosition);
                }

                if (!InventoryGui_Show_Patch._needsClippingRefresh) return;

                InventoryGui_Show_Patch._openingFrames++;
                int frame = InventoryGui_Show_Patch._openingFrames;

                bool remember = ExpandedPlayerInventoryPlugin.RememberScrollPosition == null || ExpandedPlayerInventoryPlugin.RememberScrollPosition.Value;
                float targetScroll = (remember && InventoryGui_Show_Patch._isWorldSessionInitialized)
                    ? InventoryGui_Show_Patch._savedScrollNormalizedPosition
                    : 1f;

                // During initial opening frames, ensure scroll is held at target position
                if (frame <= 3)
                {
                    if (playerGrid.m_gridRoot != null)
                    {
                        playerGrid.m_gridRoot.pivot = new Vector2(playerGrid.m_gridRoot.pivot.x, 1f);
                    }
                    if (sr != null)
                    {
                        sr.StopMovement();
                        sr.verticalNormalizedPosition = targetScroll;
                    }
                    if (playerGrid.m_scrollbar != null)
                    {
                        playerGrid.m_scrollbar.value = targetScroll;
                    }
                }

                // Determine if panel has expanded to full/usable size
                Vector3 lossyScale = gridRect.lossyScale;
                float currentScaleY = Mathf.Abs(lossyScale.y);
                float visibleGridHeight = gridRect.rect.height;
                float worldHeight = visibleGridHeight * currentScaleY;

                // CRITICAL GUARDS:
                // 1. Never finalize on frame 1 (give Unity layout and animator transitions at least 2 frames to settle).
                // 2. Never finalize if viewport height or worldHeight is non-positive or tiny.
                if (frame < 2 || visibleGridHeight <= 10f || worldHeight <= 10f)
                {
                    // Ensure mask remains disabled so graphics are not culled
                    if (mask != null && mask.enabled)
                    {
                        mask.enabled = false;
                    }
                    return;
                }

                // Wait until the panel reaches at least 80% scale OR at least 8 frames have elapsed,
                // or if we hit the safety timeout.
                bool isPanelExpanded = currentScaleY >= 0.8f || worldHeight >= visibleGridHeight * 0.8f || frame >= 8;
                bool isTimeout = frame >= MaxOpeningFrames;

                if (!isPanelExpanded && !isTimeout)
                {
                    // Panel is still scaling up; keep mask disabled so no graphics are culled
                    if (mask != null && mask.enabled)
                    {
                        mask.enabled = false;
                    }
                    return;
                }

                // Authoritative finalization of layout and clipping:
                InventoryGui_Show_Patch.EnsureScrollRect(__instance);

                // Lock pivot
                if (playerGrid.m_gridRoot != null)
                {
                    playerGrid.m_gridRoot.pivot = new Vector2(playerGrid.m_gridRoot.pivot.x, 1f);
                }

                // Force canvas update so all transform matrices and layouts are up-to-date
                Canvas.ForceUpdateCanvases();

                // Finalize target scroll
                if (sr != null)
                {
                    sr.StopMovement();
                    sr.verticalNormalizedPosition = targetScroll;
                }
                if (playerGrid.m_scrollbar != null)
                {
                    playerGrid.m_scrollbar.value = targetScroll;
                }

                // Re-enable RectMask2D now that world-space rect is strictly valid, positive, and expanded
                if (mask != null)
                {
                    mask.enabled = true;
                    mask.PerformClipping();
                }

                // Done - mark world session as initialized and clear the refresh flag
                InventoryGui_Show_Patch._isWorldSessionInitialized = true;
                InventoryGui_Show_Patch._needsClippingRefresh = false;
                InventoryGui_Show_Patch._openingFrames = 0;

                ExpandedPlayerInventoryPlugin.Log.LogInfo($"Inventory opening synchronization completed at frame {frame} (scale={currentScaleY:F2}, worldHeight={worldHeight:F1}, scroll={targetScroll:F2})");
            }
            catch (Exception e)
            {
                ExpandedPlayerInventoryPlugin.Log.LogError($"InventoryGui_Update_Patch error: {e}");
            }
        }
    }

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Show))]
    [HarmonyPriority(Priority.Low)] // Run AFTER other mods' Show postfixes
    public static class InventoryGui_Show_Patch
    {
        internal static bool _isWorldSessionInitialized = false;
        internal static bool _needsClippingRefresh = false;
        internal static int _openingFrames = 0;
        internal static bool _hasRecordedBaseOffsets = false;
        internal static float _baseGridTopOffset = -60f;
        internal static float _baseGridAnchoredX = 0f;

        internal static float _savedScrollNormalizedPosition = 1f;
        internal static int _lastScreenWidth = 0;
        internal static int _lastScreenHeight = 0;

        public static void ResetSessionState()
        {
            _isWorldSessionInitialized = false;
            _needsClippingRefresh = false;
            _openingFrames = 0;
            _hasRecordedBaseOffsets = false;
            _savedScrollNormalizedPosition = 1f;
            _lastScreenWidth = 0;
            _lastScreenHeight = 0;
        }

        public static void Postfix(InventoryGui __instance)
        {
            EnsurePlayerInventoryScrollbar(__instance);
        }

        /// <summary>
        /// Ensures ScrollRect and ScrollRectEnsureVisible components exist on playerGrid,
        /// regardless of whether we created the scrollbar or another mod did.
        /// Called from both SetupScrollUI (initial setup) and Update (deferred re-check).
        /// </summary>
        public static void EnsureScrollRect(InventoryGui gui)
        {
            if (gui == null || gui.m_playerGrid == null) return;

            var playerGrid = gui.m_playerGrid;
            var playerGridGo = playerGrid.gameObject;
            var gridRect = playerGridGo.GetComponent<RectTransform>();
            if (gridRect == null) return;

            // Ensure ScrollRect exists and is properly configured
            ScrollRect scrollRect = playerGridGo.GetComponent<ScrollRect>();
            if (scrollRect == null)
            {
                scrollRect = playerGridGo.AddComponent<ScrollRect>();
            }
            scrollRect.content = playerGrid.m_gridRoot;
            scrollRect.viewport = gridRect;
            scrollRect.verticalScrollbar = playerGrid.m_scrollbar;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.scrollSensitivity = ExpandedPlayerInventoryPlugin.ScrollSensitivity != null && ExpandedPlayerInventoryPlugin.ScrollSensitivity.Value > 0f
                ? ExpandedPlayerInventoryPlugin.ScrollSensitivity.Value
                : 350f;
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;

            // Ensure ScrollRectEnsureVisible for gamepad support
            ScrollRectEnsureVisible ensure = playerGridGo.GetComponent<ScrollRectEnsureVisible>();
            if (ensure == null)
            {
                ensure = playerGridGo.AddComponent<ScrollRectEnsureVisible>();
                ensure.Initialize();
            }
            playerGrid.m_ensureVisible = ensure;
        }

        public static void SetupScrollUI(InventoryGui gui)
        {
            if (gui == null || gui.m_playerGrid == null) return;

            var playerGrid = gui.m_playerGrid;
            var playerGridGo = playerGrid.gameObject;
            var gridRect = playerGridGo.GetComponent<RectTransform>();
            if (gridRect == null || playerGridGo.transform.parent == null) return;

            int configRows = ExpandedPlayerInventoryPlugin.PlayerInventoryRows.Value;
            int visibleRows = Math.Min(6, Math.Max(4, configRows));
            float invGridHeight = gui.m_invGridHeight > 0f ? gui.m_invGridHeight : (playerGrid.m_elementSpace > 0f ? playerGrid.m_elementSpace : 70.5f);
            float visibleGridHeight = visibleRows * invGridHeight;

            // 1. Sizing the player background frame to visible rows FIRST
            if (gui.m_player != null)
            {
                float targetPanelHeight = gui.m_playerHeight + (visibleRows - 4) * invGridHeight;
                gui.m_player.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, targetPanelHeight);
            }

            // 2. Anchor stabilization: Record baseline offset and decouple gridRect from stretch anchors
            if (!_hasRecordedBaseOffsets)
            {
                _baseGridTopOffset = gridRect.offsetMax.y;
                _baseGridAnchoredX = gridRect.anchoredPosition.x;
                _hasRecordedBaseOffsets = true;
            }

            // Lock gridRect to top-anchor so its vertical height is NEVER dependent on parent stretch or layout timing!
            gridRect.anchorMin = new Vector2(gridRect.anchorMin.x, 1f);
            gridRect.anchorMax = new Vector2(gridRect.anchorMax.x, 1f);
            gridRect.pivot = new Vector2(gridRect.pivot.x, 1f);
            gridRect.sizeDelta = new Vector2(gridRect.sizeDelta.x, visibleGridHeight);
            gridRect.anchoredPosition = new Vector2(_baseGridAnchoredX, _baseGridTopOffset);

            // 3. Ensure RectMask2D on playerGrid to clip offscreen item slots
            RectMask2D mask = playerGridGo.GetComponent<RectMask2D>();
            if (mask == null)
            {
                mask = playerGridGo.AddComponent<RectMask2D>();
            }
            mask.enabled = true;

            // 4. Ensure Scrollbar
            if (playerGrid.m_scrollbar == null)
            {
                Scrollbar? templateScrollbar = gui.m_recipeListScroll
                    ?? gui.m_trophyListScroll
                    ?? gui.m_achievementsListScroll
                    ?? gui.m_containerGrid?.m_scrollbar
                    ?? gui.GetComponentInChildren<Scrollbar>(true);

                if (templateScrollbar == null)
                {
                    // Fallback deep search across all loaded scrollbars in scene
                    var allScrollbars = Resources.FindObjectsOfTypeAll<Scrollbar>();
                    if (allScrollbars != null && allScrollbars.Length > 0)
                    {
                        templateScrollbar = allScrollbars.FirstOrDefault(sb => sb != null && sb.gameObject != null && sb.gameObject.scene.isLoaded);
                    }
                }

                GameObject playerGridScroll;
                Scrollbar scrollbar;

                if (templateScrollbar != null)
                {
                    playerGridScroll = UnityEngine.Object.Instantiate(
                        templateScrollbar.gameObject,
                        playerGridGo.transform.parent,
                        false);
                    playerGridScroll.name = "PlayerInventoryScrollbar";
                    playerGridScroll.SetActive(true);
                    playerGridScroll.transform.SetAsLastSibling();

                    scrollbar = playerGridScroll.GetComponent<Scrollbar>();
                    playerGrid.m_scrollbar = scrollbar;
                    scrollbar.interactable = true;
                    scrollbar.enabled = true;

                    // Ensure all Graphic components inside the scrollbar are enabled and visible
                    foreach (var graphic in playerGridScroll.GetComponentsInChildren<Graphic>(true))
                    {
                        graphic.enabled = true;
                        Color c = graphic.color;
                        if (c.a < 0.2f) { c.a = 1f; graphic.color = c; }
                    }
                }
                else
                {
                    // Programmatic fallback scrollbar
                    ExpandedPlayerInventoryPlugin.Log.LogWarning("EnsurePlayerInventoryScrollbar: Template scrollbar not found; creating programmatic fallback scrollbar.");
                    playerGridScroll = CreateProgrammaticScrollbar(playerGridGo.transform.parent);
                    scrollbar = playerGridScroll.GetComponent<Scrollbar>();
                    playerGrid.m_scrollbar = scrollbar;
                }

                // Align scrollbar to the right side of m_player
                RectTransform? barRect = playerGridScroll.GetComponent<RectTransform>();
                if (barRect != null)
                {
                    barRect.anchorMin = new Vector2(1f, 1f);
                    barRect.anchorMax = new Vector2(1f, 1f);
                    barRect.pivot = new Vector2(1f, 1f);

                    float barWidth = 22f;
                    barRect.sizeDelta = new Vector2(barWidth, visibleGridHeight);
                    barRect.anchoredPosition = new Vector2(-15f, _baseGridTopOffset);

                    // Ensure m_player wood frame is wide enough to enclose the scrollbar
                    float requiredWidth = gridRect.rect.width + barWidth + 50f;
                    if (gui.m_player != null && gui.m_player.rect.width < requiredWidth)
                    {
                        gui.m_player.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, requiredWidth);
                    }
                }

                ExpandedPlayerInventoryPlugin.Log.LogInfo($"Player inventory scrollbar ready (configRows={configRows}, visibleRows={visibleRows})");
            }
            else
            {
                // If scrollbar already exists (set by us on Awake, or by another mod),
                // update height and ensure active
                playerGrid.m_scrollbar.gameObject.SetActive(true);
                playerGrid.m_scrollbar.transform.SetAsLastSibling();
                RectTransform? barRect = playerGrid.m_scrollbar.transform as RectTransform;
                if (barRect != null)
                {
                    barRect.anchorMin = new Vector2(1f, 1f);
                    barRect.anchorMax = new Vector2(1f, 1f);
                    barRect.pivot = new Vector2(1f, 1f);
                    barRect.sizeDelta = new Vector2(barRect.sizeDelta.x > 0f ? barRect.sizeDelta.x : 22f, visibleGridHeight);
                    barRect.anchoredPosition = new Vector2(-15f, _baseGridTopOffset);
                }
            }

            // 5. Always ensure ScrollRect + ScrollRectEnsureVisible exist
            EnsureScrollRect(gui);
        }

        private static GameObject CreateProgrammaticScrollbar(Transform parent)
        {
            GameObject scrollbarObj = new GameObject("PlayerInventoryScrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
            scrollbarObj.transform.SetParent(parent, false);

            Image bgImage = scrollbarObj.GetComponent<Image>();
            bgImage.color = new Color(0f, 0f, 0f, 0.4f);

            GameObject slidingArea = new GameObject("Sliding Area", typeof(RectTransform));
            slidingArea.transform.SetParent(scrollbarObj.transform, false);
            RectTransform slidingRect = slidingArea.GetComponent<RectTransform>();
            slidingRect.anchorMin = Vector2.zero;
            slidingRect.anchorMax = Vector2.one;
            slidingRect.sizeDelta = Vector2.zero;

            GameObject handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handle.transform.SetParent(slidingArea.transform, false);
            RectTransform handleRect = handle.GetComponent<RectTransform>();
            handleRect.anchorMin = Vector2.zero;
            handleRect.anchorMax = Vector2.one;
            handleRect.sizeDelta = Vector2.zero;

            Image handleImage = handle.GetComponent<Image>();
            handleImage.color = new Color(0.8f, 0.7f, 0.5f, 0.8f);

            Scrollbar sb = scrollbarObj.GetComponent<Scrollbar>();
            sb.handleRect = handleRect;
            sb.targetGraphic = handleImage;
            sb.direction = Scrollbar.Direction.BottomToTop;

            return scrollbarObj;
        }

        public static void EnsurePlayerInventoryScrollbar(InventoryGui gui)
        {
            try
            {
                if (gui == null || gui.m_playerGrid == null) return;

                // Ensure UI components are created
                SetupScrollUI(gui);

                var playerGrid = gui.m_playerGrid;
                var playerGridGo = playerGrid.gameObject;

                Player localPlayer = Player.m_localPlayer;
                if (localPlayer == null) return;

                var inventory = localPlayer.GetInventory();
                if (inventory == null) return;

                int configRows = ExpandedPlayerInventoryPlugin.PlayerInventoryRows.Value;
                if (inventory.GetHeight() < configRows)
                {
                    inventory.SetHeight(configRows);
                }

                // Pre-populate grid elements so m_gridRoot has its full size
                playerGrid.UpdateInventory(inventory, localPlayer, gui.m_dragItem);

                // Lock pivot to top immediately
                if (playerGrid.m_gridRoot != null)
                {
                    playerGrid.m_gridRoot.pivot = new Vector2(playerGrid.m_gridRoot.pivot.x, 1f);
                }

                // Determine target scroll position
                bool remember = ExpandedPlayerInventoryPlugin.RememberScrollPosition == null || ExpandedPlayerInventoryPlugin.RememberScrollPosition.Value;
                float targetScroll = (remember && _isWorldSessionInitialized)
                    ? _savedScrollNormalizedPosition
                    : 1f;

                // Apply target scroll to scrollbar and scrollrect
                if (playerGrid.m_scrollbar != null)
                {
                    playerGrid.m_scrollbar.value = targetScroll;
                }

                ScrollRect sr = playerGridGo.GetComponent<ScrollRect>();
                if (sr != null)
                {
                    sr.scrollSensitivity = ExpandedPlayerInventoryPlugin.ScrollSensitivity != null && ExpandedPlayerInventoryPlugin.ScrollSensitivity.Value > 0f
                        ? ExpandedPlayerInventoryPlugin.ScrollSensitivity.Value
                        : 350f;
                    sr.StopMovement();
                    sr.verticalNormalizedPosition = targetScroll;
                }

                RectMask2D mask = playerGridGo.GetComponent<RectMask2D>();

                // SUBSEQUENT OPENS IN THE SAME WORLD SESSION:
                // UI components, bounds, and mask are already permanently initialized.
                // Keep the mask enabled and do NOT trigger opening synchronization.
                // Opens cleanly and instantly with exactly 6 rows and scrollbar in place (no flashing!).
                if (_isWorldSessionInitialized)
                {
                    if (mask != null)
                    {
                        mask.enabled = true;
                    }
                    _needsClippingRefresh = false;
                    _openingFrames = 0;
                    return;
                }

                // FIRST OPEN OF THE CURRENT WORLD SESSION:
                // Perform initial opening synchronization to allow canvas layout and clipping to settle
                if (mask != null)
                {
                    mask.enabled = false;
                }

                // Reset frame counter and arm the update synchronization
                _openingFrames = 0;
                _needsClippingRefresh = true;

                ExpandedPlayerInventoryPlugin.Log.LogInfo($"EnsurePlayerInventoryScrollbar: initialized first-time open for world session, rows={Math.Max(inventory.GetHeight(), configRows)}");
            }
            catch (Exception e)
            {
                ExpandedPlayerInventoryPlugin.Log.LogError($"EnsurePlayerInventoryScrollbar error: {e}");
            }
        }
    }
}