using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Mouledoux.Mediation
{
    public static class CatalogueTranslator
    {
        private static Dictionary<string, HashSet<Type>> m_knownTranslations =
            new Dictionary<string, HashSet<Type>>();


        public static void NotifySubscribersAsync<T>(string a_message, T a_arg, bool a_holdMessage = false)
        {
            Task notifyTask = Task.Run(() =>
               NotifySubscribers(a_message, a_arg, a_holdMessage));
        }


        public static void NotifySubscribers<T>(string a_message, T a_arg, bool a_holdMessage = false)
        {
            TryAddTypedSubscription<T>(a_message);

            foreach (Type type in m_knownTranslations[a_message])
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


        public static void TryAddTypedSubscription<T>(string a_message)
        {
            if (!m_knownTranslations.ContainsKey(a_message))
            {
                m_knownTranslations.Add(a_message, new HashSet<Type>() { typeof(object) });
            }
            m_knownTranslations[a_message].Add(typeof(T));
        }


        private static bool HasGetExplicitConversion(Type a_baseType, Type a_targetType, out MethodInfo o_method)
        {
            // Early return if the types are the same
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
    }
}