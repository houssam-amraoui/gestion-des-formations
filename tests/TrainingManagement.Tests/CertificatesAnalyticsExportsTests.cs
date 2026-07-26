using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TrainingManagement.Application.Analytics;
using TrainingManagement.Application.Certificates;
using TrainingManagement.Application.Completion;
using TrainingManagement.Domain.Constants;
using TrainingManagement.Domain.Entities;
using TrainingManagement.Domain.Enums;
using TrainingManagement.Infrastructure.Certificates;
using TrainingManagement.Infrastructure.Identity;
using TrainingManagement.Infrastructure.Persistence;
using TrainingManagement.Infrastructure.Services;
using AdminCertificatesController = TrainingManagement.Web.Areas.Admin.Controllers.CertificatesController;
using TrainerCertificatesController = TrainingManagement.Web.Areas.Trainer.Controllers.CertificatesController;
using LearnerCertificatesController = TrainingManagement.Web.Areas.Learner.Controllers.CertificatesController;

namespace TrainingManagement.Tests;

public sealed class CertificatesAnalyticsExportsTests
{
    [Fact] public void TrainingCompletionDefaults_AreEnabled(){var x=new Training();Assert.True(x.RequireAllLessonsCompleted);Assert.True(x.RequireAllMandatoryAssessmentsPassed);Assert.True(x.CertificateEnabled);}
    [Fact] public void Assessment_IsNotMandatoryByDefault()=>Assert.False(new Assessment().IsMandatory);
    [Fact] public void Certificate_IsActiveByDefault()=>Assert.Equal(CertificateStatus.Active,new Certificate().Status);
    [Fact] public void Certificate_ActiveWithoutExpiration_IsValid()=>Assert.True(new Certificate().IsValidAt(DateTime.UtcNow));
    [Fact] public void Certificate_Expired_IsNotValid(){var x=new Certificate{ExpiresAt=DateTime.UtcNow.AddMinutes(-1)};x.RefreshExpiration(DateTime.UtcNow);Assert.Equal(CertificateStatus.Expired,x.Status);}
    [Fact] public void Certificate_RevocationRequiresReason()=>Assert.Throws<InvalidOperationException>(()=>new Certificate().Revoke(" ","admin",DateTime.UtcNow));
    [Fact] public void Certificate_CanBeRevoked(){var x=new Certificate();x.Revoke("Erreur","admin",DateTime.UtcNow);Assert.Equal(CertificateStatus.Revoked,x.Status);Assert.Equal("Erreur",x.RevocationReason);}
    [Fact] public void Certificate_ExpiredCannotBeReactivated()=>Assert.Throws<InvalidOperationException>(()=>new Certificate{ExpiresAt=DateTime.UtcNow.AddDays(-1)}.Reactivate(DateTime.UtcNow));
    [Fact] public void Certificate_CanBeReactivated(){var x=new Certificate{Status=CertificateStatus.Revoked};x.Reactivate(DateTime.UtcNow);Assert.Equal(CertificateStatus.Active,x.Status);}
    [Fact] public void Certificate_SnapshotsAreIndependent(){var x=new Certificate{LearnerFullNameSnapshot="Ancien",TrainingTitleSnapshot="Formation A"};Assert.Equal("Ancien",x.LearnerFullNameSnapshot);Assert.Equal("Formation A",x.TrainingTitleSnapshot);}

    [Theory,InlineData(2024),InlineData(2025),InlineData(2026),InlineData(2030),InlineData(2099)]
    public void CertificateNumber_HasReadableSecureFormat(int year)
    {var value=new CertificateNumberGenerator().CreateCertificateNumber(new DateTime(year,1,1));Assert.Matches($"^CERT-{year}-[0-9A-F]{{8}}$",value);}

    [Theory,InlineData(1),InlineData(2),InlineData(3),InlineData(4),InlineData(5)]
    public void VerificationCode_IsSecureAndLong(int _)
    {var value=new CertificateNumberGenerator().CreateVerificationCode();Assert.True(value.Length>=24);Assert.Matches("^[0-9A-F]+$",value);}

