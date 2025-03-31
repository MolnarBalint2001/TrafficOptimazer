using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts
{
    public class WaitZoneController:MonoBehaviour
    {
        public TrafficLightController trafficLightController { get; set; }

        private BoxCollider boxCollider;

        private void Awake()
        {
            boxCollider = GetComponent<BoxCollider>();
        }

        private void OnTriggerEnter(Collider other)
        {
            
            if (other.CompareTag("Vehicle"))
            {
                trafficLightController.ObjectEnteredWaitZone(other.gameObject);
            }
        }

        private void OnTriggerExit(Collider other)
        {
           
            if (other.CompareTag("Vehicle"))
            {
                trafficLightController.ObjectExitedWaitZone(other.gameObject);
            }
        }



        private void OnDrawGizmos()
        {
            
            // Ellenőrizzük, hogy van-e BoxCollider
            if (boxCollider != null)
            {
                // Állítsd be a Gizmo színét
                Gizmos.color = Color.yellow;

                // Rajzolj egy drótkeretes (wireframe) kockát
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawWireCube(boxCollider.center, boxCollider.size);
            }
        }



    }
}
