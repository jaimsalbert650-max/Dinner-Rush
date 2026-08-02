using UnityEngine;

namespace DinnerRush
{
    /// <summary>
    /// Swaps this object's SpriteRenderer to a sprite loaded from Resources by name, at Awake.
    /// Lets art be applied to pooled/prefab objects (pickups, etc.) without baking a scene reference.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class SpriteSkin : MonoBehaviour
    {
        [SerializeField] private string resourceName;

        private void Awake()
        {
            if (string.IsNullOrEmpty(resourceName)) return;
            var s = Resources.Load<Sprite>(resourceName);
            if (s == null) return;
            var sr = GetComponent<SpriteRenderer>();
            if (sr != null) { sr.sprite = s; sr.color = Color.white; }
        }
    }
}
