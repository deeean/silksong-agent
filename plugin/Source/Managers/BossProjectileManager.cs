using System.Collections.Generic;
using HutongGames.PlayMaker;
using UnityEngine;

namespace SilksongAgent;

public class BossProjectileManager : MonoBehaviour
{
    public static BossProjectileManager Instance { get; private set; }

    private HashSet<int> _dealtDamageProjectiles = new HashSet<int>();
    private HashSet<int> _activeColliderProjectiles = new HashSet<int>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void RefreshProjectileCache()
    {
        RaycastSensor.ClearProjectiles();

        var allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        var currentCircleSlashIds = new HashSet<int>();

        foreach (var obj in allObjects)
        {
            if (obj == null || !obj.activeInHierarchy) continue;

            if (obj.name.Contains("lace_circle_slash"))
            {
                var damagerTransform = obj.transform.Find("damager");
                if (damagerTransform == null) continue;

                int projectileId = obj.GetInstanceID();
                currentCircleSlashIds.Add(projectileId);

                var damagerCollider = damagerTransform.GetComponent<Collider2D>();
                bool colliderEnabled = damagerCollider != null && damagerCollider.enabled;

                if (colliderEnabled)
                {
                    _activeColliderProjectiles.Add(projectileId);
                }
                else if (_activeColliderProjectiles.Contains(projectileId))
                {
                    _activeColliderProjectiles.Remove(projectileId);
                    _dealtDamageProjectiles.Add(projectileId);
                }

                if (_dealtDamageProjectiles.Contains(projectileId)) continue;

                RaycastSensor.AddProjectile(damagerTransform.position, Constants.LaceCircleSlashRadius);
            }
            else if (obj.name == "Cross Slash")
            {
                var heroDamagerTransform = obj.transform.Find("hero damager");
                if (heroDamagerTransform == null) continue;

                var fsm = obj.GetComponent<PlayMakerFSM>();
                if (fsm == null) continue;

                if (fsm.ActiveStateName != "Idle" && fsm.ActiveStateName != "Attacking") continue;

                RaycastSensor.AddProjectile(heroDamagerTransform.position, Constants.CircleSlashMultiRadius);
            }
        }

        _dealtDamageProjectiles.RemoveWhere(id => !currentCircleSlashIds.Contains(id));
        _activeColliderProjectiles.RemoveWhere(id => !currentCircleSlashIds.Contains(id));
    }

    public void ClearProjectileCache()
    {
        RaycastSensor.ClearProjectiles();
        _dealtDamageProjectiles.Clear();
        _activeColliderProjectiles.Clear();
    }
}