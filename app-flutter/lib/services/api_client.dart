import 'dart:async';
import 'dart:convert';
import 'dart:io';

import 'package:flutter/foundation.dart' show kIsWeb;
import 'package:http/http.dart' as http;

import '../models/employee.dart';
import '../models/expense_request.dart';
import 'api_exception.dart';

class ApiClient {
  static const _requestTimeout = Duration(seconds: 10);
  static const _connectionErrorMessage = 'No se pudo conectar con el servidor. Verifica tu conexión.';

  static String get baseUrl {
    // El emulador Android no puede usar "localhost" para llegar al host: 10.0.2.2
    // es su loopback especial hacia la máquina que lo ejecuta. En web `Platform`
    // de dart:io no está soportado, por eso se descarta antes con kIsWeb.
    if (!kIsWeb && Platform.isAndroid) {
      return 'http://10.0.2.2:5098/api';
    }
    return 'http://localhost:5098/api';
  }

  Future<Employee> getEmployeeByCode(String code) async {
    final response = await _get('/employees/by-code/$code');
    return Employee.fromJson(response);
  }

  Future<ExpenseRequest> createExpenseRequest({
    required int employeeId,
    required double amount,
    required String category,
    required String description,
  }) async {
    final response = await _post('/expense-requests', {
      'employeeId': employeeId,
      'amount': amount,
      'category': category,
      'description': description,
    });
    return ExpenseRequest.fromJson(response);
  }

  Future<List<ExpenseRequest>> listMyExpenseRequests(int employeeId) async {
    final response = await _getList('/expense-requests?employeeId=$employeeId');
    return response.map((e) => ExpenseRequest.fromJson(e as Map<String, dynamic>)).toList();
  }

  Future<Map<String, dynamic>> _get(String path) async {
    final response = await _send(() => http.get(_uri(path)));
    return _decodeMap(response);
  }

  Future<List<dynamic>> _getList(String path) async {
    final response = await _send(() => http.get(_uri(path)));
    return _decodeList(response);
  }

  Future<Map<String, dynamic>> _post(String path, Map<String, dynamic> body) async {
    final response = await _send(
      () => http.post(
        _uri(path),
        headers: {'Content-Type': 'application/json'},
        body: jsonEncode(body),
      ),
    );
    return _decodeMap(response);
  }

  Uri _uri(String path) => Uri.parse('$baseUrl$path');

  /// Ejecuta la petición con timeout y traduce los fallos de red a [ApiException],
  /// que es lo único que las pantallas capturan.
  Future<http.Response> _send(Future<http.Response> Function() request) async {
    try {
      return await request().timeout(_requestTimeout);
    } on TimeoutException {
      throw ApiException('El servidor tardó demasiado en responder. Intenta de nuevo.');
    } on SocketException {
      throw ApiException(_connectionErrorMessage);
    } on http.ClientException {
      throw ApiException(_connectionErrorMessage);
    }
  }

  Map<String, dynamic> _decodeMap(http.Response response) {
    if (response.statusCode >= 200 && response.statusCode < 300) {
      return jsonDecode(response.body) as Map<String, dynamic>;
    }
    throw ApiException(_extractErrorMessage(response));
  }

  List<dynamic> _decodeList(http.Response response) {
    if (response.statusCode >= 200 && response.statusCode < 300) {
      return jsonDecode(response.body) as List<dynamic>;
    }
    throw ApiException(_extractErrorMessage(response));
  }

  String _extractErrorMessage(http.Response response) {
    try {
      final body = jsonDecode(response.body) as Map<String, dynamic>;
      final errors = body['errors'];
      if (errors is List && errors.isNotEmpty) {
        return errors.join(' ');
      }
    } catch (_) {
      // El cuerpo no es JSON o no tiene el formato esperado; se usa el mensaje genérico de abajo.
    }

    switch (response.statusCode) {
      case 404:
        return 'No se encontró la información solicitada.';
      case 401:
        return 'Código de acceso inválido.';
      default:
        return 'Ocurrió un error inesperado (código ${response.statusCode}).';
    }
  }
}
