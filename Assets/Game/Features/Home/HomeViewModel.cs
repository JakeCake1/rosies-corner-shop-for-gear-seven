using System;
using System.Collections.Generic;
using System.Linq;
using Game.Services.Balance;
using Game.Services.Content;
using Game.Services.Levels;
using Game.Services.Localization;
using Game.Services.Onboarding;
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
        private readonly HomeProgress _progress;
        private readonly ILevelCurveService _curves;
        private readonly IContentService _content;
        private readonly ILocalizationService _texts;
        private readonly GameBalanceConfig _balance;

        /// <summary>Shop name on the sign, broken into two lines (<see cref="SignLines"/>).</summary>
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

        /// <summary>Line under the shop name ("GROCERIES · BAKERY · SWEETS").</summary>
        public string Tagline { get; }

        /// <summary>Caption of the "For reviewers" link (rich text, underlined).</summary>
        public string ReviewersLink { get; }

        /// <summary>Caption of the album button.</summary>
        public string AlbumCaption { get; }

        /// <summary>First line of the play button ("Open the shop").</summary>
        public string PlayTitle { get; }

        /// <summary>Commands for the home presenter (navigation lives outside the view model).</summary>
        public IObservable<HomeCommand> Commands => _commands;

        /// <summary>
        /// Creates the view model and follows progress changes.
        /// </summary>
        /// <param name="progress">Coins, stars, album, flags, shop name.</param>
        /// <param name="curves">Next level and its difficulty.</param>
        /// <param name="content">Item sprites for the showcase.</param>
        /// <param name="texts">Sign, button and play line texts.</param>
        /// <param name="balance">Level that opens the album.</param>
        public HomeViewModel(HomeProgress progress, ILevelCurveService curves, IContentService content,
            ILocalizationService texts, GameBalanceConfig balance)
        {
            _progress = progress;
            _balance = balance;
            _curves = curves;
            _content = content;
            _texts = texts;
            Tagline = texts.Get(HomeTexts.Tagline);
            ReviewersLink = texts.Get(HomeTexts.Reviewers);
            AlbumCaption = texts.Get(HomeTexts.Album);
            PlayTitle = texts.Get(HomeTexts.PlayTitle);
            Coins = progress.Wallet.Coins;
            Disposables.Add(_shopName);
            Disposables.Add(_subtitle);
            Disposables.Add(_playLine);
            Disposables.Add(_isPlayTricky);
            Disposables.Add(_isAlbumLocked);
            Disposables.Add(_albumBadge);
            Disposables.Add(_isAlbumPulsing);
            Disposables.Add(_showcase);
            Disposables.Add(_commands);
            progress.Profile.Changed.Subscribe(_ => Refresh()).AddTo(Disposables);
            Refresh();
        }

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
            var album = _progress.Album;
            var shopName = _progress.Shop.Name;
            var curve = _curves.Active;
            var next = _curves.NextLevel();
            var hard = _curves.Get(next).IsHard;
            _shopName.Value = SignLines.Split(string.IsNullOrEmpty(shopName) ? _texts.Get(HomeTexts.DefaultShopName) : shopName);
            _subtitle.Value = _texts.Get(curve.SubtitleKey);
            _playLine.Value = _texts.Format(hard ? HomeTexts.PlayLineTricky : HomeTexts.PlayLine, next);
            _isPlayTricky.Value = hard;
            var isOpen = curve.IsOriginal || _progress.Levels.IsCompleted(_balance.AlbumFromLevel);
            _isAlbumLocked.Value = !isOpen;
            _albumBadge.Value = isOpen && album.NewCount > 0 ? (album.NewCount > 9 ? "9+" : album.NewCount.ToString()) : string.Empty;
            _isAlbumPulsing.Value = isOpen && !_progress.Onboarding.HasFlag(Flags.AlbumOpened);
            var ids = album.Found.Count >= ShowcaseSize
                ? album.Found.Skip(album.Found.Count - ShowcaseSize).ToList()
                : _content.Catalog.DefaultShowcase.ToList();
            _showcase.Value = ids.Select(id => _content.GetItemSprite(id)).ToList();
        }
    }
}
