
using Assets.Entities;
using Globals;
using Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using UnityEngine;
using static Assets.Entities.TrafficSignal;

namespace Assets.Services
{

    /// <summary>
    /// XML proccessor and unit categorizer
    /// </summary>
    public class XmlProcessorService : IXmlProcessorService
    {

        private const float SCALE_CONSTANT = 150000f;

        private const float EARTH_RADIUS = 6378137f;



        /// <summary>
        /// OSM map minimum longitude
        /// </summary>
        private static float _minlon { get; set; }

        /// <summary>
        /// OSM map minimum latitude
        /// </summary>
        private static float _minlat { get; set; }

        /// <summary>
        /// OSM map maximum latitude
        /// </summary>
        private float _maxlat { get; set; }

        /// <summary>
        /// OSM map maximum longitude
        /// </summary>
        private float _maxlon { get; set; }

        private const float METERS_PER_DEGREE_LAT = 111320f; // 1 fok szélesség kb. 111 km
        private float METERS_PER_DEGREE_LON = 111320f * Mathf.Cos(_minlat * Mathf.Deg2Rad);

        /// <summary>
        /// All node elements from OSM data
        /// </summary>
        private List<XElement> _nodeElements { get; set; } = new List<XElement>();

        /// <summary>
        /// All way elements from OSM data
        /// </summary>
        private List<XElement> _wayElements { get; set; } = new List<XElement>();

        /// <summary>
        /// Constructor
        /// </summary>
        public XmlProcessorService() { }


        /// <summary>
        /// Parsing XML document
        /// <param name="fileName">Filename</param>
        /// </summary>
        public MapData LoadXMLDocument(string fileName)
        {
            Debug.Log($"Load xml document with filename={fileName}");

            XDocument xmlDocument = XDocument.Load($"{GlobalSystemConstants.OSM_FILES_DIRECTORY_PATH}/{fileName}");
            XElement? root = xmlDocument.Root;
            if (root == null)
                throw new Exception("Root element is not found!");

            return ProcessXmlDocument(root);
        }


        /// <summary>
        /// Process XML document
        /// </summary>
        /// <param name="root">Root element</param>
        /// <returns>Map data</returns>
        private MapData ProcessXmlDocument(XElement root)
        {

            Debug.Log("Process XML document.");

            CalculateMapSizes(root);
            GetNodeElements(root);
            GetWayElements(root);


            MapData mapData = new MapData
            {
                TrafficLights = GetTrafficLights(),
                Roads = GetRoadList(),
                /*Buildings = GetBuildingList()*/
            };

            Debug.Log($"Traffic lights: {mapData.TrafficLights.Count}");
            Debug.Log($"Roads: {mapData.Roads.Count}");
            Debug.Log($"Buildings: {mapData.Buildings.Count}");


            return mapData;

        }


        #region Calculations


      

        /// <summary>
        /// Normalizing given latitude and longitude values
        /// </summary>
        /// <param name="lat">Latitude to normalize</param>
        /// <param name="lon">Longitude to normalize</param>
        /// <returns>Normalized latitude and longitude</returns>
        private (float, float) Normalize(float lat, float lon)
        {
            float normalizedLat = (_minlat - lat) * SCALE_CONSTANT;
            float normalizedLon = (_minlon - lon) * SCALE_CONSTANT;
            return (normalizedLat, normalizedLon);
        }

        /// <summary>
        /// Calculate map size from bounds
        /// </summary>
        /// <param name="root">Root element</param>
        /// <exception cref="Exception">Bounds not found exception</exception>
        private void CalculateMapSizes(XElement root)
        {

            XElement? boundsEl = root.Element("bounds");

            if (boundsEl == null) throw new Exception("Map bounds not exist!");

            float minlon = ParseFloat(boundsEl.Attribute("minlon")!.Value);
            float maxlon = ParseFloat(boundsEl.Attribute("maxlon")!.Value);

            float minlat = ParseFloat(boundsEl.Attribute("minlat")!.Value);
            float maxlat = ParseFloat(boundsEl.Attribute("maxlat")!.Value);


            _minlon = minlon;
            _minlat = minlat;

            _maxlat = maxlat;
            _maxlon = maxlon;
        }


