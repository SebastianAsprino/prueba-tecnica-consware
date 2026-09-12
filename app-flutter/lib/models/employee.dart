class Employee {
  final int id;
  final String name;
  final String accessCode;

  Employee({required this.id, required this.name, required this.accessCode});

  factory Employee.fromJson(Map<String, dynamic> json) => Employee(
        id: json['id'] as int,
        name: json['name'] as String,
        accessCode: json['accessCode'] as String,
      );
}
