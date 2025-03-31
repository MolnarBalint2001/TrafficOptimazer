using Assets.Entities;
using Assets.Services;
using System.Collections.Generic;
using UnityEngine;
using static Assets.Scripts.TrafficLightController;
using System;
using System.Linq;
using System.IO.Abstractions;

namespace Assets.Scripts
{

    /// <summary>
    /// A* agent controller
    /// handling vehicle logic
    /// </summary>
    public class AStarAgentController : MonoBehaviour
    {
        /// <summary>
        /// Unique identifier
        /// </summary>
        public Guid Id = Guid.NewGuid();

        /// <summary>
        /// Current Road Id
        /// </summary>
        [SerializeField]
        public string currentRoadId { get; set; }


        private enum VehicleType
        {
            CAR = 0,
            TRUCK = 1,
            MOTORCYClE = 2,
            BUS = 3,
        }

        /// <summary>
        /// Elapsed time
        /// </summary>
        public float elapsedTime = 0;

        /// <summary>
        /// A* search service
        /// </summary>
        private readonly AStarSearch aStarSearch = new AStarSearch();

        /// <summary>
        /// Waypoints
        /// </summary>
        public List<Vector3>? path { get; set; }

        /// <summary>
        /// Path nodes
        /// </summary>
        public List<Node>? pathNodes { get; set; }

        /// <summary>
        /// Visited nodes
        /// </summary>
        public List<Node> visitedNodes { get; set; } = new List<Node>();

        /// <summary>
        /// Current waypoint index
        /// </summary>
        public int currentWaypointIndex = 1;

        /// <summary>
        /// Speed
        /// </summary>
        public float speed = 0f;

        /// <summary>
        /// Maximal speed
        /// </summary>
        public float maxSpeed = 15f;

        /// <summary>
        /// Acceleration
        /// </summary>
        private const float acceleration = 0.2f;

        /// <summary>
        /// Deceleration
        /// </summary>
        private const float deceleration = 5f;

        /// <summary>
        /// Agent destination
        /// </summary>
        public Node goal { get; set; }

        /// <summary>
        /// NavMesh agent start position
        /// </summary>
        public Node start { get; set; }
        
        /// <summary>
        /// OSM graph
        /// </summary>
        public GraphOSM graphOSM { get; set; }

        /// <summary>
        /// Roads
        /// </summary>
        public List<Road> roads { get; set; }

        public bool IsLaneChanger { get; private set; }

        public bool isStopped { get; set; } = false;

        public Spawner spawner;


        private void Start()
        {
            Initialize();
        }

        private void Update()
        {
            MeasureElapsedTime();
            MoveToNextWaypoint();
        }

