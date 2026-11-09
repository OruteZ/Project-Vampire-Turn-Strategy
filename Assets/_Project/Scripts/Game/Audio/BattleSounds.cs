using VampireSpawn.Core;

namespace VampireSpawn.Game
{
    // docs/audio/sounds.md 5: which slot a battle event plays. Unit sounds (6) are played by BattleView next to their animation.
    public static class BattleSounds
    {
        public static Cue Of(SoundCatalog s, BattleEvent e) => e.Kind switch
        {
            BattleEventKind.Banner => e.BannerKind switch
            {
                BannerKind.PlayerTurn => s.playerTurn,
                BannerKind.EnemyTurn => s.enemyTurn,
                BannerKind.Coffin => s.coffin,
                _ => s.fullMoon,
            },
            BattleEventKind.Popup => e.PopupKind switch
            {
                PopupKind.Heal => s.mend,
                PopupKind.Execute => s.execute,
                PopupKind.Bury => s.bury,
                PopupKind.Drain => s.drain,
                PopupKind.DrainFailed => s.drainFailed,
                _ => null,
            },
            // Only the gravedigger's rite lowers the moon with an event; a kill raises it silently.
            BattleEventKind.MoonChanged => s.moonWane,
            BattleEventKind.Rose => s.rise,
            _ => null,
        };
    }
}
