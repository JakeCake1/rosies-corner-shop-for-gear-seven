namespace Game.Services.Experiments
{
    /// <summary>
    /// Variants a reviewer can switch (level curve, rewarded offer after a loss); owns the <c>experiments</c>
    /// profile chunk, which survives "reset progress".
    /// </summary>
    public interface IExperimentsService
    {
        /// <summary>
        /// Active level curve id ("new" or "orig"); an unknown id falls back to the first curve.
        /// </summary>
        string Curve { get; }

        /// <summary>
        /// Rewarded offer after a loss: "basket" (+1 customer) or "slot" (+1 counter spot).
        /// </summary>
        string LossOffer { get; }

        /// <summary>
        /// Selects the level curve; use <c>ILevelCurveService.SwitchCurve</c>, which also clears the stars.
        /// </summary>
        /// <param name="id">Curve id.</param>
        void SetCurve(string id);

        /// <summary>
        /// Selects the rewarded offer variant.
        /// </summary>
        /// <param name="offer">"basket" or "slot".</param>
        void SetLossOffer(string offer);
    }
}
