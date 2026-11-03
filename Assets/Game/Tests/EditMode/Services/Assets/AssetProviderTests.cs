using System;
using System.Collections;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Services.Assets;
using NUnit.Framework;
using UnityEngine.ResourceManagement;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.TestTools;

namespace Game.Tests.EditMode.Services.Assets
{
    /// <summary>
    /// A failed load must not stay cached: the next call loads again instead of returning null.
    /// </summary>
    public sealed class AssetProviderTests
    {
        private ResourceManager _resources;
        private AssetProvider _provider;
        private int _loads;

        [SetUp]
        public void SetUp()
        {
            _resources = new ResourceManager();
            _provider = new AssetProvider();
            _loads = 0;
            // Failed operations log their exception through the resource manager.
            LogAssert.ignoreFailingMessages = true;
        }

        [TearDown]
        public void TearDown()
        {
            LogAssert.ignoreFailingMessages = false;
            _provider.ReleaseAll();
            _resources.Dispose();
        }

        [UnityTest]
        public IEnumerator LoadAsync_WhenLoadFails_ThrowsAndNextCallLoadsAgain() => UniTask.ToCoroutine(async () =>
        {
            _provider.StartLoad = _ => ++_loads == 1
                ? (AsyncOperationHandle)_resources.CreateCompletedOperationWithException<object>(
                    null, new InvalidOperationException("boom"))
                : _resources.CreateCompletedOperation<object>("asset", null);

            var isThrown = false;
            try
            {
                await _provider.LoadAsync<string>("key", CancellationToken.None);
            }
            catch (Exception)
            {
                isThrown = true;
            }

            var second = await _provider.LoadAsync<string>("key", CancellationToken.None);

            Assert.That(isThrown, Is.True);
            Assert.That(second, Is.EqualTo("asset"));
            Assert.That(_loads, Is.EqualTo(2));
        });

        [UnityTest]
        public IEnumerator LoadAsync_WhenFirstCallerCancelledAndLoadThenFails_LoadsAgain() => UniTask.ToCoroutine(async () =>
        {
            var operation = new ManualOperation();
            var pending = _resources.StartOperation(operation, default);
            _provider.StartLoad = _ => ++_loads == 1
                ? (AsyncOperationHandle)pending
                : _resources.CreateCompletedOperation<object>("asset", null);
            using var cts = new CancellationTokenSource();

            var first = _provider.LoadAsync<string>("key", cts.Token).AsTask();
            cts.Cancel();
            try
            {
                await first;
            }
            catch (OperationCanceledException)
            {
            }

            operation.Finish(false);
            var second = await _provider.LoadAsync<string>("key", CancellationToken.None);

            Assert.That(second, Is.EqualTo("asset"));
            Assert.That(_loads, Is.EqualTo(2));
        });

        [UnityTest]
        public IEnumerator LoadAsync_WhenLoaded_ReturnsCachedAsset() => UniTask.ToCoroutine(async () =>
        {
            _provider.StartLoad = _ => ++_loads == 1
                ? (AsyncOperationHandle)_resources.CreateCompletedOperation<object>("asset", null)
                : _resources.CreateCompletedOperation<object>("other", null);

            var first = await _provider.LoadAsync<string>("key", CancellationToken.None);
            var second = await _provider.LoadAsync<string>("key", CancellationToken.None);

            Assert.That(first, Is.EqualTo("asset"));
            Assert.That(second, Is.EqualTo("asset"));
            Assert.That(_loads, Is.EqualTo(1));
        });

        private sealed class ManualOperation : AsyncOperationBase<object>
        {
            public void Finish(bool isSuccess) =>
                Complete(isSuccess ? "asset" : null, isSuccess, isSuccess ? null : "load failed");

            protected override void Execute()
            {
            }
        }
    }
}
