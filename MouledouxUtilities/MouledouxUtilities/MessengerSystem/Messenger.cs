namespace Mouledoux.Interaction
{
    /// <summary>
    /// Delegate type required to be used by the Publisher/Subscriber
    /// </summary>
    public delegate void Callback();

    public sealed class Messenger
    {
        #region Singleton
        private static Messenger m_instance = null;

        private Messenger() { }

        public static Messenger Instance
        {
            get
            {
                if (m_instance == null)
                    m_instance = new Messenger();

                return m_instance;
            }
        }
        #endregion


        /// <summary>
        /// Adds a Callback to a list of Callbacks to be executed on the broadcast of aMessage
        /// </summary>
        /// 
        /// <param name="aMessage">Message to listen for</param>
        /// <param name="aCallback">Callback to execute on message broadcast</param>
        /// 
        /// <returns>
        /// Returns 1 if the message and Callback were correctly added
        /// </returns>
        private int AddSubscriber(string aMessage, Callback aCallback)
        {
            if(Instance.Subsciptions.ContainsKey(aMessage))
                return 0;

            Instance.Subsciptions.Add(aMessage, aCallback);
            return 1;
        }


        /// <summary>
        /// 
        /// </summary>
        /// 
        /// <param name="aMessage"></param>
        /// <param name="aCallback"></param>
        /// 
        /// <returns>
        /// </returns>
        private int UpdateSubscriber(string aMessage, Callback aCallback)
        {
            if (!Instance.Subsciptions.ContainsKey(aMessage))
                return 0;

            Instance.Subsciptions[aMessage] += aCallback;
            return 1;
        }


        /// <summary>
        /// 
        /// </summary>
        /// 
        /// <param name="aMessage"></param>
        /// <param name="aCallback"></param>
        /// 
        /// <returns>
        /// </returns>
        private int Resubscribe(string aMessage, Callback aCallback)
        {
            if (!Instance.Subsciptions.ContainsKey(aMessage))
            {
                Instance.AddSubscriber(aMessage, aCallback);
                return 0;
            }

            Instance.Subsciptions[aMessage] = aCallback;
            return 1;
        }


        /// <summary>
        /// Removes a Callback from a subscribed message, while leaving the message in the list of subscriptions
        /// </summary>
        /// 
        /// <param name="aMessage">Message to unsubscribe from</param>
        /// <param name="aCallback">Callback to remove from subscription</param>
        /// 
        /// <returns>
        /// Returns 1 if the Callback was removed,
        /// and 0 if the message was not subscribed to
        /// </returns>
        private int RemoveSubscriber(string aMessage, Callback aCallback)
        {
            if (!Instance.Subsciptions.ContainsKey(aMessage))
                return 0;

            Instance.Subsciptions[aMessage] -= aCallback;
            return 1;
        }


        /// <summary>
        /// Broadcast a message to the list of subscriptions
        /// </summary>
        /// 
        /// <param name="aMessage">Message to broadcast</param>
        /// 
        /// <returns>
        /// Returns 1 if the broadcast was successful,
        /// and 0 if the message was never subscribed to
        /// </returns>
        private int BroadcastMessage(string aMessage)
        {
            if (!Instance.Subsciptions.ContainsKey(aMessage))
                return 0;

            Instance.Subsciptions[aMessage].Invoke();
            return 1;
        }

        
        /// <summary>
        /// Dictionary of messages subscribed to, and Callbacks to run on message broadcast
        /// </summary>
        private System.Collections.Generic.Dictionary<string, Callback> Subsciptions =
            new System.Collections.Generic.Dictionary<string, Callback>();


        #region Subclasses
        /// <summary>
        /// Objects that inherit from this class can globally broadcast messages to subscribers
        /// </summary>
        public class Publisher
        {
            /// <summary>
            /// Broadcast a message for the Messenger class to 
            /// </summary>
            /// 
            /// <param name="aMessage">Message to broadcast</param>
            /// 
            /// <returns>
            /// Returns the result of Messenger.BroadcastMessage
            /// </returns>
            public int Broadcast(string aMessage)
            {
                return Instance.BroadcastMessage(aMessage);
            }
        }

        /// <summary>
        /// Objects that inherit from this class can subscribe to globally broadcasted messages
        /// </summary>
        public class Subscriber
        {
            /// <summary>
            /// Sets a Callback to be executed on the broadcast of aMessage
            /// </summary>
            /// 
            /// <param name="aMessage">Message to listen for</param>
            /// <param name="aCallback">Callback to execute on message broadcast</param>
            /// 
            /// <returns>
            /// Returns the result of Messenger.AddSubscriber
            /// </returns>
            public int Subscribe(string aMessage, Callback aCallback)
            {
                return Instance.AddSubscriber(aMessage, aCallback);
            }


            /// <summary>
            /// Removes a Callback from a subscribed message, while leaving the message in the list of subscriptions
            /// </summary>
            /// 
            /// <param name="aMessage">Message to unsubscribe from</param>
            /// <param name="aCallback">Callback to remove from subscription</param>
            /// 
            /// <returns>
            /// Returns the result of Messenger.RemoveSubscriber
            /// </returns>
            public int Unsubscribe(string aMessage, Callback aCallback)
            {
                return Instance.RemoveSubscriber(aMessage, aCallback);
            }
        }
        #endregion
    }
}