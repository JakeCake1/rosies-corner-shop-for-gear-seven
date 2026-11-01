using System.Collections.Generic;

namespace Game.Features.Gameplay.Model
{
    /// <summary>
    /// A customer's basket that wants three items of one type.
    /// </summary>
    public sealed class Basket
    {
        /// <summary>
        /// Basket capacity; every order is three of a kind.
        /// </summary>
        public const int Capacity = 3;

        /// <summary>
        /// Unique id within the level; a new id means a new customer (views animate it in).
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Wanted item type.
        /// </summary>
        public int Type { get; set; }

        /// <summary>
        /// Items already packed (0..3).
        /// </summary>
        public List<Jar> Jars { get; } = new List<Jar>();

        /// <summary>
        /// Full and waiting to ship; ships after a short presentation delay owned by the session.
        /// </summary>
        public bool IsDone { get; set; }
    }
}
