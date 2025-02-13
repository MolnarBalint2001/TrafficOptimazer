
using Assets.Entities;
using Assets.Scripts;
using Assets.Services;
using Interfaces;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Splines;
using System;
using UnityEditor.UIElements;
using Unity.VisualScripting;
using Unity.MLAgents.Policies;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;


/*[out:xml]
[bbox:{{bbox}}];

(
  way["highway"]["highway" != "path"]["highway" != "steps"]["highway" != "ciclyeway"]["highway" != "footway"]["highway"]({{bbox}});
  node["highway" = "traffic_signals"]["traffic_signals" = "signal"]({{bbox}});

);

out body;
>;
out skel qt;*/


/// <summary>
/// Container initalize
/// Load XML document and rendering a mesh
/// </summary>
public class ContainerInitialize : MonoBehaviour
{
    /// <summary>
    /// XML processor service
    /// </summary>
    private IXmlProcessorService xmlProcessorService = new XmlProcessorService();

    /// <summary>
    /// Spawner service
    /// responsible for spawning vehicles and manage reinforcement
    /// </summary>
    private Spawner spawner;

    /// <summary>
    /// Graph
    /// </summary>
    private Dictionary<string, List<Node>> graph;

    /// <summary>
    /// Node collection
    /// </summary>
    private Dictionary<string, Node> nodeCollection;

    /// <summary>
    /// Roads
    /// </summary>
    private List<Road> roads;

    /// <summary>
    /// TrafficLights
    /// </summary>
    private List<TrafficSignal> trafficLights;

    public SplineContainer splineContainer;

    const float roadWidth = 2.5f;

    public Material roadMaterial;


    void Awake()
    {
        // Betöltjük a Material-t a Resources-ből
        roadMaterial = Resources.Load<Material>("Materials/RoadMaterial");
        if (roadMaterial == null)
        {
            Debug.LogError("Nem található a Material a megadott úton!");
        }
    }
    void Start()
    {

        GameObject tf = GameObject.CreatePrimitive(PrimitiveType.Cube);
        tf.AddComponent<TrafficLightController>();
        tf.transform.position = new Vector3(0, 2, 0);
        //GenerateNetwork();
        //InitializeServices();
    }
    void Update()
    {
      
    }
    private void OnDrawGizmos()
    {
        GenerateCollisonZones();
    }
    private void InitializeServices()
    {
        if (spawner == null)
        {
            GameObject spawnerObject = new GameObject("Spawner");
            spawnerObject.transform.SetParent(transform);
            spawner = spawnerObject.AddComponent<Spawner>();
            spawner.nodes = nodeCollection;
            spawner.roads = roads;
            spawner.graph = graph;
        }


        GameObject masterAgent = new GameObject("MasterAgent");
        var masterController = masterAgent.AddComponent<MasterController>();
        masterController.spawner = spawner;

        var behaviorParams = masterAgent.AddComponent<BehaviorParameters>();
        behaviorParams.BrainParameters.ActionSpec = ActionSpec.MakeContinuous(trafficLights.Count * 2);
        behaviorParams.BehaviorType = BehaviorType.Default;
        

        var decisionRequester = masterAgent.AddComponent<DecisionRequester>();
        decisionRequester.DecisionPeriod = 5;


    }
    private void GenerateNetwork()
    {
        MapData mapData = xmlProcessorService.LoadXMLDocument("manhattan_training1.osm");
        GenerateRoads(mapData.Roads);
        GenerateTrafficLights(mapData.TrafficLights);
    }
    private void GenerateRoads(List<Road> r)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        roads = r;
        graph = GraphBuilder.BuildGraph(roads);
        nodeCollection = GraphBuilder.BuildNodeCollection(roads);
        //GraphBuilder.PrintGraph(graph);



