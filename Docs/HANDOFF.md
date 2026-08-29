# Club 12 — Handoff de sesión (estado + backlog)

> Documento para continuar en un chat nuevo. Rama de integración: **develop** (deploya a staging: club12.argentum-solutions.com.ar). `main` NO se toca. Stack: .NET 8 backend (Clean/Hexagonal, EF Core + Npgsql/Postgres) + React 19/TS/MUI/Vite frontend (vitest, ESLint `--max-warnings 0`). Todo user-facing en **español (voseo)**; código en inglés.

## ✅ MERGEADO a develop / en staging (PRs #53–#62)

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

## 🔄 EN PROGRESO (NO mergeado)
- **Wave branding (rama `feat/branding-category-colors`)**: `tokens.ts` ya tiene `brand.gold`/`brand.goldLight` + `category` (masculine=naranja, feminine=#A32CC4 púrpura/magenta). FALTA: helper `categoryColor(TournamentCategory)`, aplicar a chips MASCULINO/FEMENINO, secciones masc/fem de Temporada, badges de categoría, y estética de banner de campeón (oro + estrellas) en Campeones/Podio. Verificar + commitear + mergear.

## 📋 BACKLOG PENDIENTE (con specs, prioridad sugerida)

1. **[BRANDING]** (en progreso, ver arriba). Sistema: Masc→naranja, Fem→púrpura/magenta (#A32CC4), Oro (#E6A817)→campeones/finales, granate (#4D0000)→logo. Fiel a IG @club12basquet + claridad semántica. Gradientes cálidos naranja→granate en heros. Estética banner de campeón (crema + estrellas doradas + C12) en Campeones. Muestrear hex del logo shield; mirar club12lavuelta.com.
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
- **Proceso de delegación**: workers general-purpose en el árbol compartido, 1 por área disjunta, STRICT TDD, NO isolation:worktree, el orquestador commitea. Cada worker crea su rama (`git checkout -b`) — ojo que switchea el árbol compartido; correr de a uno para no pisar. Verificar SIEMPRE: `dotnet build` (0 warnings) + `dotnet test`; `npx tsc --noEmit` + `npx eslint --max-warnings 0` + `npx vitest run`.

## 🔁 CÓMO SEGUIR EL LOOP
Wave por wave: diseñar (design-first para features), implementar (delegar 2+ archivos no triviales), **verificar verde**, PR a develop + merge (deploya staging), **E2E en vivo** (localhost con la DB dev, el owner loguea) → arreglar lo que salga → repetir. Criterio de organizador de torneos de básquet. NADA se da por "arreglado" sin probarlo.
