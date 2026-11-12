using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static bool GameOver { get; private set; }
    public float survivalSeconds = 180f;
    float remaining;
    string result;
    GUIStyle labelStyle;
    GUIStyle titleStyle;
    GUIStyle hintStyle;

    void Awake()
    {
        remaining = survivalSeconds;
        GameOver = false;
        result = "";
        BuildRuntimeNavMesh();
    }

    void Update()
    {
        if (GameOver) return;
        remaining -= Time.deltaTime;
        if (remaining <= 0f) EndGame(true);
    }

    void BuildRuntimeNavMesh()
    {
        var sources = new System.Collections.Generic.List<NavMeshBuildSource>();
        var bounds = new Bounds(Vector3.zero, Vector3.one * 100f);
        foreach (var renderer in FindObjectsOfType<Renderer>())
        {
            if (renderer.GetComponentInParent<ChasePlayerController>() != null || renderer.GetComponentInParent<BubbaChaser>() != null) continue;
            if (!renderer.enabled || renderer is ParticleSystemRenderer) continue;
            sources.Add(new NavMeshBuildSource { shape = NavMeshBuildSourceShape.Mesh, sourceObject = renderer is SkinnedMeshRenderer ? null : renderer.GetComponent<MeshFilter>()?.sharedMesh, transform = renderer.transform.localToWorldMatrix, area = 0 });
            if (renderer is SkinnedMeshRenderer) sources.RemoveAt(sources.Count - 1);
            bounds.Encapsulate(renderer.bounds);
        }
        sources.RemoveAll(source => source.sourceObject == null);
        var settings = NavMesh.GetSettingsByID(0);
        settings.agentRadius = 0.35f;
        settings.agentHeight = 1.8f;
        settings.agentClimb = 0.5f;
        settings.agentSlope = 50f;
        bounds.Expand(new Vector3(8f, 6f, 8f));
        var data = NavMeshBuilder.BuildNavMeshData(settings, sources, bounds, Vector3.zero, Quaternion.identity);
        if (data != null)
        {
            NavMesh.AddNavMeshData(data);
            Debug.Log($"Runtime chase NavMesh built from {sources.Count} meshes; triangulation vertices: {NavMesh.CalculateTriangulation().vertices.Length}.");
        }
        else Debug.LogError("Could not build a runtime NavMesh from the school scene.");
        PlaceActorsOnNavMesh();
    }

    void PlaceActorsOnNavMesh()
    {
        var player = FindObjectOfType<ChasePlayerController>();
        var chaser = FindObjectOfType<BubbaChaser>();
        if (player == null || chaser == null) return;
        if (NavMesh.SamplePosition(player.transform.position, out var playerHit, 100f, NavMesh.AllAreas))
        {
            var controller = player.GetComponent<CharacterController>();
            controller.enabled = false;
            player.transform.position = playerHit.position + Vector3.up * 0.05f;
            controller.enabled = true;
        }
        Vector3 enemyStart = player.transform.position + player.transform.right * 12f;
        bool foundEnemyPosition = NavMesh.SamplePosition(enemyStart, out var enemyHit, 30f, NavMesh.AllAreas);
        if (!foundEnemyPosition)
            foundEnemyPosition = NavMesh.SamplePosition(chaser.transform.position, out enemyHit, 100f, NavMesh.AllAreas);
        var agent = chaser.GetComponent<NavMeshAgent>();
        if (foundEnemyPosition)
        {
            chaser.transform.position = enemyHit.position;
            if (agent != null)
            {
                agent.enabled = true;
                agent.Warp(enemyHit.position);
            }
        }
        else if (agent != null) agent.enabled = true;
    }
    public static void EndGame(bool playerWon)
    {
        if (GameOver) return;
        GameOver = true;
        var manager = FindObjectOfType<GameManager>();
        if (manager != null) manager.result = playerWon ? "YOU ESCAPED!" : "VPISAN.";
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void OnGUI()
    {
        if (labelStyle == null)
        {
            labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 25, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.white } };
            titleStyle = new GUIStyle(labelStyle) { fontSize = 54 };
            hintStyle = new GUIStyle(labelStyle) { fontSize = 17, fontStyle = FontStyle.Normal };
        }
        float panelWidth = 300;
        GUI.color = new Color(0.035f, 0.055f, 0.08f, 0.78f);
        GUI.Box(new Rect(Screen.width * 0.5f - panelWidth * 0.5f, 18, panelWidth, 64), GUIContent.none);
        GUI.color = Color.white;
        GUI.Label(new Rect(Screen.width * 0.5f - panelWidth * 0.5f, 20, panelWidth, 60), FormatTime(Mathf.Max(0, remaining)), labelStyle);
        GUI.Label(new Rect(12, Screen.height - 72, 550, 58), "WASD move  •  Shift sprint  •  Mouse look  •  V switch view  •  Esc release mouse", hintStyle);
        if (!GameOver) return;
        GUI.color = new Color(0f, 0f, 0f, 0.72f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;
        GUI.Label(new Rect(0, Screen.height * 0.34f, Screen.width, 85), result, titleStyle);
        GUI.Label(new Rect(0, Screen.height * 0.34f + 90, Screen.width, 42), "Press R to play again", labelStyle);
        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame) SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    string FormatTime(float seconds)
    {
        int total = Mathf.CeilToInt(seconds);
        return string.Format("{0:00}:{1:00}", total / 60, total % 60);
    }
}






