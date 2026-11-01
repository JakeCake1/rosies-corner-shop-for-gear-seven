using System.Collections.Generic;
using Game.Services.Content;
using UnityEngine;

namespace Game.Services.Levels
{
    /// <summary>
    /// An ordered list of levels ("New · 20" or "Original · 10"); the reviewers panel switches between curves.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Level Curve", fileName = "LevelCurve")]
    public sealed class LevelCurveConfig : ScriptableObject, IValidatableConfig
    {
        [SerializeField] private List<LevelDefinition> _levels = new List<LevelDefinition>();
        [Tooltip("Stable id stored in the save: 'new' or 'orig'.")]
        [SerializeField] private string _id = "new";
        [Tooltip("Label in the reviewers panel, e.g. 'New · 20'.")]
        [SerializeField] private string _label = "New · 20";
        [Tooltip("Home screen subtitle for this curve.")]
        [SerializeField] private string _subtitle = "Prototype · 20 levels · placeholder art";
        [Tooltip("The original curve unlocks every booster at once and skips the album and naming milestones.")]
        [SerializeField] private bool _isOriginal;

        /// <summary>Levels in play order; level n is <c>Levels[n - 1]</c>.</summary>
        public IReadOnlyList<LevelDefinition> Levels => _levels;

        /// <summary>Stable id stored in the save ("new" or "orig").</summary>
        public string Id => _id;

        /// <summary>Label of the curve in the reviewers panel.</summary>
        public string Label => _label;

        /// <summary>Home screen subtitle shown under the shop sign.</summary>
        public string Subtitle => _subtitle;

        /// <summary>True for the original 10-level prototype curve: all boosters unlocked, no meta milestones.</summary>
        public bool IsOriginal => _isOriginal;

        /// <summary>
        /// Replaces the curve content; called by the editor importer only.
        /// </summary>
        /// <param name="id">Stable id.</param>
        /// <param name="label">Reviewers panel label.</param>
        /// <param name="subtitle">Home subtitle.</param>
        /// <param name="isOriginal">Original-curve rules.</param>
        /// <param name="levels">Levels in order.</param>
        public void SetContent(string id, string label, string subtitle, bool isOriginal, List<LevelDefinition> levels)
        {
            _id = id;
            _label = label;
            _subtitle = subtitle;
            _isOriginal = isOriginal;
            _levels = levels;
        }

        /// <inheritdoc/>
        public void Validate(List<string> errors)
        {
            if (string.IsNullOrEmpty(_id)) errors.Add($"{name}: Id is empty.");
            if (_levels.Count == 0) errors.Add($"{name}: no levels.");
            for (var i = 0; i < _levels.Count; i++)
            {
                var l = _levels[i];
                var at = $"{name}: level {i + 1}";
                if (l.JarsPerRow < 2 || l.JarsPerRow > 3) errors.Add($"{at}: JarsPerRow must be 2 or 3, got {l.JarsPerRow}.");
                if (l.VisibleRows < 1) errors.Add($"{at}: VisibleRows must be positive, got {l.VisibleRows}.");
                if (l.PerType % 3 != 0 || l.PerType <= 0) errors.Add($"{at}: PerType must be a positive multiple of 3, got {l.PerType}.");
                if (l.Types < 1) errors.Add($"{at}: Types must be positive, got {l.Types}.");
                if (l.Sizes == null || l.Sizes.Length == 0) errors.Add($"{at}: Sizes is empty.");
                else
                {
                    foreach (var s in l.Sizes)
                    {
                        if (s < 1 || s > l.VisibleRows) errors.Add($"{at}: shelf size {s} doesn't fit {l.VisibleRows} rows.");
                    }
                }

                if (l.Wrap < 0f || l.Wrap > 1f) errors.Add($"{at}: Wrap must be in 0..1, got {l.Wrap}.");
                if (l.Deep < 0f || l.Deep > 1f) errors.Add($"{at}: Deep must be in 0..1, got {l.Deep}.");
                if (l.Boarded > 1) errors.Add($"{at}: at most one nailed shelf, got {l.Boarded}.");
                if (l.Frozen > 0 && l.Thaw <= 0) errors.Add($"{at}: Thaw must be positive with frozen shelves.");
            }
        }
    }
}
