using UnityEngine;

namespace DiasGames.Components
{
    public class DamageTrigger : MonoBehaviour
    {
        [SerializeField] private int DamagePoints = 50;
        [SerializeField] private string ignoreTag = string.Empty;

        private void OnTriggerEnter(Collider other)
        {
            if (!enabled || (!string.IsNullOrEmpty(ignoreTag) && other.CompareTag(ignoreTag))) return;

            IDamage damage;
            if (other.TryGetComponent(out damage))
                damage.Damage(DamagePoints);
        }
    }
}