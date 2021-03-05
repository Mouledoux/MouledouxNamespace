using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Mouledoux.Mediation
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
        private static HashSet<string> m_staleMessages = new HashSet<string>();


        public static Action OnSubAdded = default;
        public static Action OnSubRemoved = default;
        public static Action OnCatalogueEmpty = default;


        /// <summary>
        /// Broadcast a message to potential subscribers, and invokes callbacks on a seperate thread
        /// </summary>
        /// <param name="a_message">message to broadcast</param>
        /// <param name="a_args">arguments to pass to subscription callbacks</param>
        /// <param name="a_holdMessage">if there are no active subscriptions, rebroadcast when one subscribes</param>
        public static Task NotifySubscribersAsync(string a_message, T a_arg = default, bool a_holdMessage = false)
        {
            return Task.Run(() => NotifySubscribers(a_message, a_arg, a_holdMessage));
        }


        /// <summary>
        /// Broadcast a message to potential subscribers
        /// </summary>
        /// <param name="a_message">message to broadcast</param>
        /// <param name="a_args">arguments to pass to subscription callbacks</param>
        /// <param name="a_holdMessage">if there are no active subscriptions, rebroadcast when one subscribes</param>
        public static void NotifySubscribers(string a_message, T a_arg = default, bool a_holdMessage = false)
        {
            bool messageBroadcasted = TryInvokeSubscription(a_message, a_arg);

            if (!messageBroadcasted && a_holdMessage)
            {
                m_staleMessages.Add(a_message);
            }
        }


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
        /// Invokes all valid callbacks subscribed to a message
        /// </summary>
        /// <param name="a_message">message to be broadcasted</param>
        /// <param name="a_args">arguments to pass to the callback</param>
        /// <returns>returns true if the broadcast was successful</returns>
        private static bool TryInvokeSubscription(string a_message, T a_arg)
        {
            if (ValidateSubscriptionCallbacks(a_message))
            {
                return m_subscriptions[a_message].All(sub =>
                {
                    sub.Callback.Invoke(a_arg);
                    return true;
                });
            }
            else
            {
                return false;
            }
        }


        /// <summary>
        /// Checks subscription associated callbacks for null ref errors
        /// Removes any that are not validated
        /// </summary>
        /// <param name="a_message">message to validate callbacks under</param>
        /// <returns>return true if valid callbacks >= 1</returns>
        private static bool ValidateSubscriptionCallbacks(string a_message)
        {
            if (m_subscriptions.ContainsKey(a_message))
            {
                RemoveCorruptSubscriptions(a_message);

                return m_subscriptions[a_message].Count > 0 || !RemoveSubscriptionMessage(a_message);
            }
            return false;
        }


        /// <summary>
        /// A 'corrupted' sub, is a sub with a null target object
        /// </summary>
        /// <param name="a_message"></param>
        private static void RemoveCorruptSubscriptions(string a_message)
        {
            if (m_subscriptions.TryGetValue(a_message, out List<Subscription> subs))
            {
                for (int i = 0; i < subs.Count; i++)
                {
                    try
                    {
                        List<Action<T>> badSubs = subs[i].Callback.GetInvocationList().
                            Where((Delegate del) => { return del.Target.Equals(null); }).
                            ToList() as List<Action<T>>;
                        

                        while(badSubs.Count() > 0)
                        {
                            subs[i].Callback -= badSubs[0];
                            badSubs.RemoveAt(0);
                        }
                    }
                    
                    catch (NullReferenceException)
                    {
                        subs.RemoveAt(i--);
                    }
                }
            }
        }

        
        private static bool RemoveSubscriptionMessage(string a_message)
        {
            bool success = false;
            if (m_subscriptions.ContainsKey(a_message))
            {
                m_subscriptions.Remove(a_message);
                success = true;

                if (m_subscriptions.Count == 0)
                {
                    OnCatalogueEmpty?.Invoke();
                }
            }

            return success;
        }



        private static void Subscribe(string a_message, Action<T> a_callback, int a_priority = 0, bool a_acceptStaleMesages = false)
        {
            Subscription sub = new Subscription(a_message, a_callback, a_priority);
            Subscribe(sub, a_acceptStaleMesages);
        }

        private static void Subscribe(Subscription a_sub, bool a_acceptStaleMesages = false)
        {
            string message = a_sub.Message;

            if (!m_subscriptions.TryGetValue(message, out List<Subscription> tSub))
            {
                tSub = new List<Subscription>();
                m_subscriptions.Add(message, tSub);
                OnSubAdded?.Invoke();

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
            string message = a_sub.Message;

            if (m_subscriptions.TryGetValue(message, out List<Subscription> tSub))
            {
                tSub.Remove(a_sub);
                OnSubRemoved?.Invoke();

                if (tSub.Count == 0)
                {
                    RemoveSubscriptionMessage(message);
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
                    _message = value.ToLower();
                    Subscribe();
                }
            }

            public int Priority
            {
                get => _priority;
                set
                {
                    _priority = value;
                    Task.Run(() => m_subscriptions[Message].Sort());
                }
            }

            public Action<T> Callback
            {
                get => _callback;
                set => _callback = value;
            }

            public Subscription(string a_message, Action<T> a_callback, int a_priority = 0)
            {
                _message = a_message.ToLower();
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
                Catalogue<T>.Unsubscribe(this);
            }

            public int CompareTo(Subscription sub)
            {
                return sub.Priority - Priority;
            }
        }
    }
}
