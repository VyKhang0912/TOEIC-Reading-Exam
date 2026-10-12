using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using MyCompany.MyProject.Controllers;
using MyCompany.MyProject.Toeic.Dto;
using MyCompany.MyProject.Toeic.Services;

namespace MyCompany.MyProject.Web.Controllers;

[ApiController]
[Route("api/toeic")]
[IgnoreAntiforgeryToken]
public class ToeicApiController : MyProjectControllerBase
{
    private readonly IToeicAppService _toeicAppService;
    private readonly IWebHostEnvironment _env;

    public ToeicApiController(IToeicAppService toeicAppService, IWebHostEnvironment env)
    {
        _toeicAppService = toeicAppService;
        _env = env;
    }

    /// <summary>
    /// GET /api/toeic/exams
    /// Danh sách các đề thi đã xuất bản (Published) cho học viên
    /// </summary>
    [HttpGet("exams")]
    public async Task<IActionResult> GetPublishedExams()
    {
        var exams = await _toeicAppService.GetPublishedExamsAsync();
        return Ok(exams);
    }

    /// <summary>
    /// POST /api/toeic/attempts/start
    /// Bắt đầu một lượt thi mới theo ExamId chỉ định
    /// </summary>
    [HttpPost("attempts/start")]
    public async Task<IActionResult> StartAttempt([FromBody] StartAttemptInput input)
    {
        if (input == null || input.ExamId <= 0)
        {
            return BadRequest(new { message = "Vui lòng chọn đề thi hợp lệ." });
        }

        var session = await _toeicAppService.StartAttemptAsync(input);
        return Ok(session);
    }

    /// <summary>
    /// POST /api/toeic/sessions/start-smart
    /// THUẬT TOÁN PHÂN PHỐI ĐỀ THÔNG MINH CHO 1000 HỌC VIÊN ĐỒNG THỜI:
    /// Least-Assigned + Random Tie Break + Atomic Allocation + Reload Protection
    /// </summary>
    [HttpPost("sessions/start-smart")]
    public async Task<IActionResult> StartSmartSession([FromBody] StartSmartSessionInput input)
    {
        if (input == null)
        {
            input = new StartSmartSessionInput();
        }

        var session = await _toeicAppService.StartSmartSessionAsync(input);
        return Ok(session);
    }

    /// <summary>
    /// GET /api/toeic/sessions/active?studentCode=...
    /// Kiểm tra và tiếp tục lượt thi đang diễn ra khi học viên mở lại hoặc reload ứng dụng
    /// </summary>
    [HttpGet("sessions/active")]
    public async Task<IActionResult> GetActiveSession([FromQuery] string studentCode)
    {
        if (string.IsNullOrWhiteSpace(studentCode))
        {
            return BadRequest(new { message = "Mã học viên không được để trống." });
        }

        var session = await _toeicAppService.GetActiveSessionAsync(studentCode);
        return Ok(session);
    }

    /// <summary>
    /// GET /api/toeic/attempts/{attemptId}/questions
    /// Lấy danh sách 100 câu hỏi và các bài đọc cho lượt thi.
    /// BẢO MẬT: KHÔNG chứa đáp án đúng trước khi nộp bài!
    /// </summary>
    [HttpGet("attempts/{attemptId}/questions")]
    public async Task<IActionResult> GetAttemptQuestions(Guid attemptId)
    {
        if (attemptId == Guid.Empty)
        {
            return BadRequest(new { message = "Mã lượt thi không hợp lệ." });
        }

        var questions = await _toeicAppService.GetAttemptQuestionsAsync(attemptId);
        return Ok(questions);
    }

    /// <summary>
    /// POST /api/toeic/attempts/save-answer
    /// Lưu tạm đáp án được chọn (A, B, C, D hoặc null nếu bỏ chọn) hoặc cờ đánh dấu
    /// </summary>
    [HttpPost("attempts/save-answer")]
    public async Task<IActionResult> SaveAnswer([FromBody] SaveAnswerInput input)
    {
        if (input == null || input.AttemptId == Guid.Empty)
        {
            return BadRequest(new { message = "Dữ liệu lưu đáp án không hợp lệ." });
        }

        await _toeicAppService.SaveAnswerAsync(input);
        return Ok(new { success = true });
    }

