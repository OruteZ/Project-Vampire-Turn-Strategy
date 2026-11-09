using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;
using VampireSpawn.Core;
using VampireSpawn.Game.Data;

namespace VampireSpawn.Game.Screens
{
    // The Menu scene: docs/ui/main-menu.md over the first stage's board, blurred.
    public sealed class MenuFlow : MonoBehaviour
    {
        [Tooltip("게임이 쓰는 데이터. 비었거나 문제가 있으면 HTML 기본값으로 돈다")] public GameDataAsset data;
        public BoardView board;
        public Camera viewCamera;

        RectTransform screen;
        RenderTexture backdropTex;
        UnityEngine.UIElements.UIDocument toolkitMenu;
        readonly KeyEdges keys = new KeyEdges();

        void Start()
        {
            App.Ensure(data);
            Sound.Use(board.art.sound);
            Sound.Music(s => s.menuMusic);
            Sound.Ambience(null);
            screen = App.Canvas(transform);
            LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
            Show(true);
        }

        void OnDestroy()
        {
            LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
            if (backdropTex != null) backdropTex.Release();
        }

        // Only the main menu offers a language switch (docs/ui/localization.md 9).
        void OnLocaleChanged(UnityEngine.Localization.Locale _) => Show(false);

        public void Show(bool fade)
        {
            if (toolkitMenu != null) { Destroy(toolkitMenu.gameObject); toolkitMenu = null; }
            Ui.Clear(screen);
            if (!screen.TryGetComponent<Image>(out var bg)) bg = screen.gameObject.AddComponent<Image>();
            bg.color = Ui.Ink;
            Backdrop(Campaign.Stages[0]);
            System.Action<int> hover = i => { if (App.MenuSel != i) { App.MenuSel = i; Show(false); } };
            if (MenuToolkit.On && board.art.panel != null) toolkitMenu = MenuToolkit.Build(transform, board.art, App.Campaign, Demo, Camp, App.MenuSel, hover);
            else MenuScreen.Build(screen, App.Campaign, Demo, Camp, board.art, App.MenuSel, hover);
            if (fade) Fx.FadeIn(screen);
        }

        // ui-style.md 11-1: the stage board, blurred and darkened 60%. A tiny render target upscaled is the blur.
        void Backdrop(Stage s)
        {
            if (viewCamera == null || board == null) return;
            if (backdropTex == null) backdropTex = new RenderTexture(240, 135, 16) { filterMode = FilterMode.Bilinear };
            if (board.Stage != s) board.Show(s);
            viewCamera.targetTexture = backdropTex;
            var img = Ui.Node(screen, "Backdrop").gameObject.AddComponent<RawImage>();
            img.texture = backdropTex;
            img.color = new Color(0.4f, 0.4f, 0.4f);
            img.raycastTarget = false;
            Ui.Stretch(img.rectTransform);
            img.transform.SetAsFirstSibling();
        }

        void Demo() => App.GoBattle(GameData.Stages[0], demo: true);

        void Camp() => App.GoCamp(CampNav.FromMenu(App.Campaign, App.Nav));

        void Update()
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb == null) return;
            var left = keys.Pressed("left", kb.leftArrowKey.isPressed);
            var right = keys.Pressed("right", kb.rightArrowKey.isPressed);
            var enter = keys.Pressed("enter", kb.enterKey.isPressed || kb.numpadEnterKey.isPressed);
            if ((left && App.MenuSel != 0) || (right && App.MenuSel != 1)) { App.MenuSel = left ? 0 : 1; Show(false); }
            else if (enter) { if (App.MenuSel == 0) Demo(); else Camp(); }
        }
    }
}
