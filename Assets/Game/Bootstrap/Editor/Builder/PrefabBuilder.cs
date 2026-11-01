using Game.Features.Album;
using Game.Features.Gameplay.Board;
using Game.Features.Home;
using Game.Services.UI;
using Game.Services.UI.Dialogs;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static Game.Bootstrap.Editor.Builder.UiFactory;

namespace Game.Bootstrap.Editor.Builder
{
    /// <summary>
    /// Builds the UI root, window and pooled-view prefabs from code with all serialized references wired.
    /// </summary>
    internal static class PrefabBuilder
    {
        public const string Folder = ProjectBuilder.Generated + "/Prefabs";
        private static readonly Vector2 Reference = new Vector2(390f, 844f);

        public sealed class Result
        {
            public GameObject UIRoot;
            public GameObject Dialog;
            public GameObject Home;
            public GameObject Album;
            public GameObject Item;
            public GameObject Shelf;
            public GameObject Basket;
            public GameObject Booster;
        }

        public static Result BuildAll()
        {
            ProjectBuilder.EnsureFolder(Folder);
            return new Result
            {
                UIRoot = Save(BuildUIRoot(), "UIRoot"),
                Dialog = Save(BuildDialog(), "DialogWindow"),
                Home = Save(BuildHome(), "HomeWindow"),
                Album = Save(BuildAlbum(), "AlbumWindow"),
                Item = Save(BuildItem(), "ItemView"),
                Shelf = Save(BuildShelf(), "ShelfView"),
                Basket = Save(BuildBasket(), "BasketView"),
            };
        }

