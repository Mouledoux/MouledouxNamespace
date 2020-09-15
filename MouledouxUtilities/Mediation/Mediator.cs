using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Mouledoux.Mediation.Components;

namespace Mouledoux.Mediation.Systems
{
    public static class Mediator
    {
        private static Dictionary<string, HashSet<Type>> m_typedMessages =
            new Dictionary<string, HashSet<Type>>();

        public static void NotifySubscribers<T>(string a_message, T a_arg, bool a_holdMessage = false) where T : new()
        {
            TryAddTypedMessage(a_message, typeof(T));

            foreach (Type type in m_typedMessages[a_message])
            {
                if (type is T)
                {
                    typeof(Catalogue<T>).MakeGenericType(new Type[] { type }).
                        GetMethod("NotifySubscribers").Invoke(null,
                            new object[] { a_message, a_arg, a_holdMessage });
                }
            }
        }

        public static void NotifySubscribersAsync<T>(string a_message, T a_arg, bool a_holdMessage = false) where T : new()
        {
            Task notifyTask = Task.Run(() =>
               NotifySubscribers(a_message, a_arg, a_holdMessage));
        }


        private static void TryAddTypedMessage(string a_message, Type a_type)
        {
            if (!m_typedMessages.ContainsKey(a_message))
            {
                m_typedMessages.Add(a_message, new HashSet<Type>() { typeof(object) });
            }
            m_typedMessages[a_message].Add(a_type);
        }
    }
}