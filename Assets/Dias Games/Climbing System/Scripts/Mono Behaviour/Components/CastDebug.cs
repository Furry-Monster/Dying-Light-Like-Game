using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace DiasGames.Debugging
{
    public class CastDebug : MonoBehaviour
    {
        private void OnDrawGizmos()
        {
            int count = sphereGizmosQueue.Count;
            for (int i = 0; i < count; i++)
            {
                Sphere sphere = sphereGizmosQueue.Dequeue();

                Gizmos.color = sphere.color;
                Gizmos.DrawWireSphere(sphere.center, sphere.radius);

                if (sphere.remainingTime > 0)
                {
                    sphere.remainingTime -= Time.deltaTime;
                    sphereGizmosQueue.Enqueue(sphere);
                }
            }

            count = capsuleGizmosQueue.Count;
            for (int i = 0; i < count; i++)
            {
                Capsule capsule = capsuleGizmosQueue.Dequeue();

                Gizmos.color = capsule.color;
                Gizmos.DrawWireSphere(capsule.bot, capsule.radius);
                Gizmos.DrawWireSphere(capsule.top, capsule.radius);
                
                for (int j = 0; j < 4; j++)
                {
                    float x = j == 0 ? 1 : j == 2 ? -1 : 0;
                    float y = j == 1 ? 1 : j == 3 ? -1 : 0;

                    Vector3 offset = new Vector3(x, 0, y) * capsule.radius;
                    Vector3 start = capsule.bot + offset;
                    Vector3 end = capsule.top + offset;

                    Gizmos.DrawLine(start, end);
                }

                if (capsule.remainingTime > 0)
                {
                    capsule.remainingTime -= Time.deltaTime;
                    capsuleGizmosQueue.Enqueue(capsule);
                }
            }

#if UNITY_EDITOR
            count = labelGizmos.Count;
            for (int i = 0; i < count; i++)
            {
                Label label = labelGizmos.Dequeue();

                var style = new GUIStyle();
                style.normal.textColor = label.color;
                style.fontSize = 20;

                Handles.Label(label.center, label.label, style);
                if (label.remainingTime > 0)
                {
                    label.remainingTime -= Time.deltaTime;
                    labelGizmos.Enqueue(label);
                }
            }
#endif
        }

        private Queue<Sphere> sphereGizmosQueue = new Queue<Sphere>();
        public void DrawSphere(Vector3 center, float radius, Color color, float duration = 0)
        {
            Sphere sphere = new Sphere();
            sphere.center = center;
            sphere.radius = radius;
            sphere.color = color;
            sphere.remainingTime = duration;

            sphereGizmosQueue.Enqueue(sphere);
        }

        private Queue<Capsule> capsuleGizmosQueue = new Queue<Capsule>();
        public void DrawCapsule(Vector3 p1, Vector3 p2, float radius, Color color, float duration = 0)
        {
            Capsule capsule = new Capsule();
            capsule.bot = p1;
            capsule.top = p2;
            capsule.radius = radius;
            capsule.color = color;
            capsule.remainingTime = duration;

            capsuleGizmosQueue.Enqueue(capsule);
        }

        private Queue<Label> labelGizmos = new Queue<Label>();
        public void DrawLabel(string text, Vector3 position, Color color, float duration = 0)
        {
            Label label = new Label();
            label.center = position;
            label.label = text;
            label.color = color;
            label.remainingTime = duration;

            labelGizmos.Enqueue(label);
        }
    }


    public class Label
    {
        public string label;
        public Vector3 center;
        public Color color;
        public float remainingTime;
    }

    public class Sphere
    {
        public Vector3 center;
        public float radius;
        public Color color;
        public float remainingTime;
    }
    public class Capsule
    {
        public Vector3 bot;
        public Vector3 top;
        public float radius;
        public Color color;
        public float remainingTime;
    }
}