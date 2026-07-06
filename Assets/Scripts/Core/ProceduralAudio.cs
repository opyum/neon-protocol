using UnityEngine;

namespace FirstGame.Core
{
    /// <summary>
    /// Synthesises all SFX at runtime (no audio files needed). Clips are built once and cached.
    /// </summary>
    public static class ProceduralAudio
    {
        const int Freq = 44100;

        static AudioClip _shot, _reload, _hit, _hitHead, _ability, _click, _ping, _step, _hover, _music, _kill;

        public static AudioClip Shot     => _shot     ??= BuildShot();
        public static AudioClip Reload   => _reload   ??= BuildReload();
        public static AudioClip Hit      => _hit      ??= BuildBlip(820f, 0.08f, 0.35f);
        public static AudioClip HitHead  => _hitHead  ??= BuildBlip(1200f, 0.10f, 0.4f);
        public static AudioClip Ability  => _ability  ??= BuildSweep(300f, 950f, 0.28f);
        public static AudioClip Click    => _click    ??= BuildBlip(1300f, 0.04f, 0.25f);
        public static AudioClip Ping     => _ping     ??= BuildBlip(520f, 0.12f, 0.3f);
        public static AudioClip Footstep => _step     ??= BuildStep();
        public static AudioClip Hover    => _hover    ??= BuildBlip(1650f, 0.03f, 0.12f);
        public static AudioClip MusicLoop => _music   ??= BuildMusic();
        public static AudioClip Kill     => _kill     ??= BuildKill();

        static AudioClip FromSamples(string name, float[] s)
        {
            var clip = AudioClip.Create(name, s.Length, 1, Freq, false);
            clip.SetData(s, 0);
            return clip;
        }

        static AudioClip BuildShot()
        {
            int n = (int)(Freq * 0.16f);
            var s = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Freq;
                float env = Mathf.Exp(-t * 30f);
                float noise = Random.value * 2f - 1f;
                float thump = Mathf.Sin(2f * Mathf.PI * 90f * t) * Mathf.Exp(-t * 18f);
                s[i] = Mathf.Clamp(noise * env * 0.6f + thump * 0.5f, -1f, 1f);
            }
            return FromSamples("sfx_shot", s);
        }

        static AudioClip BuildReload()
        {
            int n = (int)(Freq * 0.22f);
            var s = new float[n];
            AddClick(s, 0.00f, 0.03f);
            AddClick(s, 0.12f, 0.03f);
            return FromSamples("sfx_reload", s);
        }

        static void AddClick(float[] s, float start, float dur)
        {
            int a = (int)(start * Freq);
            int len = (int)(dur * Freq);
            for (int i = 0; i < len && a + i < s.Length; i++)
            {
                float t = (float)i / Freq;
                float env = Mathf.Exp(-t * 120f);
                s[a + i] = Mathf.Clamp(s[a + i] + (Random.value * 2f - 1f) * env * 0.5f, -1f, 1f);
            }
        }

        static AudioClip BuildBlip(float hz, float dur, float amp)
        {
            int n = (int)(Freq * dur);
            var s = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Freq;
                float env = Mathf.Exp(-t * 22f);
                s[i] = Mathf.Sin(2f * Mathf.PI * hz * t) * env * amp;
            }
            return FromSamples("sfx_blip", s);
        }

        // Two ascending tones = satisfying "kill confirmed".
        static AudioClip BuildKill()
        {
            int n = (int)(Freq * 0.2f);
            var s = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Freq;
                float env = Mathf.Exp(-t * 9f);
                float hz = t < 0.06f ? 720f : 1080f;
                s[i] = (Mathf.Sin(2f * Mathf.PI * hz * t) + 0.3f * Mathf.Sin(2f * Mathf.PI * hz * 2f * t)) * env * 0.4f;
            }
            return FromSamples("sfx_kill", s);
        }

        static AudioClip BuildStep()
        {
            int n = (int)(Freq * 0.09f);
            var s = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Freq;
                float env = Mathf.Exp(-t * 45f);
                float thump = Mathf.Sin(2f * Mathf.PI * 62f * t);
                float noise = (Random.value * 2f - 1f) * 0.4f;
                s[i] = Mathf.Clamp((thump * 0.7f + noise) * env * 0.5f, -1f, 1f);
            }
            return FromSamples("sfx_step", s);
        }

        // Ambient synthwave loop: Am - F - C - G, 84 BPM, raised-cosine per-bar envelope = seamless loop.
        static AudioClip BuildMusic()
        {
            float bar = (60f / 84f) * 4f; // seconds per bar
            int bars = 4;
            float dur = bar * bars;
            int n = Mathf.RoundToInt(Freq * dur);
            var s = new float[n];

            float[][] chords =
            {
                new[] { 110.00f, 130.81f, 164.81f }, // Am
                new[] { 87.31f,  110.00f, 130.81f }, // F
                new[] { 130.81f, 164.81f, 196.00f }, // C
                new[] { 98.00f,  123.47f, 146.83f }, // G
            };

            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Freq;
                int b = (int)(t / bar) % bars;
                float local = (t - b * bar) / bar;
                float env = Mathf.Sin(Mathf.PI * local); // ~0 at bar edges => seamless

                float pad = 0f;
                foreach (var f in chords[b])
                    pad += Mathf.Sin(2f * Mathf.PI * f * t) + 0.5f * Mathf.Sin(2f * Mathf.PI * f * 1.003f * t);
                float bass = Mathf.Sin(2f * Mathf.PI * chords[b][0] * 0.5f * t) * 0.6f;
                float lfo = 0.85f + 0.15f * Mathf.Sin(2f * Mathf.PI * 0.15f * t);
                s[i] = (pad * 0.10f + bass * 0.12f) * env * lfo;
            }

            float max = 0f;
            for (int i = 0; i < n; i++) max = Mathf.Max(max, Mathf.Abs(s[i]));
            if (max > 0.95f) { float g = 0.95f / max; for (int i = 0; i < n; i++) s[i] *= g; }

            return FromSamples("music_loop", s);
        }

        static AudioClip BuildSweep(float from, float to, float dur)
        {
            int n = (int)(Freq * dur);
            var s = new float[n];
            float phase = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Freq;
                float k = t / dur;
                float hz = Mathf.Lerp(from, to, k);
                phase += 2f * Mathf.PI * hz / Freq;
                float env = Mathf.Sin(Mathf.PI * k); // fade in/out
                s[i] = Mathf.Sin(phase) * env * 0.4f;
            }
            return FromSamples("sfx_sweep", s);
        }
    }
}
