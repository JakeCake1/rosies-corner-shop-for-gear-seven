using UnityEngine;

namespace Game.Services.UI
{
    /// <summary>
    /// Non-window overlay feedback shared by all features: toasts, coach bubbles, the tutorial hand, particle
    /// bursts, praise words and the background tint.
    /// </summary>
    public interface IOverlayService
    {
        /// <summary>
        /// Shows a short message pill; a newer toast replaces the current one.
        /// </summary>
        /// <param name="message">Text (rich text allowed).</param>
        /// <param name="isWarning">Berry color for problems.</param>
        void Toast(string message, bool isWarning = false);

        /// <summary>
        /// Shows a coach bubble next to an element.
        /// </summary>
        /// <param name="message">Rich text.</param>
        /// <param name="anchor">Element the bubble refers to.</param>
        /// <param name="isAbove">Place above instead of below.</param>
        /// <param name="seconds">Auto-hide delay; 0 keeps it until <see cref="ClearCoach"/>.</param>
        void Coach(string message, RectTransform anchor, bool isAbove, float seconds);

        /// <summary>
        /// Shows the tutorial hand pointing at an element.
        /// </summary>
        /// <param name="target">Element to tap.</param>
        void PointAt(RectTransform target);

        /// <summary>
        /// Hides the coach bubble and the hand.
        /// </summary>
        void ClearCoach();

        /// <summary>
        /// Emits a particle burst at a world position.
        /// </summary>
        /// <param name="worldPosition">Center in world space.</param>
        /// <param name="kind">Particle look.</param>
        /// <param name="count">Particle count.</param>
        /// <param name="colors">Optional palette override.</param>
        void Burst(Vector3 worldPosition, EffectKind kind, int count, Color[] colors = null);

        /// <summary>
        /// Floats a praise word up from a world position.
        /// </summary>
        /// <param name="worldPosition">Start position.</param>
        /// <param name="text">The word.</param>
        /// <param name="isCombo">Bigger gold style for combo words.</param>
        void Praise(Vector3 worldPosition, string text, bool isCombo);

        /// <summary>
        /// Background state for the current screen.
        /// </summary>
        /// <param name="isTricky">Red wash for tricky levels.</param>
        /// <param name="hasBunting">Bunting along the top (home, album).</param>
        void SetBackground(bool isTricky, bool hasBunting);
    }
}
