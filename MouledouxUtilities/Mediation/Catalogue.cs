using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Mouledoux.Mediation
{
    public static class Catalogue<TKey, TArg> where TKey : IEquatable<TKey>
    {
        /// <summary>
        /// Messages and their associated subscriptions
        /// </summary>
        private static Dictionary<TKey, List<Subscription>> m_subscriptions =
            new Dictionary<TKey, List<Subscription>>();

        /// <summary>
        /// Messages that had no subscriptions at broadcast, but were marked for hold
        /// </summary>
        private static HashSet<TKey> m_staleMessages = new HashSet<TKey>();


        public static Action OnSubAdded = default;
        public static Action OnSubRemoved = default;
        public static Action OnCatalogueEmpty = default;


        /// <summary>
        /// Broadcast a message to potential subscribers, and invokes callbacks on a seperate thread
        /// </summary>
        /// <param name="a_subKey">message to broadcast</param>
        /// <param name="a_args">arguments to pass to subscription callbacks</param>
        /// <param name="a_holdMessage">if there are no active subscriptions, rebroadcast when one subscribes</param>
        public static Task NotifySubscribersAsync(TKey a_subKey, TArg a_arg = default, bool a_holdMessage = false)
        {
            return Task.Run(() => NotifySubscribers(a_subKey, a_arg, a_holdMessage));
        }


        /// <summary>
        /// Broadcast a message to potential subscribers
        /// </summary>
        /// <param name="a_subKey">message to broadcast</param>
        /// <param name="a_args">arguments to pass to subscription callbacks</param>
        /// <param name="a_holdMessage">if there are no active subscriptions, rebroadcast when one subscribes</param>
        public static void NotifySubscribers(TKey a_subKey, TArg a_arg = default, bool a_holdMessage = false)
        {
            bool messageBroadcasted = TryInvokeSubscription(a_subKey, a_arg);

            if (!messageBroadcasted && a_holdMessage)
            {
                m_staleMessages.Add(a_subKey);
            }
        }


        /// <summary>
        /// Checks if a subscription message exist
        /// </summary>
        /// <param name="a_subKey">subscription message to check</param>
        /// <returns>returns true if the message exist</returns>
        public static bool CheckForSubscription(TKey a_subKey)
        {
            return m_subscriptions.ContainsKey(a_subKey);
        }


        /// <summary>
        /// Invokes all valid callbacks subscribed to a message
        /// </summary>
        /// <param name="a_subKey">message to be broadcasted</param>
        /// <param name="a_args">arguments to pass to the callback</param>
        /// <returns>returns true if the broadcast was successful</returns>
        private static bool TryInvokeSubscription(TKey a_subKey, TArg a_arg)
        {
            if (ValidateSubscriptionCallbacks(a_subKey))
            {
                return m_subscriptions[a_subKey].All(sub =>
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
        /// <param name="a_subKey">message to validate callbacks under</param>
        /// <returns>return true if valid callbacks >= 1</returns>
        private static bool ValidateSubscriptionCallbacks(TKey a_subKey)
        {
            if (m_subscriptions.ContainsKey(a_subKey))
            {
                RemoveCorruptSubscriptions(a_subKey);

                return m_subscriptions[a_subKey].Count > 0 || !RemoveSubscriptionMessage(a_subKey);
            }
            return false;
        }


        /// <summary>
        /// A 'corrupted' sub, is a sub with a null target object
        /// </summary>
        /// <param name="a_subKey"></param>
        private static void RemoveCorruptSubscriptions(TKey a_subKey)
        {
            if (m_subscriptions.TryGetValue(a_subKey, out List<Subscription> subs))
            {
                for (int i = 0; i < subs.Count; i++)
                {
                    try
                    {
                        List<Action<TArg>> badSubs = subs[i].Callback.GetInvocationList().
                            Where((Delegate del) => { return del.Target.Equals(null); }).
                            ToList() as List<Action<TArg>>;
                        

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

        
        private static bool RemoveSubscriptionMessage(TKey a_subKey)
        {
            bool success = false;
            if (m_subscriptions.ContainsKey(a_subKey))
            {
                m_subscriptions.Remove(a_subKey);
                success = true;

                if (m_subscriptions.Count == 0)
                {
                    OnCatalogueEmpty?.Invoke();
                }
            }

            return success;
        }



        private static void Subscribe(TKey a_subKey, Action<TArg> a_callback, int a_priority = 0, bool a_acceptStaleMesages = false)
        {
            Subscription sub = new Subscription(a_subKey, a_callback, a_priority);
            Subscribe(sub, a_acceptStaleMesages);
        }

        private static void Subscribe(Subscription a_sub, bool a_acceptStaleMesages = false)
        {
            TKey message = a_sub.SubKey;

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
            TKey message = a_sub.SubKey;

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



        private static readonly object _prioritySortLocker;
        private static void SortSubsciptionsByPriorityAsync(TKey a_subKey)
        {
            lock (_prioritySortLocker)
            {
                Task.Run(() => m_subscriptions[a_subKey]?.Sort());
            }
        }


        public sealed class Subscription : IComparable<Subscription>
        {
            private TKey _subKey;
            private int _priority;

            public Action<TArg> Callback { get; set; }

            public TKey SubKey
            {
                get => _subKey;
                set
                {
                    Unsubscribe();
                    _subKey = value;
                    Subscribe();
                }
            }

            public int Priority
            {
                get => _priority;
                set
                {
                    _priority = value;
                    SortSubsciptionsByPriorityAsync(_subKey);
                }
            }


            public Subscription(TKey a_subKey, Action<TArg> a_callback, int a_priority = 0)
            {
                _subKey = a_subKey;
                _priority = a_priority;
                Callback = a_callback;
            }

            public Subscription Subscribe(bool a_acceptStaleMessages = false)
            {
                Catalogue<TKey, TArg>.Subscribe(this, a_acceptStaleMessages);
                return this;
            }

            public void Unsubscribe()
            {
                Catalogue<TKey, TArg>.Unsubscribe(this);
            }

            public int CompareTo(Subscription sub)
            {
                return sub.Priority - Priority;
            }
        }
    }
}
