# Club 12 — Historias de Usuario

> Fuente: reunión de revisión Facundo (Ferrero) + Franco (Ruggeri) — recorrido completo de la app,
> más decisiones posteriores del owner.
> Rol de PM: consolidar lo discutido, dejar criterios de aceptación accionables, resolver
> incongruencias y proponer historias que sigan el hilo del equipo.
>
> Esta es una reescritura ordenada por **flujo de construcción** y renumerada de forma secuencial.
> Convenciones: `[BUG]` corrige algo existente · `[NUEVA]` propuesta alineada · `[F2]` Fase 2.
> Prioridad (MoSCoW): **M** Must · **S** Should · **C** Could.

---

## Decisiones transversales (leer antes que nada)

Aplican a TODAS las historias. Si una historia las contradice, ganan estas.

- **D1 — Solo 2 tipos de usuario y 2 cuentas.** Se eliminan `Tournament Manager` y `Team Manager`.
  - `Owner`: cuenta compartida por Club 12 (una sola persona opera todo). Gestiona el negocio y ve
    estadísticas globales.
  - `Admin IT`: cuenta compartida por los desarrolladores. Todo lo del owner + sección de datos de
    prueba y tareas técnicas.
  - No hay auto-registro. Solo existen esas 2 cuentas.
- **D2 — Equipos y jugadores están scopeados a la temporada/torneo.** "Colón SF" del Apertura 2026
  NO es el mismo del Clausura 2027: distinto plantel, staff y estadísticas. Cada temporada se crea el
  equipo con su plantel. Es lo que mantiene el historial. Todo equipo/jugador cuelga de un torneo.
- **D3 — Contenido institucional bloqueado para el owner.** Reglamento, "Quiénes somos" y la
  plantilla de ficha médica no los edita el owner desde la app; se cambian contactando a IT.
- **D4 — Reglamento agnóstico a la app y a la temporada.** No menciona premios de una temporada
  puntual.
- **D5 — Una acción = una transacción.** Operaciones compuestas (crear torneo, respaldo,
  restauración) se resuelven atómicamente, sin cascada de alertas.
- **D6 — Magic link / self-service de password / auto-inscripción son Fase 2.** Con 2 cuentas hoy no
  se necesitan; quedan documentadas.

### Reglas de negocio resueltas (antes eran incongruencias)

- **R1 — Sanción en fechas, cleanup por días.** La sanción se **expresa y se cumple en fechas
  (jornadas)**: "2 de sanción" = el jugador se pierde las próximas 2 jornadas de su equipo. El
  barrido automático de vencimiento se mantiene por **días de calendario** como mecanismo técnico de
  limpieza (no es la fuente de verdad de "cuántas fechas faltan"). Ver HU-75.
- **R2 — Todo scopeado a la temporada, se arregla ya.** Se deja de reasignar `Team.TournamentId`; la
  participación equipo–torneo pasa a ser una inscripción por temporada (espejo de
  `PlayerTeamRegistration`). Es Must. Ver HU-98.
- **R3 — Fixture se genera al iniciar el torneo, tras asignar equipos.** *(Actualizada por HU-108.)*
  El disparador **ya no** es "Inscripción cerrada" sino la transición **"En curso"** (posterior a la
  asignación de inscriptos a divisiones), porque las zonas se pueblan recién en la asignación. Ver
  HU-108 / HU-64.
- **R4 — Básquet sin empates.** Todo partido tiene ganador. En fase de grupos el desempate se
  resuelve por tabla (HU-80); en playoff hay prórroga hasta que haya ganador (HU-82). Ver HU-70.

---

## Épica 1 — Autenticación y acceso

### HU-01 · Login oculto por URL — `M`
**Como** equipo Club 12 **quiero** un login accesible solo yendo a `/login` **para** que la gente de
afuera no sepa que existe el panel.
- No hay enlace visible al login en la navegación pública.
- Se ingresa tipeando `/login`.

### HU-02 · Ocultar header y footer en el login — `S`
**Como** usuario que entra al login **quiero** una pantalla sin header ni footer **para** un acceso
limpio y desacoplado del sitio público.
- En `/login` no se renderiza header ni footer.

### HU-03 · [BUG] Redirigir al home al cerrar sesión — `M`
**Como** usuario autenticado **quiero** que al cerrar sesión me lleve al inicio **para** no quedar en
el panel sin sesión.
- El logout ejecuta un redirect al home. Hoy se queda en el panel sin redirigir.

### HU-04 · [BUG] Página 404 sin layout — `S`
**Como** usuario que entra a una URL inexistente **quiero** una 404 sin header ni footer que me
explique y me devuelva al inicio **para** entender que la página no existe o se movió.
- La 404 no muestra header ni footer; mensaje "la página no existe o se movió"; botón al inicio.

---

## Épica 2 — Roles y usuarios

### HU-05 · Simplificar el modelo de roles a Owner y Admin IT — `M`
**Como** dueño del producto **quiero** eliminar los roles de responsable de torneo y de equipo
**para** operar con el modelo de 2 cuentas (D1).
- Se quitan `Tournament Manager` y `Team Manager` del sistema y de la UI (formularios, tablas,
  asignaciones "de qué torneo/equipo").
- Quedan `Owner` y `Admin IT`. El owner ve estadísticas globales; Admin IT ve además datos de prueba.
- El owner puede crear/editar datos de blogs, equipos y torneos.

### HU-06 · Editar mi perfil y contraseña — `M`
**Como** usuario autenticado **quiero** editar mi perfil y cambiar mi contraseña desde configuración
**para** mantener mis datos.
- Tab de configuración con edición de perfil y cambio de contraseña.
- El cambio respeta los requisitos mínimos de Identity.

### HU-07 · Listado de usuarios con soft delete — `S`
**Como** owner/admin **quiero** ver los usuarios y poder desactivarlos (no borrarlos) **para**
conservar integridad histórica.
- Tabla de usuarios con acciones: ver, editar, desactivar. "Desactivar" hace soft delete.

### HU-08 · Blanquear contraseña / editar usuario a pedido — `S`
**Como** owner/admin **quiero** blanquear contraseña o editar info de un usuario a pedido **para**
dar soporte cuando alguien pierde acceso.
- Desde el detalle del usuario se edita info y se blanquea contraseña (con requisitos de Identity).

### HU-09 · [F2] Alta de usuario por invitación con magic link — `C`
**Como** owner/admin **quiero** crear un usuario cargando solo su email y que reciba un magic link
**para** que active su cuenta desde su correo.
- Al crear no se define contraseña; se envía link de activación. Diferida (hoy 2 cuentas).

### HU-10 · [F2] Reset de contraseña self-service por magic link — `C`
**Como** usuario **quiero** recibir un magic link para restablecer mi contraseña **para** recuperarme
sin soporte.
- El blanqueo dispara un magic link al mail para que el propio usuario la cambie. Diferida.

---

## Épica 3 — Portada pública, noticias y vista pública del torneo

### HU-11 · Landing pública con accesos directos — `M`
**Como** visitante anónimo **quiero** una portada con torneos, sanciones, torneos destacados y
noticias **para** llegar rápido a lo que me interesa sin loguearme.
- Secciones: accesos directos (torneos, sanciones), torneos destacados y noticias.
- Torneos destacados: los últimos primero, se estén jugando o no. Resumen repetido más abajo.
- No requiere autenticación.

