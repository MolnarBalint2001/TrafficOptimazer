using System.Collections.Generic;
using System.Data.Common;
using UnityEngine;

namespace Assets.Entities
{

    /// <summary>
    /// Traffic signal object
    /// </summary>
    public class TrafficSignal
    {

        public enum SignalDirection
        {
            BOTH,
            BACKWARD,
            FORWARD,
        }

        /// <summary>
        /// Unique identifier
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Direction
        /// </summary>
        public SignalDirection? Direction { get; set; }

        /// <summary>
        /// Location
        /// </summary>
        public Vector3 Position { get; set; }

        /// <summary>
        /// Traffic light string representation
        /// </summary>
        /// <returns>String representation</returns>
        public override string ToString()
        {
            return $"TrafficLight({Position.ToString()})";
        }


       

    }
}
