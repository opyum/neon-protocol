using UnityEngine;
using Unity.Netcode;
using FirstGame.Core;

namespace FirstGame.Net
{
    /// <summary>Phase-1 networked player: owner reads input and moves (owner-authoritative transform
    /// replicates to others). Only the owner has a camera/audio. No combat yet.</summary>
    [RequireComponent(typeof(CharacterController))]
    public class NetPlayer : NetworkBehaviour
    {
        CharacterController _cc;
        Camera _cam;
        float _pitch, _vy;

        void Awake() => _cc = GetComponent<CharacterController>();

        public override void OnNetworkSpawn()
        {
            // Colour: your own player cyan, the others red (each client's own view).
            var col = IsOwner ? ArtPalette.Player : ArtPalette.Enemy;
            foreach (var r in GetComponentsInChildren<Renderer>())
                r.sharedMaterial = ArtPalette.MakeEmissive(col, 0.35f);

            if (IsOwner)
            {
                var camGo = new GameObject("NetCam");
                camGo.transform.SetParent(transform, false);
                camGo.transform.localPosition = new Vector3(0, 1.6f, 0);
                _cam = camGo.AddComponent<Camera>();
                camGo.AddComponent<AudioListener>();
                foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = false; // hide own body
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                var tmp = GameObject.Find("[TempNetCam]");
                if (tmp != null) Destroy(tmp);
            }
            else
            {
                _cc.enabled = false; // non-owner position is driven by the NetworkTransform
            }
        }

        void Update()
        {
            if (!IsOwner || _cc == null || !_cc.enabled) return;

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
    }
}
