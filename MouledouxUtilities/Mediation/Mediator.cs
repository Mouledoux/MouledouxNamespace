using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Mouledoux.Mediation
{
    public static class CatalogueTranslator
    {
        private static HashSet<Type> m_knownTypes = new HashSet<Type>() { typeof(object), typeof(Type) };
        private static readonly Catalogue<Type>.Subscription addTypeSub;
        private static readonly Catalogue<Type>.Subscription removeTypeSub;

        static CatalogueTranslator()
        {
            string addTypeMessage = "AddTypeToTranslator";
            string removeTypeMessage = "RemoveTypeFromTranslator";

            addTypeSub = new Catalogue<Type>.Subscription(addTypeMessage,
                (Type t) => m_knownTypes.Add(t), 99).Subscribe();

            removeTypeSub = new Catalogue<Type>.Subscription(removeTypeMessage,
                (Type t) => m_knownTypes.Remove(t), 99).Subscribe();
        }





        public static void NotifySubscribersAsync<T>(string a_message, T a_arg, bool a_holdMessage = false)
        {
            Task notifyTask = Task.Run(() =>
               NotifySubscribers(a_message, a_arg, a_holdMessage));
        }




        public static void NotifySubscribers<T>(string a_message, T a_arg, bool a_holdMessage = false)
        {
            TryAddTypedSubscription<T>();

            foreach (Type type in m_knownTypes)
            {
                if (GetHasExplicitConversion(type, typeof(T), out MethodInfo o_implicit))
                {
                    dynamic arg = o_implicit == null ? a_arg : o_implicit.Invoke(null, new object[] { a_arg });
                    InvokeGenericMethodAsType(null, "NotifySubscribers", new object[] { a_message, arg, a_holdMessage }, typeof(Catalogue<>), type);
                }
            }
        }




        public static void TryAddTypedSubscription<T>()
        {
            Type type = typeof(T);
            if (!m_knownTypes.Contains(type))
            {
                m_knownTypes.Add(typeof(T));
                Catalogue<T>.OnCatalogueEmpty += (Type t) => m_knownTypes.Remove(type);
            }
        }




        private static void InvokeGenericMethodAsType(object a_target, string a_methodName, object[] a_args, Type a_genericType, Type a_invokeType)
        {
            Type thisType = a_genericType.MakeGenericType(new Type[] { a_invokeType });
            MethodInfo typedMethod = thisType.GetMethod(a_methodName);
            
            typedMethod.Invoke(a_target, a_args);
        }





        private static bool GetHasExplicitConversion(Type a_baseType, Type a_targetType, out MethodInfo o_method)
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