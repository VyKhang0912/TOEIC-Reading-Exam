using System;
using System.IO;
using System.Linq;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Validation;
using MyCompany.MyProject.Toeic.Services;
using Shouldly;
using Xunit;

namespace MyCompany.MyProject.Tests
{
    public class ToeicDocxTests : MyProjectTestBase
    {
        [Fact]
        public void Should_Validate_And_Inspect_Test1Docx()
        {
            string test1Path = @"D:\TOEIC-Reading-Exam\test1.docx";
            if (!File.Exists(test1Path)) return;

            var extractedText = new System.Text.StringBuilder();

            using (var wordDoc = WordprocessingDocument.Open(test1Path, false))
            {
                var body = wordDoc.MainDocumentPart?.Document?.Body;
                foreach (var element in body.Elements())
                {
                    if (element is DocumentFormat.OpenXml.Wordprocessing.Paragraph p)
                    {
                        var text = p.InnerText.Trim();
                        if (!string.IsNullOrEmpty(text))
                        {
                            extractedText.AppendLine(text);
                        }
                    }
                }
                Console.WriteLine($"ImageParts in test1.docx: {wordDoc.MainDocumentPart?.ImageParts?.Count()}");
            }

            string full = extractedText.ToString();
            Console.WriteLine($"Full text length: {full.Length}");

            // 1. Test EXAM tag
            var examMatch = System.Text.RegularExpressions.Regex.Match(full, @"\[EXAM(?:\s+([^\]]+))?\](.*?)\[/EXAM\]", System.Text.RegularExpressions.RegexOptions.Singleline | System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            Console.WriteLine($"Exam match: {examMatch.Success}");
            if (examMatch.Success)
            {
                string attr = examMatch.Groups[1].Value;
                var idMatch = System.Text.RegularExpressions.Regex.Match(attr, @"id=""([^""]+)""");
                var titleMatch = System.Text.RegularExpressions.Regex.Match(attr, @"title=""([^""]+)""");
                var durMatch = System.Text.RegularExpressions.Regex.Match(attr, @"duration_minutes=""(\d+)""");
                Console.WriteLine($"  Id: {idMatch.Groups[1].Value}, Title: {titleMatch.Groups[1].Value}, Duration: {durMatch.Groups[1].Value}");
            }

            // 2. Test PART tags
            var partMatches = System.Text.RegularExpressions.Regex.Matches(full, @"\[PART(?:\s+([^\]]+))?\](.*?)\[/PART(?:\s*\d+)?\]", System.Text.RegularExpressions.RegexOptions.Singleline | System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            Console.WriteLine($"Part matches count: {partMatches.Count}");
            foreach (System.Text.RegularExpressions.Match pm in partMatches)
            {
                string attr = pm.Groups[1].Value;
                var numMatch = System.Text.RegularExpressions.Regex.Match(attr, @"number=""(\d+)""");
                Console.WriteLine($"  Part attribute: {attr} -> Number: {numMatch.Groups[1].Value}, Content length: {pm.Groups[2].Value.Length}");
            }

            // 3. Test PASSAGE tags
            var passageMatches = System.Text.RegularExpressions.Regex.Matches(full, @"\[PASSAGE\s+([^\]]+)\](.*?)\[/PASSAGE\]", System.Text.RegularExpressions.RegexOptions.Singleline | System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            Console.WriteLine($"Passage matches count: {passageMatches.Count}");

            // 4. Test QUESTION tags
            var qMatches = System.Text.RegularExpressions.Regex.Matches(full, @"\[QUESTION(?:\s+([^\]]+))?\](.*?)\[/QUESTION\]", System.Text.RegularExpressions.RegexOptions.Singleline | System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            Console.WriteLine($"Question matches count: {qMatches.Count}");
            qMatches.Count.ShouldBe(100);
        }


        [Fact]
        public void Should_Parse_Real_Test1Docx_Successfully()
        {
            string test1Path = @"D:\TOEIC-Reading-Exam\test1.docx";
            if (!File.Exists(test1Path)) return;

            var parser = new DocxParserService();
            string tempUploadDir = Path.Combine(Path.GetTempPath(), "toeic_test_uploads");
            if (!Directory.Exists(tempUploadDir)) Directory.CreateDirectory(tempUploadDir);

            using var stream = File.OpenRead(test1Path);
            var result = parser.ParseDocx(stream, tempUploadDir);

            foreach (var err in result.Errors)
            {
                Console.WriteLine($"[PARSE ERROR] {err}");
            }

            result.Success.ShouldBeTrue();
            result.Model.ShouldNotBeNull();
            result.Model.Questions.Count.ShouldBe(100);
            result.Model.Questions.Count(q => q.Part == MyCompany.MyProject.Toeic.ToeicPart.Part5).ShouldBe(30);
            result.Model.Questions.Count(q => q.Part == MyCompany.MyProject.Toeic.ToeicPart.Part6).ShouldBe(16);
            result.Model.Questions.Count(q => q.Part == MyCompany.MyProject.Toeic.ToeicPart.Part7).ShouldBe(54);
            result.Model.Groups.Count.ShouldBeGreaterThan(0);
        }

        [Fact]
        public void Should_Generate_Valid_Standard_Template()
        {
            var parser = new DocxParserService();
            var bytes = parser.GenerateStandardDocx("ETS 2024 TOEIC Reading Practice Test", "ETS-2024-TEST-01");

            // Lưu file ra disk để kiểm tra và phục vụ người dùng
            string rootPath = @"D:\TOEIC-Reading-Exam\standard_template.docx";
            File.WriteAllBytes(rootPath, bytes);

            string mvcTemplatePath = @"D:\TOEIC-Reading-Exam\Backend\10.2.0\aspnet-core\src\MyCompany.MyProject.Web.Mvc\wwwroot\templates\standard_template.docx";
            string mvcDir = Path.GetDirectoryName(mvcTemplatePath);
            if (!Directory.Exists(mvcDir)) Directory.CreateDirectory(mvcDir);
            File.WriteAllBytes(mvcTemplatePath, bytes);

            using var mem = new MemoryStream(bytes);
            using var doc = WordprocessingDocument.Open(mem, false);
            var validator = new OpenXmlValidator();
            var valErrors = validator.Validate(doc).ToList();

            foreach (var v in valErrors)
            {
                Console.WriteLine($"[OPENXML VALIDATION ERROR] {v.Description} ({v.Node?.LocalName})");
            }

            valErrors.Count.ShouldBe(0);
        }

        [Fact]
        public void Should_Return_Detailed_Validation_Errors_When_Docx_Is_Invalid()
        {
            var parser = new DocxParserService();
            string invalidDocxPath = @"D:\TOEIC-Reading-Exam\invalid_sample.docx";

            // Tạo file docx chỉ có 1 câu và thiếu đáp án đúng hợp lệ
            using (var mem = new MemoryStream())
            {
                using (var doc = WordprocessingDocument.Create(mem, DocumentFormat.OpenXml.WordprocessingDocumentType.Document))
                {
                    var main = doc.AddMainDocumentPart();
                    main.Document = new DocumentFormat.OpenXml.Wordprocessing.Document();
                    var body = new DocumentFormat.OpenXml.Wordprocessing.Body();
                    main.Document.Append(body);

                    body.Append(new DocumentFormat.OpenXml.Wordprocessing.Paragraph(new DocumentFormat.OpenXml.Wordprocessing.Run(new DocumentFormat.OpenXml.Wordprocessing.Text("[EXAM]"))));
                    body.Append(new DocumentFormat.OpenXml.Wordprocessing.Paragraph(new DocumentFormat.OpenXml.Wordprocessing.Run(new DocumentFormat.OpenXml.Wordprocessing.Text("Title: Invalid Test"))));
                    body.Append(new DocumentFormat.OpenXml.Wordprocessing.Paragraph(new DocumentFormat.OpenXml.Wordprocessing.Run(new DocumentFormat.OpenXml.Wordprocessing.Text("[/EXAM]"))));
                    body.Append(new DocumentFormat.OpenXml.Wordprocessing.Paragraph(new DocumentFormat.OpenXml.Wordprocessing.Run(new DocumentFormat.OpenXml.Wordprocessing.Text("[PART 5]"))));
                    body.Append(new DocumentFormat.OpenXml.Wordprocessing.Paragraph(new DocumentFormat.OpenXml.Wordprocessing.Run(new DocumentFormat.OpenXml.Wordprocessing.Text("[QUESTION 101]"))));
                    body.Append(new DocumentFormat.OpenXml.Wordprocessing.Paragraph(new DocumentFormat.OpenXml.Wordprocessing.Run(new DocumentFormat.OpenXml.Wordprocessing.Text("Stem: Sample question"))));
                    body.Append(new DocumentFormat.OpenXml.Wordprocessing.Paragraph(new DocumentFormat.OpenXml.Wordprocessing.Run(new DocumentFormat.OpenXml.Wordprocessing.Text("[OPTION A] Option A"))));
                    body.Append(new DocumentFormat.OpenXml.Wordprocessing.Paragraph(new DocumentFormat.OpenXml.Wordprocessing.Run(new DocumentFormat.OpenXml.Wordprocessing.Text("[OPTION B] Option B"))));
                    body.Append(new DocumentFormat.OpenXml.Wordprocessing.Paragraph(new DocumentFormat.OpenXml.Wordprocessing.Run(new DocumentFormat.OpenXml.Wordprocessing.Text("[CORRECT] Z"))));
                    body.Append(new DocumentFormat.OpenXml.Wordprocessing.Paragraph(new DocumentFormat.OpenXml.Wordprocessing.Run(new DocumentFormat.OpenXml.Wordprocessing.Text("[/QUESTION]"))));
                    body.Append(new DocumentFormat.OpenXml.Wordprocessing.Paragraph(new DocumentFormat.OpenXml.Wordprocessing.Run(new DocumentFormat.OpenXml.Wordprocessing.Text("[/PART 5]"))));

                    main.Document.Save();
                }

                File.WriteAllBytes(invalidDocxPath, mem.ToArray());
            }

            using var stream = File.OpenRead(invalidDocxPath);
            var outcome = parser.ParseDocx(stream, Path.GetTempPath());

            outcome.Success.ShouldBeFalse();
            outcome.Errors.ShouldNotBeEmpty();
            Console.WriteLine($"[DETAILED ERRORS COUNT]: {outcome.Errors.Count}");
            foreach (var err in outcome.Errors.Take(10))
            {
                Console.WriteLine($"  - {err}");
            }

            outcome.Errors.Any(e => e.Contains("Tổng số câu hỏi không đủ 100 câu")).ShouldBeTrue();
            outcome.Errors.Any(e => e.Contains("Part 5 bắt buộc có đúng 30 câu hỏi")).ShouldBeTrue();
            outcome.Errors.Any(e => e.Contains("Part 6 bắt buộc có đúng 16 câu hỏi")).ShouldBeTrue();
            outcome.Errors.Any(e => e.Contains("Part 7 bắt buộc có đúng 54 câu hỏi")).ShouldBeTrue();
            outcome.Errors.Any(e => e.Contains("Đáp án đúng (Correct Key) không hợp lệ")).ShouldBeTrue();
        }
    }
}
