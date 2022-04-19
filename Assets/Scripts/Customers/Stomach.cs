using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.Customers {
    public class Stomach {
        [SerializeField]
        private float Amount;
        [SerializeField]
        private float Max;
        public float Fullness = 0f;
        public void Add(float amount) {
            Amount += amount;
            Fullness = Amount / Max;
        }
        public float Remove( float amount ) {
            float removed = Mathf.Max(Amount, amount);
            Amount -= removed;
            Fullness = Amount / Max;
            return removed;
        }

        public Stomach(float max) {
            Max = max;
            Amount = 0f;
            Fullness = Amount / max;
        }
    }
}
