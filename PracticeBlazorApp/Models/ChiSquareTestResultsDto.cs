namespace Roll20Aggregator.Models {
    public class ChiSquareTestResultsDto {
        public ChiSquareTestResultsDto(double statistic, int degreesOfFreedom, int sampleSize, double pValue, bool significant, int minimumRollsRequired) {
            Statistic = statistic;
            DegreesOfFreedom = degreesOfFreedom;
            SampleSize = sampleSize;
            PValue = pValue;
            Significant = significant;
            MinimumRollsRequired = minimumRollsRequired;
        }

        public double Statistic { get; set; }
        public int DegreesOfFreedom { get; set; }
        public int SampleSize { get; set; }
        public double PValue { get; set; }
        public bool Significant { get; set; }
        public int MinimumRollsRequired { get; set; }
    }
}
