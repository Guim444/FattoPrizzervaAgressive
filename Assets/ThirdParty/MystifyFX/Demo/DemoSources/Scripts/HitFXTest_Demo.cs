using UnityEngine;
using MystifyFX;

namespace MystifyFX.Demo {

    public class HitFXTest_Demo : MonoBehaviour {

        public MystifyEffect effect;

        void OnCollisionEnter (Collision collision) {
            foreach (ContactPoint contact in collision.contacts) {
                Hit(contact.point);
            }

            Destroy(collision.gameObject);
        }

        void Hit (Vector3 hitPosition) {
            // Uses custom values
            // effect.HitFX(hitPosition, intensity: 1f, color: Color.yellow, radius: 1.5f, speed: 1f);

            // Uses default values set in the inspector
            effect.HitFX(hitPosition);
        }
    }
}