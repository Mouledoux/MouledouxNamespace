using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Mouledoux.Conversion
{
    public static class Rosetta
    {
        public static bool TryPerformExplicitCastFromTo<T, U>(ref T a_obj, out U o_obj)
        {
            bool hasCast = TryGetExplicitCastFromTo<T, U>(out MethodInfo explicitCast);
            o_obj = hasCast ? explicitCast.Invoke(null, new object[] { a_obj }) as dynamic : default;
            return hasCast;
        }

        public static bool TryPerformImplicitCastFromTo<T, U>(ref T a_obj, out U o_obj)
        {
            bool hasCast = TryGetImplicitCastFromTo<T, U>(out MethodInfo implicitCast);
            o_obj = hasCast ? implicitCast.Invoke(null, new object[] { a_obj }) as dynamic : default;
            return hasCast;
        }


        public static bool TryGetAnyCastFromTo<T, U>(out MethodInfo o_method)
        {
            return TryGetAnyStaticCastFromTo<T, U>(out o_method);
        }

        public static bool TryGetExplicitCastFromTo<T, U>(out MethodInfo o_method)
        {
            return TryGetMethodCastFromTo<T, U>("op_Explicit", out o_method);
        }

        public static bool TryGetImplicitCastFromTo<T, U>(out MethodInfo o_method)
        {
            return TryGetMethodCastFromTo<T, U>("op_Implicit", out o_method);
        }

        public static bool TryGetMethodCastFromTo<T, U>(string a_methodName, out MethodInfo o_method)
        {
            return TryGetAnyStaticCastFromTo<T, U>(out o_method, a_methodName);
        }

        public static bool TryGetAnyStaticCastFromTo<T, U>(out MethodInfo o_method, string a_methodName = default)
        {
            return TryGetAnyCastFromTo<T, U>(out o_method, BindingFlags.Public | BindingFlags.Static);
        }


        // Caution, needs testing
        public static bool TryGetAnyNonStaticCastFromTo<T, U>(out MethodInfo o_method, string a_methodName = default)
        {
            return TryGetAnyCastFromTo<T, U>(out o_method, BindingFlags.Public & ~BindingFlags.Static);
        }



        private static bool TryGetAnyCastFromTo<T, U>(out MethodInfo o_method, BindingFlags a_bindingFlags, string a_methodName = default)
        {
            Type origin = typeof(T);
            Type target = typeof(U);

            MethodInfo[] methods = target.GetMethods(a_bindingFlags);
            IEnumerable<MethodInfo> cast = methods.Where(mi =>
                mi.ReturnType == target &&
                (a_methodName == default || mi.Name == a_methodName) &&
                mi.GetParameters().FirstOrDefault().ParameterType == origin);

            o_method = cast.FirstOrDefault();
            return cast.Count() > 0;
        }

    }
}
