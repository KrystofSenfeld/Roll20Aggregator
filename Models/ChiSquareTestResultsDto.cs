namespace Roll20AggregatorHosted.Models {
    public class ChiSquareTestResultsDto {
        public double ChiSquareStatistic { get; set; }
        public int DegreesOfFreedom { get; set; }
        public int SampleSize { get; set; }
        public double PValue { get; set; }
        public bool IsSignificant { get; set; }
        public int MinimumRollsRequired { get; set; }
    }
}
