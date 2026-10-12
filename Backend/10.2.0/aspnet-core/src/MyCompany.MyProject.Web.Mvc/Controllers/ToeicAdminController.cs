using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MyCompany.MyProject.Controllers;
using MyCompany.MyProject.Toeic.Dto;
using MyCompany.MyProject.Toeic.Services;

namespace MyCompany.MyProject.Web.Controllers;

public class ToeicAdminController : MyProjectControllerBase
{
    private readonly IToeicAppService _toeicAppService;
    private readonly IWebHostEnvironment _env;

    public ToeicAdminController(IToeicAppService toeicAppService, IWebHostEnvironment env)
    {
        _toeicAppService = toeicAppService;
        _env = env;
    }

    // GET: /ToeicAdmin
    public async Task<IActionResult> Index(GetExamsInput input)
    {
        var model = await _toeicAppService.GetPagedExamsAsync(input);
        return View(model);
    }

    // GET: /ToeicAdmin/Edit/{id}
    public async Task<IActionResult> Edit(int id)
    {
        var exam = await _toeicAppService.GetExamDetailAsync(id);
        return View(exam);
    }

    // POST: /ToeicAdmin/ImportDocx
    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> ImportDocx(IFormFile docxFile)
    {
        if (docxFile == null || docxFile.Length == 0)
        {
            return Json(new { success = false, errors = new[] { "Vui lòng chọn file Word (.docx) hợp lệ!" } });
        }

        if (!Path.GetExtension(docxFile.FileName).Equals(".docx", StringComparison.OrdinalIgnoreCase))
        {
            return Json(new { success = false, errors = new[] { "Định dạng file không hỗ trợ! Hệ thống chỉ hỗ trợ .docx" } });
        }

        var uploadsFolder = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads", "passages");
        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
        }

        using var memoryStream = new MemoryStream();
        await docxFile.CopyToAsync(memoryStream);
        memoryStream.Position = 0;

        var result = await _toeicAppService.ImportDocxAsync(memoryStream, uploadsFolder);

        if (result.Success)
        {
            return Json(new
            {
                success = true,
                examId = result.ExamId,
                title = result.ExamTitle,
                code = result.ExamCode,
                total = result.TotalParsedQuestions,
                p5 = result.Part5Count,
                p6 = result.Part6Count,
                p7 = result.Part7Count,
                dbStatus = "Đã lưu thành công vào CSDL SQL Server (ToeicExamDb)"
            });
        }
        else
        {
            return Json(new
            {
                success = false,
                errors = result.ValidationErrors
            });
        }
    }


    // GET: /ToeicAdmin/DownloadTemplate
    public async Task<IActionResult> DownloadTemplate(string title = "ETS 2024 TOEIC Reading Practice Test", string code = "ETS-2024-TEST-01")
    {
        var bytes = await _toeicAppService.GenerateStandardDocxTemplateAsync(title, code);

        try
        {
            var wwwrootTemplateDir = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "templates");
            if (!Directory.Exists(wwwrootTemplateDir)) Directory.CreateDirectory(wwwrootTemplateDir);
            var wwwrootTemplatePath = Path.Combine(wwwrootTemplateDir, "standard_template.docx");
            await System.IO.File.WriteAllBytesAsync(wwwrootTemplatePath, bytes);

            var rootTemplatePath = @"D:\TOEIC-Reading-Exam\standard_template.docx";
            await System.IO.File.WriteAllBytesAsync(rootTemplatePath, bytes);
        }
        catch { }

        Response.Headers["Content-Disposition"] = "attachment; filename=\"standard_template.docx\"";
        return File(bytes, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", "standard_template.docx");
    }

    // POST: /ToeicAdmin/UpdateQuestion
    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> UpdateQuestion([FromBody] UpdateQuestionInput input)
    {
        if (input == null)
            return Json(new { success = false, message = "Dữ liệu không hợp lệ" });

        await _toeicAppService.UpdateQuestionAsync(input);
        return Json(new { success = true });
    }

    // POST: /ToeicAdmin/Publish
    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Publish(int id, bool isPublished)
    {
        await _toeicAppService.PublishExamAsync(id, isPublished);
        return Json(new { success = true });
    }

    // POST: /ToeicAdmin/Delete
    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        await _toeicAppService.DeleteExamAsync(id);
        return Json(new { success = true });
    }
}
