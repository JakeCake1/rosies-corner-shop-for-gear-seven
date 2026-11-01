namespace Game.Services.Haptics
{
    /// <summary>
    /// Haptic moments, matching the prototype's vibration patterns.
    /// </summary>
    public enum HapticKind
    {
        /// <summary>Item tapped.</summary>
        Tap,

        /// <summary>Basket shipped.</summary>
        Ship,

        /// <summary>Level won.</summary>
        Win,

        /// <summary>Move refused or level lost.</summary>
        Fail,
    }
}