    [Theory,InlineData("=1+1"),InlineData("+cmd"),InlineData("-2"),InlineData("@SUM(A1)")]
    public void Csv_FormulaPrefixesAreNeutralized(string value)
    {var text=Text(new CsvExportService(null!,NullLogger<CsvExportService>.Instance).Create(new[]{new[]{value}}));Assert.Contains("\"'"+value+"\"",text);}

    [Theory,InlineData("avec;point"),InlineData("avec,virgule"),InlineData("avec \"guillemet\""),InlineData("été")]
    public void Csv_ValuesAreQuotedAndUtf8(string value)
    {var bytes=new CsvExportService(null!,NullLogger<CsvExportService>.Instance).Create(new[]{new[]{value}});Assert.Equal(new byte[]{0xEF,0xBB,0xBF},bytes.Take(3));Assert.Contains(value.Replace("\"","\"\""),Text(bytes));}

    [Theory,InlineData(AnalyticsPeriod.Last7Days,7),InlineData(AnalyticsPeriod.Last30Days,30),InlineData(AnalyticsPeriod.Last90Days,90),InlineData(AnalyticsPeriod.CurrentYear,400)]
    public void Analytics_PeriodsAreValid(AnalyticsPeriod period,int maximumDays)
    {var range=AnalyticsService.Range(new(period));Assert.InRange((range.To-range.From).TotalDays,1,maximumDays);}

    [Fact] public void Analytics_CustomRequiresDates()=>Assert.Throws<ArgumentException>(()=>AnalyticsService.Range(new(AnalyticsPeriod.Custom)));
    [Fact] public void Analytics_CustomRejectsReverseRange()=>Assert.Throws<ArgumentException>(()=>AnalyticsService.Range(new(AnalyticsPeriod.Custom,DateTime.UtcNow,DateTime.UtcNow.AddDays(-1))));
    [Fact] public void Analytics_CustomRejectsMoreThan366Days()=>Assert.Throws<ArgumentException>(()=>AnalyticsService.Range(new(AnalyticsPeriod.Custom,DateTime.UtcNow.AddDays(-400),DateTime.UtcNow)));

    [Fact] public void Pdf_IsNotEmpty(){var bytes=Pdf();Assert.True(bytes.Length>1000);}
    [Fact] public void Pdf_HasValidSignature(){var bytes=Pdf();Assert.Equal("%PDF",System.Text.Encoding.ASCII.GetString(bytes,0,4));}
    [Fact] public void Pdf_ContainsAtLeastOnePage(){var bytes=Pdf();Assert.Contains("/Type/Page",System.Text.Encoding.ASCII.GetString(bytes));}
    [Fact] public void Pdf_UsesConfiguredVerificationUrl(){var model=PdfModel();Assert.EndsWith("/Certificates/Verify/CODE123456789012345678901234",model.VerificationUrl);}

    [Fact] public async Task Storage_WritesAndReadsPdf(){using var d=Temp();var s=Storage(d.Path);await s.SaveAsync("server.pdf",[1,2,3]);Assert.Equal(new byte[]{1,2,3},await s.ReadAsync("server.pdf"));}
    [Fact] public async Task Storage_RejectsTraversalOnSave(){using var d=Temp();await Assert.ThrowsAsync<InvalidOperationException>(()=>Storage(d.Path).SaveAsync("../bad.pdf",[1]));}
    [Fact] public async Task Storage_RejectsTraversalOnRead(){using var d=Temp();await Assert.ThrowsAsync<InvalidOperationException>(()=>Storage(d.Path).ReadAsync("../bad.pdf"));}
    [Fact] public async Task Storage_RejectsNonPdfNames(){using var d=Temp();await Assert.ThrowsAsync<InvalidOperationException>(()=>Storage(d.Path).SaveAsync("bad.txt",[1]));}

