import 'package:flutter/material.dart';
import 'screens/exam_list_screen.dart';

void main() {
  runApp(const ToeicReadingApp());
}

class ToeicReadingApp extends StatelessWidget {
  const ToeicReadingApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'TOEIC Reading ETS 2024',
      debugShowCheckedModeBanner: false,
      theme: ThemeData(
        useMaterial3: true,
        primaryColor: const Color(0xFF001631),
        scaffoldBackgroundColor: const Color(0xFFF8F9FF),
        colorScheme: ColorScheme.fromSeed(
          seedColor: const Color(0xFF001631),
          primary: const Color(0xFF001631),
          secondary: const Color(0xFF0051D5),
          surface: Colors.white,
        ),
        appBarTheme: const AppBarTheme(
          backgroundColor: Colors.white,
          foregroundColor: Color(0xFF001631),
          elevation: 0.5,
          centerTitle: false,
        ),
      ),
      home: const ExamListScreen(),
    );
  }
}
