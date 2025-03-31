
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using System;
using Assets.Entities;
using static UnityEditorInternal.VersionControl.ListControl;
using Assets.Services;
using static Assets.Entities.TrafficSignal;
using static UnityEngine.UI.Image;
using Unity.Burst.CompilerServices;

namespace Assets.Scripts
{

    /// <summary>
    /// Traffic controller
    /// Control the traffic light itself
    /// </summary>
    public class TrafficLightController : MonoBehaviour
    {
        /// <summary>
        /// Unique identifier
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Road unique identifier
        /// </summary>
        public string RoadId { get; set; }

        /// <summary>
        /// Centrum identifier
        /// </summary>
        public string? IntersectionId { get; set; }

        /// <summary>
        /// Traffic light that is related to this
        /// </summary>
        public TrafficLightController relatedTrafficLight { get; set; }


        #region States

        /// <summary>
        /// Light status enum
        /// </summary>
        public enum LightState { Red, Green, Yellow, None };

        /// <summary>
        /// Current status
        /// </summary>
        public LightState currentState { get; set; }

        /// <summary>
        /// Previous state
        /// </summary>
        public LightState previousState { get; set; }

        #endregion

        /// <summary>
        /// Renderer
        /// </summary>
        private Renderer objRenderer;

        /// <summary>
        /// Position of the traffic light
        /// </summary>
        public Vector3 position { get; set; }

        /// <summary>
        /// Previous node direction to the light
        /// </summary>
        public List<Node> previousNode { get; set; } = new List<Node>();

        #region Metrics 

        /// <summary>
        /// Agent queques
        /// </summary>
        public int queueLength { get; private set; } = 0;

        /// <summary>
        /// Number of passing cars under the light
        /// </summary>
        public int passingCount { get; set; } = 0;

        /// <summary>
        /// Pressure
        /// </summary>
        public int pressure { get; set; } = 0;

        #endregion

        #region Timers - controlled by master

        /// <summary>
        /// Red state time
        /// </summary>
        public float redTimer { get; set; } = 10f;

        /// <summary>
        /// Green state time
        /// </summary>
        public float greenTimer { get; set; } = 20f;

        /// <summary>
        /// Yellow state time
        /// except from control
        /// </summary>
        private const float yellowTimer = 2f;

        /// <summary>
        /// Traffic light timer
        /// except from control
        /// </summary>
        private float timer = 0f;


        #endregion

        private void Awake()
        {
            objRenderer = GetComponent<Renderer>();


            greenTimer = 10f;
            redTimer = 10f;

            int startState = UnityEngine.Random.Range(0, 3);
            currentState = LightState.Red;
            objRenderer.material.color = Color.red;
        }


        private void FixedUpdate()
        {

           
            foreach (var node in previousNode)
            {


                var direction = (new Vector3(node.Position.x, 0.2f, node.Position.z) - new Vector3(transform.position.x, 0.2f, transform.position.z)).normalized;
                var distance = Vector3.Distance(transform.position, node.Position);

                for (int i = 0; i < 16; i++)
                {

                    float angleOffset = Mathf.Lerp(-10 / 2, 10 / 2, i / (float)(16 - 1));
                    Quaternion rotation = Quaternion.Euler(0, angleOffset, 0);
                    Vector3 rayDirection = rotation * direction;


                    var transformedOrigin = new Vector3(transform.position.x, 0.2f, transform.position.z);

                    float multiplier = 1f;
                    if (distance <= 10)
                    {
                        multiplier = 5f;
                    }
                    else if (distance > 10 && distance <= 20)
                    {
                        multiplier = 2.5f;
                    }
                    else
                    {
                        multiplier = 1f;
                    }

                    //Ray ray = new Ray(transformedOrigin, rayDirection * distance * multiplier, LayerMask.GetMask());

                    List<RaycastHit> hits = Physics.RaycastAll(transformedOrigin, rayDirection, distance * multiplier, LayerMask.GetMask("VehicleLayer")).ToList();

                    pressure = hits.Count;

                    int qLength = hits.Where(x =>
                    {

                        var vehicleController = x.collider.gameObject.GetComponent<AStarAgentController>();
                        if (vehicleController != null)
                        {
                            return vehicleController.isStopped || vehicleController.speed < 5f;
                        }
                        return false;

                    }).Count();

                    queueLength = qLength;



                    //Debug.DrawRay(transformedOrigin, rayDirection * distance * multiplier, Color.yellow);
                }

            }


        }

