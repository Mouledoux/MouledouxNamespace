namespace Mouledoux.Interaction
{
    internal interface IPublisher
    {
    }



    internal interface ISubscriber
    {
    }



    internal static class Messenger
    {
        public static int BroadcastMessage(string aMessage)
        {
            return 1;
        }

        private static System.Collections.Generic.Dictionary<string, System.Delegate> m_messages =
                    new System.Collections.Generic.Dictionary<string, System.Delegate>();

        private delegate int Callback();
    }



    public class Publisher : IPublisher
    {
    }

    public class Subscriber : ISubscriber
    {
    }
}
