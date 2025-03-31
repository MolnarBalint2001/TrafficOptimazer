using Assets.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using TreeEditor;
using Unity.VisualScripting;
using UnityEngine;
using Edge = Assets.Entities.Edge;

namespace Assets.Services
{

    /// <summary>
    /// Road network graph builder
    /// </summary>
    public class GraphBuilder
    {


        public static GraphOSM BuildGraphV2(List<Road> network)
        {



            var graph = new GraphOSM();

            // Create lanes nodes
            foreach (var road in network)
            {
                for (int j = 0; j < road.ControlPoints.Count; j++)
                {

                    var node = road.ControlPoints[j];
                    float intersectionPaddingMultiplier = 1f;
                    Vector3 nodePosition = node.Position;

                    Node nextNode = null;
                    if (j != road.ControlPoints.Count - 1)
                    {
                        nextNode = road.ControlPoints[j + 1];
                    }
                    else
                    {
                        nextNode = road.ControlPoints[j - 1];
                    }




                    if (node.IsIntersectionNode)
                    {
                        var distance = Vector3.Distance(nextNode.Position, node.Position);
                        intersectionPaddingMultiplier = node.IntersectCount == 2 ? 5f : Math.Min(intersectionPaddingMultiplier * distance * 0.6f, 30f); // 30 volt

                    }

                    var dir = (nextNode.Position - node.Position).normalized;


                    if (node.IsIntersectionNode)
                    {
                        nodePosition = nodePosition + dir * intersectionPaddingMultiplier;
                    }
                    var norm = Vector3.Cross(dir, Vector3.up).normalized * (j == road.ControlPoints.Count - 1 ? 1 : -1);



                    for (var i = 0; i < road.ForwardLanes; i++)
                    {
                        var laneNode = new Node()
                        {
                            Id = $"{road.Id}_{node.Id}_FORWARD_{i}",
                            Position = nodePosition + norm * (i + 1) * 3.5f,
                            PaddedLanePosition = node.Position + norm * (i + 1) * 3.5f,
                            IsIntersectionNode = node.IsIntersectionNode,
                            OriginalPosition = node.OriginalPosition,
                            RoadId = node.RoadId,

                        };
                        graph.Nodes.Add(laneNode);
                    }
                    for (var i = 0; i < road.BackwardLanes; i++)
                    {
                        var laneNode = new Node()
                        {
                            Id = $"{road.Id}_{node.Id}_BACKWARD_{i}",
                            Position = nodePosition - norm * (i + 1) * 3.5f,
                            PaddedLanePosition = node.Position - norm * (i + 1) * 3.5f,
                            IsIntersectionNode = node.IsIntersectionNode,
                            OriginalPosition = node.OriginalPosition,
                            RoadId = node.RoadId,
                        };
                        graph.Nodes.Add(laneNode);

                    }
                }
            }

            // Connect nodes

            foreach (var road in network)
            {
                //Forward lanes
                for (var i = 0; i < road.ControlPoints.Count - 1; i++)
                {
                    var currentNode = road.ControlPoints[i];
                    var nextNode = road.ControlPoints[i + 1];

                    var weight = Vector3.Distance(currentNode.PaddedLanePosition, nextNode.PaddedLanePosition);

                    for (var j = 0; j < road.ForwardLanes; j++)
                    {
                        var currentLaneNodeId = $"{road.Id}_{currentNode.Id}_FORWARD_{j}";

                        if (road.ForwardLanes > 2)
                        {

                            if (nextNode.IsIntersectionNode)
                            {
                                graph.Edges.Add(new Edge() { Source = currentLaneNodeId, Target = $"{road.Id}_{nextNode.Id}_FORWARD_{j}" });
                                continue;
                            }
                            if (j == 0)
                            {
                                graph.Edges.Add(new Edge() { Source = currentLaneNodeId, Target = $"{road.Id}_{nextNode.Id}_FORWARD_0" });
                                graph.Edges.Add(new Edge() { Source = currentLaneNodeId, Target = $"{road.Id}_{nextNode.Id}_FORWARD_1" });
                            }
                            else if (j != 0 && j != road.ForwardLanes - 1)
                            {
                                graph.Edges.Add(new Edge() { Source = currentLaneNodeId, Target = $"{road.Id}_{nextNode.Id}_FORWARD_{j}" });
                                graph.Edges.Add(new Edge() { Source = currentLaneNodeId, Target = $"{road.Id}_{nextNode.Id}_FORWARD_{j + 1}" });
                                graph.Edges.Add(new Edge() { Source = currentLaneNodeId, Target = $"{road.Id}_{nextNode.Id}_FORWARD_{j - 1}" });
                            }
                            else
                            {
                                graph.Edges.Add(new Edge() { Source = currentLaneNodeId, Target = $"{road.Id}_{nextNode.Id}_FORWARD_{road.ForwardLanes - 1}" });
                                graph.Edges.Add(new Edge() { Source = currentLaneNodeId, Target = $"{road.Id}_{nextNode.Id}_FORWARD_{road.ForwardLanes - 2}" });
                            }
                        }
                        else
                        {
                            //Every node
                            for (var k = 0; k < road.ForwardLanes; k++)
                            {
                                var nextLaneNodeId = $"{road.Id}_{nextNode.Id}_FORWARD_{k}";

                                if (!Bidirectional(graph, currentLaneNodeId, nextLaneNodeId))
                                {
                                    graph.Edges.Add(new Edge() { Source = currentLaneNodeId, Target = nextLaneNodeId, Weight = weight });
                                }
                            }
                        }
                       
                           
                        
                      

                    }

                }
                //BackWard lanes
                for (var i = road.ControlPoints.Count - 1; i >= 1; i--)
                {
                    var currentNode = road.ControlPoints[i];
                    var nextNode = road.ControlPoints[i - 1];

                    var weight = Vector3.Distance(currentNode.PaddedLanePosition, nextNode.PaddedLanePosition);

                    for (var j = 0; j < road.BackwardLanes; j++)
                    {
                        var currentLaneNodeId = $"{road.Id}_{currentNode.Id}_BACKWARD_{j}";


                        if (nextNode.IsIntersectionNode)
                        {
                            graph.Edges.Add(new Edge() { Source = currentLaneNodeId, Target = $"{road.Id}_{nextNode.Id}_BACKWARD_{j}" });
                            continue;
                        }

                        if (road.BackwardLanes > 2)
                        {
                            if (j == 0)
                            {
                                graph.Edges.Add(new Edge() { Source = currentLaneNodeId, Target = $"{road.Id}_{nextNode.Id}_BACKWARD_0" });
                                graph.Edges.Add(new Edge() { Source = currentLaneNodeId, Target = $"{road.Id}_{nextNode.Id}_BACKWARD_1" });
                            }
                            else if (j != 0 && j != road.BackwardLanes - 1)
                            {
                                graph.Edges.Add(new Edge() { Source = currentLaneNodeId, Target = $"{road.Id}_{nextNode.Id}_BACKWARD_{j}" });
                                graph.Edges.Add(new Edge() { Source = currentLaneNodeId, Target = $"{road.Id}_{nextNode.Id}_BACKWARD_{j + 1}" });
                                graph.Edges.Add(new Edge() { Source = currentLaneNodeId, Target = $"{road.Id}_{nextNode.Id}_BACKWARD_{j - 1}" });
                            }
                            else
                            {
                                graph.Edges.Add(new Edge() { Source = currentLaneNodeId, Target = $"{road.Id}_{nextNode.Id}_BACKWARD_{road.BackwardLanes - 1}" });
                                graph.Edges.Add(new Edge() { Source = currentLaneNodeId, Target = $"{road.Id}_{nextNode.Id}_BACKWARD_{road.BackwardLanes - 2}" });
                            }
                        }
                        else
                        {
                            //Every node
                            for (var k = 0; k < road.BackwardLanes; k++)
                            {
                                var nextLaneNodeId = $"{road.Id}_{nextNode.Id}_BACKWARD_{k}";
                                if (!Bidirectional(graph, currentLaneNodeId, nextLaneNodeId))
                                {
                                    graph.Edges.Add(new Edge() { Source = currentLaneNodeId, Target = nextLaneNodeId, Weight = weight });
                                }


                            }
                        }

                    }


                }



            }


            List<Node> intersectionNodes = new List<Node>();
            foreach (Road road in network)
            {
                intersectionNodes.AddRange(road.ControlPoints.Where(x => x.IsIntersectionNode));

            }
            intersectionNodes = intersectionNodes.DistinctBy(x => x.Id).ToList();

            foreach (var node in intersectionNodes)
            {
                var roads = network.Where(x => x.ControlPoints.Any(n => n.Id == node.Id)).ToList();

                foreach (var road in roads)
                {

                    //Forward lanes connection
                    for (var i = 0; i < road.ForwardLanes; i++)
                    {
                        var laneNodeId = $"{road.Id}_{node.Id}_FORWARD_{i}";
                        var laneNode = graph.Nodes.FirstOrDefault(x => x.Id == laneNodeId);
                        var option = road.TurnLanesForward?.Split("|")[i] ?? "default";



                        if (option == "default")
                        {

                            int index = road.ControlPoints.FindIndex(x => x.Id == node.Id);
                            if (index == 0)
                            {
                                option = "left;through";
                            }

                            else if (index == road.ControlPoints.Count - 1)
                            {
                                option = "right;left;through";
                            }


                        }

                        foreach (var opt in option.Split(";"))
                        {

                            
                            if (opt == "right")
                            {
                                var neighborRoad = GetRightSideRoad(node, road, roads);//;
                                if (neighborRoad == null) continue;

                                string dir = "BACKWARD";

                                if (road.ControlPoints.Last().Id == node.Id && neighborRoad.ControlPoints.First().Id == node.Id)
                                {
                                    dir = "FORWARD";
                                }
                                var otherRoadTargets = graph.Nodes
                                    .Where(n => n.Id.Contains($"{neighborRoad.Id}_{node.Id}_{dir}"))
                                    .ToList();

                                if (otherRoadTargets.Count > 0) // Csak a legkülső sávba kanyarodás
                                {
                                    var targetId = $"{neighborRoad.Id}_{node.Id}_{dir}_{otherRoadTargets.Count - 1}";
                                    var weight = Vector3.Distance(otherRoadTargets[otherRoadTargets.Count - 1].PaddedLanePosition, laneNode.PaddedLanePosition);
                                    graph.Edges.Add(new Edge { Source = laneNodeId, Target = targetId, Weight = weight });
                                }
                              

                                /*for (int k = 0; k < otherRoadTargets.Count(); k++)
                                {
                                    var targetId = $"{neighborRoad.Id}_{node.Id}_{dir}_{k}";
                                    if (!Bidirectional(graph, laneNodeId, targetId))
                                    {

                                        var weight = Vector3.Distance(otherRoadTargets[k].PaddedLanePosition, laneNode.PaddedLanePosition);
                                        graph.Edges.Add(new Edge { Source = laneNodeId, Target = targetId, Weight = weight });
                                    }

                                }*/
                            }
                            else if (opt == "left")
                            {
                                var neighborRoad = GetLeftSideRoad(node, road, roads);//;

                                if (neighborRoad == null) continue;

                                string dir = "BACKWARD";
                                if (road.ControlPoints.Last().Id == node.Id && neighborRoad.ControlPoints.First().Id == node.Id)
                                {
                                    dir = "FORWARD";
                                }

                                if (road.ControlPoints.First().Id == node.Id && neighborRoad.ControlPoints.First().Id == node.Id)
                                {
                                    continue;
                                }

                                if (road.ControlPoints.First().Id == node.Id && neighborRoad.ControlPoints.Last().Id == node.Id)
                                {
                                    continue;
                                }

                                var otherRoadBackwardNodes = graph.Nodes
                                    .Where(n => n.Id.Contains($"{neighborRoad.Id}_{node.Id}_{dir}"))
                                    .ToList();
                                for (int k = 0; k < otherRoadBackwardNodes.Count(); k++)
                                {
                                    var targetId = $"{neighborRoad.Id}_{node.Id}_{dir}_{i}"; //k lecserélve i-re hogy ne össze vissza legyen a kereszteződéseben
                                    if (!Bidirectional(graph, laneNodeId, targetId))
                                    {
                                        var weight = Vector3.Distance(otherRoadBackwardNodes[k].PaddedLanePosition, laneNode.PaddedLanePosition);
                                        graph.Edges.Add(new Edge { Source = laneNodeId, Target = targetId, Weight = weight });
                                    }

                                }
                            }
                            else if (opt == "through")
                            {


                                var neighbourRoad = GetFrontRoad(node, road, roads);
                                if (neighbourRoad == null) continue;


                                if (road.ControlPoints.First().Id == node.Id && neighbourRoad.ControlPoints.Last().Id == node.Id)
                                {
                                    continue;
                                }


                                var otherRoadForwardNodes = graph.Nodes
                                  .Where(n => n.Id.Contains($"{neighbourRoad.Id}_{node.Id}_FORWARD"))
                                  .ToList();

                                if (road.ForwardLanes == neighbourRoad.ForwardLanes)
                                {
                                    for (int k = 0; k < otherRoadForwardNodes.Count(); k++)
                                    {
                                        var targetId = $"{neighbourRoad.Id}_{node.Id}_FORWARD_{k}";
                                        if (!Bidirectional(graph, laneNodeId, targetId))
                                        {
                                            var weight = Vector3.Distance(otherRoadForwardNodes[k].PaddedLanePosition, laneNode.PaddedLanePosition);
                                            graph.Edges.Add(new Edge { Source = laneNodeId, Target = targetId, Weight = weight });
                                        }

                                    }
                                }
                                else
                                {
                                    for (int k = 0; k < otherRoadForwardNodes.Count(); k++)
                                    {

                                        if (k == road.ForwardLanes - 1)
                                        {
                                            var targetId = $"{neighbourRoad.Id}_{node.Id}_FORWARD_{k}";
                                            if (!Bidirectional(graph, laneNodeId, targetId))
                                            {
                                                var weight = Vector3.Distance(otherRoadForwardNodes[k].PaddedLanePosition, laneNode.PaddedLanePosition);
                                                graph.Edges.Add(new Edge { Source = laneNodeId, Target = targetId, Weight = weight });
                                            }

                                          
                                        }
                                        else
                                        {
                                            var targetId = $"{neighbourRoad.Id}_{node.Id}_FORWARD_{k}";
                                            if (!Bidirectional(graph, laneNodeId, targetId))
                                            {
                                                var weight = Vector3.Distance(otherRoadForwardNodes[k].PaddedLanePosition, laneNode.PaddedLanePosition);
                                                graph.Edges.Add(new Edge { Source = laneNodeId, Target = targetId, Weight = weight });
                                            }
                                        }
                                        

                                    }
                                }
                               

                                //var neighborRoad = GetFrontRoad(road, roads);//;
                                //graph.Edges.Add(new Edge { Source = laneNodeId, Target = $"{neighborRoad.Id}_{node.Id}_FORWARD_{i}" });
                            }
                        }

                    }


                    //Bacward lanes connection
                    for (var i = 0; i < road.BackwardLanes; i++)
                    {
                        var laneNodeId = $"{road.Id}_{node.Id}_BACKWARD_{i}";
                        var laneNode = graph.Nodes.FirstOrDefault(x => x.Id == laneNodeId);
                       
                        var option = road.TurnLanesBackward?.Split("|")[i] ?? "default";

                        int throughLanes = road.TurnLanesBackward?.Split("|").Count(x=>x == "through") ?? 0;
                        


                        if (option == "default")
                        {
                            int index = road.ControlPoints.FindIndex(x => x.Id == node.Id);
                            if (index == 0)
                            {
                                option = "right;left;through";
                            }

                            else if (index == road.ControlPoints.Count - 1)
                            {
                                option = "right;through";
                            }



                        }

                        foreach (var opt in option.Split(";"))
                        {

                            
                            if (opt == "right")
                            {
                                var neighborRoad = GetRightSideRoad(node, road, roads);//;
                                if (neighborRoad == null) continue;

                                string dir = "BACKWARD";

                                if (road.ControlPoints.Last().Id == node.Id)
                                {
                                    continue;
                                }


                                var otherRoadTargets = graph.Nodes
                                    .Where(n => n.Id.Contains($"{neighborRoad.Id}_{node.Id}_{dir}"))
                                    .ToList();


                                if (road.ControlPoints.First().Id == neighborRoad.ControlPoints.First().Id)
                                {
                                    dir = "FORWARD";
                                }
                                for (int k = 0; k < otherRoadTargets.Count(); k++)
                                {
                                    var targetId = $"{neighborRoad.Id}_{node.Id}_{dir}_{k}";



                                    if (!Bidirectional(graph, laneNodeId, targetId))
                                    {
                                        var weight = Vector3.Distance(otherRoadTargets[k].PaddedLanePosition, laneNode.PaddedLanePosition);
                                        graph.Edges.Add(new Edge { Source = laneNodeId, Target = targetId, Weight = weight });
                                    }
                                    else
                                    {
                                        Debug.Log("Nem adhatom hozzá");
                                    }
                                }
                            }
                            else if (opt == "left")
                            {
                                var neighborRoad = GetLeftSideRoad(node, road, roads);
                                if (neighborRoad == null)
                                {
                                    neighborRoad = GetFrontRoad(node, road, roads);
                                }

                                if (neighborRoad == null) continue;

                                string dir = "BACKWARD";

                                var otherRoadBackwardNodes = graph.Nodes
                                    .Where(n => n.Id.Contains($"{neighborRoad.Id}_{node.Id}_{dir}"))
                                    .ToList();


                                if (road.ControlPoints[0].Id == node.Id && neighborRoad.ControlPoints[0].Id == node.Id)
                                {
                                    dir = "FORWARD";
                                }

                                for (int k = 0; k < otherRoadBackwardNodes.Count(); k++)
                                {
                                    var targetId = $"{neighborRoad.Id}_{node.Id}_{dir}_{i}"; //k lecserélve i-re hogy ne össze vissza legyen a kereszteződéseben

                                    if (!Bidirectional(graph, laneNodeId, targetId))
                                    {
                                        var weight = Vector3.Distance(otherRoadBackwardNodes[k].PaddedLanePosition, laneNode.PaddedLanePosition);
                                        graph.Edges.Add(new Edge { Source = laneNodeId, Target = targetId, Weight = weight });
                                    }
                                   
                                }
                            }
                            else if (opt == "through")
                            {
                                var neighbourRoad = GetFrontRoad(node, road, roads);
                                if (neighbourRoad == null) continue;



                                if (road.ControlPoints.Last().Id == node.Id && neighbourRoad.ControlPoints.First().Id == node.Id)
                                {
                                    continue;
                                }

                                var otherBackwardNodes = graph.Nodes
                                  .Where(n => n.Id.Contains($"{neighbourRoad.Id}_{node.Id}_BACKWARD"))
                                  .ToList();
                                

                                if (neighbourRoad.BackwardLanes == road.BackwardLanes)
                                {
                                    for (int k = 0; k < otherBackwardNodes.Count(); k++)
                                    {


                                        var targetId = $"{neighbourRoad.Id}_{node.Id}_BACKWARD_{i}";

                                        if (!Bidirectional(graph, laneNodeId, targetId))
                                        {
                                            var weight = Vector3.Distance(otherBackwardNodes[k].PaddedLanePosition, laneNode.PaddedLanePosition);
                                            graph.Edges.Add(new Edge { Source = laneNodeId, Target = targetId, Weight = weight });
                                        }

                                    }
                                }

                                else if (neighbourRoad.BackwardLanes < road.BackwardLanes)
                                {
                                    for (int k = 0; k < otherBackwardNodes.Count(); k++)
                                    {
                                        
                                        /*if (i - throughLanes == k)
                                        {
                                            var targetId = $"{neighbourRoad.Id}_{node.Id}_BACKWARD_{k}";

                                            if (!Bidirectional(graph, laneNodeId, targetId))
                                            {
                                                var weight = Vector3.Distance(otherBackwardNodes[k].PaddedLanePosition, laneNode.PaddedLanePosition);
                                                graph.Edges.Add(new Edge { Source = laneNodeId, Target = targetId, Weight = weight });
                                            }
                                        }*/

                                        var targetId = $"{neighbourRoad.Id}_{node.Id}_BACKWARD_{k}";

                                        if (!Bidirectional(graph, laneNodeId, targetId))
                                        {
                                            var weight = Vector3.Distance(otherBackwardNodes[k].PaddedLanePosition, laneNode.PaddedLanePosition);
                                            graph.Edges.Add(new Edge { Source = laneNodeId, Target = targetId, Weight = weight });
                                        }



                                    }
                                }

                                else if (neighbourRoad.BackwardLanes > road.BackwardLanes)
                                {
                                    for (int k = 0; k < otherBackwardNodes.Count(); k++)
                                    {

                                    

                                        var targetId = $"{neighbourRoad.Id}_{node.Id}_BACKWARD_{k}";

                                        if (!Bidirectional(graph, laneNodeId, targetId))
                                        {
                                            var weight = Vector3.Distance(otherBackwardNodes[k].PaddedLanePosition, laneNode.PaddedLanePosition);
                                            graph.Edges.Add(new Edge { Source = laneNodeId, Target = targetId, Weight = weight });
                                        }

                                    }
                                }
                                else
                                {
                                    for (int k = 0; k < otherBackwardNodes.Count(); k++)
                                    {


                                        var targetId = $"{neighbourRoad.Id}_{node.Id}_BACKWARD_{k}";

                                        if (!Bidirectional(graph, laneNodeId, targetId))
                                        {
                                            var weight = Vector3.Distance(otherBackwardNodes[k].PaddedLanePosition, laneNode.PaddedLanePosition);
                                            graph.Edges.Add(new Edge { Source = laneNodeId, Target = targetId, Weight = weight });
                                        }

                                    }
                                }
                               
                                
                                   
                                

                              

                                //var neighborRoad = GetFrontRoad(road, roads);//;
                                //graph.Edges.Add(new Edge { Source = laneNodeId, Target = $"{neighborRoad.Id}_{node.Id}_FORWARD_{i}" });
                            }


                        }



                    }
                }
            }

            return graph;

        }


