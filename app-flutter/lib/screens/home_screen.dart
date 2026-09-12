import 'package:flutter/material.dart';

import '../models/employee.dart';
import '../services/session_service.dart';
import 'login_screen.dart';
import 'my_requests_screen.dart';
import 'new_expense_screen.dart';

class HomeScreen extends StatefulWidget {
  final Employee employee;

  const HomeScreen({super.key, required this.employee});

  @override
  State<HomeScreen> createState() => _HomeScreenState();
}

class _HomeScreenState extends State<HomeScreen> {
  int _tabIndex = 0;
  int _requestsRefreshToken = 0;

  Future<void> _logout() async {
    await SessionService().clear();
    if (!mounted) return;
    Navigator.of(context).pushReplacement(
      MaterialPageRoute(builder: (_) => const LoginScreen()),
    );
  }

  void _onExpenseCreated() {
    setState(() {
      _tabIndex = 1;
      _requestsRefreshToken++;
    });
  }

  void _onDestinationSelected(int index) {
    setState(() {
      _tabIndex = index;
      if (index == 1) {
        // Recarga siempre al entrar a "Mis solicitudes": el estado pudo cambiar
        // desde el panel del aprobador y el IndexedStack no lo detecta solo.
        _requestsRefreshToken++;
      }
    });
  }

  @override
  Widget build(BuildContext context) {
    final screens = [
      NewExpenseScreen(employee: widget.employee, onCreated: _onExpenseCreated),
      MyRequestsScreen(employee: widget.employee, refreshToken: _requestsRefreshToken),
    ];

    return Scaffold(
      appBar: AppBar(
        title: Text(widget.employee.name),
        actions: [
          IconButton(
            icon: const Icon(Icons.logout),
            tooltip: 'Salir',
            onPressed: _logout,
          ),
        ],
      ),
      body: IndexedStack(index: _tabIndex, children: screens),
      bottomNavigationBar: NavigationBar(
        selectedIndex: _tabIndex,
        onDestinationSelected: _onDestinationSelected,
        destinations: const [
          NavigationDestination(icon: Icon(Icons.add_circle_outline), label: 'Nuevo gasto'),
          NavigationDestination(icon: Icon(Icons.list_alt), label: 'Mis solicitudes'),
        ],
      ),
    );
  }
}
