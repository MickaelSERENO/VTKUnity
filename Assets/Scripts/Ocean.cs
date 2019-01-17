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
        /// The mask values used in the ocean dataset
        /// </summary>
        private VTKValue m_maskValue;

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

        public ClippingPlaneControl ClippingPlane;

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
            //If so, create the structured points !
            if(m_parser.GetDatasetType() == VTKDatasetType.VTK_STRUCTURED_POINTS)
            {
                m_oceanGrid = GameObject.Instantiate(StructuredGrid);
                unsafe
                {
                    //Check if the first attribute is a char. If so, used these as a mask
                    byte* mask = null;
                    List<VTKFieldValue> fieldDesc = m_parser.GetPointFieldValueDescriptors();

                    foreach(var f in fieldDesc)
                    {
                        //TODO use Unity to give that value name (this is the mask value name)
                        if(f.Name == "vtkValidPointMask" && f.Format == VTKValueFormat.VTK_CHAR)
                        {
                            VTKValue      val    = m_parser.ParseAllFieldValues(f);
                            m_maskValue = val;
                            mask=(byte*)(val.Value);
                        }
                    }
                    if(!m_oceanGrid.Init(m_parser, mask))
                        return;
                }

                Vector3 size = m_oceanGrid.MaxMeshPos - m_oceanGrid.MinMeshPos;

                //Generate the small multiples
                m_smallMultiples = new List<VTKUnitySmallMultiple>();
                m_textSMMeshes   = new List<TextMesh>();

                m_smallMultiples.Add(m_oceanGrid.CreatePointFieldSmallMultiple(2));
                m_smallMultiples.Add(m_oceanGrid.CreatePointFieldSmallMultiple(3));
                //m_smallMultiples.Add(m_oceanGrid.CreatePointFieldSmallMultiple(4));

                //Place them correctly and associate the text
                for (int i = 0; i < m_smallMultiples.Count; i++)
                {
                    //Small multiple
                    var c = m_smallMultiples[i];
                    c.transform.parent = this.transform;
                    c.transform.localPosition = new Vector3(2 * i, 0, 0);
                    c.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
                    //c.SphereEnabled = true;
                    //c.PlaneEnabled  = true;
                    c.transform.localRotation = Quaternion.Euler(0.0f, 180.0f, 0.0f);

                    //Text
                    TextMesh smText = Instantiate(SmallMultipleTextProperty);
                    m_textSMMeshes.Add(smText);
                    smText.text = m_oceanGrid.GetPointFieldName((UInt32)i + 2);
                    smText.transform.parent = c.transform;
                    smText.transform.localPosition = new Vector3(0.0f, size.y + 0.1f, 0.0f);
                    smText.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
                    smText.transform.localRotation = new Quaternion(0.0f, 0.0f, 0.0f, 1.0f);
                }
            }
        }

        private void Update()
        {
            string debugMsg = "";

            //>>>>>>>>>>>>>>>>>>>>added by Mao LIN>>>>>>>>>>>>>>>>>>>>>>>>>
            for (int i = 0; i < m_smallMultiples.Count; i++)
            {
                
                VTKUnitySmallMultiple sm = m_smallMultiples[i];
                if(sm.PlaneEnabled && ClippingPlane.IsPlaneActive())
                {
                    sm.PlanePosition = ClippingPlane.GetPlanePosition(); //- sm.transform.position;
                    sm.PlaneNormal = -1*ClippingPlane.GetPlaneNormal();

                    Debug.DrawRay(ClippingPlane.GetPlanePosition(), ClippingPlane.GetPlaneNormal(), Color.green);
                    debugMsg += "SM " + i + ": [Plane] Position=" + sm.PlanePosition + ", Normal=" + sm.PlaneNormal + "\n";
                }
                /*if(sm.SphereEnabled)
                {
                    sm.SpherePosition = SphereControl.Instance.GetSpherePosition();// - sm.transform.position;
                    sm.SphereRadius = 0.1f;

                    debugMsg += "SM " + i + ": [Sphere] Position=" + sm.SpherePosition + ", Radius=" + sm.SphereRadius + "\n";
                }*/
            }

            //GameObject.FindGameObjectWithTag("DebugText").GetComponent<Text>().text = debugMsg;
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
    }
}