        private void FixedUpdate()
        {

            //Short sensors
            for (int i = 0; i < 10; i++)
            {
               
                float angle = -10f * (10 - 1) / 2 + i * 10;
                Quaternion rotation = Quaternion.Euler(0, angle, 0);
                Vector3 direction = rotation * transform.forward;

                RaycastHit hit;
                Ray ray = new Ray(transform.position + transform.forward, direction);
                if (Physics.Raycast(ray.origin, ray.direction, out hit, 1f))
                {
                    GameObject hitObject = hit.collider.gameObject;
                    if (hitObject.CompareTag("Vehicle"))
                    {

                        //Collided vehicle data
                        var otherController = hitObject.GetComponent<AStarAgentController>();
                        var otherCurrentWpIndex = otherController.currentWaypointIndex;
                        var otherPreviousNodeId = otherController.pathNodes[otherCurrentWpIndex - 1].Id;
                        var otherPreviousRoadId = otherPreviousNodeId.Split("_")[0];
                        var otherPreviousNodeDirection = otherPreviousNodeId.Split('_')[2];

                        // Actual vehicle data
                        var previousNodeId = pathNodes[currentWaypointIndex - 1].Id;
                        var previousRoadId = previousNodeId.Split("_")[0];
                        var previousNodeDirection = previousNodeId.Split("_")[2];


                        // Logical expression
                        bool isIntersectionCollision = otherPreviousRoadId != previousRoadId; 
                        if (isIntersectionCollision)
                        {

                            gameObject.GetComponent<Renderer>().material.color = Color.red;
                            hitObject.gameObject.GetComponent<Renderer>().material.color = Color.red;
                            GlobalNetworkService.intersectionCollisionOccured = true;
                            GlobalNetworkService.numberOfAccidents += 1;
                            Destroy(hitObject, 0f);
                            Destroy(gameObject, 0f);
                        }
                       
                    }
                }

                
                Debug.DrawRay(ray.origin, ray.direction * 0.5f, hit.collider ? Color.red : Color.green);
            }


            // Middle sensors
            for (int i = 0; i < 8; i++)
            {
               
                float angle = -10f * (8 - 1) / 2 + i * 10;
                Quaternion rotation = Quaternion.Euler(0, angle, 0);
                Vector3 direction = rotation * transform.forward;

                RaycastHit hit;
                Ray ray = new Ray(transform.position + transform.forward, direction);
                if (Physics.Raycast(ray.origin, ray.direction, out hit, 1.5f))
                {
                    GameObject hitObject = hit.collider.gameObject;
                    if (hitObject.CompareTag("Vehicle"))
                    {
                        speed = 0f;
                        isStopped = true;  
                    }

                    if (hitObject.CompareTag("TrafficLight"))
                    {
                        var trafficLightController = hitObject.GetComponent<TrafficLightController>();
                        if (trafficLightController.IsRed())
                        {
                            speed = 0f;
                            isStopped = true;
                        }

                        if (trafficLightController.IsGreen() || (trafficLightController.IsYellow() && trafficLightController.previousState == LightState.Green))
                        {
                            isStopped = false;
                            speed = Mathf.Lerp(speed, maxSpeed, Time.deltaTime * acceleration);
                        }
                        
                    }
                }
                else
                {
                    isStopped = false;
                    speed = Mathf.Lerp(speed, maxSpeed, Time.deltaTime * acceleration);
                }

               
                Debug.DrawRay(ray.origin, ray.direction * 1.5f, hit.collider ? Color.red : Color.yellow);
            }

            #region Remote sensors
            for (int i = 0; i < 4; i++)
            {
                
                float angle = -2.5f * (4 - 1) / 2 + i * 2.5f;
                Quaternion rotation = Quaternion.Euler(0, angle, 0);
                Vector3 direction = rotation * transform.forward;

                RaycastHit hit;
                Ray ray = new Ray(transform.position + transform.forward * 0.6f, direction);
                if (Physics.Raycast(ray.origin, ray.direction, out hit, speed * 0.8f))
                {
                    GameObject hitObject = hit.collider.gameObject;
                    if (hitObject.CompareTag("Vehicle"))
                    {
                        var otherController = hitObject.GetComponent<AStarAgentController>();
                        var otherSpeed = otherController.speed;

                        speed = Mathf.Lerp(speed, otherSpeed, Time.deltaTime * deceleration);
                    }

                    if (hitObject.CompareTag("TrafficLight"))
                    {
                        var trafficLightController = hitObject.GetComponent<TrafficLightController>();
                        if (trafficLightController.IsRed())
                        {
                            speed = Mathf.Lerp(speed, 0, Time.deltaTime * deceleration);
                        }
                    }
                }

                else
                {
                    speed = Mathf.Lerp(speed, maxSpeed, Time.deltaTime * acceleration);
                }

                
                Debug.DrawRay(ray.origin, ray.direction * speed * 0.8f, hit.collider ? Color.red : Color.magenta);
            }

            #endregion





        }

        /// <summary>
        /// Initialize vehicle settings
        /// Path finding
        /// Phsyics settings
        /// </summary>
        private void Initialize()
        {
            var foundPath = aStarSearch.FindPathV2(start.Id, goal.Id, graphOSM);
            
            if (foundPath == null || foundPath.Count == 0 || foundPath.Count == 1)
            {
                DestroySelf();
                return;
            }
           
            pathNodes = foundPath;
            path = foundPath.Select(x => x.Position).ToList();
           
            GlobalNetworkService.AddVehicleToLane(this);

            transform.position = new Vector3(start.Position.x, 0.25f, start.Position.z);
            transform.localScale = new Vector3(0.8f, 0.5f, 2f);
            var collider = gameObject.GetComponent<BoxCollider>();
            collider.size = new Vector3(1f, 1, 1f);
            collider.center = new Vector3(0, 0, 0f);
            collider.isTrigger = true;
        }

