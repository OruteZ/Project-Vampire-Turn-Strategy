using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VampireSpawn.Game
{
    // Plays SoundCatalog slots. Callers pick a slot with a selector (s => s.click), so a renamed or removed slot is a compile error.
    public static class Sound
    {
        const float FadeSeconds = 0.25f;

        static SoundCatalog bank;
        static Host host;

        public static void Use(SoundCatalog catalog) => bank = catalog;

        public static void Play(Func<SoundCatalog, Cue> pick)
        {
            if (bank != null) Play(pick(bank));
        }

        public static void Play(Cue cue)
        {
            var clip = cue?.Next();
            if (clip != null) Get().sfx.PlayOneShot(clip, cue.volume);
        }

        // A unit's own slot, or SoundCatalog.unitDefault's when the unit leaves it empty.
        public static void Play(UnitSounds own, Func<UnitSounds, Cue> pick)
        {
            var cue = own != null ? pick(own) : null;
            if ((cue == null || cue.Empty) && bank != null && bank.unitDefault != null) cue = pick(bank.unitDefault);
            Play(cue);
        }

        // Pass null to fade the music out. The same clip keeps playing across scenes.
        public static void Music(Func<SoundCatalog, Cue> pick) => Swap(Get().music, pick);

        public static void Ambience(Func<SoundCatalog, Cue> pick) => Swap(Get().ambience, pick);

        // A looping sound that sits at a place in the world, e.g. a brazier. Heard within `range` of the listener.
        public static void Loop(GameObject at, Func<SoundCatalog, Cue> pick, float near, float range)
        {
            var cue = bank != null && Application.isPlaying ? pick(bank) : null;
            var clip = cue?.Next();
            if (clip == null) return;
            var src = at.AddComponent<AudioSource>();
            src.clip = clip;
            src.volume = cue.volume;
            src.loop = true;
            src.spatialBlend = 1f;
            src.rolloffMode = AudioRolloffMode.Linear;
            src.minDistance = near;
            src.maxDistance = range;
            // Start every loop at a different point so a row of braziers does not crackle in unison; offset from position, not random.
            src.timeSamples = Mathf.Abs(Mathf.RoundToInt(at.transform.position.x * 7919f + at.transform.position.z * 104729f)) % clip.samples;
            src.Play();
        }

        static void Swap(AudioSource src, Func<SoundCatalog, Cue> pick)
        {
            var cue = pick != null && bank != null ? pick(bank) : null;
            var clip = cue?.Next();
            if (clip == src.clip && src.isPlaying) return;
            host.Swap(src, clip, cue?.volume ?? 0f);
        }

        static Host Get()
        {
            if (host != null) return host;
            var go = new GameObject("Sound");
            UnityEngine.Object.DontDestroyOnLoad(go);
            host = go.AddComponent<Host>();
            host.sfx = go.AddComponent<AudioSource>();
            host.music = go.AddComponent<AudioSource>();
            host.ambience = go.AddComponent<AudioSource>();
            host.music.loop = host.ambience.loop = true;
            return host;
        }

        sealed class Host : MonoBehaviour
        {
            public AudioSource sfx, music, ambience;
            readonly Dictionary<AudioSource, Coroutine> fading = new Dictionary<AudioSource, Coroutine>();

            public void Swap(AudioSource src, AudioClip clip, float volume)
            {
                if (fading.TryGetValue(src, out var running) && running != null) StopCoroutine(running);
                fading[src] = StartCoroutine(Fade(src, clip, volume));
            }

            IEnumerator Fade(AudioSource src, AudioClip clip, float volume)
            {
                for (var v = src.volume; src.isPlaying && v > 0f; v -= Time.unscaledDeltaTime / FadeSeconds)
                {
                    src.volume = v;
                    yield return null;
                }
                src.Stop();
                src.clip = clip;
                if (clip == null) yield break;
                src.volume = 0f;
                src.Play();
                for (var t = 0f; t < 1f; t += Time.unscaledDeltaTime / FadeSeconds)
                {
                    src.volume = volume * t;
                    yield return null;
                }
                src.volume = volume;
            }
        }
    }
}
