using System.ComponentModel;

namespace Roll20AggregatorHosted.Models.Enums {
    public enum SessionStatusEnum {
        [Description("Not Started")]
        NotStarted,

        [Description("Uploading File")]
        UploadingFile,

        [Description("Reading File")]
        ReadingFile,

        [Description("Parsing Messages")]
        ParsingRolls,

        [Description("Resolving Emote Messages")]
        ResolvingEmoteMessages,

        [Description("Finalizing")]
        Finalizing,

        [Description("Ready")]
        Ready,
    }
}
