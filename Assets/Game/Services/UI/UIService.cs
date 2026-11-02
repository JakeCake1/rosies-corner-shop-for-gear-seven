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

        private readonly Dictionary<Type, SemaphoreSlim> _openGates = new Dictionary<Type, SemaphoreSlim>();
        private readonly UIRoot _root;
        private readonly IAssetProvider _assets;
        private readonly IObjectResolver _resolver;
        private readonly Dictionary<UILayer, List<OpenWindow>> _stacks = new Dictionary<UILayer, List<OpenWindow>>();
        private readonly CancellationTokenSource _lifetime = new CancellationTokenSource();
        private readonly EventCallback<ClickEvent> _onBlockerClicked;
        // Cached: reading Token from the disposed source throws, and windows still close after Dispose at shutdown.
        private readonly CancellationToken _lifetimeToken;

        /// <inheritdoc/>
        public bool IsPopupOpen => _stacks[UILayer.Popups].Count > 0;

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
            _lifetimeToken = _lifetime.Token;
            foreach (UILayer layer in Enum.GetValues(typeof(UILayer)))
            {
                _stacks[layer] = new List<OpenWindow>();
            }

            _onBlockerClicked = _ => Back();
            _root.Blocker.RegisterCallback(_onBlockerClicked);
            UpdateBlocker();
        }

        /// <inheritdoc/>
        public async UniTask<TWindow> OpenAsync<TWindow, TViewModel>(TViewModel viewModel, CancellationToken ct)
            where TWindow : WindowView<TViewModel>, new()
            where TViewModel : ViewModel
        {
            var type = typeof(TWindow);
            if (!_openGates.TryGetValue(type, out var gate))
            {
                gate = new SemaphoreSlim(1, 1);
                _openGates.Add(type, gate);
            }

            await gate.WaitAsync(ct);
            try
            {
                var layout = await _assets.LoadAsync<VisualTreeAsset>(AddressPrefix + typeof(TWindow).Name, ct);
                // Closed after the load, not before: two opens racing through the load (a double tap) would otherwise
                // both find no window to replace and stack two copies.
                await CloseAsync<TWindow>();
                ct.ThrowIfCancellationRequested();
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
                // The window lives no longer than its opener: a dialog of an unloaded scene or a hidden screen must not
                // stay on top of the next one (an open popup blocks input everywhere through IsPopupOpen).
                entry.OpenerRegistration = ct.Register(() => CloseAsync(window).Forget());
                await window.ShowAsync(_lifetimeToken);
                // Tween cancellation completes the transition instead of throwing, so report it here.
                ct.ThrowIfCancellationRequested();
                return window;
            }
            finally
            {
                gate.Release();
            }
        }

        /// <inheritdoc/>
        public async UniTask CloseAsync(IWindow window)
        {
            if (window == null)
            {
                return;
            }

            var stack = _stacks[window.Layer];
            var entry = stack.FirstOrDefault(e => ReferenceEquals(e.Window, window));
            if (entry == null)
            {
                return;
            }

            if (entry.IsClosing)
            {
                // Callers such as the flow states continue only once the window is really gone.
                await window.Closed;
                return;
            }

            entry.IsClosing = true;
            try
            {
                await window.HideAsync(_lifetimeToken);
            }
            catch (OperationCanceledException)
            {
                // App shutdown: remove without the transition.
            }

            stack.Remove(entry);
            entry.CloseSubscription?.Dispose();
            entry.OpenerRegistration.Dispose();
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
            if (popups.Count == 0)
            {
                return;
            }

            var top = popups[popups.Count - 1];
            if (top.Window.IsClosedByBlocker)
            {
                CloseAsync(top.Window).Forget();
            }
        }

        /// <inheritdoc/>
        public void Tick()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Back();
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _lifetime.Cancel();
            _lifetime.Dispose();
            if (_root != null)
            {
                _root.Blocker.UnregisterCallback(_onBlockerClicked);
            }
        }

        private void UpdateBlocker()
        {
            var popups = _stacks[UILayer.Popups];
            _root.Blocker.SetVisible(popups.Count > 0);
            if (popups.Count == 0)
            {
                return;
            }

            _root.Blocker.BringToFront();
            popups[popups.Count - 1].Window.Root.BringToFront();
        }
    }
}
