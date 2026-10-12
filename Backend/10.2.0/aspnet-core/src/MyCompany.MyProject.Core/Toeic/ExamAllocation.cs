using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities;

namespace MyCompany.MyProject.Toeic;

[Table("ToeicExamAllocations")]
public class ExamAllocation : Entity<int>
{
    public int ExamId { get; set; }

    [ForeignKey(nameof(ExamId))]
    public virtual Exam Exam { get; set; }

    [Required]
    [StringLength(64)]
    public string SessionCode { get; set; }

    [Required]
    [StringLength(64)]
    public string StudentCode { get; set; }

    public Guid AttemptId { get; set; }

    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
}
