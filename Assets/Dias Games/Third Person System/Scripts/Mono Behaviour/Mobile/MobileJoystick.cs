using UnityEngine;
using UnityEngine.EventSystems;

namespace DiasGames.Mobile
{
    public class MobileJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [SerializeField] private GameObject player;
        public float m_MoveRange = 100;
        public bool IsCameraJoystick = false;

        [Space]
        public bool InvertX = false;
        public bool InvertY = false;

        private Touch m_CurrentTouch;

        private Canvas m_Canvas;
        private Vector2 targetPoint;

        [Space]
        [SerializeField] private RectTransform m_BackgroundRect = null;
        [SerializeField] private RectTransform m_HandleStickRect = null;

        protected void Awake()
        {
            m_Canvas = GetComponentInParent<Canvas>();

            if (player == null)
                player = GameObject.FindGameObjectWithTag("Player");
        }

        private void Start()
        {
            Vector2 center = new Vector2(0.5f, 0.5f);
            m_BackgroundRect.pivot = center;
            m_HandleStickRect.anchorMin = center;
            m_HandleStickRect.anchorMax = center;
            m_HandleStickRect.pivot = center;
            m_HandleStickRect.anchoredPosition = Vector2.zero;
        }

        private void Update()
        {
            float x = Mathf.Clamp(targetPoint.x, -1, 1) * (InvertX ? -1 : 1);
            float y = Mathf.Clamp(targetPoint.y, -1, 1) * (InvertY ? -1 : 1);

            if (IsCameraJoystick)
                player.SendMessage("OnLook", new Vector2(x, y));
            else
                player.SendMessage("OnMove", new Vector2(x, y));
        }

        public virtual void OnPointerDown(PointerEventData eventData)
        {
            OnDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            Camera cam = null;
            if (m_Canvas.renderMode == RenderMode.ScreenSpaceCamera)
                cam = m_Canvas.worldCamera;

            Vector2 position = RectTransformUtility.WorldToScreenPoint(cam, m_BackgroundRect.position);
            Vector2 radius = m_BackgroundRect.sizeDelta / 2;

            targetPoint = (eventData.position - position) / (radius * m_Canvas.scaleFactor);

            if (targetPoint.magnitude > 1)
                targetPoint.Normalize();

            m_HandleStickRect.anchoredPosition = targetPoint * radius * m_MoveRange;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            targetPoint = Vector2.zero;
            m_HandleStickRect.anchoredPosition = Vector2.zero;
        }
    }
}
