using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TrainingManagement.Application.AnswerOptions;
using TrainingManagement.Application.Assessments;
using TrainingManagement.Application.Questions;
using TrainingManagement.Domain.Constants;
using TrainingManagement.Domain.Entities;
using TrainingManagement.Domain.Enums;
using TrainingManagement.Infrastructure.Identity;
using TrainingManagement.Infrastructure.Persistence;
using TrainingManagement.Infrastructure.Services;
using AdminAssessmentsController = TrainingManagement.Web.Areas.Admin.Controllers.AssessmentsController;
using TrainerAssessmentsController = TrainingManagement.Web.Areas.Trainer.Controllers.AssessmentsController;

namespace TrainingManagement.Tests;

public sealed class AssessmentsQuestionsAnswersTests
{
    [Fact] public async Task Assessment_BelongsToLesson(){await using var p=Provider();using var s=p.CreateScope();var l=await Lesson(s);var r=await A(s).CreateAsync(NewAssessment(l.Id));Assert.Equal(l.Id,(await Db(s).Assessments.FindAsync(r.Value))!.LessonId);}
    [Fact] public async Task AssessmentOrder_IsUnique(){await using var p=Provider();using var s=p.CreateScope();var l=await Lesson(s);await A(s).CreateAsync(NewAssessment(l.Id,"a",1));await A(s).CreateAsync(NewAssessment(l.Id,"b",1));Assert.Equal([1,2],await Db(s).Assessments.OrderBy(x=>x.Order).Select(x=>x.Order).ToListAsync());}
    [Fact] public async Task AssessmentSlug_IsUnique(){await using var p=Provider();using var s=p.CreateScope();var l=await Lesson(s);Assert.True((await A(s).CreateAsync(NewAssessment(l.Id,"same"))).Succeeded);Assert.False((await A(s).CreateAsync(NewAssessment(l.Id,"same"))).Succeeded);}
    [Theory][InlineData(-1)][InlineData(101)] public async Task PassingScore_MustBeInRange(decimal score){await using var p=Provider();using var s=p.CreateScope();var l=await Lesson(s);Assert.False((await A(s).CreateAsync(NewAssessment(l.Id,passing:score))).Succeeded);}
    [Fact] public async Task MaximumAttempts_MustBePositive(){await using var p=Provider();using var s=p.CreateScope();var l=await Lesson(s);Assert.False((await A(s).CreateAsync(NewAssessment(l.Id,attempts:0))).Succeeded);}
    [Fact] public async Task TimeLimit_MustBePositive(){await using var p=Provider();using var s=p.CreateScope();var l=await Lesson(s);Assert.False((await A(s).CreateAsync(NewAssessment(l.Id,minutes:0))).Succeeded);}
    [Fact] public async Task AssessmentWithoutPublishedQuestion_CannotPublish(){await using var p=Provider();using var s=p.CreateScope();var l=await Lesson(s);var a=await A(s).CreateAsync(NewAssessment(l.Id));Assert.False((await A(s).PublishAsync(a.Value)).Succeeded);}
    [Fact] public async Task ArchivedAssessment_IsNotPublic(){await using var p=Provider();using var s=p.CreateScope();var setup=await PublishedAssessment(s);await A(s).ArchiveAsync(setup.Assessment.Id);Assert.Empty(await A(s).GetPublicByLessonAsync(setup.Lesson.Id));}
    [Fact] public async Task AssessmentMoveUpAndDown_Works(){await using var p=Provider();using var s=p.CreateScope();var l=await Lesson(s);var a=await A(s).CreateAsync(NewAssessment(l.Id,"a"));var b=await A(s).CreateAsync(NewAssessment(l.Id,"b"));await A(s).MoveUpAsync(b.Value);Assert.Equal(1,(await Db(s).Assessments.FindAsync(b.Value))!.Order);await A(s).MoveDownAsync(b.Value);Assert.Equal(2,(await Db(s).Assessments.FindAsync(b.Value))!.Order);}

