using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.UI;
using Microsoft.EntityFrameworkCore;
using MyCompany.MyProject.Toeic.Dto;

namespace MyCompany.MyProject.Toeic.Services;

public class ToeicAppService : MyProjectAppServiceBase, IToeicAppService
{
    private readonly IRepository<Exam, int> _examRepository;
    private readonly IRepository<QuestionGroup, int> _groupRepository;
    private readonly IRepository<Question, int> _questionRepository;
    private readonly IRepository<AnswerOption, int> _optionRepository;
    private readonly IRepository<ExamAttempt, Guid> _attemptRepository;
    private readonly IRepository<AttemptAnswer, int> _attemptAnswerRepository;
    private readonly IRepository<ToeicAudio, int> _audioRepository;
    private readonly IRepository<ExamAllocation, int> _allocationRepository;
    private readonly DocxParserService _docxParserService;

    // High concurrency in-memory cache and locks
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<int, List<Question>> _examQuestionsCache = new();
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<Guid, System.Threading.SemaphoreSlim> _attemptLocks = new();
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, System.Threading.SemaphoreSlim> _sessionLocks = new();

    public ToeicAppService(
        IRepository<Exam, int> examRepository,
        IRepository<QuestionGroup, int> groupRepository,
        IRepository<Question, int> questionRepository,
        IRepository<AnswerOption, int> optionRepository,
        IRepository<ExamAttempt, Guid> attemptRepository,
        IRepository<AttemptAnswer, int> attemptAnswerRepository,
        IRepository<ToeicAudio, int> audioRepository,
        IRepository<ExamAllocation, int> allocationRepository)
    {
        _examRepository = examRepository;
        _groupRepository = groupRepository;
        _questionRepository = questionRepository;
        _optionRepository = optionRepository;
        _attemptRepository = attemptRepository;
        _attemptAnswerRepository = attemptAnswerRepository;
        _audioRepository = audioRepository;
        _allocationRepository = allocationRepository;
        _docxParserService = new DocxParserService();
    }

    #region Admin Services

    public async Task<List<ExamListDto>> GetAllExamsAsync(string keyword = null)
    {
        var query = _examRepository.GetAll();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim().ToLower();
            query = query.Where(e => e.Title.ToLower().Contains(kw) || e.Code.ToLower().Contains(kw));
        }

        var exams = await query
            .OrderByDescending(e => e.CreationTime)
            .Select(e => new ExamListDto
            {
                Id = e.Id,
                Title = e.Title,
                Code = e.Code,
                Description = e.Description,
                DurationMinutes = e.DurationMinutes,
                TotalQuestions = e.TotalQuestions,
                IsPublished = e.IsPublished,
                Status = e.Status,
                CreationTime = e.CreationTime,
                AttemptCount = _attemptRepository.GetAll().Count(a => a.ExamId == e.Id)
            })
            .ToListAsync();

