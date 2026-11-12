using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public static class ChaseGameSceneSetup
{
    const string MapScene = "Assets/school/DemoScene/all_objects.unity";
    const string PlayerPrefab = "Assets/npc_casual_set_00/Prefabs/npc_csl_00_character_01m_01.prefab";
    const string BubbaPrefab = "Assets/Bubba_The_Handyman/URP/Prefabs/Handyman_ver_1.prefab";
    const string ConvertedMaterials = "Assets/ChaseGame/Materials";

    [MenuItem("Chase Game/Build playable school chase scene")]
    public static void Build()
    {
        Lightmapping.Cancel();
        Lightmapping.giWorkflowMode = Lightmapping.GIWorkflowMode.OnDemand;
        var scene = EditorSceneManager.OpenScene(MapScene, OpenSceneMode.Single);
        foreach (var old in scene.GetRootGameObjects())
        {
            if (old.name == "Chase Game" || old.name.StartsWith("Player - NPC") || old.name.StartsWith("Enemy - Bubba"))
                Object.DestroyImmediate(old);
        }

        var bounds = new Bounds(Vector3.zero, Vector3.one);
        var renderers = Object.FindObjectsOfType<Renderer>();
        int meshCount = 0;
        foreach (var renderer in renderers)
        {
            if (renderer is SkinnedMeshRenderer || renderer is ParticleSystemRenderer) continue;
            var filter = renderer.GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null) continue;
            bounds.Encapsulate(renderer.bounds);
            meshCount++;
            if (renderer.GetComponent<Collider>() == null)
            {
                var collider = Undo.AddComponent<MeshCollider>(renderer.gameObject);
                collider.sharedMesh = filter.sharedMesh;
            }
        }
        Debug.Log($"Chase scene map bounds: center={bounds.center}, size={bounds.size}, render meshes={meshCount}");

        var oldCamera = GameObject.Find("Main Camera");
        if (oldCamera != null) oldCamera.SetActive(false);
        var root = new GameObject("Chase Game");
        Undo.RegisterCreatedObjectUndo(root, "Create chase game");
        var manager = root.AddComponent<GameManager>();

        Vector3 center = bounds.center;
        Vector3 playerPosition = new Vector3(center.x - 6f, bounds.min.y + 0.2f, center.z);
        Vector3 bubbaPosition = new Vector3(center.x + 6f, bounds.min.y + 0.2f, center.z);
        var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefab);
        var bubbaPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BubbaPrefab);
        if (playerPrefab == null || bubbaPrefab == null) throw new System.Exception("A selected character prefab could not be loaded.");

        var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab, scene);
        player.name = "Player - NPC Casual 01M 01";
        player.transform.SetPositionAndRotation(playerPosition, Quaternion.identity);
        player.transform.localScale = Vector3.one;
        var modelRenderers = player.GetComponentsInChildren<Renderer>(true);
        var modelBounds = new Bounds(player.transform.position + Vector3.up, Vector3.one);
        foreach (var renderer in modelRenderers) modelBounds.Encapsulate(renderer.bounds);
        foreach (var renderer in modelRenderers)
            if (renderer.GetComponent<Collider>() != null) Undo.DestroyObjectImmediate(renderer.GetComponent<Collider>());
        var controller = player.AddComponent<CharacterController>();
        controller.height = Mathf.Clamp(modelBounds.size.y * 0.9f, 1.45f, 2.2f);
        controller.radius = Mathf.Clamp(modelBounds.size.x * 0.2f, 0.25f, 0.42f);
        controller.center = new Vector3(0f, controller.height * 0.52f, 0f);
        player.AddComponent<ChasePlayerController>();

        var bubba = (GameObject)PrefabUtility.InstantiatePrefab(bubbaPrefab, scene);
        bubba.name = "Enemy - Bubba the Handyman (Version 1)";
        bubba.transform.SetPositionAndRotation(bubbaPosition, Quaternion.Euler(0f, -90f, 0f));
        bubba.transform.localScale = Vector3.one;
        var agent = bubba.AddComponent<NavMeshAgent>();
        agent.radius = 0.35f;
        agent.height = 1.8f;
        agent.enabled = false;
        bubba.AddComponent<BubbaChaser>().target = player.transform;
        manager.survivalSeconds = 180f;

        ConvertSceneMaterials();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        var settings = EditorBuildSettings.scenes;
        var list = new List<EditorBuildSettingsScene>(settings);
        foreach (var item in list) if (item.path == MapScene) item.enabled = true;
        if (!list.Exists(item => item.path == MapScene)) list.Insert(0, new EditorBuildSettingsScene(MapScene, true));
        EditorBuildSettings.scenes = list.ToArray();
        Debug.Log("Playable chase scene saved and enabled in Build Settings.");
    }

    static void ConvertSceneMaterials()
    {
        var urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null) throw new System.Exception("The URP/Lit shader was not found.");
        Directory.CreateDirectory(ConvertedMaterials);
        AssetDatabase.Refresh();
        var cache = new Dictionary<Material, Material>();
        foreach (var renderer in Object.FindObjectsOfType<Renderer>())
        {
            var materials = renderer.sharedMaterials;
            bool changed = false;
            for (int i = 0; i < materials.Length; i++)
            {
                var source = materials[i];
                if (source == null || source.shader == urpLit || source.shader.name.StartsWith("Universal Render Pipeline/")) continue;
                if (!cache.TryGetValue(source, out var converted))
                {
                    string sourcePath = AssetDatabase.GetAssetPath(source);
                    string suffix = string.IsNullOrEmpty(sourcePath) ? System.Guid.NewGuid().ToString("N") : AssetDatabase.AssetPathToGUID(sourcePath);
                    string path = $"{ConvertedMaterials}/{source.name.Replace('/', '_')}_{suffix}.mat";
                    converted = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (converted == null)
                    {
                        converted = new Material(urpLit) { name = source.name + " (URP)" };
                        AssetDatabase.CreateAsset(converted, path);
                    }
                    converted.shader = urpLit;
                    if (source.HasProperty("_MainTex")) converted.SetTexture("_BaseMap", source.GetTexture("_MainTex"));
                    if (source.HasProperty("_Color")) converted.SetColor("_BaseColor", source.GetColor("_Color"));
                    if (source.HasProperty("_Metallic")) converted.SetFloat("_Metallic", source.GetFloat("_Metallic"));
                    if (source.HasProperty("_Glossiness")) converted.SetFloat("_Smoothness", source.GetFloat("_Glossiness"));
                    EditorUtility.SetDirty(converted);
                    cache[source] = converted;
                }
                materials[i] = converted;
                changed = true;
            }
            if (changed) renderer.sharedMaterials = materials;
        }
        AssetDatabase.SaveAssets();
    }
}




