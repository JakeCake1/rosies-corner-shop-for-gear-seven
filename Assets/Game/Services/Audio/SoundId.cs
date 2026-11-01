namespace Game.Services.Audio
{
    /// <summary>
    /// Sound effects of the game; all are synthesized at startup like the prototype's Web Audio synth.
    /// </summary>
    public enum SoundId
    {
        /// <summary>Tap on a box-shaped item: dull knock.</summary>
        TapBox,

        /// <summary>Tap on a can: metallic.</summary>
        TapCan,

        /// <summary>Tap on a bottle: glassy ring.</summary>
        TapBottle,

        /// <summary>Tap on a stick-shaped item: click.</summary>
        TapStick,

        /// <summary>Tap on a round item: soft.</summary>
        TapRound,

        /// <summary>Quiet tap for the counter, a paper bag or the hammer.</summary>
        TapSoft,

        /// <summary>Item lands in a basket.</summary>
        Clink,

        /// <summary>Basket ships; pitch rises with the combo.</summary>
        Ship,

        /// <summary>Empty shelf folds away.</summary>
        Fold,

        /// <summary>New shelf rolls in.</summary>
        Roll,

        /// <summary>Key cuts the twine / boards come off.</summary>
        Snip,

        /// <summary>Booster used, ice thawed.</summary>
        Magic,

        /// <summary>Coins gained or spent.</summary>
        Coin,

        /// <summary>Level complete fanfare.</summary>
        Win,

        /// <summary>Refused move or lost level.</summary>
        Fail,
    }
}
