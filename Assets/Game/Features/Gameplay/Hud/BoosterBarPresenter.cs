using System;
using System.Collections.Generic;
using System.Linq;
using Game.Features.Gameplay.Session;
using UnityEngine.UIElements;

namespace Game.Features.Gameplay.Hud
{
    /// <summary>
    /// The booster bar: a button per booster unlocked on the level in play, with counts kept in sync with the session
    /// and the inventory. A tap runs <see cref="BoosterActions.Use"/>.
    /// </summary>
    public sealed class BoosterBarPresenter : IDisposable
    {
        private static readonly string[] Order = { BoosterIds.Undo, BoosterIds.Pick, BoosterIds.Tidy, BoosterIds.Shuffle };

        private readonly GameplayContext _context;
        private readonly BoosterActions _actions;
        private readonly List<BoosterViewModel> _models = new List<BoosterViewModel>();
        private readonly List<BoosterButtonView> _views = new List<BoosterButtonView>();

        /// <summary>
        /// Creates the booster bar.
        /// </summary>
        /// <param name="context">Shared gameplay state.</param>
        /// <param name="actions">Runs a booster when its button is tapped.</param>
        public BoosterBarPresenter(GameplayContext context, BoosterActions actions)
        {
            _context = context;
            _actions = actions;
        }

        /// <summary>
        /// Prepares the bar for the level that just started: a button per unlocked booster, reused when the set is unchanged.
        /// </summary>
        public void Build()
        {
            var ids = Order.Where(_context.Session.IsUnlocked).ToList();
            if (ids.SequenceEqual(_models.Select(m => m.Id)))
            {
                // Same set as the previous level: keep the buttons (pooling), only drop the old gift pulse.
                foreach (var model in _models)
                {
                    model.SetGift(false);
                }
            }
            else
            {
                DisposeModels();
                ClearViews();
                foreach (var id in ids)
                {
                    AddButton(id);
                }
            }

            Refresh();
        }

        /// <summary>
        /// Updates counts, availability and pulsing of every button. Called on every board change, undo count change
        /// and inventory change.
        /// </summary>
        public void Refresh()
        {
            if (_context.Session.State == null)
            {
                return;
            }

            foreach (var model in _models)
            {
                RefreshModel(model);
            }
        }

        /// <summary>
        /// Makes the booster's icon pulse as Rosie's gift.
        /// </summary>
        /// <param name="boosterId">The gifted booster.</param>
        /// <param name="button">The gifted button, the anchor of the gift's coach tip; null when there is none.</param>
        /// <returns>False when the bar has no button for the booster.</returns>
        public bool TryMarkGift(string boosterId, out VisualElement button)
        {
            var index = _models.FindIndex(m => m.Id == boosterId);
            button = index >= 0 ? _views[index].Root : null;
            if (index < 0)
            {
                return false;
            }

            _models[index].SetGift(true);
            return true;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            ClearViews();
            DisposeModels();
        }

        private void AddButton(string boosterId)
        {
            var title = _context.Services.Texts.Get(BoosterTexts.NameOf(boosterId));
            var model = new BoosterViewModel(boosterId, title, _actions.Use);
            _models.Add(model);
            var view = _context.Screen.AddBooster();
            view.SetIcon(_context.Art.IconOf(boosterId));
            view.Bind(model);
            _views.Add(view);
        }

        private void DisposeModels()
        {
            foreach (var model in _models)
            {
                model.Dispose();
            }

            _models.Clear();
        }

        private void ClearViews()
        {
            foreach (var view in _views)
            {
                view.Unbind();
                _context.Screen.RemoveBooster(view);
            }

            _views.Clear();
        }

        private void RefreshModel(BoosterViewModel model)
        {
            if (model.Id != BoosterIds.Undo)
            {
                model.Set(_actions.OwnedCount(model.Id), false, false);
                return;
            }

            var session = _context.Session;
            var isAlmostFull = session.State.Counter.Count >= session.State.Capacity - 1;
            var canUndo = session.CanUndo.Value;
            model.Set(session.UndoLeft.Value, !canUndo, isAlmostFull && canUndo && session.UndoLeft.Value > 0);
        }
    }
}