        private static bool Bidirectional(GraphOSM graph, string sourceId, string targetId)
        {
            var bidirectional = graph.Edges.FirstOrDefault(x => x.Target == sourceId && x.Source == targetId);
            return bidirectional != null;
        }


        private static Road GetRightSideRoad(Node intersectionNode, Road currentRoad, List<Road> neighbourRoads)
        {


            Node prevIntersectionNode = null;
            if (currentRoad.ControlPoints[currentRoad.ControlPoints.Count - 1] == intersectionNode)
            {
                prevIntersectionNode = currentRoad.ControlPoints[currentRoad.ControlPoints.Count - 2];
            }
            else
            {
                prevIntersectionNode = currentRoad.ControlPoints[1];
            }

            var originDirVector = prevIntersectionNode.OriginalPosition - intersectionNode.OriginalPosition;
            // Ha első

            foreach (var road in neighbourRoads.Where(x => x.Id != currentRoad.Id))
            {
                // Ha utolsó
                if (road.ControlPoints[road.ControlPoints.Count - 1].Id == intersectionNode.Id)
                {
                    var prevNode = road.ControlPoints[road.ControlPoints.Count - 2];
                    var dirVector = prevNode.OriginalPosition - intersectionNode.OriginalPosition;

                    var angle = Vector3.Angle(dirVector.normalized, originDirVector.normalized);
                    var dot = Vector3.Dot(dirVector.normalized, originDirVector.normalized);

                    var isRightTurn = Vector3.Cross(dirVector.normalized, originDirVector.normalized).normalized.y == 1;

                    if (isRightTurn && angle > 65 && angle < 130)
                    {
                        return road;
                    }
                }
                else
                {
                    var nextNode = road.ControlPoints[1];
                    var dirVector = nextNode.OriginalPosition - intersectionNode.OriginalPosition;

                    var angle = Vector3.Angle(dirVector.normalized, originDirVector.normalized);
                    var dot = Vector3.Dot(dirVector.normalized, originDirVector.normalized);

                    var isRightTurn = Vector3.Cross(dirVector.normalized, originDirVector.normalized).normalized.y == 1;


                    if (isRightTurn && angle > 65 && angle < 130)
                    {
                        return road;
                    }
                }
            }
            //
            return null;
        }