        private void Update()
        {

           
            //Debug.Log($"TF: {Id} passing count: {passingCount}, pressure: {queueLength}");


            HandleStateTransition();
            //DiscreteChange();
        }


        public void SetState(LightState newState)
        {
            if (currentState == newState)
            {
                return;
            }

            if (newState == LightState.Red)
            {
                passingCount = 0;
            }

            currentState = newState;
            /*actionTaken = true;
            prevActionTimer = 0f;*/
           
        }


        void DiscreteChange()
        {
            switch (currentState)
            {
                case LightState.Red:
                    objRenderer.material.color = Color.red;
                    break;

                case LightState.Green:
                    objRenderer.material.color = Color.green;
                    break;

                case LightState.Yellow:
                    objRenderer.material.color = Color.yellow;
                    break;

            }
        }


        void HandleStateTransition()
        {
            timer += Time.deltaTime;
            switch (currentState)
            {
                case LightState.Green:
                    if (timer >= greenTimer)
                    {
                        ChangeState(LightState.Yellow);
                        previousState = LightState.Green;
                        passingCount = 0;

                    }
                    break;

                case LightState.Yellow:
                    if (timer >= yellowTimer)
                    {
                        if (previousState == LightState.Green)
                        {
                            ChangeState(LightState.Red);
                        }
                        else
                        {
                            ChangeState(LightState.Green);
                        }
                    }
                    break;

                case LightState.Red:
                    if (timer >= redTimer)
                    {
                        ChangeState(LightState.Yellow);
                        previousState = LightState.Red;
                    }
                    break;
            }
        }

        /// <summary>
        /// Change traffic light state
        /// </summary>
        /// <param name="newState">New state</param>
        void ChangeState(LightState newState)
        {
            currentState = newState;
            ChangeColor();
            timer = 0f;
        }

        /// <summary>
        /// Change color and state of the traffic light
        /// </summary>
        void ChangeColor()
        {
            switch (currentState)
            {
                case LightState.Green:
                    objRenderer.material.color = Color.green;
                    break;
                case LightState.Yellow:
                    objRenderer.material.color = Color.yellow;
                    break;
                case LightState.Red:
                    objRenderer.material.color = Color.red;
                    break;
            }
        }
        #region State checkers
        /// <summary>
        /// Is the light state red
        /// </summary>
        /// <returns>Red or not</returns>
        public bool IsRed()
        {
            return currentState == LightState.Red;
        }

        /// <summary>
        /// Is the light state green
        /// </summary>
        /// <returns>Green or not</returns>
        public bool IsGreen()
        {
            return currentState == LightState.Green;
        }

        /// <summary>
        /// Is the light state yellow
        /// </summary>
        /// <returns>Yellow or not</returns>
        public bool IsYellow()
        {
            return currentState == LightState.Yellow;
        }



        #endregion

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Vehicle"))
            {

                passingCount++;
            }
        }


        public void ObjectEnteredWaitZone(GameObject other)
        {
            queueLength++;
        }


        public void ObjectExitedWaitZone(GameObject other)
        {
            queueLength--;
        }

        /// <summary>
        /// Reset the traffic light overall state
        /// Counts, light state, timers
        /// </summary>
        public void ResetTrafficLight()
        {
            redTimer = 10f;
            greenTimer = 10f;

            queueLength = 0;
            passingCount = 0;
            
            pressure = 0;

            int startState = UnityEngine.Random.Range(0, 3);
            objRenderer.material.color = Color.red;
            currentState = LightState.Red;
            previousState = LightState.None;

        }


        public int GetPressure()
        {

            int pressure = GlobalNetworkService.GetLanePressure(Id);
            Debug.Log(pressure);
            return pressure;
        }



    }
}