### HU-12 · Feed de noticias con "ver todas" — `M`
**Como** visitante **quiero** ver las últimas noticias en la home y un botón "ver todas" **para**
enterarme de novedades y navegar el histórico.
- La home muestra las 3 más recientes; "ver todas" va a un listado paginado.
- Cada noticia: título, fecha de publicación, autor, imagen opcional y descripción.

### HU-13 · Detalle de noticia con URL por slug — `M`
**Como** visitante (o Club 12 compartiendo en redes) **quiero** que cada noticia tenga URL con slug
legible (no el ID) **para** compartirla y traer tráfico.
- URL con slug (ej. `/noticias/apertura-fecha-3`), no UUID.
- Detalle con título, fecha, autor, imagen y descripción completa.

### HU-14 · [NUEVA] Vista pública del torneo (fixture, resultados y posiciones) — `S`
**Como** visitante **quiero** ver el fixture, resultados y posiciones de un torneo sin loguearme
**para** seguir la competencia.
- Página pública por torneo con fixture **agrupado por jornada** (HU-63), resultados y tabla de
  posiciones (HU-78).
- Solo lectura; sin acciones de administración.

### HU-15 · [BUG] Ocultar el ID en la URL cuando debería ir un slug — `S`
**Como** visitante **quiero** que ninguna vista muestre UUIDs en la URL cuando existe un
identificador legible **para** URLs limpias sin exponer IDs internos.
- Auditar vistas públicas (noticias, torneos, equipos, perfil) y usar slug donde exista nombre.
- Perfil de usuario: usar el nombre de usuario como slug en vez del UUID.

### HU-16 · [NUEVA] Estado borrador / publicada de noticia — `C`
**Como** owner/admin **quiero** guardar una noticia como borrador antes de publicarla **para** no
exponer contenido a medio hacer.
- Estado borrador/publicada; solo las publicadas aparecen en home y listado público.
- El orden público sigue siendo por fecha de publicación (HU-12).

### HU-17 · [NUEVA] Compartir noticia con Open Graph — `C`
**Como** Club 12 publicando en redes **quiero** que la URL de noticia genere una tarjeta rica
**para** maximizar el engagement.
- Metadatos Open Graph/Twitter Card por noticia (título, imagen, descripción).

---

## Épica 4 — Contenido institucional

### HU-18 · Ver "Quiénes somos" — `M`
**Como** visitante **quiero** una sección institucional de Club 12 **para** conocer la organización.
- Visible sin login. (D3) No editable por el owner desde la app.

### HU-19 · Ver reglamento — `M`
**Como** visitante **quiero** consultar el reglamento **para** conocer las reglas.
- Agnóstico a la app y a la temporada (D4). (D3) No editable por el owner desde la app.

### HU-20 · Descargar plantilla de ficha médica — `M`
**Como** capitán/organizador **quiero** descargar la plantilla de ficha médica con el paso a paso
**para** completarla e imprimirla para mis jugadores.
- Botón que entrega un PDF (plantilla base). La sección explica los datos necesarios.

### HU-21 · [BUG] Arreglar la descarga de ficha médica en producción — `M`
**Como** usuario **quiero** que la descarga funcione en el entorno publicado **para** obtener la
ficha (hoy anda en local pero falla en el server).
- Reproducir en el entorno desplegado, encontrar la causa (entorno/ruta/permiso) y corregir.
- Verificación en el entorno real, no solo local.

---

## Épica 5 — Sanciones públicas

### HU-22 · Consulta pública de sanciones — `M`
**Como** cualquier persona **quiero** ver las sanciones sin loguearme **para** saber si un jugador
está sancionado antes de un partido sin consultar a nadie.
- Vista pública. Cada sanción: datos del jugador, duración en fechas, cuándo se aplicó y el motivo.

### HU-23 · [BUG] Buscar sanciones por nombre de jugador — `M`
**Como** usuario **quiero** buscar por nombre (ej. "Agustín") **para** encontrar a la persona, no
solo por motivo.
- "Agustín" devuelve las sanciones de jugadores cuyo nombre lo contiene. Hoy busca solo por `motivo`.

### HU-24 · [BUG] Búsqueda por coincidencia parcial (contains) — `M`
**Como** usuario **quiero** coincidencias parciales dentro del texto **para** no depender del inicio
exacto.
- Usa `contains`, no `startsWith`/`endsWith`. Case-insensitive y sin sensibilidad a acentos.

### HU-25 · Filtrar sanciones por torneo — `S`
**Como** usuario **quiero** filtrar por torneo **para** enfocarme (casi siempre se consultan por
torneo).
- Selector de torneo que acota el listado, combinable con la búsqueda por nombre.

---

## Épica 6 — Panel de administración: navegación

### HU-26 · Simplificar la navegación lateral — `M`
**Como** owner/admin **quiero** un sidebar que agrupe la administración por torneo y deje solo lo que
tiene sentido **para** no navegar entidades sueltas.
- Se **quitan** del sidebar **Divisiones**, **Fases** y **Partidos**: se gestionan desde el torneo.
- Se **mantienen** **Sanciones** y **Canchas**. Se **agrega** **Equipos y planteles** (Épica 10).
- Cada tabla tiene acciones: ver, editar, desactivar/eliminar.

### HU-27 · El asistente de torneo no vive en el sidebar — `S`
**Como** owner/admin **quiero** que el asistente solo se acceda desde "Nuevo torneo" **para** que no
sea una entrada de menú.
- Se quita "Asistente de torneo" del sidebar; se llega solo por el botón "Nuevo torneo" (HU-29).
- El asistente tiene botón volver/cancelar para abandonar el flujo.

### HU-28 · [NUEVA] Ajustes de estilo del panel — `C`
**Como** usuario del panel **quiero** scrollbars/sidebar y date pickers consistentes **para** una
experiencia prolija.
- Ajustar estilo del scrollbar del sidebar y de los date pickers.

---

## Épica 7 — Gestión de torneos

### HU-29 · Listado y CRUD de torneos — `M`
**Como** owner/admin **quiero** una tabla de torneos con filtros y acciones **para** administrarlos.
- Tabla con torneos actuales; filtros por nombre o descripción; botón "Nuevo torneo".
- Acciones por fila: ver, editar, eliminar.

### HU-30 · Vista de torneo con sub-tabs — `M`
**Como** owner/admin **quiero** entrar a un torneo y ver sus divisiones y equipos anotados **para**
gestionar todo desde el propio torneo.
- Sub-tab **Divisiones** (con creación directa vinculada al torneo) y **Equipos anotados**.
- Las entidades relacionadas se crean vinculadas al torneo, no en pantallas sueltas.

### HU-31 · Bloquear alta/edición de estructura si el torneo ya empezó — `M`
**Como** owner/admin **quiero** no poder crear divisiones ni agregar equipos con el torneo ya
arrancado **para** proteger su integridad.
- Con el torneo en curso, crear división / agregar equipo están deshabilitados; no se editan
  divisiones.
- El alta/edición de estructura solo se permite en estado **Inscripción abierta** (HU-35/HU-36).

### HU-32 · [BUG] Contador de equipos por división en 0 — `M`
**Como** owner/admin **quiero** que la tabla de divisiones del torneo muestre el conteo real de
equipos **para** no ver "0" cuando sí hay.
- El contador de equipos de la tabla de divisiones interna refleja el valor real.

