# Estado del Sistema — Club 12

Análisis de funcionalidades implementadas y pendientes, basado en los requisitos del Informe 1.

---

## Backend (Club12-Backend)

### ✅ Implementado

| Módulo | Endpoints |
|---|---|
| **Auth** | Login, Register, Refresh token, Logout, Magic Link, Password reset |
| **Jugadores** | CRUD completo + filtros (vista pública y admin) |
| **Equipos** | CRUD completo + actualizar logo + filtros |
| **Torneos** | CRUD completo + registrar equipos en torneo + filtros |
| **Divisiones** | CRUD completo + filtros |
| **Fases (Stage)** | CRUD completo + generación automática de fases y partidos + asignar/desasignar equipos |
| **Partidos** | CRUD completo + generación automática + cargar resultado + filtros |
| **Sanciones** | CRUD completo + filtros |
| **Estadísticas de jugador** | CRUD completo |
| **Goleadores** | Ranking por jugador y por equipo |
| **Usuarios** | CRUD completo + cambio de contraseña + reset de contraseña |
| **Canchas (Venue)** | CRUD completo |
| **Blog/Noticias** | CRUD completo (extra, no requerido por informe) |

### ❌ Faltante

| Funcionalidad | Prioridad |
|---|---|
| **Copias de seguridad** | Baja — es infraestructura, no es necesaria para la demo |

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
| Divisiones | `/panel/divisiones`, `/panel/divisiones/:id` | ✅ |
| Fases | `/panel/fases`, `/panel/fases/:id`, `/panel/fases/crear`, `/panel/fases/editar/:id` | ✅ |
| Partidos | `/panel/partidos`, `/panel/partidos/:id` | ✅ |
| Sanciones | `/panel/sanciones`, `/panel/sanciones/:id`, `/panel/sanciones/editar/:id` | ✅ |
| Goleadores | `/panel/puntuaciones` | ✅ |
| Canchas | `/panel/canchas`, `/panel/canchas/:id` | ✅ |
| Usuarios | `/panel/usuarios`, `/panel/usuarios/:id`, crear, editar | ✅ |
| Cambiar contraseña | `/panel/configuracion/cambiar-password` | ✅ |
| Editar perfil | `/panel/configuracion/editar-perfil` | ✅ |

### ✅ Implementado — Vista pública (no autenticado)

| Sección | Ruta | Estado |
|---|---|---|
| Home | `/` | ✅ |
| Quiénes somos | `/quienes-somos` | ✅ |
| Ficha médica | `/ficha-medica` | ✅ |
| Reglamento | `/reglamento` | ✅ |

### ❌ Faltante / Incompleto

| Funcionalidad | Detalle | Prioridad |
|---|---|---|
| **Vista pública de torneos** | Visitante no puede ver torneos, fixtures, ni posiciones sin loguearse | 🔴 Alta |
| **Vista pública de partidos** | No hay ruta pública para ver partidos ni resultados | 🔴 Alta |
| **Vista pública de equipos** | Visitante no puede ver equipos ni sus jugadores | 🔴 Alta |
| **Vista pública de goleadores** | Ranking de goleadores no accesible sin login | 🔴 Alta |
| **Vista pública de sanciones** | Visitante no puede consultar sanciones activas | 🟡 Media |
| **Estadísticas (panel)** | `/panel/estadisticas` es un `PlaceholderPage`, no implementado | 🟡 Media |
| **Crear sanción** | Existe `playerSanctionCreatePage.tsx` pero no está en el router | 🟡 Media |
| **Eliminar sanción** | Existe `playerSanctionDeletePage.tsx` pero no está en el router | 🟡 Media |
| **Estadísticas por jugador** | `playerStatisticPage.tsx` y `playerStatisticsPage.tsx` existen pero no están en el router | 🟡 Media |
| **Blog/Noticias (frontend)** | `addBlogPostForm.tsx` y `showPosts.tsx` existen pero no están en el router | 🟢 Baja |

---

## Resumen ejecutivo

- **Backend:** prácticamente completo. Todos los módulos del informe tienen endpoints funcionales.
- **Panel admin (frontend):** completo para las funciones de administración.
- **Vista pública:** es el mayor pendiente. El visitante actualmente solo ve la home estática y páginas informativas. No puede consultar torneos, partidos, posiciones, equipos ni goleadores sin autenticarse — que era uno de los objetivos principales del sistema.

**Foco para presentación:** implementar las rutas y páginas públicas para visitantes.