    [Fact] public void SingleChoice_RequiresTwoOptions(){Assert.NotNull(Question(QuestionType.SingleChoice,("Oui",true)).GetPublicationError());}
    [Fact] public void SingleChoice_RequiresOneCorrect(){Assert.NotNull(Question(QuestionType.SingleChoice,("Oui",false),("Non",false)).GetPublicationError());}
    [Fact] public void SingleChoice_RejectsSeveralCorrect(){Assert.NotNull(Question(QuestionType.SingleChoice,("Oui",true),("Non",true)).GetPublicationError());}
    [Fact] public void MultipleChoice_RequiresTwoOptions(){Assert.NotNull(Question(QuestionType.MultipleChoice,("Oui",true)).GetPublicationError());}
    [Fact] public void MultipleChoice_RequiresCorrectOption(){Assert.NotNull(Question(QuestionType.MultipleChoice,("A",false),("B",false)).GetPublicationError());}
    [Fact] public void MultipleChoice_AllowsSeveralCorrect(){Assert.Null(Question(QuestionType.MultipleChoice,("A",true),("B",true)).GetPublicationError());}
    [Fact] public void TrueFalse_RequiresExactlyTwoOptions(){Assert.NotNull(Question(QuestionType.TrueFalse,("Vrai",true)).GetPublicationError());}
    [Fact] public async Task TrueFalseOptions_CanBeGenerated(){await using var p=Provider();using var s=p.CreateScope();var x=await Assessment(s);var q=await Q(s).CreateAsync(new(x.Id,QuestionType.TrueFalse,"Test",null,null,1,null));Assert.True((await O(s).CreateTrueFalseAsync(q.Value)).Succeeded);Assert.Equal(["Vrai","Faux"],await Db(s).AnswerOptions.OrderBy(o=>o.Order).Select(o=>o.Text).ToListAsync());}
    [Fact] public void TrueFalse_RequiresOneCorrect(){Assert.NotNull(Question(QuestionType.TrueFalse,("Vrai",true),("Faux",true)).GetPublicationError());}
    [Fact] public void ShortAnswer_RejectsOptions(){var q=Question(QuestionType.ShortAnswer,("Choix",true));q.ExpectedAnswer="Réponse";Assert.NotNull(q.GetPublicationError());}
    [Fact] public void ShortAnswer_RequiresExpectedAnswer(){Assert.NotNull(Question(QuestionType.ShortAnswer).GetPublicationError());}
    [Fact] public async Task PublicModel_NeverContainsExpectedAnswer(){await using var p=Provider();using var s=p.CreateScope();var setup=await PublishedAssessment(s,QuestionType.ShortAnswer);var page=await A(s).GetPublicAsync(setup.Training.Slug,setup.Module.Slug,setup.Lesson.Slug,setup.Assessment.Slug);Assert.NotNull(page);Assert.DoesNotContain("ExpectedAnswer",page!.Questions.Single().GetType().GetProperties().Select(x=>x.Name));}

    [Fact] public async Task QuestionOrder_IsUnique(){await using var p=Provider();using var s=p.CreateScope();var a=await Assessment(s);await Q(s).CreateAsync(NewQuestion(a.Id,"A",1));await Q(s).CreateAsync(NewQuestion(a.Id,"B",1));Assert.Equal([1,2],await Db(s).Questions.OrderBy(x=>x.Order).Select(x=>x.Order).ToListAsync());}
    [Fact] public async Task AnswerOrder_IsUnique(){await using var p=Provider();using var s=p.CreateScope();var a=await Assessment(s);var q=await Q(s).CreateAsync(NewQuestion(a.Id));await O(s).CreateAsync(new(q.Value,"A",1,true));await O(s).CreateAsync(new(q.Value,"B",1,false));Assert.Equal([1,2],await Db(s).AnswerOptions.OrderBy(x=>x.Order).Select(x=>x.Order).ToListAsync());}
    [Fact] public async Task InvalidQuestion_CannotPublish(){await using var p=Provider();using var s=p.CreateScope();var a=await Assessment(s);var q=await Q(s).CreateAsync(NewQuestion(a.Id));Assert.False((await Q(s).PublishAsync(q.Value)).Succeeded);}
    [Fact] public async Task UnpublishedQuestion_IsNotPublic(){await using var p=Provider();using var s=p.CreateScope();var setup=await PublishedAssessment(s);var q=await Db(s).Questions.SingleAsync();q.IsPublished=false;await Db(s).SaveChangesAsync();var page=await A(s).GetPublicAsync(setup.Training.Slug,setup.Module.Slug,setup.Lesson.Slug,setup.Assessment.Slug);Assert.Empty(page!.Questions);}
    [Fact] public async Task PublicChoices_AreOrdered(){await using var p=Provider();using var s=p.CreateScope();var setup=await PublishedAssessment(s);var page=await A(s).GetPublicAsync(setup.Training.Slug,setup.Module.Slug,setup.Lesson.Slug,setup.Assessment.Slug);Assert.Equal(["A","B"],page!.Questions.Single().Options.Select(x=>x.Text));}
    [Fact] public async Task PublicChoices_DoNotExposeCorrectFlag(){await using var p=Provider();using var s=p.CreateScope();var setup=await PublishedAssessment(s);var page=await A(s).GetPublicAsync(setup.Training.Slug,setup.Module.Slug,setup.Lesson.Slug,setup.Assessment.Slug);Assert.DoesNotContain("IsCorrect",page!.Questions.Single().Options.First().GetType().GetProperties().Select(x=>x.Name));}
    [Fact] public async Task AdminPreview_RevealsCorrectAnswers(){await using var p=Provider();using var s=p.CreateScope();var setup=await PublishedAssessment(s);var view=await A(s).GetAdminPreviewAsync(setup.Assessment.Id);Assert.True(view!.Questions.Single().Options.Single(x=>x.Text=="A").IsCorrect);}
    [Fact] public async Task TotalPoints_AreCalculated(){await using var p=Provider();using var s=p.CreateScope();var a=await Assessment(s);await Q(s).CreateAsync(NewQuestion(a.Id,"A",points:2));await Q(s).CreateAsync(NewQuestion(a.Id,"B",points:3));Assert.Equal(5,(await A(s).GetByIdAsync(a.Id))!.TotalPoints);}