    [Fact] public async Task Completion_MissingLessonBlocks(){await using var x=await CompletionData();var r=await x.Service.EvaluateAsync(x.Enrollment.Id);Assert.False(r!.IsEligible);}
    [Fact] public async Task Completion_AllLessonsSatisfyCriterion(){await using var x=await CompletionData();await CompleteLesson(x);var r=await x.Service.EvaluateAsync(x.Enrollment.Id);Assert.True(r!.Requirements.Single(q=>q.Code=="lessons").IsMet);}
    [Fact] public async Task Completion_MandatoryFailureBlocks(){await using var x=await CompletionData();await CompleteLesson(x);var r=await x.Service.EvaluateAsync(x.Enrollment.Id);Assert.False(r!.Requirements.Single(q=>q.Code=="mandatory-assessments").IsMet);}
    [Fact] public async Task Completion_MandatorySuccessPasses(){await using var x=await CompletionData();await CompleteLesson(x);await Pass(x,80);var r=await x.Service.EvaluateAsync(x.Enrollment.Id);Assert.True(r!.IsEligible);}
    [Fact] public async Task Completion_MinimumAverageIsApplied(){await using var x=await CompletionData(90);await CompleteLesson(x);await Pass(x,80);Assert.False((await x.Service.EvaluateAsync(x.Enrollment.Id))!.IsEligible);}
    [Fact] public async Task Completion_ArchivedAssessmentIsIgnored(){await using var x=await CompletionData();x.Assessment.IsArchived=true;await x.Db.SaveChangesAsync();await CompleteLesson(x);Assert.True((await x.Service.EvaluateAsync(x.Enrollment.Id))!.IsEligible);}
    [Fact] public async Task Completion_FinalizeSetsDatesAndHundred(){await using var x=await CompletionData();await CompleteLesson(x);await Pass(x,90);var r=await x.Service.FinalizeAsync(x.Enrollment.Id);Assert.True(r.Value!.Completed);var e=await x.Db.Enrollments.FindAsync(x.Enrollment.Id);Assert.Equal(100,e!.ProgressPercentage);Assert.NotNull(e.CompletedAt);}
    [Fact] public async Task Completion_IsIdempotent(){await using var x=await CompletionData();await CompleteLesson(x);await Pass(x,90);await x.Service.FinalizeAsync(x.Enrollment.Id);var r=await x.Service.FinalizeAsync(x.Enrollment.Id);Assert.True(r.Value!.WasAlreadyCompleted);}

    [Fact] public void CertificateNumber_IndexIsUnique()=>Assert.True(Index<Certificate>(nameof(Certificate.CertificateNumber)).IsUnique);
    [Fact] public void VerificationCode_IndexIsUnique()=>Assert.True(Index<Certificate>(nameof(Certificate.VerificationCode)).IsUnique);
    [Fact] public void EnrollmentCertificate_IndexIsUnique()=>Assert.True(Index<Certificate>(nameof(Certificate.EnrollmentId)).IsUnique);
    [Fact] public void EnrollmentTraining_IndexIsUnique(){using var db=Db();Assert.True(db.Model.FindEntityType(typeof(Enrollment))!.GetIndexes().Single(x=>x.Properties.Select(p=>p.Name).SequenceEqual([nameof(Enrollment.LearnerId),nameof(Enrollment.TrainingId)])).IsUnique);}
    [Fact] public void Certificate_UsesRestrictDelete(){using var db=Db();Assert.All(db.Model.FindEntityType(typeof(Certificate))!.GetForeignKeys(),x=>Assert.Equal(DeleteBehavior.Restrict,x.DeleteBehavior));}