        foreach (var road in roads)
        {

            for (int i = 0; i < road.ControlPoints.Count - 1; i++)
            {

                var currentNode = road.ControlPoints[i];
                var nextNode = road.ControlPoints[i + 1];

               
               
               
                //Debug.DrawLine(currentNode.Position, nextNode.Position, Color.green, float.PositiveInfinity);

                if (currentNode.IsIntersectionNode)
                {
                    var children = graph[currentNode.Id];
                    var reference = children[0];
                    Vector3 refDir = reference.Position - currentNode.Position;

                    children = children.OrderBy(x =>
                    {
                        Vector3 dir = x.Position - currentNode.Position;
                        float angle = Mathf.Atan2(Vector3.Cross(refDir, dir).y, Vector3.Dot(refDir, dir));
                        return angle;
                    }).ToList();



                    for (int j = 0; j < children.Count; j++)
                    {
                        var childCurrent = children[j];
                        var childNext = children[(j + 1) % children.Count];

                        var dir = (childCurrent.Position - currentNode.Position).normalized;
                        var transformed = currentNode.Position + dir * roadWidth * 2; // kicsit eltolt a közzéponttól
                        //Debug.DrawLine(currentNode.Position, transformed, Color.red, float.PositiveInfinity);


                        //Connect childs
                        var childNormal = Vector3.Cross(dir, Vector3.up).normalized;
                        var childRight = childCurrent.Position + childNormal * roadWidth;
                        var childLeft = childCurrent.Position - childNormal * roadWidth;


                        //Eltransformált gyerek 
                        var transformedForward = (currentNode.Position - transformed).normalized;
                        var transformedNormal = Vector3.Cross(transformedForward, Vector3.up).normalized;
                        var transformedRight = transformed + transformedNormal * roadWidth;
                        var transformedLeft = transformed - transformedNormal * roadWidth;

                        //Eltransformált következő gyerek a két út között háromszögeléshez
                        var cnDir = (childNext.Position - currentNode.Position).normalized;
                        var transformedNext = currentNode.Position + cnDir * roadWidth * 2;
                        var transNextForward = (currentNode.Position - transformedNext).normalized;
                        var transNextNormal = Vector3.Cross(transNextForward, Vector3.up).normalized;
                        var transNextRight = transformedNext + transNextNormal * roadWidth;
                        var transNextLeft = transformedNext - transNextNormal * roadWidth;



                        //var arcVertices = GenerateArc(currentNode.Position, transformedRight, transNextLeft);


                        //Debug.DrawLine(arcVertices[0], new Vector3(arcVertices[0].x, 1, arcVertices[0].z), Color.cyan, float.PositiveInfinity);

                        vertices.Add(transformedRight); //0
                        vertices.Add(transformedLeft); //1
                        vertices.Add(currentNode.Position); //2
                        vertices.Add(transNextLeft); //3


                        //Child connection
                        vertices.Add(childLeft); //4
                        vertices.Add(childRight); //5

                        int vIndex1 = vertices.Count - 6;


                        triangles.Add(vIndex1);
                        triangles.Add(vIndex1 + 2);
                        triangles.Add(vIndex1 + 1);

                        triangles.Add(vIndex1);
                        triangles.Add(vIndex1 + 3);
                        triangles.Add(vIndex1 + 2);



                        triangles.Add(vIndex1 + 4);
                        triangles.Add(vIndex1);
                        triangles.Add(vIndex1 + 5);

                        triangles.Add(vIndex1 + 5);
                        triangles.Add(vIndex1);
                        triangles.Add(vIndex1 + 1);

                    }
                }

                if (!nextNode.IsIntersectionNode)
                {
                    var thirdNode = nextNode;
                    if (i < road.ControlPoints.Count - 2)
                    {
                        thirdNode = road.ControlPoints[i + 2];
                    }

                    var forward = (nextNode.Position - currentNode.Position);
                    var normal = Vector3.Cross(forward, Vector3.up).normalized;
                    var right = currentNode.Position - normal * roadWidth;
                    var left = currentNode.Position + normal * roadWidth;


                    var backward = (currentNode.Position - nextNode.Position);
                    var backNormal = Vector3.Cross(backward, Vector3.up).normalized;
                    var endLeft = nextNode.Position - backNormal * roadWidth;
                    var endRight = nextNode.Position + backNormal * roadWidth;


                    var middleForward = (thirdNode.Position - nextNode.Position);
                    var middleNormal = Vector3.Cross(middleForward, Vector3.up).normalized;
                    var middleLeft = nextNode.Position - middleNormal * roadWidth;
                    var middleRight = nextNode.Position + middleNormal * roadWidth;

                    var middleBackward = (nextNode.Position - thirdNode.Position);
                    var middleBackwardNormal = Vector3.Cross(middleBackward, Vector3.up).normalized;
                    var middleBackLeft = thirdNode.Position - middleBackwardNormal * roadWidth;
                    var middleBackRight = thirdNode.Position + middleBackwardNormal * roadWidth;

                    vertices.Add(left); //0
                    vertices.Add(right); //1
                    vertices.Add(endLeft); //2
                    vertices.Add(endRight); //3

                    vertices.Add(middleLeft); //4
                    vertices.Add(middleRight); //5
                    vertices.Add(middleBackLeft); //6
                    vertices.Add(middleBackRight); //7

                    vertices.Add(nextNode.Position); //8

                    int vIndex = vertices.Count - 9;

                    //First
                    triangles.Add(vIndex);
                    triangles.Add(vIndex + 2);
                    triangles.Add(vIndex + 1);


                    triangles.Add(vIndex + 1);
                    triangles.Add(vIndex + 2);
                    triangles.Add(vIndex + 3);

                    triangles.Add(vIndex + 2);
                    triangles.Add(vIndex + 5);
                    triangles.Add(vIndex + 8);


                    triangles.Add(vIndex + 8);
                    triangles.Add(vIndex + 4);
                    triangles.Add(vIndex + 3);


                }


                //Debug.DrawLine(currentNode.Position, nextNode.Position, Color.cyan, float.PositiveInfinity);

            }
        }

