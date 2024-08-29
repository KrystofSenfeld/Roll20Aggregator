namespace Roll20AggregatorHosted.Models {
    public class RollDto {
        public long MessageId { get; set; }
        public string RolledBy { get; set; }
        public string Avatar { get; set; }
        public string RawDieType { get; set; }
        public List<int> Values { get; set; }

        public int DiceRolledCount => Values.Count;

    }
}
