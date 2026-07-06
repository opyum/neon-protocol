using UnityEngine;

namespace FirstGame.Core
{
    /// <summary>Persistent background music (procedural loop) with a gentle fade-in.</summary>
    public class MusicPlayer : MonoBehaviour
    {
        AudioSource _src;
        float _target = 0.2f;

        void Awake()
        {
            DontDestroyOnLoad(gameObject);
            _src = gameObject.AddComponent<AudioSource>();
            _src.clip = ProceduralAudio.MusicLoop;
            _src.loop = true;
            _src.spatialBlend = 0f;
            _src.volume = 0f;
            _src.playOnAwake = false;
            _src.Play();
        }

        void Update()
        {
            if (_src.volume < _target)
                _src.volume = Mathf.MoveTowards(_src.volume, _target, Time.deltaTime * 0.1f);
        }
    }
}
