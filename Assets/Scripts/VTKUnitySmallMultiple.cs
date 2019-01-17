using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

using Sereno;

namespace Sereno
{
    public class VTKUnitySmallMultiple : MonoBehaviour 
    {
        /// <summary>
        /// Material to use
        /// </summary>
        public Material ColorMaterial = null;

        /// <summary>
        /// The Outline gameobject
        /// </summary>
        public GameObject Outline = null;

        /// <summary>
        /// The min-max line gameobject to display where the line is
        /// </summary>
        public GameObject MinMaxLine = null;

        /// <summary>
        /// The min-max label gameobject to display the minimum value
        /// </summary>
        public TextMesh MinMaxLabel = null;

        /// <summary>
        /// The mesh to use
        /// </summary>
        private Mesh m_mesh = null;

        /// <summary>
        /// The outline gameobject created from "Outline"
        /// </summary>
        private GameObject m_outline;

        /// <summary>
        /// The min-max line gameobject created from "MinMaxLine"
        /// </summary>
        private GameObject[] m_minMaxLine = new GameObject[2];

        /// <summary>
        /// The min-max line pivots
        /// </summary>
        private GameObject[] m_minMaxPivots     = new GameObject[2];

        /// <summary>
        /// The min-max label GameObject created from "MinMaxLabel"
        /// </summary>
        private TextMesh[] m_minMaxLabel = new TextMesh[2];

        /// <summary>
        /// The color to use
        /// </summary>
        private Int32 m_colorID = -1;

        /// <summary>
        /// The material in use (copy from the Material setted in the Unity Editor)
        /// </summary>
        private Material m_material;

        /// <summary>
        /// Enable the clipping plane
        /// </summary>
        private bool m_planeEnabled  = false;

        /// <summary>
        /// Enable the clipping sphere
        /// </summary>
        private bool m_sphereEnabled = false;

        //The color needed
        private static readonly LABColor coldColor  = new LABColor(new Color(59.0f / 255.0f, 76.0f / 255.0f, 192.0f / 255.0f, 1.0f));
        private static readonly LABColor warmColor  = new LABColor(new Color(180.0f / 255.0f, 4.0f / 255.0f, 38.0f / 255.0f, 1.0f));
        private static readonly LABColor whiteColor = new LABColor(XYZColor.Reference);

        // Use this for initialization
        private void Start() 
        {
        }
        
        // Update is called once per frame
        private void Update()
        {
            if(m_mesh != null && m_material != null)
            {
                Graphics.DrawMesh(m_mesh, transform.localToWorldMatrix, m_material, 1);
            }
        }

