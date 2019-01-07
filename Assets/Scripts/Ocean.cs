using UnityEngine;
using System.Collections.Generic;

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
        /// The StructuredGrid to use
        /// </summary>
        public VTKUnityStructuredGrid StructuredGrid;

        /// <summary>
        /// The Ocean file path to use
        /// </summary>
        public string FilePath;

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

                //Generate the small multiples
                m_smallMultiples = new List<VTKUnitySmallMultiple>();
                m_smallMultiples.Add(m_oceanGrid.CreatePointFieldSmallMultiple(2));
                m_smallMultiples.Add(m_oceanGrid.CreatePointFieldSmallMultiple(3));
                m_smallMultiples.Add(m_oceanGrid.CreatePointFieldSmallMultiple(4));

                //Place them correctly
                for(int i = 0; i < m_smallMultiples.Count; i++)
                {
                    var c = m_smallMultiples[i];
                    c.transform.position = new Vector3(3*i, 0, 0);
                    c.transform.Rotate(new Vector3(0.0f, 1.0f, 0.0f), 90);
                }
            }
        }

        private void OnDestroy()
        {
            GameObject.Destroy(m_oceanGrid);
        }
    }
}