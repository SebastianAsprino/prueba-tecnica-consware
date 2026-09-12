# App Flutter — Empleado

App móvil que usa el empleado para registrar gastos y ver sus solicitudes. Consume la API real de `api-net-core`; no tiene lógica de negocio propia.

## Arquitectura

```
lib/
  main.dart                       Punto de entrada + SessionGate (decide Login vs Home según haya sesión guardada)
  models/
    employee.dart                  Employee (id, name, accessCode)
    expense_request.dart           ExpenseRequest + enum ExpenseStatus, parseo de JSON del API
  services/
    api_client.dart                 Cliente HTTP (http package), maneja base URL según plataforma y errores de red
    api_exception.dart              Excepción con mensaje amigable para mostrar en pantalla
    session_service.dart            Persiste el empleado logueado en SharedPreferences
  screens/
    login_screen.dart               Input del código de 10 dígitos
    home_screen.dart                Shell con NavigationBar (Nuevo gasto / Mis solicitudes)
    new_expense_screen.dart          Formulario de registro de gasto
    my_requests_screen.dart          Lista de solicitudes del empleado con su estado
```

Sin gestor de estado externo (Provider/Riverpod/Bloc): `StatefulWidget` + `setState` es suficiente para 4 pantallas con este alcance.

## Identificación del empleado

No hay contraseña. El aprobador crea al empleado desde el panel Angular, que le entrega un **código de 10 dígitos**. El empleado lo escribe una vez en `LoginScreen`, la app llama a `GET /api/employees/by-code/{code}` y guarda el resultado (`id`, `name`, `accessCode`) en `SharedPreferences` — no vuelve a pedirlo mientras no se cierre sesión.

## Cómo correr

Requisitos: Flutter SDK (stable), y la **API corriendo primero** (`api-net-core`, ver su README) porque esta app no tiene datos propios.

```bash
cd app-flutter
flutter pub get
flutter run -d linux     # ver "Target usado" abajo
```

Antes de poder loguearte necesitas al menos un empleado creado: entra al panel Angular (`web-angular`), pestaña **Empleados**, crea uno y copia su código de 10 dígitos.

### Target usado: Linux desktop (no emulador Android)

Se decidió correr contra el target **Linux desktop** en vez del emulador Android originalmente planeado: la imagen de sistema de Android (~1GB) se descargó muy lento/se colgó durante la sesión, y bajo el límite de tiempo de la prueba no era razonable seguir esperando. El código es exactamente el mismo Flutter/Dart — solo cambia dónde se renderiza. Si tienes un emulador Android o un dispositivo físico disponibles, corre en su lugar:

```bash
flutter run -d <id-del-dispositivo>   # flutter devices para listarlos
```

`lib/services/api_client.dart` ya contempla ambos casos: usa `10.0.2.2` como host cuando detecta que corre en Android (loopback especial del emulador hacia el host) y `localhost` en cualquier otro caso — no hay que tocar código para cambiar de target.

### Requisitos de sistema para el target Linux (si no los tienes)

```bash
sudo pacman -S clang cmake   # Arch/EndeavourOS
```

## Manejo de errores

Todas las llamadas de red pasan por `ApiClient`, que:
- Atrapa `SocketException`/`ClientException` (API caída o sin red) y lanza `ApiException` con un mensaje legible en español.
- Traduce respuestas 4xx del API (`{ "errors": [...] }`) al mismo tipo de excepción.
- Cada pantalla muestra el mensaje en un `SnackBar` o una vista de error con botón "Reintentar" — nunca se deja una excepción sin capturar ni una pantalla en blanco.

## Decisiones técnicas

- **Sin gestor de estado externo.** `setState` es suficiente para el alcance actual (2 pantallas + sesión).
- **`IndexedStack` en `HomeScreen`** para no perder el formulario a medio llenar al cambiar de pestaña, con recarga forzada de "Mis solicitudes" cada vez que se entra a esa pestaña (el estado de una solicitud puede haber cambiado desde el panel Angular mientras tanto, y `IndexedStack` no lo detecta solo).
- **`SharedPreferences` para la sesión** en vez de algo más elaborado (secure storage): el "token" acá es solo un identificador de empleado, no un secreto que proteger.

## Qué falta / mejoras futuras

- Probar en un emulador o dispositivo Android real (bloqueado por tiempo, ver arriba).
- Tests de widgets para los flujos de creación y listado.
- Pull-to-refresh también en la pantalla de nuevo gasto si se agregan más datos dependientes del servidor (ej. categorías dinámicas desde el API en vez de la lista fija actual).
- Validación de formato del código de acceso más rica (hoy solo valida longitud 10 y que sea numérico).
