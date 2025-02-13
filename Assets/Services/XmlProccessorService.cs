
using Assets.Entities;
using Globals;
using Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;

namespace Assets.Services
{

    /// <summary>
    /// XML proccessor and unit categorizer
    /// </summary>
    public class XmlProcessorService : IXmlProcessorService
    {

        private const int SCALE_CONSTANT = 150000;

        /// <summary>
        /// OSM map minimum longitude
        /// </summary>
        private float _minlon { get; set; }

        /// <summary>
        /// OSM map minimum latitude
        /// </summary>
        private float _minlat { get; set; }

        /// <summary>
        /// OSM map maximum latitude
        /// </summary>
        private float _maxlat { get; set; }

        /// <summary>
        /// OSM map maximum longitude
        /// </summary>
        private float _maxlon { get; set; }

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


                List<Node> controlPoints = wayNodeList.Select(node =>
                {

                    string id = node.Attribute("id").Value;
                    if (id == "42428179")
                    {

                        int tagCount = node.Elements("tag").Count();
                        Debug.Log($"XML process: {id} tags:{tagCount}");
                    }

                    float lat = ParseFloat(node.Attribute("lat")!.Value);
                    float lon = ParseFloat(node.Attribute("lon")!.Value);

                    (float normLat, float normLon) = Normalize(lat, lon);


                    Node cp = new Node();
                    cp.Id = id;
                    cp.Position = new Vector3(normLon, 0, normLat);
                    cp.IsIntersectionNode = CheckNodeIntersection(id);
                    return cp;
                }).ToList();

                road.ControlPoints = controlPoints;
                roadList.Add(road);


            });

            Debug.Log($"Road list count: {roadList.Count}");

            return roadList;
        }


        private bool CheckNodeIntersection(string nodeId)
        {
            return _wayElements.Where(way => GetRefElementsForWay(way).Contains(nodeId)).ToList().Count() > 1;
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
                float lat = ParseFloat(x.Attribute("lat")!.Value);
                float lon = ParseFloat(x.Attribute("lon")!.Value);
                (float normLat, float normLon) = Normalize(lat, lon);

                TrafficSignal trafficSignal = new TrafficSignal();
                trafficSignal.Id = id;
                trafficSignal.Position = new Vector3(normLon, 0, normLat);

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
