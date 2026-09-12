enum ExpenseStatus { pending, approved, rejected }

ExpenseStatus expenseStatusFromString(String value) {
  switch (value) {
    case 'Approved':
      return ExpenseStatus.approved;
    case 'Rejected':
      return ExpenseStatus.rejected;
    case 'Pending':
    default:
      return ExpenseStatus.pending;
  }
}

class ExpenseRequest {
  final int id;
  final int employeeId;
  final String employeeName;
  final double amount;
  final String category;
  final String description;
  final ExpenseStatus status;
  final String? rejectionReason;
  final DateTime createdAt;
  final DateTime? decidedAt;

  ExpenseRequest({
    required this.id,
    required this.employeeId,
    required this.employeeName,
    required this.amount,
    required this.category,
    required this.description,
    required this.status,
    required this.rejectionReason,
    required this.createdAt,
    required this.decidedAt,
  });

  factory ExpenseRequest.fromJson(Map<String, dynamic> json) => ExpenseRequest(
        id: json['id'] as int,
        employeeId: json['employeeId'] as int,
        employeeName: json['employeeName'] as String,
        amount: (json['amount'] as num).toDouble(),
        category: json['category'] as String,
        description: json['description'] as String,
        status: expenseStatusFromString(json['status'] as String),
        rejectionReason: json['rejectionReason'] as String?,
        createdAt: DateTime.parse(json['createdAt'] as String),
        decidedAt: json['decidedAt'] != null ? DateTime.parse(json['decidedAt'] as String) : null,
      );
}
