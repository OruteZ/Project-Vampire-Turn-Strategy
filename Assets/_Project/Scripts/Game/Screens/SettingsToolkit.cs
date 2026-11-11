using System;
using UnityEngine.UIElements;

namespace VampireSpawn.Game.Screens
{
    // docs/ui/settings.md: the window over the main menu. A picked value is saved at once and the menu rebuilds through changed.
    public static class SettingsToolkit
    {
        public static void Build(VisualElement screen, ArtCatalog art, Label tip, Action close, Action changed)
        {
            var veil = ToolkitKit.E(null, "settings-veil", true);
            screen.Insert(screen.IndexOf(tip), veil);
            veil.RegisterCallback<ClickEvent>(e => { if (e.target == veil) close(); });
            var box = ToolkitKit.E(veil, "settings", true);
            ToolkitKit.Frame(box, art, Ui.Steel);
            ToolkitKit.L(box, Loc.T("settings.title"), "settings-title display");

            Row(box, art, tip, "settings.camera", new[] { Loc.T("settings.on"), Loc.T("settings.off") },
                GameSettings.CameraFx ? 0 : 1, i => GameSettings.CameraFx = i == 0, changed);
            Row(box, art, tip, "settings.shake", new[] { Loc.T("settings.off"), Loc.T("settings.weak"), Loc.T("settings.normal") },
                Array.IndexOf(GameSettings.Shakes, GameSettings.Shake), i => GameSettings.Shake = GameSettings.Shakes[i], changed);
            Row(box, art, tip, "settings.speed", Array.ConvertAll(GameSettings.Speeds, s => "×" + s),
                Array.IndexOf(GameSettings.Speeds, GameSettings.Speed), i => GameSettings.Speed = GameSettings.Speeds[i], changed);

            var done = ToolkitKit.Btn(box, art, Loc.T("settings.close"), BtnKind.Normal, "Esc", close);
            done.AddToClassList("settings-close");
            ToolkitKit.KeepClearOfKey(done, "Esc");
            ToolkitKit.UseFonts(screen);
        }

        static void Row(VisualElement box, ArtCatalog art, Label tip, string key, string[] values, int on, Action<int> pick, Action changed)
        {
            var row = ToolkitKit.E(box, "settings-row", true);
            ToolkitKit.L(row, Loc.T(key), "settings-name");
            ToolkitKit.Tip(row, Loc.T(key + "Tip"), tip, 300);
            var chips = ToolkitKit.E(row, "settings-values");
            for (var i = 0; i < values.Length; i++)
            {
                var n = i;
                ToolkitKit.Btn(chips, art, values[i], i == on ? BtnKind.Primary : BtnKind.Normal, null, () => { pick(n); changed(); })
                    .AddToClassList("settings-value");
            }
        }
    }
}
