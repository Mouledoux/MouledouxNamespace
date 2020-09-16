using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;
using Mouledoux.Mediation.Components;

namespace Mouledoux.Mediation.Systems
{
    public static class TypedMediator
    {
        private static Dictionary<string, HashSet<Type>> m_typedMessages =
            new Dictionary<string, HashSet<Type>>();

        public static void NotifySubscribers<T>(string a_message, T a_arg, bool a_holdMessage = false)
        {
            TryAddTypedMessage(a_message, typeof(T));

            foreach (Type type in m_typedMessages[a_message])
            {
                if (HasGetExplicitConversion(type, typeof(T), out MethodInfo o_implicit))
                {
                    Type thisType = typeof(Catalogue<>).MakeGenericType(new Type[] { type });
                    MethodInfo typedMethod = thisType.GetMethod("NotifySubscribers");

                    dynamic arg = o_implicit == null ? a_arg : o_implicit.Invoke(null, new object[] { a_arg });
                    typedMethod.Invoke(null, new object[] { a_message, arg, a_holdMessage });
                }
            }
        }

        private static bool HasGetExplicitConversion(Type a_baseType, Type a_targetType, out MethodInfo o_method)
        {
            if (a_baseType == a_targetType)
            {
                o_method = default;
                return true;
            }

            MethodInfo[] methods = a_baseType.GetMethods(BindingFlags.Public | BindingFlags.Static);
            IEnumerable<MethodInfo> cast = methods.Where(mi => mi.Name == "op_Explicit" && mi.ReturnType == a_baseType);
            bool hasCast = cast.Any(mi =>
            {
                ParameterInfo pi = mi.GetParameters().FirstOrDefault();
                return pi != null && pi.ParameterType == a_targetType;
            });

            o_method = cast.FirstOrDefault();
            return hasCast;
        }









        public static void NotifySubscribersAsync<T>(string a_message, T a_arg, bool a_holdMessage = false)
        {
            Task notifyTask = Task.Run(() =>
               NotifySubscribers(a_message, a_arg, a_holdMessage));
        }


        public static void TryAddTypedMessage(string a_message, Type a_type)
        {
            if (!m_typedMessages.ContainsKey(a_message))
            {
                m_typedMessages.Add(a_message, new HashSet<Type>() { typeof(object) });
            }
            m_typedMessages[a_message].Add(a_type);
        }
    }
}