        public static Canvas MakeCanvas(Transform parent, string name, int order)
        {
            var rt = Node(name, parent);
            var canvas = rt.gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = order;
            var scaler = rt.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = Reference;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            rt.gameObject.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        public static RectTransform SafeArea(Transform parent)
        {
            var rt = Node("SafeArea", parent).Stretch();
            var fitter = rt.gameObject.AddComponent<SafeAreaFitter>();
            Wire(fitter, ("_rect", rt));
            return rt;
        }

        private static GameObject Save(GameObject go, string name)
        {
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, $"{Folder}/{name}.prefab");
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject BuildUIRoot()
        {
            var root = new GameObject("UIRoot");
            var uiRoot = root.AddComponent<UIRoot>();
            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            es.transform.SetParent(root.transform);

            var bgCanvas = MakeCanvas(root.transform, "BackgroundCanvas", -100);
            Object.DestroyImmediate(bgCanvas.GetComponent<GraphicRaycaster>());
            var bg = Node("Background", bgCanvas.transform).Stretch();
            var bgView = bg.gameObject.AddComponent<BackgroundView>();
            Tiled("Wallpaper", bg, Kit("wallpaper")).rectTransform.Stretch();
            var wainscot = Tiled("Wainscot", bg, Kit("wainscot")).rectTransform;
            wainscot.anchorMin = new Vector2(0f, 0f);
            wainscot.anchorMax = new Vector2(1f, 0f);
            wainscot.pivot = new Vector2(0.5f, 0f);
            wainscot.sizeDelta = new Vector2(0f, 120f);
            var rail = Img("Rail", wainscot, Kit("wainscot_rail")).rectTransform;
            rail.At(new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(0f, 8f));
            rail.anchorMin = new Vector2(0f, 1f);
            rail.anchorMax = new Vector2(1f, 1f);
            var bunting = Img("Bunting", bg, Icon("bunting")).rectTransform;
            bunting.anchorMin = new Vector2(0f, 1f);
            bunting.anchorMax = new Vector2(1f, 1f);
            bunting.pivot = new Vector2(0.5f, 1f);
            bunting.sizeDelta = new Vector2(0f, 44f);
            var tint = Img("TrickyTint", bg, Kit("white"), new Color(0.78f, 0.24f, 0.31f, 0f));
            tint.rectTransform.Stretch();
            Wire(bgView, ("_trickyTint", tint), ("_bunting", bunting.gameObject));

            var main = MakeCanvas(root.transform, "MainCanvas", 10);
            var safe = SafeArea(main.transform);
            var hud = Node("HUD", safe).Stretch();
            var screens = Node("Screens", safe).Stretch();
            var popups = Node("Popups", main.transform).Stretch();
            var overlay = Node("Overlay", main.transform).Stretch();

            var blocker = Button("Blocker", popups, Kit("white"), new Color(0.16f, 0.09f, 0.05f, 0.6f));
            blocker.transition = Selectable.Transition.None;
            blocker.GetComponent<RectTransform>().Stretch();
            blocker.gameObject.SetActive(false);

            var toast = BuildToast(overlay);
            var coach = BuildCoach(overlay);
            var effects = BuildEffects(overlay);
            Wire(uiRoot, ("_hud", hud), ("_screens", screens), ("_popups", popups), ("_overlay", overlay),
                ("_blocker", blocker), ("_toast", toast), ("_coach", coach), ("_effects", effects), ("_mainCanvas", main),
                ("_background", bgView));
            return root;
        }

        private static ToastView BuildToast(Transform parent)
        {
            var rt = Node("Toast", parent).Top(96f, new Vector2(300f, 44f));
            var group = rt.gameObject.AddComponent<CanvasGroup>();
            var bg = Img("Pill", rt, Kit("pill_white"), new Color(0.23f, 0.16f, 0.11f, 0.92f));
            bg.rectTransform.Stretch();
            var text = Text("Text", rt, "Toast", 17f, Color.white);
            text.rectTransform.Stretch(14, 14, 4, 4);
            var view = rt.gameObject.AddComponent<ToastView>();
            Wire(view, ("_group", group), ("_background", bg), ("_text", text));
            return view;
        }

        private static CoachView BuildCoach(Transform parent)
        {
            var rt = Node("Coach", parent).Stretch();
            var bubble = Node("Bubble", rt).Center(Vector2.zero, new Vector2(270f, 60f));
            var group = bubble.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            var background = Img("Background", bubble, Kit("card"));
            background.rectTransform.Stretch();
            background.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            var text = Text("Text", bubble, "Tip", 16f, Ink);
            text.rectTransform.Stretch(14, 14, 10, 12);
            var fitter = Vertical(bubble, 0f, new RectOffset(14, 14, 10, 12));
            fitter.childControlHeight = true;
            FitHeight(bubble);
            text.gameObject.AddComponent<LayoutElement>();
            var hand = Fit("Hand", rt, Icon("tutorial_hand")).rectTransform.Center(Vector2.zero, new Vector2(54f, 62f));
            var view = rt.gameObject.AddComponent<CoachView>();
            Wire(view, ("_bubble", bubble), ("_bubbleGroup", group), ("_text", text), ("_hand", hand));
            return view;
        }

        private static EffectsView BuildEffects(Transform parent)
        {
            var rt = Node("Effects", parent).Stretch();
            var particle = Img("Particle", rt, Kit("circle"));
            particle.rectTransform.Center(Vector2.zero, new Vector2(6f, 6f));
            var praise = Text("Praise", rt, "Lovely!", 26f, Color.white);
            praise.rectTransform.Center(Vector2.zero, new Vector2(260f, 40f));
            praise.fontStyle = FontStyles.Bold;
            praise.outlineWidth = 0.25f;
            praise.outlineColor = new Color32(110, 29, 41, 255);
            var view = rt.gameObject.AddComponent<EffectsView>();
            Wire(view, ("_area", rt), ("_particlePrefab", particle), ("_praisePrefab", praise));
            return view;
        }

        private static GameObject BuildDialog()
        {
            var root = Node("DialogWindow", null).Stretch();
            var group = root.gameObject.AddComponent<CanvasGroup>();
            var card = Img("Card", root, Kit("card")).rectTransform;
            card.Center(new Vector2(0f, -10f), new Vector2(330f, 200f));
            var cardLayout = Vertical(card, 0f, new RectOffset(20, 20, 40, 22));
            FitHeight(card);
            var ribbon = Img("Ribbon", card, Kit("ribbon_berry"));
            ribbon.rectTransform.At(new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(280f, 46f));
            ribbon.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            var title = Text("Title", ribbon.transform, "Title", 21f, Color.white);
            title.rectTransform.Stretch(26, 26, 0, 8);
            title.fontStyle = FontStyles.Bold;
            var body = Node("Body", card);
            Vertical(body, 10f);
            body.gameObject.AddComponent<LayoutElement>();
            cardLayout.childControlHeight = true;

            var icon = Fit("Icon", body, null);
            Layout(icon, 96f);
            var stars = Node("Stars", body);
            Horizontal(stars, 10f, TextAnchor.UpperCenter);
            Layout(stars, 72f);
            for (var i = 0; i < 3; i++)
            {
                // The middle star is larger and sits higher, as in the mockup.
                var s = Fit("Star" + i, stars, Icon("star_on"));
                var side = i == 1 ? 68f : 52f;
                Layout(s, side, side);
            }

            var headline = Text("Headline", body, "Headline", 26f, Walnut, display: true);
            var text = Text("Text", body, "Text", 17f, Ink);
            var stat = Node("Stat", body);
            var statRow = Horizontal(stat, 8f, TextAnchor.MiddleCenter, true);
            statRow.padding = new RectOffset(14, 14, 0, 0);
            Layout(stat, 34f);
            var pill = Img("Background", stat, Kit("rounded_white"), Hex("#F3E8D8"));
            pill.rectTransform.Stretch();
            pill.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            pill.transform.SetAsFirstSibling();
            Text("Label", stat, "Label", 16f, Ink2, TextAlignmentOptions.Left);
            Text("Value", stat, "0", 17f, Ink, TextAlignmentOptions.Right).fontStyle = FontStyles.Bold;

            var progress = Node("Progress", body);
            Vertical(progress, 6f);
            Text("Caption", progress, "Caption", 19f, Walnut).fontStyle = FontStyles.Bold;
            var track = Img("Track", progress, Kit("bar_track"));
            Layout(track, 20f);
            var fill = Img("Fill", track.transform, Kit("bar_fill")).rectTransform;
            fill.anchorMin = new Vector2(0f, 0f);
            fill.anchorMax = new Vector2(0.5f, 1f);
            fill.offsetMin = new Vector2(3f, 3f);
            fill.offsetMax = new Vector2(-3f, -3f);

            var items = Node("Items", body);
            Horizontal(items, 2f);
            Layout(items, 60f);
            Layout(Fit("Item", items, null), 58f, 36f);

            var reward = Node("Reward", body);
            Horizontal(reward, 6f);
            Layout(reward, 40f);
            Layout(Fit("Coin", reward, Icon("coin")), 30f, 30f);
            Layout(Text("Amount", reward, "+30", 26f, Walnut), 30f, 90f).GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;

            var toggle = Node("Toggle", body);
            Horizontal(toggle, 8f, TextAnchor.MiddleCenter, true);
            Layout(toggle, 46f);
            Layout(Text("Label", toggle, "Sound", 18f, Ink, TextAlignmentOptions.Left), -1, -1, 1f);
            var sw = Button("Switch", toggle, Kit("pill_white"), Leaf);
            Layout(sw, 30f, 52f, 0f);
            var knob = Img("Knob", sw.transform, Kit("circle"), Color.white).rectTransform;
            knob.Center(new Vector2(11f, 0f), new Vector2(24f, 24f));

            var input = Node("Input", body);
            Vertical(input, 8f);
            var field = TMP_DefaultControls.CreateInputField(new TMP_DefaultControls.Resources());
            field.transform.SetParent(input, false);
            field.GetComponent<Image>().sprite = Kit("rounded_white");
            field.GetComponent<Image>().type = Image.Type.Sliced;
            foreach (var t in field.GetComponentsInChildren<TMP_Text>(true))
            {
                t.font = Body;
                t.fontSize = 20f;
            }

            Layout(field.GetComponent<Image>(), 46f);
            var ideas = Node("Ideas", input);
            Horizontal(ideas, 6f);
            Layout(ideas, 34f);
            var chip = Button("Idea", ideas, Kit("chip"));
            Layout(chip, 30f, 104f);
            Text("Text", chip.transform, "Idea", 12f, Ink).rectTransform.Stretch(4, 4, 2, 4);

            var grid = Node("Grid", body);
            var gl = grid.gameObject.AddComponent<GridLayoutGroup>();
            gl.cellSize = new Vector2(44f, 40f);
            gl.spacing = new Vector2(6f, 6f);
            gl.childAlignment = TextAnchor.MiddleCenter;
            grid.gameObject.AddComponent<LayoutElement>().preferredHeight = 180f;
            var cell = Button("Cell", grid, Kit("rounded_white"), Hex("#E7B77C"));
            Text("Text", cell.transform, "1", 18f, Color.white).rectTransform.Stretch();

            var segments = Node("Segments", body);
            Vertical(segments, 4f);
            Text("Label", segments, "Label", 15f, Ink2, TextAlignmentOptions.Left);
            var options = Node("Options", segments);
            Horizontal(options, 6f, TextAnchor.MiddleCenter, true);
            Layout(options, 36f);
            var option = Button("Option", options, Kit("rounded_white"));
            Text("Text", option.transform, "Option", 15f, Ink).rectTransform.Stretch(4, 4, 2, 2);

            var note = Text("Note", body, "Note", 13f, Ink2);
            var row = Node("Row", body);
            Horizontal(row, 10f, TextAnchor.MiddleCenter, true);
            var button = BuildDialogButton(body);

            foreach (Transform child in body)
            {
                child.gameObject.SetActive(false);
            }

            var window = root.gameObject.AddComponent<DialogWindow>();
            Wire(window, ("_canvasGroup", group), ("_card", card), ("_ribbon", ribbon), ("_ribbonBerry", Kit("ribbon_berry")),
                ("_ribbonGreen", Kit("ribbon_green")), ("_title", title), ("_body", body), ("_iconTemplate", icon),
                ("_starsTemplate", stars), ("_starOn", Icon("star_on")), ("_starOff", Icon("star_off")),
                ("_headlineTemplate", headline), ("_textTemplate", text), ("_statTemplate", stat),
                ("_progressTemplate", progress), ("_itemsTemplate", items), ("_rewardTemplate", reward),
                ("_toggleTemplate", toggle), ("_inputTemplate", input), ("_gridTemplate", grid),
                ("_segmentsTemplate", segments), ("_noteTemplate", note), ("_rowTemplate", row), ("_buttonTemplate", button),
                ("_buttonSprites", new Object[]
                {
                    Kit("btn_berry"), Kit("btn_green"), Kit("btn_wood"), Kit("btn_gold"), Kit("btn_wood"),
                }));
            return root.gameObject;
        }

        private static DialogButtonView BuildDialogButton(Transform parent)
        {
            var b = Button("Button", parent, Kit("btn_berry"));
            var le = Layout(b, 58f);
            var content = Node("Content", b.transform).Stretch(10, 10, 4, 9);
            Horizontal(content, 8f, TextAnchor.MiddleCenter);
            var ad = Img("Ad", content, Kit("rounded_white"), Color.white);
            Layout(ad, 22f, 32f);
            Text("Text", ad.transform, "AD", 13f, Leaf).rectTransform.Stretch();
            var coin = Node("Coin", content);
            Horizontal(coin, 4f);
            Layout(Fit("Icon", coin, Icon("coin")), 24f, 24f);
            var price = Text("Price", coin, "100", 20f, Ink);
            Layout(price, 24f, 46f);
            var labels = Node("Labels", content);
            Vertical(labels, 0f, null, TextAnchor.MiddleCenter);
            var label = Text("Label", labels, "Button", 22f, Color.white);
            label.fontStyle = FontStyles.Bold;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            var sub = Text("Sub", labels, "Sub", 14f, Color.white);
            var view = b.gameObject.AddComponent<DialogButtonView>();
            Wire(view, ("_button", b), ("_background", b.GetComponent<Image>()), ("_label", label), ("_subLabel", sub),
                ("_adBadge", ad.gameObject), ("_coinBadge", coin.gameObject), ("_price", price), ("_layout", le));
            return view;
        }

        private static GameObject BuildHome()
        {
            var root = Node("HomeWindow", null).Stretch();
            var group = root.gameObject.AddComponent<CanvasGroup>();

            var gear = Button("Gear", root, Kit("btn_wood"));
            gear.GetComponent<RectTransform>().At(new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-14f, -8f), new Vector2(42f, 44f));
            Fit("Icon", gear.transform, Icon("gear")).rectTransform.Stretch(9, 9, 8, 12);

            var sign = Img("Sign", root, Kit("board_walnut")).rectTransform.Top(62f, new Vector2(340f, 92f));
            var name = Text("Name", sign, HomeViewModel.DefaultShopName, 40f, Cream, display: true);
            name.rectTransform.Stretch(12, 12, 8, 30);
            var tagline = Text("Tagline", sign, "GROCERIES · BAKERY · SWEETS", 12f, Hex("#E2C38A"));
            tagline.characterSpacing = 12f;
            tagline.rectTransform.At(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 12f), new Vector2(320f, 20f));

