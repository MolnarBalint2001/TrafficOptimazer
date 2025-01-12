

using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

namespace Assets.Scripts
{

    /// <summary>
    /// Agent behaviour
    /// In this case our agents are the cars
    /// on the NavMesh
    /// </summary>
    public class AgentController : MonoBehaviour
    {

        /// <summary>
        /// Navigation agent
        /// </summary>
        private NavMeshAgent navAgent;

        /// <summary>
        /// Speed of the agent
        /// </summary>
        private float speed = 1f;

        /// <summary>
        /// Destination of the agent
        /// </summary>
        public Vector3 destination = new Vector3(-20, 0, 20);

        /// <summary>
        /// Start position of the agent
        /// </summary>
        public Vector3 startPosition;

        /// <summary>
        /// Nearest traffic light to the agent
        /// </summary>
        private TrafficLightController nearestLight;

        /// <summary>
        /// All traffic lights
        /// </summary>
        private List<TrafficLightController> trafficLights;

        /// <summary>
        /// Stopping distance from traffic lights
        /// </summary>
        private float stopDistance = 5f;

        /// <summary>
        /// All waypoints
        /// </summary>
        private List<Vector3> _waypoints { get; set; } = new List<Vector3>();


        private List<Vector3> _path = new List<Vector3>();


        private int currentNavPathIndex = 0;


        private Vector3 targetWaypoint;


        private void Start()
        {
            navAgent = gameObject.GetComponent<NavMeshAgent>();
            navAgent.speed = speed;


            if (_waypoints.Count > 0)
            {
                navAgent.SetDestination(destination);
            }
            navAgent.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            navAgent.transform.position = new Vector3(0, 0, 0);


            gameObject.GetComponent<Renderer>().material.color = Color.grey;

            trafficLights = new List<TrafficLightController>(FindObjectsOfType<TrafficLightController>());
        }



        private void Update()
        {  
            ObserveTrafficLights();
            if (!HasReachedPosition(targetWaypoint))
            {
                Debug.Log($"Elérte a cél waypointot: {targetWaypoint}");
                targetWaypoint = SelectClosestWaypointToPath();
                navAgent.SetDestination(targetWaypoint);
            }

        }


        private void ObserveTrafficLights()
        {

            foreach (var tfl in trafficLights)
            {
                float distanceToLight = Vector3.Distance(gameObject.transform.position, tfl.transform.position);


                if (distanceToLight <= stopDistance && tfl.IsRed())
                {
                    navAgent.isStopped = true;
                    break;
                }

                if (distanceToLight <= stopDistance && tfl.IsGreen())
                {
                    navAgent.isStopped = false;
                }
            }
        }


        Vector3 SelectClosestWaypointToPath()
        {
            if (!navAgent.hasPath || _waypoints.Count == 0)
                return _waypoints[0]; // Alapértelmezett waypoint, ha nincs path

            float closestDistance = float.MaxValue;
            Vector3 closestWaypoint = _waypoints[0];

            // Végigiterálunk az agent path cornerjein
            foreach (Vector3 corner in navAgent.path.corners)
            {
                foreach (Vector3 waypoint in _waypoints)
                {
                    float distance = Vector3.Distance(corner, waypoint);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestWaypoint = waypoint;
                    }
                }
            }

            Debug.Log($"Legközelebbi waypoint az útvonalhoz: {closestWaypoint}");
            return closestWaypoint;
        }

        bool HasReachedPosition(Vector3 targetPosition)
        {
            float distance = Vector3.Distance(navAgent.transform.position, targetPosition);
            return distance <= navAgent.stoppingDistance;
        }



        public void SetPosition(Vector3 position)
        {
            startPosition = position;
        }




        public void SetWaypoints(List<Vector3> waypoints)
        {
            _waypoints = waypoints;
        }


       



      
    }
}
