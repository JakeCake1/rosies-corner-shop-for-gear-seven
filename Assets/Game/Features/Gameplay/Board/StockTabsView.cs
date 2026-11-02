using System.Linq;
using Game.Features.Gameplay.Model;
using Game.Services.Localization;
using Game.Services.UI.Elements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Features.Gameplay.Board
{
    /// <summary>
    /// The stock tabs above the shelf columns: how many items wait in each column's stock and whether a hammer is
    /// hidden there. Items arrive from and return to the tab of their column.
    /// </summary>
    public sealed class StockTabsView : ElementView
    {
        private const string TextClass = "stock-tab__text";
        private const string HammerClass = "stock-tab__hammer";

        private readonly Label[] _labels = new Label[LevelState.ColumnCount];
        private readonly VisualElement[] _hammers = new VisualElement[LevelState.ColumnCount];
        private readonly ILocalizationService _texts;

        /// <summary>
        /// Creates the tabs; attach them to the cabinet's stock tabs row.
        /// </summary>
        /// <param name="texts">Stock counts.</param>
        public StockTabsView(ILocalizationService texts)
        {
            _texts = texts;
        }

        /// <summary>
        /// Shows the stock count and hammer mark of a column.
        /// </summary>
        /// <param name="index">Column index.</param>
        /// <param name="column">The column of the level state.</param>
        public void Render(int index, Model.Column column)
        {
            var hasHammer = column.Queue.Any(j => j.IsTool);
            var hasTiedHammer = column.Blocks.Any(b => b.IsLocked && b.Slots.Any(j => j != null && j.IsTool));
            var stock = column.Queue.Count - (hasHammer ? 1 : 0);
            _labels[index].text = stock > 0
                ? _texts.Format(GameplayTexts.Stock, stock)
                : _texts.Get(GameplayTexts.StockEmpty);
            _hammers[index].SetVisible(hasHammer || hasTiedHammer);
        }

        /// <summary>
        /// Returns the center of a column's tab, where items come from and go back to.
        /// </summary>
        /// <param name="index">Column index.</param>
        /// <returns>Panel coordinates.</returns>
        public Vector2 Center(int index) => _labels[index].worldBound.center;

        /// <inheritdoc/>
        protected override void OnAttached()
        {
            var labels = Root.Query<Label>(className: TextClass).ToList();
            var hammers = Root.Query(className: HammerClass).ToList();
            for (var c = 0; c < LevelState.ColumnCount; c++)
            {
                _labels[c] = labels[c];
                _hammers[c] = hammers[c];
            }
        }
    }
}