            var subtitle = Text("Subtitle", root, "Prototype", 13f, Ink2);
            subtitle.rectTransform.Top(166f, new Vector2(360f, 20f));
            var reviewers = Button("Reviewers", root, Kit("white"), new Color(1f, 1f, 1f, 0f));
            reviewers.GetComponent<RectTransform>().Top(186f, new Vector2(140f, 24f));
            var link = Text("Text", reviewers.transform, "<u>For reviewers</u>", 14f, Walnut);
            link.rectTransform.Stretch();

            var shelf = Node("Showcase", root).Top(230f, new Vector2(370f, 96f));
            var plank = Img("Plank", shelf, Kit("plank")).rectTransform;
            plank.At(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(370f, 20f));
            var items = Node("Items", shelf).Stretch(10, 10, 0, 16);
            Horizontal(items, 6f, TextAnchor.LowerCenter);
            var showcase = new Object[7];
            for (var i = 0; i < 7; i++)
            {
                var img = Fit("Item" + i, items, null);
                Layout(img, 74f, 44f);
                showcase[i] = img;
            }

            var coins = Img("Coins", root, Kit("chip")).rectTransform;
            coins.At(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, -342f), new Vector2(118f, 40f));
            Fit("Coin", coins, Icon("coin")).rectTransform.At(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(8f, 1f), new Vector2(28f, 28f));
            var coinText = Text("Value", coins, "150", 20f, Ink, TextAlignmentOptions.Left);
            coinText.rectTransform.Stretch(42, 8, 0, 3);
            coinText.fontStyle = FontStyles.Bold;

