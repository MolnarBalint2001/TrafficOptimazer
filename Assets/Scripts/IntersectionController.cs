

using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts;
using System.Globalization;
using System;
using System.Linq;

namespace Assets.Scripts
{

    /// <summary>
    /// Road intersection controller
    /// </summary>
    public class IntersectionController : MonoBehaviour
    {
        /// <summary>
        /// Unique identifier
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Traffic lights in the intersection
        /// </summary>
        public List<TrafficLightController> trafficLightsGroup { get; set; } = new List<TrafficLightController>();

        public void Intervention()
        {
            foreach (var trafficLight in trafficLightsGroup)
            {

                if (trafficLight.relatedTrafficLight != null)
                {

                }
                else
                {

                }
                trafficLight.greenTimer = 10f;
                trafficLight.redTimer = 5f;

                
            }
        }

        public int AllWaitingVehicles()
        {
            throw new NotImplementedException();
        }

        
        public float AvarageWaitingVehicles()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns that all traffic lights in the intersection are green
        /// </summary>
        /// <returns>Is all green</returns>
        public bool IsAllTrafficLightsGreen()
        {
            return trafficLightsGroup.All(x=>x.IsGreen());
        }

       




        
       

    }
}
