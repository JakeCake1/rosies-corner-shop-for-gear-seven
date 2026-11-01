using System;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Services.UI.Elements
{
    /// <summary>
    /// Small UI Toolkit helpers shared by all views: UniRx event streams and common style changes.
    /// </summary>
    public static class ElementExtensions
    {
        private static readonly TimeSpan ClickThrottle = TimeSpan.FromMilliseconds(300);

        /// <summary>
        /// Emits on every click of <paramref name="button"/>.
        /// </summary>
        /// <param name="button">Button.</param>
        /// <returns>Click stream; dispose the subscription to unregister.</returns>
        public static IObservable<Unit> OnClickAsObservable(this Button button) =>
            Observable.FromEvent(h => button.clicked += h, h => button.clicked -= h);

        /// <summary>
        /// Emits on clicks, at most once per 300 ms (guards against double taps opening things twice).
        /// </summary>
        /// <param name="button">Button.</param>
        /// <returns>Throttled click stream.</returns>
        public static IObservable<Unit> OnTapAsObservable(this Button button) =>
            button.OnClickAsObservable().ThrottleFirst(ClickThrottle);

        /// <summary>
        /// Shows or hides an element (display: flex / none).
        /// </summary>
        /// <param name="element">Element.</param>
        /// <param name="isVisible">Show it.</param>
        public static void SetVisible(this VisualElement element, bool isVisible) =>
            element.style.display = isVisible ? DisplayStyle.Flex : DisplayStyle.None;

        /// <summary>
        /// Sets (or clears) the background sprite.
        /// </summary>
        /// <param name="element">Element.</param>
        /// <param name="sprite">Sprite, or null to clear.</param>
        public static void SetSprite(this VisualElement element, Sprite sprite) =>
            element.style.backgroundImage = sprite != null ? new StyleBackground(sprite) : new StyleBackground(StyleKeyword.None);

        /// <summary>
        /// Sets the background tint.
        /// </summary>
        /// <param name="element">Element.</param>
        /// <param name="color">Tint.</param>
        public static void SetTint(this VisualElement element, Color color) =>
            element.style.unityBackgroundImageTintColor = color;

        /// <summary>
        /// Places an absolutely positioned element so that its center is at <paramref name="center"/>.
        /// </summary>
        /// <param name="element">Element.</param>
        /// <param name="center">Center in the parent's space.</param>
        /// <param name="size">Width and height.</param>
        public static void PlaceCentered(this VisualElement element, Vector2 center, Vector2 size)
        {
            element.style.position = Position.Absolute;
            element.style.width = size.x;
            element.style.height = size.y;
            element.style.left = center.x - size.x / 2f;
            element.style.top = center.y - size.y / 2f;
        }

        /// <summary>
        /// Stretches an absolutely positioned element over its parent.
        /// </summary>
        /// <param name="element">Element.</param>
        public static void Stretch(this VisualElement element)
        {
            element.style.position = Position.Absolute;
            element.style.left = 0f;
            element.style.top = 0f;
            element.style.right = 0f;
            element.style.bottom = 0f;
        }

        /// <summary>
        /// Converts a panel-space point into <paramref name="space"/>'s local coordinates.
        /// </summary>
        /// <param name="space">Target element.</param>
        /// <param name="panelPoint">Point in panel space (e.g. <c>worldBound.center</c>).</param>
        /// <returns>Local point.</returns>
        public static Vector2 FromPanel(this VisualElement space, Vector2 panelPoint) => space.WorldToLocal(panelPoint);
    }
}
