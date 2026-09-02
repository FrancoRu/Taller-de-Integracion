# Club 12 — Checklist E2E final (acceptance criteria por pantalla)

> Generado 2026-09-02 a partir de las rutas reales en `App.tsx`, las reglas de
> negocio en [`ESTADO-Y-REGLAS.md`](./ESTADO-Y-REGLAS.md) y los criterios de
> aceptación de [`historias-de-usuario.md`](./historias-de-usuario.md).
>
> **Estado de ejecución (2026-09-02, tarde):** se corrió un sweep automatizado
> (`Club12-WebClient/e2e/90-public-qa-sweep.spec.ts`, Playwright headless
> contra `E2E_BASE_URL=https://club12.argentum-solutions.com.ar`) para la
> parte pública que no depende de datos hardcodeados. Resultado: **8/11
> passed**. Los 3 que fallaron, y CUALQUIER otro ítem de este checklist que
> dependa de datos reales (todo lo admin, todo lo público que muestre un
> torneo/equipo/partido concreto), están **bloqueados por un incidente en
> vivo**: el backend de staging devuelve 502 en `/api/health` y en
> `/api/tournaments` de forma persistente (confirmado en 3+ intentos
> espaciados, no es el blip transitorio post-deploy ya conocido — el último
> deploy de backend exitoso fue ~40+ min antes de este chequeo). No tengo
> acceso SSH/Docker al servidor para reiniciarlo; el usuario decidió seguir
> marcando cada ítem afectado como bloqueado en vez de esperar. Cada casillero
> abajo dice `[x]` (verificado), `[BLOQUEADO 502]` (no se pudo probar por el
> incidente), o `[ ]` (no llegó a intentarse, más allá del 502).

Leyenda de tipo de check: **(L)** lista/filtros · **(C)** crear · **(E)**
editar · **(D)** eliminar · **(V)** ver/detalle · **(N)** navegación ·
**(X)** caso límite / error esperado.

---

> **[BLOQUEADO 502] Secciones 0 a 11 (todo el panel admin) no se pudieron
> probar en absoluto**: el login hace `POST /api/auth/login`, que con el
> backend caído devuelve 502 antes de siquiera intentar la autenticación —
> no hay forma de entrar al panel hasta que el backend esté arriba de nuevo.

## 0. Auth

- [ ] `/login` no es accesible desde ningún link de la nav pública — solo tipeando la URL. (N)
- [ ] `/login` sin header/footer. (V)
- [ ] Login con credenciales válidas → redirige al panel (Temporadas). (C)
- [ ] Login con credenciales inválidas → error inline en español, sin alert bloqueante. (X)
- [ ] Cerrar sesión → redirige al home público, ya no se puede navegar al panel sin volver a loguear. (N)
- [ ] `/panel/configuracion/cambiar-password` cambia la contraseña propia; falla con mensaje claro si la actual es incorrecta. (C)(X)
- [ ] URL inexistente → 404 sin header/footer, con botón "volver al inicio". (X)

## 1. Panel — Temporadas (`/panel/temporadas`)

- [ ] Lista pagina/filtra correctamente; loading visible mientras carga, nunca "vacío" antes de que resuelva el fetch. (L)
- [ ] Crear temporada (nombre + año) → aparece en la lista sin recargar manualmente. (C)
- [ ] Ir al detalle (`/panel/temporadas/:id`) lista los torneos de esa temporada agrupados por categoría. (V)
- [ ] "Nuevo Torneo" desde el detalle abre el wizard **pre-scopeado** a esa temporada (no pide elegir temporada de nuevo). (N)
- [ ] Eliminar torneo desde el listado de la temporada funciona y refresca la lista. (D)
- [ ] Eliminar una temporada con torneos "huérfanos" no deja torneos fantasma en selectores de otras pantallas (bug ya corregido — reverificar). (X)

## 2. Panel — Torneos