        /// <summary>
        /// Get all way xml element from the document
        /// </summary>
        /// <param name="root">Root element</param>
        /// <returns>Way element list</returns>
        private void GetWayElements(XElement root)
        {
            _wayElements = root.Elements("way").ToList();
        }



        /// <summary>
        /// Get all node xml element from the document
        /// </summary>
        /// <param name="root">Root element</param>
        /// <returns>Node element list</returns>
        private void GetNodeElements(XElement root)
        {
            _nodeElements = root.Elements("node").ToList();
        }


        /// <summary>
        /// Get all ref for a way element
        /// </summary>
        /// <param name="way">Way element</param>
        /// <returns>Ref list</returns>
        private List<string?> GetRefElementsForWay(XElement way)
        {
            return way.Elements("nd").Select(x => x.Attribute("ref")?.Value).ToList();
        }

        /// <summary>
        /// Get road list with control points
        /// and road name
        /// </summary>
        /// <returns>Road list</returns>
        private List<Road> GetRoadList()
        {
            List<Road> roadList = new List<Road>();


            if (_wayElements == null) throw new Exception("Way elements not found!");

            _wayElements.ForEach(way =>
            {

                List<string> ndRefs = GetRefElementsForWay(way);
                List<XElement> wayNodeList = ndRefs.Select(x =>
                {
                    return _nodeElements.FirstOrDefault(y => y.Attribute("id").Value == x);
                }).ToList();



              
                Road road = new Road();
                string roadId = way.Attribute("id").Value;
                road.Id = roadId;

                List<XElement> wayTags = way.Elements("tag").ToList();
                XElement laneTag = wayTags.Where(x=>x.Attribute("k").Value == "lanes").FirstOrDefault();
                XElement lanesBackward = wayTags.Where(x=>x.Attribute("k").Value == "lanes:backward").FirstOrDefault();
                XElement lanesForward = wayTags.Where(x=>x.Attribute("k").Value == "lanes:forward").FirstOrDefault();
                XElement turnLanesBackward = wayTags.Where(x=>x.Attribute("k").Value == "turn:lanes:backward").FirstOrDefault();
                XElement turnLanesForward = wayTags.Where(x=>x.Attribute("k").Value == "turn:lanes:forward").FirstOrDefault();
                XElement turnLanes = wayTags.Where(x=>x.Attribute("k").Value == "turn:lanes").FirstOrDefault();
                XElement oneWay = wayTags.Where(x => x.Attribute("k").Value == "oneway").FirstOrDefault();
              
                int lanes = laneTag == null ? 2 : int.Parse(laneTag.Attribute("v")?.Value);
              
                road.Lanes = lanes;

                road.IsOneWay = oneWay != null ? oneWay.Attribute("v")?.Value == "yes" : false;

                road.TurnLanes = turnLanes?.Attribute("v")?.Value;
                road.TurnLanesForward = turnLanesForward?.Attribute("v")?.Value;
                road.TurnLanesBackward = turnLanesBackward?.Attribute("v")?.Value;
                road.BackwardLanes = road.IsOneWay ? 0 : int.Parse(lanesBackward?.Attribute("v")?.Value ?? $"{lanes / 2 * 1}");
                road.ForwardLanes = road.IsOneWay ? road.Lanes : int.Parse(lanesForward?.Attribute("v")?.Value ?? $"{lanes / 2 * 1}");
               
                List <Node> controlPoints = wayNodeList.Select(node =>
                {
                    //Meg kell nézni hogy másikba is benne van a node és annak pozícióját
                    string id = node.Attribute("id").Value;

                    float lat = ParseFloat(node.Attribute("lat")!.Value);
                    float lon = ParseFloat(node.Attribute("lon")!.Value);

                    (float normLat, float normLon) = Normalize(lat, lon);


                    Node cp = new Node();
                    cp.Id = id;
                    cp.RoadId = roadId;
                    cp.Position = new Vector3(normLon, 0, normLat);
                    (bool isIntNode, int count) = CheckNodeIntersection(id);
                    cp.IsIntersectionNode = isIntNode;
                    cp.IntersectCount = count;
                    cp.OriginalPosition = new Vector3(normLon, 0, normLat);
                   
                    return cp;
                }).ToList();

                road.ControlPoints = controlPoints;

             
                roadList.Add(road);


             
            });


            List<Road> finalRoads = new List<Road>();
            foreach (var road in roadList)
            {
                bool findSplit = false;
                foreach (var cp in road.ControlPoints)
                {
                    (bool shouldSplit, Road splitRoad) = ShouldRoadSplit(roadList, cp);
                    if (shouldSplit && splitRoad.Id == road.Id)
                    {
                        findSplit = true;
                        var splitResult = SplitRoad(cp, road);
                        finalRoads.AddRange(splitResult);
                    }
                  
                }

                if (!findSplit)
                {
                    finalRoads.Add(road);
                }

            }
            return finalRoads;
        }


