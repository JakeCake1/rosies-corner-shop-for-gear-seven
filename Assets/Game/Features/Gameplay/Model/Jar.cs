using System.Collections.Generic;

namespace Game.Features.Gameplay.Model
{
    /// <summary>
    /// One grocery item (or the hammer tool) on a shelf, on the counter or in a basket.
    /// </summary>
    /// <remarks>
    /// Identity matters: rules compare jars by reference (as the prototype does), so never copy a jar to "update" it
    /// unless the prototype also replaced the object (wrapping in a paper bag, the second row).
    /// </remarks>
    public sealed class Jar
    {
        /// <summary>
        /// Type id of the hammer tool; real item types are catalog ids (0..161).
        /// </summary>
        public const int HammerType = -1;

        /// <summary>
        /// Unique id within the level; views key their item objects by it.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Catalog item id, or <see cref="HammerType"/> for the hammer.
        /// </summary>
        public int Type { get; set; }

        /// <summary>
        /// True for the hammer that opens the nailed shelf; tools never go to baskets or the counter.
        /// </summary>
        public bool IsTool { get; set; }

        /// <summary>
        /// Block id of the nailed shelf this hammer opens; 0 for items.
        /// </summary>
        public int TargetBlockId { get; set; }

        /// <summary>
        /// True while the item is hidden in a paper bag; taking any item from the same row unwraps it.
        /// </summary>
        public bool IsWrapped { get; set; }

        /// <summary>
        /// The faded item in the second row behind this one; it steps forward when this one is taken.
        /// </summary>
        public Jar Back { get; set; }

        /// <summary>
        /// Copies the jar (and its second-row item) for an undo snapshot or a prototype-style object spread.
        /// </summary>
        /// <returns>A new jar with the same field values.</returns>
        public Jar Clone() => new Jar
        {
            Id = Id, Type = Type, IsTool = IsTool, TargetBlockId = TargetBlockId, IsWrapped = IsWrapped,
            Back = Back?.Clone(),
        };

        /// <summary>
        /// Copies the jar keeping the same second-row object, like the prototype's <c>{...jar}</c> spread; used
        /// when an item is wrapped in a bag or gets a second row as its shelf is cut.
        /// </summary>
        /// <returns>A new jar sharing <see cref="Back"/> with this one.</returns>
        public Jar CloneShallow() => (Jar)MemberwiseClone();
    }
}
