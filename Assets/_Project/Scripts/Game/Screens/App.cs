using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VampireSpawn.Core;
using VampireSpawn.Game.Data;

namespace VampireSpawn.Game.Screens
{
    // What outlives a scene (docs/dev/scene-split.md): the campaign save, where the camp screen reopens, and the battle to start.
    // Each screen is its own scene, so whatever a screen left behind (camera, fog, board) goes away with it.
    public static class App
    {
        public const string MenuScene = "Menu", CampScene = "Camp", BattleScene = "Battle";

        public static Campaign Campaign { get; private set; }
        public static bool Demo { get; private set; } = true;
        public static int MenuSel = 1;
        // The camp state picked last this run (stage-select.md 5-3 1), and the one the next camp scene opens on.
        public static CampNav? Nav, CampStart;
        public static Stage BattleStage { get; private set; }
        public static IEnumerable<Placement> Formation { get; private set; }
        public static bool StartNow { get; private set; }

        static bool ready;

        // Any scene can be the one Play starts in, so each screen calls this first; only the first call loads.
        public static void Ensure(GameDataAsset data)
        {
            if (ready) return;
            ready = true;
            UseData(data);
            Campaign = CampaignStore.Load();
        }

        public static void ResetCampaign()
        {
            Campaign = new Campaign();
            CampaignStore.Save(Campaign);
            Nav = null;
        }

        public static void UseCampaign(Campaign campaign)
        {
            Campaign = campaign;
            CampaignStore.Save(Campaign);
        }

        public static void GoMenu() => SceneManager.LoadScene(MenuScene);

        public static void GoCamp(CampNav start)
        {
            CampStart = start;
            SceneManager.LoadScene(CampScene);
        }

        // Upgrades for a demo battle; only the editor's test battle passes any.
        public static IReadOnlyList<string> DemoUpgrades { get; private set; }

        public static void GoBattle(Stage stage, bool demo, IEnumerable<Placement> formation = null, bool startNow = false, IReadOnlyList<string> demoUpgrades = null)
        {
            BattleStage = stage;
            Demo = demo;
            DemoUpgrades = demo ? demoUpgrades : null;
            Formation = formation;
            StartNow = startNow;
            SceneManager.LoadScene(BattleScene);
        }

        static void UseData(GameDataAsset data)
        {
            if (data == null) { Debug.LogError("App 에 게임 데이터 에셋이 없다"); return; }
            var problems = data.Problems();
            if (problems.Count > 0) Debug.LogError("게임 데이터에 문제가 있다:\n" + string.Join("\n", problems));
            if (data.Missing().Count == 0) GameData.Use(data.ToDataSet());
        }

        // ui-style.md 15-1: the canvas never shrinks below 1920x1080, only the spare axis grows. Panel.asset matches.
        public static RectTransform Canvas(Transform owner)
        {
            var go = new GameObject("UI", typeof(RectTransform), typeof(UnityEngine.Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.layer = 5;
            go.transform.SetParent(owner, false);
            go.GetComponent<UnityEngine.Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            var screen = Ui.Node(go.transform, "Screen");
            Ui.Stretch(screen);
            if (EventSystem.current == null) new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule)).transform.SetParent(owner, false);
            return screen;
        }
    }

    // A key's press edge from isPressed: wasPressedThisFrame is missed while the Editor sits in the background.
    public sealed class KeyEdges
    {
        readonly Dictionary<string, bool> down = new Dictionary<string, bool>();

        public bool Pressed(string name, bool isDown)
        {
            down.TryGetValue(name, out var was);
            down[name] = isDown;
            return isDown && !was;
        }
    }
}
