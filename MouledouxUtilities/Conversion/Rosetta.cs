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


        public static bool TryPerformImplicitCastFromTo<T, U>(T a_obj, out U o_obj)
        {
            bool hasCast = TryGetImplicitCastFromTo<T, U>(out MethodInfo implicitCast);
            o_obj = hasCast ? implicitCast.Invoke(null, new object[] { a_obj }) as dynamic : default;
            return hasCast;
        }


        public static bool TryGetExplicitCastFromTo<T, U>(out MethodInfo o_method)
        {
            return TryGetMethodCastFromTo<T, U>("op_Explicit", out o_method);
        }


        public static bool TryGetImplicitCastFromTo<T, U>(out MethodInfo o_method)
        {
            return TryGetMethodCastFromTo<T, U>("op_Implicit", out o_method);
        }


        private static bool TryGetMethodCastFromTo<T, U>(string a_methodName, out MethodInfo o_method)
        {
            Type origin = typeof(T);
            Type target = typeof(U);

            MethodInfo[] methods = target.GetMethods(BindingFlags.Public | BindingFlags.Static);
            IEnumerable<MethodInfo> cast = methods.Where(mi => mi.Name == a_methodName && mi.ReturnType == target);
            bool hasCast = cast.Any(mi =>
            {
                ParameterInfo pi = mi.GetParameters().FirstOrDefault();
                return pi != null && pi.ParameterType == origin;
            });

            o_method = cast.FirstOrDefault();
            return hasCast;
        }

    }
}
