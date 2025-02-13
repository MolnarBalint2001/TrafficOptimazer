using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Assets.Entities;
using Assets.Services;
using Interfaces;
using Unity.VisualScripting;
using UnityEngine;


namespace Assets.Scripts
{
    public class TrainingCenter : MonoBehaviour
    {


        public string osm = "manhattan_training1.osm";

        private IXmlProcessorService xmlProcService = new XmlProcessorService();

        private MapData mapData;

        private Dictionary<string, List<Node>> graph;

        private float roadWidth = 2.5f;

        private Dictionary<string, Node> nodeCollection;



        private void Start()
        {

            mapData = xmlProcService.LoadXMLDocument(osm);
            graph = GraphBuilder.BuildGraph(mapData.Roads);
            nodeCollection = GraphBuilder.BuildNodeCollection(mapData.Roads);
            GenerateTrafficLights(mapData.TrafficLights);
        }



        private void Update()
        {
            BatchSpawn();
        }



        private void OnDrawGizmos()
        {

            if (mapData == null) return;
            GenerateRoads();
            GenerateCollisonZones();


        }

        private void GenerateRoads()
        {
            foreach (Road road in mapData.Roads) {

                for (int i = 0; i < road.ControlPoints.Count - 1; i++)
                {
                    var currentNode = road.ControlPoints[i];
                    var nextNode = road.ControlPoints[i + 1];

                    Gizmos.DrawLine(currentNode.Position, nextNode.Position);
                    
                }
            
            
            }
           
        }

        private void GenerateCollisonZones()
        {
            
            foreach (TrafficSignal trafficLight in mapData.TrafficLights)
            {
               
                Gizmos.DrawWireSphere(trafficLight.Position, 10);
                Gizmos.color = UnityEngine.Color.magenta;
            }
        }
        private void GenerateTrafficLights(List<TrafficSignal> trafficLights)
        {

            GameObject trafficLightsContainer = new GameObject("TrafficLightsContainer");
            foreach (TrafficSignal trafficLight in trafficLights)
            {
                var children = graph[trafficLight.Id];

                for (int i = 0; i < children.Count; i++) {
                    var child = children[i];
                    var direction = (child.Position - trafficLight.Position).normalized;
                    var offsetPoint = trafficLight.Position + direction * 5f;
                    var normal = Vector3.Cross(direction, Vector3.up).normalized;
                    var left = offsetPoint + normal * roadWidth;


                    //Debug.DrawLine(offsetPoint, trafficLight.Position, Color.magenta, float.PositiveInfinity);
                    //Debug.DrawLine(left, offsetPoint, Color.magenta, float.PositiveInfinity);

                    
                    GameObject tflObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    tflObj.name = "TrafficLight";
                    tflObj.tag = "TrafficLight";
                    tflObj.transform.localScale = new Vector3(0.2f, 5, 0.2f);
                    tflObj.transform.position = new Vector3(left.x, 2.5f, left.z);
                    tflObj.transform.SetParent(trafficLightsContainer.transform);
                    var directionToOffset = (left - offsetPoint).normalized;
                    tflObj.transform.LookAt(new Vector3(offsetPoint.x, 2.5f, offsetPoint.z));


                    //tflObj.layer = LayerMask.NameToLayer("TrafficLight");
                    var tflcont = tflObj.AddComponent<TrafficLightController>();
                    tflcont.previousNode = child;


                    GameObject waitZone = new GameObject("WaitZone");
                    waitZone.transform.position = new Vector3(tflObj.transform.position.x, 1, tflObj.transform.position.z);
                    waitZone.transform.SetParent(tflObj.transform);
                    //waitZone.transform.LookAt(new Vector3(offsetPoint.x, 1, offsetPoint.z));
                    waitZone.transform.LookAt(new Vector3(offsetPoint.x, 1, offsetPoint.z));
                    var waitCollider = waitZone.AddComponent<BoxCollider>();
                    waitCollider.isTrigger = true;
                    waitCollider.size = new Vector3(50, 2.5f, 2.5f);
                    waitCollider.center = new Vector3(-waitCollider.size.x / 2, 0, 1.25f);
                    var waitController = waitZone.AddComponent<WaitZoneController>();
                    waitController.trafficLightController = tflcont;


                    var collider = tflObj.GetComponent<BoxCollider>();
                    collider.size = new Vector3(4, 1, 12);
                    collider.center = new Vector3(0, 0, collider.size.z / 2 + 0.5f);
                    collider.isTrigger = true;

                }
                
            }


          
        }

        void BatchSpawn()
        {


            bool isPressed = Input.GetKeyDown(KeyCode.Space);
            if (isPressed)
            {
                GameObject vehiclesContainer = new GameObject("VehiclesContainer");
                System.Random random = new System.Random();

                for (int i = 0; i < 200; i++)
                {
                    int rnd1 = random.Next(0, mapData.Roads.Count);
                    var road = mapData.Roads[rnd1];
                    int rnd2 = random.Next(0, road.ControlPoints.Count);
                    var start = road.ControlPoints[rnd2];


                    start = mapData.Roads[0].ControlPoints[1];
                   
                    SpawnVehicle(start, UnityEngine.Color.black, vehiclesContainer);
                    
                  

                }


            }
        }





        void SpawnVehicle(Node start, UnityEngine.Color color, GameObject parent)
        {

            GameObject vehicle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            vehicle.name = "Vehicle";
            vehicle.tag = "Vehicle";
            vehicle.layer = LayerMask.NameToLayer("Obstacles");
            var vehcont = vehicle.AddComponent<AStarAgentController>();
            var rigidBody = vehicle.AddComponent<Rigidbody>();

            rigidBody.isKinematic = true;
            vehcont.gameObject.layer = LayerMask.NameToLayer("Obstacles");
            vehcont.roads = mapData.Roads;
            vehcont.nodes = nodeCollection;
            vehcont.graph = graph;
            vehcont.start = start;
            vehicle.GetComponent<Renderer>().material.color = color;
            vehicle.transform.SetParent(parent.transform);

        }


        private bool IsInCollisionZone(Vector3 point)
        {
            var center = new Vector3(0,0,0);
            var radius = 10f;


            // Távolság négyzete a pont és a középpont között
            float distanceSquared = (point - center).sqrMagnitude;

            // A sugár négyzete
            float radiusSquared = radius * radius;

            // Ha a távolság négyzete kisebb vagy egyenlő, mint a sugár négyzete, a pont a körben van
            return distanceSquared <= radiusSquared;
        }
        






    }
}
