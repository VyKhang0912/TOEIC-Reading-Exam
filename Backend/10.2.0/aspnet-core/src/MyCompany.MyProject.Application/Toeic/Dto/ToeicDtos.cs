using System;
using System.Collections.Generic;

namespace MyCompany.MyProject.Toeic.Dto;

public class DocxImportResultDto
{
    public bool Success { get; set; }
    public int? ExamId { get; set; }
    public string ExamTitle { get; set; }
    public string ExamCode { get; set; }
    public int TotalParsedQuestions { get; set; }
    public int Part5Count { get; set; }
    public int Part6Count { get; set; }
    public int Part7Count { get; set; }
    public List<string> ValidationErrors { get; set; } = new List<string>();
    public List<string> Warnings { get; set; } = new List<string>();
}

public class ExamListDto
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Code { get; set; }
    public string Description { get; set; }
    public int DurationMinutes { get; set; }
    public int TotalQuestions { get; set; }
    public bool IsPublished { get; set; }
    public ExamStatus Status { get; set; }
    public DateTime CreationTime { get; set; }
    public int AttemptCount { get; set; }
}

public class GetExamsInput
{
    public string Keyword { get; set; }
    public bool? IsPublished { get; set; }
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class PagedExamListDto
{
    public List<ExamListDto> Items { get; set; } = new List<ExamListDto>();
    public int TotalCount { get; set; }
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / Math.Max(PageSize, 1));
    public int PublishedCount { get; set; }
    public int DraftCount { get; set; }
    public int TotalQuestionsAllExams { get; set; }
    public int TotalAttemptsAllExams { get; set; }
    public string Keyword { get; set; }
    public bool? FilterIsPublished { get; set; }
}


public class ExamDetailDto
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Code { get; set; }
    public string Description { get; set; }
    public int DurationMinutes { get; set; }
    public int TotalQuestions { get; set; }
    public bool IsPublished { get; set; }
    public ExamStatus Status { get; set; }
    public List<QuestionGroupDto> QuestionGroups { get; set; } = new List<QuestionGroupDto>();
    public List<QuestionDto> Questions { get; set; } = new List<QuestionDto>();
}

public class QuestionGroupDto
{
    public int Id { get; set; }
    public int ExamId { get; set; }
    public ToeicPart Part { get; set; }
    public int GroupOrder { get; set; }
    public string Title { get; set; }
    public string PassageType { get; set; }
    public string PassageText { get; set; }
    public string ImageUrls { get; set; }
    public List<QuestionDto> Questions { get; set; } = new List<QuestionDto>();
}

public class QuestionDto
{
    public int Id { get; set; }
    public int ExamId { get; set; }
    public int? QuestionGroupId { get; set; }
    public int QuestionNumber { get; set; }
    public ToeicPart Part { get; set; }
    public string Stem { get; set; }
    public string Explanation { get; set; }
    public string CorrectOption { get; set; }
    public List<AnswerOptionDto> Options { get; set; } = new List<AnswerOptionDto>();
}

public class AnswerOptionDto
{
    public int Id { get; set; }
    public int QuestionId { get; set; }
    public string OptionKey { get; set; }
    public string Content { get; set; }
    public bool IsCorrect { get; set; }
}

public class UpdateQuestionInput
{
    public int Id { get; set; }
    public string Stem { get; set; }
    public string Explanation { get; set; }
    public string CorrectOption { get; set; }
    public List<UpdateOptionInput> Options { get; set; } = new List<UpdateOptionInput>();
}

public class UpdateOptionInput
{
    public int Id { get; set; }
    public string OptionKey { get; set; }
    public string Content { get; set; }
}

public class StartAttemptInput
{
    public int ExamId { get; set; }
    public string StudentName { get; set; }
    public string StudentCode { get; set; }
}

public class AttemptSessionDto
{
    public Guid AttemptId { get; set; }
    public int ExamId { get; set; }
    public string ExamTitle { get; set; }
    public string StudentName { get; set; }
    public string StudentCode { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime DeadlineTime { get; set; }
    public int RemainingSeconds { get; set; }
}

public class SaveAnswerInput
{
    public Guid AttemptId { get; set; }
    public int QuestionId { get; set; }
    public string SelectedOption { get; set; } // "A", "B", "C", "D" or null
    public bool IsFlagged { get; set; }
}

public class SubmitAttemptInput
{
    public Guid AttemptId { get; set; }
}

public class AttemptQuestionDto
{
    public int QuestionId { get; set; }
    public int QuestionNumber { get; set; }
    public ToeicPart Part { get; set; }
    public string Stem { get; set; }
    public int? QuestionGroupId { get; set; }
    public string GroupTitle { get; set; }
    public string PassageType { get; set; }
    public string PassageText { get; set; }
    public string ImageUrls { get; set; }
    public string SelectedOption { get; set; }
    public bool IsFlagged { get; set; }
    public List<AttemptOptionDto> Options { get; set; } = new List<AttemptOptionDto>();
}

public class AttemptOptionDto
{
    public int Id { get; set; }
    public string OptionKey { get; set; }
    public string Content { get; set; }
}

public class ExamResultDto
{
    public Guid AttemptId { get; set; }
    public int ExamId { get; set; }
    public string ExamTitle { get; set; }
    public string StudentName { get; set; }
    public string StudentCode { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? SubmissionTime { get; set; }
    public int TimeSpentSeconds { get; set; }
    public int CorrectCount { get; set; }
    public int WrongCount { get; set; }
    public int SkippedCount { get; set; }
    public int TotalQuestions { get; set; }
    public double AccuracyPercentage { get; set; }
    public int Part5Correct { get; set; }
    public int Part5Total { get; set; }
    public int Part6Correct { get; set; }
    public int Part6Total { get; set; }
    public int Part7Correct { get; set; }
    public int Part7Total { get; set; }
    public List<QuestionReviewDto> QuestionReviews { get; set; } = new List<QuestionReviewDto>();
}

public class QuestionReviewDto
{
    public int QuestionId { get; set; }
    public int QuestionNumber { get; set; }
    public ToeicPart Part { get; set; }
    public string Stem { get; set; }
    public string PassageText { get; set; }
    public string PassageTitle { get; set; }
    public string SelectedOption { get; set; }
    public string CorrectOption { get; set; }
    public bool IsCorrect { get; set; }
    public bool IsSkipped { get; set; }
    public string Explanation { get; set; }
    public List<AnswerOptionDto> Options { get; set; } = new List<AnswerOptionDto>();
}

public class StartSmartSessionInput
{
    public string StudentCode { get; set; }
    public string StudentName { get; set; }
    public string SessionCode { get; set; } = "SESSION-DEFAULT";
    public int? ExamId { get; set; }
    public string Difficulty { get; set; }
    public string Category { get; set; }
}

public class AudioTrackDto
{
    public int Id { get; set; }
    public int? ExamId { get; set; }
    public string Title { get; set; }
    public string FileName { get; set; }
    public string StreamUrl { get; set; }
    public int DurationSeconds { get; set; }
    public long FileSize { get; set; }
    public int? Part { get; set; }
    public bool IsPublic { get; set; }
}
