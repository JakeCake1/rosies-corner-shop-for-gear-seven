using System.Collections.Generic;
using Game.Services.Content;
using UnityEngine;

namespace Game.Services.Balance
{
    /// <summary>
    /// Economy and progression tuning: prices, pack sizes, gifts, rewards and the levels where boosters, ads, the
    /// album and other milestones appear. Defaults are the prototype values.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Game Balance", fileName = "GameBalance")]
    public sealed class GameBalanceConfig : ScriptableObject, IValidatableConfig
    {
        [Header("Economy")]
        [Tooltip("Free undos at the start of every level.")]
        [SerializeField] private int _freeUndosPerLevel = 2;
        [Tooltip("Coin price of the undo pack.")]
        [SerializeField] private int _undoPackPrice = 60;
        [Tooltip("Undos in the coin pack.")]
        [SerializeField] private int _undoPackSize = 2;
        [Tooltip("Coin price of one Tidy Up, Quick Pick or Shuffle.")]
        [SerializeField] private int _boosterPrice = 100;
        [Tooltip("Coin price of opening the third basket for a level.")]
        [SerializeField] private int _extraBasketPrice = 150;
        [Tooltip("Boosters Rosie gives when a booster unlocks.")]
        [SerializeField] private int _boosterGift = 2;
        [Tooltip("Coins per star; tricky levels pay double.")]
        [SerializeField] private int _coinsPerStar = 10;
        [Tooltip("Counter capacity after the '+1 counter spot' offer.")]
        [SerializeField] private int _extendedCapacity = 6;

        [Header("Progression")]
        [Tooltip("Level where Undo appears.")]
        [SerializeField] private int _undoUnlockLevel = 2;
        [Tooltip("Level where Tidy Up appears.")]
        [SerializeField] private int _tidyUnlockLevel = 4;
        [Tooltip("Level where Quick Pick appears.")]
        [SerializeField] private int _pickUnlockLevel = 6;
        [Tooltip("Level from which the '+' extra basket slot is shown; its coach tip plays on this level.")]
        [SerializeField] private int _extraBasketSlotLevel = 3;
        [Tooltip("Level from which rewarded-ad buttons appear.")]
        [SerializeField] private int _adsFromLevel = 6;
        [Tooltip("Level from which the start-boosters popup appears (new curve only).")]
        [SerializeField] private int _startBoostersFromLevel = 11;
        [Tooltip("Completing this level opens the album.")]
        [SerializeField] private int _albumFromLevel = 9;
        [Tooltip("After completing this level the player names the shop.")]
        [SerializeField] private int _nameShopAfterLevel = 10;

        [Header("Coaching")]
        [Tooltip("Last level with coach tips and the always-on hidden hint.")]
        [SerializeField] private int _coachTipsUntilLevel = 4;
        [Tooltip("Last level where the idle hint works without a previous loss.")]
        [SerializeField] private int _hintUntilLevel = 5;
        [Tooltip("Idle time before the hidden hint highlights a move, in seconds.")]
        [SerializeField] private float _hintIdleSeconds = 6f;

        /// <summary>Free undos at the start of every level.</summary>
        public int FreeUndosPerLevel => _freeUndosPerLevel;

        /// <summary>Coin price of the undo pack.</summary>
        public int UndoPackPrice => _undoPackPrice;

        /// <summary>Undos in the coin pack.</summary>
        public int UndoPackSize => _undoPackSize;

        /// <summary>Coin price of one Tidy Up, Quick Pick or Shuffle.</summary>
        public int BoosterPrice => _boosterPrice;

        /// <summary>Coin price of opening the third basket for a level.</summary>
        public int ExtraBasketPrice => _extraBasketPrice;

        /// <summary>Boosters Rosie gives when a booster unlocks.</summary>
        public int BoosterGift => _boosterGift;

        /// <summary>Coins per star; tricky levels pay double.</summary>
        public int CoinsPerStar => _coinsPerStar;

