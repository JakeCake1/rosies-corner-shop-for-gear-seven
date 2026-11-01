using System;
using System.Collections.Generic;
using DG.Tweening;
using Game.Services.UI;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Features.Home
{
    /// <summary>
    /// The home screen: shop sign, subtitle with the reviewers link, showcase plank, coins, album and play buttons.
    /// </summary>
    public sealed class HomeWindow : WindowView<HomeViewModel>
    {
        private const float LongNameSize = 30f;
        private const float MediumNameSize = 34f;
        private const float NormalNameSize = 40f;

        [SerializeField] private TMP_Text _shopName;
        [SerializeField] private TMP_Text _subtitle;
        [SerializeField] private Button _reviewers;
        [SerializeField] private Image[] _showcase = new Image[7];
        [SerializeField] private TMP_Text _coins;
        [SerializeField] private Button _album;
        [SerializeField] private Image _albumBackground;
        [SerializeField] private Sprite _albumOpenSprite;
        [SerializeField] private Sprite _albumLockedSprite;
        [SerializeField] private GameObject _albumLock;
        [SerializeField] private GameObject _albumBadge;
        [SerializeField] private TMP_Text _albumBadgeText;
        [SerializeField] private Button _play;
        [SerializeField] private Image _playBackground;
        [SerializeField] private Sprite _playSprite;
        [SerializeField] private Sprite _playTrickySprite;
        [SerializeField] private TMP_Text _playLine;
        [SerializeField] private Button _settings;

        private Tween _albumPulse;

        /// <inheritdoc/>
        public override UILayer Layer => UILayer.Screens;

        /// <inheritdoc/>
        protected override void OnBind(HomeViewModel vm)
        {
            vm.ShopName.Subscribe(SetShopName).AddTo(Bindings);
            vm.Subtitle.Subscribe(t => _subtitle.SetText(t)).AddTo(Bindings);
            vm.Showcase.Subscribe(SetShowcase).AddTo(Bindings);
            vm.Coins.Subscribe(c => _coins.SetText("{0}", c)).AddTo(Bindings);
            vm.IsAlbumLocked.Subscribe(x =>
            {
                _albumLock.SetActive(x);
                _albumBackground.sprite = x ? _albumLockedSprite : _albumOpenSprite;
            }).AddTo(Bindings);
            vm.AlbumBadge.Subscribe(t =>
            {
                _albumBadge.SetActive(!string.IsNullOrEmpty(t));
                _albumBadgeText.SetText(t);
            }).AddTo(Bindings);
            vm.IsAlbumPulsing.Subscribe(SetAlbumPulse).AddTo(Bindings);
            vm.PlayLine.Subscribe(t => _playLine.SetText(t)).AddTo(Bindings);
            vm.IsPlayTricky.Subscribe(x => _playBackground.sprite = x ? _playTrickySprite : _playSprite).AddTo(Bindings);
            Click(_play, vm.Play);
            Click(_album, vm.OpenAlbum);
            Click(_settings, vm.OpenSettings);
            Click(_reviewers, vm.OpenReviewers);
        }

        /// <inheritdoc/>
        protected override void OnUnbind() => SetAlbumPulse(false);

        private void Click(Button button, Action action) =>
            button.OnClickAsObservable().ThrottleFirst(TimeSpan.FromMilliseconds(300)).Subscribe(_ => action()).AddTo(Bindings);

        private void SetShopName(string name)
        {
            _shopName.SetText(name);
            _shopName.fontSize = name.Length > 18 ? LongNameSize : name.Length > 13 ? MediumNameSize : NormalNameSize;
        }

        private void SetShowcase(IReadOnlyList<Sprite> sprites)
        {
            for (var i = 0; i < _showcase.Length; i++)
            {
                var has = i < sprites.Count;
                _showcase[i].gameObject.SetActive(has);
                if (has) _showcase[i].sprite = sprites[i];
            }
        }

        private void SetAlbumPulse(bool isOn)
        {
            _albumPulse?.Kill();
            _album.transform.localScale = Vector3.one;
            if (isOn) _albumPulse = _album.transform.DOScale(1.06f, 0.5f).SetLoops(-1, LoopType.Yoyo).SetLink(gameObject);
        }
    }
}
