using System;
using UnityEngine;
using FirstGame.Core;
using FirstGame.Enemies;
using FirstGame.Player;
using FirstGame.Progression;

namespace FirstGame.Combat
{
    /// <summary>Hitscan firing, ammo, reloading and a line tracer for feedback.</summary>
    public class WeaponController : MonoBehaviour
    {
        public Camera aimCamera;
        [NonSerialized] public WeaponData weapon = WeaponCatalog.Default;
        public LayerMask hitMask = Physics.DefaultRaycastLayers; // excludes Ignore Raycast (the player)
        public Transform muzzle; // optional visual origin for the tracer
        public Transform viewmodelRoot; // camera transform; the viewmodel is parented + rebuilt here
        WeaponData _primary, _secondary;
        GameObject _viewmodel;

        public int Ammo { get; private set; }
        public bool Reloading { get; private set; }
        public bool ControlEnabled = true;

        float _nextFireTime;
        float _reloadEndTime;

        public event Action OnFired;                       // any shot leaves the barrel
        public event Action OnReloadStart;
        public event Action OnReloadEnd;
        public event Action<int, int> OnAmmoChanged;       // (ammo, magazine)
        public event Action<IDamageable, float, bool> OnHit; // (target, damage, wasHeadshot)
        public event Action OnWeaponChanged;
        public event Action<bool> OnKill;                    // (wasHeadshot) a shot killed the target

        public float baseSpreadDeg = 1.6f; // hip-fire cone at Contrôle 0; reduced by the stat + ADS
        FirstPersonController _fpc;

        void Awake()
        {
            Ammo = weapon.magazineSize;
            _fpc = GetComponent<FirstPersonController>();
        }

        void Start() => OnAmmoChanged?.Invoke(Ammo, weapon.magazineSize);

        void Update()
        {
            if (!ControlEnabled) return;

            TrySwitchInput();

            if (Reloading)
            {
                if (Time.time >= _reloadEndTime)
                {
                    Reloading = false;
                    Ammo = weapon.magazineSize;
                    OnAmmoChanged?.Invoke(Ammo, weapon.magazineSize);
                    OnReloadEnd?.Invoke();
                }
                return;
            }

            if (Keybinds.Pressed(GameAction.Reload)) BeginReload();

            if (Input.GetKey(Keybinds.Get(GameAction.Fire)) && Time.time >= _nextFireTime && Ammo > 0)
            {
                Fire();
                _nextFireTime = Time.time + 1f / Mathf.Max(0.05f, weapon.fireRate);
            }
        }

        public void SetAmmo(int value)
        {
            Ammo = Mathf.Clamp(value, 0, weapon.magazineSize);
            OnAmmoChanged?.Invoke(Ammo, weapon.magazineSize);
        }

        /// <summary>Set the two carried weapons and equip the primary. Switch in-game with 1/2/wheel.</summary>
        public void SetLoadout(WeaponData primary, WeaponData secondary)
        {
            _primary = primary ?? WeaponCatalog.Default;
            _secondary = secondary ?? _primary;
            SwitchWeapon(_primary);
        }

        public void SwitchWeapon(WeaponData w)
        {
            if (w == null) return;
            weapon = w;
            Reloading = false;
            Ammo = weapon.magazineSize;
            BuildViewmodel();
            OnWeaponChanged?.Invoke();
            OnAmmoChanged?.Invoke(Ammo, weapon.magazineSize);
        }

        void BuildViewmodel()
        {
            if (viewmodelRoot == null) return;
            if (_viewmodel != null) Destroy(_viewmodel);
            var ga = GameAssets.Instance;
            var prefab = ga != null ? ga.WeaponFor(weapon.id) : null;
            if (prefab != null)
            {
                _viewmodel = ModelUtil.Spawn(prefab, viewmodelRoot, 0.26f, byHeight: false,
                                             ArtPalette.MakeMaterial(ArtPalette.Metal, 0.7f, 0.6f));
                _viewmodel.transform.localPosition = new Vector3(0.16f, -0.18f, 0.45f);
                _viewmodel.transform.localRotation = Quaternion.identity;
            }
            else
            {
                _viewmodel = Prim.Box(viewmodelRoot, new Vector3(0.35f, -0.28f, 0.6f), new Vector3(0.12f, 0.12f, 0.5f),
                                      ArtPalette.Player, collider: false, name: "Viewmodel");
            }
            _viewmodel.name = "Viewmodel";
            ModelUtil.SetLayerRecursive(_viewmodel, PlayerRig.IgnoreRaycast);
        }

