using System.Threading;
using System.Collections.Generic;

namespace Mouledoux.Components
{
    public sealed class Mediator
    {
        /// <summary>
        /// Messages and their associated subscriptions
        /// </summary>
        private static Dictionary<string, List<Subscription>> m_orderedSubscriptions =
            new Dictionary<string, List<Subscription>>();

        /// <summary>
        /// Messages that had no subscriptions at broadcast, but were marked for hold
        /// </summary>
        private static List<string> m_staleMessages = new List<string>();



        /// <summary>
        /// Broadcast a message to potential subscribers
        /// </summary>
        /// <param name="a_message">message to broadcast</param>
        /// <param name="a_args">arguments to pass to subscription callbacks</param>
        /// <param name="a_holdMessage">if there are no active subscriptions, rebroadcast when one subscribes</param>param>
        /// <param name="a_multiThread">should callbacks be preformed on other threads</param>
        /// <returns></returns>
        public static int NotifySubscribers(string a_message, object[] a_args = null, bool a_holdMessage = false, bool a_multiThread = false)
        {
            if(a_multiThread)
            {
                ThreadPool.QueueUserWorkItem((object a) => NotifySubscribers(a_message, a_args, a_holdMessage));
            }
            else
            {
                NotifySubscribers(a_message, a_args, a_holdMessage);
            }
            return 0;
        }



        /// <summary>
        /// Checks if a subscription message exist
        /// </summary>
        /// <param name="a_message">subscription message to check</param>
        /// <returns>returns true if the message exist</returns>
        public static bool CheckForSubscription(string a_message)
        {
            return m_orderedSubscriptions.ContainsKey(a_message);
        }




        /// <summary>
        /// Broadcast a message to potential subscribers
        /// </summary>
        /// <param name="a_message">message to broadcast</param>
        /// <param name="a_args">arguments to pass to subscription callbacks</param>
        /// <param name="a_holdMessage">if there are no active subscriptions, rebroadcast when one subscribes</param>
        /// <returns></returns>
        private static int NotifySubscribers(string a_message, object[] a_args = null, bool a_holdMessage = false)
        {
            a_message = a_message.ToLower();

            // Makes sure the object array has been set to something, even if one isn't provideds
            a_args = a_args == null ? new object[0] : a_args;

            bool messageBroadcasted = TryInvokeSubscription(ref m_orderedSubscriptions, a_message, a_args);

            // If nothing is listening to the message, but it's been marked to hold
            if (!messageBroadcasted && a_holdMessage && !m_staleMessages.Contains(a_message))
            {
                // add it to the hold list
                m_staleMessages.Add(a_message);
            }

            return 0;
        }




        /// <summary>
        /// Checks subscription associated callbacks for null ref errors
        /// Removes any that are not validated
        /// </summary>
        /// <param name="a_container">message subscription dictionary</param>
        /// <param name="a_message">message to validate callbacks under</param>
        /// <returns>return true if valid callbacks >= 1</returns>
        private static bool ValidateSubscriptionCallbacks(ref Dictionary<string, List<Subscription>> a_container, string a_message)
        {
            List<Subscription> tSub;

            // get all the subscriptions to a single message
            if (a_container.TryGetValue(a_message, out tSub))
            {
                // for all the subs to the message
                for (int i = 0; i < tSub.Count; i++)
                {
                    try
                    {
                        // and for each action in each sub
                        foreach (System.Action<object[]> del in tSub[i].m_callback.GetInvocationList())
                        {
                            // if the action has no valid targets, remove it
                            tSub[i].m_callback -= del.Target.Equals(null) ? del : null;
                        }

                        // if there are no actions left on the sub
                        if (tSub[i].m_callback == null)
                        {
                            // remove the sub from the mesasge
                            // and accomidate for the sub list loosing 1
                            a_container[a_message].RemoveAt(i);
                            i--;
                        }

                        else
                        {
                            // apply the remang actions to the sub
                            a_container[a_message][i] = tSub[i];
                        }
                    }

                    // catch if the sub trigger a null ref
                    catch (System.NullReferenceException)
                    {
                        // remove it completely
                        // and accomidate for sub list loosing 1
                        tSub.RemoveAt(i);
                        i--;
                    }
                }

                // return true if the message has any remaining valid subs
                if (a_container[a_message].Count > 0)
                {
                    return true;
                }
                // else, remove the message subscription
                else
                {
                    a_container.Remove(a_message);
                }
            }

            // there are no subscriptions to that mesasge
            return false;
        }


