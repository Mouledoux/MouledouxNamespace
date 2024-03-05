using System.Threading;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;

namespace Mouledoux.Node
{
    public static class NodeNav<NodeType> where NodeType : ITraversable<NodeType>, System.IComparable<NodeType>
    {
        // ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ----------
        public static Stack<NodeType> TwinStarT(NodeType begNode, NodeType endNode, bool dualSearch = true)
        { 
            object chainLocker = new object();

            if(dualSearch)
            {
                Task backwards = Task.Run(() => SoloStar(endNode, begNode, chainLocker, false, 0f, 1f));
            }

            Task<Stack<NodeType>> forward = Task.Run(() => SoloStar(begNode, endNode, chainLocker));

            return forward.Result;
        }



        // ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ----------
        public static Stack<NodeType> SoloStar(NodeType begNode, NodeType endNode, object chainLocker, bool canReturn = true, float hMod = 1f, float gMod = 1f)
        {
            if(begNode.Equals(endNode) || begNode == null || endNode == null || !endNode.IsTraversable)
            {
                return null;
            }
            
            List<NodeType> openList = new List<NodeType>();
            List<NodeType> closedList = new List<NodeType>();

            openList.Add(begNode);

            begNode.TraversableOrigin = default;
            NodeType currentNode;

            while(openList.Count > 0)
            {
                currentNode = openList[0];

                foreach (NodeType neighborNode in currentNode.ConnectedTraversables)
                {
                    if(neighborNode == null || neighborNode.IsTraversable == false)
                    {
                        continue;
                    }
                    
                    // Locks the chain modifying to prevent overriding
                    lock(chainLocker)
                    {
                        bool endInChain = CheckOriginChainFor(neighborNode, endNode);

                        if(endInChain)
                        {
                            if(canReturn == false)
                            {
                                return null;
                            }

                            ReverseOriginChain(neighborNode);
                            neighborNode.TraversableOrigin = currentNode;
                            Stack<NodeType> returnStack = GetTraversableStackPathTo(endNode);
                            
                            foreach(NodeType tn in closedList)
                            {
                                ClearOriginChain(tn);
                            }
                            foreach(NodeType tn in openList)
                            {
                                ClearOriginChain(tn);
                            }

                            return returnStack;
                        }

                        else
                        {
                            if(!closedList.Contains(neighborNode))
                            {
                                if(!openList.Contains(neighborNode))
                                {
                                    neighborNode.TraversableOrigin = currentNode;
                                    neighborNode.gVal = GetTravelCostToRootOrigin(neighborNode) * gMod;
                                    neighborNode.hVal = neighborNode.GetHeuristicTo(endNode) * hMod;

                                    AddToSortedList(ref openList, neighborNode);
                                }
                            }

                            // We have already been to this node, so see if it's cheaper to the current node from here
                            else if(neighborNode.TraversableOrigin.Equals(currentNode) == false && neighborNode.CompareTo(currentNode) < 0)
                            {
                                currentNode.TraversableOrigin = neighborNode;
                            }                            
                        }
                    }
                }

                closedList.Add(currentNode);
                openList.Remove(currentNode);
            }

            return null;
        }



        // ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ----------
        private static Stack<NodeType> GetTraversableStackPathTo(NodeType endNode)
        {
            Stack<NodeType> returnStack = new Stack<NodeType>();
            NodeType currentNode = endNode;

            ValidateOriginChain(currentNode);

            while(currentNode != null && !currentNode.TraversableOrigin.Equals(currentNode))
            {
                try
                {
                    returnStack.Push(currentNode);
                    currentNode = currentNode.TraversableOrigin;
                }
                catch(System.OutOfMemoryException)
                {
                    // do something
                }
            }

            return returnStack;
        }




        // ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ----------
        private static int AddToSortedList<ListType>(ref List<ListType> thisSortedList, ListType listElement) where ListType : System.IComparable<ListType>
        {
            for(int i = 0; i < thisSortedList.Count; i++)
            {
                if(listElement.CompareTo(thisSortedList[i]) < 0)
                {
                    thisSortedList.Insert(i, listElement);
                    return i;
                }
            }

            thisSortedList.Add(listElement);
            return thisSortedList.Count;
        }





        // ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ----------
        public static NodeType GetRootOrigin(NodeType thisNode)
        {
            return thisNode.TraversableOrigin == null ? thisNode : GetRootOrigin(thisNode.TraversableOrigin);
        }




        // ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ----------
        public static void ClearOriginChain(NodeType thisEndNode)
        {
            foreach(NodeType tn in thisEndNode.ConnectedTraversables)
            {
                if (tn.TraversableOrigin.Equals(thisEndNode))
                {
                    ClearOriginChain(tn);
                }
            }

            if (thisEndNode.TraversableOrigin != null)
            {
                ClearOriginChain(thisEndNode.TraversableOrigin);
                thisEndNode.TraversableOrigin = default;
            }
        }




        // ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ----------
        public static void ReverseOriginChain(NodeType thisEndNode)
        {
            NodeType currentNode = thisEndNode;
            NodeType previousNode = default;
            NodeType nextNode = default;

            do
            {
                nextNode = currentNode.TraversableOrigin;
                currentNode.TraversableOrigin = previousNode;
                previousNode = currentNode;
                currentNode = nextNode;

            } while (currentNode.Equals(null) == false && currentNode.Equals(default) == false);
        }




        // ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ----------
        public static void ValidateOriginChain(NodeType thisEndNode)
        {
            NodeType iterator = thisEndNode;
            List<NodeType> originChain = new List<NodeType>();

            while (iterator.TraversableOrigin.Equals(iterator) == false)
            {
                if (originChain.Contains(iterator.TraversableOrigin))
                {
                    iterator.TraversableOrigin = default;
                }
                else
                {
                    originChain.Add(iterator);
                    iterator = iterator.TraversableOrigin;
                }
            }

            originChain.Clear();
        }




        // ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ----------
        public static bool CheckOriginChainFor(NodeType thisNode, NodeType targetNode)
        {
            NodeType nextNode = thisNode;
            ValidateOriginChain(thisNode);

            do
            {
                if (nextNode.Equals(targetNode))
                {
                    return true;
                }

                else
                {
                    nextNode = nextNode.TraversableOrigin;
                }

            } while (nextNode.TraversableOrigin != null);

            return false;
        }




        // ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ----------
        public static float GetTravelCostToRootOrigin(NodeType traversableNode)
        {
            return (traversableNode.TraversableOrigin != null)
                ? (traversableNode.fVal + GetTravelCostToRootOrigin(traversableNode.TraversableOrigin))
                : 0;
        }




        // ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ----------
        public static double GetLinearDistanceTo(NodeType origin, NodeType destination)
        {
            double distance = 0;
            int minCoord =
                (origin.Coordinates.Length < destination.Coordinates.Length)
                ? origin.Coordinates.Length
                : destination.Coordinates.Length;

            for (int i = 0; i < minCoord; i++)
            {
                double dif = origin.Coordinates[i] - destination.Coordinates[i];
                distance += System.Math.Pow(dif, 2.0);
            }

            return System.Math.Sqrt(distance);
        }        
        
        
        // ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ----------
        public static double GetManhattanDistanceTo(NodeType origin, NodeType destination)
        {
            double distance = 0;
            int minCoord =
                (origin.Coordinates.Length < destination.Coordinates.Length)
                ? origin.Coordinates.Length
                : destination.Coordinates.Length;

            for (int i = 0; i < minCoord; i++)
            {
                double dif = origin.Coordinates[i] - destination.Coordinates[i];
                distance += System.Math.Abs(dif);
            }

            return distance;
        }
    }



    public interface ITraversable<TraversableType> : System.IComparable<ITraversable<TraversableType>>
    {
        TraversableType[] ConnectedTraversables { get; }

        TraversableType TraversableOrigin {get; set;}

        float[] Coordinates {get; set;}

        /// <summary>
        /// Base cost of traversal, set by user
        /// </summary>
        float fVal { get; }

        /// <summary>
        /// The cost of travel back to the TraversableOrigin, set by NodeNav.GetTravelCostToRootOrigin
        /// </summary>
        float gVal {get; set;}

        /// <summary>
        /// A heuristic estimate of the cheapest path to the goal, set by ITraversable.GetHerusitricTo
        /// </summary>
        float hVal {get; set;}

        bool IsTraversable {get; set;}


        float GetHeuristicTo(ITraversable<TraversableType> destination);
    }
}
