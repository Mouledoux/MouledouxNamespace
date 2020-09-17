using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mouledoux.Utilities
{
    public static class Utilities
    {
        public static void ValidateDelegateCallback(ref Delegate a_delegate)
        {
            foreach (Delegate del in a_delegate.GetInvocationList())
            {
                Delegate.Remove(a_delegate, del.Target.Equals(null) ? del : default);
            }
        }
    }
}
