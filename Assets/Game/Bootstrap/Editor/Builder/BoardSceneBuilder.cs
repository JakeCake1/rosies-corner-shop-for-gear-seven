using Game.Bootstrap.Composition;
using Game.Features.Gameplay;
using Game.Features.Gameplay.Board;
using Game.Features.Gameplay.Hud;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Game.Bootstrap.Editor.Builder.UiFactory;

namespace Game.Bootstrap.Editor.Builder
{
    /// <summary>
    /// Builds the gameplay scene content into the open scene: board canvas, HUD, basket ledge, counter, cabinet,
    /// booster bar, banner and the scene lifetime scope with all references wired.
    /// </summary>
    internal static class BoardSceneBuilder
    {
        public static void Build(PrefabBuilder.Result prefabs, GameplayArt art)
        {
            var canvas = PrefabBuilder.MakeCanvas(null, "BoardCanvas", 0);
            var safe = PrefabBuilder.SafeArea(canvas.transform);
            var content = Node("Content", safe).Stretch();

            var hud = BuildHud(content);
            var basketArea = Node("BasketArea", content).Top(56f, new Vector2(358f, 104f));
            var ledge = Tiled("Ledge", basketArea, Kit("ledge")).rectTransform;
            ledge.At(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 16f), new Vector2(0f, 22f));
            ledge.anchorMin = new Vector2(-0.02f, 0f);
            ledge.anchorMax = new Vector2(1.02f, 0f);
            var basketLayer = Node("Baskets", basketArea);
            basketLayer.anchorMin = basketLayer.anchorMax = new Vector2(0.5f, 1f);
            basketLayer.pivot = new Vector2(0.5f, 1f);
            basketLayer.sizeDelta = Vector2.zero;
            var locks = new Object[3];
            for (var i = 0; i < 3; i++)
            {
                var lockButton = Button("Lock" + i, basketArea, Kit("white"), new Color(1f, 1f, 1f, 0f));
                var lr = lockButton.GetComponent<RectTransform>();
                lr.At(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 0f), new Vector2(108f, 92f));
                var ghost = Img("Ghost", lr, Art("basket/basket_front"), new Color(1f, 1f, 1f, 0.35f));
                ghost.rectTransform.Stretch();
                var plus = Img("Plus", lr, Kit("circle"), Leaf).rectTransform.Center(new Vector2(0f, 6f), new Vector2(44f, 44f));
                Text("Text", plus, "+", 34f, Color.white).rectTransform.Stretch(0, 0, 0, 4);
                lockButton.gameObject.SetActive(false);
                locks[i] = lockButton;
            }

