using Microsoft.MixedReality.Toolkit.Core.Services;
using Microsoft.MixedReality.Toolkit.Core.Definitions.Utilities;
using UnityEngine;
using UnityEngine.SpatialTracking;

namespace Sereno
{
    public class ClippingPlaneControl : MonoBehaviour
    {
        public  GameObject clippingPlane;
        private GameObject planeObj;
        private TrackedPoseDriver controller;
        private Vector3 panelPosition  = new Vector3(0, 0.039f, 0.05f);

        private void Awake()
        {
            this.controller = GetComponent<TrackedPoseDriver>();
        }

        private void Start()
        {
            //create one status record
            planeObj = GameObject.Instantiate(this.clippingPlane);

            //attach panel to hand
            planeObj.transform.localRotation = Quaternion.identity;
            planeObj.transform.localPosition = this.panelPosition;
            planeObj.transform.Rotate(90, 0, 0);
            planeObj.transform.parent = this.transform;
            planeObj.SetActive(false);
        }

        private void Update()
        {
            if(controller.isActiveAndEnabled)
            {
                foreach(var v in MixedRealityToolkit.InputSystem.DetectedControllers)
                {
                    if(v.ControllerHandedness == Handedness.Left && this.controller.poseSource == TrackedPoseDriver.TrackedPose.LeftPose)
                    {
                        if(Input.GetKey(KeyCode.Joystick1Button2))
                        { 
                            planeObj.SetActive(true);
                            Debug.Log("OK");
                        }
                        else
                            planeObj.SetActive(false);
                        break;
                    }
                }
            }
        }

        public bool IsPlaneActive()
        {
            return this.controller.isActiveAndEnabled;
        }

        public Vector3 GetPlanePosition()
        {
            return this.transform.position;
        }

        public Vector3 GetPlaneNormal()
        {
            return this.transform.up;
        }
    }
}