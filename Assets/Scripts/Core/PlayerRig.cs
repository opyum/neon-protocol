using UnityEngine;
using FirstGame.Player;
using FirstGame.Combat;
using FirstGame.Abilities;
using FirstGame.Equipment;
using FirstGame.Progression;

namespace FirstGame.Core
{
    /// <summary>Builds the full first-person player rig used by every gameplay scene.</summary>
    public static class PlayerRig
    {
        public const int IgnoreRaycast = 2; // weapon rays skip the player

        public struct Refs
        {
            public GameObject player;
            public Camera camera;
            public PlayerHealth health;
            public WeaponController weapon;
            public AbilitySystem abilities;
            public FirstPersonController controller;
            public CameraShake shake;
        }

        public static Refs Build(Vector3 spawnPos, Quaternion spawnRot)
        {
            var player = new GameObject("Player") { layer = IgnoreRaycast };
            player.transform.SetPositionAndRotation(spawnPos, spawnRot);

            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.4f;
            cc.center = new Vector3(0, 0.9f, 0);

            return Assemble(player, cc);
        }

        /// <summary>Adds the full first-person rig to an existing GameObject that already has a
        /// CharacterController (used both by Build and by the networked player).</summary>
        public static Refs Assemble(GameObject player, CharacterController cc)
        {
            var r = new Refs();
            r.player = player;
            player.layer = IgnoreRaycast;

            r.controller = player.AddComponent<FirstPersonController>();

            var pivot = new GameObject("CameraPivot");
            pivot.transform.SetParent(player.transform, false);
            pivot.transform.localPosition = new Vector3(0, 1.6f, 0);
            r.controller.cameraPivot = pivot.transform;

            var camGo = new GameObject("PlayerCamera");
            camGo.transform.SetParent(pivot.transform, false);
            r.camera = camGo.AddComponent<Camera>();
            r.camera.clearFlags = CameraClearFlags.Skybox;
            r.camera.backgroundColor = ArtPalette.Sky;
            r.camera.fieldOfView = 90f;
            r.camera.nearClipPlane = 0.05f;
            camGo.AddComponent<AudioListener>();
            camGo.tag = "MainCamera";
            r.shake = camGo.AddComponent<CameraShake>();
            PostFx.EnablePostProcessing(r.camera);

            r.health = player.AddComponent<PlayerHealth>();

            r.weapon = player.AddComponent<WeaponController>();
            r.weapon.aimCamera = r.camera;
            r.weapon.weapon = WeaponCatalog.ById(PlayerProfile.Current.weaponId);
            r.weapon.hitMask = Physics.DefaultRaycastLayers;

            r.abilities = player.AddComponent<AbilitySystem>();
            r.abilities.aimCamera = r.camera;
            r.abilities.playerHealth = r.health;
            r.abilities.body = cc;
            r.abilities.hitMask = Physics.DefaultRaycastLayers;

            // Viewmodel: real weapon model if assigned, else a teal block
            var ga = GameAssets.Instance;
            var wpnPrefab = ga != null ? ga.WeaponFor(r.weapon.weapon.id) : null;
            GameObject vm;
            if (wpnPrefab != null)
            {
                vm = ModelUtil.Spawn(wpnPrefab, camGo.transform, 0.26f, byHeight: false,
                                     ArtPalette.MakeMaterial(ArtPalette.Metal, 0.7f, 0.6f));
                vm.transform.localPosition = new Vector3(0.16f, -0.18f, 0.45f);
                vm.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
                vm.name = "Viewmodel";
            }
            else
            {
                vm = Prim.Box(camGo.transform, new Vector3(0.35f, -0.28f, 0.6f), new Vector3(0.12f, 0.12f, 0.5f),
                              ArtPalette.Player, collider: false, name: "Viewmodel");
            }
            ModelUtil.SetLayerRecursive(vm, IgnoreRaycast);

            // Juice
            var feel = player.AddComponent<GameFeel>();
            feel.weapon = r.weapon;
            feel.abilities = r.abilities;
            feel.health = r.health;
            feel.shake = r.shake;
            feel.controller = r.controller;

            // Equipment: passive armour applied at spawn + consumable utility on key G
            EquipmentEffects.ApplyArmor(EquipmentCatalog.ById(PlayerProfile.Current.equipmentId), r);
            var util = player.AddComponent<UtilityController>();
            util.Init(EquipmentCatalog.ById(PlayerProfile.Current.utilityId), r);

            return r;
        }
    }
}
