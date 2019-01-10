using HoloToolkit.Unity.InputModule;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.WSA.Input;


namespace Sereno
{
    public class ClippingPlaneControl : MonoBehaviour
    {
        public GameObject clippingPlane;
        private Dictionary<string, MotionControllerInfo> controllerDic;
        private int lastControllerNum;

        private Dictionary<string, PanelStatus> controllerPanelStatusDic = new Dictionary<string, PanelStatus>();
        private Vector3 panelPosition = new Vector3(0, 0.039f, 0.05f);

        private string activePanelControllerKey = null;


        /// <summary>
        /// For storing the GameObject and status of each panel
        /// </summary>
        class PanelStatus
        {
            public GameObject panelObj;
            public bool isOpen;
            public bool lastStatus;

            public PanelStatus(GameObject panelObj, bool isOpen)
            {
                this.panelObj = panelObj;
                this.isOpen = isOpen;
                this.lastStatus = false;
            }
        };

        private void Awake()
        {
            InteractionManager.InteractionSourceUpdated += MenuInteractionHandler;
        }

        // Use this for initialization
        private void Start()
        {
            this.controllerDic = gameObject.GetComponent<MotionControllerVisualizer>().controllerDictionary;
            this.lastControllerNum = controllerDic.Count;
        }

        private void MenuInteractionHandler(InteractionSourceUpdatedEventArgs data)
        {
            string key = GetControllerKey(data);

            if (this.controllerPanelStatusDic.ContainsKey(key))
            {
                if (key.EndsWith("Left"))
                {
                    PanelStatus panelStatus = this.controllerPanelStatusDic[key];
                    if (data.state.menuPressed)
                    {
                        this.activePanelControllerKey = key;
                        panelStatus.isOpen = true;
                        panelStatus.panelObj.SetActive(true);
                    }
                    else
                    {
                        this.activePanelControllerKey = null;
                        panelStatus.isOpen = false;
                        panelStatus.panelObj.SetActive(false);
                    }
                }
            }
        }

        private string GetControllerKey(InteractionSourceUpdatedEventArgs data)
        {
            return data.state.source.vendorId + "/" + data.state.source.productId + "/" + data.state.source.productVersion + "/" + data.state.source.handedness;
        }

        private void Update()
        {
            if (this.IsNewControllerFound())
            {
                foreach (KeyValuePair<string, MotionControllerInfo> kv in this.controllerDic)
                {
                    Debug.Log("controller count=" + controllerDic.Count + ", id=" + kv.Key + ", value=" + kv.Value.ControllerParent.name + ", id?=" + kv.Value.ControllerParent.GetInstanceID());

                    if (!this.controllerPanelStatusDic.ContainsKey(kv.Key))
                    {
                        //create one status record
                        GameObject panelObj = GameObject.Instantiate(this.clippingPlane);
                        PanelStatus panelStatus = new PanelStatus(panelObj, false);

                        //attach panel to hand
                        panelObj.transform.parent = kv.Value.ControllerParent.transform;
                        panelObj.transform.localRotation = Quaternion.identity;
                        panelObj.transform.localPosition = this.panelPosition;
                        panelObj.transform.Rotate(90, 0, 0);
                        panelObj.SetActive(false);

                        //register
                        this.controllerPanelStatusDic.Add(kv.Key, panelStatus);
                    }
                }  
            }
        }

        public bool IsPlaneActive()
        {
            return this.activePanelControllerKey != null;
        }

        public Vector3 GetPlaneNormal()
        {
            return this.controllerDic[this.activePanelControllerKey].ControllerParent.transform.up;
        }

        public Vector3 GetPlanePosition()
        {
            return this.controllerPanelStatusDic[this.activePanelControllerKey].panelObj.transform.position;
        }

        private bool IsNewControllerFound()
        {
            if (this.controllerDic.Count != this.lastControllerNum)
            {
                this.lastControllerNum = this.controllerDic.Count;
                return true;
            }
            else
                return false;
        }

    }

}