        private static Road GetLeftSideRoad(Node intersectionNode, Road currentRoad, List<Road> neighbourRoads)
        {


            Node prevIntersectionNode = null;
            if (currentRoad.ControlPoints[currentRoad.ControlPoints.Count - 1] == intersectionNode)
            {
                prevIntersectionNode = currentRoad.ControlPoints[currentRoad.ControlPoints.Count - 2];
            }
            else
            {
                prevIntersectionNode = currentRoad.ControlPoints[1];
            }

            var originDirVector = prevIntersectionNode.OriginalPosition - intersectionNode.OriginalPosition;
            // Ha első

            foreach (var road in neighbourRoads.Where(x => x.Id != currentRoad.Id))
            {
                // Ha utolsó
                if (road.ControlPoints[road.ControlPoints.Count - 1].Id == intersectionNode.Id)
                {
                    var prevNode = road.ControlPoints[road.ControlPoints.Count - 2];
                    var dirVector = prevNode.OriginalPosition - intersectionNode.OriginalPosition;

                    var angle = Vector3.Angle(dirVector.normalized, originDirVector.normalized);
                    var dot = Vector3.Dot(dirVector.normalized, originDirVector.normalized);


                    var isLeftTurn = Vector3.Cross(dirVector.normalized, originDirVector.normalized).normalized.y == -1;

                    if (isLeftTurn && angle > 65 && angle < 130)
                    {
                        return road;
                    }
                }
                else
                {
                    var nextNode = road.ControlPoints[1];
                    var dirVector = nextNode.OriginalPosition - intersectionNode.OriginalPosition;

                    var angle = Vector3.Angle(dirVector.normalized, originDirVector.normalized);
                    var dot = Vector3.Dot(dirVector.normalized, originDirVector.normalized);

                    var isLeftTurn = Vector3.Cross(dirVector.normalized, originDirVector.normalized).normalized.y == -1;
                    if (isLeftTurn && angle > 65 && angle < 130)
                    {
                        return road;
                    }
                }
            }
            //
            return null;
        }

