namespace Roll20AggregatorHosted.Models {
    public class DataItemDto {
        public int Number { get; set; }
        public string Category { get; set; }
        public double Value { get; set; }
        public string ValueDisplay => Value.ToString("n0");
        public int SampleSize { get; set; }
        public string SampleSizeDisplay => SampleSize.ToString("n0");

        public bool HasLowSampleSize => SampleSize < 30;
        public string LowSampleSizeWarning => "Low sample size could make this data point an outlier.";
        public string NotANumberWarning => "Z Score cannot be calculated because the sample size is 1.";

        public string NumberColor {
            get {
                if (Number == 1) {
                    return "gold";
                } else if (Number == 2) {
                    return "silver";
                } else if (Number == 3) {
                    return "saddlebrown";
                } else {
                    return "default";
                }
            }
        }
    }
}
