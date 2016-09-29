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
        /// Returns 1 if the message and Callback were correctly added,
        /// and 0 if the message was already subscribed to, but the Callback was still added
        /// </returns>
        private int AddSubscriber(string aMessage, Callback aCallback)
        {
            if(Subsciptions.ContainsKey(aMessage))
            {
                Subsciptions[aMessage] += aCallback;
                return 0;
            }
            else
            {
                Subsciptions.Add(aMessage, aCallback);
                return 1;
            }
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
            if (!Subsciptions.ContainsKey(aMessage))
                return 0;

            Subsciptions[aMessage] -= aCallback;
            return 1;
        }


        private int BroadcastMessage(string aMessage)
        {
            if (!Subsciptions.ContainsKey(aMessage))
                return 0;

            Subsciptions[aMessage].Invoke();
            return 1;
        }


        private System.Collections.Generic.Dictionary<string, Callback> Subsciptions =
            new System.Collections.Generic.Dictionary<string, Callback>();


        #region Subclasses
        public class Publisher
        {
            public int Publish(string aMessage)
            {
                return Instance.BroadcastMessage(aMessage);
            }
        }

        public class Subscriber
        {
            public int Subscribe(string aMessage, Callback aCallback)
            {
                return Instance.AddSubscriber(aMessage, aCallback);
            }

            public int Unsubscribe(string aMessage, Callback aCallback)
            {
                return Instance.RemoveSubscriber(aMessage, aCallback);
            }
        }
        #endregion
    }
}