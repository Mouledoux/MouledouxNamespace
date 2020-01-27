namespace Mouledoux.Components
{
    /// <summary>
    /// Static class for all mediation.
    /// </summary>
    public sealed class Mediator
    {
        /// <summary>
        /// Dictionary of subscription strings and associated delegate callbacks
        /// </summary>
        private static System.Collections.Generic.Dictionary<string, System.Action<object[]>> subscriptions =
            new System.Collections.Generic.Dictionary<string, System.Action<object[]>>();

        /// <summary>
        /// List of messages held to be re-broadcated once something is subscribed to it
        /// </summary>
        private static System.Collections.Generic.List<string> holdMessages = 
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
        public static bool CheckForSubscription(string message)
        {
            return subscriptions.ContainsKey(message);
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
        public static int NotifySubscribers(string message, object[] args = null, bool holdMessage = false)
        {
            message = message.ToLower();
            
            // Temporary delegate container for modifying subscription delegates 
            System.Action<object[]> cb;

            // Makes sure the datapack has been set to something, even if one isn't provided
            args = args == null ? new object[0] : args;

            if (subscriptions.TryGetValue(message, out cb))
            {
                System.Delegate[] delegateList = cb.GetInvocationList();

                foreach (System.Action<object[]> d in delegateList)
                {
                    cb -= d.Target.Equals(null) ? d : null;
                }

                if(cb == null)
                {
                    subscriptions.Remove(message);
                }
                
                else
                {
                    subscriptions[message] = cb;
                    subscriptions[message].Invoke(args);
                    return 1;
                }
            }
            
            // If nothing is listening to the message, but it's been marked to hold
            else if(holdMessage && !holdMessages.Contains(message))
            {
                // add it to the hold list
                holdMessages.Add(message);
            }

            return 0;
        }




        /// ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ----------
        /// <summary>
        /// Base class for all entities that will be listening for broadcasts
        /// </summary>
        public sealed class Subscriptions
        {
            /// <summary>
            /// Personal, internal record of all active subscriptions
            /// </summary>
            private System.Collections.Generic.Dictionary<string, System.Action<object[]>> localSubscriptions =
                 new System.Collections.Generic.Dictionary<string, System.Action<object[]>>();





            /// ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ----------
            /// <summary>
            /// Links a custom delegate to a message in a SPECIFIC subscription dictionary
            /// </summary>
            /// <param name="container">Reference to the dictionary of subscriptions we want to modify</param>
            /// <param name="message">The message to subscribe to</param>
            /// <param name="callback">The delegate to be linked to the broadcast message</param>
            private void Subscribe(ref System.Collections.Generic.Dictionary<string, System.Action<object[]>> container, string message, System.Action<object[]> callback)
            {
                message = message.ToLower();

                // Temporary delegate container for modifying subscription delegates 
                System.Action<object[]> cb;

                // Check to see if there is not already a subscription to this message
                if (!container.TryGetValue(message, out cb))
                {
                    // If there is not, then make one with the message and currently empty callback delegate
                    container.Add(message, cb);
                }

                /// If the subscription does already exist,
                /// then cb is populated with all associated delegates,
                /// if it does not, cb is empty.

                // Add the delegate to cb (new or populated)
                cb += callback;
                // Set the delegate linked to the message to cb
                container[message] = cb;
            }



            /// ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ----------
            /// <summary>
            /// Links a custom delegate to a message that may be breadcasted via a Publisher
            /// </summary>
            /// <param name="message">The message to subscribe to</param>
            /// <param name="callback">The delegate to be linked to the broadcast message</param>
            public void Subscribe(string message, System.Action<object[]> callback, bool acceptStaleMessages = false)
            {
                // First, adds the subscription to the internal records
                Subscribe(ref localSubscriptions, message, callback);
                // Then, adds the subscription to the public records
                Subscribe(ref Mediator.subscriptions, message, callback);


                if(acceptStaleMessages && Mediator.holdMessages.Contains(message))
                {
                    Mediator.holdMessages.Remove(message);
                    callback.Invoke(null);
                }
            }



            /// ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ----------
            /// <summary>
            /// Unlinks a custom delegate from a message in a SPECIFIC subscription dictionary
            /// </summary>
            /// <param name="container">Reference to the dictionary of subscriptions we want to modify</param>
            /// <param name="message">The message to unsubscribe from</param>
            /// <param name="callback">The delegate to be removed from the broadcast message</param>
            private void Unsubscribe(ref System.Collections.Generic.Dictionary<string, System.Action<object[]>> container, string message, System.Action<object[]> callback)
            {
                message = message.ToLower();
                
                // Temporary delegate container for modifying subscription delegates 
                System.Action<object[]> cb;

                // Check to see if there is a subscription to this message
                if (container.TryGetValue(message, out cb))
                {
                    /// If the subscription does already exist,
                    /// then cb is populated with all associated delegates.
                    /// Otherwise nothing will happen

                    // Remove the selected delegate from the callback
                    cb -= callback;

                    // Check the modified cb to see if there are any delegates left
                    if (cb == null)
                    {
                        // If there is not, then remove the subscription completely
                        container.Remove(message);
                    }
                    else
                    {
                        // If there are some left, reset the callback to the now lesser cb
                        container[message] = cb;
                    }
                }
            }



            /// ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ----------
            /// <summary>
            /// Unlinks a custom delegate from a message that may be breadcasted via a Publisher
            /// </summary>
            /// <param name="message">The message to unsubscribe from</param>
            /// <param name="callback">The delegate to be unlinked from the broadcast message</param>
            public void Unsubscribe(string message, System.Action<object[]> callback)
            {
                message = message.ToLower();

                // First, remove the subscription from the internal records
                Unsubscribe(ref localSubscriptions, message, callback);
                // Then, remove the subscription from the public records
                Unsubscribe(ref Mediator.subscriptions, message, callback);
            }


            /// <summary>
            /// Unlinks all (local) delegates from given broadcast message
            /// </summary>
            /// <param name="message">The message to unsubscribe from</param>
            public void UnsubscribeAllFrom(string message)
            {
                message = message.ToLower();

                Unsubscribe(message, localSubscriptions[message]);
            }



            /// ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ----------
            /// <summary>
            /// Unlinks all (local) delegates from every (local) broadcast message
            /// </summary>
            public void UnsubscribeAll()
            {
                foreach (string message in localSubscriptions.Keys)
                {
                    Unsubscribe(ref Mediator.subscriptions, message, localSubscriptions[message]);
                }

                localSubscriptions.Clear();
            }

            ~Subscriptions()
            {
                UnsubscribeAll();
            }
        }
    }
}