### 2.1 Wizard (`/panel/torneos/asistente`)

- [ ] Requiere temporada (no hay forma de saltear el paso). (X)
- [ ] Valida fecha límite de inscripción < fecha de inicio. (X)
- [ ] Selección de equipos scopeada correctamente (no repite equipo ya en otra zona del mismo wizard). (X)
- [ ] Configurar fase de grupos (1 o 2 vueltas). (C)
- [ ] Configurar copas Oro/Plata (+ rangos de posición → copa). (C)
- [ ] Configurar serie final a mejor de N (BO1/3/5/7). (C)
- [ ] Copa cruzada opcional con playoff obligatorio. (C)
- [ ] Paso de revisión final antes de confirmar. (V)
- [ ] Confirmar crea TODO (torneo + divisiones + stages + copas) — falla entero o crea entero, no a medias. (C)(X)
- [ ] Torneo creado por el wizard arranca en `Scheduled`/`OpenForRegistration`, sin equipos ni fixture todavía.

### 2.2 Listado / detalle / edición

- [ ] `/panel/torneos` lista y filtra. (L)
- [ ] "Ver" en la lista NO permite editar el estado del torneo directamente (solo el detalle/editar dedicado hace eso). (X)
- [ ] `/panel/torneos/:id` muestra sub-tabs coherentes (sin "Equipos"/"Equipos inscriptos" duplicados). (V)
- [ ] `/panel/torneos/:id/editar` persiste cambios y vuelve al detalle (no a un 404, no a la lista global). (E)
- [ ] Inscribir equipo (torneo en `OpenForRegistration`): alta de equipo nuevo o inscripción de uno existente + copiar plantel si aplica. (C)
- [ ] Dar de baja un equipo inscripto (mientras `OpenForRegistration`). (D)
- [ ] Asignar equipos inscriptos a zonas/división (draft, sin recargar la página completa). (C)
- [ ] Quitar un equipo ya asignado de su zona. (D)
- [ ] "Iniciar torneo": si está en `OpenForRegistration`, cierra inscripción y arranca en la misma acción; bloquea si queda algún inscripto sin zona. (C)(X)
- [ ] Tras iniciar: el fixture se generó (partidos existen), las standings arrancan en 0-0 para TODOS los equipos asignados (no solo los que jugaron). (V)
- [ ] "Revertir a borrador" (`Ongoing → RegistrationClosed`): funciona si 0 partidos jugados; bloqueado (409, mensaje claro) si hay al menos uno jugado. (C)(X)
- [ ] Editar estructura (nueva división/fase) bloqueado una vez `Ongoing`/`Finished`. (X)
- [ ] "Volver" del torneo lleva a su temporada, no a una lista global. (N)

## 3. Panel — Divisiones

- [ ] `/panel/divisiones` lista/filtra (sin mezclar divisiones de distintos torneos de forma confusa). (L)
- [ ] `/panel/divisiones/crear` y `/panel/divisiones/:id/editar` bloqueados si el torneo ya arrancó. (X)
- [ ] `/panel/divisiones/:id` — tab **Detalle**. (V)
- [ ] Tab **Equipos**: lista los equipos de la división, click navega al equipo. (V)(N)
- [ ] Tab **Posiciones**: PJ/PG/PP/GF/GC/DIF/Pts correctos; deducción de puntos (si aplica) restada ANTES de desempates, no clampeada en 0. (V)
- [ ] Tab **Posiciones**: filas que clasifican coloreadas por copa (Oro/Plata/Bronce) + leyenda; funciona también en la copa cruzada multi-grupo. (V)
- [ ] Tab **Posiciones**: botón Imprimir abre el diálogo de impresión del navegador con SOLO la tabla (sin chrome de la app); Exportar CSV descarga con headers correctos. (C)
- [ ] Tab **Goleadores** (nueva): ranking jugador/dorsal/puntos/equipo, ordenado por puntos descendente; Imprimir + Exportar CSV igual que Posiciones; estado vacío cuando no hay goleadores todavía. (V)(X)
- [ ] Tab **Partidos**: fixture agrupado por fecha, click a un partido lleva al detalle admin. (V)(N)
- [ ] Tab **Playoff**: al completar la última fecha de fase de grupos, las copas se siembran solas (auto-seed) sin acción manual. (X)
- [ ] Tab **Playoff**: bracket con byes correctos para N no potencia de 2 (ej. 5 equipos → 3 byes a semis, 2 juegan cuartos). (V)(X)
- [ ] Tab **Playoff**: cada serie BO3/5/7 muestra sus partidos reales individuales (no un único partido colapsado), tanto en admin como en la vista pública. (V)
- [ ] Tab **Playoff**: cargar resultado de un partido de serie actualiza el tally de la serie correctamente.

