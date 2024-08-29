using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Roll20AggregatorHosted.Services;
using Results = Roll20AggregatorHosted.Pages.Results.Results;

namespace Roll20AggregatorHosted.Components {
    public partial class AddGroupDialog {
        [Inject] ParsingSession ParsingSession { get; set; }
        [Inject] IJSRuntime JSRuntime { get; set; }
        [Parameter] public Results Parent { get; set; }

        public string GroupName { get; set; }
        public HashSet<string> SelectedCharacters { get; set; } = new();
        public bool AttemptedDuplicateName { get; set; }

        public bool CanSubmit => !string.IsNullOrWhiteSpace(GroupName);

        public void Initialize() {
            GroupName = string.Empty;
            SelectedCharacters.Clear();
        }

        public void ToggleCharacterSelected(string character) {
            if (!SelectedCharacters.Contains(character)) {
                SelectedCharacters.Add(character);
            } else {
                SelectedCharacters.Remove(character);
            }
        }

        public bool IsSelected(string character) => SelectedCharacters.Contains(character);

        public string GetColorClass(string character) {
            return !IsSelected(character) ? "light" : "primary";
        }

        public string GetIcon(string character) {
            return !IsSelected(character) ? "plus" : "xmark";
        }

        public async Task CreateGroup() {
            if (!CanSubmit) {
                return;
            }

            if (Parent.AllNames.Contains(GroupName)) {
                AttemptedDuplicateName = true;
                return;
            }

            AttemptedDuplicateName = false;
            Parent.CreateGroup(GroupName, new HashSet<string>(SelectedCharacters));

            await JSRuntime.InvokeVoidAsync("closeDialog", "groupModal");
        }
    }
}
