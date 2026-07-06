using UnityEngine;

namespace FirstGame.Core
{
    /// <summary>
    /// Synthesises all SFX at runtime (no audio files needed). Clips are built once and cached.
    /// </summary>
    public static class ProceduralAudio
    {
        const int Freq = 44100;

        static AudioClip _shot, _reload, _hit, _hitHead, _ability, _click, _ping;

        public static AudioClip Shot     => _shot     ??= BuildShot();
        public static AudioClip Reload   => _reload   ??= BuildReload();
        public static AudioClip Hit      => _hit      ??= BuildBlip(820f, 0.08f, 0.35f);
        public static AudioClip HitHead  => _hitHead  ??= BuildBlip(1200f, 0.10f, 0.4f);
        public static AudioClip Ability  => _ability  ??= BuildSweep(300f, 950f, 0.28f);
        public static AudioClip Click    => _click    ??= BuildBlip(1300f, 0.04f, 0.25f);
        public static AudioClip Ping     => _ping     ??= BuildBlip(520f, 0.12f, 0.3f);

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
