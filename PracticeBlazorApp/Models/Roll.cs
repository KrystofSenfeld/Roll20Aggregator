namespace Roll20Aggregator.Models {
    public class Roll {
        public Roll(string rolledBy, int value, string dieType) {
            RolledBy = rolledBy;
            Value = value;
            DieType = dieType;
        }

        public string RolledBy { get; set; }
        public int Value { get; set; }
        public string DieType { get; set; }
    }
}