        public unsafe bool InitFromPointField(VTKParser parser, Mesh mesh, Int32 uvID, Int32 valueID, Vector3Int offset, Vector3Int density, byte* mask)
        {
            m_material = new Material(ColorMaterial);

            //Check the ID
            if(uvID > 7)
            {
                Debug.Log("The uvID must be between 0 to 7. This is a limitation of Unity for working within a common mesh...");
                return false;
            }
            m_mesh    = mesh;

            VTKStructuredPoints descPts = parser.GetStructuredPointsDescriptor();

            //Determine the maximum and minimum positions
            Vector3 minModelPos  = new Vector3((float)(-descPts.Size[0]/2.0*descPts.Spacing[0]),
                                               (float)(-descPts.Size[1]/2.0*descPts.Spacing[1]),
                                               (float)(-descPts.Size[2]/2.0*descPts.Spacing[2]));
            Vector3 maxModelPos  = -minModelPos;

            //The value buffer
            List<VTKFieldValue> fieldDesc = parser.GetPointFieldValueDescriptors();
            if(fieldDesc.Count < valueID)
            {
                Debug.Log("No value to display");
                return false;
            }
            VTKValue      val    = parser.ParseAllFieldValues(fieldDesc[valueID]);
            List<Vector3> colors = new List<Vector3>((int)(density.x*density.y*density.z));

            //Determine the minimum and maximum value and their position
            double        max    = double.MinValue;
            double        min    = double.MaxValue;
            Vector3       minLoc = new Vector3();
            Vector3       maxLoc = new Vector3();

            for(UInt32 i = 0; i < val.NbValues; i++)
            {
                if(mask != null && mask[i]==0)
                    continue;
                double v = val.ReadAsDouble(i * fieldDesc[valueID].NbValuesPerTuple);
                if(max < v)
                {
                    max    = v;
                    maxLoc = new Vector3(i%descPts.Size[0], (i/descPts.Size[0])%descPts.Size[1], i/(descPts.Size[0]*descPts.Size[1]));
                }
                if(min > v)
                {
                    min = v;
                    minLoc = new Vector3(i % descPts.Size[0], (i / descPts.Size[0]) % descPts.Size[1], i / (descPts.Size[0] * descPts.Size[1]));
                }
                min = Math.Min(min, val.ReadAsDouble(i*fieldDesc[valueID].NbValuesPerTuple));
            }

            //Normalize the location (between 0.0 and 1.0 for the most "long" axis)
            Vector3[] vec = new Vector3[2] {maxLoc, minLoc};
            Vector3 modelDist = maxModelPos - minModelPos;
            float   maxModelDist = Math.Max(modelDist.x, Math.Max(modelDist.y, modelDist.z));
            for (int i = 0; i < vec.Length; i++)
            {
                Vector3 l = vec[i];
                l = (new Vector3((float)(l.x*descPts.Spacing[0]),
                                 (float)(l.y*descPts.Spacing[1]),
                                 (float)(l.z*descPts.Spacing[2])) + minModelPos)/maxModelDist;
                vec[i] = l;
                Debug.Log($"l : {l}, {l.magnitude}");
            }

            maxLoc = vec[0];
            minLoc = vec[1];
            for(UInt32 k = 0; k < density.z; k++)
            {
                for(UInt32 j = 0; j < density.y; j++)
                {
                    for (UInt32 i = 0; i < density.x; i++)
                    {
                        UInt64 fieldOff = (UInt64)(i*offset.x + j*offset.y + k*offset.z);
                        //Check the mask
                        if(mask != null && *(mask + fieldOff) == 0)
                        {
                            colors.Add(new Vector3(0.0f, 0.0f, 0.0f));
                            continue;
                        }
                        float c = val.ReadAsFloat(fieldOff*fieldDesc[valueID].NbValuesPerTuple);
                        c = (float)((c - min) / max);

                        //LAB color space (warm - cold)
                        Color? col = null;
                        if(c < 0.5)
                            col = LABColor.Lerp(coldColor, whiteColor, 2.0f*c).ToXYZ().ToRGB();
                        else
                            col = LABColor.Lerp(whiteColor, warmColor, 2.0f*(c-0.5f)).ToXYZ().ToRGB();
                        colors.Add(new Vector3(col.Value.r, col.Value.g, col.Value.b));
                    }
                }
            }
            
            //Update the mesh / material
            m_mesh.SetUVs(uvID, colors);
            m_mesh.UploadMeshData(false);
            m_colorID = uvID;
            
            for(int i = 0; i < 8; i++)
                if(i != m_colorID)
                    m_material.DisableKeyword($"TEXCOORD{i}_ON");
            m_material.EnableKeyword($"TEXCOORD{m_colorID}_ON");
            PlaneEnabled  = false;
            SphereEnabled = false;

            //Outline GameObject (cube around the gameobject)
            m_outline = GameObject.Instantiate<GameObject>(Outline);
            m_outline.transform.parent        = this.transform;
            m_outline.transform.localPosition = new Vector3(0.0f, 0.0f, 0.0f);
            m_outline.transform.localScale    = m_mesh.bounds.max - m_mesh.bounds.min;
            m_outline.transform.localRotation = new Quaternion(0.0f, 0.0f, 0.0f, 1.0f);

            //Min-max objects (line -> label)
            double[] minMaxValues = new double[2];
            minMaxValues[0] = min;
            minMaxValues[1] = max;
            for(int i = 0; i < m_minMaxLine.Length; i++)
            {
                //Line
                m_minMaxPivots[i] = new GameObject();
                m_minMaxPivots[i].transform.parent   = this.transform;
                m_minMaxPivots[i].transform.localPosition = vec[i];
                m_minMaxPivots[i].transform.localScale    = new Vector3(1.0f, 1.0f, 1.0f);
                m_minMaxPivots[i].transform.localRotation = Quaternion.LookRotation(vec[i], Vector3.up);

                m_minMaxLine[i] = GameObject.Instantiate<GameObject>(MinMaxLine);
                m_minMaxLine[i].transform.parent  = m_minMaxPivots[i].transform;
                m_minMaxLine[i].transform.localScale    = new Vector3(0.01f, 1.5f-vec[i].magnitude, 0.01f); //Thin and long
                m_minMaxLine[i].transform.localPosition = new Vector3(0.0f, 0.0f, (1.5f - vec[i].magnitude)/2.0f);
                m_minMaxLine[i].transform.localRotation = Quaternion.Euler(90.0f, 0.0f, 0.0f);

                //Label
                m_minMaxLabel[i] = GameObject.Instantiate<TextMesh>(MinMaxLabel);
                m_minMaxLabel[i].text = String.Format("{0:00.00e+0}", minMaxValues[i]);
                m_minMaxLabel[i].transform.parent        = m_minMaxPivots[i].transform;
                m_minMaxLabel[i].transform.localScale    = new Vector3(0.033f, 0.033f, 0.033f);
                m_minMaxLabel[i].transform.localPosition = new Vector3(0.0f, 0.0f, (1.5f - vec[i].magnitude));
                m_minMaxLabel[i].transform.localRotation = Quaternion.Euler(90.0f, 0.0f, 0.0f);
            }
            m_minMaxLine[0].GetComponent<MeshRenderer>().material.color = coldColor.ToXYZ().ToRGB();
            m_minMaxLine[1].GetComponent<MeshRenderer>().material.color = warmColor.ToXYZ().ToRGB();
            return true;
        }