    [Fact] public async Task Certificate_GenerationRequiresEligibleEnrollment()
    {
        await using var fixture=await CertificateData(includeIncompleteLesson:true);
        var result=await fixture.Service.GenerateAsync(fixture.Enrollment.Id);
        Assert.False(result.Succeeded);
        Assert.Empty(fixture.Db.Certificates);
    }
    [Fact] public async Task Certificate_DisabledTrainingRefusesGeneration()
    {
        await using var fixture=await CertificateData(certificateEnabled:false);
        var result=await fixture.Service.GenerateAsync(fixture.Enrollment.Id);
        Assert.False(result.Succeeded);
        Assert.Empty(fixture.Db.Certificates);
    }
    [Fact] public async Task Certificate_GenerationIsIdempotentAndKeepsSnapshots()
    {
        await using var fixture=await CertificateData();
        var first=await fixture.Service.GenerateAsync(fixture.Enrollment.Id);
        var second=await fixture.Service.GenerateAsync(fixture.Enrollment.Id);
        Assert.True(first.Succeeded);Assert.True(second.Succeeded);
        Assert.True(first.Value!.Created);Assert.False(second.Value!.Created);
        Assert.Equal(first.Value.CertificateNumber,second.Value.CertificateNumber);
        var certificate=await fixture.Db.Certificates.SingleAsync();
        Assert.Equal("Jean Apprenant",certificate.LearnerFullNameSnapshot);
        Assert.Equal("Formation certificat",certificate.TrainingTitleSnapshot);
    }
    [Fact] public async Task Certificate_ValidityMonthsCalculatesExpiration()
    {
        await using var fixture=await CertificateData(validityMonths:12);
        var result=await fixture.Service.GenerateAsync(fixture.Enrollment.Id);
        var certificate=await fixture.Db.Certificates.SingleAsync();
        Assert.True(result.Succeeded);
        Assert.NotNull(certificate.ExpiresAt);
        Assert.Equal(12,(certificate.ExpiresAt!.Value.Year-certificate.IssuedAt.Year)*12+
            certificate.ExpiresAt.Value.Month-certificate.IssuedAt.Month);
    }
    [Fact] public async Task Certificate_PublicVerificationHandlesActiveRevokedAndUnknown()
    {
        await using var fixture=await CertificateData();
        var generated=await fixture.Service.GenerateAsync(fixture.Enrollment.Id);
        var active=await fixture.Service.VerifyAsync(generated.Value!.VerificationCode);
        await fixture.Service.RevokeAsync(new(generated.Value.CertificateId,"Erreur administrative","admin"));
        var revoked=await fixture.Service.VerifyAsync(generated.Value.VerificationCode);
        var unknown=await fixture.Service.VerifyAsync("CODE-INCONNU");
        Assert.True(active.Found&&active.IsValid);
        Assert.True(revoked.Found);Assert.False(revoked.IsValid);Assert.Equal(CertificateStatus.Revoked,revoked.Status);
        Assert.False(unknown.Found);
    }
    [Fact] public async Task Certificate_DownloadIsLimitedToOwner()
    {
        await using var fixture=await CertificateData();
        var generated=await fixture.Service.GenerateAsync(fixture.Enrollment.Id);
        Assert.Null(await fixture.Service.DownloadAsync(generated.Value!.CertificateId,"other",false,false));
        var own=await fixture.Service.DownloadAsync(generated.Value.CertificateId,"learner",false,false);
        Assert.NotNull(own);Assert.Equal("%PDF",System.Text.Encoding.ASCII.GetString(own!.Content));
    }
    [Fact] public async Task Certificate_RegenerationKeepsNumber()
    {
        await using var fixture=await CertificateData();
        var generated=await fixture.Service.GenerateAsync(fixture.Enrollment.Id);
        var number=generated.Value!.CertificateNumber;
        Assert.True((await fixture.Service.RegeneratePdfAsync(generated.Value.CertificateId)).Succeeded);
        Assert.Equal(number,(await fixture.Db.Certificates.SingleAsync()).CertificateNumber);
        Assert.Equal(2,fixture.Storage.SaveCount);
    }
    [Fact] public async Task Certificate_TrainerScopeUsesAssignedTraining()
    {
        await using var fixture=await CertificateData(trainerId:"trainer");
        var generated=await fixture.Service.GenerateAsync(fixture.Enrollment.Id);
        Assert.NotNull(await fixture.Service.GetForTrainerAsync(generated.Value!.CertificateId,"trainer"));
        Assert.Null(await fixture.Service.GetForTrainerAsync(generated.Value.CertificateId,"other-trainer"));
    }

