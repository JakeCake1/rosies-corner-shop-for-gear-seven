using System.Collections.Generic;
using DG.Tweening;
using Game.Services.UI;
using Game.Services.UI.Elements;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Features.Home
{
    /// <summary>
    /// The shop front: sign with the shop name, showcase shelf, coins, album and the big play button.
    /// </summary>
    public sealed class HomeWindow : WindowView<HomeViewModel>
    {
        private const string ShopNameName = "shop-name";
        private const string SubtitleName = "subtitle";
        private const string TaglineName = "tagline";
        private const string ReviewersName = "reviewers";
        private const string ShowcaseName = "showcase-items";
        private const string CoinsName = "coins-value";
        private const string AlbumName = "album";
        private const string AlbumTextName = "album-text";
        private const string AlbumLockName = "album-lock";
        private const string AlbumBadgeName = "album-badge";
        private const string AlbumBadgeTextName = "album-badge-text";
        private const string PlayName = "play";
        private const string PlayLineName = "play-line";
        private const string PlayTitleName = "play-title";
        private const string SettingsName = "settings";
        private const string OpenAlbumClass = "kit-btn-wood";
        private const string LockedAlbumClass = "kit-btn-grey";
        private const string PlayClass = "kit-btn-berry";
        private const string TrickyPlayClass = "kit-btn-gold";
        private const float LongNameSize = 26f;
        private const float MediumNameSize = 31f;
        private const float NormalNameSize = 36f;
        private const int MediumNameLine = 12;
        private const int LongNameLine = 16;

        private readonly List<VisualElement> _showcase = new List<VisualElement>();
        private Label _shopName;
        private Label _subtitle;
        private Label _tagline;
        private Button _reviewers;
        private Label _coins;
        private Button _album;
        private Label _albumText;
        private VisualElement _albumLock;
        private VisualElement _albumBadge;
        private Label _albumBadgeText;
        private Button _play;
        private Label _playLine;
        private Label _playTitle;
        private Button _settings;
        private Tween _albumPulse;

        /// <inheritdoc/>
        public override UILayer Layer => UILayer.Screens;

        /// <summary>Album button; the locked-album tip pops up above it.</summary>
        public VisualElement AlbumButton => _album;

        /// <inheritdoc/>
        protected override void OnLayoutAttached()
        {
            _shopName = Q<Label>(ShopNameName);
            _subtitle = Q<Label>(SubtitleName);
            _tagline = Q<Label>(TaglineName);
            _reviewers = Q<Button>(ReviewersName);
            _showcase.AddRange(Q(ShowcaseName).Children());
            _coins = Q<Label>(CoinsName);
            _album = Q<Button>(AlbumName);
            _albumText = Q<Label>(AlbumTextName);
            _albumLock = Q(AlbumLockName);
            _albumBadge = Q(AlbumBadgeName);
            _albumBadgeText = Q<Label>(AlbumBadgeTextName);
            _play = Q<Button>(PlayName);
            _playLine = Q<Label>(PlayLineName);
            _playTitle = Q<Label>(PlayTitleName);
            _settings = Q<Button>(SettingsName);
        }

        /// <inheritdoc/>
        protected override void OnBind(HomeViewModel vm)
        {
            _tagline.text = vm.Tagline;
            _reviewers.text = vm.ReviewersLink;
            _albumText.text = vm.AlbumCaption;
            _playTitle.text = vm.PlayTitle;
            vm.ShopName.Subscribe(SetShopName).AddTo(Bindings);
            vm.Subtitle.Subscribe(t => _subtitle.text = t).AddTo(Bindings);
            vm.Showcase.Subscribe(SetShowcase).AddTo(Bindings);
            vm.Coins.Subscribe(c => _coins.text = c.ToString()).AddTo(Bindings);
            vm.IsAlbumLocked.Subscribe(x =>
            {
                _albumLock.SetVisible(x);
                _album.EnableInClassList(LockedAlbumClass, x);
                _album.EnableInClassList(OpenAlbumClass, !x);
            }).AddTo(Bindings);
            vm.AlbumBadge.Subscribe(t =>
            {
                _albumBadge.SetVisible(!string.IsNullOrEmpty(t));
                _albumBadgeText.text = t;
            }).AddTo(Bindings);
            vm.IsAlbumPulsing.Subscribe(SetAlbumPulse).AddTo(Bindings);
            vm.PlayLine.Subscribe(t => _playLine.text = t).AddTo(Bindings);
            vm.IsPlayTricky.Subscribe(x =>
            {
                _play.EnableInClassList(TrickyPlayClass, x);
                _play.EnableInClassList(PlayClass, !x);
            }).AddTo(Bindings);
            OnTap(_play, vm.Play);
            OnTap(_album, vm.OpenAlbum);
            OnTap(_settings, vm.OpenSettings);
            OnTap(_reviewers, vm.OpenReviewers);
        }

        /// <inheritdoc/>
        protected override void OnUnbind() => SetAlbumPulse(false);

        private void SetShopName(string name)
        {
            _shopName.text = name;
            var longest = SignLines.LongestLine(name);
            _shopName.style.fontSize = longest > LongNameLine ? LongNameSize : longest > MediumNameLine ? MediumNameSize : NormalNameSize;
        }

        private void SetShowcase(IReadOnlyList<Sprite> sprites)
        {
            for (var i = 0; i < _showcase.Count; i++)
            {
                var has = i < sprites.Count;
                _showcase[i].SetVisible(has);
                if (has)
                {
                    _showcase[i].SetSprite(sprites[i]);
                }
            }
        }

        private void SetAlbumPulse(bool isOn)
        {
            _albumPulse?.Kill();
            VisualElementTweens.SetScale(_album, Vector2.one);
            if (isOn)
            {
                _albumPulse = _album.DOScale(1.06f, 0.5f).SetLoops(-1, LoopType.Yoyo).SetId(this);
            }
        }
    }
}
