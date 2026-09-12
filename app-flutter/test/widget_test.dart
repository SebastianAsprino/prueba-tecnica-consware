import 'package:flutter_test/flutter_test.dart';
import 'package:shared_preferences/shared_preferences.dart';

import 'package:app_flutter/main.dart';

void main() {
  testWidgets('Muestra la pantalla de login cuando no hay sesión guardada', (tester) async {
    SharedPreferences.setMockInitialValues({});

    await tester.pumpWidget(const ExpenseReimbursementApp());
    await tester.pumpAndSettle();

    expect(find.text('Entrar'), findsOneWidget);
  });
}
