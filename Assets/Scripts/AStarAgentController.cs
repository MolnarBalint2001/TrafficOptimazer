using Assets.Entities;
using Assets.Services;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts
{
    public class AStarAgentController : MonoBehaviour
    {
        private enum VehicleType
        {
            CAR = 0,
            TRUCK = 1,
            MOTORCYClE = 2,
            BUS = 3,
        }

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
        private int currentWaypointIndex = 1;

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
        private float maxSpeed = 15f;

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


        private float detectionSensivity = 30f;

        private int obstacleLayer;

        private float detectionAngle = 45f;




        void Start()
        {
            obstacleLayer = LayerMask.NameToLayer("Obstacles");
            // Lekérdezzük a GameObject layerét és kiírjuk a konzolra
            int layer = gameObject.layer;
            Initialize();
        }


        void Update()
        {
            if (path is null)
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
                DetectObstacles();
                MoveToNextWaypoint();
            }

        }



        private void DetectObstacles()
        {

            if (path is null || path?.Count <= currentWaypointIndex) return;

            RaycastHit hit;
            Vector3 rayOrigin = transform.position;  // A sugár kiindulási pontja (autó pozíciója)
            Vector3 rayDirection = (path[currentWaypointIndex] - rayOrigin).normalized;

            Vector3 rayTarget = rayOrigin + rayDirection * detectionSensivity;


            bool detected = Physics.SphereCast(rayOrigin, 5f, rayTarget, out hit, detectionSensivity, obstacleLayer);


            if (detected)
            {

                // Ha van akadály a megadott távolságon belül, lassítunk
                // Pirossal rajzolja ki a Raycast-ot
                Debug.Log("Van előttem valami");
                // Lassítunk az autót a közelben lévő akadály miatt
                //currentSpeed = Mathf.Lerp(currentSpeed, originalSpeed * slowdownFactor, Time.deltaTime);
            }
            else
            {
                //Debug.Log("Nincs előttem semmi");
                // Ha nincs akadály, visszaállítjuk az eredeti sebességet
                //currentSpeed = Mathf.Lerp(currentSpeed, originalSpeed, Time.deltaTime);
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
            collider.isTrigger = true;
            TransformPath();
        }

        private void TransformPath()
        {

            if (path is null) return;
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

        private void GenerateNewPath()
        {
            Vector3 currentPosition = transform.position;
            System.Random random = new System.Random();
            int rri = random.Next(0, roads.Count);
            int rni = random.Next(0, roads[rri].ControlPoints.Count);
            goal = roads[rri].ControlPoints[rni];


            path = aStarSearch.FindPath(graph, start, goal, nodes);
            Debug.Log($"Megvan az uj path: {path.Count}");


            TransformPath();
            speed = 15f;
            currentWaypointIndex = 0;
        }



        private void MoveToNextWaypoint()
        {


            if (path is null || currentWaypointIndex >= path?.Count)
            {
                return;
            }



            if (Vector3.Distance(transform.position, goal.Position) == 0)
            {
                speed = 0f;
                GenerateNewPath();
                return;
            }

            Vector3 target = path[currentWaypointIndex];
            Vector3 direction = (target - transform.position).normalized;
            transform.position = Vector3.MoveTowards(transform.position, target, Time.deltaTime * speed);


            if (direction != Vector3.zero)
            {
                transform.forward = Vector3.Slerp(transform.forward, direction, Time.deltaTime * speed / 2);
            }


            Vector3 nextWaypoint = path[currentWaypointIndex + 1];
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


            speed = Mathf.SmoothDamp(speed, targetSpeed, ref velocity.z, 0.5f);


            if (Vector3.Distance(transform.position, target) == 0)
            {

                currentWaypointIndex++;
            }
        }



        // Trigger beállítása az akadály észleléséhez
        private void OnTriggerEnter(Collider other)
        {

            if (other.gameObject.layer == LayerMask.NameToLayer("Obstacles"))  // Tegyük fel, hogy az akadály "Obstacle" tag-gel rendelkezik
            {

                Debug.Log("Megközílette");
                var controller = other.gameObject.GetComponent<AStarAgentController>();
                speed = 0f;


            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.gameObject.layer == LayerMask.NameToLayer("Obstacles"))
            {

                speed = 15f;

                Debug.Log("Elengedte");

            }
        }

    }
}
