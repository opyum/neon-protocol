using UnityEditor;
using UnityEngine;
using Unity.Netcode;
using FirstGame.Net;

/// <summary>Builds the networked player prefab as a real asset (so its NetworkObject gets a baked
/// GlobalObjectIdHash, which runtime-created objects can't have). Menu: NEON ▸ Créer le prefab réseau.</summary>
public static class NetSetup
{
    const string Dir = "Assets/Resources";
    const string Path = Dir + "/NetPlayer.prefab";

    [MenuItem("NEON/Créer le prefab réseau")]
    public static void Build()
    {
        var root = new GameObject("NetPlayer");
        var cc = root.AddComponent<CharacterController>();
        cc.height = 1.8f; cc.radius = 0.4f; cc.center = new Vector3(0, 0.9f, 0);
        root.AddComponent<NetworkObject>();
        root.AddComponent<ClientNetworkTransform>();
        root.AddComponent<NetRig>();
        root.AddComponent<NetDamageRelay>();

        var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Body";
        Object.DestroyImmediate(body.GetComponent<Collider>());
        body.transform.SetParent(root.transform, false);
        body.transform.localPosition = new Vector3(0, 0.9f, 0);

        if (!AssetDatabase.IsValidFolder(Dir)) AssetDatabase.CreateFolder("Assets", "Resources");
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, Path);
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        Debug.Log($"[NetSetup] NetPlayer.prefab créé : {(prefab != null ? "OK" : "ÉCHEC")} ({Path})");
    }
}
