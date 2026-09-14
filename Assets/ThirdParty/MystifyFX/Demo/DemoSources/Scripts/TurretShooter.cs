using UnityEngine;

namespace MystifyFX.Demo {

    public class TurretShooter : MonoBehaviour {

        public GameObject bulletPrefab; // The bullet prefab to be instantiated
        public Transform firePoint; // The point from where the bullets will be shot
        public float fireRate = 1f; // Time interval between shots
        public float bulletLifetime = 5f; // Time after which the bullet will be destroyed
        public float bulletSpeed = 10f; // Speed at which the bullet is shot

        private float nextFireTime = 0f;

        void Update () {
            if (Time.time >= nextFireTime) {
                Shoot();
                nextFireTime = Time.time + 1f / fireRate;
            }
            transform.localRotation = Quaternion.Euler(0, Mathf.Sin(Time.time) * 10, 0);
        }

        void Shoot () {
            GameObject bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
            Rigidbody rb = bullet.GetComponent<Rigidbody>();
            rb.linearVelocity = firePoint.TransformDirection(firePoint.forward * bulletSpeed);
        }
    }
}