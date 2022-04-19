using System.Collections.Generic;
using System.Linq;

namespace Assets.Scripts.Helpers {
    public static class RandomExtensions {
        /// <summary>Randomly returns either true or false</summary>
        public static bool Bool() => UnityEngine.Random.Range(0, 2) == 1;
        public static T Random<T>(this T[] collection) {
            return collection[UnityEngine.Random.Range(0, collection.Length)];
        }
        public static T Random<T>( this List<T> collection ) {
            return collection[UnityEngine.Random.Range(0, collection.Count)];
        }
        public static IEnumerable<T> TakeRandom<T>(this List<T> collection, int take) {
            if (collection.Count <= take) {
                for ( int i = 0; i < collection.Count; i++ ) {
                    yield return collection[i];
                }
                yield break;
            }
            else {
                for ( int i = 0; i < take; i++ ) {
                    if (collection.Count == 0) {
                        break;
                    }
                    var item = Random(collection);
                    collection.Remove(item);
                    yield return item;
                }
                yield break;
            }
        }
        public static IEnumerable<T> TakeRandom<T>( this IEnumerable<T> collection, int take ) {
            return TakeRandom(collection.ToList(), take);
        }
    }
}
