using HarmonyLib;
using System;
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
                InventoryGui_Show_Patch.SetupScrollUI(__instance);
            }
            catch (Exception e)
            {
                ExpandedPlayerInventoryPlugin.Log.LogError($"InventoryGui_Awake_Patch error: {e}");
            }
        }
    }

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Update))]
    public static class InventoryGui_Update_Patch
    {
        private static int _clippingAttemptFrames = 0;
        private const int MaxClippingAttemptFrames = 60;

        public static void Postfix(InventoryGui __instance)
        {
            try
            {
                if (__instance == null || !__instance.m_animator.GetBool("visible")) return;

                if (!InventoryGui_Show_Patch._needsClippingRefresh) return;

                var playerGrid = __instance.m_playerGrid;
                if (playerGrid == null) return;

                _clippingAttemptFrames++;

                // Safety: give up after MaxClippingAttemptFrames to avoid infinite loop
                if (_clippingAttemptFrames > MaxClippingAttemptFrames)
                {
                    InventoryGui_Show_Patch._needsClippingRefresh = false;
                    _clippingAttemptFrames = 0;
                    ExpandedPlayerInventoryPlugin.Log.LogWarning("Clipping refresh timed out, clearing flag.");
                    return;
                }

                // Wait until the panel's RectTransform has non-zero world-space size,
                // meaning the Animator's scale transition has progressed enough for
                // RectMask2D to compute a meaningful clipping rect.
                var gridRect = playerGrid.GetComponent<RectTransform>();
                if (gridRect == null) return;

                Vector3 lossyScale = gridRect.lossyScale;
                float worldHeight = gridRect.rect.height * Mathf.Abs(lossyScale.y);

                // If the panel is still at or near zero scale, skip this frame
                if (worldHeight < 1f) return;

                // === Panel has real size — this is the AUTHORITATIVE setup point ===
                // All critical state is applied HERE (not in Show postfix) because:
                // 1. The Animator transition is complete, so clipping rects are valid
                // 2. All other mods' Show postfixes have already run, so our state won't be undone
                // 3. This runs on the first valid frame, giving us the final word on layout

                var playerGridGo = playerGrid.gameObject;

                // Re-ensure ScrollRect exists (may have been skipped if another mod set m_scrollbar first)
                InventoryGui_Show_Patch.EnsureScrollRect(__instance);

                // Re-apply inventory data and grid elements
                Player localPlayer = Player.m_localPlayer;
                if (localPlayer != null)
                {
                    var inventory = localPlayer.GetInventory();
                    if (inventory != null)
                    {
                        int configRows = ExpandedPlayerInventoryPlugin.PlayerInventoryRows.Value;
                        if (inventory.GetHeight() < configRows)
                        {
                            inventory.SetHeight(configRows);
                        }

                        // Force grid to regenerate all slot elements with current inventory state
                        playerGrid.UpdateInventory(inventory, localPlayer, __instance.m_dragItem);
                    }
                }

                // Lock pivot to top so ScrollRect doesn't displace items downward
                if (playerGrid.m_gridRoot != null)
                {
                    playerGrid.m_gridRoot.pivot = new Vector2(playerGrid.m_gridRoot.pivot.x, 1f);
                    playerGrid.m_gridRoot.anchoredPosition = Vector2.zero;

                    // Force layout rebuild on content root so grid elements are properly sized
                    LayoutRebuilder.ForceRebuildLayoutImmediate(playerGrid.m_gridRoot);
                }

                // Force canvas update
                Canvas.ForceUpdateCanvases();

                // Lock scroll to top (hotbar visible)
                ScrollRect sr = playerGridGo.GetComponent<ScrollRect>();
                if (sr != null)
                {
                    sr.StopMovement();
                    sr.verticalNormalizedPosition = 1f;
                }

                if (playerGrid.m_scrollbar != null)
                {
                    playerGrid.m_scrollbar.value = 1f;
                }

                // Perform clipping with now-valid rect sizes
                RectMask2D mask = playerGridGo.GetComponent<RectMask2D>();
                if (mask != null)
                {
                    mask.PerformClipping();
                }

                // Done — clear the flag
                int completedFrame = _clippingAttemptFrames;
                InventoryGui_Show_Patch._needsClippingRefresh = false;
                _clippingAttemptFrames = 0;

                ExpandedPlayerInventoryPlugin.Log.LogInfo($"Deferred clipping refresh completed on attempt {completedFrame}, worldHeight={worldHeight:F1}");
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
        // Flag: set to true by Show postfix, consumed by Update patch
        // after the Animator transition completes and the panel has real size.
        internal static bool _needsClippingRefresh = false;

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
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = playerGrid.m_elementSpace > 0f ? playerGrid.m_elementSpace : 70.5f;
            scrollRect.inertia = false;
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

            // 1. Sizing the player grid viewport and background frame to visible rows
            float visibleGridHeight = visibleRows * invGridHeight;
            gridRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, visibleGridHeight);

            if (gui.m_player != null)
            {
                float targetPanelHeight = gui.m_playerHeight + (visibleRows - 4) * invGridHeight;
                gui.m_player.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, targetPanelHeight);
            }

            // 2. Ensure RectMask2D on playerGrid to clip offscreen item slots
            RectMask2D mask = playerGridGo.GetComponent<RectMask2D>();
            if (mask == null)
            {
                mask = playerGridGo.AddComponent<RectMask2D>();
            }
            mask.enabled = true;

            // 3. Ensure Scrollbar
            if (playerGrid.m_scrollbar == null)
            {
                Scrollbar templateScrollbar = gui.m_recipeListScroll
                    ?? gui.m_trophyListScroll
                    ?? gui.m_achievementsListScroll
                    ?? gui.m_containerGrid?.m_scrollbar
                    ?? gui.GetComponentInChildren<Scrollbar>(true);

                if (templateScrollbar != null)
                {
                    GameObject playerGridScroll = UnityEngine.Object.Instantiate(
                        templateScrollbar.gameObject,
                        playerGridGo.transform.parent,
                        false);
                    playerGridScroll.name = "PlayerInventoryScrollbar";
                    playerGridScroll.SetActive(true);
                    playerGridScroll.transform.SetAsLastSibling();

                    Scrollbar scrollbar = playerGridScroll.GetComponent<Scrollbar>();
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

                    // Align scrollbar to the right side of m_player
                    RectTransform? barRect = playerGridScroll.GetComponent<RectTransform>();
                    if (barRect != null)
                    {
                        barRect.anchorMin = new Vector2(1f, 0.5f);
                        barRect.anchorMax = new Vector2(1f, 0.5f);
                        barRect.pivot = new Vector2(1f, 0.5f);

                        float barWidth = 22f;
                        barRect.sizeDelta = new Vector2(barWidth, visibleGridHeight);
                        barRect.anchoredPosition = new Vector2(-15f, gridRect.anchoredPosition.y);

                        // Ensure m_player wood frame is wide enough to enclose the scrollbar
                        float requiredWidth = gridRect.rect.width + barWidth + 50f;
                        if (gui.m_player != null && gui.m_player.rect.width < requiredWidth)
                        {
                            gui.m_player.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, requiredWidth);
                        }
                    }

                    ExpandedPlayerInventoryPlugin.Log.LogInfo($"Player inventory scrollbar created successfully (configRows={configRows}, visibleRows={visibleRows})");
                }
                else
                {
                    ExpandedPlayerInventoryPlugin.Log.LogWarning("EnsurePlayerInventoryScrollbar: Could not find template scrollbar in InventoryGui.");
                }
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
                    barRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, visibleGridHeight);
                    barRect.anchoredPosition = new Vector2(-15f, gridRect.anchoredPosition.y);
                }
            }

            // 4. Always ensure ScrollRect + ScrollRectEnsureVisible exist,
            // even if scrollbar was created by another mod (e.g. ValheimPlus)
            EnsureScrollRect(gui);
        }

        public static void EnsurePlayerInventoryScrollbar(InventoryGui gui)
        {
            try
            {
                if (gui == null || gui.m_playerGrid == null) return;

                // Ensure UI components are created
                SetupScrollUI(gui);

                var playerGrid = gui.m_playerGrid;

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

                // DO NOT set pivot/scroll/clipping here!
                // Other mods' Show postfixes may still run after us and reset grid state.
                // All critical visual state (pivot lock, scroll position, clipping) is
                // deferred to the Update patch which runs after ALL Show postfixes and
                // after the Animator transition gives the panel real world-space size.
                _needsClippingRefresh = true;

                ExpandedPlayerInventoryPlugin.Log.LogInfo($"EnsurePlayerInventoryScrollbar: flag set, rows={Math.Max(inventory.GetHeight(), configRows)}, rootRect={playerGrid.m_gridRoot?.rect}");
            }
            catch (Exception e)
            {
                ExpandedPlayerInventoryPlugin.Log.LogError($"EnsurePlayerInventoryScrollbar error: {e}");
            }
        }
    }
}