class ExamItem {
  final int id;
  final String title;
  final String code;
  final String description;
  final int durationMinutes;
  final int totalQuestions;
  final bool isPublished;
  final int attemptCount;

  ExamItem({
    required this.id,
    required this.title,
    required this.code,
    required this.description,
    required this.durationMinutes,
    required this.totalQuestions,
    required this.isPublished,
    required this.attemptCount,
  });

  factory ExamItem.fromJson(Map<String, dynamic> json) {
    return ExamItem(
      id: json['id'] ?? 0,
      title: json['title'] ?? '',
      code: json['code'] ?? '',
      description: json['description'] ?? '',
      durationMinutes: json['durationMinutes'] ?? 75,
      totalQuestions: json['totalQuestions'] ?? 100,
      isPublished: json['isPublished'] ?? false,
      attemptCount: json['attemptCount'] ?? 0,
    );
  }
}

class AttemptSession {
  final String attemptId;
  final int examId;
  final String examTitle;
  final String studentName;
  final String studentCode;
  final DateTime startTime;
  final DateTime deadlineTime;
  final int remainingSeconds;

  AttemptSession({
    required this.attemptId,
    required this.examId,
    required this.examTitle,
    required this.studentName,
    required this.studentCode,
    required this.startTime,
    required this.deadlineTime,
    required this.remainingSeconds,
  });

  factory AttemptSession.fromJson(Map<String, dynamic> json) {
    return AttemptSession(
      attemptId: json['attemptId'] ?? '',
      examId: json['examId'] ?? 0,
      examTitle: json['examTitle'] ?? '',
      studentName: json['studentName'] ?? '',
      studentCode: json['studentCode'] ?? '',
      startTime: DateTime.tryParse(json['startTime'] ?? '') ?? DateTime.now(),
      deadlineTime: DateTime.tryParse(json['deadlineTime'] ?? '') ?? DateTime.now().add(const Duration(minutes: 75)),
      remainingSeconds: json['remainingSeconds'] ?? 4500,
    );
  }
}

class OptionItem {
  final int id;
  final String optionKey; // A, B, C, D
  final String content;
  final bool? isCorrect; // Only present in review

  OptionItem({
    required this.id,
    required this.optionKey,
    required this.content,
    this.isCorrect,
  });

  factory OptionItem.fromJson(Map<String, dynamic> json) {
    return OptionItem(
      id: json['id'] ?? 0,
      optionKey: json['optionKey'] ?? '',
      content: json['content'] ?? '',
      isCorrect: json['isCorrect'],
    );
  }
}

class AttemptQuestion {
  final int questionId;
  final int questionNumber;
  final int part; // 5, 6, 7
  final String stem;
  final int? questionGroupId;
  final String? groupTitle;
  final String? passageType;
  final String? passageText;
  final String? imageUrls;
  String? selectedOption;
  bool isFlagged;
  final List<OptionItem> options;

  AttemptQuestion({
    required this.questionId,
    required this.questionNumber,
    required this.part,
    required this.stem,
    this.questionGroupId,
    this.groupTitle,
    this.passageType,
    this.passageText,
    this.imageUrls,
    this.selectedOption,
    this.isFlagged = false,
    required this.options,
  });

  factory AttemptQuestion.fromJson(Map<String, dynamic> json) {
    var rawOpts = json['options'] as List? ?? [];
    return AttemptQuestion(
      questionId: json['questionId'] ?? 0,
      questionNumber: json['questionNumber'] ?? 101,
      part: json['part'] ?? 5,
      stem: json['stem'] ?? '',
      questionGroupId: json['questionGroupId'],
      groupTitle: json['groupTitle'],
      passageType: json['passageType'],
      passageText: json['passageText'],
      imageUrls: json['imageUrls'],
      selectedOption: json['selectedOption'],
      isFlagged: json['isFlagged'] ?? false,
      options: rawOpts.map((o) => OptionItem.fromJson(o)).toList(),
    );
  }
}

class ExamResultData {
  final String attemptId;
  final int examId;
  final String examTitle;
  final String studentName;
  final String studentCode;
  final int timeSpentSeconds;
  final int correctCount;
  final int wrongCount;
  final int skippedCount;
  final int totalQuestions;
  final double accuracyPercentage;
  final int part5Correct;
  final int part5Total;
  final int part6Correct;
  final int part6Total;
  final int part7Correct;
  final int part7Total;
  final List<QuestionReviewItem> questionReviews;

  ExamResultData({
    required this.attemptId,
    required this.examId,
    required this.examTitle,
    required this.studentName,
    required this.studentCode,
    required this.timeSpentSeconds,
    required this.correctCount,
    required this.wrongCount,
    required this.skippedCount,
    required this.totalQuestions,
    required this.accuracyPercentage,
    required this.part5Correct,
    required this.part5Total,
    required this.part6Correct,
    required this.part6Total,
    required this.part7Correct,
    required this.part7Total,
    required this.questionReviews,
  });

  factory ExamResultData.fromJson(Map<String, dynamic> json) {
    var rawReviews = json['questionReviews'] as List? ?? [];
    return ExamResultData(
      attemptId: json['attemptId'] ?? '',
      examId: json['examId'] ?? 0,
      examTitle: json['examTitle'] ?? '',
      studentName: json['studentName'] ?? '',
      studentCode: json['studentCode'] ?? '',
      timeSpentSeconds: json['timeSpentSeconds'] ?? 0,
      correctCount: json['correctCount'] ?? 0,
      wrongCount: json['wrongCount'] ?? 0,
      skippedCount: json['skippedCount'] ?? 0,
      totalQuestions: json['totalQuestions'] ?? 100,
      accuracyPercentage: (json['accuracyPercentage'] as num?)?.toDouble() ?? 0.0,
      part5Correct: json['part5Correct'] ?? 0,
      part5Total: json['part5Total'] ?? 30,
      part6Correct: json['part6Correct'] ?? 0,
      part6Total: json['part6Total'] ?? 16,
      part7Correct: json['part7Correct'] ?? 0,
      part7Total: json['part7Total'] ?? 54,
      questionReviews: rawReviews.map((r) => QuestionReviewItem.fromJson(r)).toList(),
    );
  }
}

class QuestionReviewItem {
  final int questionId;
  final int questionNumber;
  final int part;
  final String stem;
  final String? passageTitle;
  final String? passageText;
  final String? selectedOption;
  final String? correctOption;
  final bool isCorrect;
  final bool isSkipped;
  final String? explanation;
  final List<OptionItem> options;

  QuestionReviewItem({
    required this.questionId,
    required this.questionNumber,
    required this.part,
    required this.stem,
    this.passageTitle,
    this.passageText,
    this.selectedOption,
    this.correctOption,
    required this.isCorrect,
    required this.isSkipped,
    this.explanation,
    required this.options,
  });

  factory QuestionReviewItem.fromJson(Map<String, dynamic> json) {
    var rawOpts = json['options'] as List? ?? [];
    return QuestionReviewItem(
      questionId: json['questionId'] ?? 0,
      questionNumber: json['questionNumber'] ?? 101,
      part: json['part'] ?? 5,
      stem: json['stem'] ?? '',
      passageTitle: json['passageTitle'],
      passageText: json['passageText'],
      selectedOption: json['selectedOption'],
      correctOption: json['correctOption'],
      isCorrect: json['isCorrect'] ?? false,
      isSkipped: json['isSkipped'] ?? false,
      explanation: json['explanation'],
      options: rawOpts.map((o) => OptionItem.fromJson(o)).toList(),
    );
  }
}