## 4. Panel — Equipos

- [ ] `/panel/equipos` lista/filtra por nombre. (L)
- [ ] `/panel/registro-equipos` inscribe un equipo (nuevo o existente) a un torneo abierto. (C)
- [ ] `/panel/equipos/:id` tabs: Detalle, **Jugadores** (plantel), Puntuaciones, Sanciones, Cuerpo técnico.
- [ ] Tab **Detalle**: editar nombre/código 3 letras/color/estilo de camiseta/escudo persiste y refleja en la vista pública. (E)
- [ ] Tab **Jugadores** — fila editable inline: "Nuevo Jugador" agrega una fila en modo edición directamente en la tabla (no un popup). (C)
- [ ] Fila nueva: Guardar con campos incompletos → advertencia en español, la fila NO se pierde, sigue editable. (X)
- [ ] Fila nueva: documento inválido / teléfono inválido / fecha de nacimiento bajo la edad mínima → cada uno con su mensaje específico. (X)
- [ ] Fila nueva: Guardar con datos válidos → crea el jugador, la fila desaparece de "nueva" y aparece en el listado real. (C)
- [ ] Fila nueva: Descartar → la fila se borra sin llamar a la API. (D)
- [ ] Jugadores YA existentes en la tabla siguen sin ser editables inline (la edición vive en el detalle del jugador) — doble click no debe abrir edición. (X)
- [ ] "Importar plantel desde CSV": descarga de plantilla trae las columnas correctas en el mismo orden que el alta/edición individual. (C)
- [ ] Importar CSV con filas mixtas (válidas + inválidas): las inválidas se listan con su motivo y NO se importan; las válidas sí, y el resumen final cuenta bien ambos grupos. (X)
- [ ] Importar CSV vacío (solo headers) → advertencia, no crashea. (X)
- [ ] Acción "Dorsal" (contexto equipo+torneo): asigna/quita dorsal; rechaza (409 con motivo) dorsal duplicado en el mismo equipo+temporada. (C)(X)
- [ ] Acción "Ficha médica": subir, aprobar/rechazar; una vez `Approved` no se puede volver a subir. (C)(X)
- [ ] Eliminar jugador con historial (stats/goleador/sanciones) → bloqueado con 409 y motivo mostrado (no un falso "eliminado"). (X)
- [ ] Tab **Cuerpo técnico**: alta/baja de Staff con rol DT o Asistente (ya NO existe "DT-Jugador"). (C)(D)(X)
- [ ] `/panel/clubes/:idOrSlug`: historial del club entre temporadas. (V)

## 5. Panel — Jugadores (listado global, `/panel/jugadores`)

- [ ] Lista pagina/filtra por nombre/apellido/documento/teléfono. (L)
- [ ] "Ver" navega por slug (`/panel/jugadores/:slug`), no por UUID. (N)
- [ ] Fila con `documentNumber` faltante no crashea (muestra "—"). (X)
- [ ] Sin acción "Editar" en la lista (edición vive en el detalle del jugador) — consistente con el tab Jugadores del equipo. (X)
- [ ] Fuera de un contexto de equipo+torneo, no aparecen las acciones Ficha médica/Dorsal. (X)

