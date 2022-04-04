using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Assets.Scripts.Objects;

namespace Assets.Scripts {
    [Serializable]
    public class Layout {
        [Serializable]
        public class Bathroom {
            public List<Option> Options;
        }
        public class Option {
            public int X;
            public int Y;
            public List<ReliefType> Options;
            public ReliefType? Current;
            public Orientation Orientation;
        }
        public Bathroom Mens;
        public Bathroom Womens;

        public override string ToString() {

            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
        static public Layout FromJson(string json) {
            return JsonConvert.DeserializeObject<Layout>(json);
        }
    }
}
