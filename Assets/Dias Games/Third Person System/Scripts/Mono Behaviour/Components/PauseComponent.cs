using UnityEngine;
using UnityEngine.InputSystem;

namespace DiasGames.Components
{
    public class PauseComponent : MonoBehaviour
    {
        [SerializeField] private GameObject pauseMenu;
        private bool _isPaused;

        CursorLockMode lockMode;
        bool visible;

        private void Start()
        {
            visible = Cursor.visible;
            lockMode = Cursor.lockState;
        }

        private void OnPause(InputValue value)
        {
            if (value.isPressed)
                OnPause(!_isPaused);
        }

        public void OnPause(bool paused)
        {
            _isPaused = paused;

            Time.timeScale = _isPaused ? 0f : 1f;
            if (pauseMenu)
                pauseMenu.SetActive(_isPaused);

            Cursor.visible = paused ? true : visible;
            Cursor.lockState = paused ? CursorLockMode.None : lockMode;
            
        }

        public void MobilePause(bool pressed)
        {
            if (pressed)
                OnPause(!_isPaused);
        }
    }
}