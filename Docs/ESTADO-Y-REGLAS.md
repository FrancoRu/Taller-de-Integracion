# Club 12 — Estado, reglas de negocio y contexto operativo

> Doc único que reemplaza `HANDOFF.md`, `REVISION-FRANCO.md`, `estado-sistema.md`,
> `ux-overhaul-backlog.md`, `REGLAS-DE-NEGOCIO.md` y `checklist-historias.md`
> (consolidados el 2026-09-02). Esos seis archivos eran notas de trabajo con
> contenido solapado/contradictorio entre sí y desactualizado respecto al código
> real — la historia PR-por-PR ya vive en `git log`; esto es lo que sobrevive de
> valor: reglas de negocio, gotchas operativos y estado real de cobertura
> funcional.
>
> Para la especificación completa de cada historia de usuario (criterios de
> aceptación) ver [`historias-de-usuario.md`](./historias-de-usuario.md). Para
> arquitectura e instalación, [`README.md`](../README.md). Para el manual de
> usuario final, [`MANUAL_USUARIO.md`](../MANUAL_USUARIO.md). Para despliegue,
> [`DEPLOYMENT.md`](../DEPLOYMENT.md).

Rama de integración: **`develop`** (deploya automáticamente a staging,
club12.argentum-solutions.com.ar vía GitHub Actions). `main` no se toca directo.
Todo texto de cara al usuario en **español (voseo)**; código e identificadores
en inglés.

---

## 1. Reglas de negocio del dominio

Estas reglas gobiernan qué se muestra/permite en cada estado — la UI debe
respetarlas de forma consistente en vez de redescubrirlas bug por bug.

### 1.1 Ciclo de vida del Torneo (state machine)

```
Scheduled → OpenForRegistration → RegistrationClosed → Ongoing → Finished
```
(+ `Canceled` desde cualquier estado no terminal; `Finished`/`Canceled` son
terminales). Excepción: `Ongoing → RegistrationClosed` ("Revertir a borrador"),
permitida solo si **ningún partido fue jugado** — borra el fixture y conserva
las asignaciones de equipos a zona.

| Acción / UI | Scheduled | OpenForReg | RegClosed | Ongoing | Finished |
|---|---|---|---|---|---|
| Editar datos del torneo | ✅ | ✅ | ✅ | ✅ (limitado) | ❌ |
| Abrir inscripción | ✅→ | — | — | — | — |
| Inscribir/dar de baja equipos | ❌ | ✅ | ❌ | ❌ | ❌ |
| Nueva división / editar estructura | ✅ | ✅ | ✅ | ❌ | ❌ |
| Asignar equipos a zonas (draft) | ❌ | ✅ | ✅ | ❌ | ❌ |
| Iniciar torneo (cierra insc. + genera fixture) | ❌ | ✅* | ✅ | — | — |
| Revertir a borrador (borra fixture) | — | — | — | ✅ (si 0 jugados) | ❌ |
| Cargar resultados / goleadores | ❌ | ❌ | ❌ | ✅ | ✅ (correcciones) |
| Campeones resueltos | — | — | — | parcial | ✅ |

\* "Iniciar torneo" desde `OpenForRegistration` cierra la inscripción y arranca
en la misma acción (encadena las dos transiciones).

**Regla clave:** el fixture (los partidos) se genera SOLO al pasar a `Ongoing`.
Antes de eso no existen partidos — por eso mover equipos de zona antes de
iniciar el torneo es una operación limpia (no hay fixture que regenerar).

### 1.2 División / Zonas

- Una **División = un "tier"** (Zona A, B, C, Primera, Reserva…). Un equipo
  juega en UNA sola zona regular ("un equipo, una zona"); la copa cruzada es
  una membresía paralela e independiente.
- Cada división corre una **fase de grupos** (round-robin, 1 o 2 vueltas
  configurable) y luego **playoffs por sub-copa** (Oro, Plata, Bronce…),
  sembrados por rango de posición final de la fase de grupos
  (`DivisionPlayoffMapping`, ej. 1-4 → Oro, 5-8 → Plata).
- Las standings siembran a TODOS los equipos asignados en 0-0 desde que el
  torneo arranca, no solo a los que ya jugaron.
