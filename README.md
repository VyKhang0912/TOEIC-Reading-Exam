# ĐỒ ÁN GIỮA KỲ: HỆ THỐNG THI THỬ TOEIC READING TRỰC TUYẾN
**Hệ thống Khảo thí Đọc hiểu Chuẩn ETS 2024 (Part 5, Part 6, Part 7 — 100 câu / 75 phút)**

---

## I. TỔNG QUAN HỆ THỐNG & CÔNG NGHỆ SỬ DỤNG
Hệ thống được phát triển theo kiến trúc chuẩn Enterprise với 2 phân hệ độc lập kết nối qua RESTful API:
1. **Backend & Admin Web (Giảng viên / Quản trị viên)**:
   - Framework: **ASP.NET Boilerplate 10.2.0 (ABP)** kiến trúc DDD (Domain-Driven Design).
   - Nền tảng: **ASP.NET Core MVC + jQuery** (Target `net9.0`).
   - ORM & CSDL: **Entity Framework Core 9.0.5** trên **Microsoft SQL Server 2022** (`ToeicExamDb`).
   - Xử lý file Word: **DocumentFormat.OpenXml 3.5.1** với parser thẻ tag và cơ chế Atomic All-or-Nothing Transaction.
   - Quản trị bảo mật: Giữ nguyên hệ thống Role/Permission/Tenancy của ABP, tích hợp chế độ demo an toàn.

2. **Frontend Mobile (Học viên thi thử)**:
   - Framework: **Flutter 3.47.4 • Dart 3.13.3**.
   - Hỗ trợ đa nền tảng: **Android Mobile, Windows Desktop, Web Browser**.
   - Thiết kế giao diện: Hiện thực theo sát 100% thiết kế từ **Google Stitch MCP**.

---