            var counter = Img("Counter", content, Kit("board_oak")).rectTransform.Top(166f, new Vector2(358f, 106f));
            var counterSlots = new Object[6];
            var doilies = new Object[6];
            for (var i = 0; i < 6; i++)
            {
                var slot = Node("Slot" + i, counter);
                slot.At(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-150f + i * 60f, 24f), new Vector2(52f, 52f));
                var doily = Img("Doily", slot, Kit("doily"));
                doily.type = Image.Type.Simple;
                doily.rectTransform.At(new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, 2f), new Vector2(56f, 20f));
                doily.transform.SetAsFirstSibling();
                counterSlots[i] = slot;
                doilies[i] = doily;
                if (i == 5) slot.gameObject.SetActive(false);
            }

            var counterLabel = Text("Label", counter, "COUNTER", 11f, Hex("#FFF6E8"), TextAlignmentOptions.Right);
            counterLabel.characterSpacing = 18f;
            counterLabel.rectTransform.At(new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-14f, 4f), new Vector2(140f, 16f));

            var cabinet = Img("Cabinet", content, Kit("cabinet")).rectTransform.Top(320f, new Vector2(358f, 420f));
            var awning = Tiled("Awning", cabinet, Kit("awning")).rectTransform;
            awning.anchorMin = new Vector2(-0.03f, 1f);
            awning.anchorMax = new Vector2(1.03f, 1f);
            awning.pivot = new Vector2(0.5f, 0f);
            awning.anchoredPosition = new Vector2(0f, -24f);
            awning.sizeDelta = new Vector2(0f, 38f);
            var tabs = Node("Tabs", cabinet);
            tabs.anchorMin = new Vector2(0f, 1f);
            tabs.anchorMax = new Vector2(1f, 1f);
            tabs.pivot = new Vector2(0.5f, 1f);
            tabs.anchoredPosition = new Vector2(0f, -22f);
            tabs.sizeDelta = new Vector2(-24f, 26f);
            Horizontal(tabs, 4f, TextAnchor.MiddleCenter, true);
            var labels = new Object[3];
            var hammers = new Object[3];
            var columns = new Object[3];
            var columnsRoot = Node("Columns", cabinet);
            columnsRoot.anchorMin = new Vector2(0f, 0f);
            columnsRoot.anchorMax = new Vector2(1f, 0f);
            columnsRoot.pivot = new Vector2(0.5f, 0f);
            columnsRoot.anchoredPosition = new Vector2(0f, 11f);
            columnsRoot.sizeDelta = new Vector2(-18f, 300f);
            for (var c = 0; c < 3; c++)
            {
                var tab = Img("Tab" + c, tabs, Kit("chip"), new Color(1f, 1f, 1f, 0.85f));
                var label = Text("Text", tab.transform, "all out", 13f, Ink2);
                label.rectTransform.Stretch(4, 18, 0, 2);
                var hammer = Fit("Hammer", tab.transform, Art("tools/hammer")).rectTransform;
                hammer.At(new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-4f, 0f), new Vector2(14f, 20f));
                hammer.gameObject.SetActive(false);
                labels[c] = label;
                hammers[c] = hammer.gameObject;
                var col = Node("Column" + c, columnsRoot);
                col.anchorMin = new Vector2(c / 3f, 0f);
                col.anchorMax = new Vector2((c + 1) / 3f, 0f);
                col.pivot = new Vector2(0.5f, 0f);
                col.offsetMin = new Vector2(2f, 0f);
                col.offsetMax = new Vector2(-2f, 300f);
                columns[c] = col;
            }

            var boosterBar = Node("Boosters", content);
            boosterBar.At(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 10f), new Vector2(358f, 90f));
            Horizontal(boosterBar, 22f, TextAnchor.MiddleCenter);
            var boosters = new BoosterButtonView[4];
            for (var i = 0; i < 4; i++)
            {
                boosters[i] = BuildBooster(boosterBar, i);
            }

            var progressTarget = (RectTransform)hud.transform.Find("Progress");
            var banner = BuildBanner(content);
            var flight = Node("Flight", content).Stretch();
            var keyFlyer = Fit("KeyFlyer", flight, Art("shelf/key_tag"));
            keyFlyer.rectTransform.Center(Vector2.zero, new Vector2(20f, 26f));
            keyFlyer.gameObject.SetActive(false);
            var pool = Node("[Pool]", content);
            pool.gameObject.SetActive(true);

            var board = content.gameObject.AddComponent<BoardView>();
            Wire(board, ("_content", content), ("_basketArea", basketArea), ("_basketLayer", basketLayer),
                ("_basketLocks", locks), ("_counter", counter), ("_counterSlots", counterSlots),
                ("_counterDoilies", doilies), ("_cabinet", cabinet), ("_columns", columns), ("_stockLabels", labels),
                ("_stockHammers", hammers), ("_boosterBar", boosterBar), ("_flightLayer", flight),
                ("_progressTarget", progressTarget), ("_hammerSprite", Art("tools/hammer")), ("_bagSprite", Art("tools/paper_bag")),
                ("_doily", Kit("doily")), ("_doilyHot", Kit("doily_hot")), ("_keyFlyer", keyFlyer),
                ("_itemPrefab", prefabs.Item.GetComponent<ItemView>()), ("_shelfPrefab", prefabs.Shelf.GetComponent<ShelfView>()),
                ("_basketPrefab", prefabs.Basket.GetComponent<BasketView>()), ("_poolContainer", pool));

            var views = content.gameObject.AddComponent<GameplayViews>();
            Wire(views, ("_hud", hud), ("_boosters", boosters), ("_banner", banner));

            var scopeGo = new GameObject("GameplayLifetimeScope");
            var scope = scopeGo.AddComponent<GameplayLifetimeScope>();
            Wire(scope, ("_board", board), ("_views", views), ("_art", art));
        }

        private static HudView BuildHud(Transform parent)
        {
            var hud = Node("Hud", parent).Top(6f, new Vector2(370f, 50f));
            var badge = Img("Badge", hud, Kit("badge_gold"));
            badge.type = Image.Type.Simple;
            badge.rectTransform.At(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Vector2(52f, 54f));
            var title = Text("Title", badge.transform, "LEVEL", 9f, Walnut);
            title.rectTransform.At(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -8f), new Vector2(50f, 12f));
            title.characterSpacing = 8f;
            var number = Text("Number", badge.transform, "1", 24f, Walnut);
            number.rectTransform.Stretch(0, 0, 14, 4);
            number.fontStyle = FontStyles.Bold;

            var progress = Img("Progress", hud, Kit("bar_track")).rectTransform;
            progress.anchorMin = new Vector2(0f, 0.5f);
            progress.anchorMax = new Vector2(1f, 0.5f);
            progress.pivot = new Vector2(0.5f, 0.5f);
            progress.offsetMin = new Vector2(60f, -15f);
            progress.offsetMax = new Vector2(-148f, 15f);
            var fillArea = Node("FillArea", progress).Stretch(3, 3, 3, 3);
            var fill = Img("Fill", fillArea, Kit("bar_fill")).rectTransform;
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = new Vector2(0f, 1f);
            fill.offsetMin = Vector2.zero;
            fill.offsetMax = Vector2.zero;
            var progressText = Text("Text", progress, "0 / 18", 17f, Color.white);
            progressText.rectTransform.Stretch(0, 0, 0, 2);
            progressText.fontStyle = FontStyles.Bold;

            var coins = Img("Coins", hud, Kit("chip")).rectTransform;
            coins.At(new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-50f, 1f), new Vector2(92f, 34f));
            Fit("Coin", coins, Icon("coin")).rectTransform.At(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(6f, 1f), new Vector2(24f, 24f));
            var coinText = Text("Value", coins, "150", 17f, Ink, TextAlignmentOptions.Left);
            coinText.rectTransform.Stretch(34, 4, 0, 3);
            coinText.fontStyle = FontStyles.Bold;

            var gear = Button("Gear", hud, Kit("btn_wood"));
            gear.GetComponent<RectTransform>().At(new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), Vector2.zero, new Vector2(42f, 44f));
            Fit("Icon", gear.transform, Icon("gear")).rectTransform.Stretch(9, 9, 8, 12);

            var view = hud.gameObject.AddComponent<HudView>();
            Wire(view, ("_badge", badge), ("_badgeGold", Kit("badge_gold")), ("_badgeRed", Kit("badge_red")),
                ("_badgeTitle", title), ("_levelNumber", number), ("_progressFill", fill), ("_progressText", progressText),
                ("_coins", coinText), ("_gear", gear));
            return view;
        }

        private static BoosterButtonView BuildBooster(Transform parent, int index)
        {
            var root = Node("Booster" + index, parent);
            Layout(root, 88f, 70f);
            var group = root.gameObject.AddComponent<CanvasGroup>();
            var glow = Img("Glow", root, Kit("circle"), new Color(1f, 0.85f, 0.4f, 0.9f));
            glow.rectTransform.At(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 6f), new Vector2(76f, 76f));
            glow.gameObject.SetActive(false);
            var tile = Button("Tile", root, Kit("rounded_white"), Hex("#FFF8EA"));
            tile.GetComponent<RectTransform>().At(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(60f, 60f));
            var icon = Fit("Icon", tile.transform, null);
            icon.rectTransform.Stretch(7, 7, 7, 7);
            var badge = Img("Badge", tile.transform, Kit("circle"), Berry);
            badge.rectTransform.At(new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-4f, -4f), new Vector2(24f, 24f));
            var count = Text("Count", badge.transform, "2", 14f, Color.white);
            count.rectTransform.Stretch(0, 0, 0, 2);
            count.fontStyle = FontStyles.Bold;
            var title = Text("Title", root, "Undo", 13f, Color.white);
            title.rectTransform.At(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 2f), new Vector2(90f, 22f));
            title.fontStyle = FontStyles.Bold;
            title.outlineWidth = 0.2f;
            title.outlineColor = new Color32(63, 39, 20, 255);
            var view = root.gameObject.AddComponent<BoosterButtonView>();
            Wire(view, ("_button", tile), ("_icon", icon), ("_title", title), ("_badge", badge), ("_count", count),
                ("_group", group), ("_glow", glow));
            root.gameObject.SetActive(false);
            return view;
        }

        private static LevelBannerView BuildBanner(Transform parent)
        {
            var root = Node("Banner", parent).Center(new Vector2(0f, 120f), new Vector2(320f, 110f));
            var group = root.gameObject.AddComponent<CanvasGroup>();
            var ribbon = Img("Ribbon", root, Kit("ribbon_green"));
            ribbon.rectTransform.At(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(300f, 52f));
            var title = Text("Title", ribbon.transform, "Level 1", 26f, Color.white);
            title.rectTransform.Stretch(20, 20, 0, 10);
            title.fontStyle = FontStyles.Bold;
            var card = Img("Sub", root, Kit("card")).rectTransform;
            card.At(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -50f), new Vector2(290f, 56f));
            var sub = Text("Text", card, "Tip", 15f, Ink);
            sub.rectTransform.Stretch(12, 12, 6, 8);
            var view = root.gameObject.AddComponent<LevelBannerView>();
            Wire(view, ("_rect", root), ("_group", group), ("_ribbon", ribbon), ("_ribbonBerry", Kit("ribbon_berry")),
                ("_ribbonGreen", Kit("ribbon_green")), ("_title", title), ("_subCard", card.gameObject), ("_sub", sub));
            return view;
        }
    }
}
