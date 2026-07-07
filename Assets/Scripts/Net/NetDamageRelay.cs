using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using FirstGame.Combat;
using FirstGame.Player;

namespace FirstGame.Net
{
    /// <summary>Bridges the local combat systems to the network. The weapon/abilities hit this
    /// (IDamageable) on the shooter's client; it routes the damage to the target's owner, whose own
    /// PlayerHealth applies it (real damage/death/respawn). Also replicates HP/kills/deaths.</summary>
    public class NetDamageRelay : NetworkBehaviour, IDamageable
    {
        public static readonly List<NetDamageRelay> All = new();

        public NetworkVariable<float> NetHp = new NetworkVariable<float>(
            100f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        public NetworkVariable<int> Kills = new NetworkVariable<int>(0);
        public NetworkVariable<int> Deaths = new NetworkVariable<int>(0);

        [System.NonSerialized] public PlayerHealth local; // set by NetRig on the owner
        ulong _lastAttacker;

        public bool IsAlive => NetHp.Value > 0f;

        public override void OnNetworkSpawn() { if (!All.Contains(this)) All.Add(this); }
        public override void OnNetworkDespawn() { All.Remove(this); }

        // Called on the SHOOTER's client by WeaponController/AbilitySystem (this = the target's relay).
        public float TakeDamage(float amount, Vector3 hitPoint, Vector3 hitNormal)
        {
            ulong shooter = NetworkManager.Singleton != null ? NetworkManager.Singleton.LocalClientId : 0;
            SubmitDamageServerRpc(amount, shooter);
            return amount; // optimistic value for the shooter's hitmarker
        }

        [ServerRpc(RequireOwnership = false)]
        void SubmitDamageServerRpc(float amount, ulong shooter) => ApplyDamageClientRpc(amount, shooter);

        [ClientRpc]
        void ApplyDamageClientRpc(float amount, ulong shooter)
        {
            if (!IsOwner || local == null) return;
            _lastAttacker = shooter;
            local.TakeDamage(amount, transform.position, Vector3.zero);
        }

        // The owner reports its own death (detected via PlayerHealth.OnDied) so the server scores it.
        public void ReportDeath() => ReportDeathServerRpc(_lastAttacker);

        [ServerRpc(RequireOwnership = false)]
        void ReportDeathServerRpc(ulong killer)
        {
            Deaths.Value++;
            if (killer == OwnerClientId) return; // self-kill: no frag
            foreach (var r in All)
                if (r != null && r != this && r.OwnerClientId == killer) { r.Kills.Value++; break; }
        }
    }
}
