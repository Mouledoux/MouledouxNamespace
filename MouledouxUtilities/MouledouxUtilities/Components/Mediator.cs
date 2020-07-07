﻿using System.Collections.Generic;

namespace Mouledoux.Components
{
    /// <summary>
    /// Static class for all mediation.
    /// </summary>
    public sealed class Mediator
    {
        private static Dictionary<string, List<Subscription>> m_orderedSubscriptions;


        /// <summary>
        /// List of messages held to be re-broadcated once something is subscribed to it
        /// </summary>
        private static List<string> m_staleMessages = 
        new List<string>();




        
        public static bool CheckForSubscription(string a_message)
        {
            return m_orderedSubscriptions.ContainsKey(a_message);
        }


        private static bool ValidateSubscriptionCallbacks(ref Dictionary<string, List<Subscription>> a_subs, string a_message)
        {
            List<Subscription> tSub;

            // get all the subscriptions to a single message
            if (a_subs.TryGetValue(a_message, out tSub))
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
                            a_subs[a_message].RemoveAt(i);
                            i--;
                        }

                        else
                        {
                            // apply the remang actions to the sub
                            a_subs[a_message][i] = tSub[i];
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
            }

            // return true if the message has any remaining valid subs
            return a_subs[a_message].Count > 0;
        }


