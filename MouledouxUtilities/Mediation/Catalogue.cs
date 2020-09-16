using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Mouledoux.Mediation.Components
{
    public static class Catalogue<T>
    {
        /// <summary>
        /// Messages and their associated subscriptions
        /// </summary>
        private static Dictionary<string, List<Subscription>> m_subscriptions =
            new Dictionary<string, List<Subscription>>();
        
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

            bool messageBroadcasted = TryInvokeSubscription(a_message, a_arg);

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
        /// <param name="a_message">message to validate callbacks under</param>
        /// <returns>return true if valid callbacks >= 1</returns>
        private static bool ValidateSubscriptionCallbacks(string a_message)
        {
            // get all the subscriptions to a single message
            if (m_subscriptions.TryGetValue(a_message, out List<Subscription> tSub))
            {
                // for all the subs to the message
                for (int i = 0; i < tSub.Count; i++)
                {
                    try
                    {
                        // and for each action in each sub
                        foreach (Action<T> del in tSub[i].Callback.GetInvocationList())
                        {
                            // if the action has no valid targets, remove it
                            tSub[i].Callback -= del.Target.Equals(null) ? del : default;
                        }

                        // if there are no actions left on the sub
                        if (tSub[i].Callback == null)
                        {
                            // remove the sub from the mesasge
                            // and accomidate for the sub list loosing 1
                            m_subscriptions[a_message].RemoveAt(i);
                            i--;
                        }

                        else
                        {
                            // apply the remang actions to the sub
                            m_subscriptions[a_message][i] = tSub[i];
                        }
                    }

                    // catch if the sub trigger a null ref
                    catch (NullReferenceException)
                    {
                        // remove it completely
                        // and accomidate for sub list loosing 1
                        tSub.RemoveAt(i);
                        i--;
                    }
                }

                // return true if the message has any remaining valid subs
                if (m_subscriptions[a_message].Count > 0)
                {
                    return true;
                }
                // else, remove the message subscription
                else
                {
                    m_subscriptions.Remove(a_message);
                }
            }

            // there are no subscriptions to that mesasge
            return false;
        }


        /// <summary>
        /// Invokes all valid callbacks subscribed to a message
        /// </summary>
        /// <param name="a_message">message to be broadcasted</param>
        /// <param name="a_args">arguments to pass to the callback</param>
        /// <returns>returns true if the broadcast was successful</returns>
        private static bool TryInvokeSubscription(string a_message, T a_arg)
        {
            if (ValidateSubscriptionCallbacks(a_message))
            {
                foreach (Subscription sub in m_subscriptions[a_message])
                {
                    sub.Callback.Invoke(a_arg);
                }
                return true;
            }
            else
            {
                return false;
            }
        }





        private static void Subscribe(string a_message, Action<T> a_callback, int a_priority = 0)
        {
            a_message = a_message.ToLower();

            Subscription sub = new Subscription(a_message, a_callback, a_priority);
            Subscribe(sub);
        }


        private static void Subscribe(Subscription a_sub, bool a_acceptStaleMesages = false)
        {
            string message = a_sub.Message.ToLower();

            if (!m_subscriptions.TryGetValue(message, out List<Subscription> tSub))
            {
                tSub = new List<Subscription>();
                m_subscriptions.Add(message, tSub);

                if (a_acceptStaleMesages && m_staleMessages.Contains(message))
                {
                    m_staleMessages.Remove(message);
                    a_sub.Callback.Invoke(default);
                }
            }

            tSub.Add(a_sub);
            m_subscriptions[message] = tSub;
            m_subscriptions[message].Sort();
        }


        private static void Unsubscribe(Subscription a_sub)
        {
            string message = a_sub.Message.ToLower();

            if (m_subscriptions.TryGetValue(message, out List<Subscription> tSub))
            {
                tSub.Remove(a_sub);

                if (tSub.Count == 0)
                {
                    m_subscriptions.Remove(message);
                }
                else
                {
                    m_subscriptions[message] = tSub;
                }
            }
        }





        public sealed class Subscription : IComparable<Subscription>
        {
            private string _message;
            private int _priority;
            private Action<T> _callback;

            public string Message
            {
                get => _message;
                set
                {
                    Unsubscribe();
                    _message = value;
                    Subscribe();
                }
            }

            public int Priority
            {
                get => _priority;
                set
                {
                    _priority = value;
                    m_subscriptions[Message].Sort();
                }
            }

            public Action<T> Callback
            {
                get => _callback;
                set => _callback = value;
            }

            public Subscription(string a_message, Action<T> a_callback, int a_priority = 0)
            {
                _message = a_message;
                _priority = a_priority;
                _callback = a_callback;
            }

            public Subscription Subscribe(bool a_acceptStaleMessages = false)
            {
                Catalogue<T>.Subscribe(this, a_acceptStaleMessages);
                return this;
            }

            public void Unsubscribe()
            {
                Task unsubTask = Task.Run(() =>
                   Catalogue<T>.Unsubscribe(this));
            }

            public int CompareTo(Subscription sub)
            {
                return sub.Priority - Priority;
            }
        }
    }
}
