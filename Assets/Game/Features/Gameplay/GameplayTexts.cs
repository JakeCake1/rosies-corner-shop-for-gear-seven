namespace Game.Features.Gameplay
{
    /// <summary>
    /// Keys of the gameplay screen, booster, tip, toast and level dialog texts in the <c>UI</c> string table.
    /// </summary>
    /// <remarks>
    /// Every key must exist in the table; <c>LocalizationKeysTests</c> checks it. English text lives in the
    /// table (<c>Services/Localization/Tables</c>), the summaries quote it so call sites stay readable.
    /// </remarks>
    internal static class GameplayTexts
    {
        /// <summary>English: "COUNTER".</summary>
        internal const string Counter = "gameplay.counter";

        /// <summary>
        /// English: "&lt;b&gt;{0}&lt;/b&gt; in stock". Arguments: {0} items left in the column's stock.
        /// </summary>
        internal const string Stock = "gameplay.stock";

        /// <summary>English: "all out".</summary>
        internal const string StockEmpty = "gameplay.stock_empty";

        /// <summary>English: "{0} rows". Arguments: {0} rows of a tall shelf (2 or more).</summary>
        internal const string ShelfRows = "gameplay.shelf_rows";

        /// <summary>English: "LEVEL".</summary>
        internal const string BadgeLevel = "gameplay.badge_level";

        /// <summary>English: "TRICKY".</summary>
        internal const string BadgeTricky = "gameplay.badge_tricky";

        /// <summary>English: "Level {0}". Arguments: {0} level number.</summary>
        internal const string BannerLevel = "gameplay.banner_level";

        /// <summary>English: "Tricky Level".</summary>
        internal const string BannerTricky = "gameplay.banner_tricky";

        /// <summary>English: "The hammer is in the left column's stock. Dig there first!".</summary>
        internal const string HammerStockLeft = "gameplay.hammer_stock_left";

        /// <summary>English: "The hammer is in the middle column's stock. Dig there first!".</summary>
        internal const string HammerStockMiddle = "gameplay.hammer_stock_middle";

        /// <summary>English: "The hammer is in the right column's stock. Dig there first!".</summary>
        internal const string HammerStockRight = "gameplay.hammer_stock_right";

        /// <summary>English: "The hammer is tied up in the left column. Find the key first!".</summary>
        internal const string HammerTiedLeft = "gameplay.hammer_tied_left";

        /// <summary>English: "The hammer is tied up in the middle column. Find the key first!".</summary>
        internal const string HammerTiedMiddle = "gameplay.hammer_tied_middle";

        /// <summary>English: "The hammer is tied up in the right column. Find the key first!".</summary>
        internal const string HammerTiedRight = "gameplay.hammer_tied_right";

        /// <summary>English: "Tap the hammer to open the nailed shelf.".</summary>
        internal const string HammerShelf = "gameplay.hammer_shelf";

        /// <summary>
        /// English: "No basket wants this one yet, so it waits on the counter. If the counter fills up, you're stuck!".
        /// </summary>
        internal const string TipCounter = "gameplay.tip_counter";

        /// <summary>English: "Basket full! It's on its way, and a new order arrives.".</summary>
        internal const string TipShip = "gameplay.tip_ship";

        /// <summary>English: "The labels show how many items are still in the stock behind each column.".</summary>
        internal const string TipStock = "gameplay.tip_stock";

        /// <summary>
        /// English: "New: &lt;b&gt;Undo&lt;/b&gt;. Took the wrong item? Take it back. Two free per level.".
        /// </summary>
        internal const string GiftUndo = "gameplay.gift_undo";

        /// <summary>
        /// English: "A gift from Rosie: &lt;b&gt;two Tidy Ups&lt;/b&gt;. Each clears the counter when it gets
        /// crowded.".
        /// </summary>
        internal const string GiftTidy = "gameplay.gift_tidy";

        /// <summary>
        /// English: "A gift from Rosie: &lt;b&gt;two Quick Picks&lt;/b&gt;. Each fills a basket for you.".
        /// </summary>
        internal const string GiftPick = "gameplay.gift_pick";

        /// <summary>English: "The counter is full".</summary>
        internal const string CounterFull = "gameplay.counter_full";

        /// <summary>English: "Only one spot left!".</summary>
        internal const string OneSpotLeft = "gameplay.one_spot_left";

        /// <summary>English: "{0} complete! +{1} coins". Arguments: {0} department name, {1} bonus coins.</summary>
        internal const string FamilyComplete = "gameplay.family_complete";

        /// <summary>
        /// English: "Tap the &lt;b&gt;{0}&lt;/b&gt;. It goes straight into the basket that asks for it.". Arguments:
        /// {0} item name.
        /// </summary>
        internal const string Tutorial = "gameplay.tutorial";

        /// <summary>English: "Tap &lt;b&gt;+&lt;/b&gt; to call one more customer. Free this time!".</summary>
        internal const string BasketTip = "gameplay.basket_tip";

        /// <summary>English: "Undo".</summary>
        internal const string BoosterUndo = "gameplay.booster_undo";

        /// <summary>English: "Quick Pick".</summary>
        internal const string BoosterPick = "gameplay.booster_pick";

        /// <summary>English: "Tidy Up".</summary>
        internal const string BoosterTidy = "gameplay.booster_tidy";

        /// <summary>English: "Shuffle".</summary>
        internal const string BoosterShuffle = "gameplay.booster_shuffle";

        /// <summary>English: "{0} +1". Arguments: {0} booster name.</summary>
        internal const string BoosterAdded = "gameplay.booster_added";

        /// <summary>English: "Quick Pick filled a basket".</summary>
        internal const string PickDone = "gameplay.pick_done";

        /// <summary>English: "No basket to fill right now".</summary>
        internal const string PickNone = "gameplay.pick_none";

        /// <summary>English: "Items went back to the stockroom".</summary>
        internal const string TidyDone = "gameplay.tidy_done";

        /// <summary>English: "The counter is already clear".</summary>
        internal const string TidyNone = "gameplay.tidy_none";

        /// <summary>English: "Nothing to shuffle".</summary>
        internal const string ShuffleNone = "gameplay.shuffle_none";

        /// <summary>English: "Move undone".</summary>
        internal const string UndoDone = "gameplay.undo_done";

        /// <summary>English: "Nothing to undo yet".</summary>
        internal const string UndoNone = "gameplay.undo_none";

        /// <summary>English: "Out of free undos on this level. Take back your last move.".</summary>
        internal const string UndoOffer = "gameplay.undo_offer";

        /// <summary>
        /// English: "Fills the basket that's closest to done, pulling items from the counter, the shelves, even the
        /// stockroom.".
        /// </summary>
        internal const string PickInfo = "gameplay.pick_info";

        /// <summary>
        /// English: "Puts up to three items from the counter back on a shelf, so the counter has room again.".
        /// </summary>
        internal const string TidyInfo = "gameplay.tidy_info";

        /// <summary>English: "Mixes up all the items on the open shelves.".</summary>
        internal const string ShuffleInfo = "gameplay.shuffle_info";

        /// <summary>English: "Get 2".</summary>
        internal const string BuyTwo = "gameplay.buy_two";

        /// <summary>English: "Get 1".</summary>
        internal const string BuyOne = "gameplay.buy_one";

        /// <summary>English: "Get 1 free".</summary>
        internal const string GetOneFree = "gameplay.get_one_free";

        /// <summary>English: "Not now".</summary>
        internal const string NotNow = "gameplay.not_now";

        /// <summary>English: "Rosie opened a third basket. On the house!".</summary>
        internal const string BasketFree = "gameplay.basket_free";

        /// <summary>English: "Extra basket".</summary>
        internal const string BasketTitle = "gameplay.basket_title";

        /// <summary>
        /// English: "Open a third basket for this level. More baskets, fewer items waiting on the counter.".
        /// </summary>
        internal const string BasketText = "gameplay.basket_text";

        /// <summary>English: "Open".</summary>
        internal const string BasketOpen = "gameplay.basket_open";

        /// <summary>English: "Open free".</summary>
        internal const string BasketOpenFree = "gameplay.basket_open_free";

        /// <summary>English: "Level {0} complete". Arguments: {0} level number.</summary>
        internal const string CompleteTitle = "gameplay.complete_title";

        /// <summary>English: "Tricky level complete".</summary>
        internal const string CompleteTrickyTitle = "gameplay.complete_tricky_title";

        /// <summary>English: "Beautifully kept!".</summary>
        internal const string HeadlineThreeStars = "gameplay.headline_three_stars";

        /// <summary>English: "Lovely work!".</summary>
        internal const string HeadlineTwoStars = "gameplay.headline_two_stars";

        /// <summary>English: "Shelf cleared!".</summary>
        internal const string HeadlineOneStar = "gameplay.headline_one_star";

        /// <summary>English: "The counter never got crowded. That's the neat way to do it.".</summary>
        internal const string CompleteNeat = "gameplay.complete_neat";

        /// <summary>
        /// English: "Keep the counter at {0} items or fewer for three stars.". Arguments: {0} star threshold.
        /// </summary>
        internal const string CompleteHint = "gameplay.complete_hint";

        /// <summary>English: "Items packed".</summary>
        internal const string StatItems = "gameplay.stat_items";

        /// <summary>English: "Most items on the counter".</summary>
        internal const string StatPeak = "gameplay.stat_peak";

        /// <summary>English: "Moves".</summary>
        internal const string StatMoves = "gameplay.stat_moves";

        /// <summary>English: "Double coins".</summary>
        internal const string DoubleCoins = "gameplay.double_coins";

        /// <summary>English: "+{0} coins". Arguments: {0} coins added.</summary>
        internal const string CoinsAdded = "gameplay.coins_added";

        /// <summary>English: "Replay".</summary>
        internal const string Replay = "gameplay.replay";

        /// <summary>English: "Next level".</summary>
        internal const string NextLevel = "gameplay.next_level";

        /// <summary>English: "Continue".</summary>
        internal const string Continue = "gameplay.continue";

        /// <summary>English: "Tricky · Rewards ×2".</summary>
        internal const string NextTricky = "gameplay.next_tricky";

        /// <summary>English: "Oops, the counter is full!".</summary>
        internal const string LossTitle = "gameplay.loss_title";

        /// <summary>English: "Only {0} item left!". Arguments: {0} is 1.</summary>
        internal const string LossOneLeft = "gameplay.loss_one_left";

        /// <summary>
        /// English: "Only {0} items left!". Arguments: {0} items left, shown when at most 35% are left.
        /// </summary>
        internal const string LossFewLeft = "gameplay.loss_few_left";

        /// <summary>English: "{0} items left". Arguments: {0} items left.</summary>
        internal const string LossLeft = "gameplay.loss_left";

        /// <summary>English: "Call one more customer".</summary>
        internal const string CallCustomer = "gameplay.call_customer";

        /// <summary>English: "One more customer is here!".</summary>
        internal const string CustomerArrived = "gameplay.customer_arrived";

        /// <summary>English: "A third basket opens and takes items straight from the counter.".</summary>
        internal const string CustomerNote = "gameplay.customer_note";

        /// <summary>English: "+1 counter spot".</summary>
        internal const string ExtraSpot = "gameplay.extra_spot";

        /// <summary>English: "One more spot on the counter".</summary>
        internal const string SpotAdded = "gameplay.spot_added";

        /// <summary>English: "Undo last move · free".</summary>
        internal const string UndoFree = "gameplay.undo_free";

        /// <summary>English: "Tidy Up ({0} left)". Arguments: {0} owned Tidy Ups.</summary>
        internal const string TidyLeft = "gameplay.tidy_left";

        /// <summary>English: "Start the level over".</summary>
        internal const string StartOver = "gameplay.start_over";

        /// <summary>English: "Lovely!".</summary>
        internal const string Praise1 = "gameplay.praise_1";

        /// <summary>English: "Sweet!".</summary>
        internal const string Praise2 = "gameplay.praise_2";

        /// <summary>English: "Delicious!".</summary>
        internal const string Praise3 = "gameplay.praise_3";

        /// <summary>English: "Well done!".</summary>
        internal const string Praise4 = "gameplay.praise_4";

        /// <summary>English: "Perfect!".</summary>
        internal const string Praise5 = "gameplay.praise_5";

        /// <summary>English: "Yummy!".</summary>
        internal const string Praise6 = "gameplay.praise_6";

        /// <summary>English: "Neat!".</summary>
        internal const string Combo1 = "gameplay.combo_1";

        /// <summary>English: "Tidy shop!".</summary>
        internal const string Combo2 = "gameplay.combo_2";

        /// <summary>English: "Rosie is proud!".</summary>
        internal const string Combo3 = "gameplay.combo_3";

        /// <summary>English: "On a roll!".</summary>
        internal const string Combo4 = "gameplay.combo_4";

        /// <summary>English: "Busy hands!".</summary>
        internal const string Combo5 = "gameplay.combo_5";

        /// <summary>English: "What a streak!".</summary>
        internal const string Combo6 = "gameplay.combo_6";

        /// <summary>English: "The shop is buzzing!".</summary>
        internal const string Combo7 = "gameplay.combo_7";

        /// <summary>English: "Look at you go!".</summary>
        internal const string Combo8 = "gameplay.combo_8";

        /// <summary>English: "Level {0}". Arguments: {0} level number.</summary>
        internal const string StartTitle = "gameplay.start_title";

        /// <summary>English: "Tricky level {0}". Arguments: {0} level number.</summary>
        internal const string StartTrickyTitle = "gameplay.start_tricky_title";

        /// <summary>
        /// English: "New: start boosters! Rosie gives you two of each. Pick what you want to begin the level with.".
        /// </summary>
        internal const string StartFirst = "gameplay.start_first";

        /// <summary>English: "A tricky one. A start booster helps!".</summary>
        internal const string StartTricky = "gameplay.start_tricky";

        /// <summary>English: "Get ready. Pick a start booster if you like.".</summary>
        internal const string StartNormal = "gameplay.start_normal";

        /// <summary>English: "Extra basket".</summary>
        internal const string StartBasketName = "gameplay.start_basket_name";

        /// <summary>English: "start with three baskets".</summary>
        internal const string StartBasketInfo = "gameplay.start_basket_info";

        /// <summary>English: "Quick Pick".</summary>
        internal const string StartQuickName = "gameplay.start_quick_name";

        /// <summary>English: "one extra for this level".</summary>
        internal const string StartQuickInfo = "gameplay.start_quick_info";

        /// <summary>English: "{0} left". Arguments: {0} owned start boosters.</summary>
        internal const string StartOwned = "gameplay.start_owned";

        /// <summary>English: "{0} coins". Arguments: {0} price in coins.</summary>
        internal const string StartPrice = "gameplay.start_price";

        /// <summary>
        /// English: "&lt;b&gt;{0}&lt;/b&gt; &lt;size=75%&gt;{1} · {2}&lt;/size&gt;". Arguments: {0} booster name, {1}
        /// what it does, {2} owned count or price.
        /// </summary>
        internal const string StartToggle = "gameplay.start_toggle";

        /// <summary>English: "Play".</summary>
        internal const string StartPlay = "gameplay.start_play";

        /// <summary>English: "Back to the shop".</summary>
        internal const string StartBack = "gameplay.start_back";

        /// <summary>English: "Not enough coins".</summary>
        internal const string NotEnoughCoins = "gameplay.not_enough_coins";
    }
}
