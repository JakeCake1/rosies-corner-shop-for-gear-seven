namespace Game.Features.Gameplay.Session
{
    /// <summary>
    /// A full basket starts shipping: the view covers it with the napkin, bursts sparks and floats the praise.
    /// </summary>
    public readonly struct ShipNotice
    {
        /// <summary>
        /// Creates a notice.
        /// </summary>
        /// <param name="slot">Basket slot (0..2).</param>
        /// <param name="basketId">Basket id, to find its view.</param>
        /// <param name="combo">Baskets in a row without a counter drop; 0 for the first.</param>
        /// <param name="word">Praise or combo word to show.</param>
        public ShipNotice(int slot, int basketId, int combo, string word)
        {
            Slot = slot;
            BasketId = basketId;
            Combo = combo;
            Word = word;
        }

        /// <summary>Basket slot (0..2).</summary>
        public int Slot { get; }

        /// <summary>Basket id.</summary>
        public int BasketId { get; }

        /// <summary>Combo count; more sparks and a higher pitch as it grows.</summary>
        public int Combo { get; }

        /// <summary>Word to float up ("Lovely!" or a combo word).</summary>
        public string Word { get; }
    }
}
