using UnityEngine;

namespace MIeMie.Combat
{
    // Asset-integration proxy for the confirmed PlayerImpact/BloodImpact call chain.
    // Swipe's original SpriteRenderer is disabled; these visuals only appear after a real hit.
    public sealed class CombatHitFx : MonoBehaviour
    {
        private static Sprite circleSprite;
        private static Sprite splatterSprite;

        private Vector2 velocity;
        private float angularVelocity;
        private float lifetime;
        private float elapsed;
        private float fadeStart;
        private Vector3 startScale;
        private Vector3 endScale;
        private SpriteRenderer spriteRenderer;
        private float initialAlpha;

        public static void SpawnHit(Vector3 position, Vector2 direction)
        {
            ForestScampCombatSettings settings = ForestScampCombatSettings.Instance;
            if (direction.sqrMagnitude < 0.001f)
                direction = Vector2.right;
            direction.Normalize();

            SpawnImpact(position, direction, settings.impactColor, settings.impactScale, settings);
            SpawnBlood(position, direction, settings.bloodProxyColor, settings.normalBloodParticles, false, settings);
        }

        public static void SpawnDeath(Vector3 position, Vector2 direction)
        {
            ForestScampCombatSettings settings = ForestScampCombatSettings.Instance;
            if (direction.sqrMagnitude < 0.001f)
                direction = Vector2.right;
            direction.Normalize();

            SpawnImpact(position, direction, settings.impactColor, settings.impactScale * 1.2f, settings);
            SpawnBlood(position, direction, settings.bloodProxyColor, Mathf.Max(1, settings.deathBloodParticles), true, settings);
        }

        private static void SpawnImpact(Vector3 position, Vector2 direction, Color color, float scale,
            ForestScampCombatSettings settings)
        {
            float baseAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            for (int index = 0; index < 5; index++)
            {
                float spread = Mathf.Lerp(-48f, 48f, index / 4f);
                float length = Mathf.Lerp(0.48f, 0.75f, 1f - Mathf.Abs(spread) / 48f) * scale;
                Color pieceColor = Color.Lerp(color, new Color(1f, 0.82f, 0.55f, 1f), index % 2 == 0 ? 0.55f : 0.15f);
                CreatePiece("Player Hit Impact", position + Vector3.back * 0.35f, pieceColor,
                    new Vector3(length, 0.055f * scale, 1f),
                    Quaternion.Euler(-45f, 0f, baseAngle + spread),
                    direction * (0.25f + index * 0.04f), 0f, settings.impactDuration, 0.35f,
                    new Vector3(length * 1.35f, 0.01f, 1f), 80);
            }
        }

        private static void SpawnBlood(Vector3 position, Vector2 direction, Color color, int count, bool death,
            ForestScampCombatSettings settings)
        {
            for (int index = 0; index < count; index++)
            {
                Vector2 tangent = new Vector2(-direction.y, direction.x);
                Vector2 scatter = direction * Random.Range(0.35f, death ? 1.4f : 0.85f)
                    + tangent * Random.Range(-0.65f, 0.65f);
                float size = Random.Range(0.055f, death ? 0.14f : 0.1f);
                CreatePiece("Blood Splatter", position + Vector3.back * 0.25f, color,
                    new Vector3(size * Random.Range(1f, 2.2f), size, 1f),
                    Quaternion.Euler(-45f, 0f, Random.Range(0f, 360f)),
                    scatter, Random.Range(-180f, 180f), settings.bloodParticleDuration, 0.65f,
                    new Vector3(size * 0.45f, size * 0.45f, 1f), 70, true);
            }

            Color decalColor = settings.groundBloodProxyColor;
            float decalSize = death ? settings.deathGroundBloodSize : settings.normalGroundBloodSize;
            CreatePiece("Blood Ground Splatter", new Vector3(position.x, position.y, 0f), decalColor,
                new Vector3(decalSize * 1.7f, decalSize, 1f),
                Quaternion.Euler(0f, 0f, Random.Range(0f, 360f)),
                Vector2.zero, 0f, death ? settings.deathGroundBloodDuration : settings.groundBloodDuration, 0.7f,
                new Vector3(decalSize * 1.9f, decalSize * 1.1f, 1f), 20, true);
        }

