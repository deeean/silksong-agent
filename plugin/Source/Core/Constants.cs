namespace SilksongAgent;

public static class Constants
{
    public const int FramesPerStep = 2;

    public const int RayCount = 32;
    public const float MaxRayDistance = 25.0f;

    public const int TerrainLayer = 8;
    public const int EnemyLayer = 11;
    public const int ProjectileLayer = 12;

    public const float LaceCircleSlashRadius = 3.0f;
    public const float CircleSlashMultiRadius = 5.0f;

    public const int LaceBossMaxHealth = 800;

    public const float HeroSpawnX = 49.27f;
    public const float HeroSpawnY = 100.5677f;
    public const float BossSpawnX = 59.19379f;
    public const float BossSpawnY = 100.5931f;

    public const string BossTowerScene = "Song_Tower_01";
    public const string MenuTitleScene = "Menu_Title";
    public const string BossTowerEntryGate = "door_cutsceneEndLaceTower";

    public const int ConsecutiveFrameThreshold = 3;

    public const int NumPlayerAnimationStates = 83; // 82 animations + Unknown
}
