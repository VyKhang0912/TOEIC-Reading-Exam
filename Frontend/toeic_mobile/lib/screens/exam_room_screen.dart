import 'dart:async';
import 'package:flutter/material.dart';
import '../models/toeic_models.dart';
import '../services/api_service.dart';
import 'exam_result_screen.dart';

class ExamRoomScreen extends StatefulWidget {
  final AttemptSession session;
  final List<AttemptQuestion> questions;

  const ExamRoomScreen({
    super.key,
    required this.session,
    required this.questions,
  });

  @override
  State<ExamRoomScreen> createState() => _ExamRoomScreenState();
}

class _ExamRoomScreenState extends State<ExamRoomScreen> {
  final ApiService _apiService = ApiService.instance;
  late int _remainingSeconds;
  Timer? _timer;
  int _currentIndex = 0; // 0 to 99 (Question 101 to 200)
  bool _isSubmitting = false;

  @override
  void initState() {
    super.initState();
    _remainingSeconds = widget.session.remainingSeconds;
    _startTimer();
  }

  @override
  void dispose() {
    _timer?.cancel();
    super.dispose();
  }

  void _startTimer() {
    _timer = Timer.periodic(const Duration(seconds: 1), (timer) {
      if (_remainingSeconds > 0) {
        setState(() {
          _remainingSeconds--;
        });
      } else {
        timer.cancel();
        _autoSubmitOnTimeUp();
      }
    });
  }

  void _autoSubmitOnTimeUp() {
    ScaffoldMessenger.of(context).showSnackBar(
      const SnackBar(
        content: Text('Hết 75 phút làm bài! Hệ thống đang tự động thu bài...'),
        backgroundColor: Colors.orange,
        duration: Duration(seconds: 4),
      ),
    );
    _performSubmit();
  }

  String _formatTimer(int seconds) {
    final m = seconds ~/ 60;
    final s = seconds % 60;
    return '${m.toString().padLeft(2, '0')}:${s.toString().padLeft(2, '0')}';
  }

  AttemptQuestion get _currentQuestion => widget.questions[_currentIndex];

  bool _isQuestionAnswered(int index) {
    if (index < 0 || index >= widget.questions.length) return false;
    final opt = widget.questions[index].selectedOption;
    return opt != null && opt.trim().isNotEmpty;
  }

  bool _canAccessQuestion(int targetIndex) {
    if (targetIndex < 0 || targetIndex >= widget.questions.length) return false;
    // Sequential requirement: all preceding questions must be answered
    for (int i = 0; i < targetIndex; i++) {
      if (!_isQuestionAnswered(i)) {
        return false;
      }
    }
    return true;
  }

