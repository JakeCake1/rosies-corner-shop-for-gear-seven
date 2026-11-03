using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Services.UI
{
    /// <summary>
    /// Toasts, coach tips, effects and the shop background.
    /// </summary>
    public interface IOverlayService
    {
        /// <summary>
        /// Shows a short message under the HUD.
        /// </summary>
        /// <param name="message">Text.</param>
        /// <param name="isWarning">Red warning style.</param>
        void Toast(string message, bool isWarning = false);

        /// <summary>
        /// Pops a white tip up above an element ("Album opens after level 9" over the locked album button).
        /// </summary>
        /// <param name="message">Text.</param>
        /// <param name="anchor">Element the tip points at.</param>
        void ToastAbove(string message, VisualElement anchor);

        /// <summary>
        /// Shows a coach bubble next to an element.
        /// </summary>
        /// <param name="message">Rich text.</param>
        /// <param name="anchor">Element to point at.</param>
        /// <param name="isAbove">Place the bubble above the anchor.</param>
        /// <param name="seconds">Seconds before it fades; 0 keeps it until <see cref="ClearCoach"/>.</param>
        void Coach(string message, VisualElement anchor, bool isAbove, float seconds);

        /// <summary>
        /// Shows the pulsing tutorial hand over an element.
        /// </summary>
        /// <param name="target">Element to tap.</param>
        void PointAt(VisualElement target);

        /// <summary>
        /// Removes the coach bubble and the hand.
        /// </summary>
        void ClearCoach();

        /// <summary>
        /// Converts a fraction of the screen to panel space, for effects that don't come from an element.
        /// Screen pixels are not panel space: the panel scales with the screen size.
        /// </summary>
        /// <param name="fraction">(0,0) is the top left corner, (1,1) the bottom right.</param>
        /// <returns>The point in panel space.</returns>
        Vector2 PanelPoint(Vector2 fraction);

        /// <summary>
        /// Bursts particles.
        /// </summary>
        /// <param name="panelPosition">Origin in panel space (<c>worldBound.center</c> of an element).</param>
        /// <param name="kind">Particle look.</param>
        /// <param name="count">Number of particles.</param>
        /// <param name="colors">Palette override.</param>
        void Burst(Vector2 panelPosition, EffectKind kind, int count, Color[] colors = null);

        /// <summary>
        /// Floats a praise word.
        /// </summary>
        /// <param name="panelPosition">Origin in panel space.</param>
        /// <param name="text">Word.</param>
        /// <param name="isCombo">Combo style.</param>
        void Praise(Vector2 panelPosition, string text, bool isCombo);

        /// <summary>
        /// Sets the background mood.
        /// </summary>
        /// <param name="isTricky">Red tint of tricky levels.</param>
        /// <param name="hasBunting">Show the bunting.</param>
        void SetBackground(bool isTricky, bool hasBunting);
    }
}
