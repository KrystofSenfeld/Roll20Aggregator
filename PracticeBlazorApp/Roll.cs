using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PracticeBlazorApp {
    public class Roll {

        public string RolledBy { get; set; }
        public int Value { get; set; }
        public string DieType { get; set; }

        public Roll(string rolledBy, int value, string dieType) {
            RolledBy = rolledBy;
            Value = value;
            DieType = dieType;
        }
    }
}
