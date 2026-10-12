using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities.Auditing;

namespace MyCompany.MyProject.Toeic;

[Table("ToeicExams")]
public class Exam : FullAuditedEntity<int>
{
    [Required]
    [StringLength(256)]
    public string Title { get; set; }

    [Required]
    [StringLength(64)]
    public string Code { get; set; }

    [StringLength(2000)]
    public string Description { get; set; }

    public int DurationMinutes { get; set; } = 75;

    public int TotalQuestions { get; set; } = 100;

    public bool IsPublished { get; set; } = false;

    public ExamStatus Status { get; set; } = ExamStatus.Draft;

    public int AllocationCount { get; set; } = 0;

    [StringLength(32)]
    public string Difficulty { get; set; } = "Standard";

    [StringLength(64)]
    public string Category { get; set; } = "ETS";

    public virtual ICollection<QuestionGroup> QuestionGroups { get; set; } = new List<QuestionGroup>();

    public virtual ICollection<Question> Questions { get; set; } = new List<Question>();

    public virtual ICollection<ExamAttempt> Attempts { get; set; } = new List<ExamAttempt>();
}
