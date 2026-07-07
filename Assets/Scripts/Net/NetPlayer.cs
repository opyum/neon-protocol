using UnityEngine;
using Unity.Netcode;
using FirstGame.Core;

namespace FirstGame.Net
{
    /// <summary>Phase-1 networked player: owner reads input and moves (owner-authoritative transform
    /// replicates to others). Wears the real animated character (Synty) — hidden for the owner
    /// (first person), visible + animated for the others. No combat yet.</summary>
    [RequireComponent(typeof(CharacterController))]
    public class NetPlayer : NetworkBehaviour
    {
        CharacterController _cc;
        Camera _cam;
        CharacterVisual _visual;
        float _pitch, _vy;
        Vector3 _lastPos;

        void Awake() => _cc = GetComponent<CharacterController>();

        public override void OnNetworkSpawn()
        {
            // Hide the placeholder capsule; wear the real character model.
            var body = transform.Find("Body");
            if (body != null) { var br = body.GetComponent<Renderer>(); if (br) br.enabled = false; }

            var ga = GameAssets.Instance;
            if (ga != null && ga.enemyCharacterPrefab != null)
                _visual = CharacterVisual.Attach(transform, ga.enemyCharacterPrefab, 1.8f, ArtPalette.Enemy);

            if (IsOwner)
            {
                // First person: hide own character so it doesn't block the view.
                if (_visual != null)
                    foreach (var r in _visual.GetComponentsInChildren<Renderer>()) r.enabled = false;

                var camGo = new GameObject("NetCam");
                camGo.transform.SetParent(transform, false);
                camGo.transform.localPosition = new Vector3(0, 1.6f, 0);
                _cam = camGo.AddComponent<Camera>();
                camGo.AddComponent<AudioListener>();
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                var tmp = GameObject.Find("[TempNetCam]");
                if (tmp != null) Destroy(tmp);
            }
            else
            {
                _cc.enabled = false; // non-owner position is driven by the NetworkTransform
            }

            _lastPos = transform.position;
        }

        void Update()
        {
            if (IsOwner && _cc != null && _cc.enabled) HandleInput();
            DriveAnimation();
        }

        void HandleInput()
        {
            float mx = Input.GetAxis("Mouse X") * Settings.MouseSensitivity;
            float my = Input.GetAxis("Mouse Y") * Settings.MouseSensitivity;
            transform.Rotate(0f, mx, 0f);
            _pitch = Mathf.Clamp(_pitch - my, -85f, 85f);
            if (_cam != null) _cam.transform.localRotation = Quaternion.Euler(_pitch, 0f, 0f);

            float h = Input.GetAxisRaw("Horizontal"), v = Input.GetAxisRaw("Vertical");
            Vector3 move = transform.right * h + transform.forward * v;
            if (move.sqrMagnitude > 1f) move.Normalize();
            move *= 5.5f;
            _vy = _cc.isGrounded ? -1f : _vy - 20f * Time.deltaTime;
            if (_cc.isGrounded && Input.GetKeyDown(KeyCode.Space)) _vy = 7f;
            _cc.Move((move + Vector3.up * _vy) * Time.deltaTime);
        }

        void DriveAnimation()
        {
            if (_visual == null) return;
            Vector3 pos = transform.position;
            float speed;
            if (IsOwner && _cc != null)
            {
                var v = _cc.velocity; v.y = 0f; speed = v.magnitude;
            }
            else
            {
                Vector3 d = pos - _lastPos; d.y = 0f;
                speed = d.magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
            }
            _lastPos = pos;
            _visual.SetSpeed(Mathf.Clamp01(speed / 5.5f));
        }
    }
}
