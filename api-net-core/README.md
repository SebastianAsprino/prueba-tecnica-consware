# API — Reembolso de Gastos

ASP.NET Core Minimal API (.NET 9) con CQRS (MediatR) y SQLite (EF Core). Es la única capa donde vive la regla de negocio.

## Arquitectura

**CQRS por *vertical slice*, no por capa técnica.** En vez de `Controllers/` + `Services/` + `Repositories/`, cada operación de negocio es un archivo autocontenido en `Features/` con su Command/Query, su Handler y el mapeo del endpoint. `Program.cs` es el composition root: registra servicios y llama a los `Map*Endpoints()` de cada feature.

```
ExpenseReimbursement.sln         Solución: API + proyecto de tests
ExpenseReimbursement.Api.Tests/  Tests xUnit (dominio, auth, handlers contra SQLite en memoria)
ExpenseReimbursement.Api/
  Program.cs                     Composition root: DI, CORS, rate limiting, migraciones automáticas, mapeo de endpoints
  Domain/
    Employee.cs                  Entidad empleado (Id, Name, AccessCode de 10 dígitos)
    ExpenseRequest.cs             Entidad gasto + reglas de negocio (auto-aprobación, transiciones de estado)
    ExpenseStatus.cs               enum Pending/Approved/Rejected
  Infrastructure/
    AppDbContext.cs                DbContext + configuración de EF (índices únicos, FKs, conversiones)
  Migrations/                     Migraciones EF Core (se aplican solas al arrancar)
  Contracts/                      DTOs de entrada/salida (Create/Reject/Login/Employee/ExpenseRequest)
  Common/
    AppResult.cs / AppResultExtensions.cs   Resultado tipado (Ok/NotFound/ValidationError/Conflict/Unauthorized) → HTTP correcto
    AdminOptions.cs / AdminTokenStore.cs / AdminAuthFilter.cs   Autenticación del aprobador (token opaco en memoria)
    SecureCompare.cs               Comparación de credenciales en tiempo constante
    RateLimitingPolicies.cs        Límite de intentos por IP en login y búsqueda por código
    AccessCodeGenerator.cs         Genera el código de 10 dígitos único de cada empleado
  Features/
    Auth/Login.cs                  POST /api/auth/login
    Employees/                     Crear, listar, importar CSV, buscar por código
    ExpenseRequests/               Crear, listar, consultar por id, aprobar, rechazar
```

## Modelo de datos

- **Employee**: `Id`, `Name` (único), `AccessCode` (10 dígitos, único, autogenerado), `CreatedAt`.
- **ExpenseRequest**: `Id`, `EmployeeId` (FK), `Amount`, `Category`, `Description`, `Status`, `AutoApproved`, `RejectionReason`, `CreatedAt`, `DecidedAt`.
- Regla de negocio (en `ExpenseRequest.Create` / `TryApprove` / `TryReject`, no en los handlers ni en ningún frontend):
  - `Amount <= 500_000` → se crea `Approved` y `AutoApproved = true` de una vez.
  - `Amount > 500_000` → se crea `Pending`.
  - `TryApprove`/`TryReject` solo tienen éxito si el estado actual es `Pending` (si no, error de conflicto).
  - Rechazar sin motivo se valida en el handler antes de tocar la entidad (400, no 409).
- Longitudes máximas (nombre 200, categoría 100, descripción y motivo de rechazo 500) definidas como constantes en las entidades y usadas tanto por EF Core como por la validación de los handlers (400 si se superan).

## Autenticación

Dos identidades distintas, ninguna con contraseña real de "usuario de base de datos":

| Quién | Cómo se identifica | Protege |
|---|---|---|
| Aprobador (Angular) | `POST /api/auth/login` con usuario/clave configurados en `Admin:Username`/`Admin:Password` → token opaco (`X-Admin-Token`) | Aprobar, rechazar, crear/importar empleados, listar empleados |
| Empleado (Flutter) | Código numérico de 10 dígitos (`AccessCode`) que el aprobador le entrega al crearlo | Nada — es solo identificación, no hay endpoints protegidos por empleado |

Las credenciales viven en un archivo **`.env`** en la raíz de `ExpenseReimbursement.Api/` (cargado al arrancar con `DotNetEnv`, antes de construir la configuración de ASP.NET Core), **no** en `appsettings.json` — así nunca quedan commiteadas en texto plano. El repo trae `.env.example` (sin secreto real) como plantilla; `.env` está en `.gitignore`.

```bash
# api-net-core/ExpenseReimbursement.Api/.env
Admin__Username=admin
Admin__Password=una-clave-propia
```

Medidas de seguridad sobre el login y el código de acceso:

- Usuario y contraseña se comparan en **tiempo constante** (`SecureCompare`, hash SHA-256 + `CryptographicOperations.FixedTimeEquals`), para no filtrar por timing cuántos caracteres coinciden.
- `POST /api/auth/login` y `GET /api/employees/by-code/{code}` tienen **rate limiting** por IP (20 intentos por minuto, luego `429 Too Many Requests`) para frenar la fuerza bruta sobre la clave del admin y sobre los códigos de 10 dígitos.
- Los tokens se generan con `RandomNumberGenerator` (256 bits) y los vencidos se purgan al emitir uno nuevo.

