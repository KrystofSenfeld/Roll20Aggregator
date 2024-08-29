using AngleSharp.Dom;
using AngleSharp;
using Microsoft.AspNetCore.Components.Forms;
using Roll20AggregatorHosted.Models;
using Roll20AggregatorHosted.Models.Enums;

namespace Roll20AggregatorHosted.Services {
    public class ParsingSession {
        private const long MAX_FILE_SIZE = 1024L * 1024 * 1024 * 2; // 2 GB
        private SessionStatusEnum sessionStatus = SessionStatusEnum.NotStarted;

        public event EventHandler<EventArgs> SessionStatusChanged;
        public SessionStatusEnum Status {
            get { return sessionStatus; }
            set {
                sessionStatus = value;
                SessionStatusChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public bool IsLoading { get; private set; } = false;
        public ParseResultsDto ParseResults { get; private set; } = new();

        public void Initialize() {
            IsLoading = true;
            Status = SessionStatusEnum.UploadingFile;
            ParseResults = new ParseResultsDto();
        }

        public async Task<bool> StartSession(IBrowserFile file) {
            try {
                IBrowsingContext context = BrowsingContext.New();
                IDocument htmlDocument;

                if (file == null) {
                    
                    Status = SessionStatusEnum.ReadingFile;
                    using StreamReader reader = new("sample.txt");
                    string html = await reader.ReadToEndAsync();
                    htmlDocument = await context.OpenAsync(req => req.Content(html));
                } else {
                    Status = SessionStatusEnum.ReadingFile;
                    string path = Path.GetTempFileName();
                    await using FileStream stream = new(path, FileMode.Create);
                    
                    await file.OpenReadStream(MAX_FILE_SIZE).CopyToAsync(stream);
                    stream.Position = 0;

                    htmlDocument = await context.OpenAsync(req => req.Content(stream, true));
                }

                Status = SessionStatusEnum.ParsingRolls;

                var parser = new FileParser(htmlDocument, this);
                await parser.Parse();
            } catch {
                CancelSession();
                return false;
            }

            if (ParseResults == null || !ParseResults.RawCharacterStats.Any()) {
                CancelSession();
                return false;
            }

            Status = SessionStatusEnum.Ready;
            IsLoading = false;

            return true;
        }

        public void CancelSession() {
            Status = SessionStatusEnum.NotStarted;
            IsLoading = false;
        }
    }
}
