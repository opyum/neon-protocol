using System;
using UnityEngine;
using FirstGame.Core;
using FirstGame.Enemies;

namespace FirstGame.Combat
{
    /// <summary>Hitscan firing, ammo, reloading and a line tracer for feedback.</summary>
    public class WeaponController : MonoBehaviour
    {
        public Camera aimCamera;
        [NonSerialized] public WeaponData weapon = WeaponCatalog.Default;
        public LayerMask hitMask = Physics.DefaultRaycastLayers; // excludes Ignore Raycast (the player)
        public Transform muzzle; // optional visual origin for the tracer

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

        void Awake()
        {
            Ammo = weapon.magazineSize;
        }

        void Start() => OnAmmoChanged?.Invoke(Ammo, weapon.magazineSize);

        void Update()
        {
            if (!ControlEnabled) return;

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

            if (Input.GetKeyDown(KeyCode.R)) BeginReload();

            if (Input.GetMouseButton(0) && Time.time >= _nextFireTime && Ammo > 0)
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

        public void SwitchWeapon(WeaponData w)
        {
            if (w == null) return;
            weapon = w;
            Reloading = false;
            Ammo = weapon.magazineSize;
            OnWeaponChanged?.Invoke();
            OnAmmoChanged?.Invoke(Ammo, weapon.magazineSize);
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

            Ray ray = new Ray(cam.transform.position, cam.transform.forward);
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
                    float damage = weapon.damage * (headshot ? weapon.headshotMultiplier : 1f);
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
