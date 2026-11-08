using UnityEngine;

namespace MIeMie.Combat
{
    [RequireComponent(typeof(Rigidbody2D))]
    [DisallowMultipleComponent]
    public sealed class PlayerMovementProof : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private OriginalPlayerSpineView spineView;

        private Rigidbody2D body;
        private Vector2 input;
        private Vector2 velocity;
        private float speed;
        private float facingAngle = 270f;
        private bool combatLocked;
        private float lungeTimer;
        private float lungeDuration;
        private float lungeSpeed;
        private Vector2 lungeDirection;
        private float dontComboKnockbackTimer;
        private Vector2 dontComboKnockbackVelocity;
        private GlobalCombatSettings settings;
        private PlayerSwordCombat swordCombat;
        private bool dodging;
        private bool dodgeHasAdvancedFrame;
        private float dodgeTimer;
        private float dodgeCooldownRemaining;
        private Vector2 dodgeDirection;

        public Vector2 Velocity => velocity;
        public float FacingAngle => facingAngle;
        public Vector2 RawInput => input;
        public float CurrentSpeed => speed;
        public bool IsDodging => dodging;
        public float DodgeCooldownRemaining => dodgeCooldownRemaining;
        public float LastLungeEntrySpeed { get; private set; }

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            settings = GlobalCombatSettings.Instance;
            swordCombat = GetComponent<PlayerSwordCombat>();
            CombatHealth health = GetComponent<CombatHealth>();
            if (health == null)
            {
                health = gameObject.AddComponent<CombatHealth>();
                health.Configure(CombatTeam.Player, 3f);
            }
            if (spineView == null)
                spineView = GetComponentInChildren<OriginalPlayerSpineView>(true);
        }

        private void Update()
        {
            input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            bool moving = input.magnitude >= settings.minimumInput;
            float normalizedFrame = Time.deltaTime * 60f;

            if (!dodging && dodgeCooldownRemaining > 0f)
                dodgeCooldownRemaining = Mathf.Max(0f, dodgeCooldownRemaining - Time.deltaTime);

            if (Input.GetKeyDown(KeyCode.Space))
                TryBeginDodge();

            if (dodging)
            {
                UpdateDodge(moving);
                return;
            }

            if (dontComboKnockbackTimer > 0f)
            {
                dontComboKnockbackTimer = Mathf.Max(0f, dontComboKnockbackTimer - Time.deltaTime);
                velocity = dontComboKnockbackVelocity;
                if (dontComboKnockbackTimer <= 0f)
                    velocity = Vector2.zero;
                return;
            }

            if (combatLocked)
            {
                lungeTimer -= Time.deltaTime;
                float targetSpeed = lungeDuration > 0.0001f
                    ? lungeSpeed * Mathf.Max(0f, lungeTimer / lungeDuration)
                    : 0f;
                speed += (targetSpeed - speed) / 3f * normalizedFrame;
                speed = Mathf.Max(speed, 0f);
                velocity = lungeDirection * speed;
                return;
            }

            if (moving)
            {
                input.Normalize();
                facingAngle = Mathf.Repeat(Mathf.Atan2(input.y, input.x) * Mathf.Rad2Deg, 360f);
                speed += (settings.runSpeed - speed) / settings.accelerationDivisor * normalizedFrame;
                velocity = input * speed;
            }
            else
            {
                speed += -speed / settings.idleDecelerationDivisor * normalizedFrame;
                if (speed < 0.01f)
                    speed = 0f;
                Vector2 facingDirection = new Vector2(
                    Mathf.Cos(facingAngle * Mathf.Deg2Rad),
                    Mathf.Sin(facingAngle * Mathf.Deg2Rad));
                velocity = facingDirection * speed;
            }

            spineView?.SetLocomotion(moving, facingAngle, speed, settings.runSpeed);
        }

        private bool TryBeginDodge()
        {
            if (dodging || dodgeCooldownRemaining > 0f)
                return false;
            if (combatLocked && swordCombat != null && !swordCombat.TryCancelForDodge())
                return false;

            combatLocked = false;
            lungeTimer = 0f;
            if (input.magnitude >= settings.minimumInput)
            {
                dodgeDirection = input.normalized;
                facingAngle = Mathf.Repeat(Mathf.Atan2(dodgeDirection.y, dodgeDirection.x) * Mathf.Rad2Deg, 360f);
            }
            else
            {
                dodgeDirection = DirectionFromAngle(facingAngle);
            }

            dodging = true;
            dodgeHasAdvancedFrame = false;
            dodgeTimer = 0f;
            dodgeCooldownRemaining = settings.dodgeCooldown;
            speed = settings.dodgeSpeed * settings.dodgeEntrySpeedMultiplier * settings.dodgeDistanceMultiplier;
            velocity = dodgeDirection * speed;
            spineView?.SetDodgeAnimation(facingAngle);
            return true;
        }

        private void UpdateDodge(bool moving)
        {
            dodgeTimer += Time.deltaTime;
            if (dodgeTimer < settings.dodgeSteerWindow && moving)
            {
                dodgeDirection = input.normalized;
                facingAngle = Mathf.Repeat(Mathf.Atan2(dodgeDirection.y, dodgeDirection.x) * Mathf.Rad2Deg, 360f);
                spineView?.SetDodgeAnimation(facingAngle);
            }

            if (dodgeHasAdvancedFrame)
                speed = Mathf.Lerp(speed, settings.dodgeSpeed, settings.dodgeSpeedLerpRate * Time.deltaTime);
            dodgeHasAdvancedFrame = true;
            velocity = dodgeDirection * speed;

            bool releasedAfterMinimum = !Input.GetKey(KeyCode.Space) && dodgeTimer > settings.dodgeMinimumDuration;
            if (releasedAfterMinimum || dodgeTimer > settings.dodgeMaximumDuration)
                EndDodge();
        }

        private void EndDodge()
        {
            dodging = false;
            dodgeHasAdvancedFrame = false;
            dodgeTimer = 0f;
            bool moving = input.magnitude >= settings.minimumInput;
            spineView?.SetLocomotion(moving, facingAngle, speed, settings.runSpeed);
        }

        public bool CancelDodgeForAttack()
        {
            if (!dodging)
                return false;

            dodging = false;
            dodgeHasAdvancedFrame = false;
            if (dodgeTimer > 0.05f)
                dodgeTimer = 0f;
            speed = settings.dodgeSpeed * settings.dodgeEntrySpeedMultiplier;
            velocity = dodgeDirection * speed;
            return true;
        }

        public void SetCombatLock(bool value)
        {
            combatLocked = value;
            if (!value)
                lungeTimer = 0f;
        }

        private static Vector2 DirectionFromAngle(float angle)
        {
            return new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
        }

        public void SetFacingAngle(float angle)
        {
            facingAngle = Mathf.Repeat(angle, 360f);
            if (combatLocked)
                lungeDirection = new Vector2(Mathf.Cos(facingAngle * Mathf.Deg2Rad), Mathf.Sin(facingAngle * Mathf.Deg2Rad));
        }

        public void StartLunge(float angle, float speedValue, float duration)
        {
            facingAngle = Mathf.Repeat(angle, 360f);
            lungeDirection = new Vector2(Mathf.Cos(facingAngle * Mathf.Deg2Rad), Mathf.Sin(facingAngle * Mathf.Deg2Rad));
            lungeSpeed = speedValue * settings.lungeDampener;
            lungeDuration = duration;
            lungeTimer = duration;
            LastLungeEntrySpeed = speed;
            if (!settings.inheritMovementSpeedOnLunge)
            {
                speed = 0f;
                velocity = Vector2.zero;
            }
        }

        public void CancelLungeFromDontCombo(float hitKnockback)
        {
            lungeTimer = 0f;
            lungeSpeed = 0f;
            speed = 0f;
            velocity = Vector2.zero;
            if (Mathf.Approximately(hitKnockback, 0f))
                return;

            dontComboKnockbackVelocity = -DirectionFromAngle(facingAngle) * (25f * hitKnockback);
            dontComboKnockbackTimer = 0.1f;
        }

        private void FixedUpdate()
        {
            body.MovePosition(body.position + velocity * Time.fixedDeltaTime);
        }
    }
}