## 6. Panel — Partidos (`/panel/partidos/:id`)

- [ ] Detalle muestra estado (pendiente/jugado), equipos, cancha, fecha. (V)
- [ ] Editar cancha/fecha de un partido no jugado; rechaza (409) si la cancha ya tiene otro partido a menos de 2h. (E)(X)
- [ ] No permite editar cancha/fecha de un partido ya jugado. (X)
- [ ] Cargar resultado: sin empates, exige ganador; checkbox de tiempo extra disponible. (C)
- [ ] Tab Puntuaciones: goleadores por equipo, suma = marcador final, solo jugadores habilitados. (C)(X)
- [ ] Walkover (W.O.) disponible como resultado alternativo. (C)
- [ ] Cargar sanción desde el partido (jugador/equipo/staff). (C)

## 7. Panel — Sanciones

- [ ] `/panel/sanciones` lista/filtra. (L)
- [ ] `/panel/sanciones/:id`: detalle con jugador, partido, motivo, duración. (V)
- [ ] `/panel/sanciones/editar/:id`: editar persiste. (E)
- [ ] Apelar sanción → estado pasa a "pendiente de revisión"; aceptar la levanta, rechazar la mantiene vigente. (C)(X)
- [ ] Vencimiento automático por días de calendario limpia sanciones cumplidas (mecanismo técnico, no cambia "cuántas fechas faltan" que se calcula por jornada). (X)

## 8. Panel — Canchas (`/panel/canchas`)

- [ ] Lista/filtra. (L)
- [ ] Crear/editar cancha con imagen + geocoding de dirección (completa lat/lng, muestra preview del mapa). (C)(E)
- [ ] Eliminar cancha con partidos asociados → bloqueado (409) con motivo. (X)
- [ ] Vista de detalle: foto hero (o placeholder), dirección + mapa embebido. (V)

## 9. Panel — Novedades / Blog

- [ ] `/panel/blog` lista, con estado borrador/publicada visible. (L)
- [ ] `/panel/blog/crear` publica una novedad nueva, visible en la vista pública tras publicar. (C)
- [ ] `/panel/blog/:id/editar` persiste cambios. (E)
- [ ] "Ver" desde el panel muestra el detalle ADMIN (no navega a la web pública). (X)
- [ ] Eliminar publicación. (D)

## 10. Panel — Usuarios

- [ ] `/panel/usuarios` lista/filtra. (L)
- [ ] `/panel/usuarios/crear` da de alta un usuario con rol. (C)
- [ ] `/panel/usuarios/invitar` manda invitación por magic link. (C)
- [ ] `/panel/usuarios/:id/editar` persiste cambios de rol/datos. (E)
- [ ] Activar/desactivar cuenta (lockout de Identity). (C)(X)
- [ ] Blanquear/resetear contraseña de otro usuario. (C)
- [ ] `/panel/configuracion/editar-perfil` edita el perfil propio. (E)

## 11. Panel — Estadísticas / Auditoría / Administración de datos

- [ ] `/panel/estadisticas`: filtro por Temporada + Torneo (el torneo se deriva de la temporada elegida) scopea el ranking; sin filtro = global. (L)(X)
- [ ] `/panel/auditoria`: acciones administrativas sensibles quedan registradas con actor+entidad+acción+timestamp; nombres legibles (no UUIDs crudos), texto en español, celdas largas con tooltip en vez de overflow. (V)
- [ ] `/panel/administracion-datos`: respaldo manual → popup explicando el proceso → spinner → bloquea navegación hasta terminar. (C)(X)
- [ ] Restaurar desde un respaldo → genera un respaldo automático de salvaguarda ANTES de sobrescribir. (X)
- [ ] Límite de cantidad de respaldos respetado (los más viejos se descartan). (X)
- [ ] Borrado total de datos solo accesible para el rol correcto (Admin IT). (X)
- [ ] Ya NO existe el botón "Cargar Datos de prueba" (se sacó este mismo trabajo). (X)

