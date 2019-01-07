﻿using System.Collections;
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
        /// The mesh to use
        /// </summary>
        private Mesh m_mesh = null;

        /// <summary>
        /// The color to use
        /// </summary>
        private Int32 m_colorID = -1;

        /// <summary>
        /// The UVID in use
        /// </summary>
        private Int32 m_uvID = -1;

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
        void Start() 
        {
        }
        
        // Update is called once per frame
        void Update()
        {
            if(m_mesh != null && ColorMaterial != null)
            {
                Graphics.DrawMesh(m_mesh, transform.localToWorldMatrix, ColorMaterial, 1);
            }
        }

        public bool InitFromPointField(VTKParser parser, Mesh mesh, Int32 uvID, Int32 valueID, Vector3Int offset, Vector3Int density)
        {
            m_uvID    = uvID;

            //Check the ID
            if(uvID > 7)
            {
                Debug.Log("The uvID must be between 0 to 7. This is a limitation of Unity for working within a common mesh...");
                return false;
            }
            m_mesh    = mesh;

            //The value buffer
            List<VTKFieldValue> fieldDesc = parser.GetPointFieldValueDescriptors();
            if(fieldDesc.Count < valueID)
            {
                Debug.Log("No value to display");
                return false;
            }
            VTKValue      val    = parser.ParseAllFieldValues(fieldDesc[valueID]);
            List<Vector3> colors = new List<Vector3>((int)(density.x*density.y*density.z));
            double        max    = double.MinValue;
            double        min    = double.MaxValue;

            for(UInt32 i = 0; i < val.NbValues; i++)
            {
                max = Math.Max(max, val.ReadAsDouble(i*fieldDesc[valueID].NbValuesPerTuple));
                min = Math.Min(min, val.ReadAsDouble(i*fieldDesc[valueID].NbValuesPerTuple));
            }
        
            Debug.Log($"Value Name : {fieldDesc[valueID].Name} Min : {min}, Max : {max}");
            for(UInt32 i = 0; i < density.x; i++)
            {
                for(UInt32 j = 0; j < density.y; j++)
                {
                    for(UInt32 k = 0; k < density.z; k++)
                    {
                        UInt64 fieldOff = (UInt64)(i*offset.x + j*offset.y + k*offset.z);
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
            
            m_mesh.SetUVs(uvID, colors);
            m_mesh.UploadMeshData(false);
            m_colorID = uvID;

            
            for(int i = 0; i < 9; i++)
                if(i != m_colorID)
                    ColorMaterial.DisableKeyword($"TEXCOORD{i}_ON");
            ColorMaterial.EnableKeyword($"TEXCOORD{m_colorID}_ON");
            PlaneEnabled  = false;
            SphereEnabled = true;
            return true;
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
                    ColorMaterial.EnableKeyword("PLANE_ON");
                else
                    ColorMaterial.DisableKeyword("PLANE_ON");
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
                    ColorMaterial.EnableKeyword("SPHERE_ON");
                else
                    ColorMaterial.DisableKeyword("SPHERE_ON");
            }
        }

        /// <summary>
        /// The UVID in use in the current mesh.
        /// </summary>
        public Int32 UVID
        {
            get { return m_uvID; }
        }
    }
}