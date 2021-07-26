﻿using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;
using Mouledoux.Casting;

namespace Mouledoux.Mediation
{
    public static class CatalogueMediator
    {
        private static HashSet<Type> m_knownTypes = new HashSet<Type>() { typeof(object), typeof(Type) };
        // private static readonly Catalogue<Type, Type>.Subscription addTypeSub;
        // private static readonly Catalogue<Type, Type>.Subscription removeTypeSub;

        static CatalogueMediator()
        {
            // string addTypeMessage = "AddTypeToTranslator";
            // string removeTypeMessage = "RemoveTypeFromTranslator";

            // Catalogue<Type, float>.NotifySubscribers(4f);

            //addTypeSub = new Catalogue<Type>.Subscription(addTypeMessage,
            //    (Type t) => m_knownTypes.Add(t), 99).Subscribe();

            //removeTypeSub = new Catalogue<Type>.Subscription(removeTypeMessage,
            //    (Type t) => m_knownTypes.Remove(t), 99).Subscribe();
        }


        public static void NotifySubscribersAsync<T>(string a_message, T a_arg, bool a_holdMessage = false)
        {
            Task notifyTask = Task.Run(() =>
               NotifyAllSubscribers(a_message, a_arg, a_holdMessage));
        }

        public static void NotifyAllSubscribers<T>(string a_message, T a_arg, bool a_holdMessage = false, bool a_excludeTypeT = false)
        {
            Type typeT = typeof(T);

            TryAddTypedSubscription<T>();
            
            foreach (Type type in m_knownTypes)
            {
                if (a_excludeTypeT && type == typeT)
                {
                    continue;
                }

                else if (Rosetta.TryGetExplicitCastFromTo(typeT, type, out MethodInfo o_implicit))
                {
                    dynamic arg = o_implicit == null ? a_arg : o_implicit.Invoke(null, new object[] { a_arg });
                    //Rosetta.InvokeGenericMethodAsType(null, "NotifySubscribers", new object[] { a_message, arg, a_holdMessage }, typeof(Catalogue<>), type);
                }
            }
        }

        public static void TryAddTypedSubscription<T>()
        {
            Catalogue<int, float>.NotifySubscribers(4);

            Type type = typeof(T);
            if (!m_knownTypes.Contains(type))
            {
                m_knownTypes.Add(type);
                //Catalogue<T>.OnCatalogueEmpty += () => m_knownTypes.Remove(type);
            }
        }
    }
}