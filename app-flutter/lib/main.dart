import 'package:flutter/material.dart';

import 'models/employee.dart';
import 'screens/home_screen.dart';
import 'screens/login_screen.dart';
import 'services/session_service.dart';

void main() {
  runApp(const ExpenseReimbursementApp());
}

class ExpenseReimbursementApp extends StatelessWidget {
  const ExpenseReimbursementApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'Reembolso de gastos',
      theme: ThemeData(colorScheme: .fromSeed(seedColor: Colors.deepPurple)),
      home: const SessionGate(),
    );
  }
}

class SessionGate extends StatefulWidget {
  const SessionGate({super.key});

  @override
  State<SessionGate> createState() => _SessionGateState();
}

class _SessionGateState extends State<SessionGate> {
  final _sessionService = SessionService();
  late Future<Employee?> _future;

  @override
  void initState() {
    super.initState();
    _future = _sessionService.loadEmployee();
  }

  @override
  Widget build(BuildContext context) {
    return FutureBuilder<Employee?>(
      future: _future,
      builder: (context, snapshot) {
        if (snapshot.connectionState != ConnectionState.done) {
          return const Scaffold(body: Center(child: CircularProgressIndicator()));
        }

        final employee = snapshot.data;
        return employee == null ? const LoginScreen() : HomeScreen(employee: employee);
      },
    );
  }
}