        /// <summary>Counter capacity after the "+1 counter spot" offer.</summary>
        public int ExtendedCapacity => _extendedCapacity;

        /// <summary>Level where Undo appears.</summary>
        public int UndoUnlockLevel => _undoUnlockLevel;

        /// <summary>Level where Tidy Up appears.</summary>
        public int TidyUnlockLevel => _tidyUnlockLevel;

        /// <summary>Level where Quick Pick appears.</summary>
        public int PickUnlockLevel => _pickUnlockLevel;

        /// <summary>Level from which the "+" extra basket slot is shown.</summary>
        public int ExtraBasketSlotLevel => _extraBasketSlotLevel;

        /// <summary>Level from which rewarded-ad buttons appear.</summary>
        public int AdsFromLevel => _adsFromLevel;

        /// <summary>Level from which the start-boosters popup appears (new curve only).</summary>
        public int StartBoostersFromLevel => _startBoostersFromLevel;

        /// <summary>Completing this level opens the album.</summary>
        public int AlbumFromLevel => _albumFromLevel;

        /// <summary>After completing this level the player names the shop.</summary>
        public int NameShopAfterLevel => _nameShopAfterLevel;

        /// <summary>Last level with coach tips and the always-on hidden hint.</summary>
        public int CoachTipsUntilLevel => _coachTipsUntilLevel;

        /// <summary>Last level where the idle hint works without a previous loss.</summary>
        public int HintUntilLevel => _hintUntilLevel;

        /// <summary>Idle time before the hidden hint highlights a move, in seconds.</summary>
        public float HintIdleSeconds => _hintIdleSeconds;

        /// <inheritdoc/>
        public void Validate(List<string> errors)
        {
            RequireAtLeast(errors, nameof(_freeUndosPerLevel), _freeUndosPerLevel, 0);
            RequireAtLeast(errors, nameof(_undoPackPrice), _undoPackPrice, 0);
            RequireAtLeast(errors, nameof(_undoPackSize), _undoPackSize, 1);
            RequireAtLeast(errors, nameof(_boosterPrice), _boosterPrice, 0);
            RequireAtLeast(errors, nameof(_extraBasketPrice), _extraBasketPrice, 0);
            RequireAtLeast(errors, nameof(_boosterGift), _boosterGift, 0);
            RequireAtLeast(errors, nameof(_coinsPerStar), _coinsPerStar, 0);
            RequireAtLeast(errors, nameof(_extendedCapacity), _extendedCapacity, 1);
            RequireAtLeast(errors, nameof(_undoUnlockLevel), _undoUnlockLevel, 1);
            RequireAtLeast(errors, nameof(_tidyUnlockLevel), _tidyUnlockLevel, 1);
            RequireAtLeast(errors, nameof(_pickUnlockLevel), _pickUnlockLevel, 1);
            RequireAtLeast(errors, nameof(_extraBasketSlotLevel), _extraBasketSlotLevel, 1);
            RequireAtLeast(errors, nameof(_adsFromLevel), _adsFromLevel, 1);
            RequireAtLeast(errors, nameof(_startBoostersFromLevel), _startBoostersFromLevel, 1);
            RequireAtLeast(errors, nameof(_albumFromLevel), _albumFromLevel, 1);
            RequireAtLeast(errors, nameof(_nameShopAfterLevel), _nameShopAfterLevel, 1);
            RequireAtLeast(errors, nameof(_coachTipsUntilLevel), _coachTipsUntilLevel, 0);
            RequireAtLeast(errors, nameof(_hintUntilLevel), _hintUntilLevel, 0);
            if (_hintIdleSeconds <= 0f)
            {
                errors.Add($"{name}: {nameof(_hintIdleSeconds)} must be positive, got {_hintIdleSeconds}");
            }
        }

        private void RequireAtLeast(List<string> errors, string field, int value, int min)
        {
            if (value < min)
            {
                errors.Add($"{name}: {field} must be at least {min}, got {value}");
            }
        }
    }
}
