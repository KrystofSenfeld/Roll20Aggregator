using System.ComponentModel;

namespace Roll20AggregatorHosted.Models.Enums {
    public enum DieTypeEnum {
        None = 0,

        [Description("d2")]
        D2 = 2,

        [Description("d4")]
        D4 = 4,

        [Description("d6")]
        D6 = 6,

        [Description("d8")]
        D8 = 8,

        [Description("d10")]
        D10 = 10,

        [Description("d12")]
        D12 = 12,

        [Description("d20")]
        D20 = 20,

        [Description("d100")]
        D100 = 100,
    }
}
