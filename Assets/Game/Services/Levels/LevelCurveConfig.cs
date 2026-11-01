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
        [Tooltip("UI string table key of the label in the reviewers panel, e.g. curve.new.label ('New · 20').")]
        [SerializeField] private string _labelKey = string.Empty;
        [Tooltip("UI string table key of the home screen subtitle for this curve, e.g. curve.new.subtitle.")]
        [SerializeField] private string _subtitleKey = string.Empty;
        [Tooltip("The original curve unlocks every booster at once and skips the album and naming milestones.")]
        [SerializeField] private bool _isOriginal;

        /// <summary>Levels in play order; level n is <c>Levels[n - 1]</c>.</summary>
        public IReadOnlyList<LevelDefinition> Levels => _levels;

        /// <summary>Stable id stored in the save ("new" or "orig").</summary>
        public string Id => _id;

        /// <summary>UI string table key of the curve's label in the reviewers panel ("New · 20").</summary>
        public string LabelKey => _labelKey;

        /// <summary>UI string table key of the home screen subtitle shown under the shop sign.</summary>
        public string SubtitleKey => _subtitleKey;

        /// <summary>True for the original 10-level prototype curve: all boosters unlocked, no meta milestones.</summary>
        public bool IsOriginal => _isOriginal;

        /// <summary>
        /// Replaces the curve content; used by tests to build a curve without the asset.
        /// </summary>
        /// <param name="id">Stable id.</param>
        /// <param name="labelKey">UI table key of the reviewers panel label.</param>
        /// <param name="subtitleKey">UI table key of the home subtitle.</param>
        /// <param name="isOriginal">Original-curve rules.</param>
        /// <param name="levels">Levels in order.</param>
        public void SetContent(string id, string labelKey, string subtitleKey, bool isOriginal,
            List<LevelDefinition> levels)
        {
            _id = id;
            _labelKey = labelKey;
            _subtitleKey = subtitleKey;
            _isOriginal = isOriginal;
            _levels = levels;
        }

        /// <inheritdoc/>
        public void Validate(List<string> errors)
        {
            if (string.IsNullOrEmpty(_id)) errors.Add($"{name}: Id is empty.");
            if (string.IsNullOrEmpty(_labelKey)) errors.Add($"{name}: LabelKey is empty.");
            if (string.IsNullOrEmpty(_subtitleKey)) errors.Add($"{name}: SubtitleKey is empty.");
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
