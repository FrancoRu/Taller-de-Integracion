# Estado del Sistema — Club 12

Análisis de funcionalidades implementadas y pendientes, basado en los requisitos del Informe 1.

---

## Mapeo de funciones del informe

| Función del informe | Estado | Detalle |
|---|---|---|
| **Gestión de Jugadores** | ✅ Completo | Alta, modificación y búsqueda (nombre / DNI) en panel admin |
| **Gestión de Equipos** | ✅ Completo | Creación y composición (altas/bajas) ✅. Estadísticas de equipo (PG/PP) vía tabla de posiciones en panel admin ✅ |
| **Registro de Sanciones** | ✅ Completo | Registrar, seguimiento, modificar ✅. Apelación + registro de decisiones (aceptar/rechazar) ✅ |
| **Registro de Estadísticas** | ✅ Completo | Goleadores ✅, carga de goles y asistencias por jugador en el partido ✅, tabla de posiciones (admin + visitantes) ✅ |
| **Gestión de Usuarios** | ✅ Completo | Registrar, modificar, reset password, eliminar y activar/desactivar (lockout de Identity) ✅ |
| **Visualización visitantes** | ✅ Completo | Equipos, jugadores, torneos, partidos, goleadores, sanciones, clasificaciones/posiciones ✅ |
| **Copias de seguridad** | ❌ | Infraestructura, no necesaria para demo |

---

## Backend (Club12-Backend)

### ✅ Implementado

| Módulo | Endpoints |
|---|---|
| **Auth** | Login, Register, Refresh token, Logout, Magic Link, Password reset |
| **Jugadores** | CRUD completo + filtros (vista pública y admin) |
| **Equipos** | CRUD completo + actualizar logo + filtros |
| **Torneos** | CRUD completo + registrar equipos en torneo + filtros |
| **Divisiones** | CRUD completo + filtros + **posiciones calculadas** (`PositionResponse`: PJ, PG, PP, GF, GC, DIF, Pts) |
| **Fases (Stage)** | CRUD completo + generación automática de fases y partidos + asignar/desasignar equipos |
| **Partidos** | CRUD completo + generación automática + cargar resultado + filtros |
| **Sanciones** | CRUD completo + filtros + apelación/resolución (`SanctionAppealStatus`, requiere migración `AddSanctionAppeal`) |
| **Estadísticas de jugador** | CRUD completo + listado filtrado por partido + tipo Puntos/Asistencias (`StatisticType`, requiere aplicar migración `AddPlayerStatisticType`) |
| **Goleadores** | Ranking por jugador y por equipo |
| **Usuarios** | CRUD completo (incluye DELETE) + activar/desactivar (lockout) + cambio de contraseña + reset de contraseña |
| **Canchas (Venue)** | CRUD completo |
| **Blog/Noticias** | CRUD completo (extra, no requerido por informe) |

### ❌ Faltante

| Funcionalidad | Detalle | Prioridad |
|---|---|---|
| **Copias de seguridad** | Infraestructura, no necesaria para la demo | 🟢 Baja |

---

## Frontend (Club12-WebClient)

### ✅ Implementado — Panel admin (autenticado)

| Sección | Ruta | Estado |
|---|---|---|
| Login / Auth | `/login`, `/auth/password-reset` | ✅ |
| Jugadores | `/panel/jugadores`, `/panel/jugadores/:id` | ✅ |
| Equipo (TeamManager) | `/panel/equipo`, `/panel/equipos/:id` | ✅ |
| Equipos (lista admin) | `/panel/equipos` | ✅ |
| Registro equipos en torneo | `/panel/registro-equipos` | ✅ |
| Torneos | `/panel/torneos`, `/panel/torneos/:id`, `/panel/torneos/:id/editar` | ✅ |
| Divisiones | `/panel/divisiones`, `/panel/divisiones/:id` (detalle, posiciones, fases) | ✅ |
| Fases | `/panel/fases`, `/panel/fases/:id`, `/panel/fases/crear`, `/panel/fases/editar/:id` | ✅ |
| Partidos | `/panel/partidos`, `/panel/partidos/:id` (detalle, carga de goles/asistencias por jugador, sanciones) | ✅ |
| Sanciones | `/panel/sanciones`, `/panel/sanciones/:id` (detalle, apelación, jugador, partido), `/panel/sanciones/editar/:id` | ✅ |
| Goleadores | `/panel/puntuaciones` | ✅ |
| Canchas | `/panel/canchas`, `/panel/canchas/:id` | ✅ |
| Usuarios | `/panel/usuarios`, `/panel/usuarios/:id`, crear, editar, eliminar, activar/desactivar | ✅ |
| Cambiar contraseña | `/panel/configuracion/cambiar-password` | ✅ |
| Editar perfil | `/panel/configuracion/editar-perfil` | ✅ |
| Estadísticas | `/panel/estadisticas` | ✅ |

### ✅ Implementado — Vista pública (no autenticado)

| Sección | Ruta | Estado |
|---|---|---|
| Home | `/` | ✅ |
| Quiénes somos | `/quienes-somos` | ✅ |
| Ficha médica | `/ficha-medica` | ✅ |
| Reglamento | `/reglamento` | ✅ |
| Equipos | `/equipos`, `/equipos/:teamId` | ✅ |
| Goleadores | `/goleadores` | ✅ |
| Sanciones | `/sanciones` | ✅ |
| Partidos | `/partidos` | ✅ |
| Torneos | `/torneos`, `/torneos/:tournamentId` (info, posiciones, equipos, partidos) | ✅ |

### ❌ Faltante / Incompleto

| Funcionalidad | Detalle | Prioridad |
|---|---|---|
| **Apelación de sanciones** | Depende del backend (no existe) | 🟡 Media |
| **Búsqueda pública** | Equipos tiene búsqueda; faltan filtros de búsqueda en vista pública de torneos y jugadores | 🟢 Baja |
| **Blog/Noticias (frontend)** | `addBlogPostForm.tsx` y `showPosts.tsx` existen pero no están en el router | 🟢 Baja |

---

## Resumen ejecutivo

- **Backend:** cubre todas las funciones del informe salvo copias de seguridad (infra). Nota: aplicar las migraciones `AddPlayerStatisticType` y `AddSanctionAppeal` a la base (`dotnet ef database update`).
- **Panel admin (frontend):** completo — posiciones, gestión completa de usuarios (eliminar + activar/desactivar), carga de goles/asistencias por partido y apelación/resolución de sanciones.
- **Vista pública:** completa para los objetivos del informe — equipos, jugadores, torneos, partidos, goleadores, sanciones y clasificaciones/posiciones.

**Estado:** las 7 funciones del informe están implementadas, salvo copias de seguridad (🟢 infra, no necesaria para la demo).

**Mejoras opcionales:** 🟢 búsqueda pública (torneos/jugadores) y blog en el router.

> Nota técnica: goleadores usa la entidad `Scorer` (separada de `PlayerStatistic`). Los goles cargados desde el partido alimentan `PlayerStatistic`, no el ranking de goleadores actual. Unificar ambos sistemas queda como mejora futura.
