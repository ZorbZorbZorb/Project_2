namespace Assets.Scripts.Helpers {
    public static class RandomExtensions {
        /// <summary>Randomly returns either true or false</summary>
        public static bool Bool() => UnityEngine.Random.Range(0, 2) == 1;
        public static T Random<T>(this T[] collection) {
            return collection[UnityEngine.Random.Range(0, collection.Length)];
        }
    }
}
