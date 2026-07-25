using TrainingManagement.Application.Attempts;

namespace TrainingManagement.Web.ViewModels.Attempts;

public sealed class AttemptPageViewModel
{
    public required AttemptDetailsModel Attempt { get; init; }
    public List<AttemptQuestionInputViewModel> Questions { get; set; } = [];
}
public sealed class AttemptQuestionInputViewModel
{
    public int AttemptQuestionId { get; set; }
    public List<int> SelectedOptionIds { get; set; } = [];
    public string? TextAnswer { get; set; }
}
public sealed class AttemptResultViewModel { public required AttemptResultModel Result { get; init; } }
public sealed class AttemptHistoryViewModel
{
    public int? TrainingId { get; set; }
    public bool? Passed { get; set; }
    public TrainingManagement.Domain.Enums.AttemptStatus? Status { get; set; }
    public IReadOnlyCollection<AttemptHistoryItem> Items { get; init; } = [];
}
