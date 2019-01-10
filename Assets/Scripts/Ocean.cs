using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.UI;

namespace Sereno
{
    /// <summary>
    /// The Ocean game object coordinating everything
    /// </summary>
    public class Ocean : MonoBehaviour
    {
        /// <summary>
        /// The Parser
        /// </summary>
        private VTKParser              m_parser;

        /// <summary>
        /// The Ocean grid structured grid
        /// </summary>
        private VTKUnityStructuredGrid m_oceanGrid;

        /// <summary>
        /// The small multiples created
        /// </summary>
        private List<VTKUnitySmallMultiple> m_smallMultiples;

        /// <summary>
        /// The small multiple text meshes created
        /// </summary>
        private List<TextMesh> m_textSMMeshes;

        /// <summary>
        /// The StructuredGrid to use
        /// </summary>
        public VTKUnityStructuredGrid StructuredGrid;

        /// <summary>
        /// The Small multiple text game object to use
        /// </summary>
        public TextMesh SmallMultipleTextProperty; 

        /// <summary>
        /// The Ocean file path to use
        /// </summary>
        public string FilePath;

        public GameObject MotionControllers;
        private ClippingPlaneControl cpControl;

        void Start()
        {
            //Parse the VTK object
            m_parser = new VTKParser($"{Application.streamingAssetsPath}/{FilePath}");
            if(!m_parser.Parse())
            {
                Debug.Log("Error at parsing the ocean dataset");
                return;
            }

            //Check if the type is structured points
            if(m_parser.GetDatasetType() == VTKDatasetType.VTK_STRUCTURED_POINTS)
            {
                m_oceanGrid = GameObject.Instantiate(StructuredGrid);
                if(!m_oceanGrid.Init(m_parser))
                    return;

                Vector3 size = m_oceanGrid.MaxMeshPos - m_oceanGrid.MinMeshPos;

                //Generate the small multiples
                m_smallMultiples = new List<VTKUnitySmallMultiple>();
                m_textSMMeshes   = new List<TextMesh>();

                m_smallMultiples.Add(m_oceanGrid.CreatePointFieldSmallMultiple(2));
                m_smallMultiples.Add(m_oceanGrid.CreatePointFieldSmallMultiple(3));
                m_smallMultiples.Add(m_oceanGrid.CreatePointFieldSmallMultiple(4));

                //Place them correctly and associate the text
                for (int i = 0; i < m_smallMultiples.Count; i++)
                {
                    //Small multiple
                    var c = m_smallMultiples[i];
                    c.transform.parent = this.transform;
                    c.transform.localPosition = new Vector3(2 * i, 0, 0);
                    c.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
                    //c.SphereEnabled = true;
                    c.PlaneEnabled  = true;
                    //c.transform.Rotate(new Vector3(0.0f, 1.0f, 0.0f), 90);

                    //Text
                    TextMesh smText = Instantiate(SmallMultipleTextProperty);
                    m_textSMMeshes.Add(smText);
                    smText.text = m_oceanGrid.GetPointFieldName((UInt32)i + 2);
                    smText.transform.parent = c.transform;
                    smText.transform.localPosition = new Vector3(0.0f, size.y + 0.5f, 0.0f);
                    smText.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
                    smText.transform.localRotation = new Quaternion(0.0f, 0.0f, 0.0f, 1.0f);
                }
            }

            //>>>>>>>>>>>>>>>>>>>>added by Mao LIN>>>>>>>>>>>>>>>>>>>>>>>>>
            cpControl = this.MotionControllers.GetComponent<ClippingPlaneControl>();
            //<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<
        }

        private void Update()
        {
            string debugMsg = "";

            //>>>>>>>>>>>>>>>>>>>>added by Mao LIN>>>>>>>>>>>>>>>>>>>>>>>>>
            for (int i = 0; i < m_smallMultiples.Count; i++)
            {
                
                VTKUnitySmallMultiple sm = m_smallMultiples[i];
                if(sm.PlaneEnabled && cpControl.IsPlaneActive())
                {
                    sm.PlanePosition = cpControl.GetPlanePosition(); //- sm.transform.position;
                    sm.PlaneNormal = -1*cpControl.GetPlaneNormal();

                    Debug.DrawRay(cpControl.GetPlanePosition(), cpControl.GetPlaneNormal(), Color.green);
                    debugMsg += "SM " + i + ": [Plane] Position=" + sm.PlanePosition + ", Normal=" + sm.PlaneNormal + "\n";
                }
                if(sm.SphereEnabled)
                {
                    sm.SpherePosition = SphereControl.Instance.GetSpherePosition();// - sm.transform.position;
                    sm.SphereRadius = 0.1f;

                    debugMsg += "SM " + i + ": [Sphere] Position=" + sm.SpherePosition + ", Radius=" + sm.SphereRadius + "\n";
                }
            }

            GameObject.FindGameObjectWithTag("DebugText").GetComponent<Text>().text = debugMsg;
            //<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<
        }

        void LateUpdate()
        {
            //Make text facing the camera
            Vector3 camPos = Camera.main.transform.position;
            foreach (var t in m_textSMMeshes)
            {
                t.transform.LookAt(new Vector3(camPos.x, t.transform.position.y, camPos.z));
                t.transform.Rotate(new Vector3(0.0f, 1.0f, 0.0f), 180);
            }
        }

        private void OnDestroy()
        {
            GameObject.Destroy(m_oceanGrid);
        }

        public void onPlaneButton(bool pressed)
        {
            foreach (var c in m_smallMultiples)
                c.PlaneEnabled = pressed;
        }

        public void onSphereButton(bool pressed)
        {
            foreach (var c in m_smallMultiples)
                c.SphereEnabled = pressed;
        }
    }
}