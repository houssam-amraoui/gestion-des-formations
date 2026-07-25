using TrainingManagement.Domain.Enums;

namespace TrainingManagement.Web.Services;

public static class TrainingDisplayExtensions
{
    public static string ToFrenchLabel(this AssessmentType value) => value switch
    {
        AssessmentType.Practice => "Entraînement",
        AssessmentType.Quiz => "Quiz",
        AssessmentType.Exam => "Examen",
        _ => value.ToString()
    };

    public static string ToFrenchLabel(this QuestionType value) => value switch
    {
        QuestionType.SingleChoice => "Choix unique",
        QuestionType.MultipleChoice => "Choix multiples",
        QuestionType.TrueFalse => "Vrai ou Faux",
        QuestionType.ShortAnswer => "Réponse courte",
        _ => value.ToString()
    };
    public static string ToFrenchLabel(this LessonContentType type) => type switch
    {
        LessonContentType.Text => "Texte",
        LessonContentType.Video => "Vidéo",
        LessonContentType.Audio => "Audio",
        LessonContentType.Pdf => "Document PDF",
        LessonContentType.ExternalLink => "Lien externe",
        _ => type.ToString()
    };
    public static string ToFrenchLabel(this TrainingLevel level) => level switch
    {
        TrainingLevel.Beginner => "Débutant",
        TrainingLevel.Intermediate => "Intermédiaire",
        TrainingLevel.Advanced => "Avancé",
        TrainingLevel.AllLevels => "Tous niveaux",
        _ => level.ToString()
    };

    public static string ToFrenchLabel(this TrainingStatus status) => status switch
    {
        TrainingStatus.Draft => "Brouillon",
        TrainingStatus.Published => "Publiée",
        TrainingStatus.Unpublished => "Dépubliée",
        TrainingStatus.Archived => "Archivée",
        _ => status.ToString()
    };

    public static string ToBadgeClass(this TrainingStatus status) => status switch
    {
        TrainingStatus.Published => "text-bg-success",
        TrainingStatus.Unpublished => "text-bg-warning",
        TrainingStatus.Archived => "text-bg-secondary",
        _ => "text-bg-light"
    };
}
