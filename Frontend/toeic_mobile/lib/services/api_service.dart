import 'dart:convert';
import 'dart:io';
import 'package:flutter/foundation.dart';
import 'package:http/http.dart' as http;
import 'package:http/io_client.dart';
import '../models/toeic_models.dart';

class ApiService {
  static final ApiService instance = ApiService._internal();
  factory ApiService() => instance;

  ApiService._internal() {
    _initClient();
  }

  // Base URL: Default to localhost:44312 for desktop/web, 10.0.2.2 for Android emulator
  String _baseUrl = 'https://localhost:44312/api/toeic';

  String get baseUrl => _baseUrl;

  void setBaseUrl(String url) {
    _baseUrl = url.endsWith('/') ? url.substring(0, url.length - 1) : url;
  }

  late http.Client _client;

  void _initClient() {
    if (!kIsWeb) {
      final httpClient = HttpClient()
        ..badCertificateCallback = (X509Certificate cert, String host, int port) => true;
      _client = IOClient(httpClient);
    } else {
      _client = http.Client();
    }
  }

  dynamic _unwrap(http.Response response) {
    if (response.statusCode >= 200 && response.statusCode < 300) {
      final data = jsonDecode(utf8.decode(response.bodyBytes));
      if (data is Map<String, dynamic> && data.containsKey('result')) {
        return data['result'];
      }
      return data;
    } else {
      throw Exception('Lỗi máy chủ (${response.statusCode}): ${response.body}');
    }
  }

  Future<List<ExamItem>> getPublishedExams() async {
    final uri = Uri.parse('$_baseUrl/exams');
    final response = await _client.get(uri);
    final data = _unwrap(response);
    if (data is List) {
      return data.map((x) => ExamItem.fromJson(x)).toList();
    }
    return [];
  }

  Future<AttemptSession> startAttempt(int examId, String studentName, String studentCode) async {
    final uri = Uri.parse('$_baseUrl/attempts/start');
    final response = await _client.post(
      uri,
      headers: {'Content-Type': 'application/json'},
      body: jsonEncode({
        'examId': examId,
        'studentName': studentName,
        'studentCode': studentCode,
      }),
    );
    final data = _unwrap(response);
    return AttemptSession.fromJson(data);
  }

  Future<List<AttemptQuestion>> getAttemptQuestions(String attemptId) async {
    final uri = Uri.parse('$_baseUrl/attempts/$attemptId/questions');
    final response = await _client.get(uri);
    final data = _unwrap(response);
    if (data is List) {
      return data.map((x) => AttemptQuestion.fromJson(x)).toList();
    }
    return [];
  }

  Future<void> saveAnswer(String attemptId, int questionId, String? selectedOption, bool isFlagged) async {
    final uri = Uri.parse('$_baseUrl/attempts/save-answer');
    await _client.post(
      uri,
      headers: {'Content-Type': 'application/json'},
      body: jsonEncode({
        'attemptId': attemptId,
        'questionId': questionId,
        'selectedOption': selectedOption,
        'isFlagged': isFlagged,
      }),
    );
  }

  Future<ExamResultData> submitAttempt(String attemptId) async {
    final uri = Uri.parse('$_baseUrl/attempts/submit');
    final response = await _client.post(
      uri,
      headers: {'Content-Type': 'application/json'},
      body: jsonEncode({
        'attemptId': attemptId,
      }),
    );
    final data = _unwrap(response);
    return ExamResultData.fromJson(data);
  }

  Future<ExamResultData> getAttemptResult(String attemptId) async {
    final uri = Uri.parse('$_baseUrl/attempts/$attemptId/result');
    final response = await _client.get(uri);
    final data = _unwrap(response);
    return ExamResultData.fromJson(data);
  }

  Future<AttemptSession> startSmartSession({
    required String studentCode,
    required String studentName,
    String? sessionCode,
    int? targetDifficulty,
    int? targetCategory,
  }) async {
    final uri = Uri.parse('$_baseUrl/sessions/start-smart');
    final response = await _client.post(
      uri,
      headers: {'Content-Type': 'application/json'},
      body: jsonEncode({
        'studentCode': studentCode,
        'studentName': studentName,
        'sessionCode': sessionCode ?? 'MAIN_SESSION',
        'targetDifficulty': targetDifficulty,
        'targetCategory': targetCategory,
      }),
    );
    final data = _unwrap(response);
    return AttemptSession.fromJson(data);
  }

  Future<AttemptSession?> getActiveSession(String studentCode, {String? sessionCode}) async {
    final query = 'studentCode=${Uri.encodeComponent(studentCode)}${sessionCode != null ? '&sessionCode=${Uri.encodeComponent(sessionCode)}' : ''}';
    final uri = Uri.parse('$_baseUrl/sessions/active?$query');
    final response = await _client.get(uri);
    final data = _unwrap(response);
    if (data == null) return null;
    return AttemptSession.fromJson(data);
  }

  String getAudioStreamUrl(int audioId) {
    return '$_baseUrl/audio/$audioId/stream';
  }
}