        private static Road GetFrontRoad(Node intersectionNode, Road currentRoad, List<Road> neighbourRoads)
        {


            Node prevIntersectionNode = null;
            if (currentRoad.ControlPoints[currentRoad.ControlPoints.Count - 1] == intersectionNode)
            {
                prevIntersectionNode = currentRoad.ControlPoints[currentRoad.ControlPoints.Count - 2];
            }
            else
            {
                prevIntersectionNode = currentRoad.ControlPoints[1];
            }

            var originDirVector = prevIntersectionNode.OriginalPosition - intersectionNode.OriginalPosition;
            // Ha első

            foreach (var road in neighbourRoads.Where(x => x.Id != currentRoad.Id))
            {
                // Ha utolsó
                if (road.ControlPoints[road.ControlPoints.Count - 1].Id == intersectionNode.Id)
                {
                    var prevNode = road.ControlPoints[road.ControlPoints.Count - 2];
                    var dirVector = prevNode.OriginalPosition - intersectionNode.OriginalPosition;

                    var angle = Vector3.Angle(dirVector.normalized, originDirVector.normalized);
                    var dot = Vector3.Dot(dirVector.normalized, originDirVector.normalized);

                    if (angle > 165 && angle <= 180)
                    {
                        return road;
                    }
                }
                else
                {
                    var nextNode = road.ControlPoints[1];
                    var dirVector = nextNode.OriginalPosition - intersectionNode.OriginalPosition;

                    var angle = Vector3.Angle(dirVector.normalized, originDirVector.normalized);
                    var dot = Vector3.Dot(dirVector.normalized, originDirVector.normalized);
                    if (angle > 165 && angle <= 180)
                    {
                        return road;
                    }
                }
            }
            //
            return null;

        }
        /// <summary>
        /// Build the graph from proccesed OSM data
        /// </summary>
        /// <param name="network">List of roads</param>
        /// <returns>Graph</returns>
        public static Dictionary<string, List<Node>> BuildGraph(List<Road> network)
        {
            Dictionary<string, List<Node>> graph = new Dictionary<string, List<Node>>();

            foreach (var road in network)
            {
                for (int i = 0; i < road.ControlPoints.Count; i++)
                {

                    Node current = road.ControlPoints[i];

                    if (!graph.ContainsKey(current.Id))
                    {

                        graph[current.Id] = new List<Node>();

                        // first
                        if (i == 0)
                        {
                            graph[current.Id].Add(road.ControlPoints[i + 1]);
                        }

                        // not last, not first
                        if (i > 0 && i < road.ControlPoints.Count - 1)
                        {
                            graph[current.Id].Add(road.ControlPoints[i - 1]);
                            graph[current.Id].Add(road.ControlPoints[i + 1]);
                        }

                        // last
                        if (i == road.ControlPoints.Count - 1)
                        {
                            graph[current.Id].Add(road.ControlPoints[i - 1]);
                        }


                    }
                    else
                    {
                        if (i == 0)
                        {
                            graph[current.Id].Add(road.ControlPoints[i + 1]);
                        }

                        // not last, not first
                        if (i > 0 && i < road.ControlPoints.Count - 1)
                        {
                            graph[current.Id].Add(road.ControlPoints[i - 1]);
                            graph[current.Id].Add(road.ControlPoints[i + 1]);
                        }

                        // last
                        if (i == road.ControlPoints.Count - 1)
                        {
                            graph[current.Id].Add(road.ControlPoints[i - 1]);
                        }
                    }
                }
            }

            return graph;
        }


        /// <summary>
        /// Print graph
        /// </summary>
        /// <param name="graph">Graph to print</param>
        public static void PrintGraph(Dictionary<string, List<Node>> graph)
        {


            foreach (var entry in graph)
            {
                Debug.Log($"Key: {entry.Key}\n");
                entry.Value.ForEach(x => Debug.Log($"\t{x.ToString()}\n"));
            }
        }

        /// <summary>
        /// Road node collection builder 
        /// </summary>
        /// <param name="roads">Roads</param>
        /// <returns>All nodes with O1 lookup</returns>

        public static Dictionary<string, Node> BuildNodeCollection(List<Road> roads)
        {

            Dictionary<string, Node> nodeCollection = new Dictionary<string, Node>();


            foreach (var road in roads)
            {
                foreach (var cp in road.ControlPoints)
                {

                    if (nodeCollection.ContainsKey(cp.Id))
                    {
                        continue;
                    }

                    if (!nodeCollection.ContainsKey(cp.Id))
                    {
                        nodeCollection.Add(cp.Id, cp);
                    }
                }
            }

            return nodeCollection;


        }
    }


}
