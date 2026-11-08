using System.Collections;
using Spine.Unity;
using UnityEngine;

namespace MIeMie.Combat
{
    [RequireComponent(typeof(CombatHealth), typeof(Rigidbody2D))]
    public sealed class ForestScampTarget : MonoBehaviour
    {
        private static Sprite deadBodyProxySprite;
        [Header("资源引用（受击参数位于 ForestScampCombatSettings）")]
        [SerializeField] private Sprite deadBodySprite;

        private CombatHealth health;
        private Rigidbody2D body;
        private SkeletonAnimation spine;
        private Renderer spineRenderer;
        private ForestScampCombatSettings settings;
        private Vector3 debugAttackerPoint;
        private Vector3 debugTargetPoint;
        private Vector2 debugCalculatedDirection;
        private Vector2 debugResultVelocity;
        private float debugKnockbackUntil;

        private void Awake()
        {
            health = GetComponent<CombatHealth>();
            settings = ForestScampCombatSettings.Instance;
            body = GetComponent<Rigidbody2D>();
            spine = GetComponentInChildren<SkeletonAnimation>(true);
            spineRenderer = spine != null ? spine.GetComponent<Renderer>() : null;
            EnsureGroundPlaneAnchors();
            health.Hit += OnHit;
            health.Died += OnDied;
        }

        private void OnDestroy()
        {
            if (health == null)
                return;
            health.Hit -= OnHit;
            health.Died -= OnDied;
        }

        private void EnsureGroundPlaneAnchors()
        {
            RoomPlateGroundAnchor enemyAnchor = GetComponent<RoomPlateGroundAnchor>();
            if (enemyAnchor == null)
                enemyAnchor = gameObject.AddComponent<RoomPlateGroundAnchor>();
            enemyAnchor.SnapNow();

            Transform shadow = transform.Find("Scamp Ground Shadow");
            if (shadow == null)
                return;

            RoomPlateGroundAnchor shadowAnchor = shadow.GetComponent<RoomPlateGroundAnchor>();
            if (shadowAnchor == null)
                shadowAnchor = shadow.gameObject.AddComponent<RoomPlateGroundAnchor>();
            shadowAnchor.SnapNow();
        }

        private void OnHit(CombatHit hit)
        {
            Vector2 direction = ((Vector2)transform.position - hit.SourcePosition).normalized;
            debugAttackerPoint = new Vector3(hit.SourcePosition.x, hit.SourcePosition.y, transform.position.z);
            debugTargetPoint = transform.position;
            debugCalculatedDirection = direction;
            body.AddForce(direction * settings.rawKnockback * settings.knockbackModifier *
                          settings.knockbackIntegrationScale * hit.TargetKnockbackMultiplier,
                settings.knockbackForceMode);
            debugResultVelocity = Vector2.zero;
            debugKnockbackUntil = Time.unscaledTime + 1.5f;
            StartCoroutine(CaptureDebugVelocityAfterPhysics());
            CombatHitFx.SpawnHit(transform.position, direction);
            CombatCameraRig.Instance?.Shake(settings.genericHitShake * hit.TargetReactionShakeMultiplier, true);
            CombatCameraRig.Instance?.ShakeForDuration(settings.unitHitShakeMin * hit.TargetReactionShakeMultiplier,
                settings.unitHitShakeMax * hit.TargetReactionShakeMultiplier,
                settings.unitHitShakeDuration, false);
            CombatTime.HitStop(settings.normalHitStop * hit.NormalHitStopMultiplier);
            StartCoroutine(HitFlash());
        }

        private IEnumerator CaptureDebugVelocityAfterPhysics()
        {
            yield return new WaitForFixedUpdate();
            if (body != null)
                debugResultVelocity = body.velocity;
        }

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying || Time.unscaledTime > debugKnockbackUntil)
                return;

            DrawPoint(debugAttackerPoint, 0.12f, new Color(1f, 0.82f, 0.05f, 1f));
            DrawPoint(debugTargetPoint, 0.12f, new Color(0.05f, 0.95f, 1f, 1f));

            Gizmos.color = Color.white;
            Gizmos.DrawLine(debugAttackerPoint, debugTargetPoint);

