using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Features.Settings;
using Game.Services.Flow;
using Game.Services.Onboarding;
using Game.Services.Save;
using Game.Services.Settings;
using Game.Services.UI;
using Game.Services.UI.Dialogs;
using Game.Tests.EditMode.Features.Gameplay.Session;
using Game.Tests.EditMode.Services.Localization;
using NUnit.Framework;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Tests.EditMode.Features.Settings
{
    /// <summary>
    /// Reset progress from the settings dialog: in a level it waits for the way home (the level's live scope must not
    /// see the profile change); on the home screen it comes first; app shutdown during the way home must not touch
    /// the profile.
    /// </summary>
    public sealed class SettingsScreenResetTests
    {
        private readonly List<string> _log = new List<string>();
        private UniTaskCompletionSource _goHome;
        private FakeDialogs _dialogs;
        private SettingsScreen _screen;

        [SetUp]
        public void SetUp()
        {
            _log.Clear();
            _goHome = new UniTaskCompletionSource();
            _dialogs = new FakeDialogs();
            _screen = new SettingsScreen(_dialogs, new StubSettings(), new SpyProfile(_log), new StubOnboarding(),
                new FakeAudioService(), new StubFlow(_log, _goHome), new StubNamePrompt(), new StubOverlay(),
                new FakeLocalizationService());
        }

        [Test]
        public void Reset_InLevel_WaitsForTheWayHome()
        {
            Open(() => { });

            ConfirmReset();
            Assert.That(_log, Is.EqualTo(new[] { "home" }), "profile must not change while leaving the level");

            _goHome.TrySetResult();
            Assert.That(_log, Is.EqualTo(new[] { "home", "reset" }));
        }

        [Test]
        public void Reset_OnHomeScreen_ComesBeforeReload()
        {
            Open(null);

            ConfirmReset();

            Assert.That(_log, Is.EqualTo(new[] { "reset", "home" }));
        }

        [Test]
        public void Reset_InLevel_FirstTapOnlyArms()
        {
            Open(() => { });

            _dialogs.Dialog.Press(ResetButton());

            Assert.That(_log, Is.Empty);
        }

        [Test]
        public void Reset_InLevel_ShutdownDuringWayHome_DoesNotTouchTheProfile()
        {
            Open(() => { });

            ConfirmReset();
            _goHome.TrySetCanceled();

            Assert.That(_log, Is.EqualTo(new[] { "home" }));
        }

        private void Open(Action restartLevel)
        {
            _screen.OpenAsync(restartLevel, CancellationToken.None).Forget();
        }

        private DialogButton ResetButton() => _dialogs.Dialog.Buttons.Single(b => b.Label == "settings.reset");

        private void ConfirmReset()
        {
            var reset = ResetButton();
            _dialogs.Dialog.Press(reset);
            _dialogs.Dialog.Press(reset);
        }

        private sealed class FakeDialogs : IDialogService
        {
            public DialogViewModel Dialog { get; private set; }

            public UniTask ShowAsync(DialogViewModel dialog, CancellationToken ct)
            {
                Dialog = dialog;
                return UniTask.CompletedTask;
            }

            public UniTask CloseAllAsync() => UniTask.CompletedTask;
        }

        private sealed class SpyProfile : IProfileService
        {
            private readonly List<string> _log;

            public SpyProfile(List<string> log) => _log = log;

            public IObservable<Unit> Changed => Observable.Empty<Unit>();

            public void Load()
            {
            }

            public void SaveAll()
            {
            }

            public void ResetProgress() => _log.Add("reset");
        }

        private sealed class StubFlow : IGameFlow
        {
            private readonly List<string> _log;
            private readonly UniTaskCompletionSource _goHome;

            public StubFlow(List<string> log, UniTaskCompletionSource goHome)
            {
                _log = log;
                _goHome = goHome;
            }

            public UniTask GoHomeAsync()
            {
                _log.Add("home");
                return _goHome.Task;
            }

            public UniTask PlayLevelAsync(int level, StartBoosters startBoosters = null) => UniTask.CompletedTask;
        }

        private sealed class StubSettings : ISettingsService
        {
            public bool IsSoundOn => true;

            public bool IsMusicOn => true;

            public bool IsHapticsOn => true;

            public void SetSoundOn(bool isOn)
            {
            }

            public void SetMusicOn(bool isOn)
            {
            }

            public void SetHapticsOn(bool isOn)
            {
            }
        }

        private sealed class StubOnboarding : IOnboardingService
        {
            public bool HasFlag(string flag) => false;

            public void SetFlag(string flag)
            {
            }

            public bool HasSeenTip(string tip) => false;

            public void MarkTipSeen(string tip)
            {
            }
        }

        private sealed class StubNamePrompt : IShopNamePrompt
        {
            public UniTask AskAsync(CancellationToken ct) => UniTask.CompletedTask;
        }

        private sealed class StubOverlay : IOverlayService
        {
            public void Toast(string message, bool isWarning = false)
            {
            }

            public void ToastAbove(string message, VisualElement anchor)
            {
            }

            public void Coach(string message, VisualElement anchor, bool isAbove, float seconds)
            {
            }

            public void PointAt(VisualElement target)
            {
            }

            public void ClearCoach()
            {
            }

            public void Burst(Vector2 panelPosition, EffectKind kind, int count, Color[] colors = null)
            {
            }

            public void Praise(Vector2 panelPosition, string text, bool isCombo)
            {
            }

            public void SetBackground(bool isTricky, bool hasBunting)
            {
            }
        }
    }
}
