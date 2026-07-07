using UnityEngine;

namespace FirstGame.Core
{
    /// <summary>Persistent player settings (input, audio, graphics) via PlayerPrefs.</summary>
    public static class Settings
    {
        public const float SensDefault = 2.0f;
        public const float VolDefault  = 0.8f;
        public const float AdsDefault  = 1.0f;
        public const float FovDefault  = 90f;

        static float _sens = -1f, _vol = -1f, _ads = -1f, _fov = -1f;
        static int _invY = -1, _vsync = -1, _quality = -1, _fsmode = -1, _resW = -1, _resH = -1;

        // ---- Input / look ----
        public static float MouseSensitivity
        {
            get { if (_sens < 0f) _sens = PlayerPrefs.GetFloat("set.sens", SensDefault); return _sens; }
            set { _sens = Mathf.Clamp(value, 0.4f, 8f); PlayerPrefs.SetFloat("set.sens", _sens); PlayerPrefs.Save(); }
        }

        /// <summary>Sensitivity multiplier applied while aiming down sights.</summary>
        public static float AdsSensMultiplier
        {
            get { if (_ads < 0f) _ads = PlayerPrefs.GetFloat("set.adssens", AdsDefault); return _ads; }
            set { _ads = Mathf.Clamp(value, 0.2f, 2f); PlayerPrefs.SetFloat("set.adssens", _ads); PlayerPrefs.Save(); }
        }

        public static bool InvertY
        {
            get { if (_invY < 0) _invY = PlayerPrefs.GetInt("set.invy", 0); return _invY == 1; }
            set { _invY = value ? 1 : 0; PlayerPrefs.SetInt("set.invy", _invY); PlayerPrefs.Save(); }
        }

        // ---- Audio ----
        public static float MasterVolume
        {
            get { if (_vol < 0f) _vol = PlayerPrefs.GetFloat("set.vol", VolDefault); return _vol; }
            set { _vol = Mathf.Clamp01(value); PlayerPrefs.SetFloat("set.vol", _vol); PlayerPrefs.Save(); AudioListener.volume = _vol; }
        }

        // ---- Graphics ----
        public static float FieldOfView
        {
            get { if (_fov < 0f) _fov = PlayerPrefs.GetFloat("set.fov", FovDefault); return _fov; }
            set { _fov = Mathf.Clamp(value, 60f, 110f); PlayerPrefs.SetFloat("set.fov", _fov); PlayerPrefs.Save(); }
        }

        public static bool VSync
        {
            get { if (_vsync < 0) _vsync = PlayerPrefs.GetInt("set.vsync", 1); return _vsync == 1; }
            set { _vsync = value ? 1 : 0; PlayerPrefs.SetInt("set.vsync", _vsync); PlayerPrefs.Save(); ApplyGraphics(); }
        }

        public static int QualityLevel
        {
            get { if (_quality < 0) _quality = PlayerPrefs.GetInt("set.quality", QualitySettings.GetQualityLevel()); return _quality; }
            set { _quality = Mathf.Clamp(value, 0, Mathf.Max(0, QualitySettings.names.Length - 1)); PlayerPrefs.SetInt("set.quality", _quality); PlayerPrefs.Save(); ApplyGraphics(); }
        }

        /// <summary>Stored as (int)FullScreenMode.</summary>
        public static int FullscreenMode
        {
            get { if (_fsmode < 0) _fsmode = PlayerPrefs.GetInt("set.fsmode", (int)Screen.fullScreenMode); return _fsmode; }
            set { _fsmode = value; PlayerPrefs.SetInt("set.fsmode", _fsmode); PlayerPrefs.Save(); ApplyGraphics(); }
        }

        public static int ResWidth  { get { if (_resW < 0) _resW = PlayerPrefs.GetInt("set.resw", Screen.width);  return _resW; } }
        public static int ResHeight { get { if (_resH < 0) _resH = PlayerPrefs.GetInt("set.resh", Screen.height); return _resH; } }

        public static void SetResolution(int w, int h)
        {
            _resW = Mathf.Max(1, w); _resH = Mathf.Max(1, h);
            PlayerPrefs.SetInt("set.resw", _resW);
            PlayerPrefs.SetInt("set.resh", _resH);
            PlayerPrefs.Save();
            ApplyGraphics();
        }

        /// <summary>Applies audio + graphics + persisted resolution/mode. Called at boot.</summary>
        public static void Apply()
        {
            AudioListener.volume = MasterVolume;
            ApplyGraphics();
        }

        public static void ApplyGraphics()
        {
            if (QualitySettings.names.Length > 0)
                QualitySettings.SetQualityLevel(Mathf.Clamp(QualityLevel, 0, QualitySettings.names.Length - 1), true);
            // Set vSync AFTER SetQualityLevel (which can overwrite it with the level's default).
            QualitySettings.vSyncCount = VSync ? 1 : 0;

            var mode = (FullScreenMode)FullscreenMode;
            if (ResWidth > 0 && ResHeight > 0) Screen.SetResolution(ResWidth, ResHeight, mode);
            else Screen.fullScreenMode = mode;
        }

        /// <summary>Restores every setting (input, audio, graphics, keybinds) to defaults.</summary>
        public static void ResetAll()
        {
            MouseSensitivity  = SensDefault;
            AdsSensMultiplier = AdsDefault;
            InvertY           = false;
            MasterVolume      = VolDefault;
            FieldOfView       = FovDefault;
            VSync             = true;
            if (QualitySettings.names.Length > 0) QualityLevel = QualitySettings.names.Length - 1;
            FullscreenMode    = (int)UnityEngine.FullScreenMode.FullScreenWindow;
            var native = Screen.currentResolution;
            SetResolution(native.width, native.height);
            Keybinds.ResetAll();
        }
    }
}