### HU-33 · [BUG] Botón deshabilitado con "no hay equipos" cuando sí hay — `M`
**Como** owner/admin **quiero** que no diga "no hay equipos" cuando existen **para** poder operar.
- Investigar el estado que deshabilita el botón pese a haber equipos y corregir el origen del dato.

### HU-34 · Quitar mínimo/máximo de equipos del torneo — `S`
**Como** dueño del producto **quiero** eliminar los campos de equipos mínimos y máximos **para**
simplificar (no aportan y el cliente no los usa).
- Se remueven `minTeams`/`maxTeams` del formulario y del asistente. Se conserva **Estado**.

---

## Épica 8 — Máquina de estados del torneo

### HU-35 · Transiciones de estado forward-only — `M`
**Como** owner/admin **quiero** que el estado del torneo solo avance y nunca retroceda **para**
reflejar el ciclo real.

Ciclo: `Programado` (inicial) → `Inscripción abierta` → `Inscripción cerrada` → `En curso` →
`Finalizado`. `Cancelado` accesible desde cualquier estado no terminal.

- `Programado` → `Inscripción abierta` | `Cancelado`.
- `Inscripción abierta` → `Inscripción cerrada` | `Cancelado`.
- `Inscripción cerrada` → `En curso` | `Cancelado`.
- `En curso` → `Finalizado` | `Cancelado`.
- `Finalizado` y `Cancelado` son **terminales**.
- El selector solo muestra las transiciones válidas desde el estado actual.

### HU-36 · "Programado" distinto de "Inscripción abierta" — `S`
**Como** owner/admin **quiero** distinguir "Programado" de "Inscripción abierta" **para** publicar el
torneo antes de abrir inscripciones.
- Solo con **Inscripción abierta** se agregan/editan equipos y divisiones (HU-31).

### HU-37 · "Inscripción cerrada" habilita la asignación (el fixture se genera al iniciar) — `M`
> **Supersedida por HU-108.** El fixture ya **no** se genera al cerrar inscripción, porque los
> equipos se asignan a divisiones *después* del cierre.
**Como** owner/admin **quiero** que "Inscripción cerrada" **habilite la asignación** de equipos a
divisiones **para** armar los grupos con el padrón definitivo antes de generar el fixture.
- La transición a "Inscripción cerrada" **congela el padrón** y habilita el paso de asignación (HU-108).
- La generación del fixture (HU-64) ocurre al pasar a **"En curso"**, tras la asignación.

---

## Épica 9 — Asistente de creación de torneo

### HU-38 · Crear el torneo completo en una sola transacción — `M`
**Como** owner/admin **quiero** que el asistente cree torneo, divisiones y config atómicamente
**para** evitar cascada de alertas y estados a medias (D5).
- Todo el armado (torneo + zonas + copas + reglas) se persiste en una transacción. Si algo falla, no
  queda un torneo parcial.

### HU-39 · Datos base del torneo con fechas — `M`
**Como** owner/admin **quiero** cargar nombre, descripción y fechas (inicio y límite de inscripción)
**para** configurar el torneo.
- Fecha de inicio y fecha límite de inscripción. Sin mínimo/máximo de equipos (HU-34).

### HU-40 · [NUEVA] Validar que el límite de inscripción sea anterior al inicio — `S`
**Como** owner/admin **quiero** que el sistema valide límite de inscripción < inicio **para** evitar
configuraciones imposibles.
- El límite de inscripción debe ser anterior al inicio (ej. una semana antes); mensaje claro si no.

### HU-41 · Selección de equipos mejorada y scopeada a la temporada — `M`
> **Supersedida por HU-106/107.** El asistente ya **no** selecciona equipos; la inscripción de
> equipos se hace con el torneo abierto (HU-107) y la asignación a divisiones tras el cierre (HU-108).
**Como** owner/admin **quiero** una pantalla de selección clara con los equipos de la temporada
**para** armar el torneo sin confusión.
- (D2) Los equipos se listan por temporada/torneo. UX rediseñada: buscar, ver seleccionados, quitar.

### HU-42 · [BUG] Un equipo asignado a una zona no aparece en otra — `M`
> **Trasladada a HU-108.** La unicidad de zona ahora se valida en el **paso de asignación** (tras el
> cierre de inscripción), no en el asistente.
**Como** owner/admin **quiero** que al asignar un equipo a la Zona A deje de aparecer en la Zona B
**para** no poder ponerlo en dos zonas.
- Asignado a Zona A, no aparece disponible en el selector de Zona B (se oculta, no solo un aviso).

### HU-43 · Configurar la fase de grupo (cantidad de enfrentamientos) — `M`
**Como** owner/admin **quiero** definir si en la fase de grupo se juega una vez, dos o N veces
**para** modelar "todos contra todos" e "ida y vuelta".
- Por división se configura cuántas veces se enfrenta cada par de equipos (ej. Copa Club 12 = 2).

### HU-44 · Configurar playoffs: Copa de Oro / Copa de Plata — `M`
**Como** owner/admin **quiero** armar playoffs con Copa de Oro y (opcional) Copa de Plata **para**
reflejar el formato del club.
- Por división puede haber Oro y Plata; se puede configurar más de una copa.
- Ejemplos: 4 equipos → Oro 1°v2°, Plata 3°v4°. 8 equipos → Oro primeros 4 (semi+final), Plata 5°-8°.

### HU-45 · Mapear posiciones de cada división a cada playoff por rangos — `M`
**Como** owner/admin **quiero** definir, por división, qué **rangos de posición** van a cada playoff
(oro/plata) o no clasifican **para** que el sistema arme los playoffs automáticamente.
- Con **grupos + playoff de más de una copa**, se define por división: rango de posición → destino.
- **Ejemplo (División A, 10 equipos):** `1–4 → Oro`, `5–8 → Plata`, `9–10 → sin playoff`.
- Los rangos no se solapan ni dejan huecos ambiguos; cada posición va a un solo destino (o ninguno).
- Los destinos salen de las copas configuradas (HU-44). Con varias divisiones, cada copa se puebla
  juntando los clasificados de las divisiones que aporten (siembra según HU-82).

### HU-46 · Configurar la serie final al mejor de N — `M`
**Como** owner/admin **quiero** definir la final como "al mejor de 3" (u otra) **para** respetar el
formato de definición.
- La ronda final se configura como serie al mejor de N (por defecto, mejor de 3).

### HU-47 · Copa / división cruzada con playoff obligatorio — `M`
**Como** owner/admin **quiero** armar la copa cruzada (todos los equipos) con grupos **+ playoff sí o
sí** **para** modelar la "Copa Club 12", que no puede quedar solo en grupos.
- La división cruzada **siempre** tiene playoff; no se puede guardar solo con grupos.
- El usuario define la cantidad de **grupos** y el reparto de equipos (grupos de tamaño distinto
  permitidos). **Ejemplo:** 42 equipos → 10 grupos.
- Define **cuántos clasifican por grupo** al playoff. El playoff soporta **BYE** cuando los
  clasificados no son potencia de 2 (los mejores avanzan sin jugar, HU-82).
- Fase de grupo configurable (HU-43). La división femenina va aparte (HU-48).