- La tabla colorea las filas que clasifican a cada copa (público y panel).

### 1.3 Playoffs / series

- Toda serie playoff (BO1/BO3/BO5/BO7) genera los partidos reales de la
  serie (no un partido único colapsado) y se puede ver tanto en el panel
  admin como en la vista pública, con el tally de la serie.
- Un bracket de eliminación con N equipos que NO es potencia de 2 usa
  **byes**: `byes = próxima_potencia_de_2(N) − N`; los mejores `byes` seeds
  pasan directo a la ronda siguiente.
- Al completarse la última fecha de la fase de grupos de una división, las
  copas de playoff se siembran automáticamente (auto-seed) según
  `Division.PlayoffMappings` — no requiere una acción manual del admin.

### 1.4 Programación de partidos (cancha + fecha)

- Se puede editar cancha y fecha/hora de un partido no jugado.
- Una misma cancha no puede tener dos partidos con menos de 2 horas de
  diferencia (exactamente 2h está permitido) — validado en el backend (409).
- No se puede editar un partido ya iniciado o finalizado.

### 1.5 Roster / plantel

- `PlayerTeamRegistration` (jugador↔equipo↔torneo) es la fuente de verdad
  del plantel por temporada — incluye el dorsal y la ficha médica de esa
  temporada. `Player.TeamId`/`Player.JerseyNumber` son punteros
  denormalizados de conveniencia ("equipo actual"), no la fuente de verdad.
- Límite configurable de jugadores por equipo y dorsal único por
  equipo+temporada, validado en el backend (409 con el motivo exacto:
  plantel lleno / dorsal duplicado / jugador ya en otro equipo de ese
  torneo).
- Alta de jugadores al plantel: fila editable directamente en la tabla
  (Nombre/Segundo nombre/Apellido/Documento/Fecha de nacimiento/Equipo/
  Teléfono/Obra social), o importación batch por CSV con las mismas
  validaciones fila por fila. La edición de un jugador YA existente vive en
  su página de detalle, no inline en la tabla.
- Ficha médica: no se puede volver a subir una vez habilitada (`Approved` →
  409). `IsHabilitado = (MedicalRecordStatus == Approved)`.

### 1.6 Equipos vs Equipos inscriptos

Conviven "Equipos" (`Team.TournamentId`) y "Equipos inscriptos" (registro de
inscripción a un torneo). Los tabs admin que antes duplicaban esta vista ya
se unificaron; si aparece de nuevo una duplicación, es la misma causa raíz.

### 1.7 Reglas transversales

- **Loading / estados vacíos**: siempre mostrar loading/skeleton hasta tener
  los datos reales. El texto de "no hay nada" solo después de que el fetch
  resolvió y efectivamente está vacío — nunca antes.
- **Navegación**: jerarquía Temporada → Torneos → Divisiones (Torneos no es
  un ítem top-level suelto del nav). "Volver" de una entidad hija vuelve a
  su padre lógico, no a un listado global sin filtrar.
- **Teléfono argentino**: exactamente 10 dígitos, sin 0/15/+54/9 (el celular
  se guarda ya "limpio" — esos prefijos son de marcado, no del número en sí).
- **Sanciones**: se expresan y cumplen en fechas/jornadas ("2 de sanción" =
  se pierde las próximas 2 jornadas de su equipo); el barrido automático por
  días de calendario es solo el mecanismo técnico de limpieza.
- **Sin empates**: todo partido de básquet tiene ganador. En fase de grupos
  el desempate se resuelve por tabla (PTS→PG→DG→H2H→DG-en-H2H); en playoff,
  prórroga hasta que haya ganador.
- **Liga neutral**: no hay local/visita real (canchas alquiladas). El modelo
  mantiene `Match.HomeTeam`/`VisitorTeam` a nivel código, pero el frontend
  nunca lo muestra como "local/visitante".

---

## 2. Cobertura funcional (historias de usuario)

`historias-de-usuario.md` tiene la especificación completa (111 HUs, criterios
de aceptación por historia). Ese documento y su checklist quedaron congelados
en el momento de escribirse (backend 551 tests / frontend 339 tests) — el
número de tests **hoy** se obtiene corriendo la suite, no leyendo un número
fijo en un doc:

