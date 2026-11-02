using System;
using DG.Tweening;
using Game.Services.Pooling;
using Game.Services.UI.Elements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Features.Gameplay.Board
{
    /// <summary>
    /// One item on the board (shelf, counter, basket or in flight). Tapping it reports its jar id.
    /// </summary>
    public sealed class ItemView : PooledElementView
    {
        private const string ItemClass = "item";
        private const string ImageClass = "item__image";
        // Prototype .jar.hint: @keyframes hint { 50% { translateY(-6px) } }, 1 s ease-in-out.
        private const float HintLift = 6f;
        private const float HintHalfSeconds = 0.5f;

        private VisualElement _image;
        private Action<ItemView> _tapped;
        private Tween _hintTween;

        /// <summary>Jar id shown by this view (0 when pooled).</summary>
        public int JarId { get; private set; }

        /// <summary>Whether the paper bag still hides the item.</summary>
        public bool IsWrapped { get; private set; }

        /// <summary>Cell the item sits in or flies to.</summary>
        public VisualElement Destination { get; set; }

        /// <summary>
        /// Creates a pooled item view.
        /// </summary>
        /// <returns>New view.</returns>
        public static ItemView Create()
        {
            var view = new ItemView();
            view.Attach(new VisualElement());
            return view;
        }

        /// <summary>
        /// Shows a jar.
        /// </summary>
        /// <param name="jarId">Jar id.</param>
        /// <param name="sprite">Item, hammer or paper-bag picture.</param>
        /// <param name="isWrapped">Hidden under a paper bag.</param>
        /// <param name="tapped">Tap callback.</param>
        public void Setup(int jarId, Sprite sprite, bool isWrapped, Action<ItemView> tapped)
        {
            JarId = jarId;
            IsWrapped = isWrapped;
            _image.SetSprite(sprite);
            _tapped = tapped;
            SetHint(false);
        }

        /// <summary>
        /// Takes the paper bag off.
        /// </summary>
        /// <param name="sprite">Item picture.</param>
        public void Unwrap(Sprite sprite)
        {
            IsWrapped = false;
            _image.SetSprite(sprite);
            _image.Kill(true);
            VisualElementTweens.SetScale(_image, Vector2.one * 0.8f);
            _image.DOScale(1f, 0.25f).SetEase(Ease.OutBack);
        }

        /// <summary>
        /// Starts or stops the hint: the item bobs up and down.
        /// </summary>
        /// <param name="isOn">Bob.</param>
        public void SetHint(bool isOn)
        {
            _hintTween?.Kill();
            _image.style.translate = new Translate(0f, 0f);
            if (!isOn)
            {
                return;
            }

            _hintTween = _image.DOTranslate(new Vector2(0f, -HintLift), HintHalfSeconds).SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        }

        /// <summary>
        /// Shakes the item ("no").
        /// </summary>
        public void Nope()
        {
            _image.Kill(true);
            _image.DOShake(0.35f, new Vector2(6f, 0f), 14);
        }

        /// <summary>
        /// Puts the item into a cell and stretches it over the cell.
        /// </summary>
        /// <param name="cell">Shelf, counter or basket cell.</param>
        public void Snap(VisualElement cell)
        {
            Destination = cell;
            Root.Kill();
            cell.Add(Root);
            Fill();
        }

        /// <summary>
        /// Stretches the item over its parent and clears flight transforms.
        /// </summary>
        public void Fill()
        {
            Root.Stretch();
            Root.style.width = StyleKeyword.Null;
            Root.style.height = StyleKeyword.Null;
            ResetTransform(Root);
        }

        /// <inheritdoc/>
        public override void OnDespawned()
        {
            base.OnDespawned();
            _image.Kill();
            ResetTransform(_image);
            _hintTween?.Kill();
            _tapped = null;
            Destination = null;
            JarId = 0;
            IsWrapped = false;
        }

        /// <inheritdoc/>
        protected override void OnAttached()
        {
            Root.AddToClassList(ItemClass);
            _image = CreateElement(ImageClass);
            _image.pickingMode = PickingMode.Ignore;
            Root.Add(_image);
            Root.RegisterCallback<PointerDownEvent>(_ => _tapped?.Invoke(this));
        }
    }
}
