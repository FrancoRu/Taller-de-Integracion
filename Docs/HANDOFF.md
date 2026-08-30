# Club 12 — Handoff de sesión (estado + backlog)

> Documento para continuar en un chat nuevo. Rama de integración: **develop** (deploya a staging: club12.argentum-solutions.com.ar). `main` NO se toca. Stack: .NET 8 backend (Clean/Hexagonal, EF Core + Npgsql/Postgres) + React 19/TS/MUI/Vite frontend (vitest, ESLint `--max-warnings 0`). Todo user-facing en **español (voseo)**; código en inglés.

## 🔴 FIXES PRIORITARIOS — feedback del owner (2026-08-30). "Staff es lo último, NO agregar más features. Estabilizar build + E2E del ciclo completo (crear temporada → torneos → jugarlo → todo anda)."

Modelo mental confirmado: **División = tier**; los **playoffs se juegan por sub-tier/copa (Oro, Plata, Bronce)**. Ordenar todo con esa lógica.

Progreso (rama `fix/ux-consolidation-e2e`, sale de develop tras #71):
- 561e86e Home season-first + wizard temporada obligatoria + season header sin chip
- e0e08aa fix del cuelgue del panel "Administración de datos" (deadlock de modales)
- 6b1db53 loader consistente en Novedades + card de Torneos sin texto apretado
- 064c452 ocultar 3er puesto en playoff sin partido por el 3º

1. ✅ **Home**: hecho (561e86e). Orden Novedades → Temporadas → Campeones, sin accesos rápidos ni torneos, hero recortado.
2. ✅ **Detalle de Temporada**: chip del año quitado (561e86e).
3. ✅ **Wizard de torneo — temporada obligatoria**: hecho (561e86e). Sin helpers, temporada required + bloqueada si viene pre-scopeada. (FALTA aún: mover el punto de entrada para crear torneo SIEMPRE desde dentro de la temporada / sacar la entrada standalone del nav.)
4. ⏳ **Campeones — jerarquía**: Temporada → **Torneo** → División → **sub-copa (Oro/Plata/Bronce)**. (Hoy Temporada→Categoría→División.) BACKEND+FRONT.
5. ⏳ **Campeones — sub-copas**: incluir ganador de Copa Plata (y Bronce). Hoy `ChampionService.GetChampionsHistoryAsync` sólo toma `podium.First` de la copa TOP (Oro). Hay que resolver un campeón POR copa/bracket y agregar `CupName` a `ChampionHistoryResponse`. `ChampionResolver.ResolvePlayoffPodium` sólo mira el top bracket → nuevo método `ResolveAllCupChampions`.
6. ⏳ **Campeones — 3er puesto**: ✅ frontend hecho (064c452, oculta el 3º cuando `hasPlayoff && third==null`). FALTA backend: exponer `HasThirdPlace` en `PodiumResponse` (más robusto que la heurística), y el **wizard de playoff debe permitir configurar "Partido por el 3er puesto"** (crea un `StageType.ThirdPlace`). Hoy NO se puede elegir.
7. ⏳ **Posiciones — copa cruzada**: la tabla de la copa cruzada NO colorea los que clasifican a playoff. Agregar `qualificationRanges` también para la división copa-cruzada.
8. ⏳ **Loaders/skeletons**: parcial. Novedades ✅ (6b1db53). Barrer el resto de páginas para consistencia (usar `CardGridSkeleton`/`DetailSkeleton`, nunca mensaje-vacío + spinner juntos).
9. ⏳ **Anchos/altos de textos**: parcial. Card de Torneos ✅ (6b1db53). Revisar el resto.
10. ⏳ **Mapa**: usar **OpenStreetMap** (embed/link free), no Google. (Público: PublicMatchPage "Ver en el mapa"; admin canchas.)
11. ✅ **Admin "Administración de datos"**: cuelgue arreglado (e0e08aa) — era un deadlock: el Dialog bloqueante de MUI (z-index 1300) tapaba el SweetAlert (z-index ~1060), su OK no se podía clickear, el await no resolvía y el `finally` que cierra el overlay nunca corría. Ahora se cierra el overlay ANTES de notificar.
12. ⏳ **Todo por slug** (recordatorio; verificar en las páginas nuevas/tocadas).
13. ⏳ **Staff de equipo** (último feature): `TeamStaff` (el `Staff` histórico se dropeó en `20260315232746_cleanDB`) + roles DT/Asistente/DT-Jugador, season-scoped, admin + vista pública, seed.
14. ⏳ **E2E FINAL** del ciclo completo con build estable.
15. ⏳ **Creación de torneo ATÓMICA (owner 2026-08-30, Image #12)**: hoy el wizard (`submitWizard.ts`) crea todo con MUCHOS requests secuenciales (POST tournaments → PUT open-registration → POST divisions → POST stages uno por uno). Debe ser UN endpoint transaccional único (crear torneo + divisiones + stages + copas + copa cruzada en una sola transacción DB, todo o nada). Es el fix más importante para "que el flujo de crear torneos ande bien".

## 🔴🔴 FEEDBACK 2 — owner (2026-08-30, tarde). BLOCKER del ciclo + tanda de bugs de admin/UX

**PRIORIDAD MÁXIMA (el ciclo no cierra sin esto):**
- **A1. Asignar equipos inscriptos a divisiones/zonas.** No existe/no se encuentra la UI para asignar cada equipo inscripto a una zona/división creada. Backend probable: `StageTeamMatchService` (asigna equipos a stage + genera matches), `TournamentEnrolledTeams.tsx`. Hay que poder agregar equipos inscriptos DENTRO de cada zona (Image #16), y después **generar el fixture** por stage.
- **A2. Stages "no activan".** En Fases (Image #15) todas salen ACTIVA=No, sólo se puede ver/eliminar, no activar → no tiene sentido. Además aparecen DUPLICADAS (2x Fase de Grupos, 2x Copa ORO Final, etc. — 18 fases). Revisar por qué se duplican y cómo se activa una fase.
- **A3. "Equipos" vs "Equipos inscriptos".** ¿Por qué hay dos tabs (Image #17/#18)? En la página de inscripción los equipos ya inscriptos aparecen igual con "Ya registrado" y checkeados (Image #16/#19) → deben EXCLUIRSE los ya inscriptos de la lista de "disponibles para registrar". Definir/limpiar la relación Equipos ↔ Equipos inscriptos.

**Bugs de modales/alertas (recurrente):**
- Muchos modales se abren/apilan → bug, se resetea o se muestran doble. Alertas se disparan 2+ veces de distinta forma. Modales quedan DETRÁS de otros (mismo patrón z-index que el fix del panel de datos). Barrer todo el admin.
- Al crear equipo, el error de validación aparece DETRÁS del modal (Image #14).

**Equipos:**
- **Escudo con fondo naranja (Image #13/#14)**: se suben PNG transparentes pero se muestran con fondo naranja. Preservar transparencia del escudo.
- **Label "Código"**: es de 3 letras pero el label no lo dice → agregar hint "(3 letras)".

**Canchas:**
- La cancha NO debería tener imagen (quitar imagen de cancha).
- Debería dejar ELEGIR en el mapa dónde está (map picker), y al "ver" mostrar en el mapa el punto + la dirección.
- Editar cancha no carga la imagen → el modal queda vacío y se pierde la imagen (bug de edición).

**Novedades / público:**
- Al abrir una noticia como visitante, la página queda centrada → debe ir al TOP (scroll to top on navigate). (Image #20)
- La imagen destacada de la noticia debería ser más chica.
- Editar publicación da **404 not found**.
- Revisar el contador de vistas (¿se arregló?).
- La cancha de básquet (BasketballCourtPattern) del hero del home "está re mal" → rehacer/quitar.

**Staff (feature, worker cayó 3x por límite de sesión):** worktree `.claude/worktrees/agent-a5058e93cc2358d01` tiene una migración creada pero el feature quedó incompleto y sin verificar. Rehacer/completar cuando haya budget.

## ✅ MERGEADO a develop / en staging (PRs #53–#71)

**Design system (base)**
- `src/design/tokens.ts` (brand/surface/ink/semantic/radius/font/pageMinHeight/**gold**/**category**), `colorName.ts` (hex→fill+ink), `jerseyStyles.ts` (11 estilos).
- **JerseySvg**: silueta de básquet (path aprobado por el owner, viewBox "100 44 248 362") + 11 estilos (solid/stripes/hoops/diagonal/chevron/sash/sides/halves/circles/gradient/vneck) tintados con color primario+secundario. Dorsal con halo de contraste. `shirtColor` = SIEMPRE hex (color picker), NO texto libre.
- `PageShell` (altura mínima constante, sin salto loading↔datos), skeletons (Table/List/CardGrid/Detail), `FilterBar` (+ "Limpiar filtros"), `TeamHero` (fondo escudo tintado), `SectionHeading` (acento naranja), `SecondaryTabs` (pills, nivel 2 vs underline nivel 1). Theme consume tokens. Pager de tablas en español vía `dataGridLocaleText()` (cada grid pasaba localeText que pisaba el theme).

**Kit de equipo**
- Backend: `Team.JerseyStyle` (default "solid") + `Team.ShirtSecondaryColor` (nullable) + migración. Seed asigna 11 estilos variados con hex.
- Front: `TeamFormDialog` con color pickers (primario/secundario) + galería de 11 templates + preview con dorsal. **Escudo prominente arriba** del form + upload (FIX: mandaba File crudo → 400; ahora FormData TeamId+LogoFile) + preview en vivo (object URL). Columna Camiseta centrada. FIX CRÍTICO: `putTeamById` devuelve boolean (204 sin body) → el dialog ahora cierra + toast + refresca ("guardar no anda" era esto). Vista pública de equipo con TeamHero + camiseta en roster.

**Roles: Admin = Owner unificado**
- Backend: Backup/Maintenance/DataMaintenance pasaron de Admin-only a `AdminOrOwner`. Front: todas las rutas admin permiten ambos; UN solo menú de sidebar; ambos aterrizan en torneos.

**Navegación / site-map**
- Sidebar admin reagrupado: **Competición** (Temporadas/Torneos/Sanciones/Canchas) / **Gestión de equipos** (Equipos/Inscripción/Jugadores) / **Novedades** / **Usuarios** / **Sistema** (Estadísticas/Auditoría/Administración de datos) / **Configuración**. Puntuaciones SACADO del nav (es vista dentro del torneo).
- Nav pública: **Inicio · Temporadas · Campeones · Sanciones · Novedades · Quiénes somos · Información**. **Torneos SACADO** (competición vía Temporadas → card de temporada → sus torneos). Home hero/cards → Temporadas.

**Temporada (Season) — modelo ADITIVO**
- Nueva entidad `Season` agrupa Torneos (Masculino + Femenino). `Tournament.SeasonId` nullable; el Torneo mantiene su categoría (HU-48 intacto). CRUD (GET público, mutaciones AdminOrOwner) + migración + seed "Temporada 2026". Front: módulo season, página pública Temporadas (lista + detalle agrupando masc/fem), admin CRUD, selector de temporada al crear/editar torneo, rutas/nav.

**Campeones + Podio**
- Backend: `ChampionResolver` deriva campeón/podio POR COMPETICIÓN (división-zona y copa cruzada): con playoff → ganador de la copa TOP (la del puesto #1, "Copa de Oro" / bracket de la cruzada), final decide 1º/2º + ThirdPlace decide 3º; solo grupo → top-3 de la tabla. Endpoints `GET tournaments/{idOrSlug}/champions` + `GET champions?seasonId=` (historial, solo torneos Finished).
- Front: componente `Podium` (1º/2º/3º con trofeo + oro/plata/bronce), página pública **Campeones** (historial agrupado por temporada; "Sin temporada" si null), banner de podio arriba de la división cuando tiene campeón decidido. VERIFICADO E2E en localhost (Clausura Primera: 1º Independiente Rural).

**Consistencia de dominio**
- Ficha médica: no se puede re-subir una vez habilitado (Approved → 409); dialog oculta upload cuando habilitado y muestra Descargar que ANDA (antes Link roto a ref privada; ahora endpoint de streaming). `IsHabilitado = (MedicalRecordStatus==Approved)`, vive en `PlayerTeamRegistration` (por temporada). Seed: 7/8 jugadores por equipo aprobados CON ficha, 1 pending.
- Eliminar jugador/cancha: guardas de integridad — BLOQUEA (409) si el jugador tiene stats/goleadores/sanciones o la cancha tiene partidos, con mensaje claro. Frontends mostraban "éxito" falso cuando el backend fallaba → ahora muestran el error real (`problemDetails`/`MutationResult`). VERIFICADO E2E.
- Segundo nombre opcional (el form lo forzaba).

**Bugs/UX sueltos arreglados**
- Slug: navegaciones admin por slug (torneo/división/sanción/blog); el SEED pegaba `Guid.NewGuid()` al slug de división/fase → SlugRegistry + migración re-backfill. Contraste: labels de modelo de camiseta, dorsal (halo), ícono ficha médica (era `secondary` navy). Canchas: centrado + ícono estadio→avatar-solo-con-foto. "Group"→"Fase de grupos" (translateStageType). "Novedades" unificado (era "Blog" en admin). Validaciones email/teléfono (front+back). 403 en voseo. Columnas numéricas/estado centradas.

## ✅ MERGEADO a develop vía **PR #64** — sesión 2026-08-29 (rama `feat/branding-category-apply`)

Loop autónomo de mejora de páginas públicas + data real. Todo verde (backend 648 tests / 0 warnings, frontend 446 / tsc / eslint) y verificado E2E con Playwright. Deployado a staging.

1. **Branding por categoría** — `categoryColor()` (single source masc→naranja `#FF5A1F` / fem→púrpura `#A32CC4`, ink por luminancia), `CategoryChip` reutilizable, `SectionHeading` con prop `accentColor`, Podio 1º naranja→**oro** (`brand.gold`).
2. **TeamBackdrop** — fondo de identidad en `PublicTeamPage`: glow del color del equipo + escudo watermark transparente en diagonal desde abajo-derecha, con fade al footer. `hexToRgba` extraído a `colorName.ts` (DRY). Va en el FONDO de la página, NO en el hero.
3. **Perfil de equipo (full-stack)** — 3 endpoints backend read-only `[AllowAnonymous]`: `GET teams/{idOrSlug}/summary` (fila de posición; **prefiere la división-zona sobre la copa cruzada** vía `Division.IsCrossDivisionCup`), `/matches` (fixture orientado al equipo), `/participations` (historial para el selector). Frontend rediseñado como **box-score**: selector de temporada/torneo, StatTiles (Posición de la tabla; **Record/PF/PC/Diferencial agregados de TODOS los partidos** vía `computeRecord`, coherente con Racha/Fixture), racha, fixture, goleadores (`scorer/by-player`), títulos en oro, plantel. Todo re-scopeado por torneo. **Liga neutral: NO se muestra local/visita** (el modelo mantiene home/visitor a nivel código). "Volver" usa `navigate(-1)`.
4. **Campeones rediseñado** — jerarquía **Temporada → Categoría (masc/fem) → División/Copa → Campeón** (`groupChampions` helper TDD). Tarjetas de premio: banda dorada "CAMPEÓN", escudo con anillo+glow, nombre en Oswald.
5. **Seed real** — `DataSeeder` reconstruido: **1 Season "Temporada 2026"** con 4 torneos: **Apertura masc + fem (Finished → campeones)** y **Clausura masc + fem (Ongoing, doble rueda, 8 fechas jugadas + 6 próximas, sin playoffs)**. Cada uno con divisiones + `Copa Club 12` cross-cup. 40 clubes reales de Entre Ríos/Paraná (Echagüe, Estudiantes de Paraná, Rowing, Sionista, Talleres, Patronato, Central Entrerriano…), **escudos reales random** subidos a Supabase desde `Seed:LogosPath` (default `D:\Escudos\Logos de Argentina\clubs\normal`) vía `SupabaseHelper.UploadImageAsync<Team>`. Blog posts contextuales. Config nuevo: `Seed:Reset` (fuerza wipe+reseed FK-safe, dev-only bajo `Seed:Enabled`) y `Seed:LogosPath`. **Reseed ejecutado + verificado** (Campeones muestra campeones masc/fem bajo Temporada 2026; Clausura en curso con Últimos+Próximos). NOTA: cada torneo tiene su propio set de clubes (Team.Slug único) → el selector de temporada del perfil muestra 1 participación por ahora (reuse cross-torneo quedó pendiente, tradeoff documentado).
6. **Home rediseñado** — hero cálido (gradiente naranja→granate, court pattern, Oswald "CLUB 12", eyebrow dorado, CTAs Ver temporadas/Campeones), torneos destacados con `CategoryChip`+estado, **franja "Campeones recientes"** (oro, crests reales), noticias, y accesos rápidos como pill strip secundario. `TournamentCard` ahora muestra su chip de categoría (también en la página de Torneos).
7. **Sweep E2E público (con Playwright en 3002) + 2 bugfixes**: verificado el ciclo completo Home → Temporadas → Temporada (masc/fem) → Torneo → División (posiciones/goleadores/partidos/llaves) → Campeones → perfil de equipo, todo con data real. BUGS ARREGLADOS: (a) **Temporadas pública salía vacía** — el endpoint `GET /api/seasons` devuelve un array plano pero el context leía `res.data.items` (undefined); era latente (no había Season antes). (b) **Podio "Campeones" se mostraba en torneos En curso** — `PublicTournamentPage` traía el podio sin gatear por estado; ahora solo si `TournamentStatus.Finished` (Apertura muestra podio, Clausura no).

## ✅ MERGEADO a develop vía **PR #65** — rama `feat/admin-season-first`

Loop autónomo (mandato del owner: "armá un plan y seguilo, arreglá todo"). Todo verde (backend 648 / frontend 458) y deployado a staging. Incluye:
1. **Fix login en español** — el `signIn` del auth context disparaba un alert global BLOQUEANTE con el mensaje crudo del API en inglés ("Invalid credentials."); ahora el login solo muestra su error inline en español ("Usuario o contraseña incorrectos"). Pendiente menor: traducir `ErrorMessages.Auth.*` del backend (asoman en flujos de auth de borde).
2. **Admin season-first** — admins/owners aterrizan en **Temporadas**; nueva página admin de detalle de temporada (`/panel/temporadas/:seasonId`) que lista los torneos de esa temporada agrupados por categoría con CTA "Nuevo Torneo" que abre el wizard **pre-scopeado** a esa temporada; drill-in "Ver torneos" desde la lista. ADITIVO: la ruta plana de Torneos y el path "Sin temporada" del wizard quedan intactos.
3. **Posiciones coloreadas** ✅ VERIFICADO — backend deriva `QualificationRanges` (from/to/copa/order) de `Division.PlayoffMappings` y las expone en el detalle de división; front colorea las filas que clasifican por tier de copa (oro/plata/bronce) + chip "Copa Oro"/"Copa Plata" por fila + leyenda. El seed ahora da Copa Oro (1-4) + Copa Plata (5-8) a las divisiones de torneos finalizados (via `SeedCupPlayoffs`), campeón desde la copa top. Playwright confirmó el coloreado en Apertura Primera. (Nota menor: el 3er puesto del podio sale "A definir" en copas BestOf=1 sin partido por 3er puesto.)
4. **Error-handling público** ✅ — los GETs iniciales públicos ya NO disparan el SweetAlert bloqueante; muestran un estado inline `LoadErrorState` ("No pudimos cargar…" + "Reintentar"). Flag opt-in `{ silent }` en los métodos GET de los contexts (mutaciones intactas, mantienen su modal). Cubre home, temporadas, temporada, torneos, torneo, equipo, sanciones, blog.
5. **Fix MUI Tabs** ✅ — deep-link a `?tab=<division>` antes de que carguen las divisiones tiraba un error de consola (value sin match); ahora cae a "info" hasta que exista el tab. Consola pública limpia (0 errores).

## ✅ MERGEADO a develop vía **PR #66** — rama `feat/admin-organizing`

Bloque admin-organizar + seguridad (verificado por build+tests; E2E admin en vivo requiere login del owner). Deployado a staging. Incluye:
1. **Deducción de puntos** ✅ — entidad `TeamPointDeduction` (DivisionId/TeamId/Points/Reason) + migración `20260829101157_AddTeamPointDeduction` (guard verde) + endpoints `POST/GET api/divisions/{id}/point-deductions` + `DELETE api/point-deductions/{id}` (AdminOrOwner salvo el GET). `PositionCalculator.CalculatePositions` recibe las deducciones y las resta ANTES de los desempates (la tabla re-rankea); NO clampea en 0 (una sanción puede hundir al equipo, como en la realidad). Front: `PointDeductionManager` en el tab Posiciones del admin de división (dialog equipo+puntos+motivo, lista con borrar) + nota `-N` con motivo en la tabla pública. Migración aplicada a la DB dev.
2. **Fases admin guard** ✅ — `StageService.CreateStageAsync`/`DeleteStageAsync` rechazan (409, "No se pueden agregar o quitar fases: el torneo ya arrancó.") cuando el torneo está Ongoing/Finished (fixture ya generado); editable mientras Scheduled/OpenForRegistration/RegistrationClosed. Front: el tab "Fases" del admin de división deshabilita "+ Nueva Fase" (tooltip) y oculta el borrar cuando arrancó. Sin migración.
3. **Stats por temporada** ✅ (frontend) — la página admin Estadísticas ahora tiene filtro Temporada + Torneo (torneos derivados de la temporada elegida) que scopea el ranking de goleadores vía los params `season`/`tournamentId` que YA existen en el endpoint scorer; las cards resumen quedan globales. Default sin filtro. Helpers `statisticsFilters.ts` TDD.
4. **Delete-integridad global** ✅ (backend, sin migración) — guards de borrado en los services de Team/Tournament/Division: bloquean (409 español) si hay historia competitiva (partidos jugados, Ongoing/Finished, deducciones, sanciones, inscripciones); si está vacío, borra con cascade coherente. OJO borra vía `ExecuteDeleteAsync` (bypassa EF → solo aplica OnDelete de la DB). ⚠️ FLAG (reportado, NO tocado, requiere migración): `MatchSeries→Team` y `Team→Players` son Cascade → un borrado crudo de Team borraría series/plantel; el guard de servicio lo previene, pero convendría endurecer el OnDelete en una migración futura.

## ✅ MERGEADO a develop vía **PR #69** — rama `feat/scoreboard-and-polish`

1. **Scoreboard del partido público** ✅ — `PublicMatchPage` rediseñada como scoreboard: marcador grande (Oswald, ganador resaltado en primary, perdedor atenuado), escudos iguales, chip de estado, fecha/cancha, "VS" si está programado; goleadores por equipo (2 columnas) + sanciones. Liga neutral (sin local/visita). Admin de partido intacto. Helpers `getScoreboardEmphasis`/`sortScorersByPoints` TDD.
   - NOTA: se mergeó `origin/develop` (#68 medical-records-storage-eligibility + rebackfill de player-slugs) a esta rama antes del PR.

## 📋 BACKLOG restante (prioridad)
0. **Goleadores del scoreboard** ✅ (PR #70) — `MatchRepository.GetDetailByIdOrSlugAsync` incluye Home/Visitor/Venue + `Scorers.ThenInclude(Player)` + `Stage.Division`; el mapping atribuye scorers al equipo vía `Scorer.Player.TeamId` y agrega por jugador. Además: cada goleador muestra la **camiseta (kit del equipo) + dorsal** (de la inscripción del torneo; `Player.JerseyNumber` es `[NotMapped]`, el real vive en `PlayerTeamRegistration.JerseyNumber`), se sacó la sección Sanciones del partido, y el seed reparte los puntos entre 5 jugadores + asigna dorsales 4-11 únicos por equipo. Verificado con Playwright.
1. **Llaves admin editables** — cargar resultado desde el bracket (ahí están los partidos).
2. **Staff de equipo** (DT/asistente/DT-jugador) — entidad/rol por equipo+temporada (+migración).
3. **Canchas imagen+mapa** ✅ (PR #71) — `Venue.Latitude/Longitude` (migración `AddVenueImageAndLocation`) + `PUT /venues/{id}/photo`; admin edita coords+imagen (preview); vista pública muestra imagen + botón "Ver en el mapa" (Google Maps por coords, sin dep de mapa). Seed con **6 canchas reales de Paraná** (Estadio Ángel Malvicino/Echagüe, Estudiantes, Rowing, Sionista, Talleres, Polideportivo Municipal) con coordenadas, en ambos seed paths (`DataSeeder` + `DataMaintenanceService`). Verificado (un partido juega en Estadio Ángel Malvicino).
4. **Auditoría completa** — extender `AuditLog` a TODA mutación admin (actor+entidad+acción+timestamp).
5. **Mensajes auth backend→español** — `ErrorMessages.Auth.*` están en inglés (ojo: tests que asertan el texto exacto de los títulos del GlobalExceptionHandler).
6. **Endurecer OnDelete** peligroso (`MatchSeries/Team→Cascade`) en una migración (flagged en #66).
7. **E2E FINAL** del ciclo completo (crear temporada→torneo→estructura→inscripción→fichas→arrancar→cargar resultados→posiciones→playoffs→campeones), verificando que TODO sea correcto — el norte.


PLAN restante (orden): SEO/meta; admin organizar (scoreboard/scoring por partido, deducción de puntos, fases bloqueadas con torneo arrancado, llaves editables); integridad+auditoría (delete-integridad global, AuditLog en toda mutación); features (staff DT, stats por temporada, canchas imagen+mapa, mensajes auth backend→español); E2E final del ciclo completo. Norte: organizar temporada de punta a punta, correcto.

## 📋 BACKLOG PENDIENTE (con specs, prioridad sugerida)

1. **[BRANDING]** ✅ base hecha (ver EN PROGRESO): categoryColor/CategoryChip, oro en podio/campeones, TeamBackdrop. Falta si se quiere: gradientes cálidos naranja→granate en más heros; estética banner crema+estrellas en Home.
2. **[POSICIONES COLOREADAS]** Marcar en la tabla de posiciones los puestos que clasifican a la siguiente fase (cutoff por copa). `DivisionStandings` solo recibe `positions[]` — NO tiene el cutoff. El BACKEND debe exponer rangos de clasificación por división (por copa: from/to + nombre), derivado de `cupPositionRange` (cups top-down). Front colorea filas + leyenda (verde=Oro, gris=Plata, etc.).
3. **[SCOREBOARD + SCORING por partido]** MatchPage YA permite cargar/editar resultado + goleadores/puntos (tab Puntuaciones → MatchStatisticsTab) + sanciones + walkover. FALTA: mejorar el LAYOUT tipo scoreboard (hoy h4 simple). Y el MODELO: los puntos del jugador deben AGREGARSE de sus partidos (box score); editar manual = solo CORRECCIÓN. La tab Puntuaciones de TeamPage hoy lista PlayerStatistic + botón "añadir" suelto → rediseñar a agregado + corrección.
4. **[STATS por torneo/temporada]** StatisticsPage hoy solo global — agregar filtro por torneo/temporada.
5. **[STAFF de equipo]** Los equipos tienen cuerpo técnico con roles: **DT, ASISTENTE, DT/JUGADOR** (player-coach). Feature: entidad/rol Staff por equipo (por temporada/inscripción). Ya existe StaffName en PlayerSanction — ver si hay entidad Staff o crearla.
6. **[DEDUCCIÓN DE PUNTOS]** Única acción "point" admin válida: deducir puntos de la tabla de un equipo en una división (penalización disciplinaria, con motivo), que PositionCalculator reste del total. Relacionado con sanciones.
7. **[SELECTOR TEMPORADA en equipo]** La vista de equipo debe tener un SELECTOR de temporada/torneo (NO tabs — no escalan) que cambia plantel/posiciones/partidos. Identidad del equipo (nombre/escudo/colores) persistente; participación por temporada = inscripciones. Season es aditiva, NO re-scopeó equipos/jugadores.
8. **[CANCHAS imagen + mapa]** `CreateVenueRequest` ya tiene `ImageFile` (puede estar a medias) — completar display+edit de la imagen. Y UBICACIÓN EN MAPA: lat/lng en Venue + mapa con marcador en view/editar (Leaflet+OpenStreetMap = sin API key; o guardar coords + link a Google Maps).
9. **[AUDITORÍA COMPLETA]** Faltan entradas en el Registro de auditoría. Ya existe `AuditLog` + `AuditConstants`. EXTENDER a TODA mutación admin (CRUD de cada entidad, review de ficha, sanciones, plantel, deducción, backup, roles) con actor+entidad+acción+timestamp.
10. **[DELETE-INTEGRIDAD GLOBAL]** Ya hecho para jugador/cancha. Extender el principio (bloquear/cascade coherente, no orfanar) a: equipo (partidos/jugadores/inscripciones), torneo (divisiones), división (fases), etc. Revisar OnDelete de cada FK + guardas en services.
11. **[SEO + HTML SEMÁNTICO]** `<title>` + meta description por ruta (hay util `pageMetadata`), Open Graph/Twitter cards (torneo/equipo/blog/temporada), heading hierarchy (h1 por página — ya via PageShell), landmarks (ya via sweep), sitemap.xml + robots.txt, lang="es", canonical. Buen mapa del sitio navegable.
12. **[UX ERROR HANDLING]** Páginas públicas (home, etc.) disparan un SweetAlert BLOQUEANTE en fallo de fetch inicial → cambiar a empty state silencioso / inline "reintentar". Modal solo para acciones del usuario (guardar/borrar), no para GETs.
13. **[FASES admin]** No permitir "+ Nueva Fase" con torneo ya arrancado (Ongoing/fixture generado) — agregar fase rompe el fixture. Ocultar/deshabilitar según estado.
14. **[LLAVES admin]** El bracket es solo-view; en admin debería permitir EDITAR (cargar resultado desde la llave — ahí están los partidos).
15. **[E2E FINAL]** Sweep completo de TODAS las pantallas (público + admin, todos los roles) verificando navegación/jerarquía/UX → arreglar lo que salga. Verificar el flujo de ficha médica end-to-end tras un reseed (subir→aprobar→habilitado→solo ver/bajar). Filtros con sentido por pantalla.

## 🎨 SISTEMA DE BRANDING (marca real, IG @club12basquet / web club12lavuelta.com)
"La primer Liga Libre de Básquet, Paraná Entre Ríos, Masculino/Femenino". Logo shield C12+pelota (contorno blanco).
- **Masculino → naranja/rojo** (#FF5A1F). **Femenino → PÚRPURA/MAGENTA** (#A32CC4) — color de categoría confirmado por el owner. **ORO** (#E6A817) → campeones/finales/trofeos. **Granate** (#4D0000) → fondo del logo.
- Gradientes cálidos, halftone, estética enérgica. Banner "CAMPEONES" = crema + estrellas doradas + C12.

## ⚙️ CONTEXTO TÉCNICO / GOTCHAS
- **gh multi-cuenta**: `gh auth switch --hostname github.com --user fnferrero97` ANTES de push/merge (la cuenta EMU no puede mergear el repo de FrancoRu).
- **Local dev**: backend `dotnet run --project Club12-Backend/API --launch-profile Facundo` (perfil "Facundo" → Postgres REMOTO en Supabase = DB de **dev**, aprobada por el owner; corre en https://localhost:5001). Frontend `npm run dev` (Vite, VITE_PORT 3001; si ocupado, 3002; proxya /api a 5001, `secure:false`). El owner loguea en localhost (yo NO tipeo contraseñas — regla de seguridad). OJO: al correr `dotnet build/test`, los workers matan el backend local (file locks); hay que relevantarlo.
- **Test harness**: SQLite in-memory que construye el schema con `EnsureCreated()` desde el MODELO (NO desde migraciones) → **bugs de registro de migración NO se ven en tests**. SIEMPRE crear migraciones con `dotnet ef migrations add X --context ApplicationDBContext` (genera .cs + .Designer.cs con `[Migration]` + snapshot). Guard test `EveryApplicationMigration_IsRegistered_WithMigrationAttribute` debe quedar verde. Contexto EF real = `ApplicationDBContext`.
- **Season-scoping**: `TeamTournamentRegistration` (equipo↔torneo) y `PlayerTeamRegistration` (jugador↔equipo↔torneo, tiene la ficha médica) son fuente de verdad; `Team.TournamentId` es puntero denormalizado. La categoría vive en el Torneo (HU-48).
- **Playoff/bracket**: `buildBracket.ts` (front) soporta bo1/3/5/7 (serie→nodo con tally), ida/vuelta, brackets con nombre, tercer puesto, BYE. Cup config: `qualifiers` + `bestOfByStage`, `cupPositionRange` (rangos top-down).
- **Proceso de delegación**: workers general-purpose en el árbol compartido, 1 por área disjunta (o 2 si son dirs disjuntos como backend vs frontend), STRICT TDD, NO isolation:worktree, el orquestador commitea. Verificar SIEMPRE: `dotnet build Club12-Backend/Solution/Club12.sln` (0 warnings — SonarAnalyzer S3358/S3267 cuentan) + `dotnet test`; `npx tsc --noEmit` + `npm run lint` + `npx vitest run`. OJO: la solución está en `Club12-Backend/Solution/Club12.sln` (NO en la raíz). El build de un worker mata el backend local (locks) → relevantarlo después.
- **Reseed dev**: `dotnet run --project Club12-Backend/API --launch-profile Facundo -- --Seed:Enabled=true --Seed:Reset=true` (args de línea de comando = máxima precedencia en .NET config). Wipea (FK-safe via `ExecuteDeleteAsync`) + seedea + sube escudos a Supabase. Correr una vez con `--Seed:Reset=true`; después arrancar sin ese flag (skip-if-teams-exist). Escudos reales en `D:\Escudos\Logos de Argentina\clubs\normal` (`Seed:LogosPath`).
- **Perfil de equipo endpoints** (nuevos, AllowAnonymous): `teams/{idOrSlug}/summary|matches|participations`. El summary prefiere la división-zona sobre la copa (`Division.IsCrossDivisionCup`). Record/PF/PC del box-score = agregado de TODOS los partidos del torneo (no solo fase de grupos), en el FRONT (`computeRecord`); la tabla de posiciones sí es solo fase de grupos.
- **Liga neutral**: NO hay local/visita (canchas alquiladas). El modelo mantiene `Match.HomeTeam/VisitorTeam` a nivel código, pero el FE nunca muestra "Local/Visita".
- **Campeones**: `GET api/champions` solo devuelve torneos **Finished** (status). Un torneo Ongoing no aparece en Campeones aunque tenga playoffs jugados.

## 🔁 CÓMO SEGUIR EL LOOP
Wave por wave: diseñar (design-first para features), implementar (delegar 2+ archivos no triviales), **verificar verde**, PR a develop + merge (deploya staging), **E2E en vivo** (localhost con la DB dev, el owner loguea) → arreglar lo que salga → repetir. Criterio de organizador de torneos de básquet. NADA se da por "arreglado" sin probarlo.
