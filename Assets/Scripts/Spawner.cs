
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Assets.Entities;
using Unity.VisualScripting;
using UnityEngine;

namespace Assets.Scripts
{

    /// <summary>
    /// Entity spawner
    /// Spawn vehicles into the road network
    /// Handles daily spawning (morning, afternoon, evening)
    /// </summary>
    public class Spawner : MonoBehaviour
    {
        /// <summary>
        /// Number of entities that will spawned
        /// </summary>
        public int batchSize { get; set; } = 400;

        /// <summary>
        /// Number of destroyed entities
        /// </summary>
        public int destroyed = 0;

        /// <summary>
        /// Elapsed time from the start
        /// </summary>
        public float elapsedTime { get; set; } = 0;

        /// <summary>
        /// OSM graph
        /// </summary>
        public GraphOSM graph { get; set; }

        /// <summary>
        /// Random
        /// </summary>
        private System.Random random = new System.Random();

        /// <summary>
        /// Vehicles container
        /// </summary>
        public GameObject vehiclesContainer;

        /// <summary>
        /// All vehicles
        /// </summary>
        public List<GameObject> vehicles { get; set; } = new List<GameObject>();

        /// <summary>
        /// Spawn rate
        /// </summary>
        public float spawnRate { get; private set; }

        /*var start82 = graph.Nodes.Where(x => x.Id.Contains("5893594956_FORWARD"));
        var start83 = graph.Nodes.Where(x => x.Id.Contains("5893594960_FORWARD"));
        var start28 = graph.Nodes.Where(x => x.Id.Contains("247758788_BACKWARD"));


        var goal82 = graph.Nodes.Where(x => x.Id.Contains("247758788_FORWARD"));
        var goal83 = graph.Nodes.Where(x => x.Id.Contains("5893594960_BACKWARD"));
        var goal28 = graph.Nodes.Where(x => x.Id.Contains("247758788_FORWARD"));*/

        private List<string> startNodes = new List<string>()
        {
            "624135682_5893594956_FORWARD_0", //82
            "624135682_5893594956_FORWARD_1",
            "624135682_5893594956_FORWARD_2",

            "624135683_5893594960_FORWARD_0", //83
            "624135683_5893594960_FORWARD_1",


            "623918128_247758788_BACKWARD_0", //28
            "623918128_247758788_BACKWARD_1",

            /*"26153999_87531833_FORWARD_0", //99
            "26153999_87531833_FORWARD_1",*/
        };


        private List<string> goalNodes = new List<string>()
        {
            "623918128_247758788_FORWARD_0", //82
            "623918128_247758788_FORWARD_1",
            "623918128_247758788_FORWARD_1",


            "624135683_5893594960_BACKWARD_0", //83
            "624135683_5893594960_BACKWARD_1",

            "624135683_5893594960_BACKWARD_0", //28
            "624135683_5893594960_BACKWARD_1"

        };


        private List<string[]> paths = new List<string[]>()
        {
            new string[]{"624135682_5893594956_FORWARD_2", "623918128_247758788_FORWARD_0"},
            new string[]{"624135682_5893594956_FORWARD_1", "624135683_5893594960_BACKWARD_1"},
            new string[]{"624135682_5893594956_FORWARD_0", "624135683_5893594960_BACKWARD_0"},
            new string[]{"623918128_247758788_BACKWARD_1", "624135682_5893594956_BACKWARD_1"},
            new string[]{"623918128_247758788_BACKWARD_0", "624135682_5893594956_BACKWARD_0"},
            new string[]{"624135683_5893594960_FORWARD_0", "624135682_5893594956_BACKWARD_0"},
            new string[]{"624135683_5893594960_FORWARD_0", "623918128_247758788_FORWARD_0"},
            new string[]{"624135683_5893594960_FORWARD_1", "623918128_247758788_FORWARD_1" },
            new string[]{"624135683_5893594960_FORWARD_2", "623918128_247758788_FORWARD_1" },
            //new string[]{"26153999_87531833_FORWARD_0", "624135683_5893594960_BACKWARD_0" },
            //new string[]{"26153999_87531833_FORWARD_1", "623918128_247758788_FORWARD_0" }
            new string[]{ "100126749_1376422360_BACKWARD_0", "623917437_5872190568_BACKWARD_3" },
            new string[]{ "100126749_1376422360_BACKWARD_0", "623917437_5872190568_BACKWARD_2" },
            new string[]{ "100126749_1376422360_BACKWARD_0", "623917437_5872190568_BACKWARD_1" }
        };

        private const float cycleTime = 240f;


        private void Awake()
        {
            vehiclesContainer = new GameObject("VehiclesContainer");
        }


        private void Start()
        {
            StartCoroutine(SpawnRoutine());
        }


        private void Update()
        {

            elapsedTime += Time.deltaTime;
            if (elapsedTime >= cycleTime) {
                elapsedTime = 0f;
            }

            elapsedTime += Time.deltaTime;


            // Reggeli csúcs (0-1.5 perc)
            if (elapsedTime < 60f)
            {

                spawnRate = 0.25f; // Gyorsabb spawnolás
            }
            // Napközben (1.5-3 perc)
            else if (elapsedTime < 120f)
            {

                spawnRate = 1f;
            }
            // Esti csúcs (3-4.5 perc)
            else if (elapsedTime < 180f)
            {

                spawnRate = 0.75f;
            }
            // Éjszaka (4.5-5 perc)
            else
            {
               
                spawnRate = 15f; // Lassabb spawnolás
            }

            //Debug.Log(elapsedTime);
          
        
        }


        IEnumerator SpawnRoutine()
        {
            while (true)
            {
                Spawn();
                yield return new WaitForSeconds(spawnRate);
            }
        }





        /// <summary>
        /// Instentiate a vehicle into to the simulation
        /// </summary>
        private void Spawn()
        {


            /* var upperLimit = graph.Nodes.Count;
             var nodes = graph.Nodes.ToList();

             var random1 = random.Next(0, upperLimit - 1);
             var random2 = random.Next(0, upperLimit - 1);

             var start = nodes[random1];

             if (start.IsIntersectionNode)
             {
                 return;
             }

             var goal = nodes[random2];*/

            var rand = random.Next(0, paths.Count);
            var randPath = paths[rand];
            var start = graph.Nodes.FirstOrDefault(x => x.Id == randPath[0]);
            var goal = graph.Nodes.FirstOrDefault(x => x.Id == randPath[1]);


            GameObject vehicle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            vehicle.name = "Vehicle";
            vehicle.tag = "Vehicle";
            vehicle.layer = LayerMask.NameToLayer("VehicleLayer");
            var vehcont = vehicle.AddComponent<AStarAgentController>();
            var rigidBody = vehicle.AddComponent<Rigidbody>();

            rigidBody.isKinematic = true;
            vehcont.start = start;
            vehcont.goal = goal;
            vehcont.graphOSM = graph;
            vehcont.spawner = this;
            vehicle.GetComponent<Renderer>().material.color = Color.black;
            vehicle.transform.SetParent(vehiclesContainer.transform);

            vehicles.Add(vehicle);
        }

        /// <summary>
        /// Respawn
        /// </summary>
        public void Respawn()
        {
            elapsedTime = 0;
            destroyed = 0;
            spawnRate = 0.5f;

            foreach (var vehicle in vehicles)
            {
                Destroy(vehicle);
            }
            
        }




    }
}
