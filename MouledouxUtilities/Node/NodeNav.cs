using System.Threading;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;

namespace Mouledoux.Node
{
    public static class NodeNav
    {
        // ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ----------
        public static Stack<T> TwinStarT<T>(ITraversable begNode, ITraversable endNode, bool dualSearch = true) where T : ITraversable
        {
            object chainLocker = new object();


            if(dualSearch)
            {
                Task backwards = Task.Run(() => SoloStar<T>(endNode, begNode, chainLocker, false, 0f, 1f));
            }

            Task<Stack<T>> forward = Task.Run(() => SoloStar<T>(begNode, endNode, chainLocker));

            return forward.Result;
        }



        public static Stack<T> SoloStar<T>(ITraversable begNode, ITraversable endNode, object chainLocker, bool canReturn = true, float hMod = 1f, float gMod = 1f) where T : ITraversable
        {
            if(begNode == endNode || begNode == null || endNode == null || !endNode.IsTraversable) return null;


            List<ITraversable> openList = new List<ITraversable>();
            List<ITraversable> closedList = new List<ITraversable>();

            openList.Add(begNode);

            begNode.TraversableOrigin = null;
            ITraversable currentNode;

            while(openList.Count > 0)
            {
                currentNode = openList[0];

                foreach (ITraversable neighborNode in currentNode.ConnectedTraversables)
                {
                    if(neighborNode == null || neighborNode.IsTraversable == false)
                    {
                        continue;
                    }
                    
                    // Locks the chain modifying to prevent overriding
                    lock(chainLocker)
                    {
                        bool endInChain = neighborNode.CheckOriginChainFor(endNode);

                        if(endInChain)
                        {
                            if(canReturn == false)
                            {
                                return null;
                            }

                            neighborNode.ReverseOriginChain();
                            neighborNode.TraversableOrigin = currentNode;
                            Stack<T> returnStack = GetTraversableStackPathTo<T>(endNode);
                            
                            foreach(ITraversable tn in closedList)
                            {
                                tn.ClearOriginChain();
                            }
                            foreach(ITraversable tn in openList)
                            {
                                tn.ClearOriginChain();
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
                                    neighborNode.gVal = neighborNode.GetTravelCostToRootOrigin() * gMod;
                                    neighborNode.hVal = neighborNode.GetHeuristicTo(endNode) * hMod;

                                    openList.AddToSortedList(neighborNode);
                                }
                            }

                            // We have already been to this node, so see if it's cheaper to the current node from here
                            else if(neighborNode.TraversableOrigin != currentNode && neighborNode.CompareTo(currentNode) < 0)
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
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="endNode"></param>
        /// <returns>A stack of ITraversable in order from origin at the top, to the provided endNode at the bottom</returns>
        private static Stack<T> GetTraversableStackPathTo<T>(ITraversable endNode) where T : ITraversable
        {
            Stack<T> returnStack = new Stack<T>();
            T currentNode = (T)endNode;

            currentNode.ValidateOriginChain();

            while(currentNode != null && !currentNode.TraversableOrigin.Equals(currentNode))
            {
                try
                {
                    returnStack.Push(currentNode);
                    currentNode = (T)currentNode.TraversableOrigin;
                }
                catch(System.OutOfMemoryException)
                {
                    // do something
                }
            }

            return returnStack;
        }




        // ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ----------
        private static int AddToSortedList<T>(this List<T> thisSortedList, T listElement) where T : System.IComparable<T>
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
        public static T GetRootOrigin<T>(this T thisNode) where T : ITraversable
        {
            return thisNode.TraversableOrigin == null ? thisNode : (T)(thisNode.TraversableOrigin.GetRootOrigin());
        }




        // ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ----------
        public static void ClearOriginChain(this ITraversable thisEndNode)
        {
            foreach(ITraversable tn in thisEndNode.ConnectedTraversables)
            {
                if (tn.TraversableOrigin == thisEndNode)
                {
                    tn.ClearOriginChain();
                }
            }

            if (thisEndNode.TraversableOrigin != null)
            {
                thisEndNode.TraversableOrigin.ClearOriginChain();
                thisEndNode.TraversableOrigin = null;
            }
        }




        // ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ----------
        public static void ReverseOriginChain(this ITraversable thisEndNode)
        {
            ITraversable currentNode = thisEndNode;
            ITraversable previousNode = null;
            ITraversable nextNode = null;

            do
            {
                nextNode = currentNode.TraversableOrigin;
                currentNode.TraversableOrigin = previousNode;
                previousNode = currentNode;
                currentNode = nextNode;

            } while (currentNode != null);
        }




        // ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ----------
        public static void ValidateOriginChain(this ITraversable thisEndNode)
        {
            ITraversable iterator = thisEndNode;
            List<ITraversable> originChain = new List<ITraversable>();

            while (iterator.TraversableOrigin != iterator)
            {
                if (originChain.Contains(iterator.TraversableOrigin))
                {
                    iterator.TraversableOrigin = null;
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
        public static bool CheckOriginChainFor(this ITraversable thisNode, ITraversable targetNode)
        {
            ITraversable nextNode = thisNode;
            thisNode.ValidateOriginChain();

            do
            {
                if (nextNode == targetNode)
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
        public static float GetTravelCostToRootOrigin(this ITraversable traversableNode)
        {
            return (traversableNode.TraversableOrigin != null)
                ? (traversableNode.fVal + traversableNode.TraversableOrigin.GetTravelCostToRootOrigin())
                : 0;
        }




        // ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ----------
        public static double GetLinearDistanceTo(this ITraversable origin, ITraversable destination)
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
        public static double GetManhattanDistanceTo(this ITraversable origin, ITraversable destination)
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



    public interface ITraversable : System.IComparable<ITraversable>
    {
        ITraversable[] ConnectedTraversables { get; }

        ITraversable TraversableOrigin {get; set;}

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


        float GetHeuristicTo(ITraversable destination);
    }
}
