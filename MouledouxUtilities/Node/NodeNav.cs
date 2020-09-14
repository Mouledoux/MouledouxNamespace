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
            if(begNode == endNode || begNode == null || endNode == null || !endNode.isTraversable) return null;


            List<ITraversable> openList = new List<ITraversable>();
            List<ITraversable> closedList = new List<ITraversable>();

            openList.Add(begNode);

            begNode.origin = null;
            ITraversable currentNode;

            while(openList.Count > 0)
            {
                currentNode = openList[0];

                foreach (ITraversable neighborNode in currentNode.GetConnectedTraversables())
                {
                    if(neighborNode == null || neighborNode.isTraversable == false) { continue; }
                    
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
                            neighborNode.origin = currentNode;
                            Stack<T> returnStack = TraversableStackPath<T>(endNode);
                            
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
                                    neighborNode.origin = currentNode;
                                    neighborNode.gVal = GetTravelCostToRootOrigin(neighborNode) * gMod;
                                    neighborNode.hVal = (float)GetDistanceTo(neighborNode, endNode) * hMod;

                                    AddToSortedList<ITraversable>(neighborNode, ref openList);
                                }
                            }

                            // We have already been to this node, so see if it's cheaper to the current node from here
                            else if(neighborNode.origin != currentNode && neighborNode.CompareTo(currentNode) < 0)
                            {
                                currentNode.origin = neighborNode;
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
        private static Stack<T> TraversableStackPath<T>(ITraversable endNode) where T : ITraversable
        {
            Stack<T> returnStack = new Stack<T>();
            T currentNode = (T)endNode;

            ValidateOriginChain(currentNode);

            while(currentNode != null && !currentNode.origin.Equals(currentNode))
            {
                try
                {
                    returnStack.Push(currentNode);
                    currentNode = (T)currentNode.origin;
                }
                catch(System.OutOfMemoryException)
                {
                    // do something
                }
            }

            return returnStack;
        }




        // ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ----------
        private static int AddToSortedList<T>(T node, ref List<T> sortedList) where T : System.IComparable<T>
        {
            for(int i = 0; i < sortedList.Count; i++)
            {
                if(node.CompareTo(sortedList[i]) < 0)
                {
                    sortedList.Insert(i, node);
                    return i;
                }
            }

            sortedList.Add(node);
            return sortedList.Count;
        }





        // ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ----------
        public static T GetRootOrigin<T>(T node) where T : ITraversable
        {
            return node.origin == null ? node : GetRootOrigin((T)node.origin);
        }




        // ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ----------
        public static void ClearOriginChain(ITraversable endNode)
        {
            foreach(ITraversable tn in endNode.GetConnectedTraversables())
            {
                if (tn.origin == endNode) ClearOriginChain(tn);
            }

            if (endNode.origin != null)
            {
                ClearOriginChain(endNode.origin);
                endNode.origin = null;
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
                nextNode = currentNode.origin;
                currentNode.origin = previousNode;
                previousNode = currentNode;
                currentNode = nextNode;

            } while (currentNode != null);
        }




        // ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ----------
        public static void ValidateOriginChain(ITraversable endNode)
        {
            ITraversable iterator = endNode;
            List<ITraversable> originChain = new List<ITraversable>();

            while (iterator.origin != iterator)
            {
                if (originChain.Contains(iterator.origin))
                {
                    iterator.origin = null;
                }
                else
                {
                    originChain.Add(iterator);
                    iterator = iterator.origin;
                }
            }
        }




        // ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ----------
        public static bool CheckOriginChainFor(ITraversable node, ITraversable higherOrigin)
        {
            ITraversable nextNode = node;
            ValidateOriginChain(node);

            do
            {
                if (nextNode == higherOrigin) return true;
                else nextNode = nextNode.origin;

            } while (nextNode.origin != null);

            return false;
        }




        // ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ----------
        public static float GetTravelCostToRootOrigin<T>(T node) where T : ITraversable
        {
            if (node.origin == null) return 0;

            else return node.fVal + GetTravelCostToRootOrigin(node.origin);
        }




        // ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ----------
        public static double GetDistanceTo<T>(T origin, T destination) where T : ITraversable
        {
            double distance = 0;
            int minCoord = origin.coordinates.Length < destination.coordinates.Length ?
                origin.coordinates.Length : destination.coordinates.Length;

            for (int i = 0; i < minCoord; i++)
            {
                double dif = origin.coordinates[i] - destination.coordinates[i];
                distance += System.Math.Pow(dif, 2.0);
            }

            return System.Math.Sqrt(distance);
        }
    }



    public interface ITraversable : System.IComparable<ITraversable>
    {
        ITraversable origin {get; set;}
        float[] coordinates {get; set;}
        float fVal {get;}
        float gVal {get; set;}
        float hVal {get; set;}
        float[] pathingValues {get; set;}

        bool isOccupied {get; set;}
        bool isTraversable {get; set;}


        ITraversable[] GetConnectedTraversables();
    }
}
