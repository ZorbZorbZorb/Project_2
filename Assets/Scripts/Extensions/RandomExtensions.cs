using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Random = UnityEngine.Random;

namespace Assets.Scripts.Extensions {
    public static class RandomExtensions {
        /// <summary>Randomly returns either true or false</summary>
        public static bool Bool() => Random.Range(0, 2) == 1;  // Why is this not a default UnityEngine.Random method?
    }
}
