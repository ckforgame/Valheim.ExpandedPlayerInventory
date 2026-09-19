using HarmonyLib;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace ExpandedPlayerInventory
{
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Show))]
    public static class InventoryGui_Show_Patch
    {
        public static void Postfix(InventoryGui __instance)
        {
            EnsurePlayerInventoryScrollbar(__instance);
        }

        public static void EnsurePlayerInventoryScrollbar(InventoryGui gui)
        {
            try
            {
                if (gui == null || gui.m_playerGrid == null) return;

                var playerGrid = gui.m_playerGrid;
                var playerGridGo = playerGrid.gameObject;
                var gridRect = playerGridGo.GetComponent<RectTransform>();
                if (gridRect == null || playerGridGo.transform.parent == null) return;

                Player localPlayer = Player.m_localPlayer;
                if (localPlayer == null) return;

                var inventory = localPlayer.GetInventory();
                if (inventory == null) return;

                int configRows = ExpandedPlayerInventoryPlugin.PlayerInventoryRows.Value;
                if (inventory.GetHeight() < configRows)
                {
                    inventory.SetHeight(configRows);
                }

                // Crucial: Update player grid BEFORE any sizing or layout so elements are generated
                // and m_gridRoot has its full expanded size immediately on the very first frame!
                playerGrid.UpdateInventory(inventory, localPlayer, gui.m_dragItem);

                int totalRows = Math.Max(inventory.GetHeight(), configRows);
                int visibleRows = Math.Min(6, Math.Max(4, totalRows));
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

                        // 4. Ensure ScrollRect on playerGrid
                        ScrollRect scrollRect = playerGridGo.GetComponent<ScrollRect>() ?? playerGridGo.AddComponent<ScrollRect>();
                        scrollRect.content = playerGrid.m_gridRoot;
                        scrollRect.viewport = gridRect;
                        scrollRect.verticalScrollbar = scrollbar;
                        scrollRect.horizontal = false;
                        scrollRect.vertical = true;
                        scrollRect.movementType = ScrollRect.MovementType.Clamped;
                        scrollRect.scrollSensitivity = playerGrid.m_elementSpace > 0f ? playerGrid.m_elementSpace : 70.5f;
                        scrollRect.inertia = false;
                        scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;

                        // 5. Gamepad scroll follow
                        ScrollRectEnsureVisible ensure = playerGridGo.GetComponent<ScrollRectEnsureVisible>() ?? playerGridGo.AddComponent<ScrollRectEnsureVisible>();
                        ensure.Initialize();
                        playerGrid.m_ensureVisible = ensure;

                        ExpandedPlayerInventoryPlugin.Log.LogInfo($"Player inventory scrollbar created successfully (totalRows={totalRows}, visibleRows={visibleRows})");
                    }
                    else
                    {
                        ExpandedPlayerInventoryPlugin.Log.LogWarning("EnsurePlayerInventoryScrollbar: Could not find template scrollbar in InventoryGui.");
                    }
                }
                else
                {
                    // If scrollbar already exists, update height and ensure active
                    playerGrid.m_scrollbar.gameObject.SetActive(true);
                    playerGrid.m_scrollbar.transform.SetAsLastSibling();
                    RectTransform? barRect = playerGrid.m_scrollbar.transform as RectTransform;
                    if (barRect != null)
                    {
                        barRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, visibleGridHeight);
                        barRect.anchoredPosition = new Vector2(-15f, gridRect.anchoredPosition.y);
                    }
                }

                // 6. Reset view to top on opening so hotbar is visible
                if (totalRows > visibleRows)
                {
                    if (playerGrid.m_gridRoot != null)
                    {
                        // Ensure pivot is top-aligned to prevent Unity ScrollRect bounds displacement
                        playerGrid.m_gridRoot.pivot = new Vector2(playerGrid.m_gridRoot.pivot.x, 1f);
                    }
                    playerGrid.ResetView();
                    if (playerGrid.m_scrollbar != null)
                    {
                        playerGrid.m_scrollbar.value = 1f;
                    }
                    ScrollRect? sr = playerGridGo.GetComponent<ScrollRect>();
                    if (sr != null)
                    {
                        sr.verticalNormalizedPosition = 1f;
                    }
                }
            }
            catch (Exception e)
            {
                ExpandedPlayerInventoryPlugin.Log.LogError($"EnsurePlayerInventoryScrollbar error: {e}");
            }
        }
    }
}