using Game.Services.UI.Elements;
using UnityEngine.UIElements;

namespace Game.Services.UI.Dialogs
{
    /// <summary>
    /// One dialog button: wooden/berry/green/gold background, label, optional sub label, ad or coin badge.
    /// </summary>
    public sealed class DialogButtonView : ElementView
    {
        private const string ButtonClass = "dialog-button";
        private const string SmallClass = "dialog-button--small";
        private const string TallClass = "dialog-button--tall";
        private const string ContentClass = "dialog-button__content";
        private const string LabelsClass = "dialog-button__labels";
        private const string LabelClass = "dialog-button__label";
        private const string SubClass = "dialog-button__sub";
        private const string AdClass = "dialog-button__ad";
        private const string AdTextClass = "dialog-button__ad-text";
        private const string CoinClass = "dialog-button__coin";
        private const string CoinIconClass = "dialog-button__coin-icon";
        private const string PriceClass = "dialog-button__price";

        private static readonly string[] StyleClasses = { "kit-btn-berry", "kit-btn-green", "kit-btn-wood", "kit-btn-gold", "kit-btn-wood" };

        /// <summary>The button element.</summary>
        public Button Button { get; private set; }

        /// <summary>
        /// Creates the button for a model.
        /// </summary>
        /// <param name="model">Button model.</param>
        /// <returns>The view (its <see cref="ElementView.Root"/> is the button).</returns>
        public static DialogButtonView Create(DialogButton model)
        {
            var view = new DialogButtonView();
            view.Attach(new Button());
            view.Setup(model);
            return view;
        }

        /// <inheritdoc/>
        protected override void OnAttached()
        {
            Button = (Button)Root;
            Button.AddToClassList(ButtonClass);
            Button.AddToClassList("btn");
        }

        private void Setup(DialogButton model)
        {
            var hasSub = !string.IsNullOrEmpty(model.SubLabel);
            Button.AddToClassList(StyleClasses[(int)model.Style]);
            Button.EnableInClassList(SmallClass, model.Style == ButtonStyle.WoodSmall);
            Button.EnableInClassList(TallClass, hasSub);

            var content = CreateElement(ContentClass);
            content.pickingMode = PickingMode.Ignore;
            Button.Add(content);
            if (model.Badge == ButtonBadge.Ad)
            {
                var ad = CreateElement(AdClass, "kit-rounded-white");
                ad.Add(CreateLabel("AD", AdTextClass));
                content.Add(ad);
            }
            else if (model.Badge == ButtonBadge.Coins)
            {
                var coin = CreateElement(CoinClass);
                coin.Add(CreateElement(CoinIconClass, "icon-coin"));
                coin.Add(CreateLabel(model.Price.ToString(), PriceClass));
                content.Add(coin);
            }

            var labels = CreateElement(LabelsClass);
            labels.Add(CreateLabel(model.Label, LabelClass));
            if (hasSub) labels.Add(CreateLabel(model.SubLabel, SubClass));
            content.Add(labels);
        }
    }
}
