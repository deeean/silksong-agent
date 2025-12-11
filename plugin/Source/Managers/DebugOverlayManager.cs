using UnityEngine;

namespace SilksongAgent;

public class DebugOverlayManager : MonoBehaviour
{
    public static DebugOverlayManager Instance;

    private bool showUI = false;
    private bool showRaycasts = false;

    private GUIStyle labelStyle;
    private GUIStyle headerStyle;
    private GUIStyle boxStyle;
    private GUIStyle keyStyle;
    private GUIStyle keyPressedStyle;
    private GUIStyle keyLabelStyle;

    private Texture2D keyNormalTex;
    private Texture2D keyPressedTex;

    private static readonly Color RaycastColorNone = new Color(0.5f, 0.5f, 0.5f, 0.15f);
    private static readonly Color RaycastColorTerrain = new Color(0f, 1f, 0f, 0.5f);
    private static readonly Color RaycastColorEnemy = new Color(1f, 0.5f, 0f, 0.7f);
    private static readonly Color RaycastColorProjectile = new Color(1f, 0f, 0f, 0.85f);
    private static readonly Color RaycastColorHazard = new Color(1f, 0f, 1f, 1f);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeStyles();
    }

    private void InitializeStyles()
    {
        labelStyle = new GUIStyle
        {
            fontSize = 14,
            normal = { textColor = Color.white },
            padding = new RectOffset(5, 5, 2, 2)
        };

        headerStyle = new GUIStyle
        {
            fontSize = 16,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.yellow },
            padding = new RectOffset(5, 5, 5, 5)
        };

        boxStyle = new GUIStyle
        {
            normal = { background = MakeBackgroundTexture(new Color(0, 0, 0, 0.7f)) },
            padding = new RectOffset(10, 10, 10, 10)
        };

        keyNormalTex = MakeKeyTexture(new Color(0.45f, 0.45f, 0.45f, 1f), new Color(0.25f, 0.25f, 0.25f, 1f));
        keyPressedTex = MakeKeyTexture(new Color(0.3f, 0.3f, 0.3f, 1f), new Color(0.6f, 0.6f, 0.6f, 1f));

        keyStyle = new GUIStyle
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white }
        };

        keyPressedStyle = new GUIStyle
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.9f, 0.9f, 0.9f, 1f) }
        };

        keyLabelStyle = new GUIStyle
        {
            fontSize = 10,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.7f, 0.7f, 0.7f, 1f) }
        };
    }

    private Texture2D MakeBackgroundTexture(Color color)
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }

    private Texture2D MakeKeyTexture(Color fillColor, Color borderColor)
    {
        int size = 32;
        int borderWidth = 2;
        Texture2D texture = new Texture2D(size, size);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool isBorder = x < borderWidth || x >= size - borderWidth ||
                               y < borderWidth || y >= size - borderWidth;
                texture.SetPixel(x, y, isBorder ? borderColor : fillColor);
            }
        }

        texture.Apply();
        return texture;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            showUI = !showUI;
            Plugin.Logger.LogInfo($"State visualization: {(showUI ? "ON" : "OFF")}");
        }

        if (Input.GetKeyDown(KeyCode.F2))
        {
            showRaycasts = !showRaycasts;
            Plugin.Logger.LogInfo($"Raycast visualization: {(showRaycasts ? "ON" : "OFF")}");
        }
    }

    private void OnGUI()
    {
        if (!showUI || SharedMemoryManager.Instance == null)
            return;

        if (labelStyle == null)
        {
            InitializeStyles();
        }

        var stateNullable = GetCurrentGameState();
        if (!stateNullable.HasValue)
            return;

        var state = stateNullable.Value;

        float x = 10;
        float y = 10;
        float width = 350;

        GUILayout.BeginArea(new Rect(x, y, width, Screen.height - 20), boxStyle);

        GUILayout.Label("=== SILKSONG AGENT STATE ===", headerStyle);
        GUILayout.Space(5);

        GUILayout.Label("PLAYER STATE", headerStyle);
        GUILayout.Label($"Position: ({state.playerPosX:F2}, {state.playerPosY:F2})", labelStyle);
        GUILayout.Label($"Velocity: ({state.playerVelX:F2}, {state.playerVelY:F2})", labelStyle);
        GUILayout.Label($"Health: {state.playerHealth} / {state.playerMaxHealth}", labelStyle);
        GUILayout.Label($"Silk: {state.playerSilk}", labelStyle);
        GUILayout.Label($"Grounded: {(state.playerGrounded == 1 ? "Yes" : "No")}", labelStyle);
        GUILayout.Label($"Can Dash: {(state.playerCanDash == 1 ? "Yes" : "No")}", labelStyle);
        GUILayout.Label($"Can Attack: {(state.playerCanAttack == 1 ? "Yes" : "No")}", labelStyle);
        GUILayout.Label($"Facing: {(state.playerFacingRight == 1 ? "Right" : "Left")}", labelStyle);

        var invincibleStyle = new GUIStyle(labelStyle) {
            normal = { textColor = state.playerInvincible == 1 ? Color.cyan : Color.white }
        };
        GUILayout.Label($"Invincible: {(state.playerInvincible == 1 ? "Yes" : "No")}", invincibleStyle);
        GUILayout.Space(10);

        GUILayout.Label("BOSS STATE", headerStyle);
        GUILayout.Label($"Position: ({state.bossPosX:F2}, {state.bossPosY:F2})", labelStyle);
        GUILayout.Label($"Velocity: ({state.bossVelX:F2}, {state.bossVelY:F2})", labelStyle);
        GUILayout.Label($"Health: {state.bossHealth} / {state.bossMaxHealth}", labelStyle);
        GUILayout.Label($"Phase: {state.bossPhase}", labelStyle);
        GUILayout.Label($"Facing: {(state.bossFacingRight == 1 ? "Right" : "Left")}", labelStyle);

        string attackStateName = GetAttackStateName((BossAttackState)state.bossAttackState);
        Color attackColor = GetAttackStateColor((BossAttackState)state.bossAttackState);
        var attackStyle = new GUIStyle(labelStyle) { normal = { textColor = attackColor } };
        GUILayout.Label($"Attack: {attackStateName}", attackStyle);

        float distance = Mathf.Sqrt(
            Mathf.Pow(state.bossPosX - state.playerPosX, 2) +
            Mathf.Pow(state.bossPosY - state.playerPosY, 2)
        );
        GUILayout.Label($"Distance: {distance:F2}", labelStyle);
        GUILayout.Space(10);

        GUILayout.Label("EPISODE INFO", headerStyle);
        GUILayout.Label($"Time: {state.episodeTime:F2}s", labelStyle);
        GUILayout.Label($"Terminated: {(state.terminated == 1 ? "Yes" : "No")}", labelStyle);
        GUILayout.Label($"Truncated: {(state.truncated == 1 ? "Yes" : "No")}", labelStyle);
        GUILayout.Space(10);

        GUILayout.Label("CONTROLS", headerStyle);
        GUILayout.Label("F1: Toggle this UI", labelStyle);
        GUILayout.Label("F2: Toggle Raycasts", labelStyle);

        GUILayout.EndArea();

        DrawInputVisualization();
    }

    private void DrawInputVisualization()
    {
        float panelWidth = 220;
        float panelHeight = 280;
        float x = Screen.width - panelWidth - 10;
        float y = 10;

        GUILayout.BeginArea(new Rect(x, y, panelWidth, panelHeight), boxStyle);

        GUILayout.Label("AGENT INPUT", headerStyle);
        GUILayout.Space(10);

        if (keyStyle == null)
        {
            InitializeStyles();
        }

        float keySize = 36;
        float keySpacing = 6;

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        DrawKeyWithLabel("A", "Heal", ActionManager.IsHealPressed, keySize);
        GUILayout.Space(keySpacing);
        DrawKeyWithLabel("S", "Claw", ActionManager.IsClawlinePressed, keySize);
        GUILayout.Space(keySpacing);
        DrawKeyWithLabel("F", "Skill", ActionManager.IsSkillPressed, keySize);
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        GUILayout.Space(10);

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        DrawKeyWithLabel("Z", "Jump", ActionManager.IsJumpPressed, keySize);
        GUILayout.Space(keySpacing);
        DrawKeyWithLabel("X", "Atk", ActionManager.IsAttackPressed, keySize);
        GUILayout.Space(keySpacing);
        DrawKeyWithLabel("C", "Dash", ActionManager.IsDashPressed, keySize);
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        GUILayout.Space(15);

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        DrawKey("^", ActionManager.IsUpPressed, keySize);
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        GUILayout.Space(keySpacing);

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        DrawKey("<", ActionManager.IsLeftPressed, keySize);
        GUILayout.Space(keySpacing);
        DrawKey("v", ActionManager.IsDownPressed, keySize);
        GUILayout.Space(keySpacing);
        DrawKey(">", ActionManager.IsRightPressed, keySize);
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        GUILayout.EndArea();
    }

    private void DrawKey(string label, bool pressed, float size)
    {
        Rect rect = GUILayoutUtility.GetRect(size, size);

        Texture2D tex = pressed ? keyPressedTex : keyNormalTex;
        GUI.DrawTexture(rect, tex);

        GUIStyle style = pressed ? keyPressedStyle : keyStyle;
        GUI.Label(rect, label, style);
    }

    private void DrawKeyWithLabel(string key, string label, bool pressed, float size)
    {
        GUILayout.BeginVertical();
        DrawKey(key, pressed, size);
        GUILayout.Label(label, keyLabelStyle, GUILayout.Width(size));
        GUILayout.EndVertical();
    }

    private Camera mainCamera;

    private void LateUpdate()
    {
        if (!showRaycasts || HeroController.instance == null)
            return;

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
                return;
        }

        Vector2 playerPos = new Vector2(
            HeroController.instance.transform.position.x,
            HeroController.instance.transform.position.y
        );

        var stateNullable = GetCurrentGameState();
        if (!stateNullable.HasValue)
            return;

        var state = stateNullable.Value;

        DrawRaycastsDebug(playerPos, state);
    }

    private void OnRenderObject()
    {
        if (!showRaycasts || HeroController.instance == null)
            return;

        Vector2 playerPos = new Vector2(
            HeroController.instance.transform.position.x,
            HeroController.instance.transform.position.y
        );

        var stateNullable = GetCurrentGameState();
        if (!stateNullable.HasValue)
            return;

        var state = stateNullable.Value;

        DrawRaycasts(playerPos, state);
    }

    private unsafe void DrawRaycasts(Vector2 origin, GameState state)
    {
        if (Camera.main == null)
            return;

        if (!lineMaterial)
        {
            lineMaterial = new Material(Shader.Find("Hidden/Internal-Colored"));
            if (lineMaterial == null)
                return;

            lineMaterial.hideFlags = HideFlags.HideAndDontSave;
            lineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            lineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            lineMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            lineMaterial.SetInt("_ZWrite", 0);
        }

        lineMaterial.SetPass(0);

        GL.PushMatrix();
        GL.LoadPixelMatrix();

        for (int i = 0; i < Constants.RayCount; i++)
        {
            float angle = (360f / Constants.RayCount) * i;
            float radians = angle * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));

            Vector3 startScreen = Camera.main.WorldToScreenPoint(new Vector3(origin.x, origin.y, 0));

            float dist = state.raycastDistances[i] * Constants.MaxRayDistance;
            RaycastHitType hitType = (RaycastHitType)state.raycastHitTypes[i];

            Color rayColor = GetRaycastColor(hitType);

            Vector2 endWorld = origin + direction * dist;
            Vector3 endScreen = Camera.main.WorldToScreenPoint(new Vector3(endWorld.x, endWorld.y, 0));

            GL.Begin(GL.LINES);
            GL.Color(rayColor);
            GL.Vertex3(startScreen.x, startScreen.y, 0);
            GL.Vertex3(endScreen.x, endScreen.y, 0);
            GL.End();

            if (hitType != RaycastHitType.None)
            {
                DrawCircle(endScreen, 2f, rayColor);
            }
        }

        GL.PopMatrix();
    }

    private void DrawCircle(Vector3 center, float radius, Color color)
    {
        GL.Begin(GL.QUADS);
        GL.Color(color);
        GL.Vertex3(center.x - radius, center.y - radius, 0);
        GL.Vertex3(center.x + radius, center.y - radius, 0);
        GL.Vertex3(center.x + radius, center.y + radius, 0);
        GL.Vertex3(center.x - radius, center.y + radius, 0);
        GL.End();
    }

    private unsafe void DrawRaycastsDebug(Vector2 origin, GameState state)
    {
        for (int i = 0; i < Constants.RayCount; i++)
        {
            float angle = (360f / Constants.RayCount) * i;
            float radians = angle * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
            Vector3 start = new Vector3(origin.x, origin.y, 0);

            float dist = state.raycastDistances[i] * Constants.MaxRayDistance;
            RaycastHitType hitType = (RaycastHitType)state.raycastHitTypes[i];

            Color rayColor = GetRaycastColor(hitType);

            Vector3 end = new Vector3(origin.x + direction.x * dist, origin.y + direction.y * dist, 0);
            Debug.DrawLine(start, end, rayColor);
        }
    }

    private static Color GetRaycastColor(RaycastHitType hitType)
    {
        return hitType switch
        {
            RaycastHitType.Terrain => RaycastColorTerrain,
            RaycastHitType.Enemy => RaycastColorEnemy,
            RaycastHitType.Projectile => RaycastColorProjectile,
            RaycastHitType.Hazard => RaycastColorHazard,
            _ => RaycastColorNone
        };
    }

    private Material lineMaterial;

    private GameState? GetCurrentGameState()
    {
        if (HeroController.instance == null)
            return null;

        return GameStateCollector.CollectGameState();
    }

    private string GetAttackStateName(BossAttackState state)
    {
        return state switch
        {
            BossAttackState.Idle => "Idle",
            BossAttackState.Hop => "Hop",
            BossAttackState.Pose => "Pose",
            BossAttackState.ComboSlashAntic => "Combo Antic",
            BossAttackState.ComboSlashAttack => "Combo Attack",
            BossAttackState.CounterAntic => "Counter Antic",
            BossAttackState.CounterStance => "Counter Stance",
            BossAttackState.CounterAttack => "Counter Attack",
            BossAttackState.RapidSlashAntic => "RapidSlash Antic",
            BossAttackState.RapidSlashAttack => "RapidSlash Attack",
            BossAttackState.JSlashAntic => "JSlash Antic",
            BossAttackState.JSlashAttack => "JSlash Attack",
            BossAttackState.DownstabAntic => "Downstab Antic",
            BossAttackState.DownstabAttack => "Downstab Attack",
            BossAttackState.ChargeAntic => "Charge Antic",
            BossAttackState.ChargeAttack => "Charge Attack",
            BossAttackState.CrossSlashAntic => "CrossSlash Antic",
            BossAttackState.CrossSlashAttack => "CrossSlash Attack",
            BossAttackState.Evade => "Evade",
            BossAttackState.Stun => "Stun",
            BossAttackState.Teleport => "Teleport",
            BossAttackState.PhaseTransition => "Phase Transition",
            BossAttackState.QuickSlashAttack => "Quick Slash",
            BossAttackState.Unknown => "Unknown",
            _ => "Invalid"
        };
    }

    private Color GetAttackStateColor(BossAttackState state)
    {
        return state switch
        {
            BossAttackState.Idle => Color.green,
            BossAttackState.Hop => new Color(0.5f, 1f, 0.5f),
            BossAttackState.Pose => new Color(0.3f, 0.8f, 0.3f),
            BossAttackState.ComboSlashAntic => Color.yellow,
            BossAttackState.CounterAntic => Color.yellow,
            BossAttackState.RapidSlashAntic => Color.yellow,
            BossAttackState.JSlashAntic => Color.yellow,
            BossAttackState.DownstabAntic => Color.yellow,
            BossAttackState.ChargeAntic => Color.yellow,
            BossAttackState.CrossSlashAntic => Color.yellow,
            BossAttackState.CounterStance => new Color(1f, 0.5f, 0f),
            BossAttackState.Evade => Color.cyan,
            BossAttackState.Stun => Color.magenta,
            BossAttackState.Teleport => Color.blue,
            BossAttackState.PhaseTransition => new Color(1f, 0f, 1f),
            BossAttackState.Unknown => Color.gray,
            _ => Color.red
        };
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