        return exams;
    }

    public async Task<PagedExamListDto> GetPagedExamsAsync(GetExamsInput input)
    {
        input ??= new GetExamsInput();
        var pageIndex = Math.Max(1, input.PageIndex);
        var pageSize = Math.Clamp(input.PageSize, 5, 100);

        var query = _examRepository.GetAll();

        if (!string.IsNullOrWhiteSpace(input.Keyword))
        {
            var kw = input.Keyword.Trim().ToLower();
            query = query.Where(e => e.Title.ToLower().Contains(kw) || e.Code.ToLower().Contains(kw));
        }

        if (input.IsPublished.HasValue)
        {
            query = query.Where(e => e.IsPublished == input.IsPublished.Value);
        }

        var totalCount = await query.CountAsync();

        var allExams = _examRepository.GetAll();
        var publishedCount = await allExams.CountAsync(e => e.IsPublished);
        var draftCount = await allExams.CountAsync(e => !e.IsPublished);
        var totalQuestionsAll = await allExams.SumAsync(e => (int?)e.TotalQuestions) ?? 0;
        var totalAttemptsAll = await _attemptRepository.GetAll().CountAsync();

        var items = await query
            .OrderByDescending(e => e.CreationTime)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new ExamListDto
            {
                Id = e.Id,
                Title = e.Title,
                Code = e.Code,
                Description = e.Description,
                DurationMinutes = e.DurationMinutes,
                TotalQuestions = e.TotalQuestions,
                IsPublished = e.IsPublished,
                Status = e.Status,
                CreationTime = e.CreationTime,
                AttemptCount = _attemptRepository.GetAll().Count(a => a.ExamId == e.Id)
            })
            .ToListAsync();

        return new PagedExamListDto
        {
            Items = items,
            TotalCount = totalCount,
            PageIndex = pageIndex,
            PageSize = pageSize,
            PublishedCount = publishedCount,
            DraftCount = draftCount,
            TotalQuestionsAllExams = totalQuestionsAll,
            TotalAttemptsAllExams = totalAttemptsAll,
            Keyword = input.Keyword,
            FilterIsPublished = input.IsPublished
        };
    }


    public async Task<ExamDetailDto> GetExamDetailAsync(int examId)
    {
        var exam = await _examRepository.GetAll()
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == examId);

        if (exam == null)
            throw new UserFriendlyException($"Đề thi #{examId} không tồn tại.");

        var groups = await _groupRepository.GetAll()
            .AsNoTracking()
            .Where(g => g.ExamId == examId)
            .OrderBy(g => g.Part)
            .ThenBy(g => g.GroupOrder)
            .ToListAsync();

        var questions = await _questionRepository.GetAll()
            .AsNoTracking()
            .Where(q => q.ExamId == examId)
            .Include(q => q.Options)
            .AsSplitQuery()
            .OrderBy(q => q.QuestionNumber)
            .ToListAsync();

        var examDto = new ExamDetailDto
        {
            Id = exam.Id,
            Title = exam.Title,
            Code = exam.Code,
            Description = exam.Description,
            DurationMinutes = exam.DurationMinutes,
            TotalQuestions = exam.TotalQuestions,
            IsPublished = exam.IsPublished,
            Status = exam.Status,
            QuestionGroups = groups.Select(g => new QuestionGroupDto
            {
                Id = g.Id,
                ExamId = g.ExamId,
                Part = g.Part,
                GroupOrder = g.GroupOrder,
                Title = g.Title,
                PassageType = g.PassageType,
                PassageText = g.PassageText,
                ImageUrls = g.ImageUrls,
                Questions = questions.Where(q => q.QuestionGroupId == g.Id).Select(q => new QuestionDto
                {
                    Id = q.Id,
                    ExamId = q.ExamId,
                    QuestionGroupId = q.QuestionGroupId,
                    QuestionNumber = q.QuestionNumber,
                    Part = q.Part,
                    Stem = q.Stem,
                    Explanation = q.Explanation,
                    CorrectOption = q.CorrectOption,
                    Options = q.Options.OrderBy(o => o.OptionKey).Select(o => new AnswerOptionDto
                    {
                        Id = o.Id,
                        QuestionId = o.QuestionId,
                        OptionKey = o.OptionKey,
                        Content = o.Content,
                        IsCorrect = o.IsCorrect
                    }).ToList()
                }).ToList()
            }).ToList(),
            Questions = questions.Select(q => new QuestionDto
            {
                Id = q.Id,
                ExamId = q.ExamId,
                QuestionGroupId = q.QuestionGroupId,
                QuestionNumber = q.QuestionNumber,
                Part = q.Part,
                Stem = q.Stem,
                Explanation = q.Explanation,
                CorrectOption = q.CorrectOption,
                Options = q.Options.OrderBy(o => o.OptionKey).Select(o => new AnswerOptionDto
                {
                    Id = o.Id,
                    QuestionId = o.QuestionId,
                    OptionKey = o.OptionKey,
                    Content = o.Content,
                    IsCorrect = o.IsCorrect
                }).ToList()
            }).ToList()
        };

        return examDto;
    }

    [UnitOfWork]
    public async Task<DocxImportResultDto> ImportDocxAsync(Stream docxStream, string uploadsFolder)
    {
        var result = new DocxImportResultDto();

        var parseOutcome = _docxParserService.ParseDocx(docxStream, uploadsFolder);
        if (!parseOutcome.Success)
        {
            result.Success = false;
            result.ValidationErrors = parseOutcome.Errors;
            return result;
        }

        var parsed = parseOutcome.Model;
        result.ExamTitle = parsed.Title;
        result.TotalParsedQuestions = parsed.Questions.Count;
        result.Part5Count = parsed.Questions.Count(q => q.Part == ToeicPart.Part5);
        result.Part6Count = parsed.Questions.Count(q => q.Part == ToeicPart.Part6);
        result.Part7Count = parsed.Questions.Count(q => q.Part == ToeicPart.Part7);

        // Check for duplicate code
        var existingCodeCount = await _examRepository.CountAsync(e => e.Code == parsed.Code);
        if (existingCodeCount > 0)
        {
            parsed.Code += "-" + DateTime.UtcNow.ToString("mmss");
        }
        result.ExamCode = parsed.Code;

        // Insert Exam
        var exam = new Exam
        {
            Title = parsed.Title,
            Code = parsed.Code,
            Description = parsed.Description,
            DurationMinutes = parsed.DurationMinutes,
            TotalQuestions = parsed.TotalQuestions,
            IsPublished = false,
            Status = ExamStatus.Draft
        };


        exam.Id = await _examRepository.InsertAndGetIdAsync(exam);
        await CurrentUnitOfWork.SaveChangesAsync();

        // Map groups
        var groupEntityMap = new Dictionary<ParsedGroupModel, QuestionGroup>();
        foreach (var g in parsed.Groups)
        {
            var groupEntity = new QuestionGroup
            {
                ExamId = exam.Id,
                Part = g.Part,
                GroupOrder = g.Order,
                Title = g.Title,
                PassageType = g.PassageType,
                PassageText = g.PassageText,
                ImageUrls = g.ImageUrls
            };
            groupEntity.Id = await _groupRepository.InsertAndGetIdAsync(groupEntity);
            groupEntityMap[g] = groupEntity;
        }

        await CurrentUnitOfWork.SaveChangesAsync();

        // Map questions and options
        foreach (var q in parsed.Questions)
        {
            int? groupId = null;
            if (q.Group != null && groupEntityMap.TryGetValue(q.Group, out var grp))
            {
                groupId = grp.Id;
            }

            var questionEntity = new Question
            {
                ExamId = exam.Id,
                QuestionGroupId = groupId,
                QuestionNumber = q.QuestionNumber,
                Part = q.Part,
                Stem = q.Stem,
                Explanation = q.Explanation,
                CorrectOption = q.CorrectOption
            };

            questionEntity.Id = await _questionRepository.InsertAndGetIdAsync(questionEntity);

            foreach (var opt in q.Options)
            {
                var optionEntity = new AnswerOption
                {
                    QuestionId = questionEntity.Id,
                    OptionKey = opt.Key,
                    Content = opt.Value,
                    IsCorrect = (opt.Key == q.CorrectOption)
                };
                await _optionRepository.InsertAsync(optionEntity);
            }
        }

        await CurrentUnitOfWork.SaveChangesAsync();

        result.Success = true;
        result.ExamId = exam.Id;
        return result;
    }

    public async Task UpdateQuestionAsync(UpdateQuestionInput input)
    {
        var question = await _questionRepository.GetAll()
            .Include(q => q.Options)
            .FirstOrDefaultAsync(q => q.Id == input.Id);

        if (question == null)
            throw new UserFriendlyException($"Câu hỏi #{input.Id} không tồn tại.");

        question.Stem = input.Stem;
        question.Explanation = input.Explanation;
        question.CorrectOption = input.CorrectOption;

        foreach (var optInput in input.Options)
        {
            var option = question.Options.FirstOrDefault(o => o.OptionKey == optInput.OptionKey);
            if (option != null)
            {
                option.Content = optInput.Content;
                option.IsCorrect = (option.OptionKey == input.CorrectOption);
            }
        }

        await _questionRepository.UpdateAsync(question);
    }

    public async Task PublishExamAsync(int examId, bool isPublished)
    {
        var exam = await _examRepository.GetAsync(examId);
        exam.IsPublished = isPublished;
        exam.Status = isPublished ? ExamStatus.Published : ExamStatus.Draft;
        await _examRepository.UpdateAsync(exam);
    }

    [UnitOfWork]
    public async Task DeleteExamAsync(int examId)
    {
        // Delete attempts & attempt answers
        var attempts = await _attemptRepository.GetAllListAsync(a => a.ExamId == examId);
        foreach (var att in attempts)
        {
            await _attemptAnswerRepository.DeleteAsync(aa => aa.ExamAttemptId == att.Id);
            await _attemptRepository.DeleteAsync(att);
        }

        // Delete questions & options
        var questions = await _questionRepository.GetAllListAsync(q => q.ExamId == examId);
        foreach (var q in questions)
        {
            await _optionRepository.DeleteAsync(o => o.QuestionId == q.Id);
            await _questionRepository.DeleteAsync(q);
        }

        // Delete groups
        await _groupRepository.DeleteAsync(g => g.ExamId == examId);

        // Delete exam
        await _examRepository.DeleteAsync(examId);
    }

    public Task<byte[]> GenerateStandardDocxTemplateAsync(string examTitle, string examCode)
    {
        var bytes = _docxParserService.GenerateStandardDocx(examTitle, examCode);
        return Task.FromResult(bytes);
    }

    #endregion

    #region Student / Mobile Exam Services

    public async Task<List<ExamListDto>> GetPublishedExamsAsync()
    {
        return await _examRepository.GetAll()
            .Where(e => e.IsPublished)
            .OrderByDescending(e => e.CreationTime)
            .Select(e => new ExamListDto
            {
                Id = e.Id,
                Title = e.Title,
                Code = e.Code,
                Description = e.Description,
                DurationMinutes = e.DurationMinutes,
                TotalQuestions = e.TotalQuestions,
                IsPublished = e.IsPublished,
                Status = e.Status,
                CreationTime = e.CreationTime,
                AttemptCount = _attemptRepository.GetAll().Count(a => a.ExamId == e.Id)
            })
            .ToListAsync();
    }

    [UnitOfWork]
    public async Task<AttemptSessionDto> StartAttemptAsync(StartAttemptInput input)
    {
        var exam = await _examRepository.FirstOrDefaultAsync(e => e.Id == input.ExamId);
        if (exam == null)
            throw new UserFriendlyException("Đề thi không tồn tại.");

        if (!exam.IsPublished)
            throw new UserFriendlyException("Đề thi này hiện đang là bản nháp hoặc đã bị thu hồi xuất bản.");

        var attempt = new ExamAttempt
        {
            Id = Guid.NewGuid(),
            ExamId = exam.Id,
            StudentName = string.IsNullOrWhiteSpace(input.StudentName) ? "Học viên TOEIC" : input.StudentName.Trim(),
            StudentCode = string.IsNullOrWhiteSpace(input.StudentCode) ? "HV-" + DateTime.UtcNow.ToString("mmss") : input.StudentCode.Trim(),
            StartTime = DateTime.UtcNow,
            DeadlineTime = DateTime.UtcNow.AddMinutes(exam.DurationMinutes),
            IsSubmitted = false
        };

        await _attemptRepository.InsertAsync(attempt);
        await CurrentUnitOfWork.SaveChangesAsync();

        // Pre-populate empty answers for all questions
        var questionIds = await _questionRepository.GetAll()
            .Where(q => q.ExamId == exam.Id)
            .Select(q => q.Id)
            .ToListAsync();

        foreach (var qId in questionIds)
        {
            var ans = new AttemptAnswer
            {
                ExamAttemptId = attempt.Id,
                QuestionId = qId,
                SelectedOption = null,
                IsFlagged = false
            };
            await _attemptAnswerRepository.InsertAsync(ans);
        }

        await CurrentUnitOfWork.SaveChangesAsync();

        return new AttemptSessionDto
        {
            AttemptId = attempt.Id,
            ExamId = exam.Id,
            ExamTitle = exam.Title,
            StudentName = attempt.StudentName,
            StudentCode = attempt.StudentCode,
            StartTime = attempt.StartTime,
            DeadlineTime = attempt.DeadlineTime,
            RemainingSeconds = (int)(attempt.DeadlineTime - DateTime.UtcNow).TotalSeconds
        };
    }

    public async Task<List<AttemptQuestionDto>> GetAttemptQuestionsAsync(Guid attemptId)
    {
        var attempt = await _attemptRepository.FirstOrDefaultAsync(a => a.Id == attemptId);
        if (attempt == null)
            throw new UserFriendlyException("Lượt thi không tồn tại.");

        var questions = await _questionRepository.GetAll()
            .AsNoTracking()
            .Where(q => q.ExamId == attempt.ExamId)
            .Include(q => q.Options)
            .Include(q => q.QuestionGroup)
            .AsSplitQuery()
            .OrderBy(q => q.QuestionNumber)
            .ToListAsync();

        var studentAnswers = await _attemptAnswerRepository.GetAll()
            .Where(aa => aa.ExamAttemptId == attemptId)
            .ToDictionaryAsync(aa => aa.QuestionId, aa => aa);

        var list = questions.Select(q =>
        {
            studentAnswers.TryGetValue(q.Id, out var ans);
            return new AttemptQuestionDto
            {
                QuestionId = q.Id,
                QuestionNumber = q.QuestionNumber,
                Part = q.Part,
                Stem = q.Stem,
                QuestionGroupId = q.QuestionGroupId,
                GroupTitle = q.QuestionGroup?.Title,
                PassageType = q.QuestionGroup?.PassageType,
                PassageText = q.QuestionGroup?.PassageText,
                ImageUrls = q.QuestionGroup?.ImageUrls,
                SelectedOption = ans?.SelectedOption,
                IsFlagged = ans?.IsFlagged ?? false,
                Options = q.Options.OrderBy(o => o.OptionKey).Select(o => new AttemptOptionDto
                {
                    Id = o.Id,
                    OptionKey = o.OptionKey,
                    Content = o.Content
                }).ToList()
                // NOTE: DO NOT expose CorrectOption or IsCorrect before submission!
            };
        }).ToList();

        return list;
    }

    public async Task SaveAnswerAsync(SaveAnswerInput input)
    {
        var attempt = await _attemptRepository.FirstOrDefaultAsync(a => a.Id == input.AttemptId);
        if (attempt == null)
            throw new UserFriendlyException("Lượt thi không tồn tại.");

        if (attempt.IsSubmitted)
            throw new UserFriendlyException("Bài thi đã được nộp hoặc kết thúc.");

        // Grace period of 60 seconds
        if (DateTime.UtcNow > attempt.DeadlineTime.AddSeconds(60))
        {
            attempt.IsSubmitted = true;
            await _attemptRepository.UpdateAsync(attempt);
            throw new UserFriendlyException("Đã hết thời gian làm bài.");
        }

        var ans = await _attemptAnswerRepository.FirstOrDefaultAsync(
            a => a.ExamAttemptId == input.AttemptId && a.QuestionId == input.QuestionId);

        if (ans == null)
        {
            ans = new AttemptAnswer
            {
                ExamAttemptId = input.AttemptId,
                QuestionId = input.QuestionId,
                SelectedOption = input.SelectedOption,
                IsFlagged = input.IsFlagged
            };
            await _attemptAnswerRepository.InsertAsync(ans);
        }
        else
        {
            ans.SelectedOption = input.SelectedOption;
            ans.IsFlagged = input.IsFlagged;
            await _attemptAnswerRepository.UpdateAsync(ans);
        }
    }

    [UnitOfWork]
    public async Task<AttemptSessionDto> StartSmartSessionAsync(StartSmartSessionInput input)
    {
        var studentCode = string.IsNullOrWhiteSpace(input?.StudentCode) ? "HV-" + DateTime.UtcNow.ToString("mmss") : input.StudentCode.Trim();
        var studentName = string.IsNullOrWhiteSpace(input?.StudentName) ? "Học viên TOEIC" : input.StudentName.Trim();
        var sessionCode = string.IsNullOrWhiteSpace(input?.SessionCode) ? "SESSION-DEFAULT" : input.SessionCode.Trim();

        var now = DateTime.UtcNow;

        // 1. RELOAD PROTECTION (TEST F):
        // Nếu học viên đang có một lượt thi dở dang trong phiên này và chưa hết giờ: tiếp tục đúng lượt thi cũ!
        var existingAttempt = await _attemptRepository.GetAll()
            .Include(a => a.Exam)
            .Where(a => a.StudentCode == studentCode
                     && a.SessionCode == sessionCode
                     && (a.Status == AttemptStatus.InProgress || !a.IsSubmitted)
                     && a.DeadlineTime > now)
            .OrderByDescending(a => a.StartTime)
            .FirstOrDefaultAsync();

        if (existingAttempt != null)
        {
            return new AttemptSessionDto
            {
                AttemptId = existingAttempt.Id,
                ExamId = existingAttempt.ExamId,
                ExamTitle = existingAttempt.Exam?.Title ?? "Đề thi TOEIC",
                StudentName = existingAttempt.StudentName,
                StudentCode = existingAttempt.StudentCode,
                StartTime = existingAttempt.StartTime,
                DeadlineTime = existingAttempt.DeadlineTime,
                RemainingSeconds = Math.Max(0, (int)(existingAttempt.DeadlineTime - now).TotalSeconds)
            };
        }

        // 2. LỌC ĐỀ THI HỢP LỆ (IsPublished, 100 câu: P5=30, P6=16, P7=54, có đáp án đúng)
        var allExams = await _examRepository.GetAll()
            .Where(e => e.IsPublished && e.TotalQuestions == 100)
            .Include(e => e.Questions)
            .ToListAsync();

        var eligibleExams = allExams.Where(e =>
        {
            var p5 = e.Questions.Count(q => q.Part == ToeicPart.Part5);
            var p6 = e.Questions.Count(q => q.Part == ToeicPart.Part6);
            var p7 = e.Questions.Count(q => q.Part == ToeicPart.Part7);
            var hasCorrect = e.Questions.All(q => !string.IsNullOrWhiteSpace(q.CorrectOption));
            return p5 == 30 && p6 == 16 && p7 == 54 && hasCorrect;
        }).ToList();

        if (!eligibleExams.Any())
        {
            eligibleExams = allExams.Any() ? allExams : await _examRepository.GetAllListAsync(e => e.IsPublished);
        }

        if (!eligibleExams.Any())
        {
            throw new UserFriendlyException("Không tìm thấy đề thi phù hợp trong hệ thống.");
        }

        Exam chosenExam = null;

        // Nếu học viên yêu cầu cụ thể 1 ExamId:
        if (input != null && input.ExamId.HasValue && input.ExamId.Value > 0)
        {
            chosenExam = eligibleExams.FirstOrDefault(e => e.Id == input.ExamId.Value);
        }

        var attemptId = Guid.NewGuid();
        var sessionLock = _sessionLocks.GetOrAdd(sessionCode, _ => new System.Threading.SemaphoreSlim(1, 1));
        await sessionLock.WaitAsync();

        try
        {
            if (chosenExam == null)
            {
                // THUẬT TOÁN: LEAST-ASSIGNED + RANDOM TIE BREAK + ATOMIC ALLOCATION
                // A. Ưu tiên đề học viên chưa từng làm
                var takenExamIds = await _attemptRepository.GetAll()
                    .Where(a => a.StudentCode == studentCode)
                    .Select(a => a.ExamId)
                    .Distinct()
                    .ToListAsync();

                var unTakenExams = eligibleExams.Where(e => !takenExamIds.Contains(e.Id)).ToList();
                var candidatePool = unTakenExams.Any() ? unTakenExams : eligibleExams;

                // B. Đếm số lượt cấp trong phiên thi này (SessionCode) từ ToeicExamAllocations
                var sessionAllocations = await _allocationRepository.GetAll()
                    .Where(a => a.SessionCode == sessionCode)
                    .GroupBy(a => a.ExamId)
                    .Select(g => new { ExamId = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.ExamId, x => x.Count);

                var poolWithCount = candidatePool.Select(e => new
                {
                    Exam = e,
                    Count = sessionAllocations.TryGetValue(e.Id, out var c) ? c : 0
                }).ToList();

                int minCount = poolWithCount.Min(x => x.Count);
                var leastAssignedGroup = poolWithCount.Where(x => x.Count == minCount).Select(x => x.Exam).ToList();

                // C. Random Tie Break (Chống trùng lặp khi nhiều học viên bấm cùng lúc)
                int randomIndex = Random.Shared.Next(0, leastAssignedGroup.Count);
                chosenExam = leastAssignedGroup[randomIndex];
            }

            // Ghi nhận vào bảng phân phối đề ngay trong lock
            var allocation = new ExamAllocation
            {
                ExamId = chosenExam.Id,
                SessionCode = sessionCode,
                StudentCode = studentCode,
                AttemptId = attemptId,
                AssignedAt = now
            };
            await _allocationRepository.InsertAsync(allocation);

            // Tăng đếm tổng số lượt phân phối
            chosenExam.AllocationCount++;
            await _examRepository.UpdateAsync(chosenExam);

            await CurrentUnitOfWork.SaveChangesAsync();
        }
        finally
        {
            sessionLock.Release();
        }

        // Tạo bản ghi ExamAttempt
        var deadline = now.AddMinutes(chosenExam.DurationMinutes);
        var attempt = new ExamAttempt
        {
            Id = attemptId,
            ExamId = chosenExam.Id,
            StudentName = studentName,
            StudentCode = studentCode,
            SessionCode = sessionCode,
            Status = AttemptStatus.InProgress,
            ConcurrencyToken = Guid.NewGuid().ToString("N"),
            StartTime = now,
            DeadlineTime = deadline,
            IsSubmitted = false
        };
        await _attemptRepository.InsertAsync(attempt);
        await CurrentUnitOfWork.SaveChangesAsync();

        // Khởi tạo trước các câu trả lời rỗng
        var questionIds = await _questionRepository.GetAll()
            .Where(q => q.ExamId == chosenExam.Id)
            .Select(q => q.Id)
            .ToListAsync();

        foreach (var qId in questionIds)
        {
            await _attemptAnswerRepository.InsertAsync(new AttemptAnswer
            {
                ExamAttemptId = attemptId,
                QuestionId = qId,
                SelectedOption = null,
                IsFlagged = false
            });
        }

        await CurrentUnitOfWork.SaveChangesAsync();

        return new AttemptSessionDto
        {
            AttemptId = attempt.Id,
            ExamId = chosenExam.Id,
            ExamTitle = chosenExam.Title,
            StudentName = attempt.StudentName,
            StudentCode = attempt.StudentCode,
            StartTime = attempt.StartTime,
            DeadlineTime = attempt.DeadlineTime,
            RemainingSeconds = (int)(attempt.DeadlineTime - now).TotalSeconds
        };
    }

    public async Task<AttemptSessionDto> GetActiveSessionAsync(string studentCode)
    {
        if (string.IsNullOrWhiteSpace(studentCode))
            return null;

        var now = DateTime.UtcNow;
        var attempt = await _attemptRepository.GetAll()
            .Include(a => a.Exam)
            .Where(a => a.StudentCode == studentCode.Trim()
                     && (a.Status == AttemptStatus.InProgress || !a.IsSubmitted)
                     && a.DeadlineTime > now)
            .OrderByDescending(a => a.StartTime)
            .FirstOrDefaultAsync();

        if (attempt == null)
            return null;

        return new AttemptSessionDto
        {
            AttemptId = attempt.Id,
            ExamId = attempt.ExamId,
            ExamTitle = attempt.Exam?.Title ?? "Đề thi TOEIC",
            StudentName = attempt.StudentName,
            StudentCode = attempt.StudentCode,
            StartTime = attempt.StartTime,
            DeadlineTime = attempt.DeadlineTime,
            RemainingSeconds = Math.Max(0, (int)(attempt.DeadlineTime - now).TotalSeconds)
        };
    }

    [UnitOfWork]
    public async Task<ExamResultDto> SubmitAttemptAsync(SubmitAttemptInput input)
    {
        if (input == null || input.AttemptId == Guid.Empty)
            throw new UserFriendlyException("Mã lượt thi không hợp lệ.");

        // Lock theo từng AttemptId chống race condition khi double submit cùng mili-giây
        var attemptLock = _attemptLocks.GetOrAdd(input.AttemptId, _ => new System.Threading.SemaphoreSlim(1, 1));
        await attemptLock.WaitAsync();

        try
        {
            var attempt = await _attemptRepository.FirstOrDefaultAsync(a => a.Id == input.AttemptId);
            if (attempt == null)
                throw new UserFriendlyException("Lượt thi không tồn tại.");

            // IDEMPOTENCY / DOUBLE SUBMIT PROTECTION (TEST E):
            // Nếu bài thi đã nộp (Status == Completed hoặc IsSubmitted == true): trả về kết quả đã lưu ngay!
            if (attempt.Status == AttemptStatus.Completed || (attempt.IsSubmitted && attempt.SubmissionTime.HasValue))
            {
                return await GetAttemptResultAsync(input.AttemptId);
            }

            // Chuyển trạng thái sang Submitting để bảo vệ atomic
            attempt.Status = AttemptStatus.Submitting;
            attempt.ConcurrencyToken = Guid.NewGuid().ToString("N");
            await _attemptRepository.UpdateAsync(attempt);
            await CurrentUnitOfWork.SaveChangesAsync();

            // Cache câu hỏi trong RAM để 1000 request cùng nộp bài không làm nghẽn DB
            if (!_examQuestionsCache.TryGetValue(attempt.ExamId, out var questions))
            {
                questions = await _questionRepository.GetAll()
                    .Where(q => q.ExamId == attempt.ExamId)
                    .AsNoTracking()
                    .ToListAsync();
                _examQuestionsCache[attempt.ExamId] = questions;
            }

            var answers = await _attemptAnswerRepository.GetAllListAsync(a => a.ExamAttemptId == input.AttemptId);
            var answersMap = answers.ToDictionary(a => a.QuestionId, a => a);

            int correctCount = 0;
            int wrongCount = 0;
            int skippedCount = 0;
            int p5Correct = 0, p6Correct = 0, p7Correct = 0;

            foreach (var q in questions)
            {
                if (!answersMap.TryGetValue(q.Id, out var ans) || string.IsNullOrWhiteSpace(ans.SelectedOption))
                {
                    skippedCount++;
                    if (ans != null) ans.IsCorrect = false;
                }
                else
                {
                    bool isCorrect = string.Equals(ans.SelectedOption.Trim(), q.CorrectOption?.Trim(), StringComparison.OrdinalIgnoreCase);
                    if (isCorrect)
                    {
                        correctCount++;
                        ans.IsCorrect = true;
                        if (q.Part == ToeicPart.Part5) p5Correct++;
                        else if (q.Part == ToeicPart.Part6) p6Correct++;
                        else if (q.Part == ToeicPart.Part7) p7Correct++;
                    }
                    else
                    {
                        wrongCount++;
                        ans.IsCorrect = false;
                    }
                }
            }

            var now = DateTime.UtcNow;
            attempt.SubmissionTime = now;
            attempt.ElapsedTimeSeconds = Math.Max(0, (int)(now - attempt.StartTime).TotalSeconds);
            attempt.IsSubmitted = true;
            attempt.Status = AttemptStatus.Completed;
            attempt.CorrectCount = correctCount;
            attempt.WrongCount = wrongCount;
            attempt.SkippedCount = skippedCount;
            attempt.Part5Score = p5Correct;
            attempt.Part6Score = p6Correct;
            attempt.Part7Score = p7Correct;
            attempt.AccuracyPercentage = questions.Count > 0 ? Math.Round((double)correctCount / questions.Count * 100.0, 1) : 0;

            await _attemptRepository.UpdateAsync(attempt);
            await CurrentUnitOfWork.SaveChangesAsync();

            return await GetAttemptResultAsync(input.AttemptId);
        }
        finally
        {
            attemptLock.Release();
        }
    }

    // AUDIO DISTRIBUTION SERVICES
    public async Task<List<AudioTrackDto>> GetAudioTracksAsync(int? examId)
    {
        var query = _audioRepository.GetAll().Where(a => a.IsPublic);
        if (examId.HasValue && examId.Value > 0)
        {
            query = query.Where(a => a.ExamId == examId.Value || a.ExamId == null);
        }

        return await query.Select(a => new AudioTrackDto
        {
            Id = a.Id,
            ExamId = a.ExamId,
            Title = a.Title,
            FileName = a.FileName,
            StreamUrl = "/api/toeic/audio/stream/" + a.FileName,
            DurationSeconds = a.DurationSeconds,
            FileSize = a.FileSize,
            Part = a.Part,
            IsPublic = a.IsPublic
        }).ToListAsync();
    }

    public async Task<ToeicAudio> GetAudioByIdAsync(int id)
    {
        return await _audioRepository.FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<ToeicAudio> GetAudioByFileNameAsync(string fileName)
    {
        return await _audioRepository.FirstOrDefaultAsync(a => a.FileName == fileName);
    }

    public async Task RegisterAudioAsync(string title, string fileName, string filePath, long fileSize, int durationSeconds, int? examId, int? part)
    {
        var audio = await _audioRepository.FirstOrDefaultAsync(a => a.FileName == fileName);
        if (audio == null)
        {
            audio = new ToeicAudio
            {
                Title = title,
                FileName = fileName,
                FilePath = filePath,
                FileSize = fileSize,
                DurationSeconds = durationSeconds,
                ExamId = examId,
                Part = part,
                ContentType = "audio/mpeg",
                IsPublic = true,
                ListenCount = 0
            };
            await _audioRepository.InsertAsync(audio);
        }
        else
        {
            audio.Title = title;
            audio.FilePath = filePath;
            audio.FileSize = fileSize;
            audio.DurationSeconds = durationSeconds;
            audio.ExamId = examId;
            audio.Part = part;
            await _audioRepository.UpdateAsync(audio);
        }
    }

    public async Task<ExamResultDto> GetAttemptResultAsync(Guid attemptId)
    {
        var attempt = await _attemptRepository.FirstOrDefaultAsync(a => a.Id == attemptId);
        if (attempt == null)
            throw new UserFriendlyException("Lượt thi không tồn tại.");

        var exam = await _examRepository.GetAsync(attempt.ExamId);
        var questions = await _questionRepository.GetAll()
            .Where(q => q.ExamId == attempt.ExamId)
            .Include(q => q.Options)
            .Include(q => q.QuestionGroup)
            .OrderBy(q => q.QuestionNumber)
            .ToListAsync();

        var answers = await _attemptAnswerRepository.GetAllListAsync(a => a.ExamAttemptId == attemptId);
        var answersMap = answers.ToDictionary(a => a.QuestionId, a => a);

        int correctCount = 0;
        int wrongCount = 0;
        int skippedCount = 0;

        int p5Correct = 0, p5Total = 0;
        int p6Correct = 0, p6Total = 0;
        int p7Correct = 0, p7Total = 0;

        var reviews = new List<QuestionReviewDto>();

        foreach (var q in questions)
        {
            answersMap.TryGetValue(q.Id, out var ans);
            bool isSkipped = ans == null || string.IsNullOrWhiteSpace(ans.SelectedOption);
            bool isCorrect = !isSkipped && string.Equals(ans.SelectedOption?.Trim(), q.CorrectOption?.Trim(), StringComparison.OrdinalIgnoreCase);

            if (isSkipped) skippedCount++;
            else if (isCorrect) correctCount++;
            else wrongCount++;

            if (q.Part == ToeicPart.Part5) { p5Total++; if (isCorrect) p5Correct++; }
            else if (q.Part == ToeicPart.Part6) { p6Total++; if (isCorrect) p6Correct++; }
            else if (q.Part == ToeicPart.Part7) { p7Total++; if (isCorrect) p7Correct++; }

            reviews.Add(new QuestionReviewDto
            {
                QuestionId = q.Id,
                QuestionNumber = q.QuestionNumber,
                Part = q.Part,
                Stem = q.Stem,
                PassageTitle = q.QuestionGroup?.Title,
                PassageText = q.QuestionGroup?.PassageText,
                SelectedOption = ans?.SelectedOption,
                CorrectOption = q.CorrectOption,
                IsCorrect = isCorrect,
                IsSkipped = isSkipped,
                Explanation = q.Explanation,
                Options = q.Options.OrderBy(o => o.OptionKey).Select(o => new AnswerOptionDto
                {
                    Id = o.Id,
                    QuestionId = o.QuestionId,
                    OptionKey = o.OptionKey,
                    Content = o.Content,
                    IsCorrect = o.IsCorrect
                }).ToList()
            });
        }

        var submissionTime = attempt.SubmissionTime ?? DateTime.UtcNow;
        var timeSpent = (int)(submissionTime - attempt.StartTime).TotalSeconds;
        if (timeSpent < 0) timeSpent = 0;

        double accuracy = questions.Count > 0 ? (double)correctCount / questions.Count * 100.0 : 0.0;

        return new ExamResultDto
        {
            AttemptId = attempt.Id,
            ExamId = exam.Id,
            ExamTitle = exam.Title,
            StudentName = attempt.StudentName,
            StudentCode = attempt.StudentCode,
            StartTime = attempt.StartTime,
            SubmissionTime = attempt.SubmissionTime,
            TimeSpentSeconds = timeSpent,
            CorrectCount = correctCount,
            WrongCount = wrongCount,
            SkippedCount = skippedCount,
            TotalQuestions = questions.Count,
            AccuracyPercentage = Math.Round(accuracy, 1),
            Part5Correct = p5Correct,
            Part5Total = p5Total,
            Part6Correct = p6Correct,
            Part6Total = p6Total,
            Part7Correct = p7Correct,
            Part7Total = p7Total,
            QuestionReviews = reviews
        };
    }

    #endregion
}