        private static bool TryInvokeSubscription(ref Dictionary<string, List<Subscription>> a_subs, string a_message, object[] a_args)
        {
            if (ValidateSubscriptionCallbacks(ref a_subs, a_message))
            {
                foreach(Subscription sub in a_subs[a_message])
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

        
        public static int NotifySubscribers(string a_message, object[] a_args = null, bool a_holdMessage = false)
        {
            a_message = a_message.ToLower();
            

            // Makes sure the object array has been set to something, even if one isn't provideds
            a_args = a_args == null ? new object[0] : a_args;

            bool messageBroadcasted = TryInvokeSubscription(ref m_orderedSubscriptions, a_message, a_args);
            
            // If nothing is listening to the message, but it's been marked to hold
            if(!messageBroadcasted && a_holdMessage && !m_staleMessages.Contains(a_message))
            {
                // add it to the hold list
                m_staleMessages.Add(a_message);
            }

            return 0;
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



        private static void Unsubscribe(ref Dictionary<string, System.Action<object[]>> a_container, string a_message, System.Action<object[]> a_callback)
        {
            a_message = a_message.ToLower();

            // Temporary delegate container for modifying subscription delegates 
            System.Action<object[]> cb;

            // Check to see if there is a subscription to this message
            if (a_container.TryGetValue(a_message, out cb))
            {
                /// If the subscription does already exist,
                /// then cb is populated with all associated delegates.
                /// Otherwise nothing will happen

                // Remove the selected delegate from the callback
                cb -= a_callback;

                // Check the modified cb to see if there are any delegates left
                if (cb == null)
                {
                    // If there is not, then remove the subscription completely
                    a_container.Remove(a_message);
                }
                else
                {
                    // If there are some left, reset the callback to the now lesser cb
                    a_container[a_message] = cb;
                }
            }
        }


        private static void UnsubscribeLocalFromMaster(ref Dictionary<string, System.Action<object[]>> a_localContainer, ref Dictionary<string, System.Action<object[]>> a_masterContainer)
        {
            foreach (string message in a_localContainer.Keys)
            {
                Unsubscribe(ref a_masterContainer, message, a_localContainer[message]);
            }

            a_localContainer.Clear();
        }




        ///// ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ----------
        ///// <summary>
        ///// Base class for all entities that will be listening for broadcasts
        ///// </summary>
        //public sealed class Subscriptions
        //{
        //    /// <summary>
        //    /// Personal, internal record of all early subscriptions
        //    /// </summary>
        //    private Dictionary<string, System.Action<object[]>> m_localEarlySubscriptions =
        //         new Dictionary<string, System.Action<object[]>>();
        //    /// <summary>
        //    /// Personal, internal record of all prime subscriptions
        //    /// </summary>
        //    private Dictionary<string, System.Action<object[]>> m_localPrimeSubscriptions =
        //         new Dictionary<string, System.Action<object[]>>();
        //    /// <summary>
        //    /// Personal, internal record of all late subscriptions
        //    /// </summary>
        //    private Dictionary<string, System.Action<object[]>> m_localLateSubscriptions =
        //         new Dictionary<string, System.Action<object[]>>();




        //    /// ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ----------
        //    /// <summary>
        //    /// Links a custom delegate to a message that may be breadcasted via a Publisher
        //    /// </summary>
        //    /// <param name="message">The message to subscribe to</param>
        //    /// <param name="callback">The delegate to be linked to the broadcast message</param>
        //    public void Subscribe(string a_message, System.Action<object[]> a_callback, bool a_acceptStaleMessages = false, int a_priority = 0)
        //    {
        //        switch(a_priority)
        //        {
        //            case 0:
        //                Mediator.Subscribe(ref m_localEarlySubscriptions, a_message, a_callback);
        //                Mediator.Subscribe(ref m_earlySubscriptions, a_message, a_callback);
        //                break;
        //            case 1:
        //                Mediator.Subscribe(ref m_localPrimeSubscriptions, a_message, a_callback);
        //                Mediator.Subscribe(ref m_primeSubscriptions, a_message, a_callback);
        //                break;
        //            case 2:
        //                Mediator.Subscribe(ref m_localLateSubscriptions, a_message, a_callback);
        //                Mediator.Subscribe(ref m_lateSubscriptions, a_message, a_callback);
        //                break;

        //            default:
        //                break;
        //        }


        //        if (a_acceptStaleMessages && m_holdMessages.Contains(a_message))
        //        {
        //            m_holdMessages.Remove(a_message);
        //            a_callback.Invoke(null);
        //        }
        //    }


        //    /// ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ----------
        //    /// <summary>
        //    /// Unlinks a custom delegate from a message that may be breadcasted via a Publisher
        //    /// </summary>
        //    /// <param name="message">The message to unsubscribe from</param>
        //    /// <param name="callback">The delegate to be unlinked from the broadcast message</param>
        //    public void Unsubscribe(string a_message, System.Action<object[]> a_callback)
        //    {
        //        a_message = a_message.ToLower();

        //        Mediator.Unsubscribe(ref m_localEarlySubscriptions, a_message, a_callback);
        //        Mediator.Unsubscribe(ref m_earlySubscriptions, a_message, a_callback);

        //        Mediator.Unsubscribe(ref m_localPrimeSubscriptions, a_message, a_callback);
        //        Mediator.Unsubscribe(ref m_primeSubscriptions, a_message, a_callback);

        //        Mediator.Unsubscribe(ref m_localLateSubscriptions, a_message, a_callback);
        //        Mediator.Unsubscribe(ref m_lateSubscriptions, a_message, a_callback);
        //    }




        //    /// <summary>
        //    /// Unlinks all (local) delegates from given broadcast message
        //    /// </summary>
        //    /// <param name="message">The message to unsubscribe from</param>
        //    public void UnsubscribeAllFrom(string a_message)
        //    {
        //        a_message = a_message.ToLower();

        //        Unsubscribe(a_message, m_localEarlySubscriptions[a_message]);
        //        Unsubscribe(a_message, m_localPrimeSubscriptions[a_message]);
        //        Unsubscribe(a_message, m_localLateSubscriptions[a_message]);
        //    }

        

        //    /// ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ----------
        //    /// <summary>
        //    /// Unlinks all (local) delegates from every (local) broadcast message
        //    /// </summary>
        //    public void UnsubscribeAll()
        //    {
        //        UnsubscribeLocalFromMaster(ref m_localEarlySubscriptions, ref m_earlySubscriptions);
        //        UnsubscribeLocalFromMaster(ref m_localPrimeSubscriptions, ref m_primeSubscriptions);
        //        UnsubscribeLocalFromMaster(ref m_localLateSubscriptions, ref m_lateSubscriptions);
        //    }

        //    ~Subscriptions()
        //    {
        //        UnsubscribeAll();
        //    }
        //}






        public sealed class Subscription : System.IComparable<Subscription>
        {
            private string _message;
            private int _priority;
            public System.Action<object[]> m_callback;

            public string m_message
            {
                get => _message;
                set => _message = value;
            }

            public int m_priority
            {
                get => _priority;
                set => _priority = value;
            }

            public Subscription(string a_message, System.Action<object[]> a_callback, int a_priority = 0)
            {
                m_message = a_message;
                m_callback = a_callback;
                m_priority = a_priority;
            }

            public int CompareTo(Subscription sub)
            {
                return sub.m_priority - m_priority;
            }
        }
    }
}
