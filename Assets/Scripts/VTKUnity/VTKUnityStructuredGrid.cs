﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Runtime.InteropServices;


namespace Sereno
{
    public class VTKUnityStructuredGrid : MonoBehaviour 
    {
        /// <summary>
        /// The VTKParser of this dataset
        /// </summary>
        private VTKParser m_parser;

        /// <summary>
        /// The Mesh representing the VTK object
        /// </summary>
        private Mesh m_mesh;

        /// <summary>
        /// The list of the small multiples
        /// </summary>
        private List<VTKUnitySmallMultiple> m_smallMultiples;

        /// <summary>
        /// The UV IDs available
        /// </summary>
        private List<Int32> m_availableUVIDs;

        /// <summary>
        /// The mask in use
        /// </summary>
        private unsafe byte* m_mask;

        /// <summary>
        /// The minimum position of the mesh
        /// </summary>
        private Vector3 m_minPos;

        /// <summary>
        /// The maximum position of the mesh
        /// </summary>
        private Vector3 m_maxPos;

        /// <summary>
        /// The small multiple prefab to represent sub data.
        /// </summary>
        public VTKUnitySmallMultiple SmallMultiplePrefab;

        /// <summary>
        /// The desired density
        /// </summary>
        public UInt32 DesiredDensity;

        void Start()
        {

        }

