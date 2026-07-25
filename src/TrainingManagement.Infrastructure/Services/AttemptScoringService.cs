using System.Text.RegularExpressions;
using TrainingManagement.Application.Attempts;
using TrainingManagement.Domain.Enums;

namespace TrainingManagement.Infrastructure.Services;

public sealed class AttemptScoringService : IAttemptScoringService
{
    public ScoringResult Score(IReadOnlyCollection<ScoringAnswer> answers, decimal passingScore)
    {
        var results = answers.Select(ScoreQuestion).ToArray();
        var maximum = answers.Sum(x => x.Points);
        var score = results.Sum(x => x.PointsAwarded);
        var percentage = maximum == 0 ? 0 : Math.Round(score * 100m / maximum, 2);
        return new(score, maximum, percentage, percentage >= passingScore, results);
    }

    private static ScoringQuestionResult ScoreQuestion(ScoringAnswer item)
    {
        var correct = item.Type switch
        {
            QuestionType.ShortAnswer => Normalize(item.TextAnswer) == Normalize(item.ExpectedAnswer),
            QuestionType.SingleChoice or QuestionType.TrueFalse =>
                item.SelectedOptionIds.Count == 1 && CorrectIds(item).SetEquals(item.SelectedOptionIds),
            QuestionType.MultipleChoice => CorrectIds(item).SetEquals(item.SelectedOptionIds),
            _ => false
        };
        return new(item.AttemptQuestionId, correct, correct ? item.Points : 0);
    }
    private static HashSet<int> CorrectIds(ScoringAnswer item) =>
        (item.CorrectOptionIds ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(int.Parse).ToHashSet();
    private static string Normalize(string? value) =>
        Regex.Replace((value ?? "").Trim(), @"\s+", " ").ToUpperInvariant();
}