        Mesh mesh = new Mesh();

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();

        Vector2[] uvs = new Vector2[mesh.vertices.Length];

        for (int i = 0; i < mesh.vertices.Length; i++)
        {
            uvs[i] = new Vector2(vertices[i].x, vertices[i].z);
        }


        mesh.uv = uvs;

        MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
        meshFilter.mesh = mesh;

        MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();

        meshRenderer.transform.position = new Vector3(0, 0, 0);

        meshRenderer.materials = new Material[] { new Material(Shader.Find("Standard")) { color = Color.white } };


        mesh.RecalculateBounds();
    }
    private void GenerateTrafficLights(List<TrafficSignal> tflList)
    {

        trafficLights = tflList;
        GameObject trafficLightsContainer = new GameObject("TrafficLightContainer");

        foreach (var trafficLight in trafficLights)
        {
            var children = graph[trafficLight.Id];
            if (children.Count <= 2)
            {
                continue;
            }

            for (int i = 0; i<children.Count; i++)
            {
                var child = children[i];
                var direction = (child.Position - trafficLight.Position).normalized;
                var offsetPoint = trafficLight.Position + direction * 5f;
                var normal = Vector3.Cross(direction, Vector3.up).normalized;
                var left = offsetPoint + normal * roadWidth;

                Debug.DrawLine(trafficLight.Position, offsetPoint, Color.magenta, float.PositiveInfinity);
                Debug.DrawLine(offsetPoint, left, Color.magenta, float.PositiveInfinity);

                Vector3[] scopeBounds = new Vector3[4];

                var childChildren = graph[child.Id];
                Debug.DrawLine(child.Position, childChildren[0].Position, Color.magenta, float.PositiveInfinity);
                var d = (child.Position - trafficLight.Position);
                Debug.DrawLine(left, child.Position, Color.red, float.PositiveInfinity);

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
    private void GenerateCollisonZones()
    {
        if (trafficLights == null) return;

        foreach (TrafficSignal trafficLight in trafficLights)
        {
            var children = graph[trafficLight.Id];
            if (children.Count < 3) continue;
          
            Gizmos.DrawWireSphere(trafficLight.Position, 10);
            Gizmos.color = UnityEngine.Color.magenta;
        }
    }


}



