using DG.Tweening;
using Game.Services.UI.Elements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Services.Pooling
{
    /// <summary>
    /// Base of pooled UI Toolkit views: on despawn it stops the view's tweens and resets the transform styles
    /// (translate, scale, rotation, opacity) that animations leave behind.
    /// </summary>
    public abstract class PooledElementView : ElementView, IPoolable
    {
        /// <inheritdoc/>
        public virtual void OnSpawned()
        {
        }

        /// <inheritdoc/>
        public virtual void OnDespawned()
        {
            DOTween.Kill(this);
            Root.Kill();
            ResetTransform(Root);
        }

        /// <summary>
        /// Clears inline transform and opacity styles of an element.
        /// </summary>
        /// <param name="element">Element.</param>
        protected static void ResetTransform(VisualElement element)
        {
            element.style.translate = StyleKeyword.Null;
            element.style.scale = StyleKeyword.Null;
            element.style.rotate = StyleKeyword.Null;
            element.style.opacity = StyleKeyword.Null;
        }

        /// <summary>
        /// Places the view absolutely in its parent.
        /// </summary>
        /// <param name="topLeft">Top-left corner.</param>
        /// <param name="size">Width and height.</param>
        public void Place(Vector2 topLeft, Vector2 size)
        {
            Root.style.position = Position.Absolute;
            Root.style.left = topLeft.x;
            Root.style.top = topLeft.y;
            Root.style.width = size.x;
            Root.style.height = size.y;
        }
    }
}
