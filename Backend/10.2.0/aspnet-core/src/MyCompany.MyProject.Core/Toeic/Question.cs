using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities;

namespace MyCompany.MyProject.Toeic;

[Table("ToeicQuestions")]
public class Question : Entity<int>
{
    public int ExamId { get; set; }

    [ForeignKey(nameof(ExamId))]
    public virtual Exam Exam { get; set; }

    public int? QuestionGroupId { get; set; }

    [ForeignKey(nameof(QuestionGroupId))]
    public virtual QuestionGroup QuestionGroup { get; set; }

    public int QuestionNumber { get; set; } // 101 - 200

    public ToeicPart Part { get; set; }

    [Required]
    public string Stem { get; set; }

    public string Explanation { get; set; }

    [Required]
    [StringLength(4)]
    public string CorrectOption { get; set; } // "A", "B", "C", "D"

    public virtual ICollection<AnswerOption> Options { get; set; } = new List<AnswerOption>();
}
