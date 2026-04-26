using System.Collections;
using System.Collections.Generic;
using UnityEngine;

    //let camera follow target
    public class CameraFollow : MonoBehaviour
    {
        public Transform target;
        public float lerpSpeed = 5.0f;

        private Vector3 offset = new Vector3(0, 0, -10);
        private Vector3 targetPos;

        private void LateUpdate()
        {
            if (target == null) return;

            targetPos = target.position + offset;
            transform.position = Vector3.Lerp(transform.position, targetPos, lerpSpeed * Time.deltaTime);
        }

        // Method to set target dynamically
        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            transform.position = target.position + offset;
        }
    }
