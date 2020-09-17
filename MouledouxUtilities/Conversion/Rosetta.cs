using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Mouledoux.Conversion
{
    public static class Rosetta
    {
        public static bool TryPerformExplicitConversionFromTo<T, U>(T a_obj, out U o_obj)
        {
            Type origin = typeof(T);
            Type target = typeof(U);

            MethodInfo[] methods = target.GetMethods(BindingFlags.Public | BindingFlags.Static);
            IEnumerable<MethodInfo> cast = methods.Where(mi => mi.Name == "op_Explicit" && mi.ReturnType == target);
            bool hasCast = cast.Any(mi =>
            {
                ParameterInfo pi = mi.GetParameters().FirstOrDefault();
                return pi != null && pi.ParameterType == origin;
            });

            o_obj = hasCast ? cast.FirstOrDefault().Invoke(null, new object[] { a_obj }) as dynamic : default;
            return hasCast;
        }



        public static bool TryPerformImplicitConversionFromTo<T, U>(T a_obj, out U o_obj)
        {
            Type origin = typeof(T);
            Type target = typeof(U);

            MethodInfo[] methods = target.GetMethods(BindingFlags.Public | BindingFlags.Static);
            IEnumerable<MethodInfo> cast = methods.Where(mi => mi.Name == "op_Implicit" && mi.ReturnType == target);
            bool hasCast = cast.Any(mi =>
            {
                ParameterInfo pi = mi.GetParameters().FirstOrDefault();
                return pi != null && pi.ParameterType == origin;
            });

            o_obj = hasCast ? cast.FirstOrDefault().Invoke(null, new object[] { a_obj }) as dynamic : default;
            return hasCast;
        }
    }
}
