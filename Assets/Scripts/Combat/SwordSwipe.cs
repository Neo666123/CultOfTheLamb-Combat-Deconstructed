using System.Collections.Generic;
using UnityEngine;

namespace MIeMie.Combat
{
    [RequireComponent(typeof(CircleCollider2D), typeof(Rigidbody2D))]
    public sealed class SwordSwipe : MonoBehaviour
    {
        private const int GizmoCircleSegments = 64;
        private readonly HashSet<CombatHealth> hitTargets = new HashSet<CombatHealth>();
        private GameObject owner;
        private float damage;
        private float hitKnockback;
        private float targetKnockbackMultiplier;
        private float targetReactionShakeMultiplier;
        private float normalHitStopMultiplier;
        private float deathHitStopMultiplier;
        private CombatTeam team;

        public static SwordSwipe Create(GameObject owner, float facingAngle, float radius, float damage,
            float hitKnockback, float targetKnockbackMultiplier, float targetReactionShakeMultiplier,
            float normalHitStopMultiplier, float deathHitStopMultiplier,
            float hitboxDuration, CombatTeam team, string sourceName)
        {
            GameObject instance = new GameObject(sourceName + " Hitbox");
            Vector2 facing = new Vector2(
                Mathf.Cos(facingAngle * Mathf.Deg2Rad),
                Mathf.Sin(facingAngle * Mathf.Deg2Rad));
            instance.transform.position = (Vector2)owner.transform.position + facing * radius;
            instance.transform.rotation = Quaternion.Euler(0f, 0f, facingAngle);
            Rigidbody2D body = instance.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            CircleCollider2D collider = instance.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = radius;
            SwordSwipe swipe = instance.AddComponent<SwordSwipe>();
            swipe.owner = owner;
            swipe.damage = damage;
            swipe.hitKnockback = hitKnockback;
            swipe.targetKnockbackMultiplier = targetKnockbackMultiplier;
            swipe.targetReactionShakeMultiplier = targetReactionShakeMultiplier;
            swipe.normalHitStopMultiplier = normalHitStopMultiplier;
            swipe.deathHitStopMultiplier = deathHitStopMultiplier;
            swipe.team = team;
            Destroy(instance, Mathf.Max(0.001f, hitboxDuration));
            return swipe;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            CombatHealth health = other.GetComponentInParent<CombatHealth>();
            if (health == null || !hitTargets.Add(health))
                return;
            CombatHit hit = new CombatHit(owner, owner.transform.position, damage,
                targetKnockbackMultiplier, targetReactionShakeMultiplier,
                normalHitStopMultiplier, deathHitStopMultiplier);
            bool dealtDamage = health.DealDamage(hit, team);
            if (dealtDamage && health.DontCombo)
                owner.GetComponent<PlayerMovementProof>()?.CancelLungeFromDontCombo(hitKnockback);
        }

        private void OnDrawGizmos()
        {
            CircleCollider2D hitbox = GetComponent<CircleCollider2D>();
            if (hitbox == null || !hitbox.enabled || !gameObject.activeInHierarchy)
                return;

            Vector3 center = transform.TransformPoint(hitbox.offset);
            float radius = hitbox.radius * Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y));
            Color previousColor = Gizmos.color;
            Gizmos.color = team == CombatTeam.Enemy
                ? new Color(1f, 0.2f, 0.15f, 1f)
                : new Color(0.15f, 1f, 0.25f, 1f);

            Vector3 previous = center + Vector3.right * radius;
            for (int index = 1; index <= GizmoCircleSegments; index++)
            {
                float angle = index * Mathf.PI * 2f / GizmoCircleSegments;
                Vector3 next = center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius;
                Gizmos.DrawLine(previous, next);
                previous = next;
            }

            float crossSize = Mathf.Max(0.05f, radius * 0.12f);
            Gizmos.DrawLine(center - Vector3.right * crossSize, center + Vector3.right * crossSize);
            Gizmos.DrawLine(center - Vector3.up * crossSize, center + Vector3.up * crossSize);
            if (owner != null)
                Gizmos.DrawLine(owner.transform.position, center);

            Gizmos.color = previousColor;
        }
    }
}
