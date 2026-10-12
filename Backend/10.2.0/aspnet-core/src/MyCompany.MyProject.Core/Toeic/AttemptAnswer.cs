using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities;

namespace MyCompany.MyProject.Toeic;

[Table("ToeicAttemptAnswers")]
public class AttemptAnswer : Entity<int>
{
    public Guid ExamAttemptId { get; set; }

    [ForeignKey(nameof(ExamAttemptId))]
    public virtual ExamAttempt ExamAttempt { get; set; }

    public int QuestionId { get; set; }

    [ForeignKey(nameof(QuestionId))]
    public virtual Question Question { get; set; }

    public int QuestionNumber { get; set; } // 101 - 200

    [StringLength(4)]
    public string SelectedOption { get; set; } // "A", "B", "C", "D" or null

    public bool IsFlagged { get; set; } = false;

    public bool? IsCorrect { get; set; }
}
