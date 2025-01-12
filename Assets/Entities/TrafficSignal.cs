using UnityEngine;

namespace Assets.Entities
{

    /// <summary>
    /// Traffic signal object
    /// </summary>
    public class TrafficSignal
    {

        public enum TrafficSignalDirection
        {
            BOTH,
            LEFT,
            RIGHT,
            NONE
        }

        /// <summary>
        /// Direction
        /// </summary>
        public TrafficSignalDirection? Direction { get; set; }

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