### HU-48 · Femenino como torneo separado, por diseño — `M`
**Como** owner/admin **quiero** que el sistema impida mezclar la división femenina con las masculinas
en el mismo torneo **para** respetar la regla del club (no se cruzan).
- Validación que evita incluir la femenina en el mismo torneo que las masculinas; va en torneo aparte.

### HU-49 · Paso de revisión antes de crear — `S`
> **Ajustada por HU-106.** El resumen ya **no** muestra equipos por zona (no se seleccionan en el
> asistente); muestra la **estructura**: divisiones, formato de grupo, copas y series.
**Como** owner/admin **quiero** un resumen final antes de confirmar **para** validar la estructura y
el formato.
- Muestra divisiones/zonas, formato de grupo, copas y series. Confirmar dispara la creación en una
  transacción (HU-38).

---

## Épica 10 — Equipos y planteles

### HU-50 · Sección de Equipos en el panel — `M`
**Como** owner/admin **quiero** una sección para crear y administrar equipos **para** cargar los
planteles tras la inscripción por WhatsApp.
- Entrada "Equipos" en el sidebar (HU-26); alta/edición/baja. (D2) Asociados a la temporada/torneo.

### HU-51 · Tab de jugadores dentro del equipo — `M`
**Como** owner/admin **quiero** un tab de plantel dentro del equipo **para** cargar todos sus
jugadores.
- Al ver un equipo aparece el tab "Jugadores/Plantel"; se agregan/editan jugadores.
- (D2) El jugador pertenece a ese equipo **en esa temporada**; otra temporada/equipo es otro registro.

### HU-52 · Inscripción manual por el administrador — `S`
**Como** owner/admin **quiero** cargar a mano equipos y planteles **para** reflejar inscripciones
acordadas por WhatsApp.
- No hay formulario público (F2). El admin crea equipo, plantel y lo asigna a la división correcta.

### HU-53 · [NUEVA] Copiar plantel de una temporada anterior — `C`
**Como** owner/admin **quiero** clonar un equipo/plantel de una temporada previa como base editable
**para** no recargar todo cada año.
- Al crear el equipo de la nueva temporada, ofrecer "importar desde temporada X".
- El resultado es un registro nuevo (D2); la ficha médica NO se hereda (HU-59).

### HU-54 · [NUEVA] Límites de plantel y dorsales únicos — `C`
**Como** owner/admin **quiero** validar tamaño de plantel y unicidad de dorsal **para** cumplir el
reglamento y evitar datos inconsistentes.
- Tope de jugadores por plantel configurable. Dorsal único dentro del equipo/temporada.
- Un jugador no puede estar en dos equipos del mismo torneo.

---

## Épica 11 — Ficha médica y elegibilidad

**Flujo E2E de validación de un jugador (no hay auto-servicio; todo pasa por la cuenta owner):**
1. El capitán/jugador entra a la página pública y **descarga la plantilla** de ficha médica (HU-20).
2. La completa para **todos los jugadores** de su equipo (impresa/PDF) y se la envía al owner por
   WhatsApp u otro medio.
3. El owner **se loguea** con la cuenta Club 12 (HU-01) y, dentro de ese **torneo → equipo →
   jugador**, **carga la ficha médica** de cada jugador (HU-55).
4. El owner **aprueba o rechaza** cada ficha (HU-58). Recién aprobada, el jugador queda **habilitado
   para ese equipo en ese torneo** (HU-57).
5. En la carga del partido, solo los jugadores habilitados y sin sanción son **elegibles** para la
   planilla (HU-60/HU-61).
> Clave de coherencia (D2/HU-98): la ficha y la habilitación son por **jugador + equipo + torneo**
> (esa inscripción de temporada), no globales. La próxima temporada arranca sin ficha (HU-59).

### HU-55 · Cargar ficha médica a un jugador para ese torneo y equipo — `M`
**Como** owner/admin **quiero** subir el archivo de ficha médica a un jugador **dentro de un torneo y
equipo específicos** **para** documentar su aptitud en esa temporada.
- Se accede al jugador por el camino **torneo → equipo → plantel → jugador** y se sube su ficha (PDF).
- La ficha queda asociada a esa **inscripción** (jugador + equipo + torneo), no al jugador de forma
  global (D2/HU-98). El mismo jugador en otro equipo/torneo tiene su propia ficha.
- Flujo real: el capitán completa la plantilla y la envía por WhatsApp; el owner la sube desde la
  cuenta Club 12 (ver Flujo E2E arriba).

### HU-56 · [NUEVA] Bucket de almacenamiento de archivos — `M`
**Como** equipo IT **quiero** preparar el bucket S3 de Supabase para recibir archivos **para** poder
guardar las fichas (hoy no está habilitado).
- Configurar el bucket para aceptar PDF, con límites de tamaño/tipo y control de acceso.

### HU-57 · Habilitación del jugador (por equipo y torneo) según ficha aprobada — `M`
**Como** owner/admin **quiero** que el jugador tenga estado habilitado/no habilitado **en ese equipo
y torneo** **para** saber si puede jugar esa temporada.
- El estado depende de: ficha **cargada y aprobada** (HU-55/HU-58) para esa inscripción. No alcanza
  con subirla.
- La habilitación es **por jugador + equipo + torneo** (D2/HU-98): estar habilitado en un torneo no
  lo habilita en otro. Se refleja en el plantel de ese torneo.

### HU-58 · Aprobar / rechazar ficha médica — `S`
**Como** owner/admin **quiero** marcar la ficha como aprobada o rechazada (con motivo) **para**
controlar la habilitación antes de jugar.
- Al aprobar, el jugador pasa a habilitado (HU-57); al rechazar, queda no habilitado con motivo.

### HU-59 · Ficha médica nueva por temporada — `S`
**Como** owner/admin **quiero** que la ficha sea por temporada **para** no reutilizar la anterior (no
es válida entre temporadas).
- Cada temporada requiere ficha nueva; no se hereda la previa.

### HU-60 · Solo son elegibles los jugadores habilitados — `M`
**Como** owner/admin **quiero** que solo los jugadores con ficha aprobada cuenten como elegibles
**para** cumplir el requisito de aptitud.
- Un jugador sin ficha aprobada (HU-57) no es elegible para la planilla del partido (HU-71).

### HU-61 · Un jugador sancionado no es convocable — `M`
**Como** owner/admin **quiero** que un jugador con sanción activa figure como no disponible **para**
no alinearlo indebidamente.
- Con sanción activa (HU-76) en el torneo, se marca no disponible para esas jornadas.

### HU-62 · [NUEVA] Aviso de jugador no habilitado al armar la fecha — `C`
**Como** owner/admin **quiero** que se advierta si un jugador no habilitado figura activo **para**
evitar que juegue sin ficha aprobada o con sanción.
- Señal visual cuando un jugador sin ficha aprobada (HU-57) o sancionado (HU-61) aparece en un
  plantel activo.

---

## Épica 12 — Fixture y jornadas

### HU-63 · Partidos gestionados desde el torneo y agrupados por jornada — `M`
**Como** owner/admin (y visitante) **quiero** administrar/ver los partidos de una división
subdivididos por **jornada** **para** leer el fixture como "Fecha 1, Fecha 2, …".
- Los partidos se gestionan a través del torneo (se quita la pantalla suelta, HU-26).
- Se agrupan por **jornada (fecha del torneo)**:

  ```
  Fecha 1
    Partido 1
    Partido 2
    ...
  Fecha 2
    Partido 1
    ...
  ```

