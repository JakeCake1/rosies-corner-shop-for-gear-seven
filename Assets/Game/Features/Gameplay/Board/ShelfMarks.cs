using System.Collections.Generic;
using DG.Tweening;
using Game.Services.UI.Elements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Features.Gameplay.Board
{
    /// <summary>
    /// The mechanics marks of a shelf (<c>Shelf.uxml</c>): the twine of a locked shelf, the key tag, the ice counter
    /// and the nailed boards.
    /// </summary>
    public sealed class ShelfMarks
    {
        private const string ContentName = "content";
        private const string CellsName = "cells";
        private const string BackMarksName = "back-marks";
        private const string TiesName = "ties";
        private const string KeyTagName = "key-tag";
        private const string IceName = "ice";
        private const string IceTextName = "ice-text";
        private const string BoardsName = "boards";
        private const string BoardClass = "shelf__board";
        private const float LockedOpacity = 0.7f;
        private const float BoardFlyX = 60f;
        private const float BoardFlyY = 40f;
        private const float BoardSpin = 40f;
        private const float BoardSeconds = 0.45f;

        private readonly List<VisualElement> _boards = new List<VisualElement>();
        private readonly VisualElement _content;
        private readonly VisualElement _cellLayer;
        private readonly VisualElement _backMarkLayer;
        private readonly VisualElement _ties;
        private readonly VisualElement _keyTag;
        private readonly VisualElement _ice;
        private readonly Label _iceText;
        private readonly VisualElement _boardLayer;
        private Tween _knockOff;
        private bool _isKnockingOff;

        /// <summary>
        /// Finds the marks in a shelf.
        /// </summary>
        /// <param name="root">The shelf root.</param>
        public ShelfMarks(VisualElement root)
        {
            _content = root.Q(ContentName);
            _cellLayer = root.Q(CellsName);
            _backMarkLayer = root.Q(BackMarksName);
            _ties = root.Q(TiesName);
            _keyTag = root.Q(KeyTagName);
            _ice = root.Q(IceName);
            _iceText = root.Q<Label>(IceTextName);
            _boardLayer = root.Q(BoardsName);
            _boardLayer.Query(className: BoardClass).ForEach(_boards.Add);
        }

        /// <summary>
        /// Applies the block's mechanics.
        /// </summary>
        /// <param name="isLocked">Tied with twine.</param>
        /// <param name="hasKey">Holds the key of another shelf.</param>
        /// <param name="frozen">Moves left until the ice melts.</param>
        /// <param name="isBoarded">Nailed shut.</param>
        public void Set(bool isLocked, bool hasKey, int frozen, bool isBoarded)
        {
            _ties.SetVisible(isLocked);
            _keyTag.SetVisible(hasKey);
            _ice.SetVisible(frozen > 0);
            if (frozen > 0)
            {
                _iceText.text = frozen.ToString();
            }

            // The boards stay up while they fly off: the render that follows the rule event already reports the block as
            // unboarded, and hiding the layer then would cut the animation in the same frame.
            _boardLayer.SetVisible(isBoarded || _isKnockingOff);
            _content.style.opacity = isLocked ? LockedOpacity : 1f;
            _cellLayer.SetVisible(!isBoarded);
            _backMarkLayer.SetVisible(!isBoarded);
        }

        /// <summary>
        /// Knocks the nailed boards off.
        /// </summary>
        /// <param name="tweenId">Id of the animation.</param>
        /// <returns>The animation.</returns>
        public Tween KnockBoardsOff(object tweenId)
        {
            _knockOff?.Kill();
            _isKnockingOff = true;
            var seq = DOTween.Sequence().SetId(tweenId).OnKill(() =>
            {
                _isKnockingOff = false;
                _boardLayer.SetVisible(false);
            });
            for (var i = 0; i < _boards.Count; i++)
            {
                var board = _boards[i];
                var dir = i % 2 == 0 ? -1f : 1f;
                seq.Join(board.DOTranslate(new Vector2(dir * BoardFlyX, BoardFlyY), BoardSeconds).SetEase(Ease.InQuad));
                seq.Join(board.DORotate(VisualElementTweens.GetRotation(board) + dir * BoardSpin, BoardSeconds));
                seq.Join(board.DOFade(0f, BoardSeconds));
            }

            _knockOff = seq;
            return seq;
        }

        /// <summary>
        /// Centre of the key tag in panel space.
        /// </summary>
        /// <returns>Panel point.</returns>
        public Vector2 KeyCenter() => _keyTag.worldBound.center;

        /// <summary>
        /// Clears every mark and puts the knocked-off boards back for the next block.
        /// </summary>
        public void Reset()
        {
            _knockOff?.Kill();
            _knockOff = null;
            _isKnockingOff = false;
            Set(false, false, 0, false);
            foreach (var board in _boards)
            {
                board.Kill();
                board.style.translate = StyleKeyword.Null;
                board.style.rotate = StyleKeyword.Null;
                board.style.opacity = StyleKeyword.Null;
            }
        }
    }
}