            DrawArrow(debugTargetPoint, debugCalculatedDirection, 1.35f,
                new Color(0.1f, 1f, 0.2f, 1f));
            if (debugResultVelocity.sqrMagnitude > 0.0001f)
                DrawArrow(debugTargetPoint, debugResultVelocity.normalized, 0.9f,
                    new Color(1f, 0.15f, 1f, 1f));
        }

        private static void DrawPoint(Vector3 center, float radius, Color color)
        {
            Gizmos.color = color;
            const int segments = 24;
            Vector3 previous = center + Vector3.right * radius;
            for (int index = 1; index <= segments; index++)
            {
                float angle = index * Mathf.PI * 2f / segments;
                Vector3 next = center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius;
                Gizmos.DrawLine(previous, next);
                previous = next;
            }
        }

        private static void DrawArrow(Vector3 origin, Vector2 direction, float length, Color color)
        {
            if (direction.sqrMagnitude < 0.0001f)
                return;

            Vector2 normalized = direction.normalized;
            Vector3 end = origin + (Vector3)(normalized * length);
            Gizmos.color = color;
            Gizmos.DrawLine(origin, end);

            Vector2 perpendicular = new Vector2(-normalized.y, normalized.x);
            Vector3 headBack = end - (Vector3)(normalized * 0.22f);
            Gizmos.DrawLine(end, headBack + (Vector3)(perpendicular * 0.12f));
            Gizmos.DrawLine(end, headBack - (Vector3)(perpendicular * 0.12f));
        }

        private void OnDied(CombatHit hit)
        {
            Vector2 direction = ((Vector2)transform.position - hit.SourcePosition).normalized;
            CombatHitFx.SpawnDeath(transform.position, direction);
            CombatTime.HitStop(settings.deathHitStop * hit.DeathHitStopMultiplier);
            CombatCameraRig.Instance?.Shake(settings.deathShake * hit.TargetReactionShakeMultiplier, true);
            CreateDeadBodyProxy(hit.SourcePosition);
            gameObject.SetActive(false);
        }

        private IEnumerator HitFlash()
        {
            if (spineRenderer == null)
                yield break;
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            spineRenderer.GetPropertyBlock(block);
            block.SetColor("_Color", new Color(1f, 0.2f, 0.2f, 1f));
            spineRenderer.SetPropertyBlock(block);
            yield return new WaitForSecondsRealtime(settings.hitFlashDuration);
            block.SetColor("_Color", Color.white);
            spineRenderer.SetPropertyBlock(block);
        }

        private void CreateDeadBodyProxy(Vector2 sourcePosition)
        {
            GameObject deadBody = new GameObject("Dead Body - Human Grunt (integration proxy)");
            deadBody.name = "Dead Body - Human Grunt (integration proxy)";
            deadBody.transform.position = transform.position;
            deadBody.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(-25f, 25f));
            deadBody.transform.localScale = deadBodySprite != null ? Vector3.one : new Vector3(0.8f, 0.35f, 1f);
            SpriteRenderer spriteRenderer = deadBody.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = deadBodySprite != null ? deadBodySprite : GetDeadBodyProxySprite();
            spriteRenderer.color = deadBodySprite != null ? Color.white : new Color(0.32f, 0.08f, 0.1f, 1f);
            spriteRenderer.sortingOrder = 999 - Mathf.RoundToInt(transform.position.y * 100f);
            RoomPlateGroundAnchor groundAnchor = deadBody.AddComponent<RoomPlateGroundAnchor>();
            groundAnchor.SnapNow();
            Rigidbody2D deadBodyPhysics = deadBody.AddComponent<Rigidbody2D>();
            deadBodyPhysics.gravityScale = 0f;
            deadBodyPhysics.drag = settings.deadBodyDrag;
            deadBodyPhysics.constraints = RigidbodyConstraints2D.FreezeRotation;
            deadBodyPhysics.AddForce(((Vector2)transform.position - sourcePosition).normalized * settings.deadBodyForce);
        }

        private static Sprite GetDeadBodyProxySprite()
        {
            if (deadBodyProxySprite != null)
                return deadBodyProxySprite;

            const int width = 64;
            const int height = 32;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "DeadBodyProxyTexture",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            Color[] pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float nx = (x + 0.5f - width * 0.5f) / (width * 0.5f);
                    float ny = (y + 0.5f - height * 0.5f) / (height * 0.5f);
                    pixels[y * width + x] = nx * nx + ny * ny <= 1f ? Color.white : Color.clear;
                }
            }
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            deadBodyProxySprite = Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 64f);
            deadBodyProxySprite.name = "DeadBodyProxySprite";
            return deadBodyProxySprite;
        }
    }
}
