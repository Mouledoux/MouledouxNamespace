using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Mouledoux.Casting
{
    public static class Rosetta
    {
        public static void InvokeGenericMethodAsType(object a_target, string a_methodName, object[] a_args, Type a_genericType, Type a_invokeType)
        {
            Type thisType = a_genericType.MakeGenericType(new Type[] { a_invokeType });
            MethodInfo typedMethod = thisType.GetMethod(a_methodName);

            typedMethod.Invoke(a_target, a_args);
        }



        public static bool TryGetAnyCastFromTo(Type a_originType, Type a_targetType, out MethodInfo o_method)
        {
            return TryGetAnyCastFromTo(a_originType, a_targetType, out o_method, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
        }

        public static bool TryPerformExplicitCastFromTo<T, U>(ref T a_obj, out U o_obj)
        {
            bool hasCast = TryGetExplicitCastFromTo(typeof(T), typeof(U), out MethodInfo explicitCast);
            o_obj = hasCast ? (U)explicitCast.Invoke(null, new object[] { a_obj }) : default;
            return hasCast;
        }

        public static bool TryPerformImplicitCastFromTo<T, U>(ref T a_obj, out U o_obj)
        {
            bool hasCast = TryGetImplicitCastFromTo(typeof(T), typeof(U), out MethodInfo implicitCast);
            o_obj = hasCast ? (U)implicitCast.Invoke(null, new object[] { a_obj }) : default;
            return hasCast;
        }



        public static bool TryGetExplicitCastFromTo(Type a_originType, Type a_targetType, out MethodInfo o_method)
        {
            return TryGetStaticMethodCastFromTo(a_originType, a_targetType, "op_Explicit", out o_method);
        }

        public static bool TryGetImplicitCastFromTo(Type a_originType, Type a_targetType, out MethodInfo o_method)
        {
            return TryGetStaticMethodCastFromTo(a_originType, a_targetType, "op_Implicit", out o_method);
        }

        public static bool TryGetStaticMethodCastFromTo(Type a_originType, Type a_targetType, string a_methodName, out MethodInfo o_method)
        {
            return TryGetAnyStaticCastFromTo(a_originType, a_targetType, out o_method, a_methodName);
        }

        public static bool TryGetAnyNonStaticCastFromTo(Type a_originType, Type a_targetType, out MethodInfo o_method, string a_methodName = default)
        {
            return TryGetAnyCastFromTo(a_originType, a_targetType, out o_method, BindingFlags.Public | BindingFlags.Instance, a_methodName);
        }

        public static bool TryGetAnyStaticCastFromTo(Type a_originType, Type a_targetType, out MethodInfo o_method, string a_methodName = default)
        {
            return TryGetAnyCastFromTo(a_originType, a_targetType, out o_method, BindingFlags.Public | BindingFlags.Static, a_methodName);
        }



        private static bool TryGetAnyCastFromTo(Type a_originType, Type a_targetType, out MethodInfo o_method, BindingFlags a_bindingFlags, string a_methodName = default)
        {
            MethodInfo[] methods = a_targetType.GetMethods(a_bindingFlags);
            IEnumerable<MethodInfo> cast = methods?.Where(mi =>
                mi.ReturnType == a_targetType &&
                (a_methodName == default || mi.Name == a_methodName) &&
                mi.GetParameters().FirstOrDefault().ParameterType == a_originType);

            o_method = cast?.FirstOrDefault();
            return cast?.Count() > 0;
        }
    }
}
