using System.Linq;
using VampireSpawn.Core;

namespace VampireSpawn.Game.Screens
{
    // Icon names (docs/art/ui-style.md 9, 14-2) and shared screen strings.
    public static class Kit
    {
        public static class Icon
        {
            public const string Hp = "ICON_DarkFantasy_Stat_Health_01_Clean", Mov = "ICON_DarkFantasy_Stat_Speed_01_Clean", Atk = "ICON_DarkFantasy_Status_Attack_01_Clean",
                Def = "ICON_DarkFantasy_Status_Defense_01_Clean", Rng = "ICON_DarkFantasy_Stat_Accuracy_01_Clean", Fang = "ICON_DarkFantasy_Status_Vampiric_01_Clean",
                Dead = "ICON_DarkFantasy_Status_Dead_01_Clean", BoardDef = "ICON_DarkFantasy_Status_Defense_01_Stroke", Target = "ICON_DarkFantasy_Status_Targeted_01_Stroke",
                Gold = "ICON_DarkFantasyMenus_Currency_Coin_01_Clean", Silver = "ICON_DarkFantasy_Inventory_Currency_01_Clean", Pop = "ICON_DarkFantasy_Inventory_Helmets_01_Clean",
                Boss = "ICON_DarkFantasy_Map_Monster_01_Clean", Star = "ICON_DarkFantasy_Map_Star_01_Clean", Skulls = "ICON_DarkFantasy_Map_Skull_01_Clean",
                Turn = "ICON_DarkFantasy_Status_Time_01_Clean", Lock = "ICON_DarkFantasyMenus_Menu_Lock_Closed_01_Clean", Trophy = "ICON_DarkFantasyMenus_Menu_Trophy_01_Clean",
                Retry = "ICON_DarkFantasyMenus_Menu_Refresh_01_Clean", Leave = "ICON_DarkFantasyMenus_Menu_Quit_01_Clean", Rules = "ICON_DarkFantasy_Inventory_Notes_01_Clean",
                Up = "ICON_DarkFantasy_Status_Up_01_Clean", Wait = "ICON_DarkFantasy_Status_Down_01_Clean", Flag = "ICON_DarkFantasy_Map_Flag_01_Clean", Home = "ICON_DarkFantasy_Map_Home_01_Clean", Sword = "ICON_SM_Wep_Sword_01_DarkFantasy",
                Heal = "ICON_DarkFantasy_Status_Health_01_Clean";

            public static readonly string[] All = { Hp, Mov, Atk, Def, Rng, Fang, Dead, Target, Gold, Silver, Pop, Boss, Star, Skulls, Turn, Lock, Trophy, Retry, Leave, Rules, Up, Wait, Flag, Home, Sword, Heal };
        }

        // docs/systems/goals.md 5-1: the goal, and under it that the vampire still wins.
        public static string Objective(Stage s) =>
            Loc.Text(s.Objective) + (s.AlsoWins.IsEmpty ? "" : "\n<size=80%>" + Loc.Text(s.AlsoWins) + "</size>");

        // docs/content/enemies.md 7: "Minions n, boss" then the count of each minion kind present.
        public static string EnemyList(Stage s)
        {
            var minions = s.Minions;
            var kinds = GameData.Minions.Select(m => (m, n: minions.Count(e => e.Kind == m.Key))).Where(x => x.n > 0).Select(x => Loc.Text(x.m.Name) + " " + x.n);
            return Loc.T("battle.enemyList", minions.Length, s.BossName) + " · " + string.Join(" · ", kinds);
        }
    }
}