- **No** se agrupa por fecha física de calendario (ej. `28-04-2026`); la fecha/hora se muestra
  dentro de la tarjeta del partido, pero el agrupador es siempre la jornada.
- Aplica en el panel y en la vista pública (HU-14).

### HU-64 · Generar el fixture automático y aleatorio al cerrar inscripción — `M`
**Como** owner/admin **quiero** que al pasar a "Inscripción cerrada" se genere el fixture completo de
forma aleatoria **para** no armarlo a mano (no existe armado manual).
- Disparador único: la transición a "Inscripción cerrada" (HU-37) genera el fixture de todas las
  divisiones.
- Emparejamiento **aleatorio**; el usuario no arma el fixture ni elige los cruces.
- Respeta la cantidad de enfrentamientos configurada por división (HU-43).
- Idempotente por división: no se regenera sobre una división con resultados cargados.

### HU-65 · Cantidad de fechas y calendario del fixture — `M`
**Como** owner/admin **quiero** que la cantidad de **fechas (jornadas)** salga de los equipos y los
enfrentamientos entre sí, asignadas a domingos por defecto **para** un calendario coherente.
- La cantidad de fechas se **deriva** del round-robin: nº de equipos × cantidad de enfrentamientos.
  - **Ejemplo:** 10 equipos, 2 veces cada uno ⇒ **18 fechas**.
  - Con nº impar de equipos, cada fecha un equipo queda **libre**; el sistema lo contempla.
- Cada jornada agrupa los partidos que se juegan en ella; todos los equipos con partido juegan.
- Por defecto se asignan a **todos los domingos** consecutivos desde el inicio. Sin canchas (HU-66).

### HU-66 · Asignar cancha al partido más tarde — `M`
**Como** owner/admin **quiero** asignar la cancha a cada partido después de generar el fixture
**para** reflejar que la cancha recién se sabe cerca de la fecha (a veces un día antes).
- El fixture no fija canchas al crearse; cada partido permite asignar/editar su cancha luego (HU-89).

### HU-67 · Orden de jornadas fijo; solo se edita la fecha de un partido — `M`
**Como** owner/admin **quiero** que el orden de las jornadas no se reordene, pero sí poder cambiar la
**fecha de calendario** de un partido puntual **para** ajustar reprogramaciones sin alterar la
estructura.
- No se reordena el fixture ni se mueve un partido de jornada: Fecha 1 es Fecha 1, etc.
- Sí se edita la fecha/hora de calendario de un partido individual (HU-68), sin cambiar su jornada.
- El domingo por defecto de cada jornada se puede cambiar luego, pero la secuencia se mantiene.

### HU-68 · [NUEVA] Reprogramar / suspender un partido — `C`
**Como** owner/admin **quiero** cambiar la fecha o suspender un partido **para** manejar imprevistos
(clima, cancha).
- Editar fecha/hora del partido y marcarlo suspendido/reprogramado sin romper el resto del fixture
  (respeta HU-67: no cambia de jornada).

---

## Épica 13 — Carga del partido: resultado y goleadores

> Convierte el sistema de "calendario" en "gestor de torneo": marcador, quién anotó y cuánto, y su
> impacto en posiciones (Épica 15) y rankings (Épica 16).

### HU-69 · Cargar el resultado del partido — `M`
**Como** owner/admin **quiero** cargar el marcador final **para** cerrar la jornada y actualizar la
tabla.
- Marcador de local y visitante y estado (jugado / pendiente / suspendido / W.O.).
- Solo se carga sobre un partido cuyo fixture ya existe (HU-64).
- Al guardar "jugado" se recalculan posiciones (Épica 15) y estadísticas (Épica 16).

### HU-70 · Sin empates: todo partido cargado tiene ganador — `M`
**Como** owner/admin **quiero** que el sistema exija un ganador **para** respetar que en básquet no
hay empates (R4).
- En fase de grupos no se permite marcador empatado en un partido "jugado".
- En playoff, un empate va a prórroga hasta que haya ganador (HU-82). Las posiciones no tienen
  columna "empatados".

### HU-71 · Cargar goleadores del partido (puntos por jugador) — `M`
**Como** owner/admin **quiero** cargar cuántos puntos anotó cada jugador de cada equipo **para**
alimentar el ranking de goleadores y la estadística individual.
- Por equipo se muestra su plantel de esa temporada (HU-98) y se ingresan los puntos por jugador.
- Solo se cargan anotadores del plantel de ese equipo en ese torneo y **elegibles** (ficha aprobada
  HU-60, sin sanción activa HU-61).
- **Coherencia:** la suma de puntos de los jugadores de un equipo debe ser **igual** a su marcador
  (HU-69); si no coincide, no se guarda y se avisa la diferencia.
- Se puede poner 0; no es obligatorio listar a todos, pero la suma debe cerrar.
- Opcional: registrar faltas por jugador. Editable: corregir recalcula rankings y estadísticas.

### HU-72 · [NUEVA] Persistir anotadores y conectarlos al ranking — `M`
**Como** equipo IT **quiero** que la carga de goleadores persista y alimente la tabla de anotadores
**para** cerrar el hueco actual (hay lectura de ranking pero sin escritura conectada).
- La carga (HU-71) escribe la estadística del jugador por partido; el ranking (HU-86) lee de esa
  misma fuente. No queda tabla de scorer "huérfana" sin escritura.

### HU-73 · [NUEVA] Walkover / ausencia (W.O.) — `S`
**Como** owner/admin **quiero** marcar un partido como W.O. cuando un equipo no se presenta **para**
aplicar el resultado reglamentario sin inventar marcador.
- Al marcar W.O. se asigna el resultado por defecto del reglamento al equipo presente.
- Impacta posiciones y estadísticas; se distingue visualmente de un partido normal.

---

## Épica 14 — Gestión de sanciones (administración)

### HU-74 · Crear una sanción a un jugador desde el partido — `M`
**Como** owner/admin **quiero** cargar una sanción, idealmente desde el partido donde ocurrió
**para** registrar la falta con su contexto (torneo, fecha, rival).
- Alta con jugador, motivo, duración (en fechas, HU-75), fecha de aplicación y torneo.
- Se puede iniciar desde el detalle del partido (precarga jugador + torneo + fecha).
- Queda visible en la consulta pública (HU-22).

### HU-75 · Duración de la sanción en fechas, cleanup por días — `M`
**Como** owner/admin **quiero** que la sanción se exprese y cumpla en **fechas (jornadas)** con el
barrido de vencimiento corriendo por días **para** que "2 de sanción" = 2 jornadas, conservando el
mecanismo técnico de limpieza (R1).
- La duración se carga y se muestra en **fechas**. Un jugador con N fechas queda no disponible
  (HU-61) las próximas N jornadas de su equipo en ese torneo; se descuenta al disputarse.
- El barrido automático de vencidas corre por **días de calendario**
  (`GetExpiredSanctionsAsync`, `IssuedDate.AddDays`) como limpieza técnica, no como fuente de verdad
  de "fechas restantes".
- La UI nunca mezcla unidades. Si una jornada se reprograma/suspende (HU-68/HU-73), el conteo sigue
  la jornada disputada, no el calendario.

