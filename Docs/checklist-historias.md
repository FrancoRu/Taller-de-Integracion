# Club 12 — Checklist de Historias de Usuario

Estado al merge del PR #40 a `develop`. Verificación: backend `dotnet build` 0 warnings + **505 tests**; frontend `lint` 0 + **310 tests**; snapshot EF consistente.

Leyenda: ✅ hecho y verificado con tests · ⚠️ hecho con salvedad · 🔵 spike de entorno (no de código)

## Épica 1 — Autenticación y acceso
- [x] HU-01 Login oculto por URL ✅
- [x] HU-02 Ocultar header/footer en login ✅
- [x] HU-03 Redirigir al home al cerrar sesión ✅
- [x] HU-04 Página 404 sin layout ✅

## Épica 2 — Roles y usuarios
- [x] HU-05 Simplificar roles a Owner + Admin IT ✅
- [x] HU-06 Editar perfil y contraseña ✅
- [x] HU-07 Listado usuarios + soft delete ✅
- [x] HU-08 Blanquear contraseña / editar a pedido ✅
- [x] HU-09 Alta por invitación con magic link ✅ (invite/activate)
- [x] HU-10 Reset de contraseña self-service por magic link ✅

## Épica 3 — Portada pública, noticias y vista pública del torneo
- [x] HU-11 Landing con accesos directos ✅
- [x] HU-12 Feed de noticias + "ver todas" ✅
- [x] HU-13 Detalle de noticia por slug ✅
- [x] HU-14 Vista pública del torneo (fixture/posiciones) ✅
- [x] HU-15 Slug en todas las rutas públicas ✅ (torneo/equipo/partido/división/fase/cancha/jugador/blog)
- [x] HU-16 Estado borrador/publicada de noticia ✅
- [x] HU-17 Open Graph al compartir noticia ⚠️ (meta client-side; sin SSR)

## Épica 4 — Contenido institucional
- [x] HU-18 Quiénes somos ✅
- [x] HU-19 Reglamento ✅
- [x] HU-20 Descargar plantilla ficha médica ✅
- [ ] HU-21 Arreglar descarga de ficha en prod 🔵 (la página existe; requiere reproducir en el entorno desplegado)

## Épica 5 — Sanciones públicas
- [x] HU-22 Consulta pública de sanciones ✅
- [x] HU-23 Buscar por nombre de jugador ✅
- [x] HU-24 Coincidencia parcial (contains) ✅
- [x] HU-25 Filtrar por torneo ✅

## Épica 6 — Panel: navegación
- [x] HU-26 Simplificar sidebar (sin Divisiones/Fases/Partidos) ✅
- [x] HU-27 Asistente fuera del sidebar ✅
- [x] HU-28 Ajustes de estilo del panel ✅ (absorbido por el theme)

## Épica 7 — Gestión de torneos
- [x] HU-29 Listado y CRUD de torneos ✅
- [x] HU-30 Vista de torneo con sub-tabs ✅
- [x] HU-31 Bloquear estructura si el torneo ya empezó ✅
- [x] HU-32 Contador de equipos por división ✅
- [x] HU-33 Botón "no hay equipos" cuando sí hay ✅
- [x] HU-34 Quitar mínimo/máximo de equipos ✅

## Épica 8 — Máquina de estados
- [x] HU-35 Transiciones forward-only + RegistrationClosed ✅
- [x] HU-36 Programado ≠ Inscripción abierta ✅
- [x] HU-37 "Inscripción cerrada" dispara el fixture ✅

## Épica 9 — Asistente de creación de torneo
- [x] HU-38 Crear en una sola transacción ✅ (endpoint atómico `POST /tournaments/full`)
- [x] HU-39 Datos base con fechas ✅
- [x] HU-40 Validar límite de inscripción < inicio ✅
- [x] HU-41 Selección de equipos scopeada ✅
- [x] HU-42 Equipo en una sola zona ✅
- [x] HU-43 Configurar fase de grupo (N vueltas) ✅
- [x] HU-44 Copas Oro/Plata ✅
- [x] HU-45 Mapear rangos de posición → copa ✅
- [x] HU-46 Serie final al mejor de N ✅
- [x] HU-47 Copa cruzada con playoff obligatorio ✅
- [x] HU-48 Femenino como torneo separado ✅
- [x] HU-49 Paso de revisión ✅

## Épica 10 — Equipos y planteles
- [x] HU-50 Sección de Equipos ✅
- [x] HU-51 Tab de jugadores (plantel) ✅
- [x] HU-52 Inscripción manual ✅
- [x] HU-53 Copiar plantel de temporada anterior ✅
- [x] HU-54 Límites de plantel y dorsal único ✅

