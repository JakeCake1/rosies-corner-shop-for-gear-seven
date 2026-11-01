using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Services.Assets;
using UniRx;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using Object = UnityEngine.Object;

namespace Game.Services.UI
{
    /// <summary>
    /// Default <see cref="IUIService"/>: one stack per layer, a shared popup blocker, Addressables prefabs
    /// instantiated through the container so windows get their <c>[Inject]</c> methods called.
    /// </summary>
    public sealed class UIService : IUIService, ITickable, IDisposable
    {
        private const string AddressPrefix = "ui/";

        private readonly UIRoot _root;
        private readonly IAssetProvider _assets;
        private readonly IObjectResolver _resolver;
        private readonly Dictionary<UILayer, List<OpenWindow>> _stacks = new Dictionary<UILayer, List<OpenWindow>>();
        private readonly CancellationTokenSource _lifetime = new CancellationTokenSource();

        /// <summary>
        /// Creates the service.
        /// </summary>
        /// <param name="root">Layer containers and the popup blocker.</param>
        /// <param name="assets">Loads window prefabs.</param>
        /// <param name="resolver">Instantiates prefabs with injection; allowed here as UI infrastructure.</param>
        public UIService(UIRoot root, IAssetProvider assets, IObjectResolver resolver)
        {
            _root = root;
            _assets = assets;
            _resolver = resolver;
            foreach (UILayer layer in Enum.GetValues(typeof(UILayer)))
            {
                _stacks[layer] = new List<OpenWindow>();
            }

            _root.Blocker.onClick.AddListener(OnBlockerClicked);
            UpdateBlocker();
        }

        /// <inheritdoc/>
        public bool IsPopupOpen => _stacks[UILayer.Popups].Count > 0;

        /// <inheritdoc/>
        public async UniTask<TWindow> OpenAsync<TWindow, TViewModel>(TViewModel viewModel, CancellationToken ct)
            where TWindow : WindowView<TViewModel>
            where TViewModel : ViewModel
        {
            await CloseAsync<TWindow>();
            var prefab = await _assets.LoadAsync<GameObject>(AddressPrefix + typeof(TWindow).Name, ct);
            var go = _resolver.Instantiate(prefab, _root.GetLayer(UILayer.Overlay));
            var window = go.GetComponent<TWindow>();
            go.transform.SetParent(_root.GetLayer(window.Layer), false);
            Stretch((RectTransform)go.transform);

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
                // App shutdown: destroy without the transition.
            }

            stack.Remove(entry);
            entry.CloseSubscription?.Dispose();
            window.Release();
            entry.ViewModel.Dispose();
            if (window.Root != null) Object.Destroy(window.Root);
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

        /// <summary>
        /// Maps Escape / the Android back button to <see cref="Back"/>; called every frame by VContainer.
        /// </summary>
        public void Tick()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) Back();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _lifetime.Cancel();
            _lifetime.Dispose();
            if (_root != null) _root.Blocker.onClick.RemoveListener(OnBlockerClicked);
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
        }

        private void OnBlockerClicked() => Back();

        private void UpdateBlocker()
        {
            var popups = _stacks[UILayer.Popups];
            var blocker = _root.Blocker.transform;
            _root.Blocker.gameObject.SetActive(popups.Count > 0);
            if (popups.Count == 0) return;

            blocker.SetAsLastSibling();
            popups[popups.Count - 1].Window.Root.transform.SetAsLastSibling();
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