### HU-76 · Vencimiento automático de sanciones — `S`
**Como** owner/admin **quiero** que las sanciones venzan solas al cumplirse su duración **para** no
habilitar/inhabilitar a mano.
- Al cumplirse (según HU-75) la sanción deja de estar activa; mientras esté activa, el jugador figura
  no disponible (HU-61).

### HU-77 · [NUEVA] Sanción también a equipo o staff — `C`
**Como** owner/admin **quiero** sancionar a un equipo o a un miembro del staff, no solo a un jugador
**para** cubrir faltas institucionales.
- El sujeto de la sanción puede ser jugador, equipo o integrante del staff; la consulta pública lo
  distingue.

---

## Épica 15 — Clasificación y playoffs

### HU-78 · Tabla de posiciones por zona/división — `M`
**Como** usuario **quiero** ver la tabla de posiciones de cada zona **para** conocer la clasificación
en tiempo real.
- Columnas: PJ, PG, PP, puntos a favor/en contra, diferencia, puntaje.
- Se recalcula automáticamente al cargar resultados (HU-69).

### HU-79 · Sistema de puntaje configurable — `S`
**Como** owner/admin **quiero** definir cuántos puntos otorga ganar y perder **para** adaptarme al
reglamento (ej. FIBA: 2 por ganado, 1 por perdido).
- Puntaje por victoria y derrota configurable por torneo/división; por defecto 2/1. Sin puntaje por
  empate (HU-70).

### HU-80 · Desempate de la tabla (fase de grupos) — `M`
**Como** owner/admin **quiero** un orden de desempate fijo y explícito **para** ordenar equipos
igualados sin ambigüedad.

Orden (mayor a menor prioridad), **solo en fase de grupos**:
1. **PTS** — puntos en la tabla.
2. **PG** — partidos ganados.
3. **DG** — diferencia de puntos (a favor − en contra) en toda la zona.
4. **H2H** — resultado directo entre los equipos empatados.
5. **DG en H2H** — diferencia considerando solo los partidos entre los empatados, cuando entre ellos
   jugaron **más de 1 partido**.

- Se resuelve escalonadamente: se pasa al criterio siguiente solo si el anterior sigue empatado.
- H2H se calcula solo entre los empatados; si son 3+, se arma la mini-tabla entre ellos.
- El criterio que resolvió cada posición debe poder mostrarse. No aplica a playoff (HU-82).

### HU-81 · Definir los clasificados a playoffs desde la tabla — `M`
**Como** owner/admin **quiero** que los clasificados a cada copa salgan de la tabla final de cada
zona **para** sembrar los playoffs correctamente.
- Cerrada la fase de grupos, el sistema toma los clasificados según el mapeo por rangos (HU-45).
- Cada copa (Oro/Plata) se puebla con los puestos definidos.

### HU-82 · Bracket de playoffs con siembra, prórroga y BYE — `M`
**Como** usuario **quiero** ver el cuadro de playoffs con los cruces sembrados **para** seguir la
definición.
- Cruces por **siembra** (mejor sembrado vs peor, etc.).
- **Prórroga**: sin empates; un partido igualado va a tiempo extra hasta que haya ganador.
- **Serie al mejor de N** (HU-46): se cierra cuando un equipo alcanza la mayoría (mejor de 3 ⇒
  primero a 2).
- **BYE** cuando los clasificados no son potencia de 2: los mejores avanzan sin jugar hasta emparejar
  el cuadro.
- El ganador de cada serie avanza automáticamente.

---

## Épica 16 — Estadísticas y rankings

### HU-83 · Estadísticas globales del sistema — `M`
**Como** owner **quiero** un panel de estadísticas generales de todo el sistema **para** una visión
global.
- Conteos: torneos por estado, partidos jugados y programados, equipos, partidos, sanciones, etc.
  Alcance: todo el sistema.

### HU-84 · Estadísticas por torneo autogeneradas — `M`
**Como** usuario **quiero** ver estadísticas de cada torneo que se actualizan solas **para** seguir
el rendimiento sin carga manual.
- Se generan automáticamente a medida que se cargan datos de partidos. Visibles en la vista de
  torneo y como tab de estadísticas dentro del torneo.

### HU-85 · [NUEVA] Estadísticas por temporada y de todos los tiempos — `S`
**Como** usuario **quiero** separar estadísticas por torneo, por temporada y globales de todas las
temporadas **para** ver el mejor de 2026 o el mejor de la historia.
- Vistas: por torneo, por temporada (ej. 2026) y acumulado histórico.
- (D2) El acumulado histórico agrega los registros de la misma persona/entidad entre temporadas.

### HU-86 · Ranking de goleadores — `S`
**Como** usuario **quiero** ver el ranking de máximos anotadores **para** seguir a los destacados.
- Ranking por torneo, por temporada y de todos los tiempos (HU-85, D2).
- Se alimenta de la carga de goleadores (HU-71/HU-72), sin carga manual aparte.
- Muestra jugador, equipo (en esa temporada), total de puntos y partidos jugados; opcional promedio.
- Desempate: mayor total; luego mayor promedio; luego menos partidos jugados.

### HU-87 · Ficha de estadística individual del jugador — `C`
**Como** usuario **quiero** ver la estadística de un jugador (total y promedio de puntos, partidos)
**para** conocer su rendimiento.
- Métricas por temporada (cada temporada es un registro distinto, HU-98). Enlaza con su historial
  (HU-88) y sus sanciones (Épica 14).

### HU-88 · [NUEVA] Historial de un jugador entre temporadas — `C`
**Como** usuario **quiero** ver la trayectoria de un jugador a través de temporadas y equipos
**para** aprovechar el historial que habilita el modelo season-scoped.
- Enlaza los registros del mismo jugador en distintas temporadas (D2): equipo, estadísticas y
  sanciones por temporada.

### HU-89 · [NUEVA] Exportar posiciones / estadísticas — `C`
**Como** owner/admin **quiero** exportar tablas y estadísticas (CSV/PDF) **para** compartirlas fuera
de la app.
- Exportación de tabla de posiciones, goleadores y fixture por torneo.

---

## Épica 17 — Canchas

### HU-90 · CRUD de canchas — `M`
**Como** owner/admin **quiero** administrar canchas con sus datos **para** asignarlas a los partidos.
- Alta/edición/baja como ítem del sidebar (HU-26). Campos: nombre, dirección/ubicación e imagen.

---

## Épica 18 — Respaldos y administración de datos

### HU-91 · Generar respaldo manual — `M`
**Como** owner/admin **quiero** generar un respaldo de la base **para** tener copias de seguridad.
- Acción de "generar respaldo"; tarda unos segundos. Existen distintos tipos de respaldo.

### HU-92 · Bloquear la app durante respaldo/restauración — `M`
**Como** owner/admin **quiero** que la app quede bloqueada mientras se genera/restaura **para** que
nadie modifique datos a mitad de camino y rompa la operación.
- Durante la operación, cualquier vista muestra un cartel de operación en curso y no permite ver ni
  editar.
- Limitación conocida: si el usuario cierra el navegador durante un respaldo manual, se pierde.

