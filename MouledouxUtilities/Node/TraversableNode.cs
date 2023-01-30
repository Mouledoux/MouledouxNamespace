using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mouledoux.Node
{
    class TraversableNode : Node, ITraversable
    {
        protected ITraversable[] m_connectedTraversables;
        public ITraversable[] ConnectedTraversables { get => m_connectedTraversables; }


        protected ITraversable m_traversableOrigin;
        public ITraversable TraversableOrigin { get => m_traversableOrigin; set => m_traversableOrigin = value; }


        protected float[] m_coordinates;
        public float[] Coordinates { get => m_coordinates; set => m_coordinates = value; }


        protected float m_fVal;
        public float fVal { get => m_fVal + gVal + hVal; }


        protected float m_gVal;
        public float gVal { get => m_gVal; set => m_gVal = value; }


        protected float m_hVal;
        public float hVal { get => m_hVal; set => m_hVal = value; }

        
        protected bool m_isTraversable;
        public bool IsTraversable { get => m_isTraversable; set => m_isTraversable = value; }


        public int CompareTo(ITraversable other)
        {
            bool isLess = other.fVal > fVal;
            bool isSame = other.fVal == fVal;
            return isSame ? 0 : isLess ? -1 : 1;
        }

        public float GetHeuristicTo(ITraversable destination)
        {
            return (float)this.GetLinearDistanceTo(destination);
        }

        public TraversableNode(float[] a_coordinates)
        {
            m_connectedTraversables = new ITraversable[0];
            m_coordinates = a_coordinates;
            m_fVal = 0;
            m_gVal = 0;
            m_hVal = 0;
            m_isTraversable = true;
        }

        public TraversableNode(ITraversable[] a_connectedTraversables, float[] a_coordinates, float a_fVal = 0, bool a_isTraversable = true)
        {
            m_connectedTraversables = a_connectedTraversables;
            m_coordinates = a_coordinates;
            m_fVal = a_fVal;
            m_gVal = 0;
            m_hVal = 0;
            m_isTraversable = a_isTraversable;
        }
    }
}