  void _showRequirementAlert([String? message]) {
    ScaffoldMessenger.of(context).clearSnackBars();
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: Text(message ?? 'Vui lòng chọn đáp án trước khi tiếp tục.'),
        backgroundColor: Colors.orange.shade900,
        duration: const Duration(seconds: 2),
      ),
    );
  }

  void _selectOption(String key) {
    final q = _currentQuestion;
    setState(() {
      q.selectedOption = key; // Select or change option
    });

    // Save answer to server in background
    _apiService.saveAnswer(widget.session.attemptId, q.questionId, q.selectedOption, q.isFlagged);
  }

  void _toggleFlag() {
    final q = _currentQuestion;
    setState(() {
      q.isFlagged = !q.isFlagged;
    });
    _apiService.saveAnswer(widget.session.attemptId, q.questionId, q.selectedOption, q.isFlagged);
  }

  bool _jumpToQuestion(int index) {
    if (index < 0 || index >= widget.questions.length) return false;

    if (index > _currentIndex) {
      if (!_isQuestionAnswered(_currentIndex)) {
        _showRequirementAlert('Vui lòng chọn đáp án trước khi tiếp tục.');
        return false;
      }
      if (!_canAccessQuestion(index)) {
        _showRequirementAlert('Vui lòng chọn đáp án trước khi tiếp tục.');
        return false;
      }
    }

    setState(() {
      _currentIndex = index;
    });
    return true;
  }

  void _onNextPressed() {
    if (!_isQuestionAnswered(_currentIndex)) {
      _showRequirementAlert('Vui lòng chọn đáp án trước khi tiếp tục.');
      return;
    }
    _jumpToQuestion(_currentIndex + 1);
  }

  void _confirmSubmit() {
    final answeredCount = widget.questions.where((q) => q.selectedOption != null && q.selectedOption!.isNotEmpty).length;
    final unansweredCount = widget.questions.length - answeredCount;

    showDialog(
      context: context,
      builder: (ctx) => AlertDialog(
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
        title: const Row(
          children: [
            Icon(Icons.assignment_turned_in, color: Color(0xFF001631)),
            SizedBox(width: 8),
            Text('Xác nhận Nộp bài thi', style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
          ],
        ),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text('Thời gian còn lại: ${_formatTimer(_remainingSeconds)}'),
            const SizedBox(height: 12),
            Container(
              padding: const EdgeInsets.all(12),
              decoration: BoxDecoration(
                color: const Color(0xFFF8F9FF),
                borderRadius: BorderRadius.circular(10),
                border: Border.all(color: const Color(0xFFE2E8F0)),
              ),
              child: Row(
                mainAxisAlignment: MainAxisAlignment.spaceAround,
                children: [
                  Column(
                    children: [
                      Text('$answeredCount', style: const TextStyle(fontSize: 20, fontWeight: FontWeight.bold, color: Colors.green)),
                      const Text('Đã làm', style: TextStyle(fontSize: 12, color: Colors.grey)),
                    ],
                  ),
                  Container(width: 1, height: 32, color: Colors.grey.shade300),
                  Column(
                    children: [
                      Text('$unansweredCount', style: TextStyle(fontSize: 20, fontWeight: FontWeight.bold, color: unansweredCount > 0 ? Colors.orange : Colors.grey)),
                      const Text('Chưa làm', style: TextStyle(fontSize: 12, color: Colors.grey)),
                    ],
                  ),
                ],
              ),
            ),
            const SizedBox(height: 12),
            if (unansweredCount > 0)
              Text(
                'Chú ý: Bạn còn $unansweredCount câu chưa chọn đáp án. Bạn có chắc chắn muốn nộp ngay?',
                style: const TextStyle(fontSize: 12, color: Colors.redAccent),
              )
            else
              const Text(
                'Bạn đã hoàn thành đủ 100 câu! Hệ thống sẽ tiến hành chấm điểm ngay sau khi nộp.',
                style: TextStyle(fontSize: 12, color: Colors.black87),
              ),
          ],
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx),
            child: const Text('Tiếp tục làm bài'),
          ),
          ElevatedButton(
            style: ElevatedButton.styleFrom(
              backgroundColor: const Color(0xFF001631),
              foregroundColor: Colors.white,
            ),
            onPressed: () {
              Navigator.pop(ctx);
              _performSubmit();
            },
            child: const Text('Nộp bài ngay'),
          ),
        ],
      ),
    );
  }

  Future<void> _performSubmit() async {
    if (_isSubmitting) return;
    setState(() => _isSubmitting = true);
    _timer?.cancel();

    showDialog(
      context: context,
      barrierDismissible: false,
      builder: (_) => const Center(
        child: Card(
          child: Padding(
            padding: EdgeInsets.all(24.0),
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                CircularProgressIndicator(color: Color(0xFF001631)),
                SizedBox(height: 16),
                Text('Đang nộp bài & chấm điểm ETS...', style: TextStyle(fontWeight: FontWeight.bold)),
              ],
            ),
          ),
        ),
      ),
    );

    try {
      final result = await _apiService.submitAttempt(widget.session.attemptId);
      if (!mounted) return;
      Navigator.pop(context); // Close dialog

      // Replace exam room with Result Screen
      Navigator.pushReplacement(
        context,
        MaterialPageRoute(
          builder: (_) => ExamResultScreen(result: result),
        ),
      );
    } catch (e) {
      if (!mounted) return;
      Navigator.pop(context); // Close dialog
      setState(() => _isSubmitting = false);
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('Lỗi khi nộp bài: $e'), backgroundColor: Colors.red),
      );
    }
  }

  void _showQuestionMatrixSheet() {
    showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(20)),
      ),
      builder: (ctx) => DraggableScrollableSheet(
        initialChildSize: 0.65,
        minChildSize: 0.4,
        maxChildSize: 0.9,
        expand: false,
        builder: (_, scrollController) => Padding(
          padding: const EdgeInsets.all(16.0),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Center(
                child: Container(
                  width: 40,
                  height: 4,
                  decoration: BoxDecoration(
                    color: Colors.grey.shade300,
                    borderRadius: BorderRadius.circular(2),
                  ),
                ),
              ),
              const SizedBox(height: 12),
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  const Text(
                    'Bảng 100 Câu hỏi',
                    style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold, color: Color(0xFF001631)),
                  ),
                  TextButton.icon(
                    onPressed: () {
                      Navigator.pop(ctx);
                      _confirmSubmit();
                    },
                    icon: const Icon(Icons.check_circle_outline, size: 16),
                    label: const Text('Nộp bài'),
                  ),
                ],
              ),
              const SizedBox(height: 8),
              // Legend
              Row(
                children: [
                  _buildLegendItem(const Color(0xFF10B981), 'Đã làm'),
                  const SizedBox(width: 8),
                  _buildLegendItem(Colors.white, 'Chưa làm', border: true),
                  const SizedBox(width: 8),
                  _buildLegendItem(const Color(0xFFF1F5F9), 'Khóa', border: true),
                  const SizedBox(width: 8),
                  _buildLegendItem(Colors.amber.shade700, 'Đã cờ'),
                ],
              ),
              const SizedBox(height: 12),
              const Divider(height: 1),
              const SizedBox(height: 12),
              Expanded(
                child: GridView.builder(
                  controller: scrollController,
                  gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(
                    crossAxisCount: 6,
                    mainAxisSpacing: 8,
                    crossAxisSpacing: 8,
                    childAspectRatio: 1.1,
                  ),
                  itemCount: widget.questions.length,
                  itemBuilder: (context, idx) {
                    final q = widget.questions[idx];
                    final isCurrent = idx == _currentIndex;
                    final isAnswered = q.selectedOption != null && q.selectedOption!.isNotEmpty;
                    final isUnlocked = _canAccessQuestion(idx);

                    Color bg = Colors.white;
                    Color textColor = Colors.black87;
                    Border? border = Border.all(color: Colors.grey.shade300);

                    if (isCurrent) {
                      bg = const Color(0xFF001631);
                      textColor = Colors.white;
                      border = null;
                    } else if (isAnswered) {
                      bg = const Color(0xFF10B981);
                      textColor = Colors.white;
                      border = null;
                    } else if (!isUnlocked) {
                      bg = const Color(0xFFF1F5F9);
                      textColor = Colors.grey.shade400;
                      border = Border.all(color: Colors.grey.shade200);
                    }

                    return InkWell(
                      onTap: () {
                        if (isUnlocked) {
                          Navigator.pop(ctx);
                          _jumpToQuestion(idx);
                        } else {
                          _showRequirementAlert('Vui lòng chọn đáp án trước khi tiếp tục.');
                        }
                      },
                      child: Container(
                        decoration: BoxDecoration(
                          color: bg,
                          borderRadius: BorderRadius.circular(8),
                          border: border,
                        ),
                        child: Stack(
                          alignment: Alignment.center,
                          children: [
                            Column(
                              mainAxisAlignment: MainAxisAlignment.center,
                              children: [
                                Text(
                                  '${q.questionNumber}',
                                  style: TextStyle(
                                    fontSize: 12,
                                    fontWeight: FontWeight.bold,
                                    color: textColor,
                                  ),
                                ),
                                if (isAnswered)
                                  Text(
                                    q.selectedOption!,
                                    style: TextStyle(
                                      fontSize: 10,
                                      fontWeight: FontWeight.w600,
                                      color: isCurrent ? Colors.amberAccent : Colors.white70,
                                    ),
                                  )
                                else if (!isUnlocked)
                                  Icon(Icons.lock_outline, size: 10, color: Colors.grey.shade400),
                              ],
                            ),
                            if (q.isFlagged)
                              Positioned(
                                top: 2,
                                right: 2,
                                child: Icon(
                                  Icons.bookmark,
                                  size: 11,
                                  color: isAnswered || isCurrent ? Colors.amberAccent : Colors.amber.shade700,
                                ),
                              ),
                          ],
                        ),
                      ),
                    );
                  },
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildLegendItem(Color color, String text, {bool border = false}) {
    return Row(
      children: [
        Container(
          width: 12,
          height: 12,
          decoration: BoxDecoration(
            color: color,
            borderRadius: BorderRadius.circular(3),
            border: border ? Border.all(color: Colors.grey) : null,
          ),
        ),
        const SizedBox(width: 4),
        Text(text, style: const TextStyle(fontSize: 11, color: Colors.grey)),
      ],
    );
  }

  @override
  Widget build(BuildContext context) {
    final q = _currentQuestion;
    final isPart5 = q.part == 5;
    final isTimerLow = _remainingSeconds < 300; // less than 5 min

    return PopScope(
      canPop: false,
      onPopInvokedWithResult: (didPop, result) async {
        if (didPop) return;
        final shouldPop = await showDialog<bool>(
          context: context,
          builder: (ctx) => AlertDialog(
            title: const Text('Rời khỏi phòng thi?'),
            content: const Text('Nếu thoát, bài thi của bạn sẽ vẫn tiếp tục tính giờ trên máy chủ.'),
            actions: [
              TextButton(onPressed: () => Navigator.pop(ctx, false), child: const Text('Ở lại làm bài')),
              TextButton(onPressed: () => Navigator.pop(ctx, true), child: const Text('Thoát ra', style: TextStyle(color: Colors.red))),
            ],
          ),
        );
        if (shouldPop == true && context.mounted) {
          Navigator.pop(context);
        }
      },
      child: Scaffold(
        backgroundColor: const Color(0xFFF8F9FF),
        appBar: AppBar(
          backgroundColor: Colors.white,
          elevation: 0.5,
          leading: IconButton(
            icon: const Icon(Icons.arrow_back, color: Color(0xFF001631)),
            onPressed: () => Navigator.maybePop(context),
          ),
          title: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                widget.session.examTitle,
                style: const TextStyle(fontSize: 14, fontWeight: FontWeight.bold, color: Color(0xFF001631)),
                overflow: TextOverflow.ellipsis,
              ),
              Text(
                'Thí sinh: ${widget.session.studentName}',
                style: const TextStyle(fontSize: 11, color: Colors.grey),
              ),
            ],
          ),
          actions: [
            // TIMER BADGE
            Container(
              margin: const EdgeInsets.symmetric(vertical: 10, horizontal: 8),
              padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
              decoration: BoxDecoration(
                color: isTimerLow ? Colors.red.shade50 : const Color(0xFFEFF4FF),
                borderRadius: BorderRadius.circular(8),
                border: Border.all(color: isTimerLow ? Colors.red : const Color(0xFF316BF3)),
              ),
              child: Row(
                children: [
                  Icon(
                    Icons.timer,
                    size: 16,
                    color: isTimerLow ? Colors.red : const Color(0xFF0051D5),
                  ),
                  const SizedBox(width: 4),
                  Text(
                    _formatTimer(_remainingSeconds),
                    style: TextStyle(
                      fontSize: 13,
                      fontWeight: FontWeight.bold,
                      color: isTimerLow ? Colors.red : const Color(0xFF0051D5),
                    ),
                  ),
                ],
              ),
            ),
            // SUBMIT BUTTON
            Padding(
              padding: const EdgeInsets.only(right: 8.0),
              child: TextButton(
                onPressed: _confirmSubmit,
                style: TextButton.styleFrom(
                  foregroundColor: Colors.white,
                  backgroundColor: const Color(0xFF001631),
                  shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
                  padding: const EdgeInsets.symmetric(horizontal: 12),
                ),
                child: const Text('Nộp bài', style: TextStyle(fontWeight: FontWeight.bold, fontSize: 13)),
              ),
            ),
          ],
        ),
        body: Column(
          children: [
            // PART SELECTOR TABS
            Container(
              color: Colors.white,
              padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
              child: Row(
                children: [
                  _buildPartTabButton('Part 5 (101-130)', 0, 5),
                  const SizedBox(width: 6),
                  _buildPartTabButton('Part 6 (131-146)', 30, 6),
                  const SizedBox(width: 6),
                  _buildPartTabButton('Part 7 (147-200)', 46, 7),
                ],
              ),
            ),

            // MAIN QUESTION & PASSAGE AREA
            Expanded(
              child: SingleChildScrollView(
                padding: const EdgeInsets.all(16),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    // PASSAGE BOX (for Part 6 and Part 7)
                    if (!isPart5 && q.passageText != null && q.passageText!.isNotEmpty) ...[
                      Container(
                        padding: const EdgeInsets.all(14),
                        decoration: BoxDecoration(
                          color: Colors.white,
                          borderRadius: BorderRadius.circular(12),
                          border: Border.all(color: const Color(0xFFE2E8F0)),
                        ),
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Row(
                              mainAxisAlignment: MainAxisAlignment.spaceBetween,
                              children: [
                                Row(
                                  children: [
                                    const Icon(Icons.article_outlined, size: 16, color: Color(0xFF0051D5)),
                                    const SizedBox(width: 6),
                                    Text(
                                      q.groupTitle ?? 'Reading Passage',
                                      style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 13, color: Color(0xFF001631)),
                                    ),
                                  ],
                                ),
                                if (q.passageType != null)
                                  Container(
                                    padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 2),
                                    decoration: BoxDecoration(
                                      color: const Color(0xFFEFF4FF),
                                      borderRadius: BorderRadius.circular(4),
                                    ),
                                    child: Text(
                                      q.passageType!,
                                      style: const TextStyle(fontSize: 10, color: Color(0xFF0051D5), fontWeight: FontWeight.bold),
                                    ),
                                  ),
                              ],
                            ),
                            const SizedBox(height: 10),
                            const Divider(height: 1),
                            const SizedBox(height: 10),
                            Text(
                              q.passageText!,
                              style: const TextStyle(fontSize: 13.5, height: 1.6, color: Colors.black87),
                            ),
                            if (q.imageUrls != null && q.imageUrls!.isNotEmpty) ...[
                              const SizedBox(height: 10),
                              ...q.imageUrls!.split(';').map(
                                    (url) => Padding(
                                      padding: const EdgeInsets.only(top: 6),
                                      child: ClipRRect(
                                        borderRadius: BorderRadius.circular(8),
                                        child: Image.network(
                                          url,
                                          errorBuilder: (context, error, stackTrace) => const SizedBox(),
                                        ),
                                      ),
                                    ),
                                  ),
                            ],
                          ],
                        ),
                      ),
                      const SizedBox(height: 16),
                    ],

                    // QUESTION CARD
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
                          // QUESTION HEADER
                          Row(
                            mainAxisAlignment: MainAxisAlignment.spaceBetween,
                            children: [
                              Row(
                                children: [
                                  Container(
                                    padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                                    decoration: BoxDecoration(
                                      color: const Color(0xFF001631),
                                      borderRadius: BorderRadius.circular(6),
                                    ),
                                    child: Text(
                                      'Câu ${q.questionNumber}',
                                      style: const TextStyle(color: Colors.white, fontWeight: FontWeight.bold, fontSize: 13),
                                    ),
                                  ),
                                  const SizedBox(width: 8),
                                  Text(
                                    'Part ${q.part}',
                                    style: const TextStyle(color: Colors.grey, fontSize: 12, fontWeight: FontWeight.w600),
                                  ),
                                ],
                              ),
                              IconButton(
                                icon: Icon(
                                  q.isFlagged ? Icons.bookmark : Icons.bookmark_border,
                                  color: q.isFlagged ? Colors.amber.shade700 : Colors.grey,
                                ),
                                tooltip: 'Đánh dấu xem lại',
                                onPressed: _toggleFlag,
                              ),
                            ],
                          ),
                          const SizedBox(height: 12),

                          // STEM
                          Text(
                            q.stem,
                            style: const TextStyle(fontSize: 15, fontWeight: FontWeight.w600, height: 1.5, color: Color(0xFF0D1C2E)),
                          ),
                          const SizedBox(height: 16),

                          // 4 OPTIONS
                          ...q.options.map((opt) {
                            final isSelected = q.selectedOption == opt.optionKey;
                            return Container(
                              margin: const EdgeInsets.only(bottom: 10),
                              child: InkWell(
                                onTap: () => _selectOption(opt.optionKey),
                                borderRadius: BorderRadius.circular(10),
                                child: Container(
                                  padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 12),
                                  decoration: BoxDecoration(
                                    color: isSelected ? const Color(0xFFEFF4FF) : Colors.white,
                                    borderRadius: BorderRadius.circular(10),
                                    border: Border.all(
                                      color: isSelected ? const Color(0xFF0051D5) : const Color(0xFFE2E8F0),
                                      width: isSelected ? 1.5 : 1.0,
                                    ),
                                  ),
                                  child: Row(
                                    children: [
                                      Container(
                                        width: 28,
                                        height: 28,
                                        decoration: BoxDecoration(
                                          shape: BoxShape.circle,
                                          color: isSelected ? const Color(0xFF0051D5) : const Color(0xFFF1F5F9),
                                        ),
                                        child: Center(
                                          child: Text(
                                            opt.optionKey,
                                            style: TextStyle(
                                              fontSize: 13,
                                              fontWeight: FontWeight.bold,
                                              color: isSelected ? Colors.white : Colors.black87,
                                            ),
                                          ),
                                        ),
                                      ),
                                      const SizedBox(width: 12),
                                      Expanded(
                                        child: Text(
                                          opt.content,
                                          style: TextStyle(
                                            fontSize: 14,
                                            color: isSelected ? const Color(0xFF001631) : Colors.black87,
                                            fontWeight: isSelected ? FontWeight.w600 : FontWeight.normal,
                                          ),
                                        ),
                                      ),
                                    ],
                                  ),
                                ),
                              ),
                            );
                          }),
                        ],
                      ),
                    ),
                  ],
                ),
              ),
            ),

            // BOTTOM BAR
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 10),
              decoration: BoxDecoration(
                color: Colors.white,
                boxShadow: [
                  BoxShadow(
                    color: Colors.black.withValues(alpha: 0.05),
                    blurRadius: 4,
                    offset: const Offset(0, -2),
                  ),
                ],
              ),
              child: Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  // PREV BUTTON
                  OutlinedButton.icon(
                    style: OutlinedButton.styleFrom(
                      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
                      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
                    ),
                    onPressed: _currentIndex > 0 ? () => _jumpToQuestion(_currentIndex - 1) : null,
                    icon: const Icon(Icons.chevron_left),
                    label: const Text('Câu trước'),
                  ),

                  // QUESTION MATRIX SHEET BUTTON
                  TextButton.icon(
                    style: TextButton.styleFrom(
                      foregroundColor: const Color(0xFF001631),
                      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 8),
                    ),
                    onPressed: _showQuestionMatrixSheet,
                    icon: const Icon(Icons.grid_view),
                    label: Text('${_currentIndex + 1} / 100', style: const TextStyle(fontWeight: FontWeight.bold)),
                  ),

                  // NEXT BUTTON
                  ElevatedButton.icon(
                    style: ElevatedButton.styleFrom(
                      backgroundColor: const Color(0xFF001631),
                      foregroundColor: Colors.white,
                      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
                      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
                    ),
                    onPressed: _currentIndex < widget.questions.length - 1
                        ? _onNextPressed
                        : _confirmSubmit,
                    icon: Icon(_currentIndex < widget.questions.length - 1 ? Icons.chevron_right : Icons.check),
                    label: Text(_currentIndex < widget.questions.length - 1 ? 'Câu tiếp' : 'Nộp bài'),
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildPartTabButton(String label, int targetIndex, int partNumber) {
    final isPartActive = _currentQuestion.part == partNumber;
    return Expanded(
      child: InkWell(
        onTap: () => _jumpToQuestion(targetIndex),
        child: Container(
          padding: const EdgeInsets.symmetric(vertical: 6),
          decoration: BoxDecoration(
            color: isPartActive ? const Color(0xFF001631) : const Color(0xFFF1F5F9),
            borderRadius: BorderRadius.circular(6),
          ),
          child: Center(
            child: Text(
              label,
              style: TextStyle(
                fontSize: 11,
                fontWeight: FontWeight.bold,
                color: isPartActive ? Colors.white : Colors.black87,
              ),
            ),
          ),
        ),
      ),
    );
  }
}
