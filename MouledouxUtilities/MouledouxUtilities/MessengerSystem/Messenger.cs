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

        private System.Collections.Generic.Dictionary<string, Callback> Subsciptions =
            new System.Collections.Generic.Dictionary<string, Callback>();

        #region Subclasses
        public class Publisher
        {
            public int Publish(string aMessage)
            {
                return 1;
            }
        }

        public class Subscriber
        {
            public int Subscribe(string aMessage, Callback aCallback)
            {
                return 1;
            }
        }
        #endregion
    }
}
