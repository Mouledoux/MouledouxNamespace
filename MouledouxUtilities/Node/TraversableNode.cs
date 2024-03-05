using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mouledoux.Node
{
    class TraversableNode : Node<TraversableNode>, ITraversable<TraversableNode>
    {
        protected TraversableNode[] m_connectedTraversables;
        public TraversableNode[] ConnectedTraversables
        {
            get => m_connectedTraversables;
        }


        protected TraversableNode m_traversableOrigin;
        public TraversableNode TraversableOrigin
        {
            get => m_traversableOrigin;
            private set => m_traversableOrigin = value;
        }


        protected float[] m_coordinates;
        public float[] Coordinates
        { 
            get => m_coordinates; 
            private set => m_coordinates = value;
        }


        protected float m_fVal;
        public float fVal
        {
            get => m_fVal + gVal + hVal;
        }


        protected float m_gVal;
        public float gVal
        {
            get => m_gVal;
            private set => m_gVal = value;
        }


        protected float m_hVal;
        public float hVal
        {
            get => m_hVal;
            private set => m_hVal = value;
        }


        protected bool m_isTraversable;
        public bool IsTraversable
        {
            get => m_isTraversable;
            private set => m_isTraversable = value;
        }

        
        public int CompareTo(ITraversable<TraversableNode> other)
        {
            bool isSame = fVal == other.fVal;
            bool isLess = fVal < other.fVal;
            return isSame ? 0 : isLess ? -1 : 1;
        }

        public TraversableNode(float[] a_coordinates)
        {
            m_connectedTraversables = new TraversableNode[0];
            m_coordinates = a_coordinates;
            m_fVal = 0;
            m_gVal = 0;
            m_hVal = 0;
            m_isTraversable = true;
        }

        public TraversableNode(TraversableNode[] a_connectedTraversables, float[] a_coordinates, float a_fVal = 0, bool a_isTraversable = true)
        {
            m_connectedTraversables = a_connectedTraversables;
            m_coordinates = a_coordinates;
            m_fVal = a_fVal;
            m_gVal = 0;
            m_hVal = 0;
            m_isTraversable = a_isTraversable;
        }

        protected void SetTraversablesToNeighbors()
        {
            m_connectedTraversables = GetNeighbors();
        }
    }
}
