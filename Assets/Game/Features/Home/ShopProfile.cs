using Game.Services.Save;

namespace Game.Features.Home
{
    /// <summary>
    /// Default <see cref="IShopProfile"/>.
    /// </summary>
    public sealed class ShopProfile : ProfileChunk<ShopProfileData>, IShopProfile
    {
        /// <inheritdoc/>
        public override string Key => "shop";

        /// <inheritdoc/>
        public string Name => Data.ShopName;

        /// <inheritdoc/>
        public void Rename(string name)
        {
            Data.ShopName = name;
            MarkChanged();
        }
    }
}