    [Theory,InlineData(typeof(AdminCertificatesController),AppRoles.Admin),InlineData(typeof(TrainerCertificatesController),AppRoles.Trainer),InlineData(typeof(LearnerCertificatesController),AppRoles.Learner)]
    public void CertificateControllers_RequireExpectedRole(Type controller,string role)
    {Assert.Equal(role,controller.GetCustomAttribute<AuthorizeAttribute>()!.Roles);}
    [Fact] public void PublicVerification_DoesNotRequireAuthentication()=>Assert.Null(typeof(TrainingManagement.Web.Controllers.CertificatesController).GetCustomAttribute<AuthorizeAttribute>());
    [Fact] public void AdminExports_RequireAdmin()=>Assert.Equal(AppRoles.Admin,typeof(TrainingManagement.Web.Areas.Admin.Controllers.ExportsController).GetCustomAttribute<AuthorizeAttribute>()!.Roles);

    private static byte[] Pdf()=>new CertificatePdfService().Generate(PdfModel());
    private static CertificatePdfModel PdfModel()=>new("Training Management","Jean Dupont","ASP.NET Core","Formateur",DateTime.UtcNow,null,"CERT-2026-ABCDEF12","CODE123456789012345678901234","http://localhost:5012/Certificates/Verify/CODE123456789012345678901234");
    private static string Text(byte[] bytes)=>System.Text.Encoding.UTF8.GetString(bytes);
    private static ApplicationDbContext Db()=>new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private static Microsoft.EntityFrameworkCore.Metadata.IIndex Index<T>(string property){using var db=Db();return db.Model.FindEntityType(typeof(T))!.GetIndexes().Single(x=>x.Properties.Single().Name==property);}
    private static LocalCertificateStorageService Storage(string root)=>new(new Host{ContentRootPath=root},Options.Create(new CertificateStorageOptions{BasePath="certs"}));
    private static TemporaryDirectory Temp()=>new();

