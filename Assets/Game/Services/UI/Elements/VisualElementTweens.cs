using DG.Tweening;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Services.UI.Elements
{
    /// <summary>
    /// DOTween shortcuts for UI Toolkit elements (DOTween ships shortcuts only for uGUI and transforms).
    /// Every tween targets its element, so <see cref="Kill"/> stops all tweens of that element.
    /// </summary>
    public static class VisualElementTweens
    {
        /// <summary>
        /// Tweens <c>style.opacity</c>.
        /// </summary>
        /// <param name="element">Animated element.</param>
        /// <param name="to">Target opacity.</param>
        /// <param name="duration">Seconds.</param>
        /// <returns>The tween.</returns>
        public static Tweener DOFade(this VisualElement element, float to, float duration) =>
            DOTween.To(() => GetOpacity(element), v => element.style.opacity = v, to, duration).SetTarget(element);

        /// <summary>
        /// Tweens a uniform <c>style.scale</c>.
        /// </summary>
        /// <param name="element">Animated element.</param>
        /// <param name="to">Target scale.</param>
        /// <param name="duration">Seconds.</param>
        /// <returns>The tween.</returns>
        public static Tweener DOScale(this VisualElement element, float to, float duration) =>
            element.DOScale(new Vector2(to, to), duration);

        /// <summary>
        /// Tweens <c>style.scale</c> per axis.
        /// </summary>
        /// <param name="element">Animated element.</param>
        /// <param name="to">Target scale.</param>
        /// <param name="duration">Seconds.</param>
        /// <returns>The tween.</returns>
        public static Tweener DOScale(this VisualElement element, Vector2 to, float duration) =>
            DOTween.To(() => GetScale(element), v => SetScale(element, v), to, duration).SetTarget(element);

        /// <summary>
        /// Tweens <c>style.translate</c> in pixels.
        /// </summary>
        /// <param name="element">Animated element.</param>
        /// <param name="to">Target offset.</param>
        /// <param name="duration">Seconds.</param>
        /// <returns>The tween.</returns>
        public static Tweener DOTranslate(this VisualElement element, Vector2 to, float duration) =>
            DOTween.To(() => GetTranslate(element), v => SetTranslate(element, v), to, duration).SetTarget(element);

        /// <summary>
        /// Tweens <c>style.rotate</c> in degrees.
        /// </summary>
        /// <param name="element">Animated element.</param>
        /// <param name="to">Target angle.</param>
        /// <param name="duration">Seconds.</param>
        /// <returns>The tween.</returns>
        public static Tweener DORotate(this VisualElement element, float to, float duration) =>
            DOTween.To(() => GetRotation(element), v => SetRotation(element, v), to, duration).SetTarget(element);

        /// <summary>
        /// Tweens the absolute <c>style.left</c>/<c>style.top</c> position.
        /// </summary>
        /// <param name="element">Absolutely positioned element.</param>
        /// <param name="to">Target top-left corner in the parent's space.</param>
        /// <param name="duration">Seconds.</param>
        /// <returns>The tween.</returns>
        public static Tweener DOPosition(this VisualElement element, Vector2 to, float duration) =>
            DOTween.To(() => GetPosition(element), v => SetPosition(element, v), to, duration).SetTarget(element);

        /// <summary>
        /// Tweens <c>style.bottom</c> of an absolutely positioned element.
        /// </summary>
        /// <param name="element">Animated element.</param>
        /// <param name="to">Target bottom offset.</param>
        /// <param name="duration">Seconds.</param>
        /// <returns>The tween.</returns>
        public static Tweener DOBottom(this VisualElement element, float to, float duration) =>
            DOTween.To(() => element.style.bottom.value.value, v => element.style.bottom = v, to, duration).SetTarget(element);

        /// <summary>
        /// Tweens <c>style.width</c> as a fraction of the parent (0..1).
        /// </summary>
        /// <param name="element">Animated element.</param>
        /// <param name="to">Target fraction.</param>
        /// <param name="duration">Seconds.</param>
        /// <returns>The tween.</returns>
        public static Tweener DOWidthPercent(this VisualElement element, float to, float duration) =>
            DOTween.To(() => element.style.width.value.value / 100f,
                v => element.style.width = Length.Percent(v * 100f), to, duration).SetTarget(element);

        /// <summary>
        /// Shakes <c>style.translate</c> and returns it to zero.
        /// </summary>
        /// <param name="element">Animated element.</param>
        /// <param name="duration">Seconds.</param>
        /// <param name="strength">Maximum offset per axis.</param>
        /// <param name="vibrato">Number of shakes.</param>
        /// <returns>The tween.</returns>
        public static Tweener DOShake(this VisualElement element, float duration, Vector2 strength, int vibrato) =>
            DOTween.Shake(() => GetTranslate(element), v => SetTranslate(element, v), duration, strength, vibrato, 0f, false)
                .OnComplete(() => SetTranslate(element, Vector2.zero))
                .SetTarget(element);

        /// <summary>
        /// Punches the uniform scale and returns it to 1.
        /// </summary>
        /// <param name="element">Animated element.</param>
        /// <param name="punch">Added scale at the peak.</param>
        /// <param name="duration">Seconds.</param>
        /// <param name="vibrato">Number of oscillations.</param>
        /// <returns>The tween.</returns>
        public static Tweener DOPunchScale(this VisualElement element, float punch, float duration, int vibrato) =>
            DOTween.Punch(() => (Vector3)GetScale(element), v => SetScale(element, v), Vector3.one * punch, duration, vibrato)
                .OnComplete(() => SetScale(element, Vector2.one))
                .SetTarget(element);

        /// <summary>
        /// Kills every tween that targets <paramref name="element"/>.
        /// </summary>
        /// <param name="element">Animated element.</param>
        /// <param name="complete">Jump to the end values first.</param>
        public static void Kill(this VisualElement element, bool complete = false) => DOTween.Kill(element, complete);

        /// <summary>Current inline opacity, or the resolved one when none is set.</summary>
        /// <param name="element">Element.</param>
        /// <returns>Opacity 0..1.</returns>
        public static float GetOpacity(VisualElement element) =>
            element.style.opacity.keyword == StyleKeyword.Undefined ? element.style.opacity.value : element.resolvedStyle.opacity;

        /// <summary>Current scale per axis.</summary>
        /// <param name="element">Element.</param>
        /// <returns>Scale.</returns>
        public static Vector2 GetScale(VisualElement element) =>
            element.style.scale.keyword == StyleKeyword.Undefined
                ? (Vector2)element.style.scale.value.value
                : (Vector2)element.resolvedStyle.scale.value;

        /// <summary>Sets the scale per axis.</summary>
        /// <param name="element">Element.</param>
        /// <param name="value">Scale.</param>
        public static void SetScale(VisualElement element, Vector2 value) =>
            element.style.scale = new Scale(new Vector3(value.x, value.y, 1f));

        /// <summary>Current translate offset in pixels.</summary>
        /// <param name="element">Element.</param>
        /// <returns>Offset.</returns>
        public static Vector2 GetTranslate(VisualElement element)
        {
            if (element.style.translate.keyword != StyleKeyword.Undefined) return element.resolvedStyle.translate;

            var t = element.style.translate.value;
            return new Vector2(t.x.value, t.y.value);
        }

        /// <summary>Sets the translate offset in pixels.</summary>
        /// <param name="element">Element.</param>
        /// <param name="value">Offset.</param>
        public static void SetTranslate(VisualElement element, Vector2 value) =>
            element.style.translate = new Translate(value.x, value.y);

        /// <summary>Current rotation in degrees.</summary>
        /// <param name="element">Element.</param>
        /// <returns>Angle.</returns>
        public static float GetRotation(VisualElement element) =>
            element.style.rotate.keyword == StyleKeyword.Undefined
                ? element.style.rotate.value.angle.ToDegrees()
                : element.resolvedStyle.rotate.angle.ToDegrees();

        /// <summary>Sets the rotation in degrees.</summary>
        /// <param name="element">Element.</param>
        /// <param name="degrees">Angle.</param>
        public static void SetRotation(VisualElement element, float degrees) =>
            element.style.rotate = new Rotate(new Angle(degrees, AngleUnit.Degree));

        /// <summary>Current absolute top-left position.</summary>
        /// <param name="element">Element.</param>
        /// <returns>Position.</returns>
        public static Vector2 GetPosition(VisualElement element) =>
            new Vector2(element.style.left.value.value, element.style.top.value.value);

        /// <summary>Sets the absolute top-left position.</summary>
        /// <param name="element">Element.</param>
        /// <param name="value">Position.</param>
        public static void SetPosition(VisualElement element, Vector2 value)
        {
            element.style.left = value.x;
            element.style.top = value.y;
        }
    }
}