        void TrySwitchInput()
        {
            if (_primary == null) return;
            if (Input.GetKeyDown(KeyCode.Alpha1) && weapon != _primary) { SwitchWeapon(_primary); return; }
            if (Input.GetKeyDown(KeyCode.Alpha2) && _secondary != null && weapon != _secondary) { SwitchWeapon(_secondary); return; }
            float wheel = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(wheel) > 0.01f && _secondary != null && _secondary != _primary)
                SwitchWeapon(weapon == _primary ? _secondary : _primary);
        }

        public void BeginReload()
        {
            if (Reloading || Ammo >= weapon.magazineSize) return;
            Reloading = true;
            _reloadEndTime = Time.time + weapon.reloadSeconds;
            OnReloadStart?.Invoke();
        }

        void Fire()
        {
            Ammo--;
            OnAmmoChanged?.Invoke(Ammo, weapon.magazineSize);
            OnFired?.Invoke();

            var cam = aimCamera != null ? aimCamera : Camera.main;
            if (cam == null) return;

            // Dispersion cone: reduced by the Contrôle stat and by aiming down sights.
            bool aiming = Keybinds.Held(GameAction.Aim);
            float spread = baseSpreadDeg * PlayerProfile.Current.SpreadMultiplier * (aiming ? 0.35f : 1f);
            Vector3 dir = cam.transform.forward;
            if (spread > 0.01f)
                dir = Quaternion.AngleAxis(UnityEngine.Random.Range(-spread, spread), cam.transform.up)
                    * Quaternion.AngleAxis(UnityEngine.Random.Range(-spread, spread), cam.transform.right) * dir;

            if (_fpc != null) _fpc.AddRecoil(aiming ? 0.35f : 0.6f); // upward view kick

            Ray ray = new Ray(cam.transform.position, dir);
            Vector3 tracerStart = muzzle != null ? muzzle.position : ray.origin;
            Vector3 end = ray.origin + ray.direction * weapon.range;
            Vfx.Muzzle(muzzle != null ? muzzle.position : ray.origin + ray.direction * 0.5f, ray.direction, ArtPalette.NeonCyan);

            if (Physics.Raycast(ray, out var hit, weapon.range, hitMask, QueryTriggerInteraction.Ignore))
            {
                end = hit.point;
                Vfx.Impact(hit.point, ArtPalette.NeonCyan);
                var head = hit.collider.GetComponent<HeadHitbox>();
                var target = hit.collider.GetComponentInParent<IDamageable>();
                if (target != null && target.IsAlive)
                {
                    bool headshot = head != null;
                    float damage = weapon.damage * (headshot ? weapon.headshotMultiplier : 1f) * PlayerProfile.Current.DamageMultiplier;
                    float dealt = target.TakeDamage(damage, hit.point, hit.normal);
                    OnHit?.Invoke(target, dealt, headshot);
                    if (!target.IsAlive) OnKill?.Invoke(headshot);
                }
            }

            Tracer(tracerStart, end);
        }

        void Tracer(Vector3 a, Vector3 b)
        {
            var go = new GameObject("Tracer");
            var lr = go.AddComponent<LineRenderer>();
            lr.sharedMaterial = ArtPalette.MakeUnlit(ArtPalette.NeonCyan);
            lr.startWidth = 0.03f;
            lr.endWidth = 0.01f;
            lr.numCapVertices = 2;
            lr.positionCount = 2;
            lr.SetPosition(0, a);
            lr.SetPosition(1, b);
            Destroy(go, 0.04f);
        }
    }
}
