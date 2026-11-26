using UnityEngine;

namespace VampireSpawn.Game
{
    // docs/ui/settings.md: player options kept in PlayerPrefs, apart from the campaign save.
    public static class GameSettings
    {
        public static readonly float[] Speeds = { 1f, 2f, 4f };
        public static readonly float[] Shakes = { 0f, 0.5f, 1f };
        // docs/ui/settings.md: small, medium, large; 0.64 keeps the tallest deploy roster (nine kinds) clear of the stage title at 1080 (#150 C2). Read when a battle starts (BattleFlow.HudZoom).
        public static readonly float[] HudZooms = { 0.55f, 0.6f, 0.64f };

        static bool? cameraFx;
        static float? shake;

        public static bool CameraFx
        {
            get => cameraFx ??= PlayerPrefs.GetInt("camera.fx", 1) != 0;
            set { cameraFx = value; PlayerPrefs.SetInt("camera.fx", value ? 1 : 0); PlayerPrefs.Save(); }
        }

        public static float Shake
        {
            get => shake ??= Mathf.Clamp01(PlayerPrefs.GetFloat("camera.shake", 1f));
            set { shake = value; PlayerPrefs.SetFloat("camera.shake", value); PlayerPrefs.Save(); }
        }

        public static float HudZoom
        {
            get => Mathf.Clamp(PlayerPrefs.GetFloat("battle.hudZoom", HudZooms[0]), HudZooms[0], HudZooms[HudZooms.Length - 1]);
            set { PlayerPrefs.SetFloat("battle.hudZoom", value); PlayerPrefs.Save(); }
        }

        // Same key the battle's F key has always used (battle-hud.md 4-5).
        public static float Speed
        {
            get => Mathf.Clamp(PlayerPrefs.GetFloat("battle.speed", 1f), 1f, 4f);
            set { PlayerPrefs.SetFloat("battle.speed", value); PlayerPrefs.Save(); }
        }
    }
}