        /// <summary>
        /// Initialize the StructuredGrid representation
        /// </summary>
        /// <param name="parser">The VKTParser to use.
        /// It should not be closed while this object is intented to being modified (e.g adding small multiples)</param>
        /// <param name="mask">The mask to apply point per point. if mask==null, we do not use it</param>
        /// <returns></returns>
        public unsafe bool Init(VTKParser parser, byte* mask=null)
        {
            m_parser = parser;
            m_mask   = mask;

            if(m_parser.GetDatasetType() != VTKDatasetType.VTK_STRUCTURED_POINTS)
            {
                Debug.Log("Error: The dataset should be a structured points dataset");
                return false;
            }

            //Get the points and modify the points / normals buffer
            VTKStructuredPoints ptsDesc = m_parser.GetStructuredPointsDescriptor();
            Vector3Int          density = Density;
            Vector3[]           pts     = new Vector3[density.x*density.y*density.z];

            //Determine the positions for "normalizing" the object (the biggest axis of the object at "size = 1")
            Vector3             minPos  = new Vector3((float)((density.x / 2) * ptsDesc.Size[0] / density.x * ptsDesc.Spacing[0]),
                                                      (float)((density.y / 2) * ptsDesc.Size[1] / density.y * ptsDesc.Spacing[1]),
                                                      (float)((density.z / 2) * ptsDesc.Size[2] / density.z * ptsDesc.Spacing[2]));
            Vector3             maxPos  = new Vector3((float)((density.x-1+density.x / 2) * ptsDesc.Size[0] / density.x * ptsDesc.Spacing[0]),
                                                      (float)((density.y-1+density.y / 2) * ptsDesc.Size[1] / density.y * ptsDesc.Spacing[1]),
                                                      (float)((density.z-1+density.z / 2) * ptsDesc.Size[2] / density.z * ptsDesc.Spacing[2]));

            float               maxAxis = Math.Max(maxPos.x-minPos.x, Math.Max(maxPos.y-minPos.y, maxPos.z-minPos.z));

            for(int k = 0; k < density.z; k++)
                for(int j = 0; j < density.y; j++)
                    for(int i = 0; i < density.x; i++)
                        pts[i+density.x*j+density.x*density.y*k] = new Vector3((float)((i-density.x/2)*ptsDesc.Size[0]/density.x*ptsDesc.Spacing[0])/maxAxis, 
                                                                               (float)((j-density.y/2)*ptsDesc.Size[1]/density.y*ptsDesc.Spacing[1])/maxAxis,
                                                                               (float)((k-density.z/2)*ptsDesc.Size[2]/density.z*ptsDesc.Spacing[2])/maxAxis);

            //Store the maximas positions
            m_minPos = pts[0];
            m_maxPos = pts[pts.Length-1];

            //The element buffer
            Vector3Int offsetMask = new Vector3Int((int)(ptsDesc.Size[0]/density.x),
                                                   (int)(ptsDesc.Size[1]/density.y*ptsDesc.Size[0]),
                                                   (int)(ptsDesc.Size[2]/density.z*ptsDesc.Size[1]*ptsDesc.Size[0]));
            int[] triangles = new int[(density.x-1)*(density.y-1)*(density.z-1)*36];
            for(int k = 0; k < density.z-1; k++)
            {
                for(int j = 0; j < density.y-1; j++)
                {
                    for(int i = 0; i < density.x-1; i++)
                    {
                        int offset = (i + j*(density.x-1) + k*(density.x-1)*(density.y-1))*36;
                        //Test the mask
                        //If we are encountering a missing mask, put the triangles to to point "0" (i.e empty triangle) 
                        //And go at the end of the loop
                        if(mask != null)
                            for(int k2=0; k2<2; k2++)
                                for(int j2=0; j2<2; j2++)
                                    for(int i2=0; i2<2; i2++)
                                        if(*(mask + (i+i2)*offsetMask.x + (j+j2)*offsetMask.y + (k+k2)*offsetMask.z) == 0)
                                        {
                                            for(int t=0; t<36; t++)
                                                triangles[offset+t]=0;
                                            goto endTriangleLoop;
                                        }

                        //Front
                        triangles[offset   ] = i+1 + j    *density.x + k   *density.x*density.y;
                        triangles[offset+1 ] = i   + j    *density.x + k   *density.x*density.y;
                        triangles[offset+2 ] = i+1 + (j+1)*density.x + k   *density.x*density.y;
                        triangles[offset+3 ] = i+1 + (j+1)*density.x + k   *density.x*density.y;
                        triangles[offset+4 ] = i   + j    *density.x + k   *density.x*density.y;
                        triangles[offset+5 ] = i   + (j+1)*density.x + k   *density.x*density.y;

                        //Back
                        triangles[offset+6 ] = i+1 + (j+1)*density.x + (k+1)*density.x*density.y;
                        triangles[offset+7 ] = i   + j    *density.x + (k+1)*density.x*density.y;
                        triangles[offset+8 ] = i+1 + j    *density.x + (k+1)*density.x*density.y;
                        triangles[offset+9 ] = i   + (j+1)*density.x + (k+1)*density.x*density.y;
                        triangles[offset+10] = i   + j    *density.x + (k+1)*density.x*density.y;
                        triangles[offset+11] = i+1 + (j+1)*density.x + (k+1)*density.x*density.y;

                        //Left
                        triangles[offset+12] = i + j    *density.x + k    *density.x*density.y;
                        triangles[offset+13] = i + j    *density.x + (k+1)*density.x*density.y;
                        triangles[offset+14] = i + (j+1)*density.x + (k+1)*density.x*density.y;
                        triangles[offset+15] = i + (j+1)*density.x + k    *density.x*density.y;
                        triangles[offset+16] = i + j    *density.x + k    *density.x*density.y;
                        triangles[offset+17] = i + (j+1)*density.x + (k+1)*density.x*density.y;

                        //Right
                        triangles[offset+18] = (i+1) + j    *density.x + (k+1)*density.x*density.y;
                        triangles[offset+19] = (i+1) + j    *density.x + k    *density.x*density.y;
                        triangles[offset+20] = (i+1) + (j+1)*density.x + (k+1)*density.x*density.y;
                        triangles[offset+21] = (i+1) + j    *density.x + k    *density.x*density.y;
                        triangles[offset+22] = (i+1) + (j+1)*density.x + k    *density.x*density.y;
                        triangles[offset+23] = (i+1) + (j+1)*density.x + (k+1)*density.x*density.y;

                        //Top
                        triangles[offset+24] = (i+1) + (j+1)*density.x + k    *density.x*density.y;
                        triangles[offset+25] = i     + (j+1)*density.x + k    *density.x*density.y;
                        triangles[offset+26] = i     + (j+1)*density.x + (k+1)*density.x*density.y;
                        triangles[offset+27] = (i+1) + (j+1)*density.x + (k+1)*density.x*density.y;
                        triangles[offset+28] = (i+1) + (j+1)*density.x + k    *density.x*density.y;
                        triangles[offset+29] = i     + (j+1)*density.x + (k+1)*density.x*density.y;

                        //Bottom
                        triangles[offset+30] = (i+1) + j*density.x + k    *density.x*density.y;
                        triangles[offset+31] = i     + j*density.x + (k+1)*density.x*density.y;
                        triangles[offset+32] = i     + j*density.x + k    *density.x*density.y;
                        triangles[offset+33] = (i+1) + j*density.x + k    *density.x*density.y;
                        triangles[offset+34] = (i+1) + j*density.x + (k+1)*density.x*density.y;
                        triangles[offset+35] = i     + j*density.x + (k+1)*density.x*density.y;

                        //End the the triangle loop
                    endTriangleLoop:
                        continue;
                    }
                }
            }

            //Create the mesh
            m_mesh = new Mesh();
            m_mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            m_mesh.vertices    = pts;
            m_mesh.triangles   = triangles;
            m_mesh.UploadMeshData(false);

            //Small multiples array
            m_smallMultiples = new List<VTKUnitySmallMultiple>();
            m_availableUVIDs = new List<Int32>();

            for(Int32 i = 0; i < 8; i++)
                m_availableUVIDs.Add(i);

            return true;
        }
        
        // Update is called once per frame
        void Update() 
        {
            
        }