    [Fact] public void Learner_CannotManageAssessments(){Assert.Equal(AppRoles.Admin,AdminRole());}
    [Fact] public void Trainer_CannotManageAssessments(){Assert.Equal(AppRoles.Admin,AdminRole());}
    [Fact] public void Admin_CanManageAssessments(){Assert.Equal(AppRoles.Admin,AdminRole());}
    [Fact] public void TrainerController_RequiresTrainerRole(){Assert.Equal(AppRoles.Trainer,typeof(TrainerAssessmentsController).GetCustomAttributes(typeof(AuthorizeAttribute),true).Cast<AuthorizeAttribute>().Single().Roles);}
    [Fact] public async Task Trainer_CanReadAssignedAssessment(){await using var p=Provider();using var s=p.CreateScope();var setup=await PublishedAssessment(s,trainerId:"trainer");Assert.NotNull(await A(s).GetTrainerPreviewAsync(setup.Assessment.Id,"trainer"));}
    [Fact] public async Task Trainer_CannotReadOtherAssessment(){await using var p=Provider();using var s=p.CreateScope();var setup=await PublishedAssessment(s,trainerId:"owner");Assert.Null(await A(s).GetTrainerPreviewAsync(setup.Assessment.Id,"other"));}

    [Fact] public async Task AssessmentSeed_IsIdempotent(){await using var p=Provider(true);using var s=p.CreateScope();await AddTrainerRole(s);var seed=s.ServiceProvider.GetRequiredService<DevelopmentDataSeeder>();await seed.SeedAsync();var first=(await Db(s).Assessments.CountAsync(),await Db(s).Questions.CountAsync(),await Db(s).AnswerOptions.CountAsync());Db(s).ChangeTracker.Clear();await seed.SeedAsync();Assert.Equal(first,(await Db(s).Assessments.CountAsync(),await Db(s).Questions.CountAsync(),await Db(s).AnswerOptions.CountAsync()));}
    [Fact] public async Task AssessmentSeed_CreatesRequestedQuiz(){await using var p=Provider(true);using var s=p.CreateScope();await AddTrainerRole(s);await s.ServiceProvider.GetRequiredService<DevelopmentDataSeeder>().SeedAsync();var quiz=await Db(s).Assessments.Include(x=>x.Questions).ThenInclude(x=>x.AnswerOptions).SingleAsync(x=>x.Slug=="quiz-introduction-aspnet-core");Assert.True(quiz.IsPublished);Assert.Equal(4,quiz.Questions.Count);Assert.Contains(quiz.Questions,x=>x.QuestionType==QuestionType.ShortAnswer&&x.ExpectedAnswer=="Program.cs");}

