import 'package:flutter/material.dart';
import '../models/toeic_models.dart';
import 'exam_list_screen.dart';

class ExamResultScreen extends StatefulWidget {
  final ExamResultData result;

  const ExamResultScreen({super.key, required this.result});

  @override
  State<ExamResultScreen> createState() => _ExamResultScreenState();
}

class _ExamResultScreenState extends State<ExamResultScreen> {
  String _activeFilter = 'ALL'; // 'ALL', 'CORRECT', 'WRONG', 'SKIPPED'

  List<QuestionReviewItem> get _filteredReviews {
    final list = widget.result.questionReviews;
    if (_activeFilter == 'CORRECT') {
      return list.where((r) => r.isCorrect).toList();
    } else if (_activeFilter == 'WRONG') {
      return list.where((r) => !r.isCorrect && !r.isSkipped).toList();
    } else if (_activeFilter == 'SKIPPED') {
      return list.where((r) => r.isSkipped).toList();
    }
    return list;
  }

  String _formatDuration(int seconds) {
    final m = seconds ~/ 60;
    final s = seconds % 60;
    return '$m phút ${s.toString().padLeft(2, '0')} giây';
  }

  @override
  Widget build(BuildContext context) {
    final r = widget.result;

    return PopScope(
      canPop: false,
      onPopInvokedWithResult: (didPop, result) {
        if (didPop) return;
        Navigator.pushAndRemoveUntil(
          context,
          MaterialPageRoute(builder: (_) => const ExamListScreen()),
          (route) => false,
        );
      },
      child: Scaffold(
        backgroundColor: const Color(0xFFF8F9FF),
        appBar: AppBar(
          backgroundColor: Colors.white,
          elevation: 0.5,
          leading: IconButton(
            icon: const Icon(Icons.home, color: Color(0xFF001631)),
            onPressed: () {
              Navigator.pushAndRemoveUntil(
                context,
                MaterialPageRoute(builder: (_) => const ExamListScreen()),
                (route) => false,
              );
            },
          ),
          title: const Text(
            'Kết Quả Thi TOEIC Reading',
            style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold, color: Color(0xFF001631)),
          ),
        ),
        body: ListView(
          padding: const EdgeInsets.all(16),
          children: [
            // SCORE OVERVIEW CARD
            Container(
              padding: const EdgeInsets.all(20),
              decoration: BoxDecoration(
                gradient: const LinearGradient(
                  colors: [Color(0xFF001631), Color(0xFF142B49)],
                  begin: Alignment.topLeft,
                  end: Alignment.bottomRight,
                ),
                borderRadius: BorderRadius.circular(16),
                boxShadow: [
                  BoxShadow(
                    color: const Color(0xFF001631).withValues(alpha: 0.2),
                    blurRadius: 10,
                    offset: const Offset(0, 4),
                  ),
                ],
              ),
              child: Column(
                children: [
                  Text(
                    r.examTitle,
                    style: const TextStyle(color: Colors.white70, fontSize: 13),
                    textAlign: TextAlign.center,
                  ),
                  const SizedBox(height: 6),
                  Text(
                    'Thí sinh: ${r.studentName} (${r.studentCode})',
                    style: const TextStyle(color: Colors.white, fontSize: 15, fontWeight: FontWeight.bold),
                  ),
                  const SizedBox(height: 18),

                  // ACCURACY CIRCLE
                  Row(
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [
                      Container(
                        padding: const EdgeInsets.all(16),
                        decoration: BoxDecoration(
                          shape: BoxShape.circle,
                          color: Colors.white.withValues(alpha: 0.1),
                          border: Border.all(color: const Color(0xFF0051D5), width: 3),
                        ),
                        child: Column(
                          children: [
                            Text(
                              '${r.accuracyPercentage.toStringAsFixed(1)}%',
                              style: const TextStyle(
                                fontSize: 28,
                                fontWeight: FontWeight.bold,
                                color: Colors.white,
                              ),
                            ),
                            const Text(
                              'Độ chính xác',
                              style: TextStyle(fontSize: 11, color: Colors.white70),
                            ),
                          ],
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 18),

                  // 4 METRICS ROW
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceAround,
                    children: [
                      _buildSummaryMetric('${r.correctCount}/100', 'Số câu đúng', const Color(0xFF10B981)),
                      _buildSummaryMetric('${r.wrongCount}', 'Số câu sai', Colors.redAccent),
                      _buildSummaryMetric('${r.skippedCount}', 'Bỏ qua', Colors.amberAccent),
                      _buildSummaryMetric(_formatDuration(r.timeSpentSeconds), 'Thời gian', Colors.white70, isSmall: true),
                    ],
                  ),
                ],
              ),
            ),
            const SizedBox(height: 16),

            // 3-PART BREAKDOWN CARD
            Container(
              padding: const EdgeInsets.all(16),
              decoration: BoxDecoration(
                color: Colors.white,
                borderRadius: BorderRadius.circular(14),
                border: Border.all(color: const Color(0xFFE2E8F0)),
              ),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  const Text(
                    'Phân tích chi tiết 3 Phần thi (Breakdown)',
                    style: TextStyle(fontSize: 15, fontWeight: FontWeight.bold, color: Color(0xFF001631)),
                  ),
                  const SizedBox(height: 14),
                  _buildPartProgress('Part 5: Hoàn thành câu (Incomplete)', r.part5Correct, r.part5Total, const Color(0xFF0284C7)),
                  const SizedBox(height: 10),
                  _buildPartProgress('Part 6: Điền đoạn văn (Text Completion)', r.part6Correct, r.part6Total, const Color(0xFF7C3AED)),
                  const SizedBox(height: 10),
                  _buildPartProgress('Part 7: Đọc hiểu văn bản (Comprehension)', r.part7Correct, r.part7Total, const Color(0xFFD97706)),
                ],
              ),
            ),
            const SizedBox(height: 16),

            // REVIEW SECTION HEADER & FILTER TABS
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                const Text(
                  'Xem lại bài làm & Lời giải',
                  style: TextStyle(fontSize: 15, fontWeight: FontWeight.bold, color: Color(0xFF001631)),
                ),
                Text(
                  '${_filteredReviews.length} câu',
                  style: const TextStyle(fontSize: 12, color: Colors.grey),
                ),
              ],
            ),
            const SizedBox(height: 10),

            // FILTER CHIPS
            SingleChildScrollView(
              scrollDirection: Axis.horizontal,
              child: Row(
                children: [
                  _buildFilterChip('ALL', 'Tất cả (100)'),
                  const SizedBox(width: 8),
                  _buildFilterChip('CORRECT', 'Đúng (${r.correctCount})', activeColor: const Color(0xFF10B981)),
                  const SizedBox(width: 8),
                  _buildFilterChip('WRONG', 'Sai (${r.wrongCount})', activeColor: Colors.redAccent),
                  const SizedBox(width: 8),
                  _buildFilterChip('SKIPPED', 'Chưa làm (${r.skippedCount})', activeColor: Colors.grey),
                ],
              ),
            ),
            const SizedBox(height: 14),

            // QUESTION REVIEW LIST
            ..._filteredReviews.map((item) => _buildReviewCard(item)),

            const SizedBox(height: 20),
            SizedBox(
              width: double.infinity,
              height: 48,
              child: ElevatedButton.icon(
                style: ElevatedButton.styleFrom(
                  backgroundColor: const Color(0xFF001631),
                  foregroundColor: Colors.white,
                  shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
                ),
                onPressed: () {
                  Navigator.pushAndRemoveUntil(
                    context,
                    MaterialPageRoute(builder: (_) => const ExamListScreen()),
                    (route) => false,
                  );
                },
                icon: const Icon(Icons.refresh),
                label: const Text('LÀM ĐỀ THI KHÁC', style: TextStyle(fontWeight: FontWeight.bold)),
              ),
            ),
            const SizedBox(height: 30),
          ],
        ),
      ),
    );
  }

  Widget _buildSummaryMetric(String value, String label, Color color, {bool isSmall = false}) {
    return Column(
      children: [
        Text(
          value,
          style: TextStyle(
            fontSize: isSmall ? 13 : 18,
            fontWeight: FontWeight.bold,
            color: color,
          ),
        ),
        const SizedBox(height: 2),
        Text(
          label,
          style: const TextStyle(fontSize: 11, color: Colors.white70),
        ),
      ],
    );
  }

  Widget _buildPartProgress(String title, int correct, int total, Color color) {
    final double pct = total > 0 ? (correct / total) : 0.0;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Row(
          mainAxisAlignment: MainAxisAlignment.spaceBetween,
          children: [
            Text(title, style: const TextStyle(fontSize: 12.5, fontWeight: FontWeight.w600, color: Colors.black87)),
            Text('$correct / $total', style: TextStyle(fontSize: 12.5, fontWeight: FontWeight.bold, color: color)),
          ],
        ),
        const SizedBox(height: 6),
        ClipRRect(
          borderRadius: BorderRadius.circular(4),
          child: LinearProgressIndicator(
            value: pct,
            backgroundColor: const Color(0xFFF1F5F9),
            valueColor: AlwaysStoppedAnimation<Color>(color),
            minHeight: 6,
          ),
        ),
      ],
    );
  }

  Widget _buildFilterChip(String key, String label, {Color? activeColor}) {
    final isSelected = _activeFilter == key;
    return InkWell(
      onTap: () => setState(() => _activeFilter = key),
      child: Container(
        padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
        decoration: BoxDecoration(
          color: isSelected ? (activeColor ?? const Color(0xFF001631)) : Colors.white,
          borderRadius: BorderRadius.circular(20),
          border: Border.all(color: isSelected ? Colors.transparent : const Color(0xFFE2E8F0)),
        ),
        child: Text(
          label,
          style: TextStyle(
            fontSize: 12,
            fontWeight: FontWeight.bold,
            color: isSelected ? Colors.white : Colors.black87,
          ),
        ),
      ),
    );
  }

  Widget _buildReviewCard(QuestionReviewItem q) {
    Color statusBg = Colors.grey.shade100;
    Color statusText = Colors.grey.shade700;
    String statusLabel = 'CHƯA LÀM';

    if (q.isCorrect) {
      statusBg = const Color(0xFFECFDF5);
      statusText = const Color(0xFF059669);
      statusLabel = 'ĐÚNG';
    } else if (!q.isSkipped) {
      statusBg = const Color(0xFFFEF2F2);
      statusText = const Color(0xFFDC2626);
      statusLabel = 'SAI';
    }

    return Container(
      margin: const EdgeInsets.only(bottom: 14),
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: const Color(0xFFE2E8F0)),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          // HEADER ROW
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Row(
                children: [
                  Container(
                    padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
                    decoration: BoxDecoration(
                      color: const Color(0xFF001631),
                      borderRadius: BorderRadius.circular(6),
                    ),
                    child: Text(
                      'Câu ${q.questionNumber}',
                      style: const TextStyle(color: Colors.white, fontWeight: FontWeight.bold, fontSize: 12),
                    ),
                  ),
                  const SizedBox(width: 6),
                  Text('Part ${q.part}', style: const TextStyle(fontSize: 12, color: Colors.grey)),
                ],
              ),
              Container(
                padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
                decoration: BoxDecoration(
                  color: statusBg,
                  borderRadius: BorderRadius.circular(4),
                ),
                child: Text(
                  statusLabel,
                  style: TextStyle(color: statusText, fontWeight: FontWeight.bold, fontSize: 11),
                ),
              ),
            ],
          ),
          const SizedBox(height: 10),

          // PASSAGE IF PRESENT
          if (q.passageText != null && q.passageText!.isNotEmpty) ...[
            Container(
              padding: const EdgeInsets.all(10),
              decoration: BoxDecoration(
                color: const Color(0xFFF8FAFC),
                borderRadius: BorderRadius.circular(8),
                border: Border.all(color: const Color(0xFFE2E8F0)),
              ),
              child: Text(
                q.passageText!,
                style: const TextStyle(fontSize: 12, color: Colors.black87, height: 1.5),
                maxLines: 4,
                overflow: TextOverflow.ellipsis,
              ),
            ),
            const SizedBox(height: 10),
          ],

          // STEM
          Text(
            q.stem,
            style: const TextStyle(fontSize: 14, fontWeight: FontWeight.w600, color: Color(0xFF0D1C2E)),
          ),
          const SizedBox(height: 12),

          // 4 OPTIONS REVIEW
          ...q.options.map((opt) {
            final isCorrectOpt = opt.optionKey == q.correctOption;
            final isStudentPick = opt.optionKey == q.selectedOption;

            Color optBg = Colors.white;
            Color optBorder = const Color(0xFFE2E8F0);
            Color optKeyBg = const Color(0xFFF1F5F9);
            Color optKeyText = Colors.black87;

            if (isCorrectOpt) {
              optBg = const Color(0xFFECFDF5);
              optBorder = const Color(0xFF10B981);
              optKeyBg = const Color(0xFF10B981);
              optKeyText = Colors.white;
            } else if (isStudentPick && !q.isCorrect) {
              optBg = const Color(0xFFFEF2F2);
              optBorder = const Color(0xFFEF4444);
              optKeyBg = const Color(0xFFEF4444);
              optKeyText = Colors.white;
            }

            return Container(
              margin: const EdgeInsets.only(bottom: 6),
              padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 8),
              decoration: BoxDecoration(
                color: optBg,
                borderRadius: BorderRadius.circular(8),
                border: Border.all(color: optBorder),
              ),
              child: Row(
                children: [
                  Container(
                    width: 22,
                    height: 22,
                    decoration: BoxDecoration(
                      shape: BoxShape.circle,
                      color: optKeyBg,
                    ),
                    child: Center(
                      child: Text(
                        opt.optionKey,
                        style: TextStyle(fontSize: 11, fontWeight: FontWeight.bold, color: optKeyText),
                      ),
                    ),
                  ),
                  const SizedBox(width: 8),
                  Expanded(
                    child: Text(
                      opt.content,
                      style: TextStyle(
                        fontSize: 13,
                        fontWeight: isCorrectOpt || isStudentPick ? FontWeight.bold : FontWeight.normal,
                        color: isCorrectOpt ? const Color(0xFF065F46) : (isStudentPick ? const Color(0xFF991B1B) : Colors.black87),
                      ),
                    ),
                  ),
                  if (isCorrectOpt)
                    const Icon(Icons.check_circle, size: 16, color: Color(0xFF10B981))
                  else if (isStudentPick)
                    const Icon(Icons.cancel, size: 16, color: Colors.redAccent),
                ],
              ),
            );
          }),

          // EXPLANATION BOX
          if (q.explanation != null && q.explanation!.trim().isNotEmpty) ...[
            const SizedBox(height: 10),
            Container(
              padding: const EdgeInsets.all(10),
              decoration: BoxDecoration(
                color: const Color(0xFFEFF6FF),
                borderRadius: BorderRadius.circular(8),
                border: Border.all(color: const Color(0xFFBFDBFE)),
              ),
              child: Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  const Icon(Icons.lightbulb_outline, size: 16, color: Color(0xFF1D4ED8)),
                  const SizedBox(width: 6),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        const Text(
                          'Giải thích chi tiết:',
                          style: TextStyle(fontSize: 11, fontWeight: FontWeight.bold, color: Color(0xFF1D4ED8)),
                        ),
                        const SizedBox(height: 2),
                        Text(
                          q.explanation!,
                          style: const TextStyle(fontSize: 12, color: Color(0xFF1E3A8A), height: 1.4),
                        ),
                      ],
                    ),
                  ),
                ],
              ),
            ),
          ],
        ],
      ),
    );
  }
}