    /// <summary>
    /// POST /api/toeic/attempts/submit
    /// Nộp bài thi, server thực hiện chấm điểm và tính thời gian làm bài
    /// BẢO VỆ 1000 HỌC VIÊN ĐỒNG THỜI + CHỐNG DOUBLE SUBMIT + IDEMPOTENT
    /// </summary>
    [HttpPost("attempts/submit")]
    public async Task<IActionResult> SubmitAttempt([FromBody] SubmitAttemptInput input)
    {
        if (input == null || input.AttemptId == Guid.Empty)
        {
            return BadRequest(new { message = "Mã lượt thi không hợp lệ." });
        }

        var result = await _toeicAppService.SubmitAttemptAsync(input);
        return Ok(result);
    }

    /// <summary>
    /// GET /api/toeic/attempts/{attemptId}/result
    /// Lấy kết quả điểm số, độ chính xác và xem lại từng câu hỏi cùng đáp án đúng + giải thích
    /// </summary>
    [HttpGet("attempts/{attemptId}/result")]
    public async Task<IActionResult> GetAttemptResult(Guid attemptId)
    {
        if (attemptId == Guid.Empty)
        {
            return BadRequest(new { message = "Mã lượt thi không hợp lệ." });
        }

        var result = await _toeicAppService.GetAttemptResultAsync(attemptId);
        return Ok(result);
    }

    /// <summary>
    /// GET /api/toeic/audios?examId=...
    /// Lấy danh sách metadata các track audio của đề thi TOEIC
    /// </summary>
    [HttpGet("audios")]
    public async Task<IActionResult> GetAudioTracks([FromQuery] int? examId)
    {
        var tracks = await _toeicAppService.GetAudioTracksAsync(examId);
        return Ok(tracks);
    }

    /// <summary>
    /// GET /api/toeic/audio/stream/{fileName}
    /// PHÂN PHỐI AUDIO CHO 1000 HỌC VIÊN ĐỒNG THỜI:
    /// - Hỗ trợ HTTP Range Requests (Status 206 Partial Content) để phát từng phần/seek
    /// - Content-Type: audio/mpeg
    /// - Cache-Control & Accept-Ranges headers
    /// - Stream trực tiếp từ file buffer OS, KHÔNG tải toàn bộ file MP3 vào RAM
    /// </summary>
    [HttpGet("audio/stream/{fileName}")]
    public IActionResult StreamAudio(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return BadRequest(new { message = "Tên file audio không hợp lệ." });
        }

        var safeName = Path.GetFileName(fileName);
        var audioFolder = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "audio");
        var filePath = Path.Combine(audioFolder, safeName);

        if (!System.IO.File.Exists(filePath))
        {
            return NotFound(new { message = "File audio không tồn tại trên máy chủ." });
        }

        Response.Headers["Accept-Ranges"] = "bytes";
        Response.Headers["Cache-Control"] = "public, max-age=86400, stale-while-revalidate=3600";
        Response.Headers["Access-Control-Allow-Origin"] = "*";

        // PhysicalFile với enableRangeProcessing: true tự động xử lý HTTP Range header và trả về 206 Partial Content
        return PhysicalFile(filePath, "audio/mpeg", enableRangeProcessing: true);
    }

    /// <summary>
    /// GET /api/toeic/audio/{id}/stream
    /// Stream audio theo ID metadata
    /// </summary>
    [HttpGet("audio/{id:int}/stream")]
    public async Task<IActionResult> StreamAudioById(int id)
    {
        var audio = await _toeicAppService.GetAudioByIdAsync(id);
        if (audio == null)
        {
            return NotFound(new { message = "Bản ghi audio không tồn tại." });
        }

        return StreamAudio(audio.FileName);
    }
}
