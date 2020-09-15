using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Mouledoux.Components
{
    //public static class MediatorMediator
    //{
    //    private static Dictionary<string, HashSet<System.Type>> m_typedMessages =
    //        new Dictionary<string, HashSet<System.Type>>();

    //    public static void Subscribe<T>(string a_message, System.Action<T> a_callback, int a_priority = 0) where T : new()
    //    {
    //        System.Type type = typeof(T);

    //        if(m_typedMessages.ContainsKey(a_message))
    //        {
    //            m_typedMessages[a_message].Add(type);
    //        }
    //        else
    //        {
    //            m_typedMessages.Add(a_message,
    //                new HashSet<System.Type>() { typeof(object), type });
    //        }
    //    }

    //    public static void NotifySubscribers<T>(string a_message, T a_arg) where T : new()
    //    {
    //        foreach(System.Type type in m_typedMessages[a_message])
    //        {
    //            if (type is T)
    //            {
    //                Mediator<T>.NotifySubscribers(a_message, (T)a_arg);
    //            }
    //        }
    //    }
    //}




    public static class Catalogue<T>
    {
        /// <summary>
        /// Messages and their associated subscriptions
        /// </summary>
        private static Dictionary<string, List<Subscription>> m_subscriptions =
            new Dictionary<string, List<Subscription>>();

        private static Dictionary<string, HashSet<System.Type>> m_typedMessages =
            new Dictionary<string, HashSet<System.Type>>();


        /// <summary>
        /// Messages that had no subscriptions at broadcast, but were marked for hold
        /// </summary>
        private static List<string> m_staleMessages = new List<string>();
        



        /// <summary>
        /// Checks if a subscription message exist
        /// </summary>
        /// <param name="a_message">subscription message to check</param>
        /// <returns>returns true if the message exist</returns>
        public static bool CheckForSubscription(string a_message)
        {
            return m_subscriptions.ContainsKey(a_message);
        }


        /// <summary>
        /// Broadcast a message to potential subscribers
        /// </summary>
        /// <param name="a_message">message to broadcast</param>
        /// <param name="a_args">arguments to pass to subscription callbacks</param>
        /// <param name="a_holdMessage">if there are no active subscriptions, rebroadcast when one subscribes</param>
        public static void NotifySubscribers(string a_message, T a_arg = default, bool a_holdMessage = false)
        {
            a_message = a_message.ToLower();

            bool messageBroadcasted = TryInvokeSubscription(ref m_subscriptions, a_message, a_arg);

            // If nothing is listening to the message, but it's been marked to hold
            if (!messageBroadcasted && a_holdMessage && !m_staleMessages.Contains(a_message))
            {
                // add it to the hold list
                m_staleMessages.Add(a_message);
            }
        }


        /// <summary>
        /// Broadcast a message to potential subscribers, and invokes callbacks on a seperate thread
        /// </summary>
        /// <param name="a_message">message to broadcast</param>
        /// <param name="a_args">arguments to pass to subscription callbacks</param>
        /// <param name="a_holdMessage">if there are no active subscriptions, rebroadcast when one subscribes</param>
        public static void NotifySubscribersAsync(string a_message, T a_arg = default, bool a_holdMessage = false)
        {
            Task notifyTask = Task.Run(() =>
               NotifySubscribers(a_message, a_arg, a_holdMessage));
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
                        foreach (System.Action<T> del in tSub[i].m_callback.GetInvocationList())
                        {
                            // if the action has no valid targets, remove it
                            tSub[i].m_callback -= del.Target.Equals(null) ? del : default;
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
        private static bool TryInvokeSubscription(ref Dictionary<string, List<Subscription>> a_container, string a_message, T a_arg)
        {
            if (ValidateSubscriptionCallbacks(ref a_container, a_message))
            {
                foreach (Subscription sub in a_container[a_message])
                {
                    sub.m_callback.Invoke(a_arg);
                }
                return true;
            }
            else
            {
                return false;
            }
        }





        private static void Subscribe(ref Dictionary<string, List<Subscription>> a_container, string a_message, System.Action<T> a_callback, int a_priority = 0)
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
                    a_sub.m_callback.Invoke(default);
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
            public System.Action<T> m_callback;

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
                set
                {
                    _priority = value;
                    m_subscriptions[m_message].Sort();
                }
            }

            public Subscription(string a_message, System.Action<T> a_callback, int a_priority = 0)
            {
                _message = a_message;
                _priority = a_priority;
                m_callback = a_callback;
            }

            public Subscription Subscribe(bool a_acceptStaleMessages = false)
            {
                Task subTask = Task.Run( () =>
                    Mediator<T>.Subscribe(ref m_subscriptions, this, a_acceptStaleMessages));
                
                return this;
            }

            public void Unsubscribe()
            {
                Task unsubTask = Task.Run( () =>
                    Mediator<T>.Unsubscribe(ref m_subscriptions, this));
            }

            public int CompareTo(Subscription sub)
            {
                return sub.m_priority - m_priority;
            }
        }
    }
}