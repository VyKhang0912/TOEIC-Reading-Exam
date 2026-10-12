import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:toeic_mobile/models/toeic_models.dart';
import 'package:toeic_mobile/screens/exam_room_screen.dart';

void main() {
  testWidgets('Test TOEIC sequential exam navigation constraints', (WidgetTester tester) async {
    tester.view.physicalSize = const Size(800, 1400);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);

    // Prepare mock session and 100 questions (101 to 200)
    final session = AttemptSession(
      attemptId: 'test-attempt-guid',
      examId: 1,
      examTitle: 'ETS 2024 TOEIC Reading Practice Test',
      studentName: 'Nguyen Van A',
      studentCode: 'HV2024',
      startTime: DateTime.now(),
      deadlineTime: DateTime.now().add(const Duration(minutes: 75)),
      remainingSeconds: 4500,
    );

    final questions = List.generate(100, (i) {
      final qNum = 101 + i;
      int part = 5;
      if (qNum >= 131 && qNum <= 146) part = 6;
      if (qNum >= 147) part = 7;

      return AttemptQuestion(
        questionId: i + 1,
        questionNumber: qNum,
        part: part,
        stem: 'Question stem #$qNum text',
        options: [
          OptionItem(id: 1, optionKey: 'A', content: 'Option A for $qNum'),
          OptionItem(id: 2, optionKey: 'B', content: 'Option B for $qNum'),
          OptionItem(id: 3, optionKey: 'C', content: 'Option C for $qNum'),
          OptionItem(id: 4, optionKey: 'D', content: 'Option D for $qNum'),
        ],
      );
    });

    await tester.pumpWidget(MaterialApp(
      home: ExamRoomScreen(
        session: session,
        questions: questions,
      ),
    ));
    await tester.pumpAndSettle();

    // 1. Initially at Question 101
    expect(find.text('Câu 101'), findsOneWidget);

    // 2. Try to press "Câu tiếp" when no answer is chosen
    await tester.tap(find.widgetWithText(ElevatedButton, 'Câu tiếp'), warnIfMissed: false);
    await tester.pump();

    // Verify warning message appeared and STILL at Question 101
    expect(find.text('Vui lòng chọn đáp án trước khi tiếp tục.'), findsOneWidget);
    expect(find.text('Câu 101'), findsOneWidget);

    // 3. Try to jump to Part 6 tab directly
    await tester.tap(find.text('Part 6 (131-146)'), warnIfMissed: false);
    await tester.pump();

    // Still blocked at Question 101
    expect(find.text('Vui lòng chọn đáp án trước khi tiếp tục.'), findsOneWidget);
    expect(find.text('Câu 101'), findsOneWidget);

    // 4. Select Option A for Question 101
    await tester.tap(find.text('Option A for 101'), warnIfMissed: false);
    await tester.pumpAndSettle();
    expect(questions[0].selectedOption, 'A');

    // Dismiss any snackbar
    ScaffoldMessenger.of(tester.element(find.byType(ExamRoomScreen))).clearSnackBars();
    await tester.pumpAndSettle();

    // Now press "Câu tiếp"
    await tester.tap(find.widgetWithText(ElevatedButton, 'Câu tiếp'), warnIfMissed: false);
    await tester.pumpAndSettle();

    // 5. Successfully advanced to Question 102!
    expect(find.text('Câu 102'), findsOneWidget);

    // 6. At Question 102 (unanswered), try to press "Câu tiếp"
    await tester.tap(find.widgetWithText(ElevatedButton, 'Câu tiếp'), warnIfMissed: false);
    await tester.pump();

    // Blocked with warning
    expect(find.text('Vui lòng chọn đáp án trước khi tiếp tục.'), findsOneWidget);
    expect(find.text('Câu 102'), findsOneWidget);

    // Dismiss snackbar
    ScaffoldMessenger.of(tester.element(find.byType(ExamRoomScreen))).clearSnackBars();
    await tester.pumpAndSettle();

    // 7. Go back to Question 101 via "Câu trước"
    await tester.tap(find.widgetWithText(OutlinedButton, 'Câu trước'), warnIfMissed: false);
    await tester.pumpAndSettle();

    expect(find.text('Câu 101'), findsOneWidget);

    // 8. Change answer for Question 101 from A to B
    await tester.tap(find.text('Option B for 101'), warnIfMissed: false);
    await tester.pumpAndSettle();

    expect(questions[0].selectedOption, 'B');

    // 9. Now move forward to Question 102 again
    await tester.tap(find.widgetWithText(ElevatedButton, 'Câu tiếp'), warnIfMissed: false);
    await tester.pumpAndSettle();

    expect(find.text('Câu 102'), findsOneWidget);
  });
}
