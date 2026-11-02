using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Features.Gameplay.Board;
using Game.Features.Gameplay.Hud;
using Game.Services.Audio;
using Game.Services.Content;
using Game.Services.Localization;
using Game.Services.UI;
using Game.Services.UI.Elements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Features.Gameplay
{
    /// <summary>
    /// The gameplay screen on the scene layer: exists while the gameplay scene is loaded and is removed with its
    /// lifetime scope. Owns the board, the HUD, the booster bar and the level banner.
    /// </summary>
    public sealed class GameplayScreen : IDisposable
    {
        private const string BoardName = "board";
        private const string HudName = "hud";
        private const string BoostersName = "boosters";
        private const string CounterLabelName = "counter-label";

        private readonly GameplayLayouts _layouts;
        private readonly VisualElement _element;
        private readonly VisualElement _boosterBar;

        /// <summary>
        /// Creates the screen and adds it to the scene layer.
        /// </summary>
        /// <param name="root">UI root with the layers.</param>
        /// <param name="layouts">Gameplay layouts.</param>
        /// <param name="overlay">Effects for the board.</param>
        /// <param name="content">Item catalog.</param>
        /// <param name="art">Gameplay pictures.</param>
        /// <param name="audio">Landing, shelf and key sounds of the board.</param>
        public GameplayScreen(UIRoot root, GameplayLayouts layouts, IOverlayService overlay, IContentService content,
            GameplayArt art, IAudioService audio, ILocalizationService texts)
        {
            _layouts = layouts;
            _element = layouts.Screen.Instantiate();
            _element.pickingMode = PickingMode.Ignore;
            _element.Stretch();
            root.GetLayer(UILayer.Scene).Add(_element);

            Hud = new HudView();
            Hud.Attach(_element.Q(HudName));
            Board = new BoardView(layouts, overlay, content, art, audio, Hud.ProgressBar, texts);
            Board.Attach(_element.Q(BoardName));
            _element.Q<Label>(CounterLabelName).text = texts.Get(GameplayTexts.Counter);
            _boosterBar = _element.Q(BoostersName);
            Banner = new LevelBannerView(layouts.Banner, Board.Root);
        }

        /// <summary>Shelves, counter and baskets.</summary>
        public BoardView Board { get; }

        /// <summary>Level badge, progress and coins.</summary>
        public HudView Hud { get; }

        /// <summary>"Level N" banner.</summary>
        public LevelBannerView Banner { get; }

        /// <summary>
        /// Completes once the board has a valid size (after the first layout pass).
        /// </summary>
        /// <param name="ct">Cancels waiting.</param>
        /// <returns>Completes when laid out.</returns>
        public async UniTask WhenLaidOutAsync(CancellationToken ct)
        {
            await UniTask.WaitUntil(() => IsLaidOut(Board.Root.layout), cancellationToken: ct);
        }

        /// <summary>
        /// Adds a booster button to the bar.
        /// </summary>
        /// <returns>The new button view.</returns>
        public BoosterButtonView AddBooster()
        {
            var view = new BoosterButtonView();
            var element = _layouts.Booster.Instantiate();
            element.AddToClassList("booster-slot");
            view.Attach(element);
            _boosterBar.Add(element);
            return view;
        }

        /// <summary>
        /// Removes a booster button from the bar.
        /// </summary>
        /// <param name="view">Button view.</param>
        public void RemoveBooster(BoosterButtonView view) => view.Root.RemoveFromHierarchy();

        /// <inheritdoc/>
        public void Dispose()
        {
            Banner.Hide();
            Hud.Unbind();
            Board.Dispose();
            _element.RemoveFromHierarchy();
        }

        private static bool IsLaidOut(Rect rect) =>
            !float.IsNaN(rect.width) && !float.IsNaN(rect.height) && rect.width > 0f && rect.height > 0f;
    }
}