## 12. Público — Home (`/`)

- [x] 2026-09-02: Home carga, `<h1>` visible, 0 errores de consola (sweep automatizado). (V)
- [ ] Orden exacto Hero → Novedades → Temporadas → Campeones — no verificado visualmente, solo "no crashea". (V)
- [ ] Loading/skeleton visible mientras carga; nunca "no hay nada" antes de que el fetch resuelva. (X)

## 13. Público — Temporadas (`/temporadas`, `/temporadas/:id`)

- [x] 2026-09-02: lista carga (`<h1>` visible), drill-in al detalle de la primera temporada si existe, 0 errores de consola (sweep automatizado). (L)(V)
- [ ] Detalle agrupa torneos masc./fem. correctamente — no verificado el agrupamiento en sí, solo que la página no crashea.

## 14. Público — Torneos (`/torneos`, `/torneos/:id`)

- [BLOQUEADO 502] `/torneos` no tiene ningún link `a[href^="/torneos/"]` — la lista pública de torneos está vacía O el 502 del backend impide que cargue. El sweep no pudo distinguir cuál de las dos causas es, porque `GET /api/tournaments` también devuelve 502 directamente (confirmado por curl). Con el backend caído, esta sección entera (tabs, llaves, podio, imprimir) queda sin poder verificarse. (L)(V)(X)

## 15. Público — Equipo (`/equipos/:teamId`)

- [BLOQUEADO 502] Depende de entrar por un torneo (§14) para encontrar un equipo real; no se pudo verificar nada de esta sección. (V)(X)

## 16. Público — Partido (`/partidos/:matchId`)

- [BLOQUEADO 502] Depende de entrar por un torneo (§14) para encontrar un partido real; no se pudo verificar nada de esta sección. (V)(X)

## 17. Público — Sanciones (`/sanciones`) y Campeones (`/campeones`)

- [x] 2026-09-02: ambas páginas cargan (`<h1>` visible), 0 errores de consola (sweep automatizado). (V)
- [ ] Búsqueda/filtro de sanciones y jerarquía de Campeones — no ejercidos, solo carga básica verificada.

## 18. Público — Novedades (`/blog`, `/blog/:idOrSlug`)

- [x] 2026-09-02: lista carga, drill-in al primer post si existe, 0 errores de consola (sweep automatizado). (L)(V)

## 19. Institucional

- [x] 2026-09-02: `/quienes-somos`, `/ficha-medica`, `/reglamento` cargan sin sesión, 0 errores de consola (sweep automatizado). (V)
- [ ] Descarga real de la plantilla PDF de ficha médica — no ejercida (solo carga de la página).

## 20. Extra verificado por el sweep (no estaba en la lista original)

- [x] 2026-09-02: `/esta-ruta-no-existe` muestra 404 con texto "no existe"/"no encontr...". (X)
- [x] 2026-09-02: `/login` no tiene ningún link `a[href="/login"]` en el home; `/login` en sí es alcanzable directo y muestra el botón "Iniciar Sesión". (N)

---

## Cómo ejecutar esto

**Opción A — en vivo con la extensión de Chrome conectada:** recorrer cada
sección de arriba en staging (club12.argentum-solutions.com.ar), marcando
cada casillero.

**Opción B — Playwright existente (`Club12-WebClient/e2e/`):** requiere
backend local (`dotnet run --project Club12-Backend/API --launch-profile
Facundo`) + frontend local (`pnpm dev`) corriendo, Y reescribir
`e2e/fixtures.ts` (nombres de equipo/zona hardcodeados de un seed anterior
al rebuild de `DataSeeder` de esta sesión) contra los equipos/zonas reales
del seed actual antes de que las specs tengan sentido.
