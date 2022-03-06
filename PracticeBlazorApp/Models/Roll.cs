namespace Roll20Aggregator.Models {
    public class Roll {
        public Roll(int id, string rolledBy, string avatar, int value, string dieType) {
            Id = id;
            RolledBy = rolledBy;
            Avatar = avatar;
            Value = value;
            DieType = dieType;
        }

        public int Id { get; }
        public string RolledBy { get; set; }
        public string Avatar { get; }
        public int Value { get; set; }
        public string DieType { get; set; }
    }
}