        private void LateUpdate()
        {
            Vector3 camPos = Camera.main.transform.position;
            foreach(var t in m_minMaxLabel)
            {
                t.transform.LookAt(new Vector3(camPos.x, t.transform.position.y, camPos.z));
                t.transform.Rotate(new Vector3(0.0f, 1.0f, 0.0f), 180);
            }
        }

        private void OnDestroy()
        {
            //Destroy created gameobjects
            foreach(var c in m_minMaxLine)
                GameObject.Destroy(c);
            foreach(var c in m_minMaxPivots)
                GameObject.Destroy(c);
        }

        /// <summary>
        /// Should we enable the clipping plane ? 
        /// </summary>
        public Boolean PlaneEnabled
        {
            get {return m_planeEnabled;}
            set
            {
                m_planeEnabled = value;
                if (value)
                    m_material.EnableKeyword("PLANE_ON");
                else
                    m_material.DisableKeyword("PLANE_ON");
            }
        }

        /// <summary>
        /// Should we enable the clipping sphere ?
        /// </summary>
        public Boolean SphereEnabled
        {
            get { return m_sphereEnabled; }
            set
            {
                m_sphereEnabled = value;
                if (value)
                    m_material.EnableKeyword("SPHERE_ON");
                else
                    m_material.DisableKeyword("SPHERE_ON");
            }
        }

        /// <summary>
        /// The clipping sphere radius
        /// </summary>
        public float SphereRadius
        {
            get { return m_material.GetFloat("_SphereRadius");}
            set { m_material.SetFloat("_SphereRadius", value);}
        }

        /// <summary>
        /// The clipping sphere position
        /// </summary>
        public Vector3 SpherePosition
        {
            get { return m_material.GetVector("_SpherePosition"); }
            set { m_material.SetVector("_SpherePosition", value); }
        }

        /// <summary>
        /// The clipping plane position
        /// </summary>
        public Vector3 PlanePosition
        {
            get { return m_material.GetVector("_PlanePosition"); }
            set { m_material.SetVector("_PlanePosition", value); }
        }

        /// <summary>
        /// The clipping plane normal
        /// </summary>
        public Vector3 PlaneNormal
        {
            get { return m_material.GetVector("_PlaneNormal"); }
            set { m_material.SetVector("_PlaneNormal", value); }
        }

        /// <summary>
        /// The UVID in use in the current mesh.
        /// </summary>
        public Int32 UVID
        {
            get { return m_colorID; }
        }
    }
}