### HU-93 · Respaldo de seguridad automático antes de restaurar — `M`
**Como** owner/admin **quiero** que al restaurar se cree primero una salvaguarda **para** recuperar
si la restauración falla.
- Antes de restaurar se genera un respaldo de seguridad; si falla, actúa como salvaguarda.
- Al finalizar con éxito, el sistema elimina la salvaguarda y la copia de restauración usada.

### HU-94 · Límite de copias de seguridad — `S`
**Como** owner/admin **quiero** un tope de copias (hoy 5) **para** controlar el almacenamiento.
- Máximo de respaldos configurado en 5 por defecto; se puede aumentar a pedido.

### HU-95 · Respaldos programados — `S`
**Como** owner/admin **quiero** respaldos automáticos programados (semanales) **para** no depender
del manual.
- Job programado que genera un respaldo una vez por semana.

### HU-96 · Borrado total de datos — `S`
**Como** owner/admin **quiero** poder borrar todos los datos **para** limpiar la base cuando
corresponda.
- Disponible para owner y admin; requiere confirmación fuerte por el impacto.

### HU-97 · Carga de datos de prueba (solo Admin IT) — `S`
**Como** admin IT **quiero** cargar datos dummy para testing **para** validar el sistema.
- Solo visible para Admin IT. Carga datos dummy **solo con la base vacía**; si ya hay datos, no
  permite generarlos.

---

## Épica 19 — Modelo season-scoped (fundacional)

### HU-98 · Equipos y jugadores scopeados por temporada/torneo — `M`
**Como** dueño del producto **quiero** que cada temporada/torneo sea un scope independiente para
equipos **y** jugadores **para** conservar el historial (Colón SF 2026 ≠ Colón SF 2027) sin que un
plantel viejo se filtre a una temporada nueva (R2).
- La participación equipo–torneo se modela como **inscripción por temporada** (patrón
  `PlayerTeamRegistration`), no como `Team.TournamentId` reasignable. Se elimina la reasignación del
  FK único.
- El plantel pertenece a esa inscripción (equipo + torneo/temporada); el mismo jugador en otra
  temporada/equipo es un registro distinto.
- Consultar un equipo/jugador en una temporada pasada devuelve su plantel, staff, estadísticas y
  sanciones intactos.
- Migración de datos contemplada. Es **Must**: bloquea estadística histórica, goleadores y sanciones
  coherentes.

### HU-99 · [NUEVA] Identidad de club estable entre temporadas — `C`
**Como** usuario **quiero** que "Colón SF" sea reconocible como el mismo club entre temporadas aunque
cada una sea un registro distinto **para** ver su trayectoria.
- Concepto de "club" (identidad estable) del que cuelgan las inscripciones por temporada; habilita
  reportes históricos por club sin romper D2.

---

## Épica 20 — Coherencia técnica transversal

### HU-100 · [NUEVA] Zona horaria Argentina en fechas y horarios — `S`
**Como** usuario **quiero** que fechas y horarios se muestren en horario de Argentina **para** que el
fixture y las noticias no confundan por desfase.
- Almacenamiento en UTC, presentación en `America/Argentina/Buenos_Aires`.

### HU-101 · [NUEVA] Auditoría de acciones sensibles — `C`
**Como** admin IT **quiero** un registro de quién borró datos, restauró respaldos o cambió estados
**para** trazabilidad (aunque haya 2 cuentas compartidas).
- Log de: borrado total, restauración, cambios de estado de torneo, blanqueo de contraseñas.

---

## Épica 21 — Rediseño visual (identidad deportiva moderna: dark + naranja)

Dirección de arte pedida por el owner: **estética deportiva moderna, tema oscuro con acentos
naranja**. Aplica a todo (sitio público + panel). Al implementar, usar el skill `frontend-design`
para calibrar la dirección visual; no hardcodear colores sueltos, todo pasa por el design system.

### HU-102 · Design system: tema oscuro con acentos naranja — `M`
**Como** dueño del producto **quiero** un design system centralizado (tema MUI) con base oscura y
naranja de marca **para** una identidad deportiva moderna y consistente en toda la app.
- Tokens de color: fondos/superficies oscuras en capas, **naranja** para highlights, CTAs, estados
  activos, foco y datos destacados; semánticos para éxito/alerta/error.
- Tipografía deportiva (aprovechar Oswald ya presente en dependencias) con jerarquía clara.
- Un único `theme` MUI como fuente de verdad; nada de estilos ad-hoc por pantalla.
- Contraste accesible (WCAG AA) sobre fondo oscuro; estados hover/focus/disabled definidos.

### HU-103 · Aplicar el tema al sitio público — `S`
**Como** visitante **quiero** un sitio público moderno y deportivo (dark + naranja) **para** una
experiencia atractiva.
- Landing (HU-11), noticias (HU-12/13), vista pública del torneo (HU-14), sanciones (HU-22):
  hero, cards, tablas de posiciones/fixture con jerarquía visual y acentos naranja.
- Responsive mobile-first.

### HU-104 · Aplicar el tema al panel de administración — `S`
**Como** owner/admin **quiero** el panel con el mismo tema oscuro/naranja **para** consistencia.
- Sidebar, tablas (DataGrid), formularios y **date pickers** consistentes con el tema (absorbe
  HU-28). Estados y foco visibles.

### HU-105 · Consistencia responsive y accesibilidad — `C`
**Como** usuario **quiero** que el rediseño sea accesible y responsive **para** usarlo en cualquier
dispositivo.
- Mobile-first, contraste AA, foco visible por teclado, dark por defecto en todo el sitio.

---

## Épica 22 — Rediseño del flujo de inscripción y armado del torneo

> Decisión del owner (aprobada): el armado del torneo se separa de la inscripción de equipos.
> Esta épica **supersede parcialmente** a R3, HU-37, HU-41, HU-42 y HU-49 (ver notas en cada una).
> Nuevo ciclo de vida: **Wizard** (torneo + estructura, sin equipos ni fixture) → **Inscripción**
> (fase abierta: se agregan/crean equipos) → **Cierre** (habilita asignación) → **Asignación**
> (repartir inscriptos en divisiones) → **Fixture** (al iniciar el torneo).

### HU-106 · El asistente crea torneo + estructura, sin equipos ni fixture — `M`
**Como** owner/admin **quiero** que el asistente cree el torneo y su estructura de divisiones/zonas/
copas **sin** seleccionar equipos ni generar el fixture **para** separar el armado de la inscripción.
- Se **elimina el paso de selección de equipos** del asistente (supersede HU-41): el wizard ya no
  lista ni asigna equipos.
- Las divisiones/zonas/copas se crean **vacías de equipos**; el fixture **no** se genera en la
  creación (supersede la generación temprana del asistente).
- El torneo queda en **"Inscripción abierta"** (`OpenForRegistration`), listo para inscribir.
- El paso de revisión (HU-49) ya **no** muestra equipos por zona: muestra la estructura (divisiones,
  formato de grupo, copas, series).

### HU-107 · Inscripción de equipos con el torneo abierto (crear nuevo / inscribir existente + copiar plantel) — `M`
**Como** owner/admin **quiero**, con el torneo en "Inscripción abierta", agregar equipos al torneo
**para** inscribir a los que se anotaron.
- **Club nuevo:** si no existe, se crea en el momento, scopeado a este torneo/temporada (D2).
- **Club existente (otra temporada):** se **inscribe** a este torneo y se le **copia el plantel** como
  base editable (HU-53). No se **duplica** la identidad del club (HU-99): se crea una nueva inscripción
  de temporada (`TeamTournamentRegistration`, HU-98).
