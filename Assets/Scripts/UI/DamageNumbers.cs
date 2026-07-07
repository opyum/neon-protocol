using System.Collections.Generic;
using UnityEngine;
using FirstGame.Core;
using FirstGame.Combat;
using FirstGame.Abilities;

namespace FirstGame.UI
{
    /// <summary>Floating world-space damage numbers, pooled (no per-shot GC). Headshots are gold + bigger.
    /// Wire once via Init() to a weapon + ability system.</summary>
    public class DamageNumbers : MonoBehaviour
    {
        class Entry { public GameObject go; public TextMesh tm; public float t; public bool active; }

        readonly List<Entry> _pool = new();
        Camera _cam;

        public static void Init(WeaponController weapon, AbilitySystem abilities)
        {
            var dn = new GameObject("[DamageNumbers]").AddComponent<DamageNumbers>();
            if (weapon != null) weapon.OnHit += (t, dmg, head) => dn.ShowAt(t, dmg, head);
            if (abilities != null) abilities.OnAbilityHit += (slot, a, t) => dn.ShowAt(t, a.damage, false);
        }

        void ShowAt(IDamageable target, float dmg, bool head)
        {
            if (dmg <= 0f) return;
            var mb = target as MonoBehaviour;
            if (mb == null) return;
            Show(mb.transform.position + Vector3.up * 1.7f, dmg, head);
        }

        void Show(Vector3 pos, float dmg, bool head)
        {
            var e = _pool.Find(x => !x.active) ?? Create();
            e.active = true;
            e.t = 0f;
            e.go.SetActive(true);
            e.go.transform.position = pos + new Vector3(Random.Range(-0.25f, 0.25f), 0f, 0f);
            e.tm.text = Mathf.CeilToInt(dmg).ToString();
            e.tm.color = head ? new Color(1f, 0.82f, 0.25f) : Color.white;
            e.tm.characterSize = head ? 0.14f : 0.10f;
        }

        Entry Create()
        {
            var go = new GameObject("dmg");
            go.transform.SetParent(transform, false);
            var tm = go.AddComponent<TextMesh>();
            tm.font = UIFactory.Font;
            go.GetComponent<MeshRenderer>().sharedMaterial = UIFactory.Font.material;
            tm.fontSize = 64;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.fontStyle = FontStyle.Bold;
            var e = new Entry { go = go, tm = tm };
            _pool.Add(e);
            return e;
        }

        void LateUpdate()
        {
            if (_cam == null) _cam = Camera.main;
            foreach (var e in _pool)
            {
                if (!e.active) continue;
                e.t += Time.deltaTime;
                float k = e.t / 0.8f;
                if (k >= 1f) { e.active = false; e.go.SetActive(false); continue; }
                e.go.transform.position += Vector3.up * (1.4f * Time.deltaTime);
                if (_cam != null) e.go.transform.rotation = _cam.transform.rotation; // billboard
                var c = e.tm.color; c.a = 1f - k; e.tm.color = c;
            }
        }
    }
}