        /// <summary>
        /// Create a small multiple object
        /// </summary>
        /// <param name="dataID">The parameter ID to use. Use Parser.GetPointFieldValueDescriptors(); to get the field ID</param>
        /// <returns>A VTKUnitySmallMultiple object.</returns>
        public VTKUnitySmallMultiple CreatePointFieldSmallMultiple(Int32 dataID)
        {
            VTKStructuredPoints   ptsDesc = m_parser.GetStructuredPointsDescriptor();
            VTKUnitySmallMultiple sm      = GameObject.Instantiate(SmallMultiplePrefab);
            Vector3Int            density = Density;

            unsafe
            {
                if(sm.InitFromPointField(m_parser, m_mesh, m_availableUVIDs[m_availableUVIDs.Count-1], dataID, 
                                         new Vector3Int((int)(ptsDesc.Size[0]/density.x), 
                                                        (int)(ptsDesc.Size[1]/density.y*ptsDesc.Size[0]),
                                                        (int)(ptsDesc.Size[2]/density.z*ptsDesc.Size[1]*ptsDesc.Size[0])),
                                         density, m_mask))
                {
                    m_smallMultiples.Add(sm);
                    m_availableUVIDs.RemoveAt(m_availableUVIDs.Count-1);
                    return sm;
                }
            }

            Destroy(sm);
            return null;
        }

        /// <summary>
        /// Remove a point field small multiple
        /// </summary>
        /// <param name="sm">The Small multiple to remove</param>
        public void RemovePointFieldSmallMultiple(VTKUnitySmallMultiple sm)
        {
            //Search for the small multiples in our array
            int id = -1;
            for(int i = 0; i < m_smallMultiples.Count; i++)
            {
                if(m_smallMultiples[i] == sm)
                {
                    id = i;
                    break;
                }
            }

            //If found, mark the UVID as available and reset the mesh.
            if(id >= 0)
            {
                m_availableUVIDs.Add(sm.UVID);
                m_smallMultiples.RemoveAt(id);

                //Free graphical memory
                if(m_mesh.isReadable)
                {
                    m_mesh.SetUVs(sm.UVID, new List<Vector3>());
                    m_mesh.UploadMeshData(false);
                }

                Destroy(sm);
            }
        }

        /// <summary>
        /// Get the point field name at ID = dataID
        /// </summary>
        /// <param name="dataID">The dataID</param>
        /// <returns>The point field name ID #dataID</returns>
        public string GetPointFieldName(UInt32 dataID)
        {
            List<VTKFieldValue> l = m_parser.GetPointFieldValueDescriptors();
            if(l.Count <= dataID)
                return null;
            return l[(int)dataID].Name;
        }

        /// <summary>
        /// Upload the mesh data to the GPU and remove the CPU double buffered memory allocated
        /// It permits to gain the CPU memory allocation
        /// </summary>
        public void UploadMeshData()
        {
            m_mesh.UploadMeshData(true);
        }

        /// <summary>
        /// Get the size diviser used for the displayability of the Ocean dataset (structured grid)
        /// </summary>
        /// <returns>The field diviser applied along all axis</returns>
        public UInt32 GetFieldSizeDiviser()
        {
            VTKStructuredPoints ptsDesc = m_parser.GetStructuredPointsDescriptor();
            UInt32 x = (ptsDesc.Size[0] + DesiredDensity - 1) / DesiredDensity;
            UInt32 y = (ptsDesc.Size[1] + DesiredDensity - 1) / DesiredDensity;
            UInt32 z = (ptsDesc.Size[2] + DesiredDensity - 1) / DesiredDensity;

            return Math.Max(Math.Max(x, y), z);
        }

        /// <summary>
        /// get the displayable size of the vector field. Indeed, due to hardware limitation, we cannot display all the vector field at once
        /// </summary>
        /// <returns>The vector field size displayable</returns>
        public Vector3Int GetDisplayableSize()
        {
            VTKStructuredPoints ptsDesc = m_parser.GetStructuredPointsDescriptor();
            if (ptsDesc.Size[0] == 0 || ptsDesc.Size[1] == 0 || ptsDesc.Size[2] == 0)
                return new Vector3Int(0, 0, 0);

            int maxRatio = (int)GetFieldSizeDiviser();
            return new Vector3Int((int)ptsDesc.Size[0] / maxRatio, (int)ptsDesc.Size[1] / maxRatio, (int)ptsDesc.Size[2] / maxRatio);
        }

        /// <summary>
        /// The density used
        /// </summary>
        public Vector3Int Density
        {
            get{ return GetDisplayableSize(); }
        }

        /// <summary>
        /// The VTKParser in use
        /// </summary>
        public VTKParser Parser
        {
            get{ return m_parser;}
        }

        /// <summary>
        /// Get the minimum mesh position
        /// </summary>
        public Vector3 MinMeshPos
        {
            get{ return m_minPos;}
        }

        /// <summary>
        /// Get the maximum mesh position
        /// </summary>
        public Vector3 MaxMeshPos
        {
            get{ return m_maxPos;}
        }
    }
}