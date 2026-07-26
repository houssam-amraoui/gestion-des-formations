using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TrainingManagement.Application.AiTrainer;
using TrainingManagement.Infrastructure.AiTrainer;
using TrainingManagement.Infrastructure.Persistence;

namespace TrainingManagement.Infrastructure.Services;

public sealed class LessonContextService(ApplicationDbContext db, IOptions<AiTrainerOptions> options) :
    ILessonContextService
{
    public async Task<LessonContextModel?> BuildAsync(int lessonId, CancellationToken token = default)
    {
        var lesson = await db.Lessons.AsNoTracking().Where(x => x.Id == lessonId)
            .Select(x => new
            {
                x.Id, x.Title, x.Summary,
                ModuleTitle = x.TrainingModule.Title,
                ModuleDescription = x.TrainingModule.Description,
                TrainingTitle = x.TrainingModule.Training.Title,
                TrainingDescription = x.TrainingModule.Training.ShortDescription,
                Contents = x.Contents.Where(c => c.IsPublished).OrderBy(c => c.Order)
                    .Select(c => new { c.Title, c.TextContent, c.Description, c.ExternalUrl }).ToList(),
                Assessments = x.Assessments.Where(a => a.IsPublished && !a.IsArchived)
                    .OrderBy(a => a.Order).Select(a => new { a.Title, a.Description }).ToList()
            }).SingleOrDefaultAsync(token);
        if (lesson is null) return null;
        var builder = new StringBuilder();
        Append(builder, "LEÇON ACTUELLE", lesson.Title, lesson.Summary);
        foreach (var content in lesson.Contents)
            Append(builder, content.Title ?? "Contenu", content.TextContent, content.Description,
                content.ExternalUrl);
        Append(builder, "MODULE", lesson.ModuleTitle, lesson.ModuleDescription);
        Append(builder, "FORMATION", lesson.TrainingTitle, lesson.TrainingDescription);
        if (lesson.Assessments.Count > 0)
        {
            builder.AppendLine("ÉVALUATIONS DISPONIBLES (ne jamais donner directement leurs réponses) :");
            foreach (var assessment in lesson.Assessments)
                Append(builder, assessment.Title, assessment.Description);
        }
        var maximum = options.Value.MaximumContextCharacters;
        var full = builder.ToString().Trim();
        var truncated = full.Length > maximum;
        var contentValue = truncated ? $"{full[..Math.Max(0, maximum - 25)].TrimEnd()}\n[Contexte tronqué]" : full;
        return new(lesson.Id, lesson.TrainingTitle, lesson.ModuleTitle, lesson.Title, contentValue, truncated);
    }

    private static void Append(StringBuilder builder, params string?[] values)
    {
        foreach (var value in values.Where(x => !string.IsNullOrWhiteSpace(x)))
            builder.AppendLine(value!.Trim());
        builder.AppendLine();
    }
}
