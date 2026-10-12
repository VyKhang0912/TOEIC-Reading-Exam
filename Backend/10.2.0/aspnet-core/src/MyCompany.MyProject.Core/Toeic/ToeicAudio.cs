using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace MyCompany.MyProject.Toeic;

[Table("ToeicAudios")]
public class ToeicAudio : Entity<int>, IHasCreationTime
{
    public int? ExamId { get; set; }

    [ForeignKey(nameof(ExamId))]
    public virtual Exam Exam { get; set; }

    [Required]
    [StringLength(256)]
    public string Title { get; set; }

    [Required]
    [StringLength(256)]
    public string FileName { get; set; }

    [Required]
    [StringLength(512)]
    public string FilePath { get; set; }

    public long FileSize { get; set; } = 0;

    public int DurationSeconds { get; set; } = 0;

    [Required]
    [StringLength(64)]
    public string ContentType { get; set; } = "audio/mpeg";

    public int? Part { get; set; }

    public bool IsPublic { get; set; } = true;

    public long ListenCount { get; set; } = 0;

    public DateTime CreationTime { get; set; } = DateTime.UtcNow;
}
