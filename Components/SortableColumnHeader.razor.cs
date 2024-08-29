using Microsoft.AspNetCore.Components;
using Roll20AggregatorHosted.Models;
using Roll20AggregatorHosted.Pages.Results;

namespace Roll20AggregatorHosted.Components {
    public partial class SortableColumnHeader<TProperty> {
        [Parameter] public ResultsStatsTab Parent { get; set; }
        [Parameter] public string ColumnName { get; set; }
        [Parameter] public string PropName { get; set; }
        [Parameter] public Func<DieStatsRowViewModel, TProperty> PropertyGetter { get; set; }

        public string VisibilityClass => Parent.CurrentSortProperty == PropName ? "visible" : "invisible";

        public string Icon {
            get {
                if (Parent.SortDirection == Models.Enums.SortDirectionEnum.Ascending) {
                    return typeof(TProperty) == typeof(string)
                        ? "fa-solid fa-arrow-up-a-z fa-fw"
                        : "fa-solid fa-arrow-up-1-9 fa-fw";
                }
                else {
                    return typeof(TProperty) == typeof(string)
                        ? "fa-solid fa-arrow-down-z-a fa-fw"
                        : "fa-solid fa-arrow-down-9-1 fa-fw";
                }
            }
        }
    }
}
