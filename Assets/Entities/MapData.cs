
using System.Collections.Generic;
using Assets.Entities;
using UnityEngine;

namespace Assets.Entities
{

    /// <summary>
    /// Map data
    /// </summary>
    public class MapData
    {
        /// <summary>
        /// Map bounds
        /// </summary>
        Vector3[] Bounds = new Vector3[4];


        /// <summary>
        /// Traffic light list
        /// </summary>
        public List<TrafficSignal> TrafficLights { get; set; } = new List<TrafficSignal>();


        /// <summary>
        /// Road list
        /// </summary>
        public List<Road> Roads { get; set; } = new List<Road>();


        /// <summary>
        /// Building list
        /// </summary>
        public List<Building> Buildings { get; set; } = new List<Building>();

    }
}
