using Game.Services.Haptics;
using Game.Services.Settings;
using NUnit.Framework;

namespace Game.Tests.EditMode.Services.Haptics
{
    // The vibration itself (Handheld.Vibrate) is compiled only for Android players, so the Editor can only
    // check that pulsing is safe for every kind and setting.
    public sealed class HapticsServiceTests
    {
        [TestCase(HapticKind.Tap)]
        [TestCase(HapticKind.Ship)]
        [TestCase(HapticKind.Win)]
        [TestCase(HapticKind.Fail)]
        public void Pulse_WhenHapticsOn_DoesNotThrow(HapticKind kind)
        {
            var service = new HapticsService(new StubSettingsService { IsHapticsOn = true });

            Assert.DoesNotThrow(() => service.Pulse(kind));
        }

        [TestCase(HapticKind.Tap)]
        [TestCase(HapticKind.Win)]
        public void Pulse_WhenHapticsOff_DoesNotThrow(HapticKind kind)
        {
            var service = new HapticsService(new StubSettingsService { IsHapticsOn = false });

            Assert.DoesNotThrow(() => service.Pulse(kind));
        }

        private sealed class StubSettingsService : ISettingsService
        {
            public bool IsSoundOn { get; set; }

            public bool IsMusicOn { get; set; }

            public bool IsHapticsOn { get; set; }

            public void SetSoundOn(bool isOn)
            {
                IsSoundOn = isOn;
            }

            public void SetMusicOn(bool isOn)
            {
                IsMusicOn = isOn;
            }

            public void SetHapticsOn(bool isOn)
            {
                IsHapticsOn = isOn;
            }
        }
    }
}
