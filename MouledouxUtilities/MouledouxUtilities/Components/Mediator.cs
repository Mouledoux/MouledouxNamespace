namespace Mouledoux.Components
{
    /// <summary>
    /// Static class for all mediation.
    /// </summary>
    public sealed class Mediator
    {
        public enum SubscriptionPriority
        {
            DEFAULT,
            EARLY,
            LATE,
        }
        /// <summary>
        /// Dictionary of subscription strings and associated delegate callbacks to be called first on message broadcast
        /// </summary>
        private static System.Collections.Generic.Dictionary<string, System.Action<object[]>> m_earlySubscriptions =
            new System.Collections.Generic.Dictionary<string, System.Action<object[]>>();

        /// <summary>
        /// Dictionary of subscription strings and associated delegate callbacks to be called after earlySubscriptions
        /// </summary>
        private static System.Collections.Generic.Dictionary<string, System.Action<object[]>> m_primeSubscriptions =
            new System.Collections.Generic.Dictionary<string, System.Action<object[]>>();

        /// <summary>
        /// Dictionary of subscription strings and associated delegate callbacks to be called last on message broadcast
        /// </summary>
        private static System.Collections.Generic.Dictionary<string, System.Action<object[]>> m_lateSubscriptions =
            new System.Collections.Generic.Dictionary<string, System.Action<object[]>>();

        /// <summary>
        /// List of messages held to be re-broadcated once something is subscribed to it
        /// </summary>
        private static System.Collections.Generic.List<string> m_holdMessages = 
        new System.Collections.Generic.List<string>();





        /// ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ----------
        /// <summary>
        /// Checks if a subscription message exist WITHOUT invoking it
        /// </summary>
        /// 
        /// <param name="message">The message to be checked</param>
        ///
        /// <returns>
        /// Ture/ False if the message exist
        /// </returns>
        public static bool CheckForSubscription(string a_message)
        {
            return m_primeSubscriptions.ContainsKey(a_message)
                || m_earlySubscriptions.ContainsKey(a_message)
                || m_lateSubscriptions.ContainsKey(a_message);
        }


        private static bool ValidateSubscriptionCallbacks(ref System.Collections.Generic.Dictionary<string, System.Action<object[]>> a_subs, string a_message)
        {
            // Temporary delegate container for modifying subscription delegates 
            System.Action<object[]> cb;

            if (a_subs.TryGetValue(a_message, out cb))
            {
                System.Delegate[] delegateList = cb.GetInvocationList();

                foreach (System.Action<object[]> del in delegateList)
                {
                    cb -= del.Target.Equals(null) ? del : null;
                }

                if (cb == null)
                {
                    a_subs.Remove(a_message);
                    return false;
                }

                else
                {
                    a_subs[a_message] = cb;
                }
            }
            return true;
        }


        private static bool TryInvokeSubscription(ref System.Collections.Generic.Dictionary<string, System.Action<object[]>> a_subs, string a_message, object[] a_args)
        {
            if (ValidateSubscriptionCallbacks(ref a_subs, a_message))
            {
                a_subs[a_message].Invoke(a_args);
                return true;
            }
            else
            {
                return false;
            }
        }


        /// ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ----------
        /// <summary>
        /// Checks to see if their are any Subscribers to the broadcasted message
        /// and invokes ALL callbacks associated with it
        /// </summary>
        /// 
        /// <param name="message">The message to be broadcasted (case sensitive)</param>
        /// <param name="args">Object array of information to be used by ALL receiving parties</param> 
        /// <param name="holdMessage">Should this message be held if there are no active subscriptions</param>   
        /// 
        /// <returns>
        /// 1 the message was broadcasted successfully
        /// 0 there are no active subscriptions
        /// </returns>
        public static int NotifySubscribers(string a_message, object[] a_args = null, bool a_holdMessage = false)
        {
            a_message = a_message.ToLower();
            

            // Makes sure the object array has been set to something, even if one isn't provideds
            a_args = a_args == null ? new object[0] : a_args;

            bool messageBroadcasted =
                TryInvokeSubscription(ref m_earlySubscriptions, a_message, a_args) |
                TryInvokeSubscription(ref m_primeSubscriptions, a_message, a_args) |
                TryInvokeSubscription(ref m_lateSubscriptions, a_message, a_args);
            
            // If nothing is listening to the message, but it's been marked to hold
            if(!messageBroadcasted && a_holdMessage && !m_holdMessages.Contains(a_message))
            {
                // add it to the hold list
                m_holdMessages.Add(a_message);
            }

            return 0;
        }



        /// ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ----------
        /// <summary>
        /// Links a custom delegate to a message in a SPECIFIC subscription dictionary
        /// </summary>
        /// <param name="container">Reference to the dictionary of subscriptions we want to modify</param>
        /// <param name="message">The message to subscribe to</param>
        /// <param name="callback">The delegate to be linked to the broadcast message</param>
        private static void Subscribe(ref System.Collections.Generic.Dictionary<string, System.Action<object[]>> a_container, string a_message, System.Action<object[]> a_callback)
        {
            a_message = a_message.ToLower();

            // Temporary delegate container for modifying subscription delegates 
            System.Action<object[]> cb;

            // Check to see if there is not already a subscription to this message
            if (!a_container.TryGetValue(a_message, out cb))
            {
                // If there is not, then make one with the message and currently empty callback delegate
                a_container.Add(a_message, cb);
            }

            /// If the subscription does already exist,
            /// then cb is populated with all associated delegates,
            /// if it does not, cb is empty.

            // Add the delegate to cb (new or populated)
            cb += a_callback;
            // Set the delegate linked to the message to cb
            a_container[a_message] = cb;
        }



        /// ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ----------
        /// <summary>
        /// Unlinks a custom delegate from a message in a SPECIFIC subscription dictionary
        /// </summary>
        /// <param name="container">Reference to the dictionary of subscriptions we want to modify</param>
        /// <param name="message">The message to unsubscribe from</param>
        /// <param name="callback">The delegate to be removed from the broadcast message</param>
        private static void Unsubscribe(ref System.Collections.Generic.Dictionary<string, System.Action<object[]>> a_container, string a_message, System.Action<object[]> a_callback)
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


        private static void UnsubscribeLocalFromMaster(ref System.Collections.Generic.Dictionary<string, System.Action<object[]>> a_localContainer, ref System.Collections.Generic.Dictionary<string, System.Action<object[]>> a_masterContainer)
        {
            foreach (string message in a_localContainer.Keys)
            {
                Unsubscribe(ref a_masterContainer, message, a_localContainer[message]);
            }

            a_localContainer.Clear();
        }




        /// ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ----------
        /// <summary>
        /// Base class for all entities that will be listening for broadcasts
        /// </summary>
        public sealed class Subscriptions
        {
            /// <summary>
            /// Personal, internal record of all early subscriptions
            /// </summary>
            private System.Collections.Generic.Dictionary<string, System.Action<object[]>> m_localEarlySubscriptions =
                 new System.Collections.Generic.Dictionary<string, System.Action<object[]>>();
            /// <summary>
            /// Personal, internal record of all prime subscriptions
            /// </summary>
            private System.Collections.Generic.Dictionary<string, System.Action<object[]>> m_localPrimeSubscriptions =
                 new System.Collections.Generic.Dictionary<string, System.Action<object[]>>();
            /// <summary>
            /// Personal, internal record of all late subscriptions
            /// </summary>
            private System.Collections.Generic.Dictionary<string, System.Action<object[]>> m_localLateSubscriptions =
                 new System.Collections.Generic.Dictionary<string, System.Action<object[]>>();




            /// ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ----------
            /// <summary>
            /// Links a custom delegate to a message that may be breadcasted via a Publisher
            /// </summary>
            /// <param name="message">The message to subscribe to</param>
            /// <param name="callback">The delegate to be linked to the broadcast message</param>
            public void Subscribe(string a_message, System.Action<object[]> a_callback, bool a_acceptStaleMessages = false, SubscriptionPriority a_priority = 0)
            {
                switch(a_priority)
                {
                    case SubscriptionPriority.EARLY:
                        Mediator.Subscribe(ref m_localEarlySubscriptions, a_message, a_callback);
                        Mediator.Subscribe(ref m_earlySubscriptions, a_message, a_callback);
                        break;
                    case SubscriptionPriority.DEFAULT:
                        Mediator.Subscribe(ref m_localPrimeSubscriptions, a_message, a_callback);
                        Mediator.Subscribe(ref m_primeSubscriptions, a_message, a_callback);
                        break;
                    case SubscriptionPriority.LATE:
                        Mediator.Subscribe(ref m_localLateSubscriptions, a_message, a_callback);
                        Mediator.Subscribe(ref m_lateSubscriptions, a_message, a_callback);
                        break;
                }


                if (a_acceptStaleMessages && m_holdMessages.Contains(a_message))
                {
                    m_holdMessages.Remove(a_message);
                    a_callback.Invoke(null);
                }
            }


            /// ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ----------
            /// <summary>
            /// Unlinks a custom delegate from a message that may be breadcasted via a Publisher
            /// </summary>
            /// <param name="message">The message to unsubscribe from</param>
            /// <param name="callback">The delegate to be unlinked from the broadcast message</param>
            public void Unsubscribe(string a_message, System.Action<object[]> a_callback)
            {
                a_message = a_message.ToLower();

                Mediator.Unsubscribe(ref m_localEarlySubscriptions, a_message, a_callback);
                Mediator.Unsubscribe(ref m_earlySubscriptions, a_message, a_callback);

                Mediator.Unsubscribe(ref m_localPrimeSubscriptions, a_message, a_callback);
                Mediator.Unsubscribe(ref m_primeSubscriptions, a_message, a_callback);

                Mediator.Unsubscribe(ref m_localLateSubscriptions, a_message, a_callback);
                Mediator.Unsubscribe(ref m_lateSubscriptions, a_message, a_callback);
            }




            /// <summary>
            /// Unlinks all (local) delegates from given broadcast message
            /// </summary>
            /// <param name="message">The message to unsubscribe from</param>
            public void UnsubscribeAllFrom(string a_message)
            {
                a_message = a_message.ToLower();

                Unsubscribe(a_message, m_localEarlySubscriptions[a_message]);
                Unsubscribe(a_message, m_localPrimeSubscriptions[a_message]);
                Unsubscribe(a_message, m_localLateSubscriptions[a_message]);
            }

        

            /// ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ----------
            /// <summary>
            /// Unlinks all (local) delegates from every (local) broadcast message
            /// </summary>
            public void UnsubscribeAll()
            {
                UnsubscribeLocalFromMaster(ref m_localEarlySubscriptions, ref m_earlySubscriptions);
                UnsubscribeLocalFromMaster(ref m_localPrimeSubscriptions, ref m_primeSubscriptions);
                UnsubscribeLocalFromMaster(ref m_localLateSubscriptions, ref m_lateSubscriptions);
            }

            ~Subscriptions()
            {
                UnsubscribeAll();
            }
        }
    }
}
