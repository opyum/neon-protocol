using UnityEngine;

namespace FirstGame.Core
{
    /// <summary>Persistent player settings (mouse sensitivity, master volume) via PlayerPrefs.</summary>
    public static class Settings
    {
        static float _sens = -1f, _vol = -1f;

        public static float MouseSensitivity
        {
            get { if (_sens < 0f) _sens = PlayerPrefs.GetFloat("set.sens", 2.0f); return _sens; }
            set { _sens = Mathf.Clamp(value, 0.4f, 8f); PlayerPrefs.SetFloat("set.sens", _sens); PlayerPrefs.Save(); }
        }

        public static float MasterVolume
        {
            get { if (_vol < 0f) _vol = PlayerPrefs.GetFloat("set.vol", 0.8f); return _vol; }
            set { _vol = Mathf.Clamp01(value); PlayerPrefs.SetFloat("set.vol", _vol); PlayerPrefs.Save(); AudioListener.volume = _vol; }
        }

        public static void Apply() => AudioListener.volume = MasterVolume;
    }
}
