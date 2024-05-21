using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace DiasGames.Mobile 
{

    public class MobileButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public enum InputButton
        {
            Jump, Roll, Crouch, Crawl, Zoom, Interact, Drop, Pause
        }

        [SerializeField] private InputButton Button = InputButton.Jump;
        [SerializeField] private GameObject player;
        [Space]
        [SerializeField] private Image m_TargetGraphic = null;
        [SerializeField] private Color NormalColor = Color.white;
        [SerializeField] private Color PressedColor = Color.white;

        // Internal controller variables
        bool wasPressed = false;
        bool pressing = false;

        private void Awake()
        {
            if (player == null)
                player = GameObject.FindGameObjectWithTag("Player");
        }

        private void Update()
        {
            wasPressed = false;
            m_TargetGraphic.color = (pressing) ? PressedColor : NormalColor;
        }

        private void SendButtonMessage()
        {
            switch (Button)
            {
                case InputButton.Jump:
                    player.SendMessage("OnJump", wasPressed);
                    break;
                case InputButton.Roll:
                    player.SendMessage("OnRoll", wasPressed);
                    break;
                case InputButton.Crouch:
                    player.SendMessage("OnCrouch", pressing);
                    break;
                case InputButton.Crawl:
                    player.SendMessage("OnCrawl", wasPressed);
                    break;
                case InputButton.Zoom:
                    player.SendMessage("OnZoom", pressing);
                    break;
                case InputButton.Interact:
                    player.SendMessage("OnInteract", wasPressed);
                    break;
                case InputButton.Drop:
                    player.SendMessage("OnDrop", wasPressed);
                    break;
                case InputButton.Pause:
                    player.SendMessage("MobilePause", wasPressed);
                    break;
                default:
                    break;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            wasPressed = true;
            pressing = true;

            SendButtonMessage();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            pressing = false;

            SendButtonMessage();
        }
    }
}