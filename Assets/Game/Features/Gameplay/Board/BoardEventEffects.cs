using Game.Features.Gameplay.Model;
using Game.Features.Gameplay.Session;
using Game.Services.Audio;
using Game.Services.UI;
using UnityEngine;

namespace Game.Features.Gameplay.Board
{
    /// <summary>
    /// Plays the rule events of a board change: an item unwraps, a key flies to the shelf it unlocks, the hammer knocks
    /// the boards off a shelf, a frozen shelf thaws.
    /// </summary>
    /// <remarks>
    /// Called by <see cref="BoardView"/> before its parts render the change, so effects start from the old positions.
    /// </remarks>
    public sealed class BoardEventEffects
    {
        private const int UnboardDust = 18;
        private const int ThawSparks = 22;

        private static readonly Color[] IceColors = { Color.white, new Color(0.75f, 0.89f, 1f), new Color(0.5f, 0.75f, 0.94f) };

        private readonly CabinetView _cabinet;
        private readonly ItemLayerView _items;
        private readonly BoardSprites _sprites;
        private readonly IOverlayService _overlay;
        private readonly IAudioService _audio;

        /// <summary>
        /// Creates the effects.
        /// </summary>
        /// <param name="cabinet">Shelves that are unlocked, unboarded or thawed.</param>
        /// <param name="items">Items that unwrap; the key flies on its layer.</param>
        /// <param name="sprites">Picture of an unwrapped item.</param>
        /// <param name="overlay">Dust and sparks.</param>
        /// <param name="audio">Hammer sounds.</param>
        public BoardEventEffects(CabinetView cabinet, ItemLayerView items, BoardSprites sprites,
            IOverlayService overlay, IAudioService audio)
        {
            _cabinet = cabinet;
            _items = items;
            _sprites = sprites;
            _overlay = overlay;
            _audio = audio;
        }

        /// <summary>
        /// Plays every rule event of <paramref name="change"/>.
        /// </summary>
        /// <param name="state">The level state after the change.</param>
        /// <param name="change">The change with its rule events.</param>
        public void Play(LevelState state, BoardChange change)
        {
            foreach (var e in change.Events)
            {
                switch (e.Type)
                {
                    case LevelEventType.JarUnwrapped:
                        Unwrap(state, e.Id);
                        break;
                    case LevelEventType.ShelfUnlocked:
                        FlyKey(e.Other, e.Id);
                        break;
                    case LevelEventType.ShelfUnboarded:
                        KnockBoardsOff(e.Id);
                        break;
                    case LevelEventType.ShelfThawed:
                        Thaw(e.Id);
                        break;
                }
            }
        }

        private static Jar FindJar(LevelState state, int jarId)
        {
            foreach (var column in state.Columns)
            {
                foreach (var block in column.Blocks)
                {
                    foreach (var jar in block.Slots)
                    {
                        if (jar != null && jar.Id == jarId)
                        {
                            return jar;
                        }
                    }
                }
            }

            return null;
        }

        private void Unwrap(LevelState state, int jarId)
        {
            var jar = FindJar(state, jarId);
            if (jar != null && _items.TryGet(jarId, out var item))
            {
                item.Unwrap(_sprites.OfType(jar.Type));
            }
        }

        private void FlyKey(int fromBlock, int toBlock)
        {
            if (!_cabinet.TryGet(toBlock, out var target))
            {
                return;
            }

            var start = _cabinet.TryGet(fromBlock, out var source) ? source.KeyCenter() : target.Root.worldBound.center;
            _items.Flights.FlyKey(start, target.Root);
        }

        private void KnockBoardsOff(int blockId)
        {
            if (!_cabinet.TryGet(blockId, out var nailed))
            {
                return;
            }

            _overlay.Burst(nailed.Root.worldBound.center, EffectKind.Dust, UnboardDust);
            nailed.KnockBoardsOff();
            _audio.Play(SoundId.Snip);
            _audio.Play(SoundId.Fold);
        }

        private void Thaw(int blockId)
        {
            if (_cabinet.TryGet(blockId, out var thawed))
            {
                _overlay.Burst(thawed.Root.worldBound.center, EffectKind.Spark, ThawSparks, IceColors);
            }
        }
    }
}
