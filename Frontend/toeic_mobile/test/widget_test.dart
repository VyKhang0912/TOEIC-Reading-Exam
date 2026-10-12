import 'package:flutter_test/flutter_test.dart';
import 'package:toeic_mobile/main.dart';

void main() {
  testWidgets('TOEIC App loads smoke test', (WidgetTester tester) async {
    await tester.pumpWidget(const ToeicReadingApp());
    expect(find.text('TOEIC Reading'), findsOneWidget);
  });
}
