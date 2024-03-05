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
        private static Dictionary<TKey, TArg> m_staleMessages = new Dictionary<TKey, TArg>();


        public static Action<Subscription> OnSubAdded = default;
        public static Action<Subscription> OnSubRemoved = default;
        public static Action OnCatalogueEmpty = default;



        /// <summary>
        /// Broadcast a message to potential subscribers, and invokes callbacks on a seperate thread
        /// </summary>
        /// <param name="a_subKey">message to broadcast</param>
        /// <param name="a_args">arguments to pass to subscription callbacks</param>
        /// <param name="a_holdMessage">if there are no active subscriptions, rebroadcast when one subscribes</param>
        public static Task NotifySubscribersAsync(TKey a_subKey, TArg a_arg = default, bool a_holdMessage = false, bool a_safeNotify = true)
        {
            return Task.Run(() => NotifySubscribers(a_subKey, a_arg, a_holdMessage, a_safeNotify));
        }



        /// <summary>
        /// Broadcast a message to potential subscribers
        /// </summary>
        /// <param name="a_subKey">message to broadcast</param>
        /// <param name="a_args">arguments to pass to subscription callbacks</param>
        /// <param name="a_holdMessage">if there are no active subscriptions, rebroadcast when one subscribes</param>
        public static void NotifySubscribers(TKey a_subKey, TArg a_arg = default, bool a_holdMessage = false, bool a_safeNotify = true)
        {
            bool messageBroadcasted = a_safeNotify
                ? TryInvokeSubscription(a_subKey, a_arg) 
                : InvokeSubscriptionUnsafe(a_subKey, a_arg);

            if (!messageBroadcasted && a_holdMessage)
            {
                TryAddToStaleMessages(a_subKey, a_arg);
            }
        }


        private static bool TryAddToStaleMessages(TKey a_subKey, TArg a_arg)
        {
            if(!m_staleMessages.ContainsKey(a_sub))
            {
                m_staleMessages.Add(a_subKey, a_arg);
            }
            else
            {
                m_staleMessages[a_subKey] = a_arg;
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
            bool _results = false;

            if (ValidateSubscriptionCallbacks(a_subKey))
            {
                _results = InvokeSubscriptionUnsafe(a_subKey, a_arg);
            }

            return _results;
        }



        /// <summary>
        /// Invokes all valid callbacks subscribed to a message
        /// </summary>
        /// <param name="a_subKey">message to be broadcasted</param>
        /// <param name="a_args">arguments to pass to the callback</param>
        /// <returns>returns true if the broadcast was successful</returns>
        private static bool InvokeSubscriptionUnsafe(TKey a_subKey, TArg a_arg)
        {
            bool _results = m_subscriptions[a_subKey].All(sub =>
            {
                sub.Callback.Invoke(a_arg);
                return true;
            });

            return _results;
        }



        /// <summary>
        /// Checks subscription associated callbacks for null ref errors
        /// Removes any that are not valid
        /// </summary>
        /// <param name="a_subKey">message to validate callbacks under</param>
        /// <returns>return true if valid callbacks >= 1</returns>
        private static bool ValidateSubscriptionCallbacks(TKey a_subKey)
        {
            bool _results = false;

            if (CheckForSubscription(a_subKey))
            {
                RemoveCorruptSubscriptions(a_subKey);

                _results = m_subscriptions[a_subKey].Count > 0 || !RemoveSubscriptionMessage(a_subKey);
            }
            return _results;
        }



        /// <summary>
        /// A 'corrupt' sub, is a sub with a null target object
        /// </summary>
        /// <param name="a_subKey"></param>
        /// <returns>return true if any corrupted callbacks were removed</returns>
        private static bool RemoveCorruptSubscriptions(TKey a_subKey)
        {
            bool _results = false;

            if (m_subscriptions.TryGetValue(a_subKey, out List<Subscription> _subs))
            {
                for (int i = 0; i < _subs.Count; i++)
                {
                    try
                    {
                        List<Action<TArg>> _badSubs = _subs[i].Callback.GetInvocationList().
                            Where((Delegate _del) => { return _del.Target.Equals(null); }).
                            ToList() as List<Action<TArg>>;
                        
                        int _badSubCount = _badSubs.Count;
                        _results = _badSubCount > 0;

                        for(int j = _badSubCount; j > 0; j--)
                        { 
                            _subs[i].Callback -= _badSubs[j];
                        }

                        _badSubs.Clear();
                    }
                    
                    catch (NullReferenceException)
                    {
                        _subs.RemoveAt(i--);
                    }
                }
            }

            return _results;
        }


        
        private static bool RemoveSubscriptionMessage(TKey a_subKey)
        {
            bool _results = false;

            if (CheckForSubscription(a_subKey))
            {
                m_subscriptions.Remove(a_subKey);
                _results = true;

                if (m_subscriptions.Count == 0)
                {
                    OnCatalogueEmpty?.Invoke();
                }
            }

            return _results;
        }



        private static int Subscribe(TKey a_subKey, Action<TArg> a_callback, int a_priority = 0, bool a_acceptStaleMesages = false)
        {
            Subscription sub = new Subscription(a_subKey, a_callback, a_priority);
            return Subscribe(sub, a_acceptStaleMesages);
        }



        private static int Subscribe(Subscription a_sub, bool a_acceptStaleMesages = false)
        {
            int _results = 0;

            if(a_sub != null && a_sub != default)
            {
                TKey _message = a_sub.SubKey;
                List<Subscription> _tSub;

                bool _existingSub = m_subscriptions.TryGetValue(_message, out _tSub);

                if (_existingSub == false)
                {
                    _tSub = new List<Subscription>();
                    m_subscriptions.Add(_message, _tSub);

                    if (a_acceptStaleMesages && m_staleMessages.ContainsKey(_message))
                    {
                        TArg staleArg = m_staleMessages[_message];
                        a_sub.Callback?.Invoke(staleArg);
                        m_staleMessages.Remove(_message);
                    }
                }

                _tSub.Add(a_sub);
                _results = _tSub.Count;

                m_subscriptions[_message] = _tSub;
                m_subscriptions[_message].Sort();
                
                OnSubAdded?.Invoke(a_sub);
            }

            return _results;
        }



        private static bool Unsubscribe(Subscription a_sub)
        {
            bool _results = false;

            if(a_sub != null)
            {
                TKey _message = a_sub.SubKey;

                if (m_subscriptions.TryGetValue(_message, out List<Subscription> _tSub))
                {
                    _tSub.Remove(a_sub);

                    if (_tSub.Count == 0)
                    {
                        RemoveSubscriptionMessage(_message);
                    }
                    else
                    {
                        m_subscriptions[_message] = _tSub;
                    }

                    _results = true;
                    OnSubRemoved?.Invoke(a_sub);
                }
            }

            return _results;
        }



        public static int GetSubscriptionCount(TKey a_subKey)
        {
            int? _results = m_subscriptions[a_subKey]?.Count;

            return (int)(_results == null ? 0 : _results);
        }



        private static readonly object m_prioritySortLocker;
        private static void SortSubsciptionsByPriorityAsync(TKey a_subKey)
        {
            lock (m_prioritySortLocker)
            {
                Task.Run(() => m_subscriptions[a_subKey]?.Sort());
            }
        }





        public sealed class Subscription : IComparable<Subscription>
        {
            private TKey m_subKey;
            private int m_priority;
            public Action<TArg> Callback { get; set; }



            public TKey SubKey
            {
                get => m_subKey;
                set
                {
                    Unsubscribe();
                    m_subKey = value;
                    Subscribe();
                }
            }



            public int Priority
            {
                get => m_priority;
                set
                {
                    m_priority = value;
                    SortSubsciptionsByPriorityAsync(SubKey);
                }
            }



            public Subscription(TKey a_subKey, Action<TArg> a_callback, int a_priority = 0)
            {
                SubKey = a_subKey;
                Priority = a_priority;
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



            public int CompareTo(Subscription a_sub)
            {
                return a_sub.Priority - Priority;
            }
        }
    }
}
