using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Services.UI.Dialogs
{
    /// <summary>
    /// A pressable dialog button: background by style, label, optional second line and "AD"/coin badge.
    /// </summary>
    public sealed class DialogButtonView : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private Image _background;
        [SerializeField] private TMP_Text _label;
        [SerializeField] private TMP_Text _subLabel;
        [SerializeField] private GameObject _adBadge;
        [SerializeField] private GameObject _coinBadge;
        [SerializeField] private TMP_Text _price;
        [SerializeField] private LayoutElement _layout;
        [SerializeField] private float _height = 58f;
        [SerializeField] private float _smallHeight = 44f;
        [SerializeField] private Color _lightText = Color.white;
        [SerializeField] private Color _darkText = new Color(0.23f, 0.16f, 0.11f);

        /// <summary>The button; the dialog subscribes to its clicks.</summary>
        public Button Button => _button;

        /// <summary>
        /// Shows <paramref name="model"/>.
        /// </summary>
        /// <param name="model">Button content.</param>
        /// <param name="background">Sprite for the button style.</param>
        public void Setup(DialogButton model, Sprite background)
        {
            _background.sprite = background;
            _label.SetText(model.Label);
            var hasSub = !string.IsNullOrEmpty(model.SubLabel);
            _subLabel.gameObject.SetActive(hasSub);
            if (hasSub) _subLabel.SetText(model.SubLabel);
            _adBadge.SetActive(model.Badge == ButtonBadge.Ad);
            _coinBadge.SetActive(model.Badge == ButtonBadge.Coins);
            if (model.Badge == ButtonBadge.Coins) _price.SetText("{0}", model.Price);
            var isSmall = model.Style == ButtonStyle.WoodSmall;
            _layout.preferredHeight = isSmall ? _smallHeight : hasSub ? _height + 12f : _height;
            _label.fontSize = isSmall ? 18f : 22f;
            var isLight = model.Style == ButtonStyle.Berry || model.Style == ButtonStyle.Green;
            _label.color = isLight ? _lightText : _darkText;
            _subLabel.color = _label.color;
        }
    }
}