            var album = Button("Album", root, Kit("btn_grey"));
            album.GetComponent<RectTransform>().At(new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-16f, -338f), new Vector2(150f, 48f));
            Text("Text", album.transform, "Album", 20f, Color.white).rectTransform.Stretch(0, 22, 0, 6);
            var lockIcon = Fit("Lock", album.transform, Icon("lock")).rectTransform;
            lockIcon.At(new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-14f, 2f), new Vector2(18f, 20f));
            var badge = Img("Badge", album.transform, Kit("circle"), Berry).rectTransform;
            badge.At(new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-6f, -4f), new Vector2(24f, 24f));
            var badgeText = Text("Text", badge, "3", 13f, Color.white);
            badgeText.rectTransform.Stretch();

            var play = Button("Play", root, Kit("btn_berry"));
            play.GetComponent<RectTransform>().At(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 250f), new Vector2(350f, 82f));
            var playTitle = Text("Title", play.transform, "Open the shop", 30f, Color.white);
            playTitle.rectTransform.Stretch(0, 0, 8, 34);
            playTitle.fontStyle = FontStyles.Bold;
            var playLine = Text("Line", play.transform, "Level 1", 17f, Color.white);
            playLine.rectTransform.Stretch(0, 0, 44, 12);

            var window = root.gameObject.AddComponent<HomeWindow>();
            Wire(window, ("_canvasGroup", group), ("_shopName", name), ("_subtitle", subtitle), ("_reviewers", reviewers),
                ("_showcase", showcase), ("_coins", coinText), ("_album", album), ("_albumBackground", album.GetComponent<Image>()),
                ("_albumOpenSprite", Kit("btn_wood")), ("_albumLockedSprite", Kit("btn_grey")), ("_albumLock", lockIcon.gameObject),
                ("_albumBadge", badge.gameObject), ("_albumBadgeText", badgeText), ("_play", play),
                ("_playBackground", play.GetComponent<Image>()), ("_playSprite", Kit("btn_berry")),
                ("_playTrickySprite", Kit("btn_gold")), ("_playLine", playLine), ("_settings", gear));
            return root.gameObject;
        }

        private static GameObject BuildAlbum()
        {
            var root = Node("AlbumWindow", null).Stretch();
            var group = root.gameObject.AddComponent<CanvasGroup>();
            Img("Backdrop", root, Kit("white"), Hex("#A9BC96")).rectTransform.Stretch();
            var header = Node("Header", root).Top(8f, new Vector2(370f, 56f));
            var back = Button("Back", header, Kit("btn_wood"));
            back.GetComponent<RectTransform>().At(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(84f, 44f));
            Text("Text", back.transform, "Back", 18f, Ink).rectTransform.Stretch(0, 0, 0, 6);
            Text("Title", header, "Album", 32f, Cream, display: true).rectTransform.Stretch(90, 90, 0, 0);
            var total = Text("Total", header, "0 / 162", 16f, Cream, TextAlignmentOptions.Right);
            total.rectTransform.Stretch(260, 0, 0, 0);

            var scrollRt = Img("Scroll", root, Kit("card")).rectTransform.Stretch(10, 10, 70, 14);
            var scroll = scrollRt.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            var viewport = Node("Viewport", scrollRt).Stretch(6, 6, 6, 6);
            viewport.gameObject.AddComponent<RectMask2D>();
            var content = Node("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.sizeDelta = Vector2.zero;
            Vertical(content, 6f, new RectOffset(8, 8, 8, 16));
            FitHeight(content);
            scroll.viewport = viewport;
            scroll.content = content;

            var headerTemplate = Node("SectionHeader", content);
            Vertical(headerTemplate, 0f);
            Layout(headerTemplate, 48f);
            Text("Name", headerTemplate, "Dairy", 22f, Walnut, TextAlignmentOptions.Left, true);
            Text("Count", headerTemplate, "0 / 16", 13f, Ink2, TextAlignmentOptions.Left);
            var grid = Node("Grid", content);
            var gl = grid.gameObject.AddComponent<GridLayoutGroup>();
            gl.cellSize = new Vector2(52f, 86f);
            gl.spacing = new Vector2(4f, 4f);
            gl.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gl.constraintCount = 6;
            gl.childAlignment = TextAnchor.UpperCenter;
            FitHeight(grid);
            var cell = Node("Cell", grid);
            Fit("Image", cell, null).rectTransform.At(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(40f, 64f));
            var label = Text("Name", cell, "?", 10f, Ink);
            label.rectTransform.At(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(52f, 22f));
            headerTemplate.gameObject.SetActive(false);
            grid.gameObject.SetActive(false);
            cell.gameObject.SetActive(false);

            var window = root.gameObject.AddComponent<AlbumWindow>();
            Wire(window, ("_canvasGroup", group), ("_content", content), ("_headerTemplate", headerTemplate),
                ("_gridTemplate", grid), ("_cellTemplate", cell), ("_total", total), ("_back", back), ("_scroll", scroll));
            return root.gameObject;
        }

        private static GameObject BuildItem()
        {
            var root = Node("ItemView", null).Center(Vector2.zero, new Vector2(40f, 64f));
            var glow = Img("Glow", root, Kit("circle"), new Color(1f, 0.86f, 0.4f, 0.9f));
            glow.rectTransform.Stretch(-8, -8, 6, -6);
            glow.gameObject.SetActive(false);
            var image = Fit("Image", root, null);
            image.raycastTarget = true;
            image.rectTransform.Stretch();
            var view = root.gameObject.AddComponent<ItemView>();
            Wire(view, ("_rect", root), ("_image", image), ("_glow", glow));
            return root.gameObject;
        }

        private static GameObject BuildShelf()
        {
            var root = Node("ShelfView", null).At(Vector2.zero, new Vector2(0.5f, 0f), Vector2.zero, new Vector2(110f, 60f));
            var content = Node("Content", root).Stretch();
            var contentGroup = content.gameObject.AddComponent<CanvasGroup>();
            Img("Back", content, Kit("shelf_back")).rectTransform.Stretch();
            var postL = Img("PostL", content, Kit("shelf_post")).rectTransform;
            postL.anchorMin = new Vector2(0f, 0f);
            postL.anchorMax = new Vector2(0f, 1f);
            postL.pivot = new Vector2(0f, 0.5f);
            postL.sizeDelta = new Vector2(5f, 0f);
            var postR = Img("PostR", content, Kit("shelf_post")).rectTransform;
            postR.anchorMin = new Vector2(1f, 0f);
            postR.anchorMax = new Vector2(1f, 1f);
            postR.pivot = new Vector2(1f, 0.5f);
            postR.sizeDelta = new Vector2(5f, 0f);
            var cap = Img("Cap", content, Kit("shelf_floor")).rectTransform;
            cap.anchorMin = new Vector2(0f, 1f);
            cap.anchorMax = new Vector2(1f, 1f);
            cap.pivot = new Vector2(0.5f, 1f);
            cap.sizeDelta = new Vector2(0f, 6f);
            var floors = Node("Floors", content).Stretch();
            var floor = Img("Floor", floors, Kit("shelf_floor"));
            var cells = Node("Cells", content).Stretch();

            var badge = Img("RowsBadge", root, Kit("chip"), Hex("#5E3C20")).rectTransform;
            badge.At(new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -2f), new Vector2(64f, 18f));
            var rowsText = Text("Text", badge, "2 rows", 12f, Color.white);
            rowsText.rectTransform.Stretch(0, 0, 0, 2);

            var ties = Node("Ties", root).Stretch();
            Img("Twine", ties, Art("shelf/tie_twine")).rectTransform.Stretch(2, 2, 2, 2);
            Fit("Seal", ties, Art("shelf/tie_seal")).rectTransform.Center(Vector2.zero, new Vector2(34f, 34f));
            var key = Fit("KeyTag", root, Art("shelf/key_tag")).rectTransform;
            key.At(new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(0f, 4f), new Vector2(20f, 26f));

            var ice = Img("Ice", root, Kit("ice")).rectTransform;
            ice.Stretch();
            var iceBadge = Img("Badge", ice, Kit("pill_white"), Hex("#2F7BD6")).rectTransform;
            iceBadge.At(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -4f), new Vector2(48f, 22f));
            var iceText = Text("Text", iceBadge, "3", 14f, Color.white);
            iceText.rectTransform.Stretch(0, 0, 0, 2);

            var boards = Node("Boards", root).Stretch();
            for (var i = 0; i < 3; i++)
            {
                var b = Img("Board" + i, boards, Kit("board")).rectTransform;
                b.anchorMin = new Vector2(0f, 0.08f + i * 0.32f);
                b.anchorMax = new Vector2(1f, 0.08f + i * 0.32f + 0.24f);
                b.offsetMin = new Vector2(-2f, 0f);
                b.offsetMax = new Vector2(2f, 0f);
                b.localRotation = Quaternion.Euler(0f, 0f, i == 1 ? -3f : 2f);
            }

            Fit("Hammer", boards, Art("tools/hammer")).rectTransform.Center(Vector2.zero, new Vector2(26f, 40f));

            var view = root.gameObject.AddComponent<ShelfView>();
            Wire(view, ("_rect", root), ("_floors", floors), ("_cells", cells), ("_floorPrefab", floor),
                ("_rowsBadge", badge.gameObject), ("_rowsText", rowsText), ("_ties", ties.gameObject), ("_keyTag", key),
                ("_ice", ice.gameObject), ("_iceText", iceText), ("_boards", boards), ("_contentGroup", contentGroup));
            ties.gameObject.SetActive(false);
            key.gameObject.SetActive(false);
            ice.gameObject.SetActive(false);
            boards.gameObject.SetActive(false);
            badge.gameObject.SetActive(false);
            return root.gameObject;
        }

        private static GameObject BuildBasket()
        {
            var root = Node("BasketView", null).At(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(108f, 92f));
            var group = root.gameObject.AddComponent<CanvasGroup>();
            Img("Back", root, Art("basket/basket_back")).rectTransform.Stretch();
            var slots = new Object[3];
            for (var i = 0; i < 3; i++)
            {
                var s = Node("Slot" + i, root);
                s.At(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(12f + i * 25f, -4f), new Vector2(34f, 54f));
                slots[i] = s;
            }

            Img("Front", root, Art("basket/basket_front")).rectTransform.Stretch();
            var napkin = Fit("Napkin", root, Art("basket/napkin_done")).rectTransform;
            napkin.At(new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -40f), new Vector2(88f, 40f));
            napkin.gameObject.SetActive(false);
            var tag = Img("Tag", root, Kit("chip")).rectTransform;
            tag.At(new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(0f, 10f), new Vector2(64f, 30f));
            var icon = Fit("Icon", tag, null);
            icon.rectTransform.At(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(6f, 1f), new Vector2(16f, 26f));
            var count = Text("Count", tag, "0/3", 17f, Ink, TextAlignmentOptions.Left);
            count.rectTransform.Stretch(26, 4, 0, 3);
            count.fontStyle = FontStyles.Bold;
            var view = root.gameObject.AddComponent<BasketView>();
            Wire(view, ("_rect", root), ("_slots", slots), ("_tagIcon", icon), ("_tagCount", count), ("_napkin", napkin),
                ("_group", group));
            return root.gameObject;
        }
    }
}
