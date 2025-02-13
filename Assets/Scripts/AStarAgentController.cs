using Assets.Entities;
using Assets.Services;
using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts;
using static Assets.Scripts.TrafficLightController;
using System.Transactions;
using System.Security.Cryptography;

namespace Assets.Scripts
{

    /// <summary>
    /// A* agent controller
    /// handling vehicle logic
    /// </summary>
    public class AStarAgentController : MonoBehaviour
    {
        private enum VehicleType
        {
            CAR = 0,
            TRUCK = 1,
            MOTORCYClE = 2,
            BUS = 3,
        }

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
        /// Current waypoint index
        /// </summary>
        public int currentWaypointIndex = 1;

        /// <summary>
        /// Renderer
        /// </summary>
        private Renderer renderer;

        /// <summary>
        /// Speed
        /// </summary>
        public float speed = 15f;

        /// <summary>
        /// Maximal speed
        /// </summary>
        private float maxSpeed = 20f;

        /// <summary>
        /// Minimal turn speed
        /// </summary>
        private float turnSpeed = 2f;

        /// <summary>
        /// Target speed
        /// </summary>
        private float targetSpeed;

        /// <summary>
        /// Agent destination
        /// </summary>
        public Node goal { get; set; }

        /// <summary>
        /// NavMesh agent start position
        /// </summary>
        public Node start { get; set; }

        /// <summary>
        /// Graph
        /// </summary>
        public Dictionary<string, List<Node>> graph { get; set; }

        /// <summary>
        /// Collection of nodes
        /// </summary>
        public Dictionary<string, Node> nodes { get; set; }

        /// <summary>
        /// Roads
        /// </summary>
        public List<Road> roads { get; set; }

        private Vector3 velocity = Vector3.zero;

        public bool isStopped { get; set; } = false;

        private GameObject? currentTrafficLight { get; set; }


        private Rigidbody rb;


        public Spawner spawner;
       

        void Start()
        {
           
            rb = GetComponent<Rigidbody>();
            Initialize();
        }

        void Update()
        {

            MeasureElapsedTime();
            if (path == null)
            {
                System.Random random = new System.Random();
                int rri = random.Next(0, roads.Count);
                int rni = random.Next(0, roads[rri].ControlPoints.Count);
                goal = roads[rri].ControlPoints[rni];
                path = aStarSearch.FindPath(graph, start, goal, nodes);
                TransformPath();
            }
            else
            {
                MoveToNextWaypoint();
            }
        }

        private void Initialize()
        {
            
            System.Random random = new System.Random();
            int rri = random.Next(0, roads.Count);
            int rni = random.Next(0, roads[rri].ControlPoints.Count);
            goal = roads[rri].ControlPoints[rni];
            path = aStarSearch.FindPath(graph, start, goal, nodes);
            gameObject.transform.position = new Vector3(start.Position.x, 1, start.Position.z);
            transform.localScale = new Vector3(1, 1, 2.5f);
            var collider = gameObject.GetComponent<BoxCollider>();
            collider.size = new Vector3(1.5f, 1, 1.5f);
            collider.center = new Vector3(0, 0, 0.25f);
            collider.isTrigger = true;
            TransformPath();
        }

        private void TransformPath()
        {

            if (path == null) return;
            List<Vector3> transformedPath = new List<Vector3>();

            for (int i = 0; i < path.Count; i++)
            {
                Vector3 current = path[i];
                Vector3 direction = Vector3.zero;
                if (i == path.Count - 1)
                {
                    direction = path[i - 1] - path[i];
                }

                if (i < path.Count - 1)
                {
                    direction = path[i + 1] - path[i];
                }


                Vector3 right = Vector3.Cross(Vector3.up, direction).normalized;
                Vector3 transformed = new Vector3((current + right * 2f).x, 1, (current + right * 2f).z);

                transformedPath.Add(transformed);

            }


            path = transformedPath;


        }

        private void MoveToNextWaypoint()
        {


            
            if (path == null || currentWaypointIndex >= path?.Count - 1 || Vector3.Distance(transform.position, goal.Position) == 0)
            {
                
                Destroy(this.gameObject);
                spawner.SetDestroyed();
                return;
            }

            if (isStopped)
            {
                return;
            }


            Vector3 target = path[currentWaypointIndex];
            Vector3 direction = (target - transform.position).normalized;
            transform.position = Vector3.MoveTowards(transform.position, target, Time.deltaTime * speed);


            if (direction != Vector3.zero)
            {
                transform.forward = Vector3.Slerp(transform.forward, direction, Time.deltaTime * speed / 2);
            }


            /*Vector3 nextWaypoint = path[currentWaypointIndex + 1];
            if (nextWaypoint != null)
            {
                var dir1 = (path[currentWaypointIndex] - transform.position).normalized;
                var dir2 = (nextWaypoint - path[currentWaypointIndex]).normalized;
                float turnAngle = Vector3.Angle(dir1, dir2);


                if (Vector3.Distance(transform.position, target) < 5f)
                {
                    
                    targetSpeed = Mathf.Lerp(maxSpeed, turnSpeed, Mathf.InverseLerp(0, 90f, turnAngle));
                }

            }


            speed = Mathf.SmoothDamp(speed, targetSpeed, ref velocity.z, 0.5f);*/


            if (Vector3.Distance(transform.position, target) == 0)
            {

                currentWaypointIndex++;
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

      
        public void DestroySelf()
        {
            Destroy(this.gameObject);
        }

        #endregion


        #region Collision triggers

        private void OnTriggerEnter(Collider other)
        {

            if (other.gameObject.CompareTag("TrafficLight"))
            {
                
                TrafficLightController tflController = other.gameObject.GetComponent<TrafficLightController>();
                LightState lightStatus = tflController.currentState;
                if (lightStatus == LightState.Red) {
                    isStopped = true;
                    speed = 0f;
                    currentTrafficLight = other.gameObject;
                }

            }

            if (other.gameObject.CompareTag("Vehicle"))
            {

                if (other.CompareTag("Front") || other.CompareTag("Body"))
                {
                    gameObject.GetComponent<Renderer>().material.color = Color.red;
                    isStopped = true;
                    Destroy(this.gameObject, 2f);
                    spawner.SetDestroyed();
                   
                }
                else
                {
                    var controller = other.gameObject.GetComponent<AStarAgentController>();
                    speed = controller.speed;
                    if (speed == 0)
                    {
                        isStopped = true;
                    }
                }
            }
        }



        private void OnTriggerStay(Collider other)
        {
            if (other.gameObject.CompareTag("TrafficLight"))
            {
                TrafficLightController controller = other.gameObject.GetComponent<TrafficLightController>();
                LightState status = controller.currentState;
                if (status == LightState.Green)
                {
                    isStopped = false;
                    speed = 15f;
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


            if (other.gameObject.CompareTag("Vehicle"))
            {
                speed = 15f;
                isStopped = false;
            }
        }

        #endregion


    }
}
