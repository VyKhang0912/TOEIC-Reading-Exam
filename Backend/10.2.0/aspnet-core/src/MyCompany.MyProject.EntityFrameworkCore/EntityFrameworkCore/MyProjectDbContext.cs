using Abp.Zero.EntityFrameworkCore;
using MyCompany.MyProject.Authorization.Roles;
using MyCompany.MyProject.Authorization.Users;
using MyCompany.MyProject.MultiTenancy;
using MyCompany.MyProject.Toeic;
using Microsoft.EntityFrameworkCore;

namespace MyCompany.MyProject.EntityFrameworkCore;

public class MyProjectDbContext : AbpZeroDbContext<Tenant, Role, User, MyProjectDbContext>
{
    /* Define a DbSet for each entity of the application */
    public virtual DbSet<Exam> Exams { get; set; }
    public virtual DbSet<QuestionGroup> QuestionGroups { get; set; }
    public virtual DbSet<Question> Questions { get; set; }
    public virtual DbSet<AnswerOption> AnswerOptions { get; set; }
    public virtual DbSet<ExamAttempt> ExamAttempts { get; set; }
    public virtual DbSet<AttemptAnswer> AttemptAnswers { get; set; }
    public virtual DbSet<ToeicAudio> ToeicAudios { get; set; }
    public virtual DbSet<ExamAllocation> ExamAllocations { get; set; }

    public MyProjectDbContext(DbContextOptions<MyProjectDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Question>(b =>
        {
            b.HasIndex(q => new { q.ExamId, q.QuestionNumber }).IsUnique();
            b.HasOne(q => q.QuestionGroup)
             .WithMany(g => g.Questions)
             .HasForeignKey(q => q.QuestionGroupId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AnswerOption>(b =>
        {
            b.HasIndex(o => new { o.QuestionId, o.OptionKey }).IsUnique();
        });

        modelBuilder.Entity<AttemptAnswer>(b =>
        {
            b.HasIndex(a => new { a.ExamAttemptId, a.QuestionId }).IsUnique();
            b.HasOne(a => a.Question)
             .WithMany()
             .HasForeignKey(a => a.QuestionId)
             .OnDelete(DeleteBehavior.Restrict);
            b.HasOne(a => a.ExamAttempt)
             .WithMany(ea => ea.Answers)
             .HasForeignKey(a => a.ExamAttemptId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ExamAttempt>(b =>
        {
            b.HasOne(ea => ea.Exam)
             .WithMany(e => e.Attempts)
             .HasForeignKey(ea => ea.ExamId)
             .OnDelete(DeleteBehavior.Cascade);
            b.HasIndex(ea => new { ea.SessionCode, ea.StudentCode });
            b.HasIndex(ea => ea.Status);
        });

        modelBuilder.Entity<Exam>(b =>
        {
            b.HasIndex(e => e.Code);
            b.HasIndex(e => e.IsPublished);
        });

        modelBuilder.Entity<ToeicAudio>(b =>
        {
            b.HasIndex(a => a.ExamId);
            b.HasIndex(a => a.FileName);
        });

        modelBuilder.Entity<ExamAllocation>(b =>
        {
            b.HasIndex(a => new { a.SessionCode, a.StudentCode });
            b.HasIndex(a => new { a.ExamId, a.SessionCode });
        });
    }
}

