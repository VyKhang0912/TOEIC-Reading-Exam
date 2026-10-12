using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using MyCompany.MyProject.Toeic.Dto;

namespace MyCompany.MyProject.Toeic.Services;

public class ParsedExamModel
{
    public string Title { get; set; } = "TOEIC Reading Test";
    public string Code { get; set; } = $"ETS-{DateTime.UtcNow:yyyyMMddHHmmss}";
    public string Description { get; set; } = "ETS 2024 Standard TOEIC Reading Exam";
    public int DurationMinutes { get; set; } = 75;
    public int TotalQuestions { get; set; } = 100;
    public List<ParsedGroupModel> Groups { get; set; } = new List<ParsedGroupModel>();
    public List<ParsedQuestionModel> Questions { get; set; } = new List<ParsedQuestionModel>();
}

public class ParsedGroupModel
{
    public ToeicPart Part { get; set; }
    public int Order { get; set; }
    public string Title { get; set; }
    public string PassageType { get; set; } = "General";
    public string PassageText { get; set; }
    public string ImageUrls { get; set; }
    public List<ParsedQuestionModel> Questions { get; set; } = new List<ParsedQuestionModel>();
}

public class ParsedQuestionModel
{
    public int QuestionNumber { get; set; }
    public ToeicPart Part { get; set; }
    public string Stem { get; set; }
    public string Explanation { get; set; }
    public string CorrectOption { get; set; }
    public Dictionary<string, string> Options { get; set; } = new Dictionary<string, string>();
    public ParsedGroupModel Group { get; set; }
}

