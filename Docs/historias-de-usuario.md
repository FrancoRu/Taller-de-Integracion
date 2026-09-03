# Club 12 — Historias de Usuario

> Reescritura 2026-09-02: el doc original (reunión Facundo/Franco + decisiones del owner) quedó
> desactualizado en varios puntos estructurales tras sesiones posteriores de desarrollo — más
> notablemente, la aparición de una entidad **Temporada (Season)** por encima de Torneo, que no
> existía cuando se escribió la primera versión. Esta versión fue verificada historia por historia
> contra el código real (backend + frontend), no contra memoria de lo que "se pidió" — cada HU dice
> si sigue vigente, cambió, o quedó obsoleta, citando dónde en el código se verificó. Se mantiene la
> numeración original de HU donde la historia sigue existiendo (el código la referencia por número en
> varios comentarios — renumerar rompería esa trazabilidad); las historias nuevas se agregan al final
> con números nuevos.
>
> **Actualización 2026-09-03**: se agregaron HU-118/HU-119 (umbral de 4 jugadores habilitados por
> equipo para cargar un resultado normal / para iniciar el torneo) y HU-86b (top 10 en el ranking
> público de goleadores), se corrigieron HU-62/HU-73/HU-78/HU-82/HU-109 con lo que cambió en esa
> sesión, y se marcaron dos ítems más de la Épica 24 como resueltos (`MatchId` required, roster
> mostrando "No habilitado" para todos). Mismo criterio que la reescritura de abajo: verificado
> contra código real, no contra memoria de lo pedido.
>
> Convenciones: `[BUG]` corrige algo existente · `[NUEVA]` propuesta alineada · `[F2]` Fase 2.
> Prioridad (MoSCoW): **M** Must · **S** Should · **C** Could.
> `✅` = verificada vigente tal cual · `🔄` = vigente pero cambió su implementación (ver nota) ·
> `❌` = obsoleta/reemplazada (ver nota) · `⚠️` = vigente pero con una brecha real detectada en el código.

---

## Decisiones transversales (leer antes que nada)

Aplican a TODAS las historias. Si una historia las contradice, ganan estas.

- **D1 — Solo 2 tipos de usuario y 2 cuentas.** Sigue vigente. `Owner` y `Admin IT` son los únicos
  roles (`Club12-WebClient/src/modules/core/enum/user/userRolesType.ts`); una migración
  (`20260828021949_RemoveTournamentAndTeamManagerRoles`) eliminó los roles viejos de la base.
- **D2 — Equipos y jugadores están scopeados a la temporada/torneo.** Sigue vigente como principio,
  pero el nivel de scope ya no es solo "torneo": ver **D7** más abajo — ahora hay una capa
  `Season` (Temporada) por encima que agrupa torneos, sin cambiar cómo se scopean equipos/jugadores
  (siguen colgando del Torneo vía `TeamTournamentRegistration`/`PlayerTeamRegistration`).
- **D3 — Contenido institucional bloqueado para el owner.** Sigue vigente (`/quienes-somos`,
  `/reglamento`, ficha médica no tienen ruta de edición en el panel).
- **D4 — Reglamento agnóstico a la app y a la temporada.** Sigue vigente.
- **D5 — Una acción = una transacción.** Sigue vigente (el asistente de torneo, HU-38, sigue
  persistiendo todo en una transacción).
- **D6 — ~~Magic link / self-service de password / auto-inscripción son Fase 2~~ — OBSOLETA.**
  Magic link de invitación (HU-09) y self-service de reset de contraseña (HU-10) están **totalmente
  implementados y en producción**, no diferidos: `Club12-Backend/API/Controllers/AuthController.cs`
  expone `POST /api/auth/invite`, `/activate`, `/password-reset/request` y `/password-reset/confirm`;
  el frontend tiene una pantalla completa "Olvidé mi contraseña"
  (`Club12-WebClient/src/views/auth/forgotPassword.tsx`). Ver HU-09/HU-10 actualizadas.
- **D7 — [NUEVO] Temporada (Season) agrupa Torneos; no reemplaza el scoping por torneo.** Una
  `Season` (`Club12-Backend/Domain/Entities/Models/Season.cs`) agrupa varios torneos del mismo
  período competitivo (habitualmente uno masculino + uno femenino). Es el punto de entrada del panel
  admin (reemplaza a "Torneos" como ítem top-level del sidebar — ver HU-26 actualizada). El FK
  `Season → Tournament` es `SetNull`, no `Cascade`: borrar/limpiar una temporada nunca borra sus
  torneos ni su historial. Ver HU-113 (nueva).

### Reglas de negocio resueltas (antes eran incongruencias)

- **R1 — Sanción en fechas, cleanup por días.** Sigue vigente tal cual (HU-75).
- **R2 — Todo scopeado a la temporada.** Sigue vigente (HU-98), ahora con la capa Season encima (D7).
- **R3 — Fixture se genera al iniciar el torneo, tras asignar equipos.** Sigue vigente, confirmado en
  código: `TournamentService.cs` genera el fixture únicamente dentro de la transición a `Ongoing`
  ("En curso"). Ver HU-64/HU-108.
- **R4 — Básquet sin empates.** Sigue vigente en fase de grupos (HU-70). La prórroga
  (`WentToOvertime`) es un campo puramente informativo que garantiza que no haya empate; el avance
  automático de ganador entre rondas de bracket (HU-82) ya está implementado — ver Épica 24.

---

## Épica 1 — Autenticación y acceso

### HU-01 · Login oculto por URL — `M` ✅
Sigue vigente tal cual. Sin link visible en la nav pública; solo accesible tipeando `/login`.

### HU-02 · Ocultar header y footer en el login — `S` ✅
Sigue vigente. `/login` se renderiza fuera de `PublicLayout`.

### HU-03 · [BUG] Redirigir al home al cerrar sesión — `M` ✅
Sigue vigente y arreglado (`SidebarLayout.tsx`'s `handleLogOut` navega a home tras `logOut()`).

### HU-04 · [BUG] Página 404 sin layout — `S` ✅
Sigue vigente. El catch-all `*` renderiza `<NotFound/>` sin header/footer, con botón al inicio.

---

## Épica 2 — Roles y usuarios

### HU-05 · Simplificar el modelo de roles a Owner y Admin IT — `M` ✅
Sigue vigente (ver D1).

### HU-06 · Editar mi perfil y contraseña — `M` ✅
Sigue vigente. `/panel/configuracion/editar-perfil` y `/panel/configuracion/cambiar-password`.

### HU-07 · Listado de usuarios con soft delete — `S` ✅
Sigue vigente: `isActive` (activar/desactivar) es un toggle distinto de eliminar.

### HU-08 · Blanquear contraseña / editar usuario a pedido — `S` ✅
Sigue vigente.

### HU-09 · Alta de usuario por invitación con magic link — `M` 🔄
**Ya NO es Fase 2 (ver D6) — está en producción.**
**Como** owner/admin **quiero** crear un usuario cargando solo su email y que reciba un magic link
**para** que active su cuenta desde su correo.
- `POST /api/auth/invite` (Admin/Owner) crea el usuario sin contraseña y dispara el mail de
  activación; `POST /api/auth/activate` consume el token y define la primera contraseña.
- Ruta admin: `/panel/usuarios/invitar`.

### HU-10 · Reset de contraseña self-service por magic link — `M` 🔄
**Ya NO es Fase 2 (ver D6) — está en producción.**
**Como** usuario **quiero** pedir un magic link para restablecer mi contraseña **para** recuperarme
sin soporte.
- `POST /api/auth/password-reset/request` + `/confirm`; pantalla "¿Olvidaste tu contraseña?" enlazada
  desde el login.

---

## Épica 3 — Portada pública, noticias y vista pública del torneo

### HU-11 · Landing pública con accesos directos — `M` 🔄
**Cambió el contenido de la portada, no el principio.**
**Como** visitante anónimo **quiero** una portada que me lleve rápido a lo que me interesa sin
loguearme **para** no perder tiempo navegando.
- Hero con dos CTA: **"Ver temporadas"** y **"Campeones"** (ya no "torneos destacados" ni accesos
  directos a sanciones).
- Sección **"Temporadas"** (no "torneos") con las temporadas recientes — consistente con D7: la
  portada ahora es season-first.
- Sección de campeones recientes y feed de últimas noticias (ver HU-12).
- No requiere autenticación.

### HU-12 · Feed de noticias con "ver todas" — `M` ✅
Sigue vigente: últimas noticias en home + "ver todas" a `/blog` paginado.

### HU-13 · Detalle de noticia con URL por slug — `M` ✅
Sigue vigente (`/blog/:idOrSlug`).

### HU-14 · Vista pública del torneo (fixture, resultados y posiciones) — `S` 🔄
**Sigue existiendo, pero ya no es el punto de entrada principal.**
**Como** visitante **quiero** ver el fixture, resultados y posiciones de un torneo sin loguearme
**para** seguir la competencia.
- Camino principal: `/temporadas` → temporada → tarjeta de torneo → `/torneos/:tournamentId`.
- La ruta plana `/torneos` también sigue existiendo.
- Solo lectura.

### HU-15 · [BUG] Ocultar el ID en la URL cuando debería ir un slug — `S` ✅
Sigue vigente: torneo, equipo, partido, división, sanción y blog navegan por slug donde existe.

### HU-16 · Estado borrador / publicada de noticia — `S` 🔄
**Ya no es "[NUEVA]" — está implementado.** Chip Publicada/Borrador en el listado admin, switch en
edición; solo las publicadas aparecen en público.

### HU-17 · Compartir noticia con Open Graph — `C` 🔄
**Ya no es "[NUEVA]" — está implementado**, y se extendió más allá de noticias: `pageMetadata.ts`
genera Open Graph/Twitter Card para blog, equipo, torneo y temporada, no solo noticias.

---

## Épica 4 — Contenido institucional

### HU-18 · Ver "Quiénes somos" — `M` ✅
### HU-19 · Ver reglamento — `M` ✅
Ambas siguen vigentes tal cual, sin ruta de edición en el panel (D3).

### HU-20 · Descargar plantilla de ficha médica — `M` 🔄
Sigue vigente, pero la descarga cambió de mecanismo: hoy es un asset estático público
(`/documents/ficha-medica-club12.pdf`, link `download` directo), no un endpoint del backend.

### HU-21 · [BUG] Arreglar la descarga de ficha médica en producción — `M` ⚠️
**No verificable desde el código si el bug original sigue existiendo** — pero el cambio de mecanismo
(HU-20: ahora es un asset estático servido por el frontend, no un endpoint backend) cambia por
completo el modo de falla a auditar. Si vuelve a fallar en producción, el sospechoso ya no es un
problema de ruta/permiso del backend, sino si el PDF quedó efectivamente incluido en el build
estático desplegado.

---

## Épica 5 — Sanciones públicas

### HU-22 · Consulta pública de sanciones — `M` ✅
### HU-23 · [BUG] Buscar sanciones por nombre de jugador — `M` ✅
Sigue vigente y verificado con tests (`PlayerSanctionSearchTests.cs`): el buscador matchea contra el
nombre del jugador sancionado, no solo contra el motivo.

### HU-24 · [BUG] Búsqueda por coincidencia parcial (contains) — `M` ⚠️
**`contains` case-insensitive: sí. Insensible a acentos: NO, y es una decisión deliberada, no un bug
pendiente.** El código documenta por qué: requeriría una extensión de Postgres (`unaccent`/`citext`)
no habilitada en el proyecto, y el proveedor de test en SQLite no la traduciría. Este criterio de
aceptación debería sacarse del doc en vez de quedar como pendiente.

### HU-25 · Filtrar sanciones por torneo — `S` ✅
Sigue vigente, combinable con la búsqueda por nombre.

---

## Épica 6 — Panel de administración: navegación

### HU-26 · Simplificar la navegación lateral — `M` 🔄
**El sidebar cambió más de lo que esta historia anticipaba (ver D7/HU-113).**
**Como** owner/admin **quiero** un sidebar agrupado y sin entidades sueltas **para** navegar por
flujo, no por tabla.
- Divisiones, Fases y Partidos siguen sin ítem propio (se gestionan desde el torneo/división).
- **"Torneos" tampoco tiene ítem propio.** El ítem top-level del grupo "Competición" es
  **"Temporadas"**; un torneo se alcanza siempre entrando por su temporada
  (`Club12-WebClient/src/views/core/components/SidebarLayout.tsx`, con un test de regresión
  (`SidebarLayout.test.tsx`) que confirma que "Torneos" no está en el DOM).
- Se agregó **"Inscripción de equipos"** (`/panel/registro-equipos`) como ítem propio del grupo de
  gestión de equipos, y **"Equipos y planteles"** (Épica 10).

### HU-27 · El asistente de torneo no vive en el sidebar — `S` 🔄
Sigue vigente en el principio, pero el botón "Nuevo torneo" que lo dispara ya no vive en una página
global de Torneos (que ya no existe en el sidebar) — vive en el detalle de una Temporada
(`AdminSeasonDetailPage.tsx`), y pre-scopea el asistente a esa temporada (`seasonId` en el estado de
navegación).

### HU-28 · Ajustes de estilo del panel — `C`
No re-verificado en esta pasada (ítem cosmético); sin evidencia de que se haya revertido.

---

## Épica 7 — Gestión de torneos

### HU-29 · Listado y CRUD de torneos — `M` ❌ OBSOLETA (reemplazada por el flujo de HU-26/HU-27)
La tabla plana de torneos (`TournamentsPage.tsx`, ruta `/panel/torneos`) sigue existiendo como
componente/ruta, pero **no está enlazada desde ningún lado** del panel — quedó huérfana. El flujo
real hoy es: Temporada → tarjetas de torneo agrupadas por categoría (dentro de
`AdminSeasonDetailPage.tsx`). Candidata a eliminarse del código o a documentarse como vista de
respaldo, no como el flujo principal.

### HU-30 · Vista de torneo con sub-tabs — `M` 🔄
**Son 4 tabs, no 2.** `TournamentPage.tsx`: `Detalle | Divisiones | Equipos | Asignación`. El tab
**Asignación** es nuevo (ver HU-108) y no tenía HU propia en la versión anterior de este documento.

### HU-31 · Bloquear alta/edición de estructura si el torneo ya empezó — `M` ✅
Sigue vigente, con mensaje explícito en el backend
(`ErrorMessages.StructuralEditNotAllowed`): los cambios estructurales solo se permiten en
`OpenForRegistration`.

### HU-32 · [BUG] Contador de equipos por división en 0 — `M`
No re-verificado en esta pasada.

### HU-33 · [BUG] Botón deshabilitado con "no hay equipos" cuando sí hay — `M`
No re-verificado en esta pasada.

### HU-34 · Quitar mínimo/máximo de equipos del torneo — `S` ✅
Sigue vigente: `minTeams`/`maxTeams` no existen en `Tournament`, el wizard ni la request de
creación (migración `20260828010238_DropTournamentTeamCountLimits`).

---

## Épica 8 — Máquina de estados del torneo

### HU-35 · Transiciones de estado — `M` 🔄
**Ya no es estrictamente forward-only.**
Ciclo: `Programado → Inscripción abierta → Inscripción cerrada → En curso → Finalizado`, más
`Cancelado` desde cualquier estado no terminal — **y una excepción agregada**: `En curso →
Inscripción cerrada` ("revertir a borrador"), solo si el torneo no tiene partidos jugados. Ver
HU-114 (nueva).
- ⚠️ **Brecha detectada**: el mapa de transiciones del frontend
  (`Club12-WebClient/src/modules/tournament/utils/tournamentStatusTransitions.ts`) todavía lista
  solo `Finished`/`Canceled` como próximos estados válidos desde `Ongoing` — no incluye la reversión.
  El botón dedicado "Revertir a borrador" en `TournamentPage.tsx` funciona igual porque no usa ese
  mapa, pero cualquier OTRO lugar de la UI que sí lo use (ej. un selector genérico de próximo estado)
  no va a ofrecer esta opción. Corregir el mapa para que quede consistente con el backend.

### HU-36 · "Programado" distinto de "Inscripción abierta" — `S` ✅
### HU-37 · "Inscripción cerrada" habilita la asignación — `M` ✅
Ambas siguen vigentes tal cual (y la nota "supersedida por HU-108" de la versión anterior del doc
sigue siendo correcta: el fixture se genera al pasar a "En curso", no al cerrar inscripción).

---

## Épica 9 — Asistente de creación de torneo

### HU-38 · Crear el torneo completo en una sola transacción — `M` ✅
Sigue vigente: el wizard envía la estructura completa en un solo `createFullTournament`.

### HU-39 · Datos base del torneo con fechas — `M` 🔄
**Se agregó un campo obligatorio que no estaba en la versión original: la Temporada.**
El primer paso del wizard ahora también exige elegir `seasonId` ("El torneo debe pertenecer a una
temporada.") — consistente con D7/HU-113. Nombre, descripción y fechas siguen igual.

### HU-40 · Validar que el límite de inscripción sea anterior al inicio — `S` ✅

### HU-41 · Selección de equipos — `M` ❌ OBSOLETA, tal como ya anotaba la versión anterior
Confirmado: el wizard no tiene paso de selección de equipos (`STEP_LABELS` no incluye "Equipos").
La inscripción vive en HU-107, la asignación a zonas en HU-108.

### HU-42 · Un equipo asignado a una zona no aparece en otra — `M` ❌ trasladada a HU-108
Sin cambios respecto a la nota anterior: se valida en la asignación post-cierre, no en el wizard.

### HU-43 · Configurar la fase de grupo (cantidad de enfrentamientos) — `M` ✅
### HU-44 · Configurar playoffs con varias copas — `M` ✅
Ambas siguen vigentes.

### HU-45 · Mapear posiciones de cada división a cada playoff por rangos — `M` 🔄
**Ya no lo edita el admin a mano en el wizard — se deriva automáticamente.**
El editor manual de rangos ("1-4 → Oro, 5-8 → Plata") ya no existe en el wizard: los rangos se
**derivan** de la cantidad de clasificados (`qualifiers`) que cada copa tiene configurada, en el
orden en que las copas fueron agregadas (`deriveCupMappings`, ver HU-112 nueva). La validación de
rangos sin solapes sigue existiendo, pero solo como guarda defensiva del lado del servidor
(`PlayoffMappingValidator.cs`).

### HU-46 · Configurar la serie final al mejor de N — `M` ⚠️
**El campo de configuración sigue existiendo y funciona en el wizard/UI — pero ver la brecha grande
en HU-82: un "mejor de 3" configurado hoy NO genera 3 partidos reales de forma automática en
producción.**

### HU-47 · Copa / división cruzada con playoff obligatorio — `M` ⚠️
**Brecha de validación client-side detectada.** El backend sigue modelando la copa cruzada con playoff
obligatorio, pero la validación del PASO del wizard (`validateCrossCupStep`) no chequea
`cups.length > 0` — el estado inicial arranca con `cups: []`, así que, tal como está escrito hoy, el
formulario permitiría avanzar con una copa cruzada sin ningún playoff configurado, contradiciendo la
regla. No confirmado si el backend rechaza esto de forma independiente al recibir la request — a
revisar antes de confiar en que está cubierto.

### HU-48 · Femenino como torneo separado, por diseño — `M` ✅

### HU-49 · Paso de revisión antes de crear — `S` ✅
Sigue vigente tal cual la nota anterior: el resumen muestra estructura (divisiones, formato, copas,
series), no equipos por zona (porque ya no se seleccionan en el wizard).

---

## Épica 10 — Equipos y planteles

### HU-50 · Sección de Equipos en el panel — `M` ✅

### HU-51 · Tab de jugadores dentro del equipo — `M` 🔄
Sigue vigente, pero el equipo tiene más tabs que antes: Detalle, **Jugadores**, Puntuaciones,
Sanciones y **Cuerpo técnico** (nuevo, ver HU-116).

### HU-52 · Alta de jugadores al plantel — `S` 🔄
**El mecanismo cambió de formulario modal a fila editable en la tabla.**
**Como** owner/admin **quiero** cargar jugadores directamente en la tabla del plantel **para** dar de
alta varios sin abrir un popup por cada uno.
- "Nuevo Jugador" agrega una fila en modo edición directamente en el `DataGrid` del plantel — no una
  ventana modal separada.
- La fila nueva se completa celda por celda (Nombre/Segundo nombre/Apellido/Documento/Fecha de
  nacimiento/Equipo/Teléfono/Obra social); Guardar valida y crea; Descartar la elimina sin llamar a la
  API.
- Los jugadores YA existentes en la tabla siguen sin ser editables ahí mismo — la edición de un
  jugador existente vive en su página de detalle, no inline (decisión explícita, con test de
  regresión: "no ofrece una acción Editar en la fila — la edición vive en el detalle").

### HU-53 · Copiar plantel de una temporada anterior — `C` 🔄 REUBICADA, no eliminada
**El botón "Importar plantel de una temporada anterior" del tab Jugadores del equipo YA NO EXISTE
— fue reemplazado por importación CSV (HU-117, nueva).** Pero la funcionalidad de copiar un plantel
entero de una temporada previa **sigue existiendo**, reubicada dentro del flujo de **inscripción de
un equipo existente a un nuevo torneo** (`EnrollTeamDialog.tsx`, checkbox "Copiar plantel de su
temporada anterior") — es decir, sigue siendo parte de HU-107, no una acción separada del tab de
plantel.

### HU-54 · Límites de plantel y dorsales únicos — `C` ✅
Sigue vigente tal cual: tope configurable de jugadores por plantel, dorsal único por equipo+torneo,
un jugador no puede estar en dos equipos del mismo torneo (`PlayerService.RegisterPlayerToTeamAsync`).

---

## Épica 11 — Ficha médica y elegibilidad

(Flujo E2E sin cambios respecto a la versión anterior: descarga de plantilla → owner sube y aprueba
la ficha por jugador+equipo+torneo → habilitación → elegibilidad en la carga del partido.)

### HU-55 · Cargar ficha médica a un jugador para ese torneo y equipo — `M` ✅
### HU-56 · Bucket de almacenamiento de archivos — `M` 🔄
**Ya no es "hoy no está habilitado" — está implementado.** Bucket privado `medical-records` dedicado
(`SupabaseMedicalRecordStorage.cs`), con `StoreAsync`/`DownloadAsync` y tests propios. El código está
listo; el único trabajo restante por ambiente es de aprovisionamiento/infra, no de desarrollo.

### HU-57 · Habilitación del jugador según ficha aprobada — `M` 🔄
Sigue vigente, con una regla adicional: la habilitación también exige que exista un **archivo
realmente almacenado**, no alcanza con el estado `Approved` sin referencia de archivo.

### HU-58 · Aprobar / rechazar ficha médica — `S` ✅
### HU-59 · Ficha médica nueva por temporada — `S` ✅
### HU-60 · Solo son elegibles los jugadores habilitados — `M` ✅
### HU-61 · Un jugador sancionado no es convocable — `M` ✅
### HU-62 · Aviso de jugador no habilitado al armar la fecha — `C` 🔄
**El aviso ahora lista a TODOS los jugadores no elegibles de una planilla, no solo el primero
encontrado — y por nombre real, nunca por UUID crudo.** Corregido: antes,
`PlayerStatisticService` tiraba una excepción apenas encontraba el PRIMER jugador sancionado o no
habilitado, con el ID del jugador crudo en el mensaje (`El jugador {guid} no está habilitado...`) —
esto obligaba al admin a corregir la planilla de a un jugador por vez, y el mensaje era ilegible.
Ahora (`PlayerStatisticService.FindRosterEligibilityIssuesAsync`) junta TODAS las violaciones de
ambos equipos antes de tirar un único error agrupado por equipo, con el nombre real de cada
jugador (`ErrorMessages.MatchSheet.PlayersNotEligible`).

Las seis siguen vigentes en el fondo; ver también HU-73 y HU-109 (nuevas reglas relacionadas
agregadas en esta pasada).

### HU-118 · [NUEVO] Umbral mínimo de habilitados por equipo (walkover automático) — `M`
**Como** owner/admin **quiero** que el sistema bloquee la carga de un resultado normal si un equipo
no tiene suficientes jugadores habilitados **para** forzar que ese partido se cargue como walkover
en vez de como un resultado jugado.
- Un equipo necesita **al menos 4 jugadores habilitados** de su plantel de esa temporada para que se
  le pueda cargar un resultado normal (`PlayerStatisticService.EnsureTeamMeetsHabilitadoMinimumAsync`,
  usa `TournamentCompletabilityValidator.MinPlayersPerTeam`, que bajó de 5 a 4 y ahora cuenta
  jugadores **habilitados**, no simplemente registrados en el plantel).
- Se chequea ANTES de mirar qué jugadores puntuaron en la planilla — un equipo por debajo del
  umbral falla esta regla aunque la planilla venga vacía.
- Si un equipo no llega al mínimo, `PUT /api/matches/{id}/result-from-sheets` y
  `PUT /api/matches/{id}/result-from-team-sheet` rechazan con 409 y un mensaje que indica cuántos
  habilitados tiene y que debe cargarse como walkover (`ErrorMessages.MatchSheet.TeamRequiresWalkOver`);
  el walkover en sí sigue siendo una acción manual del admin ("Marcar W.O.", HU-73), no automática —
  el sistema bloquea el camino incorrecto, no ejecuta el correcto por su cuenta.

### HU-119 · [NUEVO] Torneo no puede iniciar si algún equipo no llega al mínimo de habilitados — `M`
**Como** owner/admin **quiero** que el sistema me impida iniciar un torneo si algún equipo inscripto
no tiene suficientes jugadores habilitados **para** no arrancar una temporada que va a terminar en
walkovers por incumplimiento de plantel.
- Extiende la guarda de completitud existente (HU-109): la regla `TeamTooFewPlayers` de
  `TournamentCompletabilityValidator` ahora cuenta jugadores **habilitados** (ficha médica
  Aprobada + archivo real almacenado) en vez de simplemente registrados, y el mínimo bajó de 5 a 4
  — alineado con el umbral de walkover de HU-118.
- Un equipo con 5 jugadores registrados pero ninguno habilitado ya NO pasa esta guarda (antes sí,
  porque solo contaba registros); el torneo se queda bloqueado en `RegistrationClosed` hasta que se
  corrija.

---

## Épica 12 — Fixture y jornadas

### HU-63 · Partidos agrupados por jornada — `M` ✅

### HU-64 · Generar el fixture automático al cerrar inscripción — `M` ❌ el disparador que describe está mal
**El disparador real es "En curso" (iniciar el torneo), no "Inscripción cerrada".** Esta era ya una
inconsistencia interna del doc anterior (contradecía a HU-37/HU-108, que sí tenían el disparador
correcto). El código lo confirma sin ambigüedad: `TournamentService.cs` solo llama
`GenerateFixtureAsync` dentro de la rama `newStatus == TournamentStatus.Ongoing`. El resto de la
historia (aleatorio, respeta enfrentamientos configurados, idempotente por división) sigue vigente.

### HU-65 · Cantidad de fechas y calendario del fixture — `M` ✅
### HU-66 · Asignar cancha al partido más tarde — `M` ✅
### HU-67 · Orden de jornadas fijo — `M` ✅
### HU-68 · Reprogramar / suspender un partido — `C` 🔄
Sigue vigente, con una precisión de ubicación: la acción vive en la **lista de fixture** (fila del
partido), no en la página de detalle del partido — no es un cambio funcional, solo dónde está el
botón.

---

## Épica 13 — Carga del partido: resultado y goleadores

### HU-69 · Cargar el resultado del partido — `M` 🔄
Sigue vigente, con un campo nuevo: checkbox **"Se jugó tiempo extra"** (`Match.WentToOvertime`) — es
puramente informativo, no cambia el cálculo del marcador ni la regla de sin-empates (HU-70). **Además**,
la carga ahora está bloqueada si algún equipo no llega al mínimo de jugadores habilitados — ver
HU-118 (nueva).

### HU-70 · Sin empates: todo partido cargado tiene ganador — `M` ✅
### HU-71 · Cargar goleadores del partido — `M` ✅
### HU-72 · Persistir anotadores y conectarlos al ranking — `M` ✅

### HU-73 · Walkover / ausencia (W.O.) — `S` 🔄
Sigue vigente tal cual como acción MANUAL del admin ("Marcar W.O.", elige el equipo presente,
`MatchService.LoadWalkOverAsync`). **Precisión agregada en esta pasada**: el sistema no dispara un
walkover solo — lo que hace es bloquear la carga de un resultado NORMAL cuando un equipo no llega al
mínimo de 4 habilitados (HU-118), dejando el walkover manual como el único camino disponible para
cerrar ese partido. No hay detección automática de "equipo ausente" más allá de esa guarda.

---

## Épica 14 — Gestión de sanciones (administración)

### HU-74 · Crear una sanción desde el partido — `M` ✅
### HU-75 · Duración de la sanción en fechas, cleanup por días — `M` ✅
### HU-76 · Vencimiento automático de sanciones — `S` ✅
### HU-77 · Sanción también a equipo o staff — `C` ✅
Sigue vigente: `SanctionSubjectType` (Jugador/Equipo/Staff) existe en el modelo y la consulta pública
distingue el tipo de sujeto.

**Sin HU en la versión anterior: workflow de apelación** — ver HU-115 (nueva).

---

## Épica 15 — Clasificación y playoffs

### HU-78 · Tabla de posiciones por zona/división — `M` 🔄
Sigue vigente, con un agregado sin HU previa: en una división **sin fase de playoff**, el 1er puesto
de la tabla pública se corona (ícono + fila destacada) **en vivo**, apenas es el líder actual — no
espera a que termine la temporada (`crownFirstPlace` en `PublicDivisionPanel.tsx`/
`divisionStandings.tsx`). Corregido en esta sesión tras una corrección del owner: la primera versión
solo coronaba una vez decidido el podio; el criterio correcto es que el color siga a la posición
actual, igual que el resto del tinte por rango de clasificación.

### HU-79 · Sistema de puntaje configurable — `S` ✅
### HU-80 · Desempate de la tabla (fase de grupos) — `M` ✅

Las dos siguen vigentes tal cual.

### HU-81 · Definir los clasificados a playoffs desde la tabla — `M` 🔄
Sigue vigente en el concepto, con dos cambios: los rangos ya no son un editor manual (ver HU-45), y
**el poblado de las copas ya no requiere un paso manual separado** — ver HU-82.

### HU-82 · Bracket de playoffs con siembra, prórroga y BYE — `M` ✅
**Como** usuario **quiero** ver el cuadro de playoffs con los cruces sembrados **para** seguir la
definición.
- ✅ **Auto-siembra**: al completarse la fase de grupos de una división, las copas de playoff se
  siembran **automáticamente** (`StageService.TryAutoSeedPlayoffPhaseAsync`), sin acción manual del
  admin. No pisa una siembra ya hecha a mano.
- ✅ **BYE**: sigue funcionando para clasificados que no son potencia de 2.
- ✅ **Sin empates** en fase de grupos; prórroga (`WentToOvertime`) es el mecanismo, ver HU-69.
- ✅ **"Serie al mejor de N" (HU-46) se genera de verdad en producción (corregido 2026-09-02).**
  `StageService.SeedPlayoffCupsAsync`/`SeedKnockoutStageAsync`/`SeedMultiGroupCrossCupStageAsync`
  crean un `MatchSeries` real (vía `MatchSeriesService`) para cada cruce cuando `Stage.BestOf > 1` —
  ya no siembran un `Match` plano. `SeriesInProgressPanel` (tab Playoff de la división, admin)
  agrega la acción de UI que faltaba para cargar el 2º/3er partido de una serie
  (`addGameToSeries`).
- ✅ **El ganador de cada serie avanza automáticamente (corregido 2026-09-02).**
  `StageService.TryAdvanceStageWinnerAsync` toma el ganador de cada slot recién decidido (serie o
  partido único) y lo carga en el slot correspondiente de la ronda siguiente del mismo bracket —
  llamado desde las 3 acciones de carga de resultado de `MatchController`, y también desde el propio
  seeding para propagar los BYE automáticamente.
- ✅ **Partido por el tercer puesto, opcional por copa.** `StageType.ThirdPlace` es un tipo de etapa
  propio; cuando la copa lo incluye, los dos perdedores de semifinal avanzan automáticamente a ese
  partido (`StageService.AdvanceLosersToThirdPlaceAsync`). No tenía HU propia en ninguna versión
  anterior de este documento.

---

## Épica 16 — Estadísticas y rankings

### HU-83 · Estadísticas globales del sistema — `M` ✅

### HU-84 · Estadísticas por torneo — `M` 🔄
**No existe un tab "Estadísticas" dentro del torneo como tal.** `TournamentPage.tsx` tiene
`Detalle | Divisiones | Equipos | Asignación`, sin tab de estadísticas. Las estadísticas scopeadas a
un torneo viven en los tabs de cada división (Posiciones/Goleadores/Partidos/Playoff) y en la página
global `StatisticsPage.tsx`, filtrable por torneo/temporada.

### HU-85 · Estadísticas por temporada y de todos los tiempos — `S` ✅
Sigue vigente tal cual (`ScorerRepository.GetPlayerScoresAsync`).

### HU-86 · Ranking de goleadores — `S` 🔄
**Más simple de lo que describía la versión anterior — corregir las expectativas, no solo la
ubicación.**
- ✅ Ahora tiene un lugar fijo en el panel: tab **"Goleadores"** dentro de la división admin
  (`DivisionScorersTable.tsx`), con Imprimir + Exportar CSV, igual que Posiciones.
- ✅ Se alimenta de la misma carga de HU-71/72, sin escritura manual aparte.
- ✅ Muestra jugador, dorsal (de la inscripción del torneo, no del puntero denormalizado), puntos y
  equipo.
- ❌ **NO** muestra partidos jugados ni promedio — esos campos no existen en `ScorerByPlayerResponse`.
- ❌ **NO** hay ninguna lógica de desempate — el orden es `OrderByDescending(Points)` liso, sin
  segundo criterio. Si dos jugadores empatan en puntos, el orden entre ellos es indefinido.

### HU-87 · Ficha de estadística individual del jugador — `C` ✅
### HU-88 · Historial de un jugador entre temporadas — `C` ✅

Ambas siguen vigentes tal cual.

### HU-86b · [NUEVO] Top 10 en el ranking de goleadores público — `S`
**Como** visitante **quiero** ver un ranking de goleadores corto en la página pública del torneo
**para** no tener que scrollear una lista larga en una vista que es solo un vistazo rápido.
- La tabla de goleadores de la página pública de una división limita a los primeros 10
  (`PUBLIC_TOP_SCORERS_LIMIT` en `PublicDivisionPanel.tsx`, prop `limit` de `DivisionScorersTable`).
- El panel admin (HU-86) NO tiene este límite — sigue mostrando el ranking completo, paginado.

### HU-89 · Exportar posiciones / estadísticas — `C` 🔄
**Solo CSV — no hay exportación a PDF.** El botón "Imprimir" abre el diálogo de impresión nativo del
navegador sobre una hoja con estilos de impresión (el usuario puede elegir "Guardar como PDF" desde
ahí), pero no hay generación de PDF del lado de la app (`jsPDF`/`pdfmake`/servidor) en ningún punto
del código. Corregir "CSV/PDF" a "CSV + impresión nativa del navegador".

---

## Épica 17 — Canchas

### HU-90 · CRUD de canchas — `M` 🔄
Sigue vigente, con más de lo que describía: además de nombre/dirección/imagen, cada cancha tiene
**latitud/longitud** con un **mapa interactivo (Leaflet/OpenStreetMap)** — geocodificado
automáticamente desde la dirección tipeada, con un pin arrastrable a mano para corregir. También
tiene `Slug` (link público estable).

---

## Épica 18 — Respaldos y administración de datos

### HU-91 · Generar respaldo manual — `M` ✅
### HU-92 · Bloquear la app durante respaldo/restauración — `M` ✅

### HU-93 · Respaldo de seguridad automático antes de restaurar — `M` 🔄
Sigue vigente en el mecanismo (se genera una salvaguarda antes de restaurar), con una corrección: la
salvaguarda **NO se elimina** al terminar con éxito — queda en el catálogo de respaldos, sujeta solo
a la poda normal por retención en el próximo backup.

### HU-94 · Límite de copias de seguridad — `S` 🔄
El tope por defecto es **7**, no 5 (`BackupOptions.RetentionCount = 7`).

### HU-95 · Respaldos programados — `S` 🔄
El intervalo por defecto es **diario (24 horas)**, no semanal (`BackupOptions.IntervalHours = 24`).
Es configurable, pero "semanal" no es lo que trae por defecto.

### HU-96 · Borrado total de datos — `S` ✅
Sigue vigente, restringido a `AdminOrOwner` con confirmación fuerte.

### HU-97 · Carga de datos de prueba (solo Admin IT) — `S` ❌ ELIMINADA (deliberado, no un bug)
El botón "Cargar Datos de prueba" fue sacado del panel de Administración de datos. Esta historia debe
retirarse del documento, no marcarse como pendiente.

---

## Épica 19 — Modelo season-scoped (fundacional)

### HU-98 · Equipos y jugadores scopeados por temporada/torneo — `M` 🔄
El principio sigue vigente tal cual (`TeamTournamentRegistration`/`PlayerTeamRegistration` como
fuente de verdad, sin reasignar `Team.TournamentId`), pero la historia conflaba "temporada" y
"torneo" como lo mismo. Ya no lo son — ver D7/HU-113: `Season` es una entidad propia por encima de
`Tournament`. El scoping de equipos/jugadores sigue siendo a nivel Torneo, sin cambios.

### HU-99 · Identidad de club estable entre temporadas — `C` 🔄
**Ya no es "[NUEVA]"/especulativa — está implementada.** Entidad `Club` real
(`Club12-Backend/Domain/Entities/Models/Club.cs`) con `Team.ClubId` opcional y su propio
`ClubService`. Reclasificar de Could/no-empezada a Must/hecha.

---

## Épica 20 — Coherencia técnica transversal

### HU-100 · Zona horaria Argentina en fechas y horarios — `S` ✅
Sigue vigente: almacenamiento en UTC, presentación en `America/Argentina/Buenos_Aires`.

### HU-101 · Auditoría de acciones sensibles — `C` ⚠️ brecha real detectada
**Como** admin IT **quiero** un registro de quién borró datos, restauró respaldos o cambió estados
**para** trazabilidad.
- ✅ Los 4 tipos de acción de la historia original están cubiertos por el enum (`AuditAction`:
  `DataWipe`, `BackupRestore`, `TournamentStatusChange`, `PasswordReset`) — no hay auditoría de
  "toda mutación administrativa", solo estas 4 (consistente con `Docs/ESTADO-Y-REGLAS.md`, que ya
  listaba "auditoría completa" como pendiente).
- ✅ Nombres legibles: se resuelve el nombre real del objetivo (torneo, usuario por email) en vez de
  mostrar un UUID crudo, y el detalle queda en español (`ToSpanishLabel()`).
- ✅ **`AuditAction.BackupRestore` se loguea (corregido 2026-09-02).** `BackupOperationsService`
  ahora recibe `IAuditService` y llama `LogAsync` al final de `RestoreBackupAsync`.

---

## Épica 21 — Rediseño visual

### HU-102 · Design system: tema oscuro con acentos naranja — `M` ✅
### HU-103 · Aplicar el tema al sitio público — `S` ✅
### HU-104 · Aplicar el tema al panel de administración — `S` ✅
### HU-105 · Consistencia responsive y accesibilidad — `C` 🔄
Las cuatro siguen vigentes en el principio; se confirmó además una implementación de sidebar
responsive específica (AppBar colapsable + drawer temporal en mobile, comentarios de código citan
"HU-105" explícitamente) que la versión anterior del doc no detallaba.

---

## Épica 22 — Rediseño del flujo de inscripción y armado del torneo

### HU-106 · El asistente crea torneo + estructura, sin equipos ni fixture — `M` ✅
### HU-107 · Inscripción de equipos con el torneo abierto — `M` 🔄
Sigue vigente tal cual, con una precisión: ahora tiene una **ruta/ítem de sidebar propios**
(`/panel/registro-equipos`, "Inscripción de equipos"), separados tanto del wizard como de la
asignación a zonas — es un paso de su propio ciclo de vida, no una sub-pantalla de otra cosa. La
opción "copiar plantel de temporada anterior" (HU-53) vive acá.

### HU-108 · Asignar inscriptos a divisiones y generar el fixture al iniciar — `M` 🔄
Sigue vigente tal cual, con una precisión de UI: el paso de asignación tiene su propio tab
**"Asignación"** en `TournamentPage.tsx` (ver HU-30).

### HU-109 · Guardas de completitud — `M` 🔄
Sigue vigente: `TournamentCompletabilityValidator` implementa las 6 reglas (zona con muy pocos
equipos, equipo sin asignar, equipo en más de una zona, rango de playoff más allá de los equipos
asignados, grupo de copa cruzada con muy pocos equipos, y equipo con muy pocos jugadores), con su
propia suite de tests. **La última regla cambió en esta pasada — ver HU-119 (nueva)**: ya no cuenta
jugadores simplemente registrados sino habilitados, y el mínimo bajó de 5 a 4.

### HU-110 · Copa cruzada con múltiples grupos de tamaño variable — `M` 🔄
**El "gap actual" que anotaba la versión anterior ya está cerrado.**
`Division.QualifiersPerGroup` es un campo de primera clase; el diseño elegido es una única División
`IsCrossDivisionCup=true` que contiene VARIOS `Stage` de tipo Grupo (no N divisiones cruzadas como
especulaba el doc viejo). El ejemplo real también cambió: ya no es "37 equipos → 10 grupos" — el
seed actual usa una Copa Cruzada masculina de 6 zonas de fase de grupos (5 de 4 equipos + 1 de 3,
ida y vuelta) alimentando un bracket combinado de 12 con byes.

### HU-111 · Calendario: jornadas de zona y copa no se solapan — `M` ✅
Sigue vigente: zonas juegan domingos, copa cruzada otro día fijo, mismo helper compartido
(`RoundCalendar`) para producción y seed.

### HU-112 · [NUEVO] Auto-derivación de las rondas de una copa según su cantidad de clasificados — `M`
**Como** owner/admin **quiero** que la cantidad de rondas de una copa (Final / Semis+Final /
Cuartos+Semis+Final...) se calcule sola a partir de cuántos equipos clasifican **para** no tener que
configurar cada ronda a mano.
- Al definir `qualifiers` (cantidad de clasificados) para una copa, el wizard deriva automáticamente
  su estructura de rondas — referenciado en el propio código como "HU-112"
  (`submitWizard.ts`/`deriveCupMappings`). Reemplaza el editor manual de rangos que describía la
  HU-45 original.

---

## Épica 23 — Temporada, sanciones ampliadas y funcionalidades nuevas sin historia previa

Historias nuevas para funcionalidad que existe en el código pero no tenía HU en ninguna versión
anterior de este documento.

### HU-113 · [NUEVO] Entidad Temporada (Season) agrupando torneos — `M`
**Como** owner/admin **quiero** agrupar los torneos de un mismo período (habitualmente uno masculino
+ uno femenino) bajo una Temporada **para** organizar el club año a año en vez de torneo por torneo
suelto.
- CRUD de Temporada (nombre, año) en el panel; es el punto de entrada del sidebar admin (ver HU-26).
- El detalle de una temporada lista sus torneos agrupados por categoría, con "Nuevo Torneo"
  pre-scopeado a esa temporada.
- Vista pública equivalente (`/temporadas`, `/temporadas/:id`).
- `Season → Tournament` es `SetNull`, no `Cascade`: borrar/limpiar una temporada nunca destruye el
  historial de sus torneos.
- No cambia el scoping de equipos/jugadores (sigue siendo a nivel Torneo, D2/HU-98).

### HU-114 · [NUEVO] Revertir un torneo "En curso" a borrador — `M`
**Como** owner/admin **quiero** poder deshacer el inicio de un torneo si todavía no se jugó nada
**para** corregir una asignación de zonas equivocada sin tener que recrear el torneo entero.
- Transición `Ongoing → RegistrationClosed`, disponible solo si **cero partidos fueron jugados**.
- Borra el fixture generado; conserva las asignaciones de equipos a zona (no hay que rehacerlas).
- Bloqueada (409, con motivo) si algún partido ya se jugó.
- ⚠️ Ver la brecha del mapa de transiciones del frontend anotada en HU-35.

### HU-115 · [NUEVO] Apelación de sanciones — `S`
**Como** equipo/jugador sancionado **quiero** poder apelar una sanción **para** que el admin la
revise antes de que se cumpla en su totalidad.
- Una sanción activa puede pasar a estado "apelada" con motivo; el admin la resuelve como aceptada
  (levanta la sanción) o rechazada (sigue vigente).
- Sin ruta de auto-servicio pública confirmada en esta pasada — a verificar si la apelación la carga
  el propio equipo o el admin en su representación.

### HU-116 · [NUEVO] Cuerpo técnico del equipo (DT / Asistente) — `S`
**Como** owner/admin **quiero** registrar el cuerpo técnico de un equipo (Director Técnico,
Asistente) por temporada **para** llevar esa información igual que el plantel de jugadores.
- Tab "Cuerpo técnico" dentro del equipo (junto a Jugadores/Puntuaciones/Sanciones).
- Roles: solo **DT** y **Asistente** (el rol "DT-Jugador" que existió brevemente fue retirado —
  quedan dos roles, no tres).
- Scopeado por equipo + temporada, igual que el plantel.
- Visible también en la página pública del equipo.

### HU-117 · [NUEVO] Importación de plantel por CSV — `S`
**Como** owner/admin **quiero** cargar varios jugadores de una sola vez subiendo un CSV **para** dar
de alta un plantel completo más rápido que fila por fila.
- Reemplaza al viejo botón "Importar plantel de una temporada anterior" en el tab Jugadores del
  equipo (esa funcionalidad de copiar-de-temporada-previa sigue existiendo, pero reubicada — ver
  HU-53).
- Columnas en el mismo orden que la fila editable de alta manual (HU-52): Nombre, Segundo nombre,
  Apellido, Documento, Fecha de nacimiento, Teléfono, Obra social.
- Cada fila se valida con las mismas reglas que el alta manual antes de enviarse; las filas inválidas
  se listan con su motivo y NO se importan — las válidas sí, sin que un error en una fila bloquee al
  resto.
- Botón para descargar una plantilla CSV vacía con los headers correctos.

---

## Épica 24 — Deuda técnica y brechas conocidas (no son historias, son follow-ups)

Recopilado de la auditoría 2026-09-02. No son features nuevas — son casos donde el comportamiento
documentado/esperado y el código real difieren, y alguien tiene que decidir qué lado gana.

> **Resueltos (2026-09-02, misma sesión, después de la auditoría)**: los ítems 1-5 de abajo ya
> fueron corregidos y están en `develop`. **Resueltos (2026-09-03, sesión de reglas de
> habilitación)**: los ítems 8-10. Épica 24 completa — no quedan ítems abiertos.

1. ~~**Series playoff Best-of-N no se generan en producción (HU-82/HU-46).**~~ Resuelto:
   `StageService.SeedPlayoffCupsAsync`/`SeedKnockoutStageAsync`/`SeedMultiGroupCrossCupStageAsync`
   crean un `MatchSeries` real por cruce cuando `Stage.BestOf > 1`, y `SeriesInProgressPanel` (tab
   Playoff de División, admin) agrega la acción de UI para cargar el 2º/3er partido de una serie.
2. ~~**Sin avance automático entre rondas de bracket (HU-82).**~~ Resuelto:
   `StageService.TryAdvanceStageWinnerAsync` empuja el ganador de cada slot decidido (serie o
   partido único) a la ronda siguiente del mismo bracket — llamado desde las 3 acciones de carga de
   resultado de `MatchController` y desde el propio seeding (para propagar byes).
3. ~~**`AuditAction.BackupRestore` nunca se loguea (HU-101).**~~ Resuelto:
   `BackupOperationsService.RestoreBackupAsync` ahora loguea vía `IAuditService`.
4. ~~**Mapa de transiciones de estado del frontend desactualizado (HU-35/HU-114).**~~ Resuelto:
   `tournamentStatusTransitions.ts` incluye `Ongoing → RegistrationClosed`.
5. ~~**Validación del wizard permite copa cruzada sin playoff (HU-47).**~~ Resuelto:
   `validateCrossCupStep` rechaza una copa cruzada con `cups.length === 0`.
6. ~~**`TournamentsPage.tsx` (listado plano de torneos) quedó huérfano (HU-29).**~~ Resuelto
   (2026-09-02, commit `7eb4dd0`): se eliminaron las tres rutas huérfanas (`/torneos`,
   `/panel/torneos`, `/panel/divisiones` sin scope) — cada "Volver"/cancelar ahora usa
   `navigate(-1)` en vez de apuntar a una de estas rutas muertas. Esta nota había quedado
   desactualizada en la pasada del 2026-09-03: el archivo ya no existe en el repo, solo el texto no
   se había corregido.
7. ~~`Docs/QA-CHECKLIST-E2E.md`~~ — el checklist se retiró del repo (ya cumplió su función; el E2E
   contra staging que documentaba se completó en la sesión del 2026-09-03, ver ítems 8-10). Si se
   necesita un nuevo barrido E2E, generar uno nuevo desde cero contra las rutas reales, no reflotar
   el archivo viejo.
8. ~~**`LoadMatchResultFromSheetsRequest.MatchId` marcado `required` rechazaba toda carga de
   resultado (HU-69).**~~ Resuelto (2026-09-03): el DTO nunca recibe `matchId` del cliente (viene de
   la ruta), pero `required` hacía que `System.Text.Json` rechazara el body entero antes de que el
   controller pudiera asignarlo desde la ruta — ningún resultado se pudo cargar por este endpoint
   hasta el fix.
9. ~~**El roster de un partido mostraba "No habilitado" para todos, incluso jugadores realmente
   habilitados (HU-62).**~~ Resuelto (2026-09-03): `CreateMap<Player, PublicPlayerResponse>()` no
   tenía mapeo para `MedicalRecordStatus`/`IsHabilitado`/`JerseyNumber` (viven en
   `PlayerTeamRegistration`, no en `Player`), y el include de EF tampoco cargaba esa navegación para
   el roster (sí para los goleadores). El dato real en base siempre fue correcto — solo lo que
   mostraba la respuesta del partido estaba mal. Corregido con un paso `AfterMap` en
   `MatchProfile.cs` + el include faltante en `MatchRepository.GetDetailByIdOrSlugAsync`.
10. ~~**Datos de seed sembrados antes de la feature de ficha médica quedaron con TODOS los jugadores
    en `Pending` (HU-57/HU-118/HU-119).**~~ Resuelto (2026-09-03):
    `MedicalRecordSeedBackfiller.BackfillMedicalRecordsAsync` ahora también toma como candidatos los
    registros `Pending`/`Rejected` cuyo `CreatedBy == AuditConstants.SystemUser` — un valor que solo
    escribe el seeder, nunca una acción real de admin — y los aprueba junto con subirles el archivo.
    Un registro creado por un admin real, sin importar su estado, nunca entra en este camino. Esto
    hace que el self-heal automático de `DataSeeder.SeedAsync` (que ya corre solo con que
    `Seed:Enabled=true`, sin flags extra) cubra tanto una base nunca migrada a la feature de ficha
    médica como una restaurada desde un backup viejo — no hace falta una acción puntual como la que
    se usó para arreglar el dato en vivo esta sesión.

---

## Resumen

- **Test counts**: verificado 2026-09-03: **826 tests backend, 725 tests frontend**, ambos en verde
  (la cifra "808/645" de dos pasadas atrás ya está vieja). Correr
  `dotnet test Club12-Backend/Solution/Club12.sln` y `npx vitest run` (desde `Club12-WebClient`)
  para el número vigente en cualquier momento — no confiar en un número congelado en este documento.
- **Historias obsoletas/reemplazadas**: HU-29 (listado plano de torneos, eliminado — ver Épica 24
  ítem 6), HU-41/HU-42 (superadas por HU-106/107/108, sin cambios respecto a la nota original),
  HU-64 (disparador de fixture mal descripto), HU-97 (eliminada a propósito).
- **Épica 24 (deuda técnica) completa**: los 10 ítems están resueltos, ninguno queda abierto. Los
  últimos tres en cerrarse (2026-09-03): HU-69 (`MatchId` required bloqueaba toda carga de
  resultado), HU-62 (roster mostraba "No habilitado" para todos por un mapeo faltante), y el
  self-heal de datos de seed viejos (ítem 10, guarda por `CreatedBy == SystemUser`).
- **Cambio funcional más grande de esta pasada (2026-09-03)**: las tres reglas de habilitación
  alrededor de la carga de resultados — HU-118 (umbral de 4 habilitados para cargar un resultado
  normal) y HU-119 (mismo umbral para poder iniciar el torneo) — más la corrección del mensaje de
  error de HU-62 (lista agrupada por equipo, con nombres reales) y el bug de visualización del
  roster (ítem 9 de Épica 24).
- **Cambio estructural más grande de la pasada anterior**: la entidad Temporada (Season) por encima
  de Torneo (D7/HU-113), que reordenó la navegación completa del panel admin y el punto de entrada
  del wizard.
