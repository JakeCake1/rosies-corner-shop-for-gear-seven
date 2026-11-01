using Game.Services.Save;

namespace Game.Services.Experiments
{
    /// <summary>
    /// Default <see cref="IExperimentsService"/>.
    /// </summary>
    public sealed class ExperimentsService : ProfileChunk<ExperimentsData>, IExperimentsService
    {
        /// <inheritdoc/>
        public override string Key => "experiments";

        /// <inheritdoc/>
        public override bool IsKeptOnReset => true;

        /// <inheritdoc/>
        public string Curve => Data.Curve;

        /// <inheritdoc/>
        public string LossOffer => Data.LossOffer;

        /// <inheritdoc/>
        public void SetCurve(string id)
        {
            Data.Curve = id;
            MarkChanged();
        }

        /// <inheritdoc/>
        public void SetLossOffer(string offer)
        {
            Data.LossOffer = offer;
            MarkChanged();
        }
    }
}
