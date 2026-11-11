using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using VampireSpawn.Core;
using VampireSpawn.Game.Data;

namespace VampireSpawn.Game.Screens
{
    // The Menu scene: docs/ui/main-menu.md over the Synty Dark Fantasy cathedral demo scene (ui-style.md 11-1).
    public sealed class MenuFlow : MonoBehaviour
    {
        public const string BackdropScene = "Assets/Synty/PolygonDarkFantasy/Scenes/Demo_Cathedral_01.unity";
        // Turns the demo shot so the castle and moon sit right of the menu column (main-menu.md 8).
        const float BackdropYaw = -14f;

        [Tooltip("게임이 쓰는 데이터. 비었거나 문제가 있으면 HTML 기본값으로 돈다")] public GameDataAsset data;
        public BoardView board;
        public Camera viewCamera;

        UIDocument menu;
        readonly KeyEdges keys = new KeyEdges();

        void Start()
        {
            App.Ensure(data);
            Sound.Use(board.art.sound);
            Sound.Music(s => s.menuMusic);
            Sound.Ambience(null);
            LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
            // The demo scene's own camera (and its AudioListener) takes over.
            if (viewCamera != null) viewCamera.gameObject.SetActive(false);
            SceneManager.sceneLoaded += OnBackdrop;
            SceneManager.LoadScene(BackdropScene, LoadSceneMode.Additive);
            Show(true);
        }

        void OnDestroy()
        {
            LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
            SceneManager.sceneLoaded -= OnBackdrop;
        }

        // Sky, fog and ambient light come from the active scene only.
        void OnBackdrop(Scene s, LoadSceneMode _)
        {
            if (s.path != BackdropScene) return;
            SceneManager.SetActiveScene(s);
            foreach (var root in s.GetRootGameObjects())
                foreach (var cam in root.GetComponentsInChildren<Camera>())
                    cam.transform.Rotate(0f, BackdropYaw, 0f, Space.World);
        }

        // Only the main menu offers a language switch (docs/ui/localization.md 9).
        void OnLocaleChanged(UnityEngine.Localization.Locale _) => Show(false);

        public void Show(bool fade)
        {
            if (menu != null) Destroy(menu.gameObject);
            System.Action<int> hover = i => { if (App.MenuSel != i) { App.MenuSel = i; Show(false); } };
            menu = MenuToolkit.Build(transform, board.art, App.Campaign, Demo, Camp, App.MenuSel, hover);
            if (fade) Fx.FadeIn(menu.rootVisualElement);
        }

        void Demo() => App.GoBattle(GameData.Stages[0], demo: true);

        void Camp() => App.GoCamp(CampNav.FromMenu(App.Campaign, App.Nav));

        void Update()
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb == null) return;
            var up = keys.Pressed("up", kb.upArrowKey.isPressed);
            var down = keys.Pressed("down", kb.downArrowKey.isPressed);
            var enter = keys.Pressed("enter", kb.enterKey.isPressed || kb.numpadEnterKey.isPressed);
            // Campaign (1) sits above the demo (0) in the list.
            if ((up && App.MenuSel != 1) || (down && App.MenuSel != 0)) { App.MenuSel = up ? 1 : 0; Show(false); }
            else if (enter) { if (App.MenuSel == 0) Demo(); else Camp(); }
        }
    }
}
