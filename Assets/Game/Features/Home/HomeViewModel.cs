using System;
using System.Collections.Generic;
using System.Linq;
using Game.Services.Content;
using Game.Services.Levels;
using Game.Services.Progress;
using Game.Services.UI;
using UniRx;
using UnityEngine;

namespace Game.Features.Home
{
    /// <summary>
    /// State and commands of the home screen: shop sign, showcase, coins, album button and "Open the shop".
    /// </summary>
    public sealed class HomeViewModel : ViewModel
    {
        /// <summary>
        /// Name on the sign until the player names the shop.
        /// </summary>
        public const string DefaultShopName = "Rosie's Corner Shop";

        private const int AlbumFromLevel = 9;
        private const int ShowcaseSize = 7;

        private readonly ReactiveProperty<string> _shopName = new ReactiveProperty<string>();
        private readonly ReactiveProperty<string> _subtitle = new ReactiveProperty<string>();
        private readonly ReactiveProperty<string> _playLine = new ReactiveProperty<string>();
        private readonly ReactiveProperty<bool> _isPlayTricky = new ReactiveProperty<bool>();
        private readonly ReactiveProperty<bool> _isAlbumLocked = new ReactiveProperty<bool>();
        private readonly ReactiveProperty<string> _albumBadge = new ReactiveProperty<string>();
        private readonly ReactiveProperty<bool> _isAlbumPulsing = new ReactiveProperty<bool>();
        private readonly ReactiveProperty<IReadOnlyList<Sprite>> _showcase = new ReactiveProperty<IReadOnlyList<Sprite>>();
        private readonly Subject<HomeCommand> _commands = new Subject<HomeCommand>();
        private readonly IProgressService _progress;
        private readonly ILevelCurveService _curves;
        private readonly IContentService _content;

        /// <summary>
        /// Creates the view model and follows progress changes.
        /// </summary>
        /// <param name="progress">Coins, stars, album, shop name.</param>
        /// <param name="curves">Next level and its difficulty.</param>
        /// <param name="content">Item sprites for the showcase.</param>
        public HomeViewModel(IProgressService progress, ILevelCurveService curves, IContentService content)
        {
            _progress = progress;
            _curves = curves;
            _content = content;
            Coins = progress.Coins;
            Disposables.Add(_shopName);
            Disposables.Add(_subtitle);
            Disposables.Add(_playLine);
            Disposables.Add(_isPlayTricky);
            Disposables.Add(_isAlbumLocked);
            Disposables.Add(_albumBadge);
            Disposables.Add(_isAlbumPulsing);
            Disposables.Add(_showcase);
            Disposables.Add(_commands);
            progress.Changed.Subscribe(_ => Refresh()).AddTo(Disposables);
            Refresh();
        }

        /// <summary>Shop name on the sign.</summary>
        public IReadOnlyReactiveProperty<string> ShopName => _shopName;

        /// <summary>Line under the sign ("Prototype · 20 levels · placeholder art").</summary>
        public IReadOnlyReactiveProperty<string> Subtitle => _subtitle;

        /// <summary>Second line of the play button ("Level 4 · Tricky · Rewards ×2").</summary>
        public IReadOnlyReactiveProperty<string> PlayLine => _playLine;

        /// <summary>Play button styled red-tricky.</summary>
        public IReadOnlyReactiveProperty<bool> IsPlayTricky => _isPlayTricky;

        /// <summary>Album button greyed with a lock (before level 9 is cleared).</summary>
        public IReadOnlyReactiveProperty<bool> IsAlbumLocked => _isAlbumLocked;

        /// <summary>"NEW" badge count ("3", "9+"), empty to hide.</summary>
        public IReadOnlyReactiveProperty<string> AlbumBadge => _albumBadge;

        /// <summary>Album button pulses until the album is opened the first time.</summary>
        public IReadOnlyReactiveProperty<bool> IsAlbumPulsing => _isAlbumPulsing;

        /// <summary>Seven items on the showcase plank.</summary>
        public IReadOnlyReactiveProperty<IReadOnlyList<Sprite>> Showcase => _showcase;

        /// <summary>Coins.</summary>
        public IReadOnlyReactiveProperty<int> Coins { get; }

        /// <summary>Commands for the home presenter (navigation lives outside the view model).</summary>
        public IObservable<HomeCommand> Commands => _commands;

        /// <summary>
        /// "Open the shop" tapped.
        /// </summary>
        public void Play() => _commands.OnNext(HomeCommand.Play);

        /// <summary>
        /// Album button tapped.
        /// </summary>
        public void OpenAlbum() => _commands.OnNext(_isAlbumLocked.Value ? HomeCommand.AlbumLocked : HomeCommand.Album);

        /// <summary>
        /// Gear tapped.
        /// </summary>
        public void OpenSettings() => _commands.OnNext(HomeCommand.Settings);

        /// <summary>
        /// "For reviewers" link tapped.
        /// </summary>
        public void OpenReviewers() => _commands.OnNext(HomeCommand.Reviewers);

        private void Refresh()
        {
            var data = _progress.Data;
            var curve = _curves.Active;
            var next = _curves.NextLevel();
            var hard = _curves.Get(next).IsHard;
            _shopName.Value = string.IsNullOrEmpty(data.ShopName) ? DefaultShopName : data.ShopName;
            _subtitle.Value = curve.Subtitle;
            _playLine.Value = "Level " + next + (hard ? " · Tricky · Rewards ×2" : string.Empty);
            _isPlayTricky.Value = hard;
            var isOpen = curve.IsOriginal || data.Stars.ContainsKey(AlbumFromLevel);
            _isAlbumLocked.Value = !isOpen;
            _albumBadge.Value = isOpen && data.AlbumNew > 0 ? (data.AlbumNew > 9 ? "9+" : data.AlbumNew.ToString()) : string.Empty;
            _isAlbumPulsing.Value = isOpen && !_progress.HasFlag(Flags.AlbumOpened);
            var ids = data.Found.Count >= ShowcaseSize
                ? data.Found.Skip(data.Found.Count - ShowcaseSize).ToList()
                : _content.Catalog.DefaultShowcase.ToList();
            _showcase.Value = ids.Select(id => _content.Catalog.Get(id).Sprite).ToList();
        }
    }
}
