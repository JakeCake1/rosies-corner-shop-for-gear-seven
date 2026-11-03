using Game.Services.Haptics;

namespace Game.Tests.EditMode.Features.Gameplay.Session
{
    internal sealed class FakeHapticsService : IHapticsService
    {
        public void Pulse(HapticKind kind)
        {
        }
    }
}
