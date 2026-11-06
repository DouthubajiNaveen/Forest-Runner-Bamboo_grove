using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ForestRunner
{
    public enum Sfx { Click, Coin, Jump, Slide, Land, PowerUp, ShieldBreak, Hit, GameOver, Splash, Unlock, Buy, Denied, Swish }

    /// <summary>
    /// Persistent audio hub. Every sound (sfx, bird calls, ambience, music) is synthesised in code
    /// at start-up, so the project needs no audio files. To use real audio instead, drop clips named
    /// like the keys below into Assets/Resources/Audio/ (e.g. "coin", "jump", "music_day") and they
    /// override the generated ones automatically.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        const int SR = 22050;
        AudioSource music, ambience, runLoop;
        AudioSource[] sfxPool;
        int sfxIndex;
        readonly Dictionary<Sfx, AudioClip> clips = new Dictionary<Sfx, AudioClip>();
        readonly List<AudioClip> birds = new List<AudioClip>();
        AudioClip stepClip, ambDay, ambNight, musicDay, musicNight;
        bool nightMode;
        bool ready;

        public static AudioManager Ensure()
        {
            if (Instance != null) return Instance;
            GameObject go = new GameObject("AudioManager");
            DontDestroyOnLoad(go);
            return go.AddComponent<AudioManager>();
        }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            music = NewSource("Music", true);
            ambience = NewSource("Ambience", true);
            runLoop = NewSource("RunLoop", true);
            sfxPool = new AudioSource[8];
            for (int i = 0; i < sfxPool.Length; i++) sfxPool[i] = NewSource("Sfx" + i, false);

            Generate();
            ApplyVolumes();
            ready = true;
            StartCoroutine(BirdLoop());
        }

        AudioSource NewSource(string n, bool loop)
        {
            GameObject g = new GameObject(n);
            g.transform.SetParent(transform, false);
            AudioSource s = g.AddComponent<AudioSource>();
            s.playOnAwake = false;
            s.loop = loop;
            s.spatialBlend = 0f;
            return s;
        }

        // ------------------------------------------------------------------ public API

        public void Play(Sfx s, float volume, float pitch)
        {
            if (!ready) return;
            AudioClip c;
            if (!clips.TryGetValue(s, out c) || c == null) return;
            AudioSource src = sfxPool[sfxIndex];
            sfxIndex = (sfxIndex + 1) % sfxPool.Length;
            src.clip = c;
            src.pitch = pitch;
            src.volume = Mathf.Clamp01(volume) * SaveSystem.SfxVolume;
            src.Play();
        }

        public void PlayClick() { Play(Sfx.Click, 0.8f, 1f); }

        public void SetRunning(bool on, float speed)
        {
            if (!ready) return;
            if (on)
            {
                runLoop.pitch = Mathf.Clamp(0.8f + speed / 40f, 0.8f, 1.7f);
                runLoop.volume = 0.35f * SaveSystem.SfxVolume;
                if (!runLoop.isPlaying) runLoop.Play();
            }
            else if (runLoop.isPlaying) runLoop.Stop();
        }

        public void StartLevelAudio(bool night, int levelIndex)
        {
            if (!ready) return;
            nightMode = night;
            AudioClip m = night ? musicNight : musicDay;
            if (music.clip != m || !music.isPlaying) { music.clip = m; music.Play(); }
            AudioClip a = night ? ambNight : ambDay;
            if (ambience.clip != a || !ambience.isPlaying) { ambience.clip = a; ambience.Play(); }
        }

        public void StopRunLoop() { SetRunning(false, 0f); }

        public void ApplyVolumes()
        {
            if (music != null) music.volume = 0.45f * SaveSystem.MusicVolume;
            if (ambience != null) ambience.volume = 0.5f * SaveSystem.SfxVolume;
        }

        IEnumerator BirdLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(UnityEngine.Random.Range(2.5f, 7f));
                if (!nightMode && birds.Count > 0)
                {
                    AudioClip c = birds[UnityEngine.Random.Range(0, birds.Count)];
                    AudioSource src = sfxPool[sfxIndex];
                    sfxIndex = (sfxIndex + 1) % sfxPool.Length;
                    src.clip = c;
                    src.pitch = UnityEngine.Random.Range(0.9f, 1.15f);
                    src.volume = UnityEngine.Random.Range(0.25f, 0.5f) * SaveSystem.SfxVolume;
                    src.Play();
                }
            }
        }

        // ------------------------------------------------------------------ synthesis

        static AudioClip Make(string name, float dur, Func<float, float> f, bool loop)
        {
            AudioClip over = Resources.Load<AudioClip>("Audio/" + name);
            if (over != null) return over;
            int n = Mathf.CeilToInt(dur * SR);
            float[] d = new float[n];
            for (int i = 0; i < n; i++) d[i] = Mathf.Clamp(f(i / (float)SR), -1f, 1f);
            AudioClip c = AudioClip.Create(name, n, 1, SR, false);
            c.SetData(d, 0);
            return c;
        }

        const float TAU = Mathf.PI * 2f;

        void Generate()
        {
            System.Random rnd = new System.Random(1234);
            Func<float> noise = delegate { return (float)(rnd.NextDouble() * 2.0 - 1.0); };

            clips[Sfx.Click] = Make("click", 0.08f, t => Mathf.Sin(TAU * 820f * t) * Mathf.Exp(-t * 55f) * 0.5f, false);
            clips[Sfx.Coin] = Make("coin", 0.4f, delegate (float t)
            {
                float lt = t < 0.08f ? t : t - 0.08f;
                float fr = t < 0.08f ? 987.77f : 1318.51f;
                float e = Mathf.Exp(-lt * 12f);
                return (0.45f * Mathf.Sin(TAU * fr * t) + 0.15f * Mathf.Sin(TAU * fr * 2f * t)) * e;
            }, false);

            float ph1 = 0f;
            clips[Sfx.Jump] = Make("jump", 0.28f, delegate (float t)
            {
                float f = 280f + 520f * (t / 0.28f);
                ph1 += TAU * f / SR;
                return 0.4f * Mathf.Sin(ph1) * (1f - t / 0.28f);
            }, false);

            float lp1 = 0f;
            clips[Sfx.Slide] = Make("slide", 0.55f, delegate (float t)
            {
                lp1 += (noise() - lp1) * 0.12f;
                return lp1 * Mathf.Sin(Mathf.PI * t / 0.55f) * 1.6f;
            }, false);

            float lp2 = 0f;
            clips[Sfx.Swish] = Make("swish", 0.16f, delegate (float t)
            {
                lp2 += (noise() - lp2) * 0.3f;
                return lp2 * Mathf.Sin(Mathf.PI * t / 0.16f) * 0.9f;
            }, false);

            clips[Sfx.Land] = Make("land", 0.22f, t => (Mathf.Sin(TAU * 85f * t) * 0.6f + noise() * 0.15f) * Mathf.Exp(-t * 22f), false);

            float[] arp = new float[] { 523.25f, 659.25f, 783.99f, 1046.5f };
            clips[Sfx.PowerUp] = Make("powerup", 0.7f, delegate (float t)
            {
                int k = Mathf.Min(3, (int)(t / 0.08f));
                float lt = t - k * 0.08f;
                float e = Mathf.Exp(-lt * (k == 3 ? 4f : 12f));
                return 0.38f * (Mathf.Sin(TAU * arp[k] * t) + 0.3f * Mathf.Sin(TAU * arp[k] * 2f * t)) * e;
            }, false);

            clips[Sfx.ShieldBreak] = Make("shield_break", 0.5f, delegate (float t)
            {
                float f = 1800f - 1400f * t;
                return (0.3f * Mathf.Sin(TAU * f * t) + noise() * 0.35f) * Mathf.Exp(-t * 8f);
            }, false);

            float lp3 = 0f;
            clips[Sfx.Hit] = Make("hit", 0.55f, delegate (float t)
            {
                lp3 += (noise() - lp3) * 0.4f;
                return (lp3 * 0.8f + Mathf.Sin(TAU * 68f * t) * 0.8f) * Mathf.Exp(-t * 8f);
            }, false);

            float[] fall = new float[] { 440f, 392f, 349.23f, 293.66f };
            clips[Sfx.GameOver] = Make("gameover", 1.3f, delegate (float t)
            {
                int k = Mathf.Min(3, (int)(t / 0.28f));
                float lt = t - k * 0.28f;
                float fr = fall[k] * (1f + 0.01f * Mathf.Sin(TAU * 6f * t));
                return 0.4f * Mathf.Sin(TAU * fr * t) * Mathf.Exp(-lt * (k == 3 ? 2.5f : 4f));
            }, false);

            float lp4 = 0f;
            clips[Sfx.Splash] = Make("splash", 0.7f, delegate (float t)
            {
                lp4 += (noise() - lp4) * 0.2f;
                return lp4 * 1.4f * Mathf.Exp(-t * 5f);
            }, false);

            float[] up = new float[] { 392f, 523.25f, 659.25f, 783.99f, 1046.5f };
            clips[Sfx.Unlock] = Make("unlock", 0.9f, delegate (float t)
            {
                int k = Mathf.Min(4, (int)(t / 0.1f));
                float lt = t - k * 0.1f;
                return 0.35f * Mathf.Sin(TAU * up[k] * t) * Mathf.Exp(-lt * (k == 4 ? 3f : 9f));
            }, false);

            clips[Sfx.Buy] = Make("buy", 0.4f, delegate (float t)
            {
                float lt = t < 0.1f ? t : t - 0.1f;
                float fr = t < 0.1f ? 1568f : 2093f;
                return 0.35f * Mathf.Sin(TAU * fr * t) * Mathf.Exp(-lt * 10f);
            }, false);

            clips[Sfx.Denied] = Make("denied", 0.25f, t => Mathf.Sign(Mathf.Sin(TAU * 110f * t)) * 0.2f * (1f - t / 0.25f), false);

            // footsteps loop: two thuds per cycle
            float lp5 = 0f;
            stepClip = Make("footsteps", 0.4f, delegate (float t)
            {
                float lt = t % 0.2f;
                lp5 += (noise() - lp5) * 0.25f;
                return (lp5 * 0.7f + Mathf.Sin(TAU * 120f * lt) * 0.4f) * Mathf.Exp(-lt * 40f);
            }, true);

            // bird calls
            float[][] birdNotes = new float[][]
            {
                new float[] { 3200f, 3800f, 3400f },
                new float[] { 2500f, 2900f, 2500f, 3100f },
                new float[] { 4200f, 3600f },
                new float[] { 2800f, 3300f, 3900f, 3300f }
            };
            for (int b = 0; b < birdNotes.Length; b++)
            {
                float[] notes = birdNotes[b];
                float noteLen = 0.09f;
                float dur = notes.Length * (noteLen + 0.03f);
                float ph = 0f;
                birds.Add(Make("bird" + b, dur, delegate (float t)
                {
                    int k = Mathf.Min(notes.Length - 1, (int)(t / (noteLen + 0.03f)));
                    float lt = t - k * (noteLen + 0.03f);
                    if (lt > noteLen) return 0f;
                    float f = notes[k] + 600f * (lt / noteLen) + 80f * Mathf.Sin(TAU * 35f * lt);
                    ph += TAU * f / SR;
                    return 0.25f * Mathf.Sin(ph) * Mathf.Sin(Mathf.PI * lt / noteLen);
                }, false));
            }

            // ambience: wind + rustle (+ crickets at night). Frequencies are integer cycles per loop for seamless looping.
            float lpw = 0f, lpr = 0f;
            ambDay = Make("ambience_day", 8f, delegate (float t)
            {
                lpw += (noise() - lpw) * 0.015f;
                lpr += (noise() - lpr) * 0.5f;
                float wind = lpw * (0.55f + 0.45f * Mathf.Sin(TAU * t / 8f)) * 6f;
                float gust = Mathf.Max(0f, Mathf.Sin(TAU * 3f * t / 8f) * Mathf.Sin(TAU * 7f * t / 8f));
                float rustle = (noise() - lpr) * 0.12f * gust;
                return wind * 0.5f + rustle;
            }, true);
            float lpn = 0f;
            ambNight = Make("ambience_night", 8f, delegate (float t)
            {
                lpn += (noise() - lpn) * 0.012f;
                float wind = lpn * (0.5f + 0.5f * Mathf.Sin(TAU * t / 8f)) * 5f;
                float gate = Mathf.Sin(TAU * 176f * t / 8f) > 0.2f ? 1f : 0f;
                float env = 0.5f + 0.5f * Mathf.Sin(TAU * 2f * t / 8f);
                float cricket = Mathf.Sin(TAU * 4200f * t) * gate * env * 0.06f;
                return wind * 0.45f + cricket;
            }, true);

            musicDay = MakeMusic("music_day", 96f, 1.0f, new int[] { 0, 2, 4, 2, 3, 4, 2, 1, 0, 2, 3, 4, 6, 4, 3, 2, 0, 2, 4, 5, 4, 3, 2, 0, 1, 2, 4, 3, 2, 1, 0, 0 });
            musicNight = MakeMusic("music_night", 76f, 0.84f, new int[] { 0, 1, 3, 1, 2, 3, 1, 0, 3, 4, 3, 2, 1, 0, 1, 3, 4, 6, 4, 3, 1, 2, 3, 1, 0, 1, 2, 1, 0, 0, 0, 0 });
        }

        /// <summary>Soft pentatonic flute melody over pad chords with a light taiko pulse; 8 bars.</summary>
        AudioClip MakeMusic(string name, float bpm, float pitchMul, int[] melody)
        {
            float beat = 60f / bpm;
            float total = beat * 32f;
            float[] scale = new float[] { 220f, 261.63f, 293.66f, 329.63f, 392f, 440f, 523.25f, 587.33f, 659.25f };
            float[] chordRoot = new float[] { 110f, 87.31f, 98f, 82.41f };
            System.Random rnd = new System.Random(77);
            float lpn = 0f;
            return Make(name, total, delegate (float t)
            {
                float v = 0f;
                int bi = (int)(t / beat);
                float bt = t - bi * beat;
                int note = melody[Mathf.Min(melody.Length - 1, bi)];
                float f = scale[Mathf.Clamp(note, 0, scale.Length - 1)] * pitchMul;
                float e = Mathf.Min(1f, bt / 0.03f) * Mathf.Exp(-bt * 2.2f);
                float vib = 1f + 0.006f * Mathf.Sin(TAU * 5f * t);
                v += 0.22f * (Mathf.Sin(TAU * f * vib * t) + 0.25f * Mathf.Sin(TAU * f * 2f * t)) * e;

                int bar = (int)(t / (beat * 4f)) % 4;
                float cr = chordRoot[bar] * pitchMul;
                float barT = t % (beat * 4f);
                float pe = Mathf.Min(1f, barT / 0.4f) * Mathf.Min(1f, (beat * 4f - barT) / 0.4f);
                v += 0.12f * pe * (Mathf.Sin(TAU * cr * t) + 0.6f * Mathf.Sin(TAU * cr * 1.5f * t) + 0.4f * Mathf.Sin(TAU * cr * 2f * t));

                if (bi % 2 == 0) v += 0.28f * Mathf.Sin(TAU * 62f * bt) * Mathf.Exp(-bt * 9f);
                else
                {
                    lpn += (((float)rnd.NextDouble() * 2f - 1f) - lpn) * 0.6f;
                    v += 0.06f * lpn * Mathf.Exp(-bt * 30f);
                }
                return v * 0.9f;
            }, true);
        }
    }
}
