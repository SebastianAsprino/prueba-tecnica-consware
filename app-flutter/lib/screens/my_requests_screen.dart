import 'package:flutter/material.dart';

import '../models/employee.dart';
import '../models/expense_request.dart';
import '../services/api_client.dart';
import '../services/api_exception.dart';

class MyRequestsScreen extends StatefulWidget {
  final Employee employee;
  final int refreshToken;

  const MyRequestsScreen({super.key, required this.employee, required this.refreshToken});

  @override
  State<MyRequestsScreen> createState() => _MyRequestsScreenState();
}

class _MyRequestsScreenState extends State<MyRequestsScreen> {
  final _apiClient = ApiClient();

  late Future<List<ExpenseRequest>> _future;

  @override
  void initState() {
    super.initState();
    _load();
  }

  @override
  void didUpdateWidget(covariant MyRequestsScreen oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (oldWidget.refreshToken != widget.refreshToken) {
      _load();
    }
  }

  void _load() {
    setState(() {
      _future = _apiClient.listMyExpenseRequests(widget.employee.id);
    });
  }

  Future<void> _refresh() async {
    final result = _apiClient.listMyExpenseRequests(widget.employee.id);
    setState(() => _future = result);
    await result;
  }

  @override
  Widget build(BuildContext context) {
    return FutureBuilder<List<ExpenseRequest>>(
      future: _future,
      builder: (context, snapshot) {
        if (snapshot.connectionState == ConnectionState.waiting) {
          return const Center(child: CircularProgressIndicator());
        }

        if (snapshot.hasError) {
          final message = snapshot.error is ApiException
              ? (snapshot.error as ApiException).message
              : 'Ocurrió un error inesperado.';
          return _ErrorView(message: message, onRetry: _load);
        }

        final requests = snapshot.data ?? [];
        if (requests.isEmpty) {
          return RefreshIndicator(
            onRefresh: _refresh,
            child: ListView(
              children: const [
                SizedBox(height: 120),
                Center(child: Text('Aún no tienes solicitudes.')),
              ],
            ),
          );
        }

        return RefreshIndicator(
          onRefresh: _refresh,
          child: ListView.separated(
            padding: const EdgeInsets.all(12),
            itemCount: requests.length,
            separatorBuilder: (_, _) => const SizedBox(height: 8),
            itemBuilder: (context, index) => _RequestCard(request: requests[index]),
          ),
        );
      },
    );
  }
}

class _RequestCard extends StatelessWidget {
  final ExpenseRequest request;

  const _RequestCard({required this.request});

  @override
  Widget build(BuildContext context) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(12),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Text(request.category, style: const TextStyle(fontWeight: FontWeight.bold)),
                _StatusChip(status: request.status),
              ],
            ),
            const SizedBox(height: 4),
            Text('\$${request.amount.toStringAsFixed(0)} COP'),
            const SizedBox(height: 4),
            Text(request.description),
            if (request.status == ExpenseStatus.rejected && request.rejectionReason != null) ...[
              const SizedBox(height: 8),
              Text(
                'Motivo: ${request.rejectionReason}',
                style: TextStyle(color: Theme.of(context).colorScheme.error),
              ),
            ],
          ],
        ),
      ),
    );
  }
}

class _StatusChip extends StatelessWidget {
  final ExpenseStatus status;

  const _StatusChip({required this.status});

  @override
  Widget build(BuildContext context) {
    final (label, color) = switch (status) {
      ExpenseStatus.pending => ('Pendiente', Colors.orange),
      ExpenseStatus.approved => ('Aprobada', Colors.green),
      ExpenseStatus.rejected => ('Rechazada', Colors.red),
    };

    return Chip(
      label: Text(label, style: const TextStyle(color: Colors.white)),
      backgroundColor: color,
      visualDensity: VisualDensity.compact,
    );
  }
}

class _ErrorView extends StatelessWidget {
  final String message;
  final VoidCallback onRetry;

  const _ErrorView({required this.message, required this.onRetry});

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(24),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(Icons.wifi_off, size: 48, color: Theme.of(context).colorScheme.error),
            const SizedBox(height: 12),
            Text(message, textAlign: TextAlign.center),
            const SizedBox(height: 12),
            OutlinedButton(onPressed: onRetry, child: const Text('Reintentar')),
          ],
        ),
      ),
    );
  }
}
