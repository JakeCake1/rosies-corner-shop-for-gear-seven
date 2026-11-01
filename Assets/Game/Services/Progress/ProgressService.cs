using System;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Services.Content;
using Game.Services.Save;
using UniRx;

namespace Game.Services.Progress
{
    /// <summary>
    /// Default <see cref="IProgressService"/>: holds the profile, mirrors coins into a reactive property and saves
    /// through <see cref="ISaveService"/> after each change.
    /// </summary>
    public sealed class ProgressService : IProgressService, IDisposable
    {
        /// <summary>
        /// Coins paid once when every item of a department is in the album.
        /// </summary>
        public const int FamilyBonus = 50;

        private readonly ISaveService _saveService;
        private readonly IContentService _content;
        private readonly ReactiveProperty<int> _coins = new ReactiveProperty<int>();
        private readonly Subject<Unit> _changed = new Subject<Unit>();
        private readonly CancellationTokenSource _lifetime = new CancellationTokenSource();

        /// <summary>
        /// Creates the service; call <see cref="Load"/> before use.
        /// </summary>
        /// <param name="saveService">Profile persistence.</param>
        /// <param name="content">Item catalog, for department completion.</param>
        public ProgressService(ISaveService saveService, IContentService content)
        {
            _saveService = saveService;
            _content = content;
        }

        /// <inheritdoc/>
        public PlayerProfileData Data { get; private set; } = PlayerProfileData.CreateNew();

        /// <inheritdoc/>
        public IReadOnlyReactiveProperty<int> Coins => _coins;

        /// <inheritdoc/>
        public IObservable<Unit> Changed => _changed;

        /// <inheritdoc/>
        public void Load()
        {
            Data = _saveService.Load();
            _coins.Value = Data.Coins;
        }

        /// <inheritdoc/>
        public void Save() => _saveService.SaveAsync(Data, _lifetime.Token).Forget();

        /// <inheritdoc/>
        public void AddCoins(int amount)
        {
            if (amount <= 0) return;

            Data.Coins += amount;
            NotifyChanged();
        }

        /// <inheritdoc/>
        public bool TrySpend(int amount)
        {
            if (amount < 0 || Data.Coins < amount) return false;

            Data.Coins -= amount;
            NotifyChanged();
            return true;
        }

        /// <inheritdoc/>
        public int GetBooster(string id) => Data.Boosters.TryGetValue(id, out var n) ? n : 0;

        /// <inheritdoc/>
        public void AddBooster(string id, int delta)
        {
            Data.Boosters[id] = Math.Max(0, GetBooster(id) + delta);
            NotifyChanged();
        }

        /// <inheritdoc/>
        public bool HasFlag(string flag) => Data.Flags.Contains(flag);

        /// <inheritdoc/>
        public void SetFlag(string flag)
        {
            if (Data.Flags.Add(flag)) NotifyChanged();
        }

        /// <inheritdoc/>
        public void CompleteLevel(int level, int stars)
        {
            Data.Stars[level] = Math.Max(stars, Data.Stars.TryGetValue(level, out var prev) ? prev : 0);
            NotifyChanged();
        }

        /// <inheritdoc/>
        public string RegisterFound(int itemId)
        {
            if (Data.Found.Contains(itemId)) return null;

            Data.Found.Add(itemId);
            Data.AlbumNew++;
            var family = _content.Catalog.Get(itemId).Family;
            string completed = null;
            if (!Data.CompletedFamilies.Contains(family) &&
                _content.Catalog.Items.Where(i => i.Family == family).All(i => Data.Found.Contains(i.Id)))
            {
                Data.CompletedFamilies.Add(family);
                Data.Coins += FamilyBonus;
                completed = family;
            }

            NotifyChanged();
            return completed;
        }

        /// <inheritdoc/>
        public void ResetProgress()
        {
            var keep = Data;
            Data = PlayerProfileData.CreateNew();
            Data.IsSoundOn = keep.IsSoundOn;
            Data.IsMusicOn = keep.IsMusicOn;
            Data.IsHapticsOn = keep.IsHapticsOn;
            Data.Curve = keep.Curve;
            Data.LossOffer = keep.LossOffer;
            NotifyChanged();
        }

        /// <inheritdoc/>
        public void NotifyChanged()
        {
            _coins.Value = Data.Coins;
            _changed.OnNext(Unit.Default);
            Save();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _lifetime.Cancel();
            _lifetime.Dispose();
            _changed.OnCompleted();
            _changed.Dispose();
            _coins.Dispose();
        }
    }
}
