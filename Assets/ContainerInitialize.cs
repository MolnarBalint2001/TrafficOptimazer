
using Assets.Entities;
using Assets.Scripts;
using Assets.Services;
using Interfaces;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Splines;


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

    private Dictionary<string, List<Node>> graph;

    private Dictionary<string, Node> nodeCollection;

    private List<Road> roads;

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



        BuildWithoutIntersections();
        //SplineTest();
        //DefaultTestMesh();
        //GenerateTrafficLights();

    }


    void Update()
    {
        BatchSpawn();
        //SpawnAStarAgent();
    }

    void BatchSpawn()
    {
        bool isPressed = Input.GetKeyDown(KeyCode.Space);
        if (isPressed)
        {
            System.Random random = new System.Random();


            for (int i = 0; i < 300; i++)
            {
                int rnd1 = random.Next(0, roads.Count);
                var road = roads[rnd1];
                int rnd2 = random.Next(0, road.ControlPoints.Count);
                var start = road.ControlPoints[rnd2];
                SpawnVehicle(start, Color.black);

            }


        }
    }


    void SpawnVehicle(Node start, Color color)
    {

        GameObject vehicle = GameObject.CreatePrimitive(PrimitiveType.Cube);
        vehicle.name = "Vehicle";
        vehicle.tag = "Vehicle";
        vehicle.layer = LayerMask.NameToLayer("Obstacles");
        var vehcont = vehicle.AddComponent<AStarAgentController>();
        var rigidBody = vehicle.AddComponent<Rigidbody>();
        rigidBody.isKinematic = true;
        vehcont.gameObject.layer = LayerMask.NameToLayer("Obstacles");
        vehcont.roads = roads;
        vehcont.nodes = nodeCollection;
        vehcont.graph = graph;
        vehcont.start = start;
        vehicle.GetComponent<Renderer>().material.color = color;


    }

    void BuildWithoutIntersections()
    {
        Debug.Log("Angle test: " + Vector3.Angle(new Vector3(0, 0, 1), new Vector3(1, 0, 1)));
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        MapData mapData = xmlProcessorService.LoadXMLDocument("manhattan.osm");

        roads = mapData.Roads;
        graph = GraphBuilder.BuildGraph(mapData.Roads);
        nodeCollection = GraphBuilder.BuildNodeCollection(mapData.Roads);
        //GraphBuilder.PrintGraph(graph);



        foreach (var road in mapData.Roads)
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


        GenerateTrafficLights(mapData.TrafficLights);

        Mesh mesh = new Mesh();

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();




        MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
        meshFilter.mesh = mesh;

        MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();

        meshRenderer.transform.position = new Vector3(0, 0, 0);

        meshRenderer.materials = new Material[] { new Material(Shader.Find("Standard")) { color = Color.white } };


        mesh.RecalculateBounds();
    }



    void GenerateTrafficLights(List<TrafficSignal> trafficLights)
    {
        foreach (var trafficLight in trafficLights)
        {

            GameObject tflObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tflObj.name = "TrafficLight";
            tflObj.tag = "TrafficLight";
            tflObj.layer = LayerMask.NameToLayer("TrafficLights");
            var tflcont = tflObj.AddComponent<TrafficLightController>();
            tflcont.position = trafficLight.Position;
            Debug.DrawLine(tflcont.position, new Vector3(tflcont.position.x, tflcont.position.y + 20, tflcont.position.z), Color.magenta, float.PositiveInfinity);

        }
    }


}



