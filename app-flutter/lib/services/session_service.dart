import 'package:shared_preferences/shared_preferences.dart';

import '../models/employee.dart';

class SessionService {
  static const _keyEmployeeId = 'employee_id';
  static const _keyEmployeeName = 'employee_name';
  static const _keyAccessCode = 'employee_access_code';

  Future<Employee?> loadEmployee() async {
    final prefs = await SharedPreferences.getInstance();
    final id = prefs.getInt(_keyEmployeeId);
    final name = prefs.getString(_keyEmployeeName);
    final code = prefs.getString(_keyAccessCode);

    if (id == null || name == null || code == null) {
      return null;
    }

    return Employee(id: id, name: name, accessCode: code);
  }

  Future<void> saveEmployee(Employee employee) async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.setInt(_keyEmployeeId, employee.id);
    await prefs.setString(_keyEmployeeName, employee.name);
    await prefs.setString(_keyAccessCode, employee.accessCode);
  }

  Future<void> clear() async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.remove(_keyEmployeeId);
    await prefs.remove(_keyEmployeeName);
    await prefs.remove(_keyAccessCode);
  }
}
