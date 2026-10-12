using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities;

namespace MyCompany.MyProject.Toeic;

[Table("ToeicAnswerOptions")]
public class AnswerOption : Entity<int>
{
    public int QuestionId { get; set; }

    [ForeignKey(nameof(QuestionId))]
    public virtual Question Question { get; set; }

    [Required]
    [StringLength(4)]
    public string OptionKey { get; set; } // "A", "B", "C", "D"

    [Required]
    public string Content { get; set; }

    public bool IsCorrect { get; set; }
}
