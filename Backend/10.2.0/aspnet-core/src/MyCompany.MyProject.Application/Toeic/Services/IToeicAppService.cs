using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Abp.Application.Services;
using MyCompany.MyProject.Toeic.Dto;

namespace MyCompany.MyProject.Toeic.Services;

public interface IToeicAppService : IApplicationService
{
    // Admin features
    Task<List<ExamListDto>> GetAllExamsAsync(string keyword = null);
    Task<PagedExamListDto> GetPagedExamsAsync(GetExamsInput input);
    Task<ExamDetailDto> GetExamDetailAsync(int examId);
    Task<DocxImportResultDto> ImportDocxAsync(Stream docxStream, string uploadsFolder);
    Task UpdateQuestionAsync(UpdateQuestionInput input);
    Task PublishExamAsync(int examId, bool isPublished);
    Task DeleteExamAsync(int examId);
    Task<byte[]> GenerateStandardDocxTemplateAsync(string examTitle, string examCode);

    // Mobile / Exam taking features
    Task<List<ExamListDto>> GetPublishedExamsAsync();
    Task<AttemptSessionDto> StartAttemptAsync(StartAttemptInput input);
    Task<AttemptSessionDto> StartSmartSessionAsync(StartSmartSessionInput input);
    Task<AttemptSessionDto> GetActiveSessionAsync(string studentCode);
    Task<List<AttemptQuestionDto>> GetAttemptQuestionsAsync(Guid attemptId);
    Task SaveAnswerAsync(SaveAnswerInput input);
    Task<ExamResultDto> SubmitAttemptAsync(SubmitAttemptInput input);
    Task<ExamResultDto> GetAttemptResultAsync(Guid attemptId);

    // High-concurrency Audio distribution
    Task<List<AudioTrackDto>> GetAudioTracksAsync(int? examId);
    Task<ToeicAudio> GetAudioByIdAsync(int id);
    Task<ToeicAudio> GetAudioByFileNameAsync(string fileName);
    Task RegisterAudioAsync(string title, string fileName, string filePath, long fileSize, int durationSeconds, int? examId, int? part);
}