        /// <summary>
        /// Move to the next waypoint in the path
        /// </summary>
        private void MoveToNextWaypoint()
        {

            
            if (currentWaypointIndex >= path?.Count - 1 || Vector3.Distance(transform.position, goal.Position) == 0)
            {
                GlobalNetworkService.RemoveVehicleFromLane(this);
                DestroySelf();
                return;
            }


            if (isStopped)
            {
                return;
            }

         
            Vector3 target = path[currentWaypointIndex];
           
            Vector3 transformedTarget = new Vector3(target.x, 0.25f, target.z);
            Node targetNode = pathNodes[currentWaypointIndex];
            Node previousNode = pathNodes[currentWaypointIndex - 1];


            SetIsLangeChanger(targetNode, previousNode);


            Vector3 direction = (transformedTarget - transform.position).normalized;
            Quaternion targetRotation = Quaternion.LookRotation(direction);

            transform.position = Vector3.MoveTowards(transform.position, transformedTarget, Time.deltaTime * speed);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);



            if (direction != Vector3.zero)
            {
                transform.forward = Vector3.Slerp(transform.forward, direction, Time.deltaTime * speed / 2);
            }

            if (Vector3.Distance(transform.position, transformedTarget) == 0)
            {
               
                currentWaypointIndex++;
                visitedNodes.Add(pathNodes[currentWaypointIndex - 1]);
                GlobalNetworkService.AddVehicleToLane(this);
                GlobalNetworkService.RemoveVehicleFromLane(this);

            }


        }

        #region Timers
        /// <summary>
        /// Measuring the elapsed time between start and goal
        /// Observable metrics
        /// </summary>
        private void MeasureElapsedTime()
        {
           if (goal != null || transform.position != goal.Position)
            {
                elapsedTime += Time.deltaTime;

            }
        }




        #endregion

        public void DestroySelf()
        {
            Destroy(this.gameObject);
        }


        private void SetIsLangeChanger(Node targetNode, Node previousNode)
        {

            var targetSplitted = targetNode.Id.Split("_");
            var previousSplitted = previousNode.Id.Split("_");

            var targetRoadId = targetSplitted.First();
            var previousRoadId = previousSplitted.First();

            var targetLaneIndex = targetSplitted.Last();
            var prevLaneIndex = previousSplitted.Last();

            if (targetRoadId == previousRoadId && targetLaneIndex != prevLaneIndex)
            {
                IsLaneChanger = true;
            }
            else
            {
                IsLaneChanger = false;
            }
        }



        /*#region Collision triggers

        private void OnTriggerEnter(Collider other)
        {

            if (other.gameObject.CompareTag("TrafficLight"))
            {

                TrafficLightController tflController = other.gameObject.GetComponent<TrafficLightController>();
                LightState lightStatus = tflController.currentState;
                if (lightStatus == LightState.Red)
                {
                    isStopped = true;
                    speed = 0f;
                    currentTrafficLight = other.gameObject;
                }

            }

            else if(other.gameObject.CompareTag("Vehicle"))
            {
                var controller = other.gameObject.GetComponent<AStarAgentController>();

                if (controller.isStopped)
                {
                    isStopped = true;
                }
                else
                {
                    speed = controller.speed - 2f;
                }
            }
        }



        private void OnTriggerStay(Collider other)
        {
            if (other.gameObject.CompareTag("TrafficLight"))
            {
                TrafficLightController controller = other.gameObject.GetComponent<TrafficLightController>();
                LightState currentState = controller.currentState;
                LightState prevState = controller.previousState;
                if (currentState == LightState.Green)
                {
                    isStopped = false;
                    speed = 15f;
                }
                
                else if (currentState == LightState.Yellow && prevState == LightState.Green)
                {
                    isStopped = false;
                    speed = 15f;
                }

                else
                {
                    isStopped = true;
                    
                }
            }


            else if(other.gameObject.CompareTag("Vehicle"))
            {
                Debug.Log("Vehicle Detected: " + other.gameObject.name);
                var controller = other.gameObject.GetComponent<AStarAgentController>();
                if (controller.isStopped)
                {
                    isStopped = true;
                    speed = 0f;  // Leállítjuk, ha a másik autó is áll
                }
                else
                {
                    speed = Mathf.Max(controller.speed - 2f, 0f);  // Lassítunk, ha közel van egy másik jármű
                }
            }


        }




        private void OnTriggerExit(Collider other)
        {
            if (other.gameObject.CompareTag("TrafficLight"))
            {
                TrafficLightController controller = other.gameObject.GetComponent<TrafficLightController>();
                LightState status = controller.currentState;
                if (status != LightState.Red)
                {
                    currentTrafficLight = null;
                }
            }


            else if(other.gameObject.CompareTag("Vehicle"))
            {
                speed = 15f;
                isStopped = false;
            }
        }

        #endregion*/


    }
}
