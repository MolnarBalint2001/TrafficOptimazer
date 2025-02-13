
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using System;
using Assets.Entities;
using static UnityEditorInternal.VersionControl.ListControl;

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
        public Guid Id { get; set; } = Guid.NewGuid();

        #region States

        /// <summary>
        /// Light status enum
        /// </summary>
        public enum LightState { Red, Green, Yellow, None};

        /// <summary>
        /// Current status
        /// </summary>
        public LightState currentState { get; set; } = LightState.Green;

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
        /// Queue scope threshold
        /// </summary>
        private const float queueScope = 20f;

        /// <summary>
        /// Previous node direction to the light
        /// </summary>
        public Node previousNode { get; set; }


        #region Metrics 

        /// <summary>
        /// Agent queques
        /// </summary>
        public int queueLength { get; set; } = 0;

        /// <summary>
        /// Number of passing cars under the light
        /// </summary>
        public int passingCount { get; set; } = 0;

        #endregion

        #region Timers - controlled by master

        /// <summary>
        /// Red state time
        /// </summary>
        public float redTimer = 3f;

        /// <summary>
        /// Green state time
        /// </summary>
        public float greenTimer = 5f;

        /// <summary>
        /// Yellow state time
        /// except from control
        /// </summary>
        private const float yellowTimer = 3f;

        /// <summary>
        /// Traffic light timer
        /// except from control
        /// </summary>
        private float timer = 0f;

        #endregion

        private void Start()
        {
            objRenderer = GetComponent<Renderer>();
            objRenderer.material.color = Color.green;
        }

        private void Update()
        {
            HandleStateTransition();
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

      
        public void SetPosition(Vector3 pos)
        {
            position = pos;
        }


        public void SetTimer(float t)
        {
            timer = t;
        }


        public void SetQueueLength(int length)
        {
            queueLength = length;
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
            if (other.gameObject.CompareTag("Vehicle"))
            {
                passingCount++;
            }
        }


        public void ObjectEnteredWaitZone(GameObject other)
        {
            //Debug.Log($"{other.name} entered to the wait zone.");
            queueLength++;
            
        }


        public void ObjectExitedWaitZone(GameObject other)
        {
            //Debug.Log($"{other.name} exited from the wait zone.");
            queueLength--;
        }


        public void ResetTrafficLight()
        {
            redTimer = 10f;
            greenTimer = 10f;

            queueLength = 0;
            passingCount = 0;

            currentState = LightState.Green;
            previousState = LightState.None;

        }



    }
}
