using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using Assets.Services;
using Unity.Barracuda;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;
using UnityEditor;
using UnityEngine;
using static Assets.Scripts.TrafficLightController;

namespace Assets.Scripts
{
    /// <summary>
    /// Traffic lights master agent
    /// handling red and green timer
    /// sycncronizing the lamps
    /// </summary>
    public class MasterController : Agent
    {

        /// <summary>
        /// Spawner
        /// </summary>
        public Spawner spawner { get; set; }

        /// <summary>
        /// TrafficLights
        /// </summary>
        private List<TrafficLightController> trafficLights;

        /// <summary>
        /// Vehicles
        /// </summary>
        private List<AStarAgentController> vehicles;

        /// <summary>
        /// Simulation time
        /// </summary>
        public float simulationTime = 0f;

        /// <summary>
        /// Maximal cycle time for a traffic light
        /// </summary>
        public const float maxCycleTime = 80f;

        /// <summary>
        /// Minimal cycle time for a traffic light
        /// </summary>
        public const float minCycleTime = 20f;


        public int actionCount { get; private set; } = 0;

        
        public Dictionary<string, TrafficMetricData> trafficLightData = new Dictionary<string, TrafficMetricData>();

        public List<int> numberOfAccidents = new List<int>();
      

        public void OnEnable()
        {

            trafficLights = GetTrafficLights();
            vehicles = GetVehicles();


            var behaviorParams = GetComponent<BehaviorParameters>();
            behaviorParams.BehaviorType = BehaviorType.Default;
            behaviorParams.BehaviorName = "MasterController";

            //Load an existing model 
            NNModel model = AssetDatabase.LoadAssetAtPath<NNModel>("Assets/training/results/ContTest8/MasterController.onnx");


            Debug.Log($"Model: {(model == null ? "null" : model.name)}");
            behaviorParams.Model = model;

            /*int actionSpaceSize = GlobalNetworkService.trafficLightsCount * 2;
            Debug.Log($"Space size: {actionSpaceSize}");
            behaviorParams.BrainParameters.ActionSpec = ActionSpec.MakeContinuous(actionSpaceSize);*/


            int[] discreteBranchSizes = new int[trafficLights.Count];
            for (int i = 0; i< trafficLights.Count; i++)
            {
                discreteBranchSizes[i] = 2;
            }
            behaviorParams.BrainParameters.ActionSpec = ActionSpec.MakeContinuous(GlobalNetworkService.trafficLightsCount * 2);
            

            int vectorObservationSpaceSize = GlobalNetworkService.trafficLightsCount * 4;
            Debug.Log($"Observation space size: {vectorObservationSpaceSize}");
            behaviorParams.BrainParameters.VectorObservationSize = vectorObservationSpaceSize;
            base.OnEnable();
        }

        /// <summary>
        /// Collecting observations from the environment
        /// to make decisions
        /// </summary>
        /// <param name="sensor">Sensor</param>
        public override void CollectObservations(VectorSensor sensor)
        {
            foreach (var trafficLight in trafficLights)
            {

                /*Debug.Log("Observation queque length: " + trafficLight.queueLength);
                Debug.Log("Observation passing count: " + trafficLight.passingCount);
                Debug.Log("Observation traffic light state " + (int)trafficLight.currentState);*/
                sensor.AddObservation((int)trafficLight.currentState);
                sensor.AddObservation(trafficLight.queueLength);
                sensor.AddObservation(trafficLight.pressure);
                sensor.AddObservation(trafficLight.passingCount);

            }
        }

        /// <summary>
        /// Heuristic definition
        /// </summary>
        /// <param name="actionsOut">Emitted actions</param>
        public override void Heuristic(in ActionBuffers actionsOut)
        {

        }