## II. THIẾT KẾ GIAO DIỆN TỪ GOOGLE STITCH MCP
Toàn bộ 5 giao diện của hệ thống được lấy trực tiếp từ dự án Google Stitch:
- **Project URL**: [https://stitch.withgoogle.com/projects/8946255732279470606?pli=1](https://stitch.withgoogle.com/projects/8946255732279470606?pli=1)
- **ID Dự án**: `projects/8946255732279470606`

### Danh sách 5 màn hình chính:
1. **ADMIN 1 — Quản lý Đề thi TOEIC Reading + Import DOCX**:
   - Stitch Screen ID: `8c5c762543a246a7979f1ba4b5ec7e38`
   - Đường dẫn trên Web: `https://localhost:44312/ToeicAdmin`
   - Chức năng: Thống kê số lượng đề, số câu, lượt thi; bảng danh sách đề thi; tìm kiếm đề; tải file mẫu Word chuẩn; Modal kéo thả upload DOCX kiểm tra lỗi và import nguyên khối.

2. **ADMIN 2 — Soạn thảo & Chỉnh sửa Đề thi TOEIC Workspace**:
   - Stitch Screen ID: `9a2b631296d04e2c9fc99662a73ae390`
   - Đường dẫn trên Web: `https://localhost:44312/ToeicAdmin/Edit/{id}`
   - Chức năng: Workspace 3 cột (Cột 1: Lưới 100 câu Part 5/6/7; Cột 2: Đoạn văn bài đọc Passage formatted; Cột 3: Trình soạn thảo câu hỏi Stem, 4 phương án A/B/C/D, đáp án đúng và Lời giải thích); xuất bản / hủy xuất bản đề.

3. **MOBILE 1 — Chọn Đề thi TOEIC Reading (Student Mobile)**:
   - Stitch Screen ID: `9ba683831ec74277938b404396b109be`
   - Đường dẫn source: `Frontend/toeic_mobile/lib/screens/exam_list_screen.dart`
   - Chức năng: Hero banner TOEIC ETS 2024, tải danh sách đề thi đã xuất bản từ API thật, hộp thoại quy chế phòng thi, nhập tên thí sinh và bắt đầu làm bài.

4. **MOBILE 2 — Phòng thi Trực tuyến TOEIC Reading (Mobile)**:
   - Stitch Screen ID: `483063f18f9e4f86ab5a390c456eed4c`
   - Đường dẫn source: `Frontend/toeic_mobile/lib/screens/exam_room_screen.dart`
   - Chức năng: Làm bài đúng 100 câu trong 75 phút; đồng hồ đếm ngược đồng bộ máy chủ; cảnh báo đỏ khi còn < 5 phút; đọc bài đọc và xem ảnh; chọn/đổi/bỏ chọn đáp án; đánh dấu câu (Flag); mở BottomSheet lưới 100 câu; tự động lưu tiến độ lên máy chủ; tự động thu bài khi hết 75 phút; xác nhận nộp bài hiển thị rõ số câu đã làm và chưa làm.

5. **MOBILE 3 — Kết quả Thi & Xem lại Đáp án (Mobile)**:
   - Stitch Screen ID: `282e065463924e7bbe24347354580ffe`
   - Đường dẫn source: `Frontend/toeic_mobile/lib/screens/exam_result_screen.dart`
   - Chức năng: Thẻ điểm hiển thị phần trăm chính xác, số câu đúng/sai/bỏ qua, thời gian làm bài; phân tích chi tiết 3 phần (Part 5: /30, Part 6: /16, Part 7: /54); bộ lọc xem câu đúng/sai/chưa làm; hiển thị đáp án học viên vs đáp án đúng chuẩn cùng lời giải thích chi tiết.

---

## III. QUY CÁCH BỘ TAGS WORD (.DOCX) & VALIDATION NGUYÊN KHỐI
Hệ thống sử dụng bộ parser OpenXML độc quyền xử lý định dạng cấu trúc file Word chuẩn:

### 1. Cấu trúc bộ Tags:
```text
[EXAM]
Title: ETS 2024 TOEIC Reading Practice Test 01
Code: ETS-2024-TEST-01
Description: Standard 100-Question TOEIC Reading Exam
Duration: 75
[/EXAM]

[PART 5]
[QUESTION 101]
Stem: The board of directors announced that the new policy will make our logistics services more ______ for all international partners.
[OPTION A] convenient
[OPTION B] conveniently
[OPTION C] convenience
[OPTION D] convenientness
[CORRECT] A
[EXPLANATION] Vị trí sau 'more' cần một tính từ bổ nghĩa cho tân ngữ -> chọn đáp án A (convenient).
[/QUESTION]
... (30 câu từ 101 đến 130)
[/PART 5]

[PART 6]
[GROUP 1]
[TITLE] Questions 131-134 refer to the following memorandum.
[TYPE] Memorandum
[PASSAGE]
MEMORANDUM - Meridian Logistics International
To: All Staff Members | From: Operations Management
We are delighted to announce that our regional distribution hub will begin operations next month. 131. ______ the construction phase, our engineering team maintained exemplary safety protocols...
[/PASSAGE]
[QUESTION 131]
Stem: Chọn từ thích hợp điền vào vị trí 131.
[OPTION A] Throughout
[OPTION B] Except
[OPTION C] Rather than
[OPTION D] In addition to
[CORRECT] A
[EXPLANATION] 'Throughout the construction phase' mang nghĩa 'Xuyên suốt giai đoạn thi công'.
[/QUESTION]
... (4 nhóm x 4 câu = 16 câu từ 131 đến 146)
[/GROUP]
[/PART 6]

[PART 7]
[GROUP 1]
[TITLE] Questions 147-148 refer to the following email.
[TYPE] Email
[PASSAGE]
Nội dung bài đọc đoạn văn đơn/kép/ba...
[/PASSAGE]
[QUESTION 147]
...
[/QUESTION]
[/GROUP]
... (54 câu từ 147 đến 200)
[/PART 7]
```

### 2. Quy tắc Kiểm tra Toàn vẹn (Atomic Validation):
- Kiểm tra số lượng: Đúng đủ **100 câu hỏi** đánh số liên tục từ **101 đến 200**.
- Cấu trúc Part: Part 5 đúng 30 câu (101-130), Part 6 đúng 16 câu (131-146), Part 7 đúng 54 câu (147-200).
- Mỗi câu phải có đủ 4 phương án A, B, C, D và đúng 1 đáp án `[CORRECT]`.
- Nhóm câu Part 6 và Part 7 bắt buộc phải có bài đọc `[PASSAGE]` tương ứng.
- **Rollback toàn diện**: Nếu phát hiện dù chỉ 1 lỗi, hệ thống hủy bỏ (Rollback) toàn bộ giao dịch, không lưu dữ liệu rác vào CSDL và trả về danh sách vị trí lỗi cụ thể để Giảng viên chỉnh sửa.

---

## IV. CẤU TRÚC CƠ SỞ DỮ LIỆU SQL SERVER (`ToeicExamDb`)
Các thực thể TOEIC được tích hợp vào `MyProjectDbContext`:
1. `ToeicExams`: Lưu thông tin đề thi, thời gian 75 phút, tổng 100 câu, trạng thái xuất bản.
2. `ToeicQuestionGroups`: Lưu các nhóm bài đọc Part 6 & Part 7 (tiêu đề, phân loại, nội dung văn bản, đường dẫn hình ảnh đính kèm).
3. `ToeicQuestions`: Lưu câu hỏi (101 - 200), thuộc Part nào, nội dung câu hỏi, đáp án đúng (A/B/C/D), lời giải thích. Có Unique Index `(ExamId, QuestionNumber)` chống trùng câu.
4. `ToeicAnswerOptions`: Lưu 4 phương án A, B, C, D cho từng câu hỏi.
5. `ToeicExamAttempts`: Lưu lượt thi của từng học viên (mã định danh Guid, thời gian bắt đầu, hạn nộp 75 phút, thời gian nộp bài, điểm số Part 5/6/7, độ chính xác %).
6. `ToeicAttemptAnswers`: Lưu từng lựa chọn của học viên cho 100 câu (A/B/C/D hoặc null nếu bỏ qua), trạng thái cờ đánh dấu (Flag), kết quả đúng/sai.

---

## V. HƯỚNG DẪN KHỞI CHẠY HỆ THỐNG

### 1. Khởi chạy Backend & Admin Web:
```powershell
# Chuyển đến thư mục Web.Mvc
cd D:\TOEIC-Reading-Exam\Backend\10.2.0\aspnet-core\src\MyCompany.MyProject.Web.Mvc

# Chạy ứng dụng ASP.NET Boilerplate MVC
dotnet run
```
- Truy cập Admin Web tại: **[https://localhost:44312/ToeicAdmin](https://localhost:44312/ToeicAdmin)**
- Tài khoản quản trị mặc định: `admin` / `123qwe`

### 2. Khởi chạy Mobile Flutter App:
```powershell
# Chuyển đến thư mục Flutter
cd D:\TOEIC-Reading-Exam\Frontend\toeic_mobile

# Chạy ứng dụng trên Windows Desktop
flutter run -d windows

# Hoặc chạy trên Trình duyệt Web
flutter run -d chrome

# Hoặc chạy trên Android Emulator (Đảm bảo đã mở giả lập)
flutter run -d android
```

*Lưu ý khi thi trên các thiết bị khác nhau:*
- Khi chạy trên Windows Desktop / Web Chrome: API mặc định kết nối tới `https://localhost:44312/api/toeic`.
- Khi chạy trên Android Emulator: Vào biểu tượng Cài đặt trên góc AppBar và chọn `https://10.0.2.2:44312/api/toeic`.
- Khi chạy trên Điện thoại thật: Nhập địa chỉ IP mạng LAN của máy tính chạy backend (VD: `https://192.168.1.x:44312/api/toeic`).

---

## VI. CÁC BƯỚC THÍ SINH CẦN TỰ LÀM ĐỂ NỘP BÀI GIỮA KỲ
1. **Quay Video Demo Thực tế**:
   - Sử dụng phần mềm quay màn hình (OBS Studio hoặc Xbox Game Bar trên Windows: `Win + Alt + R`).
   - Quay trọn vẹn luồng hoạt động:
     - *Video 1:* Vào Admin Web, tải file mẫu `.docx`, import file Word, xem thông báo thành công và chuyển sang giao diện soạn thảo 3 cột.
     - *Video 2:* Thao tác chỉnh sửa câu hỏi, duyệt qua các Part 5, 6, 7 và xuất bản đề thi.
     - *Video 3:* Mở Flutter App, chọn đề thi, vào phòng thi 75 phút, làm bài Part 5/6/7, mở lưới 100 câu, nộp bài và xem bảng điểm + lời giải chi tiết.
   - Lưu các file video vào thư mục: `D:\TOEIC-Reading-Exam\videos\`.

2. **Kiểm tra File DOCX đề thi**:
   - Có thể tải file chuẩn được hệ thống sinh sẵn tại `D:\TOEIC-Reading-Exam\standard_template.docx` hoặc nhấn nút "Tải mẫu file chuẩn (.docx)" trên giao diện Admin.

3. **Nộp bài lên Giảng viên**:
   - Nén mã nguồn hoặc đẩy lên Git theo hướng dẫn riêng của Giảng viên môn học.
   - Không đưa mật khẩu nhạy cảm hoặc file rác vào gói nộp.