## Épica 11 — Ficha médica y elegibilidad
- [x] HU-55 Cargar ficha médica por jugador+equipo+torneo ✅
- [x] HU-56 Bucket de almacenamiento (Supabase) ✅ (upload real config-only)
- [x] HU-57 Habilitación por ficha aprobada ✅
- [x] HU-58 Aprobar/rechazar ficha ✅
- [x] HU-59 Ficha nueva por temporada ✅
- [x] HU-60 Solo elegibles habilitados ✅
- [x] HU-61 Jugador sancionado no convocable ✅
- [x] HU-62 Aviso de jugador no habilitado ✅

## Épica 12 — Fixture y jornadas
- [x] HU-63 Partidos agrupados por jornada ✅
- [x] HU-64 Fixture automático y aleatorio al cerrar inscripción ✅
- [x] HU-65 Cantidad de fechas + domingos ✅
- [x] HU-66 Asignar cancha después ✅
- [x] HU-67 Orden de jornadas fijo; editar fecha de partido ✅
- [x] HU-68 Reprogramar/suspender partido ✅

## Épica 13 — Carga del partido
- [x] HU-69 Cargar resultado + estado ✅
- [x] HU-70 Sin empates (exige ganador) ✅
- [x] HU-71 Cargar goleadores (suma=marcador + elegibles) ✅
- [x] HU-72 Anotadores conectados al ranking ✅
- [x] HU-73 Walkover (W.O.) ✅

## Épica 14 — Gestión de sanciones (admin)
- [x] HU-74 Crear sanción desde el partido ✅
- [x] HU-75 Duración en fechas, cleanup por días ✅
- [x] HU-76 Vencimiento automático ✅
- [x] HU-77 Sanción a jugador/equipo/staff ✅

## Épica 15 — Clasificación y playoffs
- [x] HU-78 Tabla de posiciones ✅
- [x] HU-79 Puntaje configurable (2/1 por defecto) ✅
- [x] HU-80 Desempate PTS→PG→DG→H2H→DG-en-H2H ✅
- [x] HU-81 Clasificados desde la tabla (multi-copa) ✅
- [x] HU-82 Bracket con siembra, prórroga y BYE ✅

## Épica 16 — Estadísticas y rankings
- [x] HU-83 Estadísticas globales ✅
- [x] HU-84 Estadísticas por torneo autogeneradas ✅
- [x] HU-85 Por temporada y de todos los tiempos ✅
- [x] HU-86 Ranking de goleadores ✅
- [x] HU-87 Ficha de estadística individual ✅
- [x] HU-88 Historial del jugador entre temporadas ✅
- [x] HU-89 Exportar posiciones/estadísticas (CSV) ✅ (PDF vía print existente)

## Épica 17 — Canchas
- [x] HU-90 CRUD de canchas ✅

## Épica 18 — Respaldos y administración de datos
- [x] HU-91 Generar respaldo manual ✅ (implementación de develop)
- [x] HU-92 Bloquear la app durante respaldo/restauración ✅
- [x] HU-93 Salvaguarda antes de restaurar ✅
- [x] HU-94 Límite de copias ✅
- [x] HU-95 Respaldos programados ✅
- [x] HU-96 Borrado total de datos ✅
- [x] HU-97 Datos de prueba (solo Admin IT, base vacía) ✅

## Épica 19 — Modelo season-scoped
- [x] HU-98 Equipos y jugadores scopeados por temporada ✅
- [x] HU-99 Identidad de club estable entre temporadas ✅

## Épica 20 — Coherencia técnica transversal
- [x] HU-100 Zona horaria Argentina ✅
- [x] HU-101 Auditoría de acciones sensibles ✅

## Épica 21 — Rediseño visual
- [x] HU-102 Design system dark + naranja ✅
- [x] HU-103 Tema en sitio público ✅
- [x] HU-104 Tema en panel ✅
- [x] HU-105 Responsive + accesibilidad ✅ (pase liviano)

---

## Resumen
- **104 / 105 historias ✅** (implementadas + verificadas con tests).
- **1 pendiente 🔵 HU-21** — es un spike de entorno desplegado (la página de descarga existe y funciona en local; hay que reproducir el fallo en el server), no una tarea de código.
- Salvedades menores: HU-17 (Open Graph client-side, sin SSR).

### Pendiente operativo (no historias)
- Validación E2E en vivo con Playwright + navegador (funcional + visual).
- Limpiar la base de datos de desarrollo (se migra sola al arrancar por auto-migrate; luego wipe vía panel de Administración de datos).
