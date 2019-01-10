using HoloToolkit.Unity;
using HoloToolkit.Unity.InputModule;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.WSA.Input;

namespace Sereno
{
    public class SphereControl : Singleton<SphereControl>, IControllerInputHandler
    {
        private IPointingSource currentPointingSource;
        private uint currentSourceId;
        private Vector3 spherePosition;

        public void OnInputPositionChanged(InputPositionEventData eventData)
        {
            //Debug.Log(">>>sourceID=" + eventData.SourceId + ", IsRight=" + IsFromRightController(eventData));
            if (this.IsFromRightController(eventData)) //to avoid multi-controller conflict
            {
                if (FocusManager.Instance.TryGetPointingSource(eventData, out currentPointingSource))
                {
                    this.currentSourceId = eventData.SourceId;
                }
            }
        }

        private bool IsFromRightController(InputPositionEventData eventData)
        {
            InteractionInputSource inputSource = (InteractionInputSource)eventData.InputSource;
            if (inputSource != null)
            {
                InteractionSourceHandedness handedness = inputSource.GetHandedness(eventData.SourceId);
                return (handedness == InteractionSourceHandedness.Right);
            }
            return false;
        }

        // Use this for initialization
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            string debugMsg;
            if (this.currentPointingSource != null)
            {
                FocusDetails focusDetails = FocusManager.Instance.GetFocusDetails(currentPointingSource);
                Vector3 hitPosition = focusDetails.Point;
                this.spherePosition = hitPosition;

                debugMsg = "position=" + hitPosition;
            }
            else
            {
                debugMsg = "currentPointingSource=null";
            }
            //GameObject.FindGameObjectWithTag("DebugText").GetComponent<Text>().text = debugMsg;
        }

        public Vector3 GetSpherePosition()
        {
            return this.spherePosition;
        }
    }
}

    












