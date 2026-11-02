using System.Collections.Generic;

namespace Game.Features.Gameplay.Model
{
    /// <summary>
    /// A shelf in a column: one or more rows of item cells, possibly tied, frozen or nailed shut.
    /// </summary>
    public sealed class Block
    {
        /// <summary>
        /// Unique id within the level; the key that a tied shelf's key, the hammer and the views use.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Height in rows (1..3); the shelf folds only when every cell of every row is empty.
        /// </summary>
        public int Height { get; set; }

        /// <summary>
        /// Item cells, row by row from the bottom, <c>Height * jarsPerRow</c> long; empty cells are null.
        /// </summary>
        public Jar[] Slots { get; set; }

        /// <summary>
        /// Tied with twine: untouchable until the shelf holding its key folds.
        /// </summary>
        public bool IsLocked { get; set; }

        /// <summary>
        /// Id of the tied shelf this shelf's brass key opens when this shelf folds; 0 when it carries no key.
        /// </summary>
        public int KeyTargetId { get; set; }

        /// <summary>
        /// Remaining shipped baskets until the ice melts; 0 means not frozen.
        /// </summary>
        public int Frozen { get; set; }

        /// <summary>
        /// Nailed shut with boards: contents hidden until the hammer is used.
        /// </summary>
        public bool IsBoarded { get; set; }

        /// <summary>
        /// True when the player may tap items on this shelf (the prototype's <c>open(b)</c>).
        /// </summary>
        public bool IsOpen => !IsLocked && Frozen <= 0 && !IsBoarded;

        /// <summary>
        /// Returns true when every cell is empty, i.e. the shelf should fold away.
        /// </summary>
        /// <returns>Whether the shelf has no items left.</returns>
        public bool IsEmpty()
        {
            foreach (var slot in Slots)
            {
                if (slot != null)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
