using System.Collections.Generic;
using UnityEngine;

namespace SilksongAgent;

public enum RaycastHitType
{
    None = 0,
    Terrain = 1,
    Enemy = 2,
    Projectile = 3,
    Hazard = 4
}

public struct RaycastResult
{
    public float distance;
    public RaycastHitType type;
}

public static class RaycastSensor
{
    private static readonly int TerrainMask = 1 << Constants.TerrainLayer;
    private static readonly int EnemyMask = 1 << Constants.EnemyLayer;
    private static readonly int ProjectileMask = 1 << Constants.ProjectileLayer;
    private static readonly int CombinedMask = TerrainMask | EnemyMask | ProjectileMask;

    private static List<(Vector2 position, float radius)> projectiles = new List<(Vector2, float)>();
    private static readonly RaycastHit2D[] hitBuffer = new RaycastHit2D[16];
    private static readonly Vector2[] rayDirections = new Vector2[Constants.RayCount];

    static RaycastSensor()
    {
        for (int i = 0; i < Constants.RayCount; i++)
        {
            float angle = (360f / Constants.RayCount) * i;
            float radians = angle * Mathf.Deg2Rad;
            rayDirections[i] = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
        }
    }

    public static void ClearProjectiles()
    {
        projectiles.Clear();
    }

    public static void AddProjectile(Vector2 position, float radius)
    {
        projectiles.Add((position, radius));
    }

    public static void PerformRaycast(Vector2 origin, out float[] distances, out RaycastHitType[] hitTypes)
    {
        distances = new float[Constants.RayCount];
        hitTypes = new RaycastHitType[Constants.RayCount];

        for (int i = 0; i < Constants.RayCount; i++)
        {
            Vector2 direction = rayDirections[i];

            int hitCount = Physics2D.RaycastNonAlloc(origin, direction, hitBuffer, Constants.MaxRayDistance);

            float closestDistance = Constants.MaxRayDistance;
            RaycastHitType hitType = RaycastHitType.None;

            float bossProjectileDistance = CheckBossProjectileInDirection(origin, direction);
            if (bossProjectileDistance < closestDistance)
            {
                closestDistance = bossProjectileDistance;
                hitType = RaycastHitType.Projectile;
            }

            for (int j = 0; j < hitCount; j++)
            {
                var hit = hitBuffer[j];
                if (hit.collider == null || hit.distance >= closestDistance)
                    continue;

                int layer = hit.collider.gameObject.layer;

                var healthManager = hit.collider.GetComponent<HealthManager>();
                if (healthManager != null)
                {
                    closestDistance = hit.distance;
                    hitType = RaycastHitType.Enemy;
                    continue;
                }

                var damageHero = hit.collider.GetComponent<DamageHero>();
                if (damageHero != null)
                {
                    closestDistance = hit.distance;
                    hitType = RaycastHitType.Hazard;
                    continue;
                }

                if (layer == Constants.ProjectileLayer)
                {
                    closestDistance = hit.distance;
                    hitType = RaycastHitType.Projectile;
                }
                else if (layer == Constants.EnemyLayer)
                {
                    closestDistance = hit.distance;
                    hitType = RaycastHitType.Enemy;
                }
                else if (layer == Constants.TerrainLayer)
                {
                    closestDistance = hit.distance;
                    hitType = RaycastHitType.Terrain;
                }
            }

            distances[i] = closestDistance;
            hitTypes[i] = hitType;
        }
    }

    private static float CheckBossProjectileInDirection(Vector2 origin, Vector2 direction)
    {
        float closestDistance = Constants.MaxRayDistance;

        foreach (var (position, radius) in projectiles)
        {
            float distance = RayCircleIntersection(origin, direction, position, radius);
            if (distance > 0 && distance < closestDistance)
            {
                closestDistance = distance;
            }
        }

        return closestDistance;
    }

    private static float RayCircleIntersection(Vector2 rayOrigin, Vector2 rayDir, Vector2 circleCenter, float radius)
    {
        Vector2 toCircle = circleCenter - rayOrigin;

        float projectionLength = Vector2.Dot(toCircle, rayDir);

        if (projectionLength < 0)
            return -1f;

        Vector2 closestPoint = rayOrigin + rayDir * projectionLength;
        float distanceToCenter = Vector2.Distance(closestPoint, circleCenter);

        if (distanceToCenter > radius)
            return -1f;

        float halfChord = Mathf.Sqrt(radius * radius - distanceToCenter * distanceToCenter);
        float intersectionDistance = projectionLength - halfChord;

        if (intersectionDistance < 0)
            intersectionDistance = projectionLength + halfChord;

        if (intersectionDistance > 0 && intersectionDistance <= Constants.MaxRayDistance)
            return intersectionDistance;

        return -1f;
    }

    public static float[] PerformRaycast(Vector2 origin)
    {
        PerformRaycast(origin, out float[] distances, out _);
        return distances;
    }

    public static void DrawDebugRays(Vector2 origin, float[] distances)
    {
        if (distances == null || distances.Length != Constants.RayCount)
            return;

        for (int i = 0; i < Constants.RayCount; i++)
        {
            float angle = (360f / Constants.RayCount) * i;
            float radians = angle * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));

            float actualDistance = distances[i] * Constants.MaxRayDistance;
            Vector2 endPoint = origin + direction * actualDistance;

            Color rayColor = distances[i] < 1.0f ? Color.green : Color.red;
            Debug.DrawLine(origin, endPoint, rayColor);
        }
    }
}
