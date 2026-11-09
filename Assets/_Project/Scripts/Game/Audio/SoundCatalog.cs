using System;
using UnityEngine;

namespace VampireSpawn.Game
{
    // One sound slot. An empty slot plays nothing, so the game runs with any part of docs/audio/sounds.md missing.
    [Serializable]
    public sealed class Cue
    {
        [Tooltip("변형. 무작위 없이 차례로 돈다 (docs/audio/sounds.md 1)")] public AudioClip[] clips;
        [Range(0f, 1f)] public float volume = 1f;

        [NonSerialized] int next;

        public bool Empty => clips == null || clips.Length == 0;

        public AudioClip Next()
        {
            if (Empty) return null;
            next %= clips.Length;
            return clips[next++];
        }
    }

    // docs/audio/sounds.md 6. Each UnitLook carries one; an empty slot falls back to SoundCatalog.unitDefault.
    [Serializable]
    public sealed class UnitSounds
    {
        public Cue attack, hurt, death, step;
    }

    // Every sound the game plays, as typed slots (docs/audio/sounds.md). Code picks a slot by field, never by name.
    [CreateAssetMenu(fileName = "SoundCatalog", menuName = "VampireSpawn/Sound Catalog")]
    public sealed class SoundCatalog : ScriptableObject
    {
        [Header("음악 (2)")] public Cue menuMusic;
        public Cue campMusic, battleMusic;

        [Header("환경음 (3)")] public Cue battleAmbience;
        public Cue campAmbience;
        [Tooltip("화로 칸마다 그 자리에서 도는 불 소리")] public Cue brazier;

        [Header("UI (4)")] public Cue click;
        public Cue hover, denied, select, place, purchase, march;

        [Header("전투 진행 (5)")] public Cue playerTurn;
        public Cue enemyTurn, coffin, fullMoon, moonWane, execute, mend, bury, drain, drainFailed, rise, victory, defeat;

        [Header("유닛 (6)")]
        [Tooltip("UnitLook 의 빈 칸 대신")] public UnitSounds unitDefault;
        [Tooltip("시체에서 일어난 아군. 원래 병종 소리 대신")] public UnitSounds spawn;
    }
}