        /// <summary>
        /// Make actions based on observations from the environment
        /// Actions: contineous, discrete
        /// </summary>
        /// <param name="actions">Action list</param>
        public override void OnActionReceived(ActionBuffers actions)
        {

           
            if (StepCount % 1500 == 0)
            {
                

                foreach (var tfData in trafficLightData)
                {
                    var measuredMetrics = trafficLightData[tfData.Key];


                    var avgCycleTime = measuredMetrics.greenTimer.Average() + measuredMetrics.redTimer.Average() + 2f;

                    if (avgCycleTime < minCycleTime || avgCycleTime > maxCycleTime)
                    {
                        Debug.Log($"Cycle time error END! {avgCycleTime}");
                        AddReward(-0.2f * Mathf.Abs(avgCycleTime - ((minCycleTime + maxCycleTime) / 2)));
                        if (actionCount > 5)
                        {
                            EndEpisode();
                        }
                    }
                    else
                    {
                        AddReward(0.45f);
                    }

                    var avgPressure = measuredMetrics.pressure.Average();
                    AddReward((float)measuredMetrics.passingCount.Average() * 0.05f);

                    
                    AddReward((float)measuredMetrics.queueLength.Average() * -0.03f);
                }


                
              
                ContinousAction(actions.ContinuousActions.ToList());

                trafficLightData.Clear();
               
                actionCount += 1;

                var avgAccidents = numberOfAccidents.Average();
                if (actionCount > 5)
                {
                    

                    if (avgAccidents < 2.5f)
                    {
                        if (avgAccidents < 1f)
                        {
                            AddReward(1f);
                            EndEpisode();
                        }
                        else
                        {
                            AddReward((float)avgAccidents * 0.15f);
                        }
                       
                    }
                    
                }
                else
                {
                    AddReward((float)avgAccidents * -0.1f);
                    EndEpisode();
                }

                numberOfAccidents.Clear();
            }
            else
            {
                foreach (var trafficLight in trafficLights)
                {

                    if (!trafficLightData.ContainsKey(trafficLight.Id))
                    {
                        trafficLightData.Add(trafficLight.Id, new TrafficMetricData());
                    }
                  
                    trafficLightData[trafficLight.Id].pressure.Add(trafficLight.pressure);
                    trafficLightData[trafficLight.Id].passingCount.Add(trafficLight.passingCount);
                    trafficLightData[trafficLight.Id].queueLength.Add(trafficLight.queueLength);
                    trafficLightData[trafficLight.Id].greenTimer.Add(trafficLight.greenTimer);
                    trafficLightData[trafficLight.Id].redTimer.Add(trafficLight.redTimer);
                    numberOfAccidents.Add(GlobalNetworkService.numberOfAccidents);
                }
            }
            
        


        }

        /// <summary>
        /// Continous action decision handling
        /// </summary>
        /// <param name="actions">Continous actions</param>
        private void ContinousAction(List<float> actions)
        {
           Debug.Log("Lefut a containous actions: " + actions.Count);
            for (int i = 0; i < trafficLights.Count; i++)
            {

                
                float greenTimeNorm = actions[i * 2];
                float redTimeNorm = actions[i * 2 + 1];


                

                //Scaling
                float greenTime = Mathf.Lerp(10f, 60f, greenTimeNorm);
                float redTime = Mathf.Lerp(10f, 60f, redTimeNorm);

                //Debug.Log($"Timers: G-{greenTime}s R-{redTime}s");


                trafficLights[i].greenTimer = greenTime;
                trafficLights[i].redTimer = redTime;


            }
        }

        /// <summary>
        /// Discrete action decision handling
        /// </summary>
        /// <param name="actions">Discrete actions</param>
        private void DiscreteAction(List<int> actions)
        {

            //Debug.Log("Action count: " + actions.Count);
            for (int i = 0; i < trafficLights.Count; i++)
            {

                //Debug.Log($"Discrete action: {actions[i]}");

                trafficLights[i].SetState((LightState)actions[i]);
            }

        }

        /// <summary>
        /// Reset the simulation environment
        /// Traffic lights reset
        /// Destroy, respawn vehicles
        /// </summary>
        public override void OnEpisodeBegin()
        {
            /*spawner.Respawn();
            foreach (var trafficLight in trafficLights)
            {
                trafficLight.ResetTrafficLight();
            }
            GlobalNetworkService.intersectionCollisionOccured = false;
            GlobalNetworkService.numberOfAccidents = 0;
            actionCount = 0;*/

        }


        /// <summary>
        /// Get all traffic lights from the network
        /// </summary>
        /// <returns>List of traffic lights</returns>
        private List<TrafficLightController> GetTrafficLights()
        {
            return FindObjectsByType<TrafficLightController>(FindObjectsSortMode.None).ToList();
        }

        /// <summary>
        /// Get all vehicles from the network
        /// </summary>
        /// <returns>List of vehicles</returns>
        private List<AStarAgentController> GetVehicles()
        {
            return FindObjectsByType<AStarAgentController>(FindObjectsSortMode.None).ToList();
        }
    }


    public class TrafficMetricData
    {
        public List<int> passingCount { get; set; } = new List<int>();

        public List<int> pressure { get; set; } = new List<int>();


        public List<int> queueLength { get; set; } = new List<int>();

        public List<float> redTimer { get; set; } = new List<float>();


        public List<float> greenTimer { get; set; } = new List<float>();
    }
}
