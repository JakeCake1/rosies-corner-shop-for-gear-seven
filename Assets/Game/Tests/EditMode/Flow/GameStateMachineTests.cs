using System;
using System.Collections;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Flow;
using NUnit.Framework;
using UniRx;
using UnityEngine;
using UnityEngine.TestTools;
using VContainer;

namespace Game.Tests.EditMode.Flow
{
    /// <summary>
    /// Failed state entries: the half-entered state is exited, the failure is reported with a working retry.
    /// </summary>
    public sealed class GameStateMachineTests
    {
        private IObjectResolver _resolver;
        private GameStateMachine _machine;
        private FlakyState _state;

        [SetUp]
        public void SetUp()
        {
            _state = new FlakyState();
            var builder = new ContainerBuilder();
            builder.RegisterInstance(_state);
            _resolver = builder.Build();
            _machine = new GameStateMachine(_resolver);
        }

        [TearDown]
        public void TearDown()
        {
            _machine.Dispose();
            _resolver.Dispose();
        }

        [UnityTest]
        public IEnumerator Enter_WhenStateFails_ExitsItAndReportsFailure() => UniTask.ToCoroutine(async () =>
        {
            _state.IsFailing = true;
            Func<UniTask> retry = null;
            using var subscription = _machine.Failed.Subscribe(r => retry = r);
            LogAssert.Expect(LogType.Exception, new System.Text.RegularExpressions.Regex("load failed"));

            var isThrown = false;
            try
            {
                await _machine.Enter<FlakyState>(CancellationToken.None);
            }
            catch (InvalidOperationException)
            {
                isThrown = true;
            }

            Assert.That(isThrown, Is.True);
            Assert.That(_state.ExitCount, Is.EqualTo(1));
            Assert.That(retry, Is.Not.Null);
        });

        [UnityTest]
        public IEnumerator Retry_AfterFailure_EntersTheState() => UniTask.ToCoroutine(async () =>
        {
            _state.IsFailing = true;
            Func<UniTask> retry = null;
            using var subscription = _machine.Failed.Subscribe(r => retry = r);
            LogAssert.Expect(LogType.Exception, new System.Text.RegularExpressions.Regex("load failed"));
            try
            {
                await _machine.Enter<FlakyState>(CancellationToken.None);
            }
            catch (InvalidOperationException)
            {
                // Reported through Failed as well.
            }

            _state.IsFailing = false;
            await retry();

            Assert.That(_state.EnteredCount, Is.EqualTo(1));
        });

        [UnityTest]
        public IEnumerator Enter_WhenCancelled_DoesNotReportFailure() => UniTask.ToCoroutine(async () =>
        {
            _state.IsCancelling = true;
            var failures = 0;
            using var subscription = _machine.Failed.Subscribe(_ => failures++);

            try
            {
                await _machine.Enter<FlakyState>(CancellationToken.None);
            }
            catch (OperationCanceledException)
            {
                // Expected.
            }

            Assert.That(failures, Is.EqualTo(0));
        });

        private sealed class FlakyState : IGameState
        {
            public bool IsFailing;
            public bool IsCancelling;
            public int EnteredCount;
            public int ExitCount;

            public UniTask Enter(CancellationToken ct)
            {
                if (IsCancelling)
                {
                    throw new OperationCanceledException();
                }
                if (IsFailing)
                {
                    throw new InvalidOperationException("load failed");
                }

                EnteredCount++;
                return UniTask.CompletedTask;
            }

            public UniTask Exit(CancellationToken ct)
            {
                ExitCount++;
                return UniTask.CompletedTask;
            }
        }
    }
}
