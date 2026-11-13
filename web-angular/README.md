# Panel Angular — Aprobador

Panel web que usa el aprobador para revisar solicitudes, gestionar empleados y ver el historial de movimientos. Angular 22 standalone (sin `NgModule`), sin Router (no hace falta con 3 vistas detrás de un login) y sin NgRx (el estado de cada vista es local, con signals).

## Arquitectura

```
src/app/
  app.ts / app.html               Shell: gate de login + tabs (Solicitudes / Empleados / Historial)
  app.config.ts                    Providers globales: HttpClient + interceptor de auth
  core/
    auth.service.ts                 Login, guarda el token en localStorage, signal isLoggedIn
    auth.interceptor.ts             Interceptor funcional: agrega X-Admin-Token a cada request, desloguea en 401
    api-config.ts                   URL base del API
    employee.service.ts / employee.model.ts
    expense-request.service.ts / expense-request.model.ts
  features/
    login/                          Formulario usuario/clave
    requests-table/                 Tabla de solicitudes, filtro por estado (default Pendientes), aprobar/rechazar
    employees/                       Crear empleado, importar CSV, ver código de acceso de cada uno
    historial/                       Historial completo con filtro por empleado/movimiento/mes y export a CSV
```

Cada feature es un componente standalone independiente; `app.ts` decide cuál mostrar con un signal `activeTab`, sin necesidad de `@angular/router` para 3 pantallas.

## Autenticación

El login **no es cosmético**: `login.ts` llama a `POST /api/auth/login` en el API (las credenciales reales viven ahí, no en este proyecto — ver README de `api-net-core`). Si son correctas, el API devuelve un token que:
- se guarda en `localStorage` vía `AuthService`,
- se adjunta automáticamente como header `X-Admin-Token` en cada petición saliente gracias a `authInterceptor`,
- si el API responde 401 (token vencido o inválido), el interceptor desloguea automáticamente y vuelve a mostrar el login.

Credenciales: las que definas en el `.env` del API (`Admin__Username` / `Admin__Password`; la plantilla `.env.example` trae `admin` / `change-me`).

## Funcionalidades por pestaña

- **Solicitudes** — tabla con filtro por estado (Pendiente/Aprobada/Rechazada/Todas, default Pendiente). Aprobar es un botón directo; rechazar abre un modal que exige motivo antes de habilitar el submit (también validado en el backend).
- **Empleados** — crear un empleado (nombre) o importar varios por CSV (una columna `Name`, un nombre por línea). Muestra el código de 10 dígitos de cada uno para que el aprobador se lo entregue al empleado (es lo único que necesita para usar la app Flutter).
- **Historial** — todas las solicitudes sin importar estado, con filtro por nombre de empleado, por tipo de movimiento (Pendiente/Auto-aprobado/Aprobado/Rechazado — distingue auto-aprobación de aprobación manual usando el campo `autoApproved` del API) y por mes, más un botón para exportar el resultado filtrado a CSV.

## Cómo correr

Requisitos: **Node ≥ 22.22.3 / 24.15+ / 26** (Angular CLI 22 lo exige; si tu Node es más viejo, `nvm install --lts` antes de instalar dependencias). La **API debe estar corriendo primero** (`api-net-core`, puerto `5098` por defecto — ver `core/api-config.ts` si cambia).

```bash
cd web-angular
npm install
npm start          # equivalente a: ng serve
```

Tests unitarios (Vitest vía `ng test`):

```bash
npm test -- --watch=false
```

Queda en `http://localhost:4200`. CSS con Tailwind v4 + DaisyUI (tema `silk`), configurado en `.postcssrc.json` y `src/styles.css`.

## Decisiones técnicas

- **Sin `@angular/router`.** Con 3 vistas planas detrás de un único gate de login, un signal (`activeTab`) alcanza; sumar Router aquí sería complejidad sin beneficio real.
- **Sin NgRx/estado global.** Cada feature carga sus propios datos con `HttpClient` + signals; no hay estado compartido entre pestañas que lo justifique.
- **Login real contra el API, no un check cosmético en el cliente.** Cualquier "clave" puesta solo en el bundle de un SPA es visible por DevTools — el chequeo real ocurre en el backend (ver decisión equivalente en el README del API).
- **Modal de rechazo en vez de `window.prompt`.** Mejor UX y consistente con el resto de la interfaz (Tailwind/DaisyUI) que el prompt nativo del navegador usado en una iteración inicial.

## Qué falta / mejoras futuras

- Guards de ruta reales si en algún momento se agrega `@angular/router` (hoy la protección de vistas es solo un `@if` sobre `isLoggedIn()`, suficiente porque todas las llamadas HTTP igual exigen el token del backend).
- Paginación en las tablas de Solicitudes/Historial cuando el volumen de datos crezca.
- Más tests unitarios de componentes y del interceptor de auth (hoy hay tests del componente raíz y del helper de errores `core/http-error.ts`).
- Editar/eliminar un empleado (hoy solo se puede crear).