    private static string? AdminRole()=>typeof(AdminAssessmentsController).GetCustomAttributes(typeof(AuthorizeAttribute),true).Cast<AuthorizeAttribute>().Single().Roles;
    private static ServiceProvider Provider(bool seed=false){var c=new ServiceCollection();c.AddLogging();c.AddDbContext<ApplicationDbContext>(o=>o.UseInMemoryDatabase(Guid.NewGuid().ToString()));c.AddIdentityCore<ApplicationUser>().AddRoles<IdentityRole>().AddEntityFrameworkStores<ApplicationDbContext>();c.AddScoped<IAssessmentService,AssessmentService>();c.AddScoped<IQuestionService,QuestionService>();c.AddScoped<IAnswerOptionService,AnswerOptionService>();if(seed){c.AddSingleton<IOptions<SeedTrainerOptions>>(Options.Create(new SeedTrainerOptions{Email="trainer@seed.test",Password="Trainer123!",FirstName="Seed",LastName="Trainer"}));c.AddScoped<DevelopmentDataSeeder>();}return c.BuildServiceProvider();}
    private static ApplicationDbContext Db(IServiceScope s)=>s.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    private static IAssessmentService A(IServiceScope s)=>s.ServiceProvider.GetRequiredService<IAssessmentService>();
    private static IQuestionService Q(IServiceScope s)=>s.ServiceProvider.GetRequiredService<IQuestionService>();
    private static IAnswerOptionService O(IServiceScope s)=>s.ServiceProvider.GetRequiredService<IAnswerOptionService>();
    private static AssessmentCreateModel NewAssessment(int lessonId,string slug="assessment",int? order=null,decimal passing=50,int? attempts=null,int? minutes=null)=>new(lessonId,$"Assessment {slug}",slug,null,AssessmentType.Quiz,order,passing,attempts,minutes,false,false);
    private static QuestionCreateModel NewQuestion(int assessmentId,string text="Question",int? order=null,decimal points=1)=>new(assessmentId,QuestionType.SingleChoice,text,null,order,points,null);
    private static async Task<Lesson> Lesson(IServiceScope s,string? trainerId=null){var category=new Category{Name="C"+Guid.NewGuid(),Slug="c-"+Guid.NewGuid()};var training=new Training{Title="Training",Slug="t-"+Guid.NewGuid(),ShortDescription="Short",Description="Description",Category=category,Language="Français",EstimatedDurationHours=1,Status=TrainingStatus.Published,TrainerId=trainerId};var module=new TrainingModule{Training=training,Title="Module",Slug="module",Order=1,IsPublished=true};var lesson=new Lesson{TrainingModule=module,Title="Lesson",Slug="lesson",Order=1,IsPublished=true,IsPreview=true};Db(s).Add(lesson);await Db(s).SaveChangesAsync();return lesson;}
    private static async Task<Assessment> Assessment(IServiceScope s,string? trainerId=null){var l=await Lesson(s,trainerId);var r=await A(s).CreateAsync(NewAssessment(l.Id));return (await Db(s).Assessments.FindAsync(r.Value))!;}
    private static Question Question(QuestionType type,params (string Text,bool Correct)[] options){var q=new Question{QuestionType=type,Statement="Question",Points=1,Assessment=new Assessment()};foreach(var o in options)q.AnswerOptions.Add(new AnswerOption{Text=o.Text,IsCorrect=o.Correct});return q;}
    private static async Task<(Training Training,TrainingModule Module,Lesson Lesson,Assessment Assessment)> PublishedAssessment(IServiceScope s,QuestionType type=QuestionType.SingleChoice,string? trainerId=null){var a=await Assessment(s,trainerId);var l=await Db(s).Lessons.Include(x=>x.TrainingModule).ThenInclude(x=>x.Training).SingleAsync(x=>x.Id==a.LessonId);var create=await Q(s).CreateAsync(new(a.Id,type,"Question",null,null,2,type==QuestionType.ShortAnswer?"Secret":null));var q=(await Db(s).Questions.FindAsync(create.Value))!;if(type!=QuestionType.ShortAnswer){await O(s).CreateAsync(new(q.Id,"B",null,false));await O(s).CreateAsync(new(q.Id,"A",1,true));}await Q(s).PublishAsync(q.Id);await A(s).PublishAsync(a.Id);return(l.TrainingModule.Training,l.TrainingModule,l,a);}
    private static async Task AddTrainerRole(IServiceScope s)=>await s.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>().CreateAsync(new IdentityRole(AppRoles.Trainer));
}
