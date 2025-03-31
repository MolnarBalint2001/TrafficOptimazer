
using System.Collections.Generic;
using System.Linq;
using Assets.Entities;
using Assets.Scripts;
using UnityEditor;
using UnityEngine;
using System.Collections;
using Unity.MLAgents;
using Unity.VisualScripting;

namespace Assets.Services
{
    public class TestGraph : MonoBehaviour
    {
        public GraphOSM graph { get; set; }

        public Node start { get; set; }

        public Node start2 { get; set; }

        public Node goal { get; set; }

        public Node goal2 { get; set; }

        public List<Node> startNodes { get; set; } = new List<Node>();

        public List<Node> goalNodes { get; set; } = new List<Node>();

        public Spawner spawner;


        public GameObject trafficLightsContainer;

        private void Awake()
        {

            trafficLightsContainer = new GameObject("TrafficLightsContainer");

            var processor = new XmlProcessorService();
            var mapData = processor.LoadXMLDocument("vorosmarty_legnagyobb.osm");


            graph = GraphBuilder.BuildGraphV2(mapData.Roads);
            GlobalNetworkService.graph = graph;
            GlobalNetworkService.InitializeStore(graph);
           

            VisualizeTrafficLights(mapData.TrafficLights);
            VisualizeGraph(graph);
           

        }

        private void Start()
        {
            InitializeServices();
        }



        private void InitializeServices()
        {

            GameObject spawnerObject = new GameObject();
            spawnerObject.transform.SetParent(transform);
            spawner = spawnerObject.AddComponent<Spawner>();
            spawner.graph = graph;

            GameObject masterAgentObject = new GameObject("MasterAgent");
            MasterController masterAgent = masterAgentObject.AddComponent<MasterController>();
            masterAgent.spawner = spawner;

            var decisionRequester = masterAgent.AddComponent<DecisionRequester>();
            decisionRequester.DecisionPeriod = 50;
        }


        private void VisualizeTrafficLights(List<TrafficSignal> trafficLights)
        {
            foreach (var trafficSignal in trafficLights)
            {

                var lanes = graph.Nodes.Where(x => x.Id.Contains(trafficSignal.Id));

                switch (trafficSignal.Direction)
                {
                    case TrafficSignal.SignalDirection.FORWARD:
                        var forwardSignalLanes = lanes.Where(x => x.Id.Contains($"{trafficSignal.Id}_FORWARD"));
                        foreach (var lane in forwardSignalLanes)
                        {
                            GenerateTrafficLight(lane, trafficSignal.Id);

                        }
                        break;
                    case TrafficSignal.SignalDirection.BACKWARD:
                        var backwardSignalLanes = lanes.Where(x => x.Id.Contains($"{trafficSignal.Id}_BACKWARD"));
                        foreach (var lane in backwardSignalLanes)
                        {
                            GenerateTrafficLight(lane, trafficSignal.Id);

                        }
                        break;
                    case TrafficSignal.SignalDirection.BOTH:
                        var bothSignalLanes = lanes.Where(x => x.Id.Contains($"{trafficSignal.Id}"));
                        foreach (var lane in bothSignalLanes)
                        {

                            GenerateTrafficLight(lane, trafficSignal.Id);

                        }
                        break;
                }

                GlobalNetworkService.trafficLightsCount = trafficLightsContainer.transform.childCount;

                //Debug.DrawLine(trafficSignal.Position, new Vector3(trafficSignal.Position.x, 5, trafficSignal.Position.z), Color.green, float.PositiveInfinity);
            }

        }
        private void GenerateTrafficLight(Node laneNode, string Id)
        {


            GameObject trafficLightObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            trafficLightObject.name = "TrafficLight";
            trafficLightObject.tag = "TrafficLight";
            trafficLightObject.transform.localScale = new Vector3(0.2f, 3f, 0.2f);
            trafficLightObject.transform.position = new Vector3(laneNode.Position.x, 1.5f, laneNode.Position.z);
            trafficLightObject.transform.SetParent(trafficLightsContainer.transform);

            //tflObj.transform.LookAt(new Vector3(offsetPoint.x, 2.5f, offsetPoint.z));


            var tflcont = trafficLightObject.AddComponent<TrafficLightController>();

            var splittedNodeId = laneNode.Id.Split("_");
            tflcont.Id = $"TF-{splittedNodeId[0]}_{splittedNodeId[2]}_{splittedNodeId[3]}_{Id}";

            var sourceNodes = graph.Edges
                .Where(x=>x.Target == laneNode.Id)
                .Select(x=>x.Source)
                .Where(x =>
                {
                    var splitSourceId = x.Split("_");
                    var sourceRoadId = splitSourceId[0];
                    var laneDirection = splitSourceId[2];
                    var laneIndex = splitSourceId[3];

                    if (laneDirection == splittedNodeId[2] && laneIndex == splittedNodeId[3])
                    {
                        return true;
                    }

                    return false;
                });

        
            
              tflcont.previousNode = graph.Nodes.Where(x => sourceNodes.Contains(x.Id)).ToList();
            
           
         

            foreach (var sourceNodeId in sourceNodes)
            {

                var sourceNode = graph.Nodes.FirstOrDefault(x=>x.Id == sourceNodeId);
                Debug.DrawLine(sourceNode.Position, new Vector3(sourceNode.Position.x, 2, sourceNode.Position.z), Color.yellow, float.PositiveInfinity);
            
            
            }


            var collider = trafficLightObject.GetComponent<BoxCollider>();
            collider.size = new Vector3(1.1f, 1, 1.1f);
            collider.center = new Vector3(0, 0, 0);
            collider.isTrigger = true;

        }
        private void VisualizeGraph(GraphOSM graph)
        {

            var nodes = graph.Nodes;
            var edges = graph.Edges;




            foreach (var node in nodes)
            {
                var splittedNodeId = node.Id.Split("_");
                var isBackward = splittedNodeId[2] == "BACKWARD";
                var nodeEdges = edges.Where(x => x.Source == node.Id).Select(x => x.Target).ToList();
                var targets = nodes.Where(x => nodeEdges.Contains(x.Id)).ToList();


                //Debug.DrawLine(node.Position, new Vector3(node.Position.x, 2, node.Position.z), isBackward ? Color.magenta : Color.red, float.PositiveInfinity);

                //Debug.DrawLine(node.OriginalPosition, new Vector3(node.OriginalPosition.x, 2, node.OriginalPosition.z), Color.yellow, float.PositiveInfinity);



                foreach (var target in targets)
                {




                    //Debug.DrawLine(node.Position, target.Position, Color.yellow, float.PositiveInfinity);
                    DrawArrow.ForDebug(node.Position, target.Position, Color.white);
                   


                }



            }


        }
    }
}
