using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.UIElements;
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

        RenderTexture backdropTex;
        UIDocument menu;
        bool settingsOpen;
        readonly KeyEdges keys = new KeyEdges();

        void Start()
        {
            App.Ensure(data);
            Sound.Use(board.art.sound);
            Sound.Music(s => s.menuMusic);
            Sound.Ambience(null);
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
            if (menu != null) Destroy(menu.gameObject);
            Backdrop(Campaign.Stages[0]);
            System.Action<int> hover = i => { if (App.MenuSel != i) { App.MenuSel = i; Show(false); } };
            menu = MenuToolkit.Build(transform, board.art, App.Campaign, backdropTex, Demo, Camp, App.MenuSel, hover, () => Settings(true));
            var root = menu.rootVisualElement;
            if (settingsOpen) SettingsToolkit.Build(root.Q("menu"), board.art, root.Q<Label>("tip"), () => Settings(false), () => Show(false));
            if (fade) Fx.FadeIn(menu.rootVisualElement);
        }

        // ui-style.md 11-1: a tiny render target upscaled is the blur.
        void Backdrop(Stage s)
        {
            if (viewCamera == null || board == null) return;
            if (backdropTex == null) backdropTex = new RenderTexture(240, 135, 16) { filterMode = FilterMode.Bilinear };
            if (board.Stage != s) board.Show(s);
            viewCamera.targetTexture = backdropTex;
        }

        void Demo() => App.GoBattle(GameData.Stages[0], demo: true);

        void Camp() => App.GoCamp(CampNav.FromMenu(App.Campaign, App.Nav));

        void Settings(bool open) { settingsOpen = open; Show(false); }

        void Update()
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb == null) return;
            var esc = keys.Pressed("esc", kb.escapeKey.isPressed);
            if (settingsOpen) { if (esc) Settings(false); return; }
            var left = keys.Pressed("left", kb.leftArrowKey.isPressed);
            var right = keys.Pressed("right", kb.rightArrowKey.isPressed);
            var enter = keys.Pressed("enter", kb.enterKey.isPressed || kb.numpadEnterKey.isPressed);
            if ((left && App.MenuSel != 0) || (right && App.MenuSel != 1)) { App.MenuSel = left ? 0 : 1; Show(false); }
            else if (enter) { if (App.MenuSel == 0) Demo(); else Camp(); }
        }
    }
}
