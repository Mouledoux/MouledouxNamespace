using System.Collections;
using System.Collections.Generic;

namespace Mouledoux.Node
{
    public abstract class Node<NodeType> where NodeType : Node<NodeType>
    {
        private List<NodeType> m_neighbors;
        private List<object> m_information;

        public Node()
        {
            m_neighbors = new List<NodeType>();
            m_information = new List<object>();
        }

        // ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ----------
        public NodeType[] GetNeighbors()
        {
            NodeType[] myNeighbors = m_neighbors.ToArray();
            return myNeighbors;
        }

        // ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ----------
        public NodeType[] GetNeighborhood(uint a_layers = 1)
        {   
            int index = 0;
            int neighbors = 0;

            List<NodeType> neighborhood = new List<NodeType>();
            neighborhood.Add(this as NodeType);

            for(int i = 0; i < a_layers; i++)
            {
                neighbors = neighborhood.Count;
                for(int j = index; j < neighbors; j++)
                {
                    foreach(NodeType n in neighborhood[j].GetNeighbors())
                    {
                        if(!neighborhood.Contains(n))
                        {
                            neighborhood.Add(n);
                        }
                    }
                    index = j;
                }
            }
            neighborhood.Remove(this as NodeType);
            return neighborhood.ToArray();
        }

        // ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ----------
        public NodeType[] GetNeighborhoodLayers(uint a_innerBand, uint a_bandWidth = 1)
        {
            //innerBand--;
            NodeType[] n1 = GetNeighborhood(a_innerBand + a_bandWidth);
            NodeType[] n2 = GetNeighborhood(a_innerBand);

            List<NodeType> neighborhood = new List<NodeType>();

            foreach (NodeType n in n1)
            {
                neighborhood.Add(n);
            }

            foreach (NodeType n in n2)
            {
                neighborhood.Remove(n);
            }
            
            return neighborhood.ToArray();
        }

        // ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ----------
        public int AddNeighbor(NodeType a_newNeighbor)
        {
            if (a_newNeighbor == null)
            {
                return -1;
            }
            else if (a_newNeighbor == this)
            {
                return -2;
            }

            if (!m_neighbors.Contains(a_newNeighbor))
            {
                m_neighbors.Add(a_newNeighbor);
            }

            if (!a_newNeighbor.m_neighbors.Contains(this as NodeType))
            {
                a_newNeighbor.AddNeighbor(this as NodeType);
            }

            return 0;
        }

        // ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ----------
        public bool CheckIsNeighbor(NodeType a_node)
        {
            return m_neighbors.Contains(a_node);
        }

        // ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ----------
        public int RemoveNeighbor(NodeType a_oldNeighbor)
        {
            if (CheckIsNeighbor(a_oldNeighbor))
            {
                m_neighbors.Remove(a_oldNeighbor);
                a_oldNeighbor.RemoveNeighbor(this as NodeType);
            }

            return 0;
        }

        // ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ----------
        public int ClearNeighbors()
        {
            foreach (NodeType n in m_neighbors)
            {
                n.RemoveNeighbor(this as NodeType);
            }
            
            m_neighbors.Clear();

            return 0;
        }

        // ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ----------
        public int TradeNeighbors(NodeType a_neighbor)
        {
            if (a_neighbor == null)
            {
                return -1;
            }
            else if (!m_neighbors.Contains(a_neighbor))
            {
                return -2;
            }

            NodeType[] myNeighbors;


            // // Remove eachother as neighbors, so they aren't neighbors to themselves
            // RemoveNeighbor(a_neighbor);
            // a_neighbor.RemoveNeighbor(this);

            // Save this node neighbors to a temp array
            myNeighbors = m_neighbors.ToArray();
        

            ClearNeighbors();                               // Clear this node's neighbors
            foreach (NodeType n in a_neighbor.GetNeighbors())    // For each neighbor of my neighbor
            {
                AddNeighbor(n);                                 // Copy it to this node's neighbors
            }
            AddNeighbor(a_neighbor);                        // Add the neighbor back to this node's neighbors


            a_neighbor.ClearNeighbors();                    // Clear the neighbor's neighbors
            foreach (NodeType n in myNeighbors)                  // For each node in the temp array
            {
                a_neighbor.AddNeighbor(n);                        // Copy it to the neighbor's new neighbors
            }
            a_neighbor.AddNeighbor(this as NodeType);                   // Add this node back to the neighbor's neighbors

            return 0;
        }



        // ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ----------
        public int AddInformation(object a_info)
        {
            m_information.Add(a_info);
            return 0;
        }

        // ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ----------
        public int RemoveInformation(object a_info)
        {
            if (m_information.Contains(a_info))
            {
                m_information.Remove(a_info);
            }

            return 0;
        }

        // ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ----------
        public bool CheckInformationFor(object a_info)
        {
            return m_information.Contains(a_info);
        }

        // ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ----------
        public NodeInfoType[] GetInformation<NodeInfoType>()
        {
            List<NodeInfoType> returnList = new List<NodeInfoType>();

            foreach(object info in m_information)
            {
                if(typeof(NodeInfoType).IsAssignableFrom(info.GetType()))
                {
                    returnList.Add((NodeInfoType)info);
                }
            }
            return returnList.ToArray();
        }

        public NodeInfoType[] GetNeighborsInformation<NodeInfoType>()
        {
            return GetMassNodeInfoTypermation<NodeInfoType>(GetNeighbors());
        }

        public NodeInfoType[] GetNeighborhoodInformation<NodeInfoType>(uint a_layers = 1)
        {
            return GetMassNodeInfoTypermation<NodeInfoType>(GetNeighborhood(a_layers));
        }

        public NodeInfoType[] GetNeighborhoodLayersInformation<NodeInfoType>(uint a_innerBand, uint a_bandWidth = 1)
        {
            return GetMassNodeInfoTypermation<NodeInfoType>(GetNeighborhoodLayers(a_innerBand, a_bandWidth));
        }

        private static NodeInfoType[] GetMassNodeInfoTypermation<NodeInfoType>(NodeType[] nodes)
        {
            List<NodeInfoType> returnList = new List<NodeInfoType>();

            foreach(NodeType node in nodes)
            {
                foreach(NodeInfoType info in node.GetInformation<NodeInfoType>())
                {
                    returnList.Add(info);
                }
            }

            return returnList.ToArray();
        }
    }
}