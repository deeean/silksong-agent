using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityStandardAssets.ImageEffects;

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

    private void Update()
    {
        if (!CommandLineArgs.NoFx) return;

        // F9 키로 렌더링 토글
        if (Input.GetKeyDown(KeyCode.F9))
        {
            CameraRenderScaledPatch.MinimalRendering = !CameraRenderScaledPatch.MinimalRendering;
            Plugin.Logger.LogInfo($"Minimal rendering: {CameraRenderScaledPatch.MinimalRendering}");
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

        // Audio 비활성화
        AudioListener.volume = 0f;
        AudioListener.pause = true;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (CommandLineArgs.NoFx)
        {
            DisableEffectRenderers();
            DisableCpuHeavyComponents();
        }
    }

    private void DisableCpuHeavyComponents()
    {
        // 높은 우선순위 - 디버그/프로파일링 컴포넌트
        DisableComponents<InputDebugger>();
        DisableComponents<PerformanceHud>();
        DisableComponents<CodeProfiler>();
        DisableComponents<CheatCodeListener>();

        // 중간 우선순위 - 렌더링 효과
        DisableComponents<RealtimeReflections>();
        DisableComponents<CameraBlurPlane>();
        DisableComponents<ColorCurvesManager>();

        // 추가 - 블러/파티클/풀 효과
        DisableComponents<BlurManager>();
        DisableComponents<LightBlurredBackground>();
        DisableComponents<SceneParticlesController>();
        DisableComponents<GrassBehaviour>();
        DisableComponents<Grass>();

        // 포스트 프로세싱 효과
        DisableComponents<BloomOptimized>();
        DisableComponents<LensCAAndDistortion>();
        DisableComponents<LightBlur>();

        // 환경 앰비언트 효과
        DisableComponents<AmbientFloat>();
        DisableComponents<AmbientSway>();
        DisableComponents<WaveEffectControl>();

        // 트레일/먼지/비네트 효과
        DisableComponents<TrackingTrail>();
        DisableComponents<CharacterLightDust>();
        DisableComponents<StatusVignette>();

        // 오디오 관련
        DisableComponents<AudioEventManager>();
        DisableComponents<AudioLoopMaster>();
        DisableComponents<AudioSourceFadeControl>();
        DisableComponents<AudioPlayWhenGrounded>();
        DisableComponents<AudioPlayStateSync>();
        DisableComponents<AnimatedVolumeControl>();
        DisableComponents<FadeAudioOnPause>();
        DisableComponents<FadeAudioOnAwake>();

        // 진동 관련
        DisableComponents<VibrationManagerUpdater>();
        DisableComponents<HeroVibrationRegion>();
        DisableComponents<AudioVibrationSyncer>();
        DisableComponents<AudioSyncedVibration>();
        DisableComponents<RainVibrationRegion>();
        DisableComponents<SpriteAlphaVibration>();

        // 시각 효과 추가
        DisableComponents<AmbientLightAnimator>();
        DisableComponents<AnimatedFadeGroup>();
        DisableComponents<ColourPainter>();
        DisableComponents<ColourDistanceSilhouette>();
        DisableComponents<DashEffect>();
        DisableComponents<FadeGroup>();
        DisableComponents<FlashMaterialGroup>();
        DisableComponents<SimpleFadeOut>();
        DisableComponents<SimpleSpriteFade>();
        DisableComponents<SpriteFadePulse>();
        DisableComponents<TK2DSpriteFadePulse>();
        DisableComponents<SpriteFlashDistanceSilhouette>();

        // 파티클/먼지 효과
        DisableComponents<DebrisParticle>();
        DisableComponents<CycloneDust>();
        DisableComponents<DriftflyCloud>();
        DisableComponents<WaterfallParticles>();
        DisableComponents<ParticleCulling>();

        // 환경 장식
        DisableComponents<Dragonfly>();
        DisableComponents<TinyMossFly>();
        DisableComponents<FakeBat>();
        DisableComponents<FloatingObject>();
        DisableComponents<IdleBuzzing>();
        DisableComponents<IdleBuzzingV2>();
        DisableComponents<MossClump>();

        // 적 타격 효과
        DisableComponents<EnemyHitEffectsRegular>();
        DisableComponents<EnemyHitEffectsGhost>();
        DisableComponents<EnemyHitEffectsBlackKnight>();
        DisableComponents<EnemyHitEffectsShade>();
        DisableComponents<EnemyHitEffectsArmoured>();
        DisableComponents<EnemyHitEffectsBasic>();
        DisableComponents<InfectedEnemyEffects>();

        // 플레이어 효과
        DisableComponents<JumpEffects>();
        DisableComponents<RunEffects>();
        DisableComponents<SoftLandEffect>();
        DisableComponents<HardLandEffect>();
        DisableComponents<HeroFallParticle>();

        // 회전/흔들림 효과
        DisableComponents<JitterSelfSimple>();
        DisableComponents<JitterFixPosition>();
        DisableComponents<JitterEnemyInside>();
        DisableComponents<LoopRotator>();
        DisableComponents<SpinSelfSimple>();
        DisableComponents<RandomTranslation>();

        // 스패터/페인트 효과
        DisableComponents<PaintSplat>();
        DisableComponents<PaintBullet>();
        DisableComponents<SpatterOrange>();
        DisableComponents<SpatterHoney>();
        DisableComponents<QuickBurn>();

        // 풀링 효과
        DisableComponents<PooledEffectManager>();

        // 조명/셰이더 효과
        DisableComponents<ThreadIlluminationTK2D>();
        DisableComponents<SpriteExtruder>();
        DisableComponents<SetGlobalShaderPos>();
        DisableComponents<SetGlobalShaderTime>();

        // 오디오 필터
        DisableComponents<LowPassDistance>();
        DisableComponents<SpawnableAudioSource>();
        DisableComponents<PlayAudioAndRecycle>();

        // 추가 포스트 프로세싱
        DisableComponents<UberPostprocess>();
        DisableComponents<DebandEffect>();
        DisableComponents<FastNoise>();

        // 카메라 효과
        DisableComponents<CameraFade>();
        DisableComponents<CameraShakeWhileEnabled>();
        DisableComponents<CameraShakeResponderMechanim>();
        DisableComponents<NewCameraNoise>();
    }

    private void DisableComponents<T>() where T : MonoBehaviour
    {
        var components = FindObjectsByType<T>(FindObjectsSortMode.None);
        foreach (var component in components)
        {
            component.enabled = false;
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
