namespace Game.Features.Gameplay.Session
{
    /// <summary>
    /// A full basket starts shipping: the view covers it with the napkin, bursts sparks and floats the praise.
    /// </summary>
    public readonly struct ShipNotice
    {
        /// <summary>Basket slot (0..2).</summary>
        public int Slot { get; }

        /// <summary>Basket id.</summary>
        public int BasketId { get; }

        /// <summary>Combo count; more sparks and a higher pitch as it grows.</summary>
        public int Combo { get; }

        /// <summary>
        /// UI string table key of the word to float up ("Lovely!" or a combo word); the board view resolves it.
        /// </summary>
        public string WordKey { get; }

        /// <summary>
        /// Creates a notice.
        /// </summary>
        /// <param name="slot">Basket slot (0..2).</param>
        /// <param name="basketId">Basket id, to find its view.</param>
        /// <param name="combo">Baskets in a row without a counter drop; 0 for the first.</param>
        /// <param name="wordKey">UI string table key of the praise or combo word to show.</param>
        public ShipNotice(int slot, int basketId, int combo, string wordKey)
        {
            Slot = slot;
            BasketId = basketId;
            Combo = combo;
            WordKey = wordKey;
        }
    }
}