        private (bool, int) CheckNodeIntersection(string nodeId)
        {
            int count = _wayElements.Where(way => GetRefElementsForWay(way).Contains(nodeId)).ToList().Count();
            return (count > 1, count);
        }


        private List<Road> SplitRoad(Node splitNode, Road road)
        {
            List<Road> splittedRoads = new List<Road>();

            var road1 = new Road();
            var road2 = new Road();
            road1.Id = road.Id + $"-sr0-{splitNode.Id}";
            road1.TurnLanes = road.TurnLanes;
            road1.TurnLanesBackward = road.TurnLanesBackward;
            road1.TurnLanesForward = road.TurnLanesForward;
            road1.Lanes = road.Lanes;
            road1.IsOneWay = road.IsOneWay;
            road1.ForwardLanes = road.ForwardLanes;
            road1.BackwardLanes = road.BackwardLanes;


            road2.Id = road.Id + $"-sr1-{splitNode.Id}";
            road2.TurnLanes = road.TurnLanes;
            road2.TurnLanesBackward = road.TurnLanesBackward;
            road2.TurnLanesForward = road.TurnLanesForward;
            road2.Lanes = road.Lanes;
            road2.IsOneWay = road.IsOneWay;
            road2.ForwardLanes = road.ForwardLanes;
            road2.BackwardLanes = road.BackwardLanes;

            //Split point index
            int nodeIndex = road.ControlPoints.FindIndex(x => x.Id == splitNode.Id);

            int r1Index = road.ControlPoints.Count - 1;
            int r2Index = 0;

            for (int i = nodeIndex + 1; i < road.ControlPoints.Count; ++i)
            {
                if (road.ControlPoints[i].IsIntersectionNode)
                {
                    
                    r1Index = i;
                    break;
                }
            }

            
            for (int i = nodeIndex - 1; i >= 0; --i)
            {
                if (road.ControlPoints[i].IsIntersectionNode)
                {
                    r2Index = i;
                    break;
                }
            }

           
            road1.ControlPoints.AddRange(road.ControlPoints.Skip(nodeIndex).Take(r1Index - nodeIndex + 1));
            road2.ControlPoints.AddRange(road.ControlPoints.Skip(r2Index).Take(nodeIndex - r2Index + 1));
           

            splittedRoads.Add(road1);
            splittedRoads.Add(road2);
           

            return splittedRoads;
        }



        private (bool, Road) ShouldRoadSplit(List<Road> roads, Node splitNode)
        {
            var filteredRoads = roads.Where(x=>x.ControlPoints.Any(y=>y.Id == splitNode.Id));
            bool isInner = false;
            bool isLastOrFirst = false;
            Road splitRoad = null;
            foreach (var road in filteredRoads)
            {
                int index = road.ControlPoints.FindIndex(x => x.Id == splitNode.Id);
                isInner = (index != 0 && index != road.ControlPoints.Count - 1) || isInner;
                if (index != 0 && index != road.ControlPoints.Count - 1)
                {
                    splitRoad = road;
                }
             
                isLastOrFirst = (index == 0 || index == road.ControlPoints.Count - 1) || isLastOrFirst;
            }

            return (isInner && isLastOrFirst, splitRoad);
        }


