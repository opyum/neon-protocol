using UnityEngine;
using Unity.Netcode;
using FirstGame.Core;
using FirstGame.Player;
using FirstGame.UI;

namespace FirstGame.Net
{
    /// <summary>The networked player. The OWNER gets the full real rig (their agent's abilities,
    /// their weapon, the real HUD, pause menu, scoreboard) via PlayerRig.Assemble. Others just show
    /// the animated character. Damage/health/respawn go through PlayerHealth + NetDamageRelay.</summary>
    [RequireComponent(typeof(CharacterController))]
    public class NetRig : NetworkBehaviour
    {
        CharacterController _cc;
        CharacterVisual _visual;
        NetDamageRelay _relay;
        PlayerHealth _health;
        Vector3 _lastPos;

        void Awake() => _cc = GetComponent<CharacterController>();

        public override void OnNetworkSpawn()
        {
            _relay = GetComponent<NetDamageRelay>();
            var body = transform.Find("Body");
            if (body != null) { var br = body.GetComponent<Renderer>(); if (br) br.enabled = false; }

            if (IsOwner)
            {
                gameObject.name = "NetPlayer(Owner)";
                var rig = PlayerRig.Assemble(gameObject, _cc);
                _health = rig.health;
                _health.SpawnPoint = SpawnPoint();
                Teleport(SpawnPoint());

                var hudCanvas = UIFactory.CreateCanvas("HUDCanvas", 0);
                var hud = hudCanvas.gameObject.AddComponent<HUD>();
                hud.playerHealth = rig.health;
                hud.weapon = rig.weapon;
                hud.abilities = rig.abilities;

                KillFeed.Init(rig.weapon);
                DamageNumbers.Init(rig.weapon, rig.abilities);

                new GameObject("[PauseMenu]").AddComponent<PauseMenu>();
                new GameObject("[Scoreboard]").AddComponent<NetScoreboard>();

                if (_relay != null)
                {
                    _relay.local = _health;
                    _relay.NetHp.Value = _health.MaxHealth;
                    _health.OnHealthChanged += OnOwnerHealth;
                    _health.OnDied += () => _relay.ReportDeath();
                }

                var tmp = GameObject.Find("[TempNetCam]");
                if (tmp != null) Destroy(tmp);
            }
            else
            {
                // Keep the CharacterController ENABLED so raycasts can hit this remote player.
                // We never call Move() on it — the NetworkTransform drives its position.
                var ga = GameAssets.Instance;
                if (ga != null && ga.enemyCharacterPrefab != null)
                    _visual = CharacterVisual.Attach(transform, ga.enemyCharacterPrefab, 1.8f, ArtPalette.Enemy);
            }
            _lastPos = transform.position;
        }

        void OnOwnerHealth(float h, float max) { if (_relay != null) _relay.NetHp.Value = h; }

        void Update()
        {
            if (IsOwner || _visual == null) return;
            Vector3 pos = transform.position;
            Vector3 d = pos - _lastPos; d.y = 0f;
            _lastPos = pos;
            _visual.SetSpeed(Mathf.Clamp01(d.magnitude / Mathf.Max(Time.deltaTime, 0.0001f) / 5.5f));
        }

        void Teleport(Vector3 pos)
        {
            bool was = _cc.enabled;
            _cc.enabled = false;
            transform.position = pos;
            _cc.enabled = was;
        }

        Vector3 SpawnPoint() => (OwnerClientId % 2 == 0)
            ? new Vector3(-8f, 0.2f, -16f)
            : new Vector3(8f, 0.2f, 16f);
    }
}
