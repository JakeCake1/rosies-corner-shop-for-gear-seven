using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniRx;
using UnityEngine;

namespace Game.Services.Save
{
    /// <summary>
    /// Default <see cref="IProfileService"/>; receives every registered <see cref="IProfileChunk"/> from the
    /// container and saves only the chunk that changed.
    /// </summary>
    public sealed class ProfileService : IProfileService, IDisposable
    {
        private readonly ISaveService _save;
        private readonly IReadOnlyList<IProfileChunk> _chunks;
        private readonly Subject<Unit> _changed = new Subject<Unit>();
        private readonly CompositeDisposable _subscriptions = new CompositeDisposable();
        private bool _isLoaded;

        /// <inheritdoc/>
        public IObservable<Unit> Changed => _changed;

        /// <summary>
        /// Creates the service and follows the chunks' changes; call <see cref="Load"/> before use.
        /// </summary>
        /// <param name="save">Chunk persistence.</param>
        /// <param name="chunks">All profile chunks.</param>
        /// <exception cref="InvalidOperationException">Two chunks share a key.</exception>
        public ProfileService(ISaveService save, IReadOnlyList<IProfileChunk> chunks)
        {
            var duplicate = chunks.GroupBy(c => c.Key).FirstOrDefault(g => g.Count() > 1);
            if (duplicate != null)
            {
                throw new InvalidOperationException($"Profile chunk key '{duplicate.Key}' is used twice.");
            }

            _save = save;
            _chunks = chunks;
            foreach (var chunk in chunks)
            {
                chunk.Changed.Subscribe(_ => OnChunkChanged(chunk)).AddTo(_subscriptions);
            }
        }

        /// <inheritdoc/>
        public void Load()
        {
            var missing = new List<IProfileChunk>();
            foreach (var chunk in _chunks)
            {
                if (_save.TryLoad(chunk))
                {
                    continue;
                }
                else
                {
                    chunk.Restore(null);
                    missing.Add(chunk);
                }
            }

            if (missing.Count > 0)
            {
                _save.TryMigrateLegacy(missing);
            }
            _isLoaded = true;
            _changed.OnNext(Unit.Default);
        }

        /// <inheritdoc/>
        public void SaveAll()
        {
            // Synchronous: the OS may kill a paused app before a background write runs.
            if (!_isLoaded)
            {
                return;
            }

            // One broken chunk must not skip the others, nor the caller's work after SaveAll (pausing the audio).
            foreach (var chunk in _chunks)
            {
                try
                {
                    _save.Save(chunk);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Saving chunk '{chunk.Key}' failed: {e}");
                }
            }
        }

        /// <inheritdoc/>
        public void ResetProgress()
        {
            foreach (var chunk in _chunks.Where(c => !c.IsKeptOnReset))
            {
                chunk.Restore(null);
                Save(chunk);
            }

            _changed.OnNext(Unit.Default);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _subscriptions.Dispose();
            _changed.OnCompleted();
            _changed.Dispose();
        }

        private void OnChunkChanged(IProfileChunk chunk)
        {
            Save(chunk);
            _changed.OnNext(Unit.Default);
        }

        private void Save(IProfileChunk chunk)
        {
            // Until Load ran the chunks hold a new player's defaults; saving them (e.g. the app losing focus at
            // startup) would overwrite the stored profile before it is read.
            if (!_isLoaded)
            {
                return;
            }

            // Not cancelled on dispose: at app shutdown a save may wait behind another chunk's write, and cancelling
            // it would drop that chunk's last change. SaveService is built to finish writes after shutdown starts.
            _save.SaveAsync(chunk, CancellationToken.None).Forget();
        }
    }
}