        /// <summary>
        /// Get building list with control points
        /// </summary>
        /// <returns>Building list</returns>
        /// <exception cref="Exception">Way elements not found</exception>
        private List<Building> GetBuildingList()
        {

            List<Building> buildingList = new List<Building>();

            if (_wayElements == null) throw new Exception("Way elements not found!");

            _wayElements.ForEach(way =>
            {
                List<XElement> wayNodeList = _nodeElements
                .Where(node => GetRefElementsForWay(way).Contains(node.Attribute("id")!.Value))
                .ToList();

                Debug.Log($"Way node list count: {wayNodeList.Count}");


                bool isBuilding = way.Elements("tag").Any(x => (x.Attribute("k")?.Value == "building") && (x.Attribute("v")?.Value == "yes"));

                if (isBuilding)
                {
                    Building road = new Building
                    {
                        ControlPoints = wayNodeList.Select(node =>
                        {

                            float lat = ParseFloat(node.Attribute("lat")!.Value);
                            float lon = ParseFloat(node.Attribute("lon")!.Value);

                            (float normLat, float normLon) = Normalize(lat, lon);
                            return new Vector3(normLon, 0, normLat);
                        }).ToList()
                    };

                    buildingList.Add(road);
                }

            });

            Debug.Log($"Building list count: {buildingList.Count}");

            return buildingList;
        }

        /// <summary>
        /// Get all traffic lights from the document
        /// </summary>
        /// <returns>Traffic light list</returns>
        private List<TrafficSignal> GetTrafficLights()
        {

            List<XElement> trafficLightElements = _nodeElements
                .Where(x =>
                {
                    var filter = x.Elements("tag").Attributes().ToList().Where(y => y.Value == "traffic_signals").ToList();
                    if (filter.Count > 0) return true;
                    return false;
                }).ToList();

            List<TrafficSignal> trafficLights = trafficLightElements.Select(x =>
            {
                string id = x.Attribute("id")?.Value;

                var tags = x.Elements("tag");
                var directionTag = tags.FirstOrDefault(y=>y.Attribute("k").Value == "traffic_signals:direction");
                var crossingTag = tags.FirstOrDefault(y=>y.Attribute("k").Value == "crossing");


                var crossingTagValue = crossingTag?.Attribute("v").Value;

                SignalDirection signalDirection =  /*crossingTagValue == "traffic_signals" ? SignalDirection.BOTH :*/ SignalDirection.FORWARD;

                if (directionTag != null)
                {

                    var directionTagValue = directionTag.Attribute("v").Value;

                    if (directionTagValue == "both")
                    {
                        signalDirection = SignalDirection.BOTH;
                    }
                    else if (directionTagValue == "forward")
                    {
                        signalDirection = SignalDirection.FORWARD;
                    }
                    else if (directionTagValue == "backward")
                    {
                        signalDirection = SignalDirection.BACKWARD;
                    }
                }
            
             
                float lat = ParseFloat(x.Attribute("lat")!.Value);
                float lon = ParseFloat(x.Attribute("lon")!.Value);
                (float normLat, float normLon) = Normalize(lat, lon);

                TrafficSignal trafficSignal = new TrafficSignal();
                trafficSignal.Id = id;
                trafficSignal.Position = new Vector3(normLon, 0, normLat);
                trafficSignal.Direction = signalDirection;

                return trafficSignal;
            }).ToList();

            Debug.Log($"Traffic lights count: {trafficLights.Count}");

            return trafficLights;
        }


        public float ParseFloat(string value)
        {
            return float.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
        }

        #endregion



    }
}
