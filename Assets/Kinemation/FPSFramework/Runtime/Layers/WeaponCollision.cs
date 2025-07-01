// Designed by KINEMATION, 2023

using Kinemation.FPSFramework.Runtime.Core.Components;
using Kinemation.FPSFramework.Runtime.Core.Types;
using UnityEngine;

namespace Kinemation.FPSFramework.Runtime.Layers
{
    public class WeaponCollision : AnimLayer
    {
        [SerializeField] protected LayerMask layerMask;

        protected Vector3 start;
        protected Vector3 end;
        protected LocRot smoothPose;
        protected LocRot offsetPose;

        private void Awake()
        {
            // OPTIONAL: Remove Enemy layers from LayerMask if somehow included in Inspector
            int enemyLayer = LayerMask.NameToLayer("EnemyHead");
            int largeColliderLayer = LayerMask.NameToLayer("LargeColliders");

            // Remove EnemyHead and LargeCollider from mask if present
            int maskWithoutEnemy = layerMask & ~(1 << enemyLayer) & ~(1 << largeColliderLayer);
            layerMask = maskWithoutEnemy;
        }

        private void OnDrawGizmos()
        {
            if (!drawDebugInfo) return;

            Gizmos.color = Color.green;
            Gizmos.DrawLine(start, end);
        }

        protected void Trace()
        {
            var blockData = GetGunAsset().blockData;

            float traceLength = blockData.weaponLength;
            float startOffset = blockData.startOffset;
            float threshold = blockData.threshold;
            LocRot restPose = blockData.restPose;

            start = GetMasterPivot().position - GetMasterPivot().forward * startOffset;
            end = start + GetMasterPivot().forward * traceLength;

            if (Physics.Raycast(start, GetMasterPivot().forward, out RaycastHit hit, traceLength, layerMask))
            {
                // Double-check by code: ignore if enemy or large collider
                int hitLayer = hit.collider.gameObject.layer;
                if (hitLayer == LayerMask.NameToLayer("EnemyHead") || hitLayer == LayerMask.NameToLayer("LargeColliders"))
                {
                    offsetPose = LocRot.identity;
                    return;
                }

                float distance = (end - start).magnitude - (hit.point - start).magnitude;
                if (distance > threshold)
                {
                    offsetPose = restPose;
                }
                else
                {
                    offsetPose.position = new Vector3(0f, 0f, -distance);
                    offsetPose.rotation = Quaternion.Euler(0f, 0f, 15f * (distance / threshold));
                }
            }
            else
            {
                offsetPose = LocRot.identity;
            }
        }

        public override void UpdateLayer()
        {
            if (GetGunAsset() == null) return;

            Trace();
            smoothPose = CoreToolkitLib.Interp(smoothPose, offsetPose, 10f, Time.deltaTime);

            GetMasterIK().Offset(smoothPose.position, smoothLayerAlpha);
            GetMasterIK().Offset(smoothPose.rotation, smoothLayerAlpha);
        }
    }
}