public class DocxParserService
{
    public (bool Success, ParsedExamModel Model, List<string> Errors) ParseDocx(Stream docxStream, string uploadsFolder)
    {
        var errors = new List<string>();
        var parsedExam = new ParsedExamModel();
        var extractedText = new StringBuilder();
        var savedImagePaths = new List<string>();

        try
        {
            using (var wordDoc = WordprocessingDocument.Open(docxStream, false))
            {
                var mainPart = wordDoc.MainDocumentPart;
                var body = mainPart?.Document?.Body;
                if (body == null)
                {
                    errors.Add("Tập tin Word không có nội dung hợp lệ.");
                    return (false, null, errors);
                }

                // 1. Trích xuất text từ Paragraphs và Tables
                foreach (var element in body.Elements())
                {
                    if (element is Paragraph p)
                    {
                        var text = p.InnerText.Trim();
                        if (!string.IsNullOrEmpty(text))
                        {
                            extractedText.AppendLine(text);
                        }
                    }
                    else if (element is Table t)
                    {
                        foreach (var row in t.Elements<TableRow>())
                        {
                            var cellTexts = row.Elements<TableCell>().Select(c => c.InnerText.Trim());
                            extractedText.AppendLine(string.Join(" | ", cellTexts));
                        }
                    }
                }

                // 2. Trích xuất hình ảnh nhúng nếu có
                if (mainPart.ImageParts != null && mainPart.ImageParts.Any() && !string.IsNullOrWhiteSpace(uploadsFolder))
                {
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    int imgIndex = 1;
                    foreach (var imgPart in mainPart.ImageParts)
                    {
                        try
                        {
                            var ext = Path.GetExtension(imgPart.Uri.OriginalString);
                            if (string.IsNullOrEmpty(ext)) ext = ".png";
                            var fileName = $"docx_img_{DateTime.UtcNow:yyyyMMddHHmmss}_{imgIndex++}{ext}";
                            var targetPath = Path.Combine(uploadsFolder, fileName);
                            using (var sourceStream = imgPart.GetStream())
                            using (var targetStream = File.Create(targetPath))
                            {
                                sourceStream.CopyTo(targetStream);
                            }
                            savedImagePaths.Add($"/uploads/passages/{fileName}");
                        }
                        catch
                        {
                            // Bỏ qua lỗi lưu từng ảnh riêng lẻ
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            errors.Add($"Lỗi đọc file Word DOCX: {ex.Message}");
            return (false, null, errors);
        }

        var fullContent = extractedText.ToString();
        if (string.IsNullOrWhiteSpace(fullContent))
        {
            errors.Add("Nội dung văn bản trong file Word trống rỗng.");
            return (false, null, errors);
        }

        // Kiểm tra xem file thuộc Schema Tag-Attribute (như test1.docx) hay Schema Block-Tag cũ
        bool isAttributeSchema = fullContent.Contains("schema=\"TOEIC-TAGS-1\"", StringComparison.OrdinalIgnoreCase)
                                 || Regex.IsMatch(fullContent, @"\[PART\s+number=""\d+""", RegexOptions.IgnoreCase);

        if (isAttributeSchema)
        {
            ParseAttributeSchema(fullContent, parsedExam, savedImagePaths, errors);
        }
        else
        {
            ParseBlockSchema(fullContent, parsedExam, savedImagePaths, errors);
        }

        // Kiểm tra nghiêm ngặt 100 câu và tính hợp lệ của toàn bộ đề thi
        ValidateParsedExam(parsedExam, errors);

        if (errors.Any())
        {
            return (false, null, errors);
        }

        return (true, parsedExam, errors);
    }

    #region Schema 1: Tag-Attribute Schema (test1.docx - TOEIC-TAGS-1)

    private void ParseAttributeSchema(string fullContent, ParsedExamModel parsedExam, List<string> savedImages, List<string> errors)
    {
        // 1. Parse [EXAM id="..." title="..." duration_minutes="..." total_questions="..."]
        var examMatch = Regex.Match(fullContent, @"\[EXAM(?:\s+([^\]]+))?\](.*?)\[/EXAM\]", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        if (examMatch.Success)
        {
            string attr = examMatch.Groups[1].Value;
            var idMatch = Regex.Match(attr, @"id=""([^""]+)""", RegexOptions.IgnoreCase);
            if (idMatch.Success) parsedExam.Code = idMatch.Groups[1].Value.Trim();

            var titleMatch = Regex.Match(attr, @"title=""([^""]+)""", RegexOptions.IgnoreCase);
            if (titleMatch.Success) parsedExam.Title = titleMatch.Groups[1].Value.Trim();

            var durMatch = Regex.Match(attr, @"duration_minutes=""(\d+)""", RegexOptions.IgnoreCase);
            if (durMatch.Success && int.TryParse(durMatch.Groups[1].Value, out var dur)) parsedExam.DurationMinutes = dur;

            var totalMatch = Regex.Match(attr, @"total_questions=""(\d+)""", RegexOptions.IgnoreCase);
            if (totalMatch.Success && int.TryParse(totalMatch.Groups[1].Value, out var tot)) parsedExam.TotalQuestions = tot;
        }

        // 2. Parse Part 5: [PART number="5" ...] ... [/PART]
        var part5Match = Regex.Match(fullContent, @"\[PART\s+number=""5""[^\]]*\](.*?)\[/PART(?:\s*\d+)?\]", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        if (!part5Match.Success)
        {
            errors.Add("Thiếu khối [PART number=\"5\"]...[/PART] bắt buộc.");
        }
        else
        {
            ParseAttributeQuestions(part5Match.Groups[1].Value, ToeicPart.Part5, null, parsedExam, errors);
        }

        // 3. Parse Part 6: [PART number="6" ...] ... [/PART]
        var part6Match = Regex.Match(fullContent, @"\[PART\s+number=""6""[^\]]*\](.*?)\[/PART(?:\s*\d+)?\]", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        if (!part6Match.Success)
        {
            errors.Add("Thiếu khối [PART number=\"6\"]...[/PART] bắt buộc.");
        }
        else
        {
            ParseAttributePartWithPassages(part6Match.Groups[1].Value, ToeicPart.Part6, parsedExam, savedImages, errors);
        }

        // 4. Parse Part 7: [PART number="7" ...] ... [/PART]
        var part7Match = Regex.Match(fullContent, @"\[PART\s+number=""7""[^\]]*\](.*?)\[/PART(?:\s*\d+)?\]", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        if (!part7Match.Success)
        {
            errors.Add("Thiếu khối [PART number=\"7\"]...[/PART] bắt buộc.");
        }
        else
        {
            ParseAttributePartWithPassages(part7Match.Groups[1].Value, ToeicPart.Part7, parsedExam, savedImages, errors);
        }
    }

    private void ParseAttributePartWithPassages(string content, ToeicPart part, ParsedExamModel model, List<string> savedImages, List<string> errors)
    {
        // Trích xuất tất cả các đoạn [PASSAGE id="..." ...]
        var passageRegex = new Regex(@"\[PASSAGE\s+([^\]]+)\](.*?)\[/PASSAGE\]", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        var passageMatches = passageRegex.Matches(content);

        var passageDict = new Dictionary<string, ParsedGroupModel>(StringComparer.OrdinalIgnoreCase);
        int passageIndex = 0;

        foreach (Match pm in passageMatches)
        {
            string attr = pm.Groups[1].Value;
            string text = pm.Groups[2].Value.Trim();

            var idMatch = Regex.Match(attr, @"id=""([^""]+)""", RegexOptions.IgnoreCase);
            string passageId = idMatch.Success ? idMatch.Groups[1].Value.Trim() : $"P{model.Groups.Count + 1}";

            var qStartMatch = Regex.Match(attr, @"question_start=""(\d+)""", RegexOptions.IgnoreCase);
            var qEndMatch = Regex.Match(attr, @"question_end=""(\d+)""", RegexOptions.IgnoreCase);
            string rangeTitle = (qStartMatch.Success && qEndMatch.Success)
                ? $"Questions {qStartMatch.Groups[1].Value}–{qEndMatch.Groups[1].Value}"
                : $"Passage {passageId}";

            var typeMatch = Regex.Match(attr, @"content_type=""([^""]+)""", RegexOptions.IgnoreCase);
            string contentType = typeMatch.Success ? typeMatch.Groups[1].Value.Trim() : "Text";

            var group = new ParsedGroupModel
            {
                Part = part,
                Order = model.Groups.Count + 1,
                Title = rangeTitle,
                PassageType = contentType,
                PassageText = text
            };

            // Gán ảnh nếu có ảnh trích xuất
            if (savedImages != null && savedImages.Count > passageIndex)
            {
                group.ImageUrls = savedImages[passageIndex];
            }

            if (string.IsNullOrWhiteSpace(group.PassageText))
            {
                group.PassageText = !string.IsNullOrWhiteSpace(group.ImageUrls)
                    ? $"[Hình ảnh tài liệu: {rangeTitle}]"
                    : $"Tài liệu đọc hiểu {rangeTitle} ({contentType}). Vui lòng xem câu hỏi bên dưới.";
            }

            model.Groups.Add(group);
            passageDict[passageId] = group;
            passageIndex++;
        }

        // Parse các câu hỏi của Part
        var qRegex = new Regex(@"\[QUESTION(?:\s+([^\]]+))?\](.*?)\[/QUESTION\]", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        var qMatches = qRegex.Matches(content);

        foreach (Match qm in qMatches)
        {
            string attr = qm.Groups[1].Value;
            string qBody = qm.Groups[2].Value.Trim();

            var numMatch = Regex.Match(attr, @"number=""(\d+)""", RegexOptions.IgnoreCase);
            int qNum = numMatch.Success ? int.Parse(numMatch.Groups[1].Value) : 0;

            var passageAttrMatch = Regex.Match(attr, @"passage=""([^""]+)""", RegexOptions.IgnoreCase);
            string passageRef = passageAttrMatch.Success ? passageAttrMatch.Groups[1].Value.Trim() : "";

            ParsedGroupModel matchedGroup = null;
            if (!string.IsNullOrEmpty(passageRef) && passageDict.TryGetValue(passageRef, out var grp))
            {
                matchedGroup = grp;
            }
            else if (model.Groups.Any(g => g.Part == part))
            {
                // Fallback tìm theo khoảng câu hỏi trong title
                matchedGroup = model.Groups.LastOrDefault(g => g.Part == part);
            }

            var question = ParseSingleQuestion(qNum, qBody, part, matchedGroup, errors);
            if (question != null)
            {
                model.Questions.Add(question);
                matchedGroup?.Questions.Add(question);
            }
        }
    }

    private void ParseAttributeQuestions(string blockContent, ToeicPart part, ParsedGroupModel group, ParsedExamModel model, List<string> errors)
    {
        var qMatches = Regex.Matches(blockContent, @"\[QUESTION(?:\s+([^\]]+))?\](.*?)\[/QUESTION\]", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        foreach (Match qm in qMatches)
        {
            string attr = qm.Groups[1].Value;
            string qBody = qm.Groups[2].Value.Trim();

            var numMatch = Regex.Match(attr, @"number=""(\d+)""", RegexOptions.IgnoreCase);
            int qNum = numMatch.Success ? int.Parse(numMatch.Groups[1].Value) : 0;

            var question = ParseSingleQuestion(qNum, qBody, part, group, errors);
            if (question != null)
            {
                model.Questions.Add(question);
                group?.Questions.Add(question);
            }
        }
    }

    private ParsedQuestionModel ParseSingleQuestion(int qNum, string qBody, ToeicPart part, ParsedGroupModel group, List<string> errors)
    {
        // Trích xuất Stem
        string stem = "";
        var optFirstMatch = Regex.Match(qBody, @"\[OPTION(?:\s+key=""[A-D]""|\s*[A-D])\]", RegexOptions.IgnoreCase);
        if (optFirstMatch.Success)
        {
            stem = qBody.Substring(0, optFirstMatch.Index).Trim();
        }
        else
        {
            stem = qBody;
        }

        stem = Regex.Replace(stem, @"^(?:Stem:|Nội dung:)\s*", "", RegexOptions.IgnoreCase).Trim();

        // Trích xuất Options A, B, C, D (hỗ trợ cả [OPTION key="A"] và [OPTION A])
        var options = new Dictionary<string, string>();
        foreach (var key in new[] { "A", "B", "C", "D" })
        {
            var optRegex = new Regex($@"\[OPTION(?:\s+key=""{key}""|\s*{key})\]\s*(.*?)(?=\[OPTION|\[CORRECT|\[EXPLANATION|\[/QUESTION|\z)", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            var optMatch = optRegex.Match(qBody);
            if (optMatch.Success)
            {
                var optText = optMatch.Groups[1].Value.Trim();
                optText = Regex.Replace(optText, $@"\[/OPTION(?:\s*{key})?\]", "", RegexOptions.IgnoreCase).Trim();
                options[key] = optText;
            }
        }

        // Trích xuất Correct Option
        string correct = "";
        var corMatch = Regex.Match(qBody, @"\[CORRECT\]\s*([A-D])", RegexOptions.IgnoreCase);
        if (corMatch.Success)
        {
            correct = corMatch.Groups[1].Value.ToUpper().Trim();
        }

        // Trích xuất Explanation
        string explanation = "";
        var expMatch = Regex.Match(qBody, @"\[EXPLANATION\]\s*(.*?)(?=\[/QUESTION|\z)", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        if (expMatch.Success)
        {
            explanation = expMatch.Groups[1].Value.Trim();
        }

        return new ParsedQuestionModel
        {
            QuestionNumber = qNum,
            Part = part,
            Stem = stem,
            Explanation = explanation,
            CorrectOption = correct,
            Options = options,
            Group = group
        };
    }

    #endregion

    #region Schema 2: Block Tags Schema (Template chuẩn)

    private void ParseBlockSchema(string fullContent, ParsedExamModel parsedExam, List<string> savedImages, List<string> errors)
    {
        // 1. Parse [EXAM] block
        var examMatch = Regex.Match(fullContent, @"\[EXAM\](.*?)\[/EXAM\]", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        if (examMatch.Success)
        {
            var examText = examMatch.Groups[1].Value;
            var titleMatch = Regex.Match(examText, @"Title:\s*(.+)", RegexOptions.IgnoreCase);
            if (titleMatch.Success) parsedExam.Title = titleMatch.Groups[1].Value.Trim();

            var codeMatch = Regex.Match(examText, @"Code:\s*(.+)", RegexOptions.IgnoreCase);
            if (codeMatch.Success) parsedExam.Code = codeMatch.Groups[1].Value.Trim();

            var descMatch = Regex.Match(examText, @"Description:\s*(.+)", RegexOptions.IgnoreCase);
            if (descMatch.Success) parsedExam.Description = descMatch.Groups[1].Value.Trim();

            var durMatch = Regex.Match(examText, @"Duration:\s*(\d+)", RegexOptions.IgnoreCase);
            if (durMatch.Success && int.TryParse(durMatch.Groups[1].Value, out var dur)) parsedExam.DurationMinutes = dur;
        }

        // 2. Parse Part 5
        var part5Match = Regex.Match(fullContent, @"\[PART\s*5\](.*?)\[/PART\s*5\]", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        if (!part5Match.Success)
        {
            errors.Add("Thiếu khối [PART 5]...[/PART 5] bắt buộc.");
        }
        else
        {
            ParseBlockQuestions(part5Match.Groups[1].Value, ToeicPart.Part5, null, parsedExam, errors);
        }

        // 3. Parse Part 6
        var part6Match = Regex.Match(fullContent, @"\[PART\s*6\](.*?)\[/PART\s*6\]", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        if (!part6Match.Success)
        {
            errors.Add("Thiếu khối [PART 6]...[/PART 6] bắt buộc.");
        }
        else
        {
            ParseBlockPartWithGroups(part6Match.Groups[1].Value, ToeicPart.Part6, parsedExam, errors);
        }

        // 4. Parse Part 7
        var part7Match = Regex.Match(fullContent, @"\[PART\s*7\](.*?)\[/PART\s*7\]", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        if (!part7Match.Success)
        {
            errors.Add("Thiếu khối [PART 7]...[/PART 7] bắt buộc.");
        }
        else
        {
            ParseBlockPartWithGroups(part7Match.Groups[1].Value, ToeicPart.Part7, parsedExam, errors);
        }
    }

    private void ParseBlockPartWithGroups(string content, ToeicPart part, ParsedExamModel model, List<string> errors)
    {
        var groupMatches = Regex.Matches(content, @"\[GROUP(?:\s+\d+)?\](.*?)\[/GROUP\]", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        if (groupMatches.Count == 0)
        {
            errors.Add($"Part {(int)part} bắt buộc phải có các nhóm bài đọc [GROUP]...[/GROUP].");
            return;
        }

        int groupOrder = model.Groups.Count + 1;
        foreach (Match gm in groupMatches)
        {
            var groupText = gm.Groups[1].Value;
            var group = new ParsedGroupModel
            {
                Part = part,
                Order = groupOrder++,
                Title = $"Nhóm bài đọc Part {(int)part}"
            };

            var titleMatch = Regex.Match(groupText, @"\[TITLE\](.*?)\[/TITLE\]", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            if (titleMatch.Success) group.Title = titleMatch.Groups[1].Value.Trim();

            var typeMatch = Regex.Match(groupText, @"\[TYPE\](.*?)\[/TYPE\]", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            if (typeMatch.Success) group.PassageType = typeMatch.Groups[1].Value.Trim();

            var passageMatch = Regex.Match(groupText, @"\[PASSAGE\](.*?)\[/PASSAGE\]", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            if (passageMatch.Success) group.PassageText = passageMatch.Groups[1].Value.Trim();

            var imageMatch = Regex.Match(groupText, @"\[IMAGE\](.*?)\[/IMAGE\]", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            if (imageMatch.Success) group.ImageUrls = imageMatch.Groups[1].Value.Trim();

            if (string.IsNullOrWhiteSpace(group.PassageText) && string.IsNullOrWhiteSpace(group.ImageUrls))
            {
                errors.Add($"Part {(int)part} - Nhóm '{group.Title}': Thiếu nội dung bài đọc [PASSAGE] hoặc hình ảnh [IMAGE].");
            }

            model.Groups.Add(group);
            ParseBlockQuestions(groupText, part, group, model, errors);
        }
    }

    private void ParseBlockQuestions(string blockContent, ToeicPart part, ParsedGroupModel group, ParsedExamModel model, List<string> errors)
    {
        var qMatches = Regex.Matches(blockContent, @"\[QUESTION(?:\s+(\d+))?\](.*?)\[/QUESTION\]", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        foreach (Match qm in qMatches)
        {
            int qNum = 0;
            if (qm.Groups[1].Success && int.TryParse(qm.Groups[1].Value, out var n))
            {
                qNum = n;
            }

            var qBody = qm.Groups[2].Value.Trim();
            var question = ParseSingleQuestion(qNum, qBody, part, group, errors);
            if (question != null)
            {
                model.Questions.Add(question);
                group?.Questions.Add(question);
            }
        }
    }

    #endregion

    #region Strict Validation (100 Questions, P5:30, P6:16, P7:54)

    private void ValidateParsedExam(ParsedExamModel model, List<string> errors)
    {
        if (model.Questions.Count != 100)
        {
            errors.Add($"Tổng số câu hỏi không đủ 100 câu (hiện có: {model.Questions.Count} câu).");
        }

        int p5Count = model.Questions.Count(q => q.Part == ToeicPart.Part5);
        if (p5Count != 30)
        {
            errors.Add($"Part 5 bắt buộc có đúng 30 câu hỏi (hiện có: {p5Count} câu).");
        }

        int p6Count = model.Questions.Count(q => q.Part == ToeicPart.Part6);
        if (p6Count != 16)
        {
            errors.Add($"Part 6 bắt buộc có đúng 16 câu hỏi (hiện có: {p6Count} câu).");
        }

        int p7Count = model.Questions.Count(q => q.Part == ToeicPart.Part7);
        if (p7Count != 54)
        {
            errors.Add($"Part 7 bắt buộc có đúng 54 câu hỏi (hiện có: {p7Count} câu).");
        }

        var questionNumbers = model.Questions.Select(q => q.QuestionNumber).ToHashSet();
        for (int i = 101; i <= 200; i++)
        {
            if (!questionNumbers.Contains(i))
            {
                errors.Add($"Thiếu câu hỏi số #{i} trong đề thi.");
            }
        }

        foreach (var q in model.Questions)
        {
            string qRef = q.QuestionNumber > 0 ? $"Câu #{q.QuestionNumber}" : $"Câu hỏi Part {(int)q.Part}";

            if (string.IsNullOrWhiteSpace(q.Stem))
            {
                errors.Add($"{qRef}: Nội dung câu hỏi (Stem) trống.");
            }

            foreach (var opt in new[] { "A", "B", "C", "D" })
            {
                if (!q.Options.ContainsKey(opt) || string.IsNullOrWhiteSpace(q.Options[opt]))
                {
                    errors.Add($"{qRef}: Thiếu phương án lựa chọn [{opt}].");
                }
            }

            if (string.IsNullOrWhiteSpace(q.CorrectOption) || !new[] { "A", "B", "C", "D" }.Contains(q.CorrectOption))
            {
                errors.Add($"{qRef}: Đáp án đúng (Correct Key) không hợp lệ (nhận được: '{q.CorrectOption}', bắt buộc là A, B, C hoặc D).");
            }
        }
    }

    #endregion

    #region Standard Template Generation (OpenXML Valid & Microsoft Word Compatible)

    /// <summary>
    /// Generates a valid standard 100-question TOEIC Reading DOCX template file.
    /// Strictly complies with OpenXML ECMA-376 schema so Microsoft Word opens without any warning or error.
    /// </summary>
    public byte[] GenerateStandardDocx(string examTitle = "ETS 2024 TOEIC Reading Practice Test", string examCode = "ETS-2024-TEST-01")
    {
        using var memStream = new MemoryStream();
        using (var wordDoc = WordprocessingDocument.Create(memStream, WordprocessingDocumentType.Document, true))
        {
            var mainPart = wordDoc.AddMainDocumentPart();
            mainPart.Document = new Document();
            var body = new Body();
            mainPart.Document.Append(body);

            // Thêm Styles part chuẩn để Microsoft Word không cảnh báo thiếu định dạng
            var stylePart = mainPart.AddNewPart<StyleDefinitionsPart>();
            stylePart.Styles = new Styles();
            stylePart.Styles.Save();

            void AddP(string text, bool isBold = false)
            {
                var p = new Paragraph();
                var lines = (text ?? "").Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
                for (int i = 0; i < lines.Length; i++)
                {
                    var run = new Run();
                    if (isBold)
                    {
                        // ĐÚNG CHUẨN SCHEMA ECMA-376: Thẻ <w:b/> PHẢI nằm trong RunProperties <w:rPr/>
                        run.Append(new RunProperties(new Bold()));
                    }
                    run.Append(new Text(lines[i]) { Space = SpaceProcessingModeValues.Preserve });
                    p.Append(run);

                    if (i < lines.Length - 1)
                    {
                        p.Append(new Run(new Break()));
                    }
                }
                body.Append(p);
            }

            AddP("[EXAM]", true);
            AddP($"Title: {examTitle}");
            AddP($"Code: {examCode}");
            AddP("Description: Standard 100-Question TOEIC Reading Examination (Part 5, Part 6, Part 7)");
            AddP("Duration: 75");
            AddP("[/EXAM]");
            AddP("");

            // PART 5: 30 Questions (101 - 130)
            AddP("[PART 5]", true);
            string[] p5Vocab = {
                "convenient", "reliable", "efficient", "temporary", "productive",
                "innovative", "accurate", "flexible", "qualified", "accessible",
                "thorough", "significant", "complimentary", "promising", "eligible",
                "substantial", "strategic", "prompt", "collaborative", "essential",
                "outstanding", "exceptional", "diligent", "cost-effective", "confidential",
                "comprehensive", "competitive", "consistent", "beneficial", "adequate"
            };

            for (int i = 101; i <= 130; i++)
            {
                int idx = i - 101;
                string word = p5Vocab[idx % p5Vocab.Length];
                AddP($"[QUESTION {i}]");
                AddP($"Stem: The board of directors announced that the new policy will make our logistics services more ______ for all international partners.");
                AddP($"[OPTION A] {word}");
                AddP($"[OPTION B] {word}ly");
                AddP($"[OPTION C] {word}ness");
                AddP($"[OPTION D] {word}ity");
                AddP("[CORRECT] A");
                AddP($"[EXPLANATION] Vị trí sau 'more' và trước giới từ 'for' cần một tính từ bổ nghĩa cho tân ngữ -> chọn đáp án A ({word}).");
                AddP("[/QUESTION]");
            }
            AddP("[/PART 5]");
            AddP("");

            // PART 6: 16 Questions (131 - 146)
            AddP("[PART 6]", true);
            string[] p6Types = { "Notice", "Email", "Memo", "Press Release" };
            for (int g = 1; g <= 4; g++)
            {
                int startQ = 131 + (g - 1) * 4;
                int endQ = startQ + 3;
                AddP($"[GROUP {g}]", true);
                AddP($"[TITLE] Questions {startQ}-{endQ} refer to the following {p6Types[g - 1].ToLower()}.");
                AddP($"[TYPE] {p6Types[g - 1]}");
                AddP("[PASSAGE]");
                AddP($"MEMORANDUM - Meridian Logistics International\nTo: All Staff Members | From: Operations Management\nWe are delighted to announce that our regional distribution hub will begin operations next month. {startQ}. ______ the construction phase, our engineering team maintained exemplary safety protocols. Every facility has been outfitted with high-capacity sorters. {startQ + 1}. ______. In addition, all personnel are required to attend an orientation seminar. {startQ + 2}. ______ ensure compliance with global standards, attendance will be recorded digitally. Thank you for your continued dedication. {startQ + 3}. ______.");
                AddP("[/PASSAGE]");

                for (int q = startQ; q <= endQ; q++)
                {
                    AddP($"[QUESTION {q}]");
                    if (q == startQ)
                    {
                        AddP($"Stem: Chọn từ thích hợp điền vào vị trí {q}.");
                        AddP("[OPTION A] Throughout");
                        AddP("[OPTION B] Except");
                        AddP("[OPTION C] Rather than");
                        AddP("[OPTION D] In addition to");
                        AddP("[CORRECT] A");
                        AddP("[EXPLANATION] 'Throughout the construction phase' mang nghĩa 'Xuyên suốt giai đoạn thi công'.");
                    }
                    else if (q == startQ + 1)
                    {
                        AddP($"Stem: Chọn câu văn thích hợp nhất điền vào vị trí {q}.");
                        AddP("[OPTION A] These upgrades will dramatically accelerate order dispatch timelines.");
                        AddP("[OPTION B] The parking area will remain open for visitors tomorrow.");
                        AddP("[OPTION C] We regret to inform you of the unavoidable delay.");
                        AddP("[OPTION D] Please submit your reimbursement forms before Friday.");
                        AddP("[CORRECT] A");
                        AddP("[EXPLANATION] Câu văn liền trước nói về thiết bị phân loại công suất cao -> câu liên kết phù hợp nhất là tăng tốc thời gian xuất kho.");
                    }
                    else if (q == startQ + 2)
                    {
                        AddP($"Stem: Chọn từ thích hợp điền vào vị trí {q}.");
                        AddP("[OPTION A] In order to");
                        AddP("[OPTION B] As for");
                        AddP("[OPTION C] Due to");
                        AddP("[OPTION D] Because");
                        AddP("[CORRECT] A");
                        AddP("[EXPLANATION] 'In order to + V_inf' để chỉ mục đích tuân thủ quy chuẩn.");
                    }
                    else
                    {
                        AddP($"Stem: Chọn từ thích hợp điền vào vị trí {q}.");
                        AddP("[OPTION A] Sincerely");
                        AddP("[OPTION B] Cordially");
                        AddP("[OPTION C] Respectfully");
                        AddP("[OPTION D] Best regards");
                        AddP("[CORRECT] D");
                        AddP("[EXPLANATION] Lời chào kết thúc thông báo nội bộ.");
                    }
                    AddP("[/QUESTION]");
                }
                AddP("[/GROUP]");
            }
            AddP("[/PART 6]");
            AddP("");

            // PART 7: 54 Questions (147 - 200)
            AddP("[PART 7]", true);
            AddP("[GROUP 1]", true);
            AddP("[TITLE] Questions 147-148 refer to the following advertisement.");
            AddP("[TYPE] Advertisement");
            AddP("[PASSAGE]");
            AddP("EXCEL PEAK CATERING SERVICES\nPlanning a corporate luncheon, gala banquet, or executive symposium?\nExcel Peak provides artisanal dining experiences tailored to professional events. Our packages include premium menu customization, certified service personnel, and complete tableware setup. Reserve before November 1st to receive a 15% discount on all culinary selections.");
            AddP("[/PASSAGE]");
            AddP("[QUESTION 147]");
            AddP("Stem: What is offered in the advertisement?");
            AddP("[OPTION A] Professional event catering services");
            AddP("[OPTION B] Commercial restaurant equipment");
            AddP("[OPTION C] Culinary arts classes for executives");
            AddP("[OPTION D] Venue leasing for private parties");
            AddP("[CORRECT] A");
            AddP("[EXPLANATION] Bài quảng cáo giới thiệu dịch vụ cung cấp tiệc cho doanh nghiệp (Catering services).");
            AddP("[/QUESTION]");
            AddP("[QUESTION 148]");
            AddP("Stem: How can customers qualify for a special promotion?");
            AddP("[OPTION A] By booking a reservation prior to November 1");
            AddP("[OPTION B] By ordering a minimum of 200 meals");
            AddP("[OPTION C] By paying the invoice in cash");
            AddP("[OPTION D] By subscribing to an annual plan");
            AddP("[CORRECT] A");
            AddP("[EXPLANATION] 'Reserve before November 1st to receive a 15% discount'.");
            AddP("[/QUESTION]");
            AddP("[/GROUP]");

            int currentQ = 149;
            int[] groupSizes = { 2, 2, 3, 3, 2, 4, 4, 3, 4, 5, 5, 5, 5, 5 };
            string[] docTypes = {
                "Notice", "Text Message Chain", "Email", "Schedule", "Press Release",
                "Article", "Online Form", "Policy Memo", "Product Review",
                "Email and Schedule (Double)", "Article and Feedback (Double)",
                "Email, Order Form and Notice (Triple)", "Memo, Invoice and Policy (Triple)", "Brochure, Survey and Letter (Triple)"
            };

            for (int gi = 0; gi < groupSizes.Length; gi++)
            {
                int groupNum = gi + 2;
                int size = groupSizes[gi];
                int endQ = currentQ + size - 1;
                string type = docTypes[gi];

                AddP($"[GROUP {groupNum}]", true);
                AddP($"[TITLE] Questions {currentQ}-{endQ} refer to the following {type.ToLower()}.");
                AddP($"[TYPE] {type}");
                AddP("[PASSAGE]");
                AddP($"DOCUMENT CONTEXT: {type.ToUpper()}\nOrganization: Meridian Global Partners\nRegarding business procedures and strategic operational updates. All relevant stakeholders must examine the criteria outlined below. Compliance with safety standards and international regulations is strictly observed across all administrative departments. Any inquiries regarding this correspondence should be addressed to the department head.");
                AddP("[/PASSAGE]");

                for (int q = currentQ; q <= endQ; q++)
                {
                    AddP($"[QUESTION {q}]");
                    AddP($"Stem: According to the document, what is stated regarding question #{q}?");
                    AddP("[OPTION A] Procedures must be approved by the operational committee.");
                    AddP("[OPTION B] Additional documentation is required for registration.");
                    AddP("[OPTION C] Deadlines have been extended for international participants.");
                    AddP("[OPTION D] Inspection reports will be published next quarter.");
                    AddP("[CORRECT] A");
                    AddP($"[EXPLANATION] Căn cứ theo văn bản hướng dẫn: 'Procedures must be approved by the operational committee' -> Đáp án A.");
                    AddP("[/QUESTION]");
                }
                AddP("[/GROUP]");

                currentQ += size;
            }

            AddP("[/PART 7]");

            mainPart.Document.Save();
            wordDoc.Save();
        }

        return memStream.ToArray();
    }

    #endregion
}
