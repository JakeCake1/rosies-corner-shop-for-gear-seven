using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Services.Assets;
using Game.Services.UI.Elements;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;
using VContainer.Unity;

namespace Game.Services.UI
{
    /// <summary>
    /// Window factory and per-layer stacks. A window's layout (UXML) is loaded through Addressables and its
    /// elements are created when the window opens and removed from the panel when it closes.
    /// </summary>
    public sealed class UIService : IUIService, ITickable, IDisposable
    {
        private const string AddressPrefix = "ui/";
        private const string WindowClass = "window";

        private readonly UIRoot _root;
        private readonly IAssetProvider _assets;
        private readonly IObjectResolver _resolver;
        private readonly Dictionary<UILayer, List<OpenWindow>> _stacks = new Dictionary<UILayer, List<OpenWindow>>();
        private readonly CancellationTokenSource _lifetime = new CancellationTokenSource();
        private readonly EventCallback<ClickEvent> _onBlockerClicked;

        /// <summary>
        /// Creates the service.
        /// </summary>
        /// <param name="root">Layer containers.</param>
        /// <param name="assets">Loads window layouts.</param>
        /// <param name="resolver">Injects window views.</param>
        public UIService(UIRoot root, IAssetProvider assets, IObjectResolver resolver)
        {
            _root = root;
            _assets = assets;
            _resolver = resolver;
            foreach (UILayer layer in Enum.GetValues(typeof(UILayer)))
            {
                _stacks[layer] = new List<OpenWindow>();
            }

            _onBlockerClicked = _ => Back();
            _root.Blocker.RegisterCallback(_onBlockerClicked);
            UpdateBlocker();
        }

        /// <inheritdoc/>
        public bool IsPopupOpen => _stacks[UILayer.Popups].Count > 0;

        /// <inheritdoc/>
        public async UniTask<TWindow> OpenAsync<TWindow, TViewModel>(TViewModel viewModel, CancellationToken ct)
            where TWindow : WindowView<TViewModel>, new()
            where TViewModel : ViewModel
        {
            await CloseAsync<TWindow>();
            var layout = await _assets.LoadAsync<VisualTreeAsset>(AddressPrefix + typeof(TWindow).Name, ct);
            var element = layout.Instantiate();
            element.AddToClassList(WindowClass);
            element.pickingMode = PickingMode.Ignore;
            element.Stretch();

            var window = new TWindow();
            _resolver.Inject(window);
            window.Attach(element);
            _root.GetLayer(window.Layer).Add(element);

            var entry = new OpenWindow(window, viewModel);
            entry.CloseSubscription = window.CloseRequested.Subscribe(w => CloseAsync(w).Forget());
            _stacks[window.Layer].Add(entry);
            window.Bind(viewModel);
            UpdateBlocker();
            using (var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, _lifetime.Token))
            {
                await window.ShowAsync(linked.Token);
            }

            return window;
        }

        /// <inheritdoc/>
        public async UniTask CloseAsync(IWindow window)
        {
            if (window == null) return;

            var stack = _stacks[window.Layer];
            var entry = stack.FirstOrDefault(e => ReferenceEquals(e.Window, window));
            if (entry == null || entry.IsClosing) return;

            entry.IsClosing = true;
            try
            {
                await window.HideAsync(_lifetime.Token);
            }
            catch (OperationCanceledException)
            {
                // App shutdown: remove without the transition.
            }

            stack.Remove(entry);
            entry.CloseSubscription?.Dispose();
            window.Root.RemoveFromHierarchy();
            window.Release();
            entry.ViewModel.Dispose();
            UpdateBlocker();
        }

        /// <inheritdoc/>
        public async UniTask CloseAsync<TWindow>() where TWindow : IWindow
        {
            var open = _stacks.Values.SelectMany(s => s).Where(e => e.Window is TWindow).ToList();
            foreach (var e in open)
            {
                await CloseAsync(e.Window);
            }
        }

        /// <inheritdoc/>
        public async UniTask CloseAllAsync(UILayer layer)
        {
            var open = _stacks[layer].AsEnumerable().Reverse().ToList();
            foreach (var e in open)
            {
                await CloseAsync(e.Window);
            }
        }

        /// <inheritdoc/>
        public void Back()
        {
            var popups = _stacks[UILayer.Popups];
            if (popups.Count == 0) return;

            var top = popups[popups.Count - 1];
            if (top.Window.IsClosedByBlocker) CloseAsync(top.Window).Forget();
        }

        /// <inheritdoc/>
        public void Tick()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) Back();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _lifetime.Cancel();
            _lifetime.Dispose();
            if (_root != null) _root.Blocker.UnregisterCallback(_onBlockerClicked);
        }

        private void UpdateBlocker()
        {
            var popups = _stacks[UILayer.Popups];
            _root.Blocker.SetVisible(popups.Count > 0);
            if (popups.Count == 0) return;

            _root.Blocker.BringToFront();
            popups[popups.Count - 1].Window.Root.BringToFront();
        }

        private sealed class OpenWindow
        {
            public OpenWindow(IWindow window, ViewModel viewModel)
            {
                Window = window;
                ViewModel = viewModel;
            }

            public IWindow Window { get; }

            public ViewModel ViewModel { get; }

            public IDisposable CloseSubscription { get; set; }

            public bool IsClosing { get; set; }
        }
    }
}