        private static void CreatePiece(string objectName, Vector3 position, Color color, Vector3 scale,
            Quaternion rotation, Vector2 pieceVelocity, float spin, float duration, float fadeAt,
            Vector3 finalScale, int sortingOrder, bool useSplatterSprite = false)
        {
            GameObject piece = new GameObject(objectName);
            piece.transform.position = position;
            piece.transform.rotation = rotation;
            piece.transform.localScale = scale;
            SpriteRenderer renderer = piece.AddComponent<SpriteRenderer>();
            renderer.sprite = useSplatterSprite ? GetSplatterSprite() : GetCircleSprite();
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;

            // All proxy FX are 2D visuals. Keep their X/Y motion, but project their depth onto the
            // same screenshot-room plane so the perspective follow camera cannot create parallax.
            RoomPlateGroundAnchor groundAnchor = piece.AddComponent<RoomPlateGroundAnchor>();
            groundAnchor.SnapNow();

            CombatHitFx behaviour = piece.AddComponent<CombatHitFx>();
            behaviour.velocity = pieceVelocity;
            behaviour.angularVelocity = spin;
            behaviour.lifetime = duration;
            behaviour.fadeStart = Mathf.Clamp01(fadeAt);
            behaviour.startScale = scale;
            behaviour.endScale = finalScale;
            behaviour.spriteRenderer = renderer;
            behaviour.initialAlpha = color.a;
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            float normalized = elapsed / Mathf.Max(0.001f, lifetime);
            transform.position += (Vector3)(velocity * Time.deltaTime);
            transform.Rotate(0f, 0f, angularVelocity * Time.deltaTime, Space.Self);
            transform.localScale = Vector3.Lerp(startScale, endScale, normalized);

            if (normalized >= fadeStart)
            {
                float fade = 1f - Mathf.InverseLerp(fadeStart, 1f, normalized);
                Color color = spriteRenderer.color;
                color.a = initialAlpha * Mathf.Clamp01(fade);
                spriteRenderer.color = color;
            }

            if (normalized >= 1f)
                Destroy(gameObject);
        }

        private static Sprite GetCircleSprite()
        {
            if (circleSprite != null)
                return circleSprite;

            const int size = 32;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "CombatFxCircle",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float nx = (x + 0.5f - size * 0.5f) / (size * 0.5f);
                float ny = (y + 0.5f - size * 0.5f) / (size * 0.5f);
                float alpha = Mathf.Clamp01((1f - Mathf.Sqrt(nx * nx + ny * ny)) * 8f);
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            circleSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 32f);
            circleSprite.name = "CombatFxCircleSprite";
            return circleSprite;
        }

        private static Sprite GetSplatterSprite()
        {
            if (splatterSprite != null)
                return splatterSprite;

            const int size = 64;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "CombatFxBloodSplatter",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            Vector3[] lobes =
            {
                new Vector3(0f, 0f, 0.56f),
                new Vector3(-0.47f, 0.08f, 0.31f),
                new Vector3(0.43f, 0.18f, 0.27f),
                new Vector3(0.18f, -0.43f, 0.25f),
                new Vector3(-0.22f, 0.48f, 0.20f),
                new Vector3(0.70f, -0.22f, 0.12f),
                new Vector3(-0.68f, -0.31f, 0.10f)
            };
            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float nx = (x + 0.5f - size * 0.5f) / (size * 0.5f);
                float ny = (y + 0.5f - size * 0.5f) / (size * 0.5f);
                float alpha = 0f;
                for (int index = 0; index < lobes.Length; index++)
                {
                    Vector3 lobe = lobes[index];
                    float distance = Vector2.Distance(new Vector2(nx, ny), new Vector2(lobe.x, lobe.y));
                    alpha = Mathf.Max(alpha, Mathf.Clamp01((lobe.z - distance) * 18f));
                }
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            splatterSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 64f);
            splatterSprite.name = "CombatFxBloodSplatterSprite";
            return splatterSprite;
        }
    }
}
