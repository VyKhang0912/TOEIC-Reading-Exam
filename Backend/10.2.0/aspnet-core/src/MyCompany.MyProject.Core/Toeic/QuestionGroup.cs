using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities;

namespace MyCompany.MyProject.Toeic;

[Table("ToeicQuestionGroups")]
public class QuestionGroup : Entity<int>
{
    public int ExamId { get; set; }

    [ForeignKey(nameof(ExamId))]
    public virtual Exam Exam { get; set; }

    public ToeicPart Part { get; set; }

    public int GroupOrder { get; set; }

    [StringLength(512)]
    public string Title { get; set; }

    [StringLength(64)]
    public string PassageType { get; set; }

    public string PassageText { get; set; }

    public string ImageUrls { get; set; }

    public virtual ICollection<Question> Questions { get; set; } = new List<Question>();
}