```bash
dotnet test Club12-Backend/Solution/Club12.sln   # backend
cd Club12-WebClient && npx vitest run             # frontend
```

Resumen de qué cubre el sistema (a alto nivel; ver §1 y `historias-de-usuario.md`
para el detalle):

- **Autenticación y roles**: login oculto por URL, 2 roles (Owner / Admin IT),
  invitación por magic link, reset de contraseña self-service.
- **Temporadas y torneos**: `Season` agrupa torneos (masc./fem.); wizard de
  creación atómico (un solo endpoint transaccional); máquina de estados del
  torneo (§1.1); copa cruzada multi-grupo.
- **Divisiones y playoffs**: fase de grupos + copas por sub-tier, series
  BO1/3/5/7 reales, auto-seed de playoff, bracket con byes.
- **Equipos y plantel**: alta/baja/edición, kit visual (SVG de camiseta,
  color/estilo), staff técnico (DT/Asistente) por equipo+temporada, roster
  editable en tabla + importación CSV batch, ficha médica y habilitación.
- **Partidos**: fixture automático por jornadas, carga de resultado +
  goleadores + estadísticas, walkover, reprogramación con regla de cancha.
- **Sanciones**: alta, apelación, resolución, vencimiento automático.
- **Estadísticas y goleadores**: ranking global / por temporada / por
  torneo, exportable a CSV, imprimible.
- **Canchas**: CRUD con imagen y ubicación en mapa (OpenStreetMap).
- **Usuarios**: CRUD, activar/desactivar, cambio/reset de contraseña.
- **Blog/Novedades**: publicación pública de noticias.
- **Auditoría**: registro de acciones administrativas sensibles
  (actor+entidad+acción+timestamp), con nombres legibles (no UUIDs crudos).
- **Backups**: respaldo manual y programado, restauración con salvaguarda
  previa, bloqueo de navegación durante la operación.

---

## 3. Contexto técnico / gotchas operativos

Conocimiento no derivable solo leyendo el código — hay que saberlo de
antemano para no perder tiempo redescubriéndolo.

- **`gh` multi-cuenta**: `gh auth switch --hostname github.com --user
  fnferrero97` antes de pushear/mergear (una cuenta secundaria no tiene
  permiso de merge sobre el repo de FrancoRu).
- **Dev local**: backend con `dotnet run --project Club12-Backend/API
  --launch-profile Facundo` (perfil "Facundo" apunta a Postgres remoto en
  Supabase = base de **dev** compartida, no una local descartable).
  Frontend con `npm run dev` (Vite, puerto 3001 o 3002 si está ocupado;
  proxea `/api` al backend local).
- **Harness de tests backend**: SQLite in-memory que arma el schema con
  `EnsureCreated()` desde el MODELO actual (no reproduce migraciones) →
  **bugs de registro de migración no se detectan en tests**. Siempre generar
  migraciones con `dotnet ef migrations add <Nombre> --project Infrastructure
  --startup-project API --output-dir Migrations --context
  ApplicationDBContext` (genera `.cs` + `.Designer.cs` con el atributo
  `[Migration]` + el snapshot). Hay un test guard
  (`EveryApplicationMigration_IsRegistered_WithMigrationAttribute`) que debe
  quedar en verde.
- **Solución backend**: está en `Club12-Backend/Solution/Club12.sln`, NO en
  la raíz de `Club12-Backend`.
- **Season-scoping**: `TeamTournamentRegistration` (equipo↔torneo) y
  `PlayerTeamRegistration` (jugador↔equipo↔torneo, con la ficha médica) son
  la fuente de verdad de la participación por temporada;
  `Team.TournamentId`/`Player.TeamId` son punteros denormalizados de
  conveniencia. La categoría (masc./fem.) vive en el Torneo.
- **`Season → Tournament` es `SetNull`, no `Cascade`** — deliberado, para
  preservar el historial del torneo aunque se borre/limpie la temporada.
