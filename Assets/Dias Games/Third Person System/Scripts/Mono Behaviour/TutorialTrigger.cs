using UnityEngine;

namespace DiasGames.Tutorial
{

    public class TutorialTrigger : MonoBehaviour
    {
        [SerializeField] private GameObject targetTutorial;

        private void Awake()
        {
            targetTutorial.SetActive(false);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
                targetTutorial.SetActive(true);
        }
        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
                targetTutorial.SetActive(false);
        }
    }
}