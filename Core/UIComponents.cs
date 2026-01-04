namespace Turbo.Plugins.Custom.Core
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using Turbo.Plugins.Default;

    /// <summary>
    /// Shared UI Components Library v2.0
    /// 
    /// Provides consistent, reusable UI elements for all custom plugins:
    /// - Buttons, toggles, checkboxes
    /// - Input fields, selectors, sliders
    /// - Panels, cards, modals
    /// - Progress bars, badges, tags
    /// - Lists, tables
    /// - Tooltips
    /// - Item highlights
    /// </summary>
    public static class UIComponents
    {
        #region Core Access

        private static CorePlugin Core => CorePlugin.Instance;
        private static IController Hud => Core?.Hud;

        #endregion

        #region Buttons

        /// <summary>
        /// Draw a standard button
        /// </summary>
        public static void DrawButton(RectangleF rect, string text, bool hovered, bool disabled = false, bool primary = false, bool danger = false)
        {
            if (Core == null) return;

            IBrush brush;
            if (disabled) brush = Core.BtnDisabled;
            else if (danger && hovered) brush = Core.BtnDanger;
            else if (primary && hovered) brush = Core.BtnPrimary;
            else if (hovered) brush = Core.BtnHover;
            else if (primary) brush = Hud.Render.CreateBrush(200, 50, 120, 180, 0);
            else brush = Core.BtnDefault;

            brush.DrawRectangle(rect);

            var font = disabled ? Core.FontMuted : Core.FontBody;
            var layout = font.GetTextLayout(text);
            font.DrawText(layout,
                rect.X + (rect.Width - layout.Metrics.Width) / 2,
                rect.Y + (rect.Height - layout.Metrics.Height) / 2);
        }

        /// <summary>
        /// Draw a small icon button
        /// </summary>
        public static void DrawIconButton(RectangleF rect, string icon, bool hovered, bool danger = false, bool disabled = false)
        {
            if (Core == null) return;

            IBrush brush;
            if (disabled) brush = Core.BtnDisabled;
            else if (danger && hovered) brush = Core.BtnDanger;
            else if (hovered) brush = Core.BtnHover;
            else brush = Core.BtnDefault;

            brush.DrawRectangle(rect);

            var layout = Core.FontIcon.GetTextLayout(icon);
            Core.FontIcon.DrawText(layout,
                rect.X + (rect.Width - layout.Metrics.Width) / 2,
                rect.Y + (rect.Height - layout.Metrics.Height) / 2);
        }

        /// <summary>
        /// Draw a toggle button (two states)
        /// </summary>
        public static void DrawToggleButton(RectangleF rect, string text, bool active, bool hovered)
        {
            if (Core == null) return;

            var brush = active ? Core.BtnActive : (hovered ? Core.BtnHover : Core.BtnDefault);
            brush.DrawRectangle(rect);

            if (active) Core.BorderAccent.DrawRectangle(rect);

            var font = active ? Core.FontBody : Core.FontSmall;
            var layout = font.GetTextLayout(text);
            font.DrawText(layout,
                rect.X + (rect.Width - layout.Metrics.Width) / 2,
                rect.Y + (rect.Height - layout.Metrics.Height) / 2);
        }

        /// <summary>
        /// Draw a tab button
        /// </summary>
        public static void DrawTabButton(RectangleF rect, string icon, string label, bool active, bool hovered)
        {
            if (Core == null) return;

            var brush = active ? Core.BtnActive : (hovered ? Core.BtnHover : Core.BtnDefault);
            brush.DrawRectangle(rect);

            // Draw icon and label vertically centered
            string displayText = string.IsNullOrEmpty(label) ? icon : $"{icon} {label}";
            var layout = Core.FontSmall.GetTextLayout(displayText);
            Core.FontSmall.DrawText(layout,
                rect.X + (rect.Width - layout.Metrics.Width) / 2,
                rect.Y + (rect.Height - layout.Metrics.Height) / 2);
        }

        #endregion

        #region Toggles & Checkboxes

        /// <summary>
        /// Draw a toggle switch
        /// </summary>
        public static void DrawToggle(float x, float y, float width, float height, bool value)
        {
            if (Core == null) return;

            Core.ToggleTrack.DrawRectangle(x, y, width, height);

            float knobSize = height - 4;
            float knobX = value ? x + width - knobSize - 2 : x + 2;
            var knobBrush = value ? Core.ToggleOn : Core.ToggleOff;
            knobBrush.DrawRectangle(knobX, y + 2, knobSize, knobSize);
        }

        /// <summary>
        /// Draw a toggle switch and return its bounding rect
        /// </summary>
        public static RectangleF DrawToggleWithRect(float x, float y, float width, float height, bool value)
        {
            DrawToggle(x, y, width, height, value);
            return new RectangleF(x, y, width, height);
        }

        /// <summary>
        /// Draw a labeled toggle row
        /// </summary>
        public static float DrawLabeledToggle(float x, float y, float width, string label, bool value, bool hovered,
            Dictionary<string, RectangleF> clickAreas, string clickId)
        {
            if (Core == null) return 0;

            float rowH = 32;
            var rowRect = new RectangleF(x, y, width, rowH);
            
            if (hovered)
                Core.SurfaceOverlay.DrawRectangle(rowRect);

            var layout = Core.FontBody.GetTextLayout(label);
            Core.FontBody.DrawText(layout, x + 8, y + (rowH - layout.Metrics.Height) / 2);

            float toggleW = 44, toggleH = 20;
            float tx = x + width - toggleW - 8;
            float ty = y + (rowH - toggleH) / 2;

            DrawToggle(tx, ty, toggleW, toggleH, value);

            if (clickAreas != null && !string.IsNullOrEmpty(clickId))
                clickAreas[clickId] = rowRect;

            return rowH;
        }

        /// <summary>
        /// Draw a checkbox
        /// </summary>
        public static void DrawCheckbox(float x, float y, float size, bool value, bool hovered)
        {
            if (Core == null) return;

            var brush = hovered ? Core.SurfaceOverlay : Core.SurfaceCard;
            brush.DrawRectangle(x, y, size, size);
            Core.BorderDefault.DrawRectangle(x, y, size, size);

            if (value)
            {
                // Draw checkmark
                var checkLayout = Core.FontSmall.GetTextLayout("✓");
                Core.FontSuccess.DrawText(checkLayout, x + (size - checkLayout.Metrics.Width) / 2,
                    y + (size - checkLayout.Metrics.Height) / 2);
            }
        }

        /// <summary>
        /// Draw a labeled checkbox row
        /// </summary>
        public static float DrawLabeledCheckbox(float x, float y, float width, string label, bool value, bool hovered,
            Dictionary<string, RectangleF> clickAreas, string clickId)
        {
            if (Core == null) return 0;

            float rowH = 28;
            float checkSize = 18;
            
            var rowRect = new RectangleF(x, y, width, rowH);
            if (hovered) Core.SurfaceOverlay.DrawRectangle(rowRect);

            DrawCheckbox(x + 8, y + (rowH - checkSize) / 2, checkSize, value, hovered);

            var layout = Core.FontBody.GetTextLayout(label);
            Core.FontBody.DrawText(layout, x + checkSize + 16, y + (rowH - layout.Metrics.Height) / 2);

            if (clickAreas != null && !string.IsNullOrEmpty(clickId))
                clickAreas[clickId] = rowRect;

            return rowH;
        }

        #endregion

        #region Input Fields

        /// <summary>
        /// Draw an input field
        /// </summary>
        public static void DrawInputField(RectangleF rect, string value, string placeholder, bool hovered, bool focused = false)
        {
            if (Core == null) return;

            var brush = focused ? Core.SurfaceOverlay : (hovered ? Core.SurfaceOverlay : Core.SurfaceCard);
            brush.DrawRectangle(rect);

            var border = focused ? Core.BorderFocus : Core.BorderDefault;
            border.DrawRectangle(rect);

            string display = string.IsNullOrEmpty(value) ? placeholder : value;
            var font = string.IsNullOrEmpty(value) ? Core.FontMuted : Core.FontMono;
            var layout = font.GetTextLayout(display);
            font.DrawText(layout, rect.X + 10, rect.Y + (rect.Height - layout.Metrics.Height) / 2);
        }

        /// <summary>
        /// Draw a selector (prev/next arrows with value)
        /// </summary>
        public static void DrawSelector(RectangleF rect, string value, bool hovered, bool canPrev = true, bool canNext = true)
        {
            if (Core == null) return;

            var brush = hovered ? Core.SurfaceOverlay : Core.SurfaceCard;
            brush.DrawRectangle(rect);
            Core.BorderDefault.DrawRectangle(rect);

            // Arrows
            var leftLayout = Core.FontSmall.GetTextLayout("◀");
            var rightLayout = Core.FontSmall.GetTextLayout("▶");
            
            var leftFont = canPrev ? Core.FontSmall : Core.FontMuted;
            var rightFont = canNext ? Core.FontSmall : Core.FontMuted;
            
            leftFont.DrawText(leftLayout, rect.X + 6, rect.Y + (rect.Height - leftLayout.Metrics.Height) / 2);
            rightFont.DrawText(rightLayout, rect.X + rect.Width - rightLayout.Metrics.Width - 6,
                rect.Y + (rect.Height - rightLayout.Metrics.Height) / 2);

            // Value
            var valueLayout = Core.FontMono.GetTextLayout(value);
            Core.FontMono.DrawText(valueLayout,
                rect.X + (rect.Width - valueLayout.Metrics.Width) / 2,
                rect.Y + (rect.Height - valueLayout.Metrics.Height) / 2);
        }

        /// <summary>
        /// Draw a labeled selector row
        /// </summary>
        public static float DrawLabeledSelector(float x, float y, float width, string label, string value,
            bool hovered, Dictionary<string, RectangleF> clickAreas, string baseClickId,
            bool canPrev = true, bool canNext = true)
        {
            if (Core == null) return 0;

            float rowH = 32;
            float selectorW = 100;

            var layout = Core.FontBody.GetTextLayout(label);
            Core.FontBody.DrawText(layout, x + 8, y + (rowH - layout.Metrics.Height) / 2);

            var selectorRect = new RectangleF(x + width - selectorW - 8, y + 4, selectorW, rowH - 8);
            DrawSelector(selectorRect, value, hovered, canPrev, canNext);

            // Click areas for prev/next
            if (clickAreas != null && !string.IsNullOrEmpty(baseClickId))
            {
                clickAreas[$"{baseClickId}_prev"] = new RectangleF(selectorRect.X, selectorRect.Y, 30, selectorRect.Height);
                clickAreas[$"{baseClickId}_next"] = new RectangleF(selectorRect.X + selectorRect.Width - 30, selectorRect.Y, 30, selectorRect.Height);
            }

            return rowH;
        }

        /// <summary>
        /// Draw a slider
        /// </summary>
        public static void DrawSlider(RectangleF rect, float value, float min, float max, bool hovered)
        {
            if (Core == null) return;

            // Track
            float trackH = 6;
            float trackY = rect.Y + (rect.Height - trackH) / 2;
            Core.ProgressTrack.DrawRectangle(rect.X, trackY, rect.Width, trackH);

            // Fill
            float pct = Math.Max(0, Math.Min(1, (value - min) / (max - min)));
            Core.ProgressFill.DrawRectangle(rect.X, trackY, rect.Width * pct, trackH);

            // Handle
            float handleSize = 14;
            float handleX = rect.X + (rect.Width - handleSize) * pct;
            float handleY = rect.Y + (rect.Height - handleSize) / 2;
            
            var handleBrush = hovered ? Core.BtnHover : Core.BtnDefault;
            handleBrush.DrawRectangle(handleX, handleY, handleSize, handleSize);
        }

        /// <summary>
        /// Draw a labeled slider row
        /// </summary>
        public static float DrawLabeledSlider(float x, float y, float width, string label, float value, float min, float max,
            string format, bool hovered, Dictionary<string, RectangleF> clickAreas, string clickId)
        {
            if (Core == null) return 0;

            float rowH = 36;
            float sliderW = 100;
            float valueW = 50;

            var labelLayout = Core.FontBody.GetTextLayout(label);
            Core.FontBody.DrawText(labelLayout, x + 8, y + 4);

            // Value display
            var valueStr = string.Format(format, value);
            var valueLayout = Core.FontMono.GetTextLayout(valueStr);
            Core.FontMono.DrawText(valueLayout, x + width - valueW - 8, y + 4);

            // Slider
            var sliderRect = new RectangleF(x + width - sliderW - valueW - 16, y + 20, sliderW, 16);
            DrawSlider(sliderRect, value, min, max, hovered);

            if (clickAreas != null && !string.IsNullOrEmpty(clickId))
                clickAreas[clickId] = sliderRect;

            return rowH;
        }

        #endregion

        #region Panels & Cards

        /// <summary>
        /// Draw a panel background
        /// </summary>
        public static void DrawPanel(float x, float y, float width, float height, byte alpha = 255)
        {
            if (Core == null) return;

            var bgBrush = Hud.Render.CreateBrush(alpha, 16, 16, 22, 0);
            bgBrush.DrawRectangle(x, y, width, height);

            var borderBrush = Hud.Render.CreateBrush((byte)(alpha * 0.7f), 55, 58, 75, 1f);
            borderBrush.DrawRectangle(x, y, width, height);
        }

        /// <summary>
        /// Draw an elevated panel
        /// </summary>
        public static void DrawElevatedPanel(float x, float y, float width, float height)
        {
            if (Core == null) return;

            Core.SurfaceElevated.DrawRectangle(x, y, width, height);
            Core.BorderDefault.DrawRectangle(x, y, width, height);
        }

        /// <summary>
        /// Draw a card
        /// </summary>
        public static void DrawCard(RectangleF rect, bool hovered = false, bool selected = false)
        {
            if (Core == null) return;

            var brush = selected ? Core.SurfaceOverlay : (hovered ? Core.SurfaceOverlay : Core.SurfaceCard);
            brush.DrawRectangle(rect);

            if (selected)
                Core.BorderAccent.DrawRectangle(rect);
        }

        /// <summary>
        /// Draw a section with header
        /// </summary>
        public static float DrawSection(float x, float y, float width, string title, string icon = null)
        {
            if (Core == null) return 0;

            float headerH = 24;

            // Header background
            Core.SurfaceElevated.DrawRectangle(x, y, width, headerH);

            // Title
            string displayTitle = string.IsNullOrEmpty(icon) ? title : $"{icon} {title}";
            var layout = Core.FontSubheader.GetTextLayout(displayTitle);
            Core.FontSubheader.DrawText(layout, x + 10, y + (headerH - layout.Metrics.Height) / 2);

            return headerH;
        }

        #endregion

        #region Progress & Status

        /// <summary>
        /// Draw a progress bar
        /// </summary>
        public static void DrawProgressBar(float x, float y, float width, float height, float progress, StatusType type = StatusType.Success)
        {
            if (Core == null) return;

            // Track
            Core.ProgressTrack.DrawRectangle(x, y, width, height);

            // Fill
            float fillW = width * Math.Max(0, Math.Min(1, progress));
            var fill = GetProgressBrush(type);
            fill.DrawRectangle(x, y, fillW, height);
        }

        private static IBrush GetProgressBrush(StatusType type)
        {
            switch (type)
            {
                case StatusType.Warning: return Core.ProgressFillWarning;
                case StatusType.Error: return Core.ProgressFillError;
                default: return Core.ProgressFill;
            }
        }

        /// <summary>
        /// Draw a status badge
        /// </summary>
        public static void DrawBadge(float x, float y, string icon, string value, StatusType type = StatusType.Info)
        {
            if (Core == null) return;

            var iconLayout = Core.FontIcon.GetTextLayout(icon);
            var valueLayout = Core.FontBody.GetTextLayout(value);

            float w = iconLayout.Metrics.Width + valueLayout.Metrics.Width + 14;
            float h = Math.Max(iconLayout.Metrics.Height, valueLayout.Metrics.Height) + 6;

            var bg = Core.GetStatusBrush(type);
            bg.DrawRectangle(x, y, w, h);

            Core.FontIcon.DrawText(iconLayout, x + 4, y + 3);
            Core.FontBody.DrawText(valueLayout, x + iconLayout.Metrics.Width + 10, y + 3);
        }

        /// <summary>
        /// Draw a small tag/pill
        /// </summary>
        public static void DrawTag(float x, float y, string text, StatusType type = StatusType.Info)
        {
            if (Core == null) return;

            var layout = Core.FontMicro.GetTextLayout(text);
            float w = layout.Metrics.Width + 10;
            float h = layout.Metrics.Height + 4;

            var bg = Core.GetStatusBrush(type);
            bg.DrawRectangle(x, y, w, h);

            Core.FontMicro.DrawText(layout, x + 5, y + 2);
        }

        /// <summary>
        /// Draw a status indicator dot
        /// </summary>
        public static void DrawStatusDot(float x, float y, float radius, StatusType type)
        {
            if (Core == null) return;

            var brush = Core.GetStatusBrush(type);
            brush.DrawRectangle(x - radius, y - radius, radius * 2, radius * 2);
        }

        #endregion

        #region Scrollbar

        /// <summary>
        /// Draw a vertical scrollbar
        /// </summary>
        public static void DrawVerticalScrollbar(float x, float y, float height, int totalItems, int visibleItems, int scrollOffset)
        {
            if (Core == null || totalItems <= visibleItems) return;

            Core.ScrollTrack.DrawRectangle(x, y, 6, height);

            float thumbH = height * ((float)visibleItems / totalItems);
            thumbH = Math.Max(20, thumbH); // Minimum thumb size
            float thumbY = y + (height - thumbH) * ((float)scrollOffset / Math.Max(1, totalItems - visibleItems));

            Core.ScrollThumb.DrawRectangle(x, thumbY, 6, thumbH);
        }

        /// <summary>
        /// Draw a horizontal scrollbar
        /// </summary>
        public static void DrawHorizontalScrollbar(float x, float y, float width, int totalItems, int visibleItems, int scrollOffset)
        {
            if (Core == null || totalItems <= visibleItems) return;

            Core.ScrollTrack.DrawRectangle(x, y, width, 6);

            float thumbW = width * ((float)visibleItems / totalItems);
            thumbW = Math.Max(20, thumbW);
            float thumbX = x + (width - thumbW) * ((float)scrollOffset / Math.Max(1, totalItems - visibleItems));

            Core.ScrollThumb.DrawRectangle(thumbX, y, thumbW, 6);
        }

        #endregion

        #region Headers & Labels

        /// <summary>
        /// Draw a title
        /// </summary>
        public static float DrawTitle(float x, float y, string text)
        {
            if (Core == null) return 0;

            var layout = Core.FontTitle.GetTextLayout(text);
            Core.FontTitle.DrawText(layout, x, y);
            return layout.Metrics.Height;
        }

        /// <summary>
        /// Draw a section header
        /// </summary>
        public static float DrawHeader(float x, float y, string text)
        {
            if (Core == null) return 0;

            var layout = Core.FontHeader.GetTextLayout(text);
            Core.FontHeader.DrawText(layout, x, y);
            return layout.Metrics.Height;
        }

        /// <summary>
        /// Draw a subheader
        /// </summary>
        public static float DrawSubheader(float x, float y, string text)
        {
            if (Core == null) return 0;

            var layout = Core.FontSubheader.GetTextLayout(text);
            Core.FontSubheader.DrawText(layout, x, y);
            return layout.Metrics.Height;
        }

        /// <summary>
        /// Draw body text
        /// </summary>
        public static float DrawText(float x, float y, string text, IFont font = null)
        {
            if (Core == null) return 0;

            font = font ?? Core.FontBody;
            var layout = font.GetTextLayout(text);
            font.DrawText(layout, x, y);
            return layout.Metrics.Height;
        }

        /// <summary>
        /// Draw muted/hint text
        /// </summary>
        public static float DrawHint(float x, float y, string text)
        {
            if (Core == null) return 0;

            var layout = Core.FontMuted.GetTextLayout(text);
            Core.FontMuted.DrawText(layout, x, y);
            return layout.Metrics.Height;
        }

        /// <summary>
        /// Draw a horizontal separator
        /// </summary>
        public static void DrawSeparator(float x, float y, float width)
        {
            if (Core == null) return;
            Core.BorderDefault.DrawLine(x, y, x + width, y);
        }

        #endregion

        #region Tooltips

        private static string _tooltipText;
        private static RectangleF _tooltipAnchor;

        /// <summary>
        /// Set tooltip to be drawn (call from hover detection)
        /// </summary>
        public static void SetTooltip(string text, RectangleF anchor)
        {
            _tooltipText = text;
            _tooltipAnchor = anchor;
        }

        /// <summary>
        /// Clear the tooltip
        /// </summary>
        public static void ClearTooltip()
        {
            _tooltipText = null;
        }

        /// <summary>
        /// Draw the current tooltip (call at end of paint)
        /// </summary>
        public static void DrawTooltip()
        {
            if (Core == null || string.IsNullOrEmpty(_tooltipText)) return;

            var layout = Core.FontSmall.GetTextLayout(_tooltipText);
            float w = Math.Min(layout.Metrics.Width, 250) + 16;
            float h = layout.Metrics.Height + 10;

            float x = _tooltipAnchor.X + _tooltipAnchor.Width / 2 - w / 2;
            float y = _tooltipAnchor.Y - h - 5;

            // Keep on screen
            if (x + w > Hud.Window.Size.Width - 10) x = Hud.Window.Size.Width - w - 10;
            if (y < 10) y = _tooltipAnchor.Y + _tooltipAnchor.Height + 5;
            if (x < 10) x = 10;

            Core.SurfaceModal.DrawRectangle(x, y, w, h);
            Core.BorderDefault.DrawRectangle(x, y, w, h);

            Core.FontSmall.DrawText(layout, x + 8, y + 5);

            _tooltipText = null;
        }

        #endregion

        #region Item Highlights

        /// <summary>
        /// Draw a highlight around an item rect
        /// </summary>
        public static void DrawItemHighlight(RectangleF rect, HighlightType type)
        {
            if (Core == null) return;

            IBrush brush;
            switch (type)
            {
                case HighlightType.Positive: brush = Core.HighlightPositive; break;
                case HighlightType.Negative: brush = Core.HighlightNegative; break;
                case HighlightType.Special: brush = Core.HighlightSpecial; break;
                default: brush = Core.HighlightNeutral; break;
            }

            brush.DrawRectangle(rect);
        }

        /// <summary>
        /// Draw a highlight with pulsing effect
        /// </summary>
        public static void DrawPulsingHighlight(RectangleF rect, HighlightType type, float phase)
        {
            if (Core == null) return;

            // Calculate alpha based on phase (0-1)
            byte baseAlpha = 160;
            byte pulseRange = 60;
            byte alpha = (byte)(baseAlpha + pulseRange * Math.Sin(phase * Math.PI * 2));

            IBrush brush;
            switch (type)
            {
                case HighlightType.Positive:
                    brush = Hud.Render.CreateBrush(alpha, 80, 220, 120, 2.5f);
                    break;
                case HighlightType.Negative:
                    brush = Hud.Render.CreateBrush(alpha, 230, 90, 90, 2.5f);
                    break;
                case HighlightType.Special:
                    brush = Hud.Render.CreateBrush(alpha, 180, 120, 255, 2.5f);
                    break;
                default:
                    brush = Hud.Render.CreateBrush(alpha, 150, 150, 170, 2f);
                    break;
            }

            brush.DrawRectangle(rect);
        }

        #endregion

        #region Utility

        /// <summary>
        /// Check if mouse is over a rectangle
        /// </summary>
        public static bool IsMouseOver(RectangleF rect)
        {
            if (Core == null) return false;

            int mx = Hud.Window.CursorX;
            int my = Hud.Window.CursorY;
            return mx >= rect.X && mx <= rect.X + rect.Width &&
                   my >= rect.Y && my <= rect.Y + rect.Height;
        }

        /// <summary>
        /// Get text width
        /// </summary>
        public static float GetTextWidth(string text, IFont font = null)
        {
            if (Core == null) return 0;
            font = font ?? Core.FontBody;
            return font.GetTextLayout(text).Metrics.Width;
        }

        /// <summary>
        /// Get text height
        /// </summary>
        public static float GetTextHeight(string text, IFont font = null)
        {
            if (Core == null) return 0;
            font = font ?? Core.FontBody;
            return font.GetTextLayout(text).Metrics.Height;
        }

        /// <summary>
        /// Truncate text to fit width
        /// </summary>
        public static string TruncateText(string text, float maxWidth, IFont font = null)
        {
            if (Core == null || string.IsNullOrEmpty(text)) return text;
            font = font ?? Core.FontBody;

            if (font.GetTextLayout(text).Metrics.Width <= maxWidth)
                return text;

            for (int i = text.Length - 1; i > 0; i--)
            {
                string truncated = text.Substring(0, i) + "...";
                if (font.GetTextLayout(truncated).Metrics.Width <= maxWidth)
                    return truncated;
            }

            return "...";
        }

        #endregion
    }

    #region Enums

    public enum HighlightType
    {
        Neutral,
        Positive,
        Negative,
        Special
    }

    #endregion
}
