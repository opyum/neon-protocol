using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using FirstGame.Core;
using FirstGame.UI;

namespace FirstGame.Net
{
    /// <summary>Networked 1v1 player. Movement is owner-authoritative; COMBAT is server-authoritative:
    /// the owner sends a fire ray, the server re-raycasts against server positions and applies damage
    /// to a replicated Health NetworkVariable. Wears the animated Synty character.</summary>
    [RequireComponent(typeof(CharacterController))]
    public class NetPlayer : NetworkBehaviour
    {
        public NetworkVariable<float> Health = new NetworkVariable<float>(100f);

        CharacterController _cc;
        Camera _cam;
        CharacterVisual _visual;
        float _pitch, _vy, _nextFire, _hitFlash;
        Vector3 _lastPos;

        Text _healthText;
        Image _hitmarker;

        const float MaxHp = 100f, ShotDamage = 26f, FireInterval = 0.16f, Range = 100f;

        void Awake() => _cc = GetComponent<CharacterController>();

        public override void OnNetworkSpawn()
        {
            var body = transform.Find("Body");
            if (body != null) { var br = body.GetComponent<Renderer>(); if (br) br.enabled = false; }

            var ga = GameAssets.Instance;
            if (ga != null && ga.enemyCharacterPrefab != null)
                _visual = CharacterVisual.Attach(transform, ga.enemyCharacterPrefab, 1.8f, ArtPalette.Enemy);

            Health.OnValueChanged += OnHealthChanged;
            if (IsServer) Health.Value = MaxHp;

            if (IsOwner)
            {
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

                Teleport(SpawnPoint());
                BuildHud();
            }
            else
            {
                _cc.enabled = false;
            }

            _lastPos = transform.position;
        }

        public override void OnNetworkDespawn() => Health.OnValueChanged -= OnHealthChanged;

        void Update()
        {
            bool alive = Health.Value > 0f;
            if (IsOwner && _cc != null && _cc.enabled && alive) HandleInput();
            DriveAnimation();

            if (IsOwner && _hitmarker != null)
            {
                _hitFlash = Mathf.Max(0f, _hitFlash - Time.deltaTime * 3.5f);
                _hitmarker.color = new Color(1f, 0.85f, 0.2f, _hitFlash);
            }
        }

        // ---------- Movement + look (owner) ----------
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

            if (Input.GetMouseButton(0) && Time.time >= _nextFire) { _nextFire = Time.time + FireInterval; Fire(); }
        }

        void Fire()
        {
            if (_cam == null) return;
            var origin = _cam.transform.position;
            var dir = _cam.transform.forward;
            Vfx.Muzzle(origin + dir * 0.4f, dir, ArtPalette.NeonCyan);
            FireServerRpc(origin, dir);
        }

        // ---------- Server-authoritative combat ----------
        [ServerRpc]
        void FireServerRpc(Vector3 origin, Vector3 dir)
        {
            Vector3 end = origin + dir * Range;
            if (Physics.Raycast(origin, dir, out var hit, Range, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                end = hit.point;
                var target = hit.collider.GetComponentInParent<NetPlayer>();
                if (target != null && target != this && target.Health.Value > 0f)
                {
                    target.Health.Value = Mathf.Max(0f, target.Health.Value - ShotDamage);
                    HitConfirmClientRpc();
                    if (target.Health.Value <= 0f) target.StartCoroutine(target.ServerRespawn());
                }
            }
            TracerClientRpc(origin, end);
        }

        [ClientRpc]
        void TracerClientRpc(Vector3 a, Vector3 b)
        {
            var go = new GameObject("NetTracer");
            var lr = go.AddComponent<LineRenderer>();
            lr.sharedMaterial = ArtPalette.MakeUnlit(ArtPalette.NeonCyan);
            lr.startWidth = 0.03f; lr.endWidth = 0.01f; lr.numCapVertices = 2; lr.positionCount = 2;
            lr.SetPosition(0, a); lr.SetPosition(1, b);
            Destroy(go, 0.05f);
            Vfx.Impact(b, ArtPalette.NeonCyan);
        }

        [ClientRpc]
        void HitConfirmClientRpc() { if (IsOwner) _hitFlash = 0.9f; }

        IEnumerator ServerRespawn()
        {
            yield return new WaitForSeconds(2.5f);
            Health.Value = MaxHp;
            TeleportClientRpc(SpawnPoint());
        }

        [ClientRpc]
        void TeleportClientRpc(Vector3 pos) { if (IsOwner) Teleport(pos); }

        void Teleport(Vector3 pos)
        {
            bool was = _cc.enabled;
            _cc.enabled = false;
            transform.position = pos;
            _cc.enabled = was;
            _vy = 0f;
        }

        Vector3 SpawnPoint() => (OwnerClientId % 2 == 0)
            ? new Vector3(-8f, 0.3f, -16f)
            : new Vector3(8f, 0.3f, 16f);

        // ---------- Visual ----------
        void OnHealthChanged(float oldV, float newV)
        {
            if (IsOwner && _healthText != null)
            {
                _healthText.text = Mathf.CeilToInt(newV).ToString();
                _healthText.color = newV > 50f ? new Color(0.24f, 0.86f, 0.52f)
                    : newV > 25f ? new Color(0.96f, 0.78f, 0.22f) : new Color(0.92f, 0.26f, 0.30f);
            }
            if (newV <= 0f && oldV > 0f) _visual?.Die();
            if (newV >= MaxHp && oldV <= 0f && _visual != null)
                foreach (var r in _visual.GetComponentsInChildren<Renderer>()) r.enabled = !IsOwner; // re-show on respawn
        }

        void DriveAnimation()
        {
            if (_visual == null) return;
            Vector3 pos = transform.position;
            float speed;
            if (IsOwner && _cc != null) { var v = _cc.velocity; v.y = 0f; speed = v.magnitude; }
            else { Vector3 d = pos - _lastPos; d.y = 0f; speed = d.magnitude / Mathf.Max(Time.deltaTime, 0.0001f); }
            _lastPos = pos;
            _visual.SetSpeed(Mathf.Clamp01(speed / 5.5f));
        }

        void BuildHud()
        {
            UIFactory.EnsureEventSystem();
            var canvas = UIFactory.CreateCanvas("NetHud", 5);
            var cross = UIFactory.Label(canvas.transform, "+", 40, ArtPalette.NeonCyan, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Place(cross.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(60, 60));

            var hm = UIFactory.AddChild(canvas.transform, "HM");
            UIFactory.Place(hm, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(22, 22));
            _hitmarker = hm.gameObject.AddComponent<Image>();
            _hitmarker.color = new Color(1f, 0.85f, 0.2f, 0f);

            var block = UIFactory.AddChild(canvas.transform, "Hp");
            UIFactory.Place(block, Vector2.zero, Vector2.zero, new Vector2(40, 40), new Vector2(240, 90));
            UIFactory.Panel(block, new Color(ArtPalette.UiInk.r, ArtPalette.UiInk.g, ArtPalette.UiInk.b, 0.7f));
            _healthText = UIFactory.Label(block, "100", 56, new Color(0.24f, 0.86f, 0.52f), TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Stretch(_healthText.rectTransform, 8);
        }
    }
}
