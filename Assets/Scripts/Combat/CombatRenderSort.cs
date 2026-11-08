using UnityEngine;

namespace MIeMie.Combat
{
    [DisallowMultipleComponent]
    public sealed class CombatRenderSort : MonoBehaviour
    {
        [SerializeField] private Renderer characterRenderer;
        [SerializeField] private Renderer shadowRenderer;
        [SerializeField] private int orderOffset = 1000;
        [SerializeField] private float unitsToOrder = 100f;

        public void Bind(Renderer character, Renderer shadow)
        {
            characterRenderer = character;
            shadowRenderer = shadow;
            ApplyOrder();
        }

        private void LateUpdate()
        {
            ApplyOrder();
        }

        private void ApplyOrder()
        {
            int order = orderOffset - Mathf.RoundToInt(transform.position.y * unitsToOrder);
            if (characterRenderer != null)
                characterRenderer.sortingOrder = order;
            if (shadowRenderer != null)
                shadowRenderer.sortingOrder = order - 1;
        }
    }
}
