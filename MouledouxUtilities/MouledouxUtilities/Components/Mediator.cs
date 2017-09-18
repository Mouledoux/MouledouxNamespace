namespace Mouledoux.Components
{
    /// <summary>
    /// Static class for all mediation.
    /// </summary>
    public sealed class Mediator
    {
        /// The below code is a standard singleton
        #region Singleton
        private Mediator() { }

        private static Mediator _instance;

        public static Mediator instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new Mediator();
                }

                return _instance;
            }
        }
        #endregion Singleton


        /// <summary>
        /// Dictionary of subscription strings and associated delegate callbacks
        /// </summary>
        private System.Collections.Generic.Dictionary<string, Callback.Callback> subscriptions =
            new System.Collections.Generic.Dictionary<string, Callback.Callback>();


        /// <summary>
        /// Checks to see if their are any Subscribers to the broadcasted message
        /// and invokes ALL callbacks associated with it
        /// </summary>
        /// <param name="message">The message to be broadcasted (case sensitive)</param>
        /// <param name="data">Packet of information to be used by ALL recieving parties</param>
        public void NotifySubscribers(string message, Callback.Packet data)
        {
            // Temporary delegate container for modifying subscription delegates 
            Callback.Callback cb;

            // Check to see if the message has any valid subscriptions
            if (instance.subscriptions.TryGetValue(message, out cb))
            {
                // Invokes ALL associated delegates with the data Packet as the argument
                cb.Invoke(data);
            }
        }


        /// <summary>
        /// Base class for all entities that will be listing for broadcasts
        /// </summary>
        public sealed class Subscriptions
        {
            /// <summary>
            /// Personal, internal record of all active subscriptions
            /// </summary>
            private System.Collections.Generic.Dictionary<string, Callback.Callback> localSubscriptions =
                 new System.Collections.Generic.Dictionary<string, Callback.Callback>();

            /// <summary>
            /// Links a custom delegate to a message in a SPECIFIC subscription dictionary
            /// </summary>
            /// <param name="container">Refrence to the dictionary of subscriptions we want to modify</param>
            /// <param name="message">The message to subscribe to (case sensitive)</param>
            /// <param name="callback">The delegate to be linked to the broadcast message</param>
            private void Subscribe(ref System.Collections.Generic.Dictionary<string, Callback.Callback> container, string message, Callback.Callback callback)
            {
                // Temporary delegate container for modifying subscription delegates 
                Callback.Callback cb;

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


            /// <summary>
            /// Links a custom delegate to a message that may be breadcasted via a Publisher
            /// </summary>
            /// <param name="message">The message to subscribe to (case sensitive)</param>
            /// <param name="callback">The delegate to be linked to the broadcast message</param>
            public void Subscribe(string message, Callback.Callback callback)
            {
                // First, adds the subscription to the internaal records
                Subscribe(ref localSubscriptions, message, callback);
                // Then, adds the subcription to the public records
                Subscribe(ref instance.subscriptions, message, callback);
            }


            /// <summary>
            /// Unlinks a custom delegate from a message in a SPECIFIC subscription dictionary
            /// </summary>
            /// <param name="container">Refrence to the dictionary of subscriptions we want to modify</param>
            /// <param name="message">The message to unsubscribe from (case sensitive)</param>
            /// <param name="callback">The delegate to be removed from the broadcast message</param>
            public void Unsubscribe(ref System.Collections.Generic.Dictionary<string, Callback.Callback> container, string message, Callback.Callback callback)
            {
                // Temporary delegate container for modifying subscription delegates 
                Callback.Callback cb;

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
                        // If tere is not, then remove the subscription completely
                        container.Remove(message);
                    }
                    else
                    {
                        // If there are some left, reset the callback to the now lesser cb
                        container[message] = cb;
                    }
                }
            }


            /// <summary>
            /// Unlinks a custom delegate from a message that may be breadcasted via a Publisher
            /// </summary>
            /// <param name="message">The message to unsubscribe from (case sensitive)</param>
            /// <param name="callback">The delegate to be unlinked from the broadcast message</param>
            public void Unsubscribe(string message, Callback.Callback callback)
            {
                // First, remove the subscription from the internal records
                Unsubscribe(ref localSubscriptions, message, callback);
                // Then, remove the subcription from the public records
                Unsubscribe(ref instance.subscriptions, message, callback);
            }


            /// <summary>
            /// Unlinks all (local) delegates from given broadcast message
            /// </summary>
            /// <param name="message">The message to unsubscribe from (case sensitive)</param>
            public void UnsubcribeAllFrom(string message)
            {
                Unsubscribe(message, localSubscriptions[message]);
            }


            /// !!! IMPORTANT !!! ///
            /// The method below - UnsubscribeAll()
            /// MUST BE CALLED whenever a class inheriting from subscriber is removed
            /// If it is not, you WILL get NULL REFRENCE ERRORS

            /// <summary>
            /// Unlinks all (local) delegates from every (local) broadcast message
            /// </summary>
            public void UnsubscribeAll()
            {
                foreach (string message in localSubscriptions.Keys)
                {
                    Unsubscribe(ref instance.subscriptions, message, localSubscriptions[message]);
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
