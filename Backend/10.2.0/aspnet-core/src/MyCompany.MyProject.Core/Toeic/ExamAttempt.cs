using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities;

namespace MyCompany.MyProject.Toeic;

[Table("ToeicExamAttempts")]
public class ExamAttempt : Entity<Guid>
{
    public int ExamId { get; set; }

    [ForeignKey(nameof(ExamId))]
    public virtual Exam Exam { get; set; }

    [Required]
    [StringLength(128)]
    public string StudentName { get; set; }

    [StringLength(64)]
    public string StudentCode { get; set; }

    [StringLength(64)]
    public string SessionCode { get; set; }

    public AttemptStatus Status { get; set; } = AttemptStatus.InProgress;

    [StringLength(64)]
    public string ConcurrencyToken { get; set; }

    public int ElapsedTimeSeconds { get; set; } = 0;

    public DateTime StartTime { get; set; }

    public DateTime DeadlineTime { get; set; }

    public DateTime? SubmissionTime { get; set; }

    public bool IsSubmitted { get; set; } = false;

    public int CorrectCount { get; set; } = 0;

    public int WrongCount { get; set; } = 0;

    public int SkippedCount { get; set; } = 0;

    public int Part5Score { get; set; } = 0;

    public int Part6Score { get; set; } = 0;

    public int Part7Score { get; set; } = 0;

    public double AccuracyPercentage { get; set; } = 0;

    public virtual ICollection<AttemptAnswer> Answers { get; set; } = new List<AttemptAnswer>();
}