El doble guion bajo (`Admin__Username`) es la convención estándar de ASP.NET Core para mapear a `Admin:Username` en la configuración anidada — por eso `DotNetEnv` (que solo pone variables de entorno) funciona sin código adicional aparte de `Env.Load()` en `Program.cs`.

## Endpoints

| Método | Ruta | Auth | Notas |
|---|---|---|---|
| POST | `/api/auth/login` | — | `{ username, password }` → `{ token }`. 401 si son incorrectas. |
| GET | `/api/employees` | Admin | Lista con `id`, `name`, `accessCode`. |
| POST | `/api/employees` | Admin | `{ name }` → crea y genera el código. 400 si el nombre ya existe. |
| POST | `/api/employees/import` | Admin | `multipart/form-data`, campo `file` (CSV, columna `Name`, un nombre por línea). Devuelve creados + omitidos con motivo. |
| GET | `/api/employees/by-code/{code}` | — | Lo usa Flutter para "iniciar sesión". 404 si el código no existe. |
| POST | `/api/expense-requests` | — | `{ employeeId, amount, category, description }`. 404 si el empleado no existe, 400 si algo falla validación. |
| GET | `/api/expense-requests?status=&employeeId=` | — | Filtros opcionales, ambos combinables. |
| GET | `/api/expense-requests/{id}` | — | 404 si no existe. |
| POST | `/api/expense-requests/{id}/approve` | Admin | 409 si no está `Pending`. |
| POST | `/api/expense-requests/{id}/reject` | Admin | `{ reason }` obligatorio (400 si falta) — se valida **antes** de comprobar el estado (404 si el id no existe tiene prioridad). |

Todas las respuestas de error usan la forma `{ "errors": ["..."] }` con el código HTTP correspondiente (400/401/404/409).

## Cómo correr

Requisitos: **.NET 9 SDK** (el repo se generó apuntando a `net9.0`; si tienes .NET 10 instalado y falla el restore por un bug de paquete en algunas distros, confirma que `dotnet --list-sdks` incluye la 9.x).

`.env` está en `.gitignore` (no se commitea, son credenciales), así que el primer paso al clonar el repo es crearlo a partir de la plantilla:

```bash
cd api-net-core/ExpenseReimbursement.Api
cp .env.example .env   # define Admin__Username / Admin__Password (la plantilla trae admin / change-me)
dotnet run
```

- No hay ningún paso manual de creación de base de datos: al arrancar, `Program.cs` llama a `Database.Migrate()` y crea `expense-reimbursement.db` (SQLite) si no existe.
- Por defecto queda en `http://localhost:5098` (perfil `http` de `launchSettings.json`). Swagger UI en `http://localhost:5098/swagger`.
- CORS: sin configuración permite cualquier origen (cómodo en desarrollo local con los otros dos proyectos; la API no usa cookies, el token viaja en un header). Para restringirlo, define `Cors__AllowedOrigins__0=http://localhost:4200` (y `__1`, `__2`… si hay más) en `.env`.
- `ExpenseReimbursement.Api.http` trae ejemplos de todas las rutas para el cliente HTTP de VS Code / Rider / Visual Studio.

### Tests

```bash
cd api-net-core
dotnet test
```

Cubren la regla de negocio (`ExpenseRequest`), el generador de códigos, el almacén de tokens (con `FakeTimeProvider` para la expiración), la comparación en tiempo constante, el login y los handlers de empleados/solicitudes contra una base **SQLite en memoria** (mismo motor que en ejecución, no un mock).

### Probar rápido con curl

```bash
# login de admin
curl -X POST http://localhost:5098/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"<tu-clave-del-.env>"}'

# crear empleado (usa el token del paso anterior)
curl -X POST http://localhost:5098/api/employees \
  -H "Content-Type: application/json" -H "X-Admin-Token: <token>" \
  -d '{"name":"Ana"}'
```

## Decisiones técnicas

- **Minimal API + MediatR en vez de Controllers.** Vertical slices por feature, sin capas de servicios intermedias que no aportan nada a este tamaño de proyecto.
- **SQLite en vez de SQL Server/PostgreSQL.** Ver justificación en el README raíz — es SQL real vía EF Core, no una simplificación a memoria.
- **`AppResult<T>` en vez de excepciones para control de flujo.** Los handlers devuelven un resultado tipado (Ok/NotFound/ValidationError/Conflict/Unauthorized) que una extensión mapea a `IResult` — evita que un `try/catch` genérico en el endpoint oculte el código HTTP real que debería devolver cada caso.
- **Token de admin opaco en memoria en vez de JWT.** Válido durante la vida del proceso (8h de expiración simulada); se pierde si el API se reinicia (el aprobador debe volver a loguearse). Suficiente para la prueba, no para producción.
- **Código de acceso de 10 dígitos para empleados en vez de "nombre libre" o contraseña.** Balance entre el "no hace falta autenticación" del enunciado y evitar que cualquiera pueda crear gastos a nombre de otro empleado con solo escribir su nombre.

## Qué falta / mejoras futuras

- JWT con expiración real + refresh token en vez del token opaco en memoria.
- Tests de integración HTTP (WebApplicationFactory) sobre los endpoints completos (hoy se prueban dominio y handlers).
- Paginación en `GET /api/expense-requests` y `GET /api/employees`.
- Endpoint para revocar/rotar el código de acceso de un empleado sin borrar su historial.
