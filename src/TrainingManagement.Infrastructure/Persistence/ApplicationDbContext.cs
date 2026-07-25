using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TrainingManagement.Infrastructure.Identity;
using TrainingManagement.Domain.Entities;

namespace TrainingManagement.Infrastructure.Persistence;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Training> Trainings => Set<Training>();
    public DbSet<TrainingModule> TrainingModules => Set<TrainingModule>();
    public DbSet<Lesson> Lessons => Set<Lesson>();
    public DbSet<LessonContent> LessonContents => Set<LessonContent>();
    public DbSet<Assessment> Assessments => Set<Assessment>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<AnswerOption> AnswerOptions => Set<AnswerOption>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<LessonProgress> LessonProgresses => Set<LessonProgress>();
    public DbSet<AssessmentAttempt> AssessmentAttempts => Set<AssessmentAttempt>();
    public DbSet<AttemptQuestion> AttemptQuestions => Set<AttemptQuestion>();
    public DbSet<LearnerAnswer> LearnerAnswers => Set<LearnerAnswer>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(user => user.FirstName).HasMaxLength(100).IsRequired();
            entity.Property(user => user.LastName).HasMaxLength(100).IsRequired();
            entity.Property(user => user.ProfilePictureUrl).HasMaxLength(2048);
        });

        builder.Entity<Category>(entity =>
        {
            entity.Property(category => category.Name).HasMaxLength(150).IsRequired();
            entity.Property(category => category.Slug).HasMaxLength(180).IsRequired();
            entity.Property(category => category.Description).HasMaxLength(1000);
            entity.Property(category => category.ImageUrl).HasMaxLength(500);
            entity.HasIndex(category => category.Name).IsUnique();
            entity.HasIndex(category => category.Slug).IsUnique();
        });

        builder.Entity<Training>(entity =>
        {
            entity.Property(training => training.Title).HasMaxLength(200).IsRequired();
            entity.Property(training => training.Slug).HasMaxLength(220).IsRequired();
            entity.Property(training => training.ShortDescription).HasMaxLength(500).IsRequired();
            entity.Property(training => training.Description).IsRequired();
            entity.Property(training => training.ThumbnailUrl).HasMaxLength(500);
            entity.Property(training => training.Language).HasMaxLength(50).IsRequired();
            entity.Property(training => training.Price).HasPrecision(18, 2);
            entity.HasIndex(training => training.Slug).IsUnique();
            entity.HasOne(training => training.Category)
                .WithMany(category => category.Trainings)
                .HasForeignKey(training => training.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ApplicationUser>()
                .WithMany(user => user.Trainings)
                .HasForeignKey(training => training.TrainerId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<TrainingModule>(entity =>
        {
            entity.Property(module => module.Title).HasMaxLength(200).IsRequired();
            entity.Property(module => module.Slug).HasMaxLength(220).IsRequired();
            entity.Property(module => module.Description).HasMaxLength(1000);
            entity.HasIndex(module => new { module.TrainingId, module.Slug }).IsUnique();
            entity.HasIndex(module => new { module.TrainingId, module.Order }).IsUnique();
            entity.HasOne(module => module.Training)
                .WithMany(training => training.Modules)
                .HasForeignKey(module => module.TrainingId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Lesson>(entity =>
        {
            entity.Property(lesson => lesson.Title).HasMaxLength(200).IsRequired();
            entity.Property(lesson => lesson.Slug).HasMaxLength(220).IsRequired();
            entity.Property(lesson => lesson.Summary).HasMaxLength(1000);
            entity.HasIndex(lesson => new { lesson.TrainingModuleId, lesson.Slug }).IsUnique();
            entity.HasIndex(lesson => new { lesson.TrainingModuleId, lesson.Order }).IsUnique();
            entity.HasOne(lesson => lesson.TrainingModule)
                .WithMany(module => module.Lessons)
                .HasForeignKey(lesson => lesson.TrainingModuleId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<LessonContent>(entity =>
        {
            entity.Property(content => content.Title).HasMaxLength(200);
            entity.Property(content => content.ExternalUrl).HasMaxLength(1000);
            entity.Property(content => content.Description).HasMaxLength(1000);
            entity.HasIndex(content => new { content.LessonId, content.Order }).IsUnique();
            entity.HasOne(content => content.Lesson)
                .WithMany(lesson => lesson.Contents)
                .HasForeignKey(content => content.LessonId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Assessment>(entity =>
        {
            entity.Property(item => item.Title).HasMaxLength(200).IsRequired();
            entity.Property(item => item.Slug).HasMaxLength(220).IsRequired();
            entity.Property(item => item.Description).HasMaxLength(2000);
            entity.Property(item => item.PassingScore).HasPrecision(5, 2);
            entity.HasIndex(item => new { item.LessonId, item.Slug }).IsUnique();
            entity.HasIndex(item => new { item.LessonId, item.Order }).IsUnique();
            entity.HasOne(item => item.Lesson).WithMany(item => item.Assessments)
                .HasForeignKey(item => item.LessonId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Question>(entity =>
        {
            entity.Property(item => item.Statement).IsRequired();
            entity.Property(item => item.ExpectedAnswer).HasMaxLength(2000);
            entity.Property(item => item.Points).HasPrecision(10, 2);
            entity.HasIndex(item => new { item.AssessmentId, item.Order }).IsUnique();
            entity.HasOne(item => item.Assessment).WithMany(item => item.Questions)
                .HasForeignKey(item => item.AssessmentId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<AnswerOption>(entity =>
        {
            entity.Property(item => item.Text).HasMaxLength(1000).IsRequired();
            entity.HasIndex(item => new { item.QuestionId, item.Order }).IsUnique();
            entity.HasOne(item => item.Question).WithMany(item => item.AnswerOptions)
                .HasForeignKey(item => item.QuestionId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Enrollment>(entity =>
        {
            entity.Property(item => item.LearnerId).IsRequired();
            entity.Property(item => item.ProgressPercentage).HasPrecision(5, 2);
            entity.HasIndex(item => new { item.LearnerId, item.TrainingId });
            entity.HasOne<ApplicationUser>().WithMany()
                .HasForeignKey(item => item.LearnerId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ApplicationUser>().WithMany()
                .HasForeignKey(item => item.CreatedByAdminId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(item => item.Training).WithMany(item => item.Enrollments)
                .HasForeignKey(item => item.TrainingId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<LessonProgress>(entity =>
        {
            entity.HasIndex(item => new { item.EnrollmentId, item.LessonId }).IsUnique();
            entity.HasOne(item => item.Enrollment).WithMany(item => item.LessonProgresses)
                .HasForeignKey(item => item.EnrollmentId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(item => item.Lesson).WithMany(item => item.ProgressRecords)
                .HasForeignKey(item => item.LessonId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<AssessmentAttempt>(entity =>
        {
            entity.Property(item => item.Score).HasPrecision(10, 2);
            entity.Property(item => item.MaximumScore).HasPrecision(10, 2);
            entity.Property(item => item.PercentageScore).HasPrecision(5, 2);
            entity.Property(item => item.ConcurrencyToken).IsConcurrencyToken();
            entity.HasIndex(item => new { item.EnrollmentId, item.AssessmentId, item.AttemptNumber }).IsUnique();
            entity.HasOne(item => item.Enrollment).WithMany(item => item.Attempts)
                .HasForeignKey(item => item.EnrollmentId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(item => item.Assessment).WithMany(item => item.Attempts)
                .HasForeignKey(item => item.AssessmentId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<AttemptQuestion>(entity =>
        {
            entity.Property(item => item.StatementSnapshot).IsRequired();
            entity.Property(item => item.PointsSnapshot).HasPrecision(10, 2);
            entity.Property(item => item.ExpectedAnswerSnapshot).HasMaxLength(2000);
            entity.HasIndex(item => new { item.AssessmentAttemptId, item.DisplayOrder }).IsUnique();
            entity.HasIndex(item => new { item.AssessmentAttemptId, item.QuestionId }).IsUnique();
            entity.HasOne(item => item.AssessmentAttempt).WithMany(item => item.Questions)
                .HasForeignKey(item => item.AssessmentAttemptId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(item => item.Question).WithMany()
                .HasForeignKey(item => item.QuestionId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<LearnerAnswer>(entity =>
        {
            entity.Property(item => item.TextAnswer).HasMaxLength(2000);
            entity.Property(item => item.AnswerTextSnapshot).HasMaxLength(1000);
            entity.Property(item => item.PointsAwarded).HasPrecision(10, 2);
            entity.HasIndex(item => new { item.AttemptQuestionId, item.AnswerOptionId }).IsUnique();
            entity.HasOne(item => item.AttemptQuestion).WithMany(item => item.Answers)
                .HasForeignKey(item => item.AttemptQuestionId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<AnswerOption>().WithMany()
                .HasForeignKey(item => item.AnswerOptionId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
