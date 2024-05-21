using UnityEngine;
using DiasGames.Components;
using System.Collections;
using UnityEngine.SceneManagement;

namespace DiasGames.Controller
{
    public class LevelController : MonoBehaviour
    {
        [SerializeField] private GameObject player = null;
        [SerializeField] private float delayToRestartLevel = 3f;

        // player components
        private Health _playerHealth;

        // controller vars
        private bool _isRestartingLevel;

        private void Awake()
        {
            if (player == null)
                player = GameObject.FindGameObjectWithTag("Player");

            _playerHealth = player.GetComponent<Health>();
        }

        private void OnEnable()
        {
            _playerHealth.OnDead += RestartLevel;
        }
        private void OnDisable()
        {
            _playerHealth.OnDead -= RestartLevel;
        }

        // Restarts the current level
        private void RestartLevel()
        {
            if (!_isRestartingLevel)
                StartCoroutine(OnRestart());
        }

        public void LoadScene(string name)
        {
            SceneManager.LoadScene(name);
        }

        private IEnumerator OnRestart()
        {
            _isRestartingLevel = true;

            yield return new WaitForSeconds(delayToRestartLevel);

            SceneManager.LoadScene(SceneManager.GetActiveScene().name);

            _isRestartingLevel = false;
        }
    }
}