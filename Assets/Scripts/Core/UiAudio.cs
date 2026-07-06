using UnityEngine;

namespace FirstGame.Core
{
    /// <summary>Persistent UI SFX source (hover / click), created on first use.</summary>
    public class UiAudio : MonoBehaviour
    {
        static UiAudio _i;
        AudioSource _src;

        public static UiAudio I
        {
            get
            {
                if (_i == null)
                {
                    var go = new GameObject("[UiAudio]");
                    DontDestroyOnLoad(go);
                    _i = go.AddComponent<UiAudio>();
                    _i._src = go.AddComponent<AudioSource>();
                    _i._src.spatialBlend = 0f;
                    _i._src.playOnAwake = false;
                }
                return _i;
            }
        }

        public void Hover() { if (ProceduralAudio.Hover != null) _src.PlayOneShot(ProceduralAudio.Hover, 0.5f); }
        public void Click() { if (ProceduralAudio.Click != null) _src.PlayOneShot(ProceduralAudio.Click, 0.6f); }
    }
}
