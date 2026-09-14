using UnityEngine;

namespace MystifyFX.Demo {
    public class AutoRoll : MonoBehaviour {
        public Vector3 pointA;
        public Vector3 pointB;
        public float speed = 5f;

        private Vector3 targetPoint;

        void Start () {
            targetPoint = pointB;
        }

        void Update () {
            
            float step = speed * Time.deltaTime;
            Vector3 previousPosition = transform.position;
            transform.position = Vector3.MoveTowards(transform.position, targetPoint, step);

            if (Vector3.Distance(transform.position, targetPoint) < 0.001f) {
                targetPoint = targetPoint == pointA ? pointB : pointA;
            }

            // Calculate the direction of movement
            Vector3 direction = (transform.position - previousPosition).normalized;

            // Rotate the sphere to simulate rolling
            float distance = Vector3.Distance(transform.position, previousPosition);
            transform.Rotate(direction * (distance * 360 / (2 * Mathf.PI * transform.localScale.x)));
        }
    }

}