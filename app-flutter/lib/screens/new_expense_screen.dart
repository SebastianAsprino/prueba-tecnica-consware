import 'package:flutter/material.dart';

import '../models/employee.dart';
import '../models/expense_request.dart';
import '../services/api_client.dart';
import '../services/api_exception.dart';

const _categories = ['Comida', 'Transporte', 'Viáticos', 'Papelería', 'Otro'];

class NewExpenseScreen extends StatefulWidget {
  final Employee employee;
  final VoidCallback onCreated;

  const NewExpenseScreen({super.key, required this.employee, required this.onCreated});

  @override
  State<NewExpenseScreen> createState() => _NewExpenseScreenState();
}

class _NewExpenseScreenState extends State<NewExpenseScreen> {
  final _formKey = GlobalKey<FormState>();
  final _amountController = TextEditingController();
  final _descriptionController = TextEditingController();
  final _apiClient = ApiClient();

  String? _category;
  bool _submitting = false;

  @override
  void dispose() {
    _amountController.dispose();
    _descriptionController.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (!_formKey.currentState!.validate() || _category == null) {
      if (_category == null) {
        setState(() {}); // fuerza redibujar el mensaje de categoría obligatoria
      }
      return;
    }

    setState(() => _submitting = true);

    try {
      final amount = double.parse(_amountController.text.replaceAll(',', '.'));
      final expense = await _apiClient.createExpenseRequest(
        employeeId: widget.employee.id,
        amount: amount,
        category: _category!,
        description: _descriptionController.text.trim(),
      );

      if (!mounted) return;

      final autoApproved = expense.status == ExpenseStatus.approved;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(
            autoApproved
                ? 'Gasto registrado y aprobado automáticamente.'
                : 'Gasto registrado. Queda pendiente de aprobación.',
          ),
        ),
      );

      _amountController.clear();
      _descriptionController.clear();
      setState(() => _category = null);
      widget.onCreated();
    } on ApiException catch (e) {
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text(e.message), backgroundColor: Theme.of(context).colorScheme.error),
      );
    } finally {
      if (mounted) setState(() => _submitting = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return SingleChildScrollView(
      padding: const EdgeInsets.all(16),
      child: Form(
        key: _formKey,
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Text('Registrar gasto', style: Theme.of(context).textTheme.headlineSmall),
            const SizedBox(height: 16),
            TextFormField(
              controller: _amountController,
              keyboardType: const TextInputType.numberWithOptions(decimal: true),
              decoration: const InputDecoration(labelText: 'Monto (COP)', border: OutlineInputBorder()),
              validator: (value) {
                final amount = double.tryParse((value ?? '').replaceAll(',', '.'));
                if (amount == null || amount <= 0) {
                  return 'Ingresa un monto válido mayor que cero.';
                }
                return null;
              },
            ),
            const SizedBox(height: 16),
            DropdownButtonFormField<String>(
              initialValue: _category,
              decoration: const InputDecoration(labelText: 'Categoría', border: OutlineInputBorder()),
              items: _categories.map((c) => DropdownMenuItem(value: c, child: Text(c))).toList(),
              onChanged: (value) => setState(() => _category = value),
              validator: (value) => value == null ? 'Selecciona una categoría.' : null,
            ),
            const SizedBox(height: 16),
            TextFormField(
              controller: _descriptionController,
              maxLines: 3,
              decoration: const InputDecoration(labelText: 'Descripción', border: OutlineInputBorder()),
              validator: (value) => (value == null || value.trim().isEmpty) ? 'La descripción es obligatoria.' : null,
            ),
            const SizedBox(height: 24),
            FilledButton(
              onPressed: _submitting ? null : _submit,
              child: _submitting
                  ? const SizedBox(height: 20, width: 20, child: CircularProgressIndicator(strokeWidth: 2))
                  : const Text('Enviar solicitud'),
            ),
          ],
        ),
      ),
    );
  }
}
