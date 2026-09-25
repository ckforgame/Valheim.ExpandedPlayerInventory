# Rule: UI Layout, Viewport & RectMask2D Culling Prevention

## Context
Unity UI (`UGUI`) elements like `RectMask2D`, `ScrollRect`, and `RectTransform` are sensitive to layout timing, canvas scaling, and animator transitions.

## Mandatory Rules
1. **Never Enable `RectMask2D` on Invalid or Zero-Height Viewport**:
   - During opening animation frames (frame 1, or scale < 0.8), the inventory window has not finished scaling up.
   - If `RectMask2D.PerformClipping()` executes when `gridRect.rect.height <= 10f` or `worldHeight <= 10f`, Unity treats the clipping area as empty, resulting in a blank wooden board with no items rendered.
   - Always ensure `mask.enabled = false` until the viewport height and lossy scale settle.
2. **Top-Anchor GridRect**:
   - `gridRect` must be decoupled from parent stretching anchors:
     ```csharp
     gridRect.anchorMin = new Vector2(gridRect.anchorMin.x, 1f);
     gridRect.anchorMax = new Vector2(gridRect.anchorMax.x, 1f);
     gridRect.pivot = new Vector2(gridRect.pivot.x, 1f);
     ```
   - This ensures chest container openings or UI scale changes do not collapse the player grid height.
3. **GUI Visible Rows Clamping**:
   - In `InventoryGui.SetInventorySize`, visible rows must be clamped to `Math.Min(6, Math.Max(4, configRows))`.
   - Never allow GUI container to allocate 20 rows on screen at once; rows beyond 6 must be accessed via scrolling.
4. **Performance & GC Allocations**:
   - Never call `Resources.FindObjectsOfTypeAll` during runtime.
   - In `InventoryGui.Update`, use cached component references (`_cachedGridRect`, `_cachedMask`, `_cachedScrollRect`).
   - Fast-bailout when `InventoryGui.m_animator.GetBool("visible") == false`.
