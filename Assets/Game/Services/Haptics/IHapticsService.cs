namespace Game.Services.Haptics
{
    /// <summary>
    /// Short vibrations on taps, shipments, wins and losses (Android only, like the prototype).
    /// </summary>
    public interface IHapticsService
    {
        /// <summary>
        /// Vibrates once if vibration is on and the device supports it.
        /// </summary>
        /// <param name="kind">What happened; decides the strength where the platform allows.</param>
        void Pulse(HapticKind kind);
    }
}
