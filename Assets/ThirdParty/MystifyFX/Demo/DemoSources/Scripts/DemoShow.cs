using UnityEngine;
using System.Collections;
using UnityEngine.UI;

namespace MystifyFX.Demo {

    public class DemoShow : MonoBehaviour {

        [System.Serializable]
        public struct PoV {
            public Vector3 position;
            public Quaternion rotation;
            public string title;
        }

        public PoV[] povs;
        public RectTransform panelProgress;
        public Text text;
        public float pauseDuration = 2f; // Duration to pause at each PoV
        public float transitionDuration = 1f; // Duration of the transition between PoVs

        void Start() {
            StartCoroutine(TransitionCamera());
        }

        IEnumerator TransitionCamera() {
            int currentIndex = 0;
            while (true) {
                PoV currentPoV = povs[currentIndex];
                PoV nextPoV = povs[(currentIndex + 1) % povs.Length];

                // Transition to the next PoV
                float elapsedTime = 0f;
                while (elapsedTime < transitionDuration) {
                    float t = elapsedTime / transitionDuration;
                    t = t * t * (3f - 2f * t); // Smoothstep for easing
                    transform.position = Vector3.Lerp(currentPoV.position, nextPoV.position, t);
                    transform.rotation = Quaternion.Slerp(currentPoV.rotation, nextPoV.rotation, t);
                    elapsedTime += Time.deltaTime;
                    yield return null;
                }

                // Ensure final position and rotation are set
                transform.position = nextPoV.position;
                transform.rotation = nextPoV.rotation;
                text.text = "Effects: " + nextPoV.title;

                // Update progress bar during pause
                elapsedTime = 0f;
                while (elapsedTime < pauseDuration) {
                    float progress = elapsedTime / pauseDuration;
                    panelProgress.anchorMax = new Vector2(progress, panelProgress.anchorMax.y);
                    elapsedTime += Time.deltaTime;
                    yield return null;
                }

                // Reset progress bar
                panelProgress.anchorMax = new Vector2(0, panelProgress.anchorMax.y);

                // Move to the next PoV
                currentIndex = (currentIndex + 1) % povs.Length;
            }
        }
    }
}
