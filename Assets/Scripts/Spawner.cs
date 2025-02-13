using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.Entities;
using Unity.Mathematics;
using Unity.MLAgents;
using Unity.VisualScripting;
using UnityEngine;

namespace Assets.Scripts
{
    public class Spawner : MonoBehaviour
    {

        public int batchSize { get; set; } = 200;

        public int destroyed = 0;

        public List<Road> roads { get; set; }

        public Dictionary<string, Node> nodes { get; set; }

        public Dictionary<string, List<Node>> graph { get; set; }

        private const int treshold = 10;

        private System.Random random = new System.Random();


        public GameObject vehiclesContainer;

        private void Start()
        {
            vehiclesContainer = new GameObject("VehiclesContainer");
        }


        private void Update()
        {
            BatchSpawn();
            SupplementarySpawn();
        }


        private void BatchSpawn()
        {
            bool isPressed = Input.GetKeyDown(KeyCode.Space);
            if (isPressed)
            {
                for (int i = 0; i < batchSize; i++)
                {
                    Spawn();
                }
            }
        }



        private void Spawn()
        {

            int rnd1 = random.Next(0, roads.Count);
            var road = roads[rnd1];
            int rnd2 = random.Next(0, road.ControlPoints.Count);
            var start = road.ControlPoints[rnd2];


            GameObject vehicle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            vehicle.name = "Vehicle";
            vehicle.tag = "Vehicle";
            vehicle.layer = LayerMask.NameToLayer("Obstacles");
            var vehcont = vehicle.AddComponent<AStarAgentController>();
            var rigidBody = vehicle.AddComponent<Rigidbody>();

            rigidBody.isKinematic = true;
            vehcont.gameObject.layer = LayerMask.NameToLayer("Obstacles");
            vehcont.roads = roads;
            vehcont.nodes = nodes;
            vehcont.graph = graph;
            vehcont.start = start;
            vehcont.spawner = this;
            vehicle.GetComponent<Renderer>().material.color = Color.black;
            vehicle.transform.SetParent(vehiclesContainer.transform);

            GameObject front = new GameObject("Front");
            front.transform.SetParent(vehicle.transform);
            var frontCollider = vehicle.AddComponent<BoxCollider>();
            frontCollider.tag = "Front";
            frontCollider.isTrigger = true;
            
            GameObject back = new GameObject("Back");
            back.transform.SetParent(vehicle.transform);
            var backCollider = vehicle.AddComponent<BoxCollider>();
            backCollider.tag = "Back";
            backCollider.isTrigger = true;

            GameObject body = new GameObject("Body");
            body.transform.SetParent(vehicle.transform);
            var bodyCollider = vehicle.AddComponent<BoxCollider>();
            bodyCollider.tag = "Body";
            bodyCollider.isTrigger = true;
        }

        private void SupplementarySpawn()
        {
            if (treshold > destroyed) return;
            for (int i = 0; i < destroyed; i++)
            {
                Spawn();
            }

            destroyed = 0;
        }


        public void SetDestroyed()
        {
            destroyed++;
        }


        public void Respawn()
        {
            
            destroyed = 0;
            var vehicles = FindObjectsByType<AStarAgentController>(FindObjectsSortMode.None).ToList();
            foreach (var vehicle in vehicles)
            {
                var controller = vehicle.GetComponent<AStarAgentController>();
                controller.DestroySelf();
            }
            BatchSpawn();
        }




    }
}