- El plantel copiado es **editable** para la nueva temporada: altas de jugadores nuevos, bajas de los
  que no siguen. La ficha médica no se hereda (HU-59).
- Un admin/owner puede **mover un jugador de un club a otro** (antes del inicio o a mitad de
  temporada); el cambio afecta **solo la temporada actual** (`PlayerTeamRegistration`, HU-98), nunca
  las anteriores.
- Solo se inscribe/edita mientras el torneo está en "Inscripción abierta" (HU-31).
- El equipo queda **inscripto al torneo, todavía sin división** asignada.

### HU-108 · Asignar inscriptos a divisiones y generar el fixture al iniciar — `M`
**Como** owner/admin **quiero**, tras cerrar la inscripción, asignar los equipos inscriptos a
divisiones/zonas y **recién ahí** generar el fixture **para** armar los grupos con el padrón
definitivo.
- El **paso de asignación se habilita** cuando el torneo pasa a "Inscripción cerrada"
  (`RegistrationClosed`).
- Se reparten los equipos **inscriptos** en divisiones/zonas; un equipo va a **una sola** zona
  (supersede HU-42: la unicidad de zona ahora se valida en la asignación, no en el wizard).
- El fixture **ya no** se genera al cerrar inscripción (supersede R3 / HU-37): se genera al **iniciar
  el torneo** tras la asignación (nueva transición `RegistrationClosed → En curso`).
- Reusa la asignación equipo→fase existente y la generación **idempotente** del fixture (HU-64).

### HU-109 · Guardas de completitud: no permitir torneos que no se puedan completar — `M`
**Como** organizador de torneos **quiero** que el sistema impida armar o iniciar un torneo en un
estado imposible de completar **para** no terminar con grupos vacíos, copas sin equipos o fixtures
rotos. *(La estructura se configura en el wizard antes de conocer los inscriptos; estas guardas
cierran esa brecha.)*
- **Dos momentos de control:**
  - **Asignación** (`RegistrationClosed`): **validación en vivo** que le muestra al admin exactamente
    qué falta para poder iniciar.
  - **Iniciar torneo** (`RegistrationClosed → En curso`, disparador del fixture — HU-108): **bloqueo
    duro**; no se puede iniciar hasta que todo sea completable.
- **Reglas (criterio de básquet):**
  1. Cada división/zona que juega tiene **≥ 2 equipos** asignados (no hay grupo ni bracket con 0-1).
  2. Todo equipo **inscripto** está asignado a **exactamente una zona regular** (división con
     `IsCrossDivisionCup=false`): sin huérfanos y no en dos zonas (HU-42). **La copa cruzada NO cuenta
     como zona**: es una competencia paralela que incluye equipos de todas las zonas, así que un
     equipo juega su zona **y** la copa cruzada (HU-47) sin violar esta regla.
  3. **Copa cruzada** (HU-47): cantidad de grupos ≤ equipos; cada grupo **≥ 2**. El playoff soporta
     **BYE** para no-potencias-de-2 (HU-82).
  4. **Mapeos de playoff** (HU-45): si un rango arranca más allá de la cantidad de equipos de esa
     división, se **bloquea** hasta corregir (ajustar el mapeo o asignar más equipos); no se recorta
     ni se permite una copa vacía.
  5. Coherencia general de básquet: sin empates (R4); la siembra de playoff exige la fase de grupos
     completa; la serie final al mejor de N necesita 2 equipos.

### HU-110 · Copa cruzada con múltiples grupos de tamaño variable — `M`
**Como** organizador **quiero** dividir la copa cruzada en varios grupos (de tamaños distintos) cuyos
mejores clasifiquen a un único cuadro **para** modelar la Copa Club 12 tal como se juega.
- La copa cruzada (opcional por edición — HU-47) puede dividir sus equipos en **N grupos** de **tamaño
  variable**. *Ejemplo real:* 37 equipos → 10 grupos (7 de 4 + 3 de 3), doble rueda por grupo.
- Clasifican los **primeros K de cada grupo** (configurable; por defecto **1**) a **un único bracket**
  de la copa, sembrado con **BYE** cuando no son potencia de 2 (HU-82).
- **Gap actual:** el modelo arma la copa cruzada como un solo grupo. Hay que soportar N grupos que
  alimenten un cuadro común (candidato: N divisiones cruzadas cuyos clasificados se juntan por HU-45;
  verificar la generación antes de diseñar).
- Completitud (HU-109): cada grupo de la copa cruzada **≥ 2**; el bracket necesita **≥ 2** clasificados.

### HU-111 · Calendario: jornadas de zona y copa no se solapan; fechas primero, hora/cancha después — `M`
**Como** organizador **quiero** que al generar el fixture las jornadas de las zonas y las de la copa
cruzada **no caigan el mismo día**, y que primero se fijen solo **fechas** **para** poder asignar
horario y cancha con calma sin choques.
- La generación del fixture asigna **solo fechas** (jornadas): **sin horario ni cancha**.
- Las jornadas de **zonas (divisiones)** y de **copa cruzada** del mismo torneo **no se pisan en los
  mismos días** (un equipo no juega zona y copa el mismo día).
- Después, el admin asigna **hora + cancha** por partido (edición de fecha/cancha ya existe: HU-66/67).
- El orden de jornadas queda fijo (HU-67); editar la fecha de un partido no cambia su jornada (HU-68).

---

## Resumen de prioridades

- **Must (arrancar ya):** HU-01, HU-03, HU-05, HU-06, HU-11/12/13, HU-18/19/20/21,
  HU-22/23/24, HU-26, HU-29/30/31/32/33, HU-35/37, HU-38/39/41/42/43/44/45/46/47/48,
  HU-50/51, HU-55/56/57/60/61, HU-63/64/65/66/67, HU-69/70/71/72, HU-74/75,
  HU-78/80/81/82, HU-83/84, HU-90, HU-91/92/93, HU-98, HU-102 (design system).
- **Should:** HU-02, HU-04, HU-07/08, HU-14/15, HU-25, HU-27, HU-34, HU-36, HU-40, HU-49, HU-52,
  HU-58/59, HU-73, HU-76, HU-79, HU-85/86, HU-94/95/96/97, HU-100, HU-103/104 (rediseño).
- **Could / Fase 2:** HU-09/10 (F2), HU-16/17, HU-28, HU-53/54, HU-62, HU-68, HU-77,
  HU-87/88/89, HU-99, HU-101, HU-105.

### Orden de ataque sugerido
1. **Fundacional:** HU-98 (season-scoping) y HU-05 (roles) — sostienen casi todo lo demás.
2. **Bugs que bloquean el uso real:** HU-21, HU-23/24, HU-32/33, HU-03.
3. **Núcleo del gestor:** máquina de estados (HU-35/37) → asistente (HU-38–48) → fixture
   (HU-63–67) → carga de partido y goleadores (HU-69–72) → clasificación/playoffs (HU-78–82).
4. **Alrededor:** ficha médica/elegibilidad (HU-55–61), sanciones (HU-74–76), estadísticas
   (HU-83–86), respaldos (HU-91–93).

> Cada épica "Must" es candidata natural a un change de OpenSpec independiente.
