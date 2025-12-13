using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace SilksongAgent;

public class NoFxManager : MonoBehaviour
{
    public static NoFxManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (CommandLineArgs.NoFx)
        {
            ApplyNoFxSettings();
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
    }

    private void ApplyNoFxSettings()
    {
        // Quality settings
        QualitySettings.vSyncCount = 0;
        QualitySettings.shadows = ShadowQuality.Disable;
        QualitySettings.pixelLightCount = 0;
        QualitySettings.antiAliasing = 0;
        QualitySettings.softParticles = false;
        QualitySettings.particleRaycastBudget = 0;
        QualitySettings.realtimeReflectionProbes = false;
        QualitySettings.softVegetation = false;
        QualitySettings.skinWeights = SkinWeights.OneBone;
        QualitySettings.lodBias = 0.3f;
        QualitySettings.maximumLODLevel = 2;
        QualitySettings.globalTextureMipmapLimit = 2;
        QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable;
        QualitySettings.billboardsFaceCameraPosition = false;

        // Rendering
        OnDemandRendering.renderFrameInterval = 1;
        GraphicsSettings.useScriptableRenderPipelineBatching = true;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (CommandLineArgs.NoFx)
        {
            DisableEffectRenderers();
        }
    }

    private void DisableEffectRenderers()
    {
        var particleRenderers = FindObjectsByType<ParticleSystemRenderer>(FindObjectsSortMode.None);
        foreach (var renderer in particleRenderers)
        {
            renderer.enabled = false;
        }

        var trailRenderers = FindObjectsByType<TrailRenderer>(FindObjectsSortMode.None);
        foreach (var renderer in trailRenderers)
        {
            renderer.enabled = false;
        }

        var lineRenderers = FindObjectsByType<LineRenderer>(FindObjectsSortMode.None);
        foreach (var renderer in lineRenderers)
        {
            renderer.enabled = false;
        }

        var lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
        foreach (var light in lights)
        {
            light.enabled = false;
        }

        var reflectionProbes = FindObjectsByType<ReflectionProbe>(FindObjectsSortMode.None);
        foreach (var probe in reflectionProbes)
        {
            probe.enabled = false;
        }

        var spriteMasks = FindObjectsByType<SpriteMask>(FindObjectsSortMode.None);
        foreach (var mask in spriteMasks)
        {
            mask.enabled = false;
        }

        var cameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
        foreach (var camera in cameras)
        {
            camera.allowHDR = false;
            camera.allowMSAA = false;
        }

        var animators = FindObjectsByType<Animator>(FindObjectsSortMode.None);
        foreach (var animator in animators)
        {
            animator.cullingMode = AnimatorCullingMode.CullCompletely;
        }

        var projectors = FindObjectsByType<Projector>(FindObjectsSortMode.None);
        foreach (var projector in projectors)
        {
            projector.enabled = false;
        }

        var lensFlares = FindObjectsByType<LensFlare>(FindObjectsSortMode.None);
        foreach (var flare in lensFlares)
        {
            flare.enabled = false;
        }
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}
