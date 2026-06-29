# Estado del Sistema — Club 12

Análisis de funcionalidades implementadas y pendientes, basado en los requisitos del Informe 1.

---

## Mapeo de funciones del informe

| Función del informe | Estado | Detalle |
|---|---|---|
| **Gestión de Jugadores** | ✅ Completo | Alta, modificación y búsqueda (nombre / DNI) en panel admin |
| **Gestión de Equipos** | ✅ Completo | Creación y composición (altas/bajas) ✅. Estadísticas de equipo (PG/PP) vía tabla de posiciones en panel admin ✅ |
| **Registro de Sanciones** | ⚠️ Parcial | Registrar, seguimiento, modificar ✅. **Apelación de sanciones — falta** |
| **Registro de Estadísticas** | ⚠️ Parcial | Goleadores ✅, puntos por jugador ✅, tabla de posiciones (admin + visitantes) ✅. Asistencias — no diferenciadas |
| **Gestión de Usuarios** | ⚠️ Parcial | Registrar, modificar, reset password ✅. **Eliminar (no wired) / activar-desactivar (no existe) — falta** |
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
| **Sanciones** | CRUD completo + filtros |
| **Estadísticas de jugador** | CRUD completo |
| **Goleadores** | Ranking por jugador y por equipo |
| **Usuarios** | CRUD completo (incluye DELETE) + cambio de contraseña + reset de contraseña |
| **Canchas (Venue)** | CRUD completo |
| **Blog/Noticias** | CRUD completo (extra, no requerido por informe) |

### ❌ Faltante

| Funcionalidad | Detalle | Prioridad |
|---|---|---|
| **Activar / desactivar usuario** | El informe pide inhabilitar/habilitar cuentas. No existe endpoint (solo DELETE permanente) | 🟡 Media |
| **Apelación de sanciones** | El informe lo pide explícito. No existe entidad ni endpoint | 🟡 Media |
| **Asistencias** | Las estadísticas no diferencian goles de asistencias (valor genérico) | 🟢 Baja |
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
| Partidos | `/panel/partidos`, `/panel/partidos/:id` (detalle, puntuaciones por jugador, sanciones) | ✅ |
| Sanciones | `/panel/sanciones`, `/panel/sanciones/:id`, `/panel/sanciones/editar/:id` | ✅ |
| Goleadores | `/panel/puntuaciones` | ✅ |
| Canchas | `/panel/canchas`, `/panel/canchas/:id` | ✅ |
| Usuarios | `/panel/usuarios`, `/panel/usuarios/:id`, crear, editar | ⚠️ Sin eliminar / activar-desactivar |
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
| **Eliminar usuario** | El backend expone DELETE pero el contexto de usuario del frontend no lo expone ni hay acción en la UI | 🟡 Media |
| **Activar / desactivar usuario** | Depende del endpoint de backend (no existe) | 🟡 Media |
| **Apelación de sanciones** | Depende del backend (no existe) | 🟡 Media |
| **Búsqueda pública** | Equipos tiene búsqueda; faltan filtros de búsqueda en vista pública de torneos y jugadores | 🟢 Baja |
| **Blog/Noticias (frontend)** | `addBlogPostForm.tsx` y `showPosts.tsx` existen pero no están en el router | 🟢 Baja |

---

## Resumen ejecutivo

- **Backend:** cubre casi todo el informe. Pendientes reales: activar/desactivar usuario y apelación de sanciones (ambos requeridos por el informe), asistencias diferenciadas y copias de seguridad.
- **Panel admin (frontend):** funcional, incluye tabla de posiciones. Hueco restante: gestión completa de usuarios (eliminar / activar-desactivar).
- **Vista pública:** completa para los objetivos del informe — equipos, jugadores, torneos, partidos, goleadores, sanciones y clasificaciones/posiciones.

**Foco recomendado para la presentación:**
1. 🟡 Gestión de usuarios: eliminar + activar/desactivar.
2. 🟡 Apelación de sanciones.
