# Sistema de Reembolso de Gastos — Consware

Prueba técnica: sistema de reembolso de gastos de empleados con tres partes independientes que comparten un mismo backend.

| Carpeta | Qué es | Detalle |
|---|---|---|
| [`api-net-core/`](api-net-core/README.md) | API REST (ASP.NET Core / .NET 9) — el corazón del sistema, dueña de toda la regla de negocio | [README](api-net-core/README.md) |
| [`app-flutter/`](app-flutter/README.md) | App móvil del empleado (Flutter) | [README](app-flutter/README.md) |
| [`web-angular/`](web-angular/README.md) | Panel web del aprobador (Angular) | [README](web-angular/README.md) |

Cada carpeta tiene su propio README con estructura de carpetas, arquitectura y comandos de ejecución específicos. Este README da la vista general y las decisiones que aplican a todo el sistema.

## Caso de negocio

Un empleado registra un gasto (monto, categoría, descripción). Si el monto es **≤ $500.000 COP** se aprueba automáticamente; si es mayor, queda **Pendiente** y un aprobador debe decidir. Un rechazo siempre requiere motivo. Una solicitud Aprobada o Rechazada no cambia de estado. Esta regla vive **únicamente en el backend** (`ExpenseRequest` en `api-net-core`) — ningún frontend la reimplementa.

## Cómo correr todo (orden recomendado)

1. **API** — es la dependencia de los otros dos. Ver [api-net-core/README.md](api-net-core/README.md).
   ```bash
   cd api-net-core/ExpenseReimbursement.Api
   cp .env.example .env   # credenciales del admin, no se commitea
   dotnet run
   ```
   Aplica las migraciones y crea el archivo SQLite solo, sin pasos manuales. Queda en `http://localhost:5098`, con Swagger en `/swagger`.

2. **Panel Angular** (aprobador) — ver [web-angular/README.md](web-angular/README.md).
   ```bash
   cd web-angular
   npm install
   npm start
   ```
   `http://localhost:4200`, login con `admin` / `admin123` (ver sección de credenciales en su README).

3. **App Flutter** (empleado) — ver [app-flutter/README.md](app-flutter/README.md). Necesita al menos un empleado creado desde el panel Angular (te da un código de 10 dígitos) para poder "iniciar sesión" en la app.
   ```bash
   cd app-flutter
   flutter pub get
   flutter run -d linux   # o el device que tengas disponible
   ```

## Identificación de usuarios (por qué existe un "login")

El enunciado exime de autenticación explícitamente solo a la app del empleado, no al panel del aprobador. Decisiones tomadas al respecto:

- **Empleado (Flutter):** no tiene contraseña. El aprobador lo crea desde Angular (uno por uno o por CSV) y el sistema le genera un **código numérico de 10 dígitos** único, que es lo único que el empleado necesita para identificarse en la app. Sigue el espíritu de "no es necesario implementar autenticación" del enunciado, pero evita que cualquiera pueda registrar gastos a nombre de cualquiera con solo escribir un nombre.
- **Aprobador (Angular):** sí tiene usuario/contraseña, porque aprobar/rechazar y administrar empleados son acciones sensibles. Las credenciales viven **en el API**, en un archivo `.env` (`Admin__Username`/`Admin__Password`, no commiteado — ver `api-net-core/ExpenseReimbursement.Api/.env.example`) — nunca en el frontend, porque cualquier "secreto" puesto en un SPA es visible en el bundle JS. Angular solo llama a `POST /api/auth/login` y reenvía el token que le devuelve el API en cada acción protegida.

## Decisiones técnicas transversales

- **CQRS con Minimal API en vez de Controllers.** El enunciado sugiere Controllers; se usó Minimal API + MediatR (comandos/queries por *vertical slice* en `Features/`) porque es más directo de leer para un proyecto de este tamaño y sigue siendo un CQRS real y explícito, no una capa de servicios disfrazada.
- **SQLite en vez de SQL Server/PostgreSQL.** El enunciado permite cualquier motor SQL. Se eligió SQLite para no depender de levantar un servidor de base de datos (ni con Docker) dentro de una prueba de tiempo limitado. Sigue siendo SQL real vía EF Core (migraciones, LINQ-to-SQL, relaciones con FK), portable a SQL Server cambiando un `UseSqlite` por `UseSqlServer` y la cadena de conexión.
- **Sin JWT.** La autenticación del admin usa un token opaco en memoria (se pierde si el API se reinicia) en vez de JWT, para no sumar una librería y su configuración a una prueba de 4 horas. Documentado como mejora futura.

## Qué quedó fuera por tiempo

- Tests automatizados (unitarios/integración) en los tres proyectos.
- Paginación en los listados (API, Angular, Flutter) — con pocos datos no hacía falta, pero no escala.
- JWT real con expiración/refresh en vez del token opaco en memoria.
- Registro de auditoría a nivel de base de datos (quién aprobó/rechazó cada solicitud) — hoy solo se guarda la fecha de decisión.
- Internacionalización / soporte multi-moneda (todo asume COP).
- CI/CD y contenedores (Docker) para los tres proyectos.

## Uso de IA

Usé **Claude Code** de punta a punta durante toda la sesión: para escribir el esqueleto de los tres proyectos (API en .NET, app Flutter, panel Angular), la lógica de la regla de negocio, la integración entre frontends y backend, y para depurar problemas de entorno (SDK de .NET 10 roto en Arch, instalación de Flutter, imágenes de emulador Android, etc.). Las decisiones de arquitectura, alcance y qué simplificar (SQLite, Minimal API, el modelo de autenticación con código de 10 dígitos para empleados) las tomé yo y se las indiqué explícitamente a la IA; también validé cada endpoint con `curl` y probé la app Flutter/el panel Angular corriendo de verdad antes de darlos por buenos.