    private static async Task<CompletionFixture> CompletionData(decimal minimum=60)
    {
        var db=Db();var cat=new Category{Name="C",Slug=Guid.NewGuid().ToString()};var training=new Training{Title="T",Slug=Guid.NewGuid().ToString(),ShortDescription="S",Description="D",Language="fr",EstimatedDurationHours=1,Category=cat,Status=TrainingStatus.Published,MinimumAverageScore=minimum};var module=new TrainingModule{Training=training,Title="M",Slug="m",Order=1,IsPublished=true};var lesson=new Lesson{TrainingModule=module,Title="L",Slug="l",Order=1,IsPublished=true};var assessment=new Assessment{Lesson=lesson,Title="Q",Slug="q",Order=1,IsPublished=true,IsMandatory=true};var enrollment=new Enrollment{LearnerId="learner",Training=training,Status=EnrollmentStatus.Active};db.AddRange(lesson,assessment,enrollment);await db.SaveChangesAsync();return new(db,new TrainingCompletionService(db),enrollment,lesson,assessment);
    }
    private static async Task CompleteLesson(CompletionFixture x){x.Db.LessonProgresses.Add(new(){EnrollmentId=x.Enrollment.Id,LessonId=x.Lesson.Id,Status=LessonProgressStatus.Completed});await x.Db.SaveChangesAsync();}
    private static async Task Pass(CompletionFixture x,decimal score){x.Db.AssessmentAttempts.Add(new(){EnrollmentId=x.Enrollment.Id,AssessmentId=x.Assessment.Id,AttemptNumber=1,Status=AttemptStatus.Submitted,PercentageScore=score,Passed=score>=70});await x.Db.SaveChangesAsync();}
    private static async Task<CertificateFixture> CertificateData(bool includeIncompleteLesson=false,
        bool certificateEnabled=true,int? validityMonths=null,string? trainerId=null)
    {
        var db=Db();
        db.Users.Add(new ApplicationUser{Id="learner",FirstName="Jean",LastName="Apprenant",
            UserName="learner@test.local",Email="learner@test.local"});
        if(trainerId is not null)db.Users.Add(new ApplicationUser{Id=trainerId,FirstName="Anne",LastName="Formatrice",
            UserName="trainer@test.local",Email="trainer@test.local"});
        var category=new Category{Name="Certificats",Slug=Guid.NewGuid().ToString("N")};
        var training=new Training{Title="Formation certificat",Slug=Guid.NewGuid().ToString("N"),
            ShortDescription="Description",Description="Description",Language="fr",
            EstimatedDurationHours=1,Category=category,Status=TrainingStatus.Published,
            CertificateEnabled=certificateEnabled,CertificateValidityMonths=validityMonths,TrainerId=trainerId};
        var enrollment=new Enrollment{LearnerId="learner",Training=training,Status=EnrollmentStatus.Active};
        db.Enrollments.Add(enrollment);
        if(includeIncompleteLesson)
        {
            var module=new TrainingModule{Training=training,Title="Module",Slug="module",Order=1,IsPublished=true};
            db.Lessons.Add(new Lesson{TrainingModule=module,Title="Leçon",Slug="lecon",Order=1,IsPublished=true});
        }
        await db.SaveChangesAsync();
        var storage=new MemoryStorage();
        var service=new CertificateService(db,new TrainingCompletionService(db),new CertificateNumberGenerator(),
            new FakePdf(),storage,Options.Create(new ApplicationOptions
            {Name="Training Management",PublicBaseUrl="http://localhost:5012"}),
            NullLogger<CertificateService>.Instance);
        return new(db,service,enrollment,storage);
    }
    private sealed record CompletionFixture(ApplicationDbContext Db,TrainingCompletionService Service,Enrollment Enrollment,Lesson Lesson,Assessment Assessment):IAsyncDisposable{public ValueTask DisposeAsync()=>Db.DisposeAsync();}
    private sealed record CertificateFixture(ApplicationDbContext Db,CertificateService Service,
        Enrollment Enrollment,MemoryStorage Storage):IAsyncDisposable{public ValueTask DisposeAsync()=>Db.DisposeAsync();}
    private sealed class FakePdf:ICertificatePdfService
    {public byte[] Generate(CertificatePdfModel model)=>System.Text.Encoding.ASCII.GetBytes("%PDF");}
    private sealed class MemoryStorage:ICertificateStorageService
    {
        private readonly Dictionary<string,byte[]> files=[];
        public int SaveCount{get;private set;}
        public Task<string> SaveAsync(string serverFileName,byte[] content,CancellationToken cancellationToken=default)
        {SaveCount++;files[serverFileName]=content;return Task.FromResult(serverFileName);}
        public Task<byte[]?> ReadAsync(string relativePath,CancellationToken cancellationToken=default)=>
            Task.FromResult(files.TryGetValue(relativePath,out var content)?content:null);
    }
    private sealed class Host:IWebHostEnvironment{public string ApplicationName{get;set;}="Tests";public IFileProvider WebRootFileProvider{get;set;}=new NullFileProvider();public string WebRootPath{get;set;}="";public string EnvironmentName{get;set;}="Development";public string ContentRootPath{get;set;}="";public IFileProvider ContentRootFileProvider{get;set;}=new NullFileProvider();}
    private sealed class TemporaryDirectory:IDisposable{public string Path{get;}=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"tm-"+Guid.NewGuid().ToString("N"));public void Dispose(){if(Directory.Exists(Path))Directory.Delete(Path,true);}}
}
