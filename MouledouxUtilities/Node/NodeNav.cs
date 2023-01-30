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

                foreach (ITraversable neighborNode in currentNode.GetConnectedTraversables())
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
                            Stack<T> returnStack = GetTraversableStackPathTo<T>(endNode);
                            
                            foreach(ITraversable tn in closedList)
                            {
                                ClearOriginChain(tn);
                            }
                            foreach(ITraversable tn in openList)
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
                                    neighborNode.hVal = (float)GetDistanceTo(neighborNode, endNode) * hMod;

                                    AddToSortedList(neighborNode, ref openList);
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
        private static int AddToSortedList<T>(T listElement, ref List<T> sortedList) where T : System.IComparable<T>
        {
            for(int i = 0; i < sortedList.Count; i++)
            {
                if(listElement.CompareTo(sortedList[i]) < 0)
                {
                    sortedList.Insert(i, listElement);
                    return i;
                }
            }

            sortedList.Add(listElement);
            return sortedList.Count;
        }





        // ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ----------
        public static T GetRootOrigin<T>(T node) where T : ITraversable
        {
            return node.TraversableOrigin == null ? node : GetRootOrigin((T)node.TraversableOrigin);
        }




        // ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ----------
        public static void ClearOriginChain(ITraversable endNode)
        {
            foreach(ITraversable tn in endNode.GetConnectedTraversables())
            {
                if (tn.TraversableOrigin == endNode)
                {
                    ClearOriginChain(tn);
                }
            }

            if (endNode.TraversableOrigin != null)
            {
                ClearOriginChain(endNode.TraversableOrigin);
                endNode.TraversableOrigin = null;
            }
        }




        // ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ----------
        public static void ReverseOriginChain(ITraversable endNode)
        {
            ITraversable currentNode = endNode;
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
        public static void ValidateOriginChain(this ITraversable endNode)
        {
            ITraversable iterator = endNode;
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
        public static bool CheckOriginChainFor(ITraversable node, ITraversable higherOrigin)
        {
            ITraversable nextNode = node;
            node.ValidateOriginChain();

            do
            {
                if (nextNode == higherOrigin)
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
        public static float GetTravelCostToRootOrigin(ITraversable traversableNode)
        {
            return (traversableNode.TraversableOrigin == null) ? 0 : (traversableNode.fVal + GetTravelCostToRootOrigin(traversableNode.TraversableOrigin));
        }




        // ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ----------
        public static double GetDistanceTo(ITraversable origin, ITraversable destination)
        {
            double distance = 0;
            int minCoord = origin.Coordinates.Length < destination.Coordinates.Length ?
                origin.Coordinates.Length : destination.Coordinates.Length;

            for (int i = 0; i < minCoord; i++)
            {
                double dif = origin.Coordinates[i] - destination.Coordinates[i];
                distance += System.Math.Pow(dif, 2.0);
            }

            return System.Math.Sqrt(distance);
        }
    }



    public interface ITraversable : System.IComparable<ITraversable>
    {
        ITraversable TraversableOrigin {get; set;}
        float[] Coordinates {get; set;}
        float fVal {get;}
        float gVal {get; set;}
        float hVal {get; set;}
        float[] PathingValues {get; set;}

        bool IsOccupied {get; set;}
        bool IsTraversable {get; set;}


        ITraversable[] GetConnectedTraversables();
    }
}