        /// <summary>
        /// Invokes all valid callbacks subscribed to a message
        /// </summary>
        /// <param name="a_container">message subscription dictionary</param>
        /// <param name="a_message">message to be broadcasted</param>
        /// <param name="a_args">arguments to pass to the callback</param>
        /// <returns>returns true if the broadcast was successful</returns>
        private static bool TryInvokeSubscription(ref Dictionary<string, List<Subscription>> a_container, string a_message, object[] a_args)
        {
            if (ValidateSubscriptionCallbacks(ref a_container, a_message))
            {
                foreach (Subscription sub in a_container[a_message])
                {
                    sub.m_callback.Invoke(a_args);
                }
                return true;
            }
            else
            {
                return false;
            }
        }


        private static void Subscribe(ref Dictionary<string, List<Subscription>> a_container, string a_message, System.Action<object[]> a_callback, int a_priority = 0)
        {
            a_message = a_message.ToLower();

            Subscription sub = new Subscription(a_message, a_callback, a_priority);
            Subscribe(ref a_container, sub);
        }


        private static void Subscribe(ref Dictionary<string, List<Subscription>> a_container, Subscription a_sub, bool a_acceptStaleMesages = false)
        {
            string message = a_sub.m_message.ToLower();
            List<Subscription> tSub;

            if (!a_container.TryGetValue(message, out tSub))
            {
                tSub = new List<Subscription>();
                a_container.Add(message, tSub);

                if (a_acceptStaleMesages && m_staleMessages.Contains(message))
                {
                    m_staleMessages.Remove(message);
                    a_sub.m_callback.Invoke(null);
                }
            }

            tSub.Add(a_sub);
            a_container[message] = tSub;
            a_container[message].Sort();
        }


        private static void Unsubscribe(ref Dictionary<string, List<Subscription>> a_container, Subscription a_sub)
        {
            string message = a_sub.m_message.ToLower();

            List<Subscription> tSub;

            if (a_container.TryGetValue(message, out tSub))
            {
                tSub.Remove(a_sub);

                if (tSub.Count == 0)
                {
                    a_container.Remove(message);
                }
                else
                {
                    a_container[message] = tSub;
                }
            }
        }





        public sealed class Subscription : System.IComparable<Subscription>
        {
            private string _message;
            private int _priority;
            public System.Action<object[]> m_callback;

            public string m_message
            {
                get => _message;
                set
                {
                    Unsubscribe();
                    _message = value;
                    Subscribe();
                }
            }

            public int m_priority
            {
                get => _priority;
                set => _priority = value;
            }

            public Subscription(string a_message, System.Action<object[]> a_callback, int a_priority = 0)
            {
                _message = a_message;
                _priority = a_priority;
                m_callback = a_callback;
            }

            public Subscription Subscribe(bool a_acceptStaleMessages = false)
            {
                ThreadPool.QueueUserWorkItem((object a) => Mediator.Subscribe(ref m_orderedSubscriptions, this, a_acceptStaleMessages));    
                return this;
            }

            public void Unsubscribe()
            {
                ThreadPool.QueueUserWorkItem((object a) => Mediator.Unsubscribe(ref m_orderedSubscriptions, this));
            }

            public int CompareTo(Subscription sub)
            {
                return sub.m_priority - m_priority;
            }
        }
    }
}
