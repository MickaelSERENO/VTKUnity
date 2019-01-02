using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using sereno;
using System;
using System.Runtime.InteropServices;


namespace sereno
{
    public class VTKObjectUnity : MonoBehaviour 
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
        /// The small multiple prefab to represent sub data.
        /// </summary>
        public VTKUnitySmallMultiple SmallMultiplePrefab;

        /// <summary>
        /// The desired density
        /// </summary>
        public UInt32 DesiredDensity;

        /// <summary>
        /// The file path
        /// </summary>
        public string FilePath;

        void Start() 
        {
            //Parse
            m_parser = new VTKParser(FilePath);
            if(!m_parser.Parse())
            {
                Debug.Log("Error at parsing the dataset");
                return;
            }

            if(m_parser.GetDatasetType() != VTKDatasetType.VTK_STRUCTURED_POINTS)
            {
                Debug.Log("Error: The dataset should be a structured points dataset");
                return;
            }

            //Get the points and modify the points / normals buffer
            VTKStructuredPoints ptsDesc = m_parser.GetStructuredPointsDescriptor();
            Vector3Int          density = Density;
            Vector3[]           pts     = new Vector3[density.x*density.y*density.z];

            for(int i = 0; i < density.x; i++)
                for(int j = 0; j < density.y; j++)
                    for(int k = 0; k < density.z; k++)
                        pts[i+density.x*j+density.x*density.y*k] = new Vector3((float)((i-density.x/2)*ptsDesc.Size[0]/density.x*ptsDesc.Spacing[0]), 
                                                                               (float)((j-density.y/2)*ptsDesc.Size[1]/density.y*ptsDesc.Spacing[1]),
                                                                               (float)((k-density.z/2)*ptsDesc.Size[2]/density.z*ptsDesc.Spacing[2]));

            //The element buffer
            int[] triangles = new int[(density.x-1)*(density.y-1)*(density.z-1)*36];
            for(int i = 0; i < density.x-1; i++)
            {
                for(int j = 0; j < density.y-1; j++)
                {
                    for(int k = 0; k < density.z-1; k++)
                    {
                        int offset = (i + j*(density.x-1) + k*(density.x-1)*(density.y-1))*36;

                        //Front
                        triangles[offset   ] = i   + j    *density.x + k   *density.x*density.y;
                        triangles[offset+1 ] = i+1 + j    *density.x + k   *density.x*density.y;
                        triangles[offset+2 ] = i+1 + (j+1)*density.x + k   *density.x*density.y;
                        triangles[offset+3 ] = i   + j    *density.x + k   *density.x*density.y;
                        triangles[offset+4 ] = i+1 + (j+1)*density.x + k   *density.x*density.y;
                        triangles[offset+5 ] = i   + (j+1)*density.x + k   *density.x*density.y;

                        //Back
                        triangles[offset+6 ] = i   + j    *density.x + (k+1)*density.x*density.y;
                        triangles[offset+7 ] = i+1 + (j+1)*density.x + (k+1)*density.x*density.y;
                        triangles[offset+8 ] = i+1 + j    *density.x + (k+1)*density.x*density.y;
                        triangles[offset+9 ] = i   + j    *density.x + (k+1)*density.x*density.y;
                        triangles[offset+10] = i   + (j+1)*density.x + (k+1)*density.x*density.y;
                        triangles[offset+11] = i+1 + (j+1)*density.x + (k+1)*density.x*density.y;

                        //Left
                        triangles[offset+12] = i + j    *density.x + (k+1)*density.x*density.y;
                        triangles[offset+13] = i + j    *density.x + k    *density.x*density.y;
                        triangles[offset+14] = i + (j+1)*density.x + (k+1)*density.x*density.y;
                        triangles[offset+15] = i + j    *density.x + k    *density.x*density.y;
                        triangles[offset+16] = i + (j+1)*density.x + k    *density.x*density.y;
                        triangles[offset+17] = i + (j+1)*density.x + (k+1)*density.x*density.y;

                        //Right
                        triangles[offset+18] = (i+1) + j    *density.x + k    *density.x*density.y;
                        triangles[offset+19] = (i+1) + j    *density.x + (k+1)*density.x*density.y;
                        triangles[offset+20] = (i+1) + (j+1)*density.x + (k+1)*density.x*density.y;
                        triangles[offset+21] = (i+1) + (j+1)*density.x + k    *density.x*density.y;
                        triangles[offset+22] = (i+1) + j    *density.x + k    *density.x*density.y;
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
                        triangles[offset+34] = (i+1) + j*density.x + (k+1)*density.x*density.y;
                        triangles[offset+33] = (i+1) + j*density.x + k    *density.x*density.y;
                        triangles[offset+35] = i     + j*density.x + (k+1)*density.x*density.y;
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

            CreatePointFieldSmallMultiple(1);
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

            if(sm.InitFromPointField(m_parser, m_mesh, m_availableUVIDs[m_availableUVIDs.Count-1], dataID, 
                                     new Vector3Int((int)(ptsDesc.Size[0]/density.x), 
                                                    (int)(ptsDesc.Size[1]/density.y*ptsDesc.Size[0]),
                                                    (int)(ptsDesc.Size[2]/density.z*ptsDesc.Size[1]*ptsDesc.Size[0])),
                                     density))
            {
                m_smallMultiples.Add(sm);
                m_availableUVIDs.RemoveAt(m_availableUVIDs.Count-1);
                return sm;
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
    }
}