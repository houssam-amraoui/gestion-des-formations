using System.ComponentModel.DataAnnotations;
using TrainingManagement.Domain.Enums;

namespace TrainingManagement.Domain.Entities;

public sealed class Question
{
    public int Id { get; set; }
    public int AssessmentId { get; set; }
    public Assessment Assessment { get; set; } = null!;
    public QuestionType QuestionType { get; set; }
    [Required] public string Statement { get; set; } = string.Empty;
    public string? Explanation { get; set; }
    public int Order { get; set; }
    public decimal Points { get; set; }
    [MaxLength(2000)] public string? ExpectedAnswer { get; set; }
    public bool IsPublished { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public ICollection<AnswerOption> AnswerOptions { get; set; } = new List<AnswerOption>();

    public string? GetPublicationError()
    {
        if (Assessment.IsArchived) return "Une question d’une évaluation archivée ne peut pas être publiée.";
        if (string.IsNullOrWhiteSpace(Statement)) return "L’énoncé est obligatoire.";
        if (Points < 0) return "Les points doivent être supérieurs ou égaux à zéro.";
        var options = AnswerOptions.ToArray();
        return QuestionType switch
        {
            QuestionType.SingleChoice when options.Length < 2 => "Une question à choix unique exige au moins deux choix.",
            QuestionType.SingleChoice when options.Count(option => option.IsCorrect) != 1 => "Une question à choix unique exige exactement une bonne réponse.",
            QuestionType.MultipleChoice when options.Length < 2 => "Une question à choix multiples exige au moins deux choix.",
            QuestionType.MultipleChoice when options.All(option => !option.IsCorrect) => "Une question à choix multiples exige au moins une bonne réponse.",
            QuestionType.TrueFalse when options.Length != 2 => "Une question Vrai ou Faux exige exactement deux choix.",
            QuestionType.TrueFalse when !options.Any(option => option.Text.Equals("Vrai", StringComparison.OrdinalIgnoreCase)) ||
                                                 !options.Any(option => option.Text.Equals("Faux", StringComparison.OrdinalIgnoreCase))
                => "Les choix Vrai et Faux sont obligatoires.",
            QuestionType.TrueFalse when options.Count(option => option.IsCorrect) != 1 => "Une question Vrai ou Faux exige exactement une bonne réponse.",
            QuestionType.ShortAnswer when options.Length != 0 => "Une question à réponse courte ne peut pas contenir de choix.",
            QuestionType.ShortAnswer when string.IsNullOrWhiteSpace(ExpectedAnswer) => "La réponse attendue est obligatoire.",
            QuestionType.ShortAnswer when ExpectedAnswer.Length > 2000 => "La réponse attendue ne peut pas dépasser 2000 caractères.",
            _ => null
        };
    }

    public void Publish(DateTime utcNow)
    {
        var error = GetPublicationError();
        if (error is not null) throw new InvalidOperationException(error);
        IsPublished = true;
        UpdatedAt = utcNow;
    }
}
