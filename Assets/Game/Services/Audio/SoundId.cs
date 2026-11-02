namespace Game.Services.Audio
{
    /// <summary>
    /// Sound effects of the game; each maps to one clip in <see cref="AudioLibrary"/>. The basket ship sound is not
    /// listed: it has one clip per combo step and is played by <see cref="IAudioService.PlayShip"/>.
    /// </summary>
    public enum SoundId
    {
        /// <summary>Tap on a box-shaped item: dull knock.</summary>
        TapBox,

        /// <summary>Tap on a can: metallic.</summary>
        TapCan,

        /// <summary>Tap on a bottle: glassy ring.</summary>
        TapBottle,

        /// <summary>Tap on a stick-shaped item (popsicle, lollipop, baguette): click.</summary>
        TapStick,

        /// <summary>Tap on a round item, the hammer or a paper bag: soft thud.</summary>
        TapRound,

        /// <summary>Item lands in a basket; also confirms a settings toggle or a start booster pick.</summary>
        Clink,

        /// <summary>Item lands on the counter.</summary>
        Counter,

        /// <summary>Empty shelf folds away; boards come off a nailed shelf.</summary>
        Fold,

        /// <summary>New shelf drops in from the stockroom, and the opening shelves at level start.</summary>
        Roll,

        /// <summary>Key cuts the twine; hammer pries the boards.</summary>
        Snip,

        /// <summary>Booster used, undo, extra basket, frozen shelf thaws.</summary>
        Magic,

        /// <summary>Coins spent or earned, each star on the win screen.</summary>
        Coin,

        /// <summary>Level complete, album intro, shop renamed.</summary>
        Win,

        /// <summary>Level lost, or a tap refused because the counter is full.</summary>
        Fail,
    }
}
