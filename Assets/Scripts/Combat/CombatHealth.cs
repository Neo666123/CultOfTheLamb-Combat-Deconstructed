using System;
using UnityEngine;

namespace MIeMie.Combat
{
    public enum CombatTeam { Player = 1, Enemy = 2 }

    public readonly struct CombatHit
    {
        public readonly GameObject Source;
        public readonly Vector2 SourcePosition;
        public readonly float Damage;
        public readonly float TargetKnockbackMultiplier;
        public readonly float TargetReactionShakeMultiplier;
        public readonly float NormalHitStopMultiplier;
        public readonly float DeathHitStopMultiplier;

        public CombatHit(GameObject source, Vector2 sourcePosition, float damage,
            float targetKnockbackMultiplier, float targetReactionShakeMultiplier,
            float normalHitStopMultiplier, float deathHitStopMultiplier)
        {
            Source = source;
            SourcePosition = sourcePosition;
            Damage = damage;
            TargetKnockbackMultiplier = Mathf.Max(0f, targetKnockbackMultiplier);
            TargetReactionShakeMultiplier = Mathf.Max(0f, targetReactionShakeMultiplier);
            NormalHitStopMultiplier = Mathf.Max(0f, normalHitStopMultiplier);
            DeathHitStopMultiplier = Mathf.Max(0f, deathHitStopMultiplier);
        }
    }

    [DisallowMultipleComponent]
    public sealed class CombatHealth : MonoBehaviour
    {
        [SerializeField] private CombatTeam team = CombatTeam.Enemy;
        [SerializeField] private float maximum = 3f;
        [SerializeField, Tooltip("原版 Health.DontCombo。启用时，命中者会取消当前冲刺，并按攻击段 HitKnockback 对自己施加短暂反冲；它不会改变本目标受到的击退力。")] private bool dontCombo;

        public event Action<CombatHit> Hit;
        public event Action<CombatHit> Died;

        public CombatTeam Team => team;
        public bool DontCombo => dontCombo;
        public float Current { get; private set; }
        public bool IsDead => Current <= 0f;

        private void Awake()
        {
            Current = maximum;
        }

        public void Configure(CombatTeam configuredTeam, float configuredMaximum)
        {
            team = configuredTeam;
            maximum = Mathf.Max(0.01f, configuredMaximum);
            Current = maximum;
        }

        public bool DealDamage(CombatHit hit, CombatTeam attackerTeam)
        {
            PlayerMovementProof playerMovement = GetComponent<PlayerMovementProof>();
            if (IsDead || attackerTeam == team || (playerMovement != null && playerMovement.IsDodging))
                return false;

            Current = Mathf.Max(0f, Current - hit.Damage);
            if (Current > 0f)
                Hit?.Invoke(hit);
            else
                Died?.Invoke(hit);
            return true;
        }
    }
}
