
using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Entities;
using Assets.Scripts;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace Assets.Services
{

    /// <summary>
    /// Global network service
    /// Analysing the network flow
    /// Store each vehicles actual lanes and paths
    /// </summary>
    public class GlobalNetworkService
    {
        /// <summary>
        /// Lanes and vehicles
        /// </summary>

        public static Dictionary<string, int> laneVehicleStore = new Dictionary<string, int>();

        /// <summary>
        /// Occuped nodes
        /// </summary>
        public static Dictionary<string, AStarAgentController> occupiedNodes = new Dictionary<string, AStarAgentController>();


        /// <summary>
        /// Traffic lights count
        /// </summary>
        public static int trafficLightsCount { get; set; }

        /// <summary>
        /// OSM graph
        /// </summary>
        public static GraphOSM graph { get; set; }



        /// <summary>
        /// Is any intersection collision happened
        /// </summary>
        public static bool intersectionCollisionOccured { get; set; } = false;

        /// <summary>
        /// Number of accidents
        /// </summary>
        public static int numberOfAccidents { get; set; } = 0;


        public static int actionTakenCount { get; set; } = 0;


        public static void RegisterNode(string nodeId, AStarAgentController vehicle)
        {

            if (!occupiedNodes.ContainsKey(nodeId))
            {
                occupiedNodes.Add(nodeId, vehicle);
            }

        }


        public static void UnregisterNode(string nodeId, AStarAgentController vehicle)
        {
            if (occupiedNodes.ContainsKey(nodeId) && occupiedNodes[nodeId].Id == vehicle.Id)
            {
                occupiedNodes.Remove(nodeId);
            }
        }



        public static bool IsNodeOccupied(string nodeId)
        {
            return occupiedNodes.ContainsKey(nodeId);
        }

        public static void InitializeStore(GraphOSM graph)
        {
            foreach (var node in graph.Nodes)
            {
                var splittedNodeId = node.Id.Split("_");
                string laneId = $"{splittedNodeId[0]}_{splittedNodeId[2]}_{splittedNodeId[3]}";
                if (!laneVehicleStore.ContainsKey(laneId))
                {
                    laneVehicleStore[laneId] = 0;
                }
            }
        }

        public static void AddVehicleToLane(AStarAgentController vehicle)
        {
            var splittedCurrentNodeId = vehicle.pathNodes[vehicle.currentWaypointIndex].Id.Split("_");
            string laneId = $"{splittedCurrentNodeId[0]}_{splittedCurrentNodeId[2]}_{splittedCurrentNodeId[3]}";

            laneVehicleStore[laneId] += 1;

        }

        public static void RemoveVehicleFromLane(AStarAgentController vehicle)
        {
            var splittedCurrentNodeId = vehicle.pathNodes[vehicle.currentWaypointIndex - 1]?.Id?.Split("_");
            string laneId = $"{splittedCurrentNodeId[0]}_{splittedCurrentNodeId[2]}_{splittedCurrentNodeId[3]}";

            if (laneVehicleStore[laneId] > 0)
            {
                laneVehicleStore[laneId] -= 1;
            }
           

        }


        public static int GetLanePressure(string trafficLightId)
        {

            string key = trafficLightId.Split("-")[1];
            if (laneVehicleStore.ContainsKey(key))
            {
                return laneVehicleStore[key];
            }

            return 0;
           
        }


        public static void PrintNetworkService()
        {
            foreach (var entry in laneVehicleStore)
            {
                //Debug.Log($"{entry.Key} - {entry.Value} vehicles.");
            }
        }


    }
}
