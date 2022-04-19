using Assets.Scripts.Customers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Scripts.Helpers {
    public static class CustomerExtensions {
        public static float AverageFullness( this IEnumerable<Customer> collection ) => collection.Average(x => x.Bladder.Fullness);
        public static float[] GetBladders(this IEnumerable<Customer> collection) => collection.Select(x => x.Bladder.Fullness).ToArray();
    }
}