- **Playoff/bracket**: `buildBracket.ts` (frontend) soporta BO1/3/5/7 (serie
  → nodo con tally), ida/vuelta, brackets con nombre propio, tercer puesto y
  BYE. La config de copa usa `qualifiers` + `bestOfByStage` +
  `cupPositionRange` (rangos de posición top-down).
- **Patrón "non-throwing by contract"**: una operación de soporte best-effort
  (ej. `IAuditService.LogAsync`, el auto-seed de playoff) nunca debe hacer
  fallar la operación principal — atrapa sus propias excepciones y solo
  loguea un warning.
- **Errores de infraestructura (502/503/504)** nunca vienen de la propia
  API — son el proxy/Cloudflare reportando que el backend está
  inalcanzable/reiniciando (típicamente durante un deploy). El frontend los
  distingue por código de estado y siempre muestra el mensaje genérico en
  español, sin importar la forma del cuerpo de la respuesta (una página de
  error de Cloudflare puede tener un campo `title` que imita accidentalmente
  la forma de un `ProblemDetails` de la API).

---

## 4. Backlog conocido (vigente)

Todo lo que en las notas de trabajo originales aparecía como pendiente y ya
se resolvió (design system, slugs en vez de UUIDs, validaciones de
teléfono/email, popup+spinner+bloqueo de navegación en backup/restore,
camiseta SVG, escudo semitransparente, seed con datos realistas, nav
jerárquico Temporada→Torneo→División, Ver/Editar unificado, staff de
equipo, roster editable, importación CSV, tab Goleadores, auto-seed de
playoff, mensajes de error en español) se sacó de esta lista — verificar
en `git log` si hace falta el detalle de cuándo/cómo.

> **Corrección (2026-09-02, auditoría de `historias-de-usuario.md`)**: "series
> BO-N reales" NO debió estar en la lista de resuelto — era en ese momento la
> brecha abierta más importante encontrada.
>
> **Actualización (2026-09-02, misma sesión)**: ya se resolvió del todo.
> `StageService.SeedPlayoffCupsAsync`/`SeedKnockoutStageAsync`/
> `SeedMultiGroupCrossCupStageAsync` ahora crean un `MatchSeries` real por
> cruce cuando `Stage.BestOf > 1`; `StageService.TryAdvanceStageWinnerAsync`
> empuja el ganador de cada serie decidida a la ronda siguiente del mismo
> bracket (con bye automático), llamado desde las 3 acciones de carga de
> resultado de `MatchController`; y `SeriesInProgressPanel` (tab Playoff de
> División, admin) agrega la acción de UI que faltaba para cargar el 2º/3er
> partido de una serie. `AuditAction.BackupRestore` también quedó resuelto
> (se loguea en `BackupOperationsService.RestoreBackupAsync`).

Lo que sigue abierto, hasta donde se sabe a la fecha de este documento:

- **E2E final del ciclo completo**: crear temporada → torneo → estructura →
  inscripción → fichas → arrancar → cargar resultados → posiciones →
  playoffs → campeones, barriendo cada pantalla (público + admin, todos los
  roles) y verificando coherencia de navegación.
- **Auditoría completa**: extender `AuditLog` a toda mutación administrativa
  que todavía no quede registrada.
- **Mensajes de auth backend → español**: `ErrorMessages.Auth.*` sigue en
  inglés en algunos flujos de borde (ojo: hay tests que assertan el texto
  exacto de esos títulos).
- **Endurecer `OnDelete` peligroso**: `MatchSeries→Team` y `Team→Players`
  son `Cascade` en la base — el guard a nivel de servicio ya previene el
  borrado con historia, pero convendría endurecer el `OnDelete` en una
  migración futura para que la protección no dependa solo del código de
  aplicación.
- **Consolidar documentación de deliverables formales**: `historias-de-usuario.md`
  fue reescrito de punta a punta el 2026-09-02 contra el código real (ver su
  propio changelog). `README.md` y `MANUAL_USUARIO.md` siguen pendientes de
  la misma pasada — todavía describen pantallas/rutas que cambiaron (ej.
  rutas de Fases ya no existen, "Torneos" ya no es un ítem top-level del
  nav).
  contra el estado real de la app.
