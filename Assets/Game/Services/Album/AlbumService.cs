using System.Collections.Generic;
using System.Linq;
using Game.Services.Content;
using Game.Services.Save;
using Game.Services.Wallet;

namespace Game.Services.Album
{
    /// <summary>
    /// Default <see cref="IAlbumService"/>.
    /// </summary>
    public sealed class AlbumService : ProfileChunk<AlbumData>, IAlbumService
    {
        /// <summary>
        /// Coins paid once when every item of a department is in the album.
        /// </summary>
        public const int FamilyBonus = 50;

        private readonly IContentService _content;
        private readonly IWalletService _wallet;

        /// <inheritdoc/>
        public override string Key => "album";

        /// <inheritdoc/>
        public IReadOnlyList<int> Found => Data.Found;

        /// <inheritdoc/>
        public int NewCount => Data.NewCount;

        /// <summary>
        /// Creates the service.
        /// </summary>
        /// <param name="content">Item catalog, for department completion.</param>
        /// <param name="wallet">Receives the department bonus.</param>
        public AlbumService(IContentService content, IWalletService wallet)
        {
            _content = content;
            _wallet = wallet;
        }

        /// <inheritdoc/>
        public bool IsFamilyCompleted(string family) => Data.CompletedFamilies.Contains(family);

        /// <inheritdoc/>
        public string RegisterFound(int itemId)
        {
            if (Data.Found.Contains(itemId))
            {
                return null;
            }

            Data.Found.Add(itemId);
            Data.NewCount++;
            var family = _content.Catalog.Get(itemId).Family;
            var isCompleted = !Data.CompletedFamilies.Contains(family) &&
                              _content.Catalog.Items.Where(i => i.Family == family).All(i => Data.Found.Contains(i.Id));
            if (!isCompleted)
            {
                MarkChanged();
                return null;
            }

            // The wallet is saved before the family is marked done: a process kill in between repeats the bonus
            // check on the next find instead of losing the bonus for good.
            _wallet.Add(FamilyBonus);
            Data.CompletedFamilies.Add(family);
            MarkChanged();
            return family;
        }

        /// <inheritdoc/>
        public void MarkSeen()
        {
            if (Data.NewCount == 0)
            {
                return;
            }

            Data.NewCount = 0;
            MarkChanged();
        }
    }
}
