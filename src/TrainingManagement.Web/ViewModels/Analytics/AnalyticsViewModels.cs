using System.ComponentModel.DataAnnotations;
using TrainingManagement.Application.Analytics;

namespace TrainingManagement.Web.ViewModels.Analytics;

public sealed class AnalyticsFilterViewModel : IValidatableObject
{
    public AnalyticsPeriod Period { get; set; } = AnalyticsPeriod.Last30Days;
    [DataType(DataType.Date)] public DateTime? DateFrom { get; set; }
    [DataType(DataType.Date)] public DateTime? DateTo { get; set; }
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Period == AnalyticsPeriod.Custom && (DateFrom is null || DateTo is null))
            yield return new("Les deux dates sont obligatoires pour une période personnalisée.");
        if (DateFrom > DateTo) yield return new("La date de début ne peut pas être après la date de fin.");
        if (DateFrom is not null && DateTo is not null && (DateTo - DateFrom)?.TotalDays > 365)
            yield return new("La période ne peut pas dépasser 366 jours.");
    }
    public AnalyticsFilterModel ToModel() => new(Period, DateFrom, DateTo);
}
