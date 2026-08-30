# Revisión Club 12 — estado, bugs y pendientes (para Franco)

Fecha: 2026-08-30. Rama de integración: `develop`. Todo lo marcado ✅ ya está mergeado a `develop` (PRs #72–#81) y va a staging.

Este documento resume: (1) qué se arregló, (2) qué falta / qué revisar, y (3) verificación de la base de datos. Complementa `Docs/HANDOFF.md`.

---

## 1) Verificación de la base de datos (staging)

**La DB está bien armada.** Se verificó vía API (con la sesión del owner) sobre el "Torneo Apertura 2026" (masculino, en curso):
- 4 divisiones/zonas (Zona A–D) existen.
- Zona A tiene **11 equipos asignados** (StageTeamMatch) — idem las demás.
- **315 partidos** generados (fixture completo: grupos + Copa Oro + Copa Plata).

Lo que se percibía como "no se asignaron equipos / no anda nada" **NO era la DB**, sino bugs de cálculo/render (ver abajo). No hace falta rehacer la DB.

---

## 2) Bugs encontrados por E2E y ARREGLADOS (PRs #78–#81)

- ✅ **Iniciar torneo daba error / "se rompía" (#78).** El botón "Iniciar torneo" estaba habilitado con la inscripción abierta, pero el backend sólo permite `OpenForRegistration → RegistrationClosed → Ongoing` (error *"Cannot change tournament status..."*). Ahora "Iniciar torneo" **cierra la inscripción y después inicia** (encadena las transiciones) y no deja iniciar si queda algún inscripto sin zona.
- ✅ **Divisiones se veían vacías en el admin (#79).** La API devolvía las 4 zonas (200) pero el listado mostraba "0-0 de 0" porque derivaba las filas de un context compartido (race). Ahora renderiza la respuesta del fetch.
- ✅ **Standings/count de equipos en 0 (#81).** Las posiciones se calculaban SÓLO con partidos jugados, así que un torneo recién iniciado mostraba tablas vacías y "Equipos = 0". Ahora la tabla se siembra con TODOS los equipos asignados a la zona (0-0) desde el arranque.
- ✅ **Alertas duplicadas / apiladas (#81).** El toast global ahora deduplica mensajes idénticos dentro de 2.5s (dos requests encadenados o el mismo error surfaceado dos veces ya no apilan). "Iniciar torneo" dejó de mostrar un dialog extra encima del toast.
- ✅ **ABM de Fases quitado (#80).** El detalle de división ya no tiene tab "Fases" (es todo automático).

## Bugs/UX arreglados antes (PRs #72–#77)

- ✅ Home season-first, wizard con temporada obligatoria, cuelgue del panel "Administración de datos" (deadlock de modales), loader de Novedades, cards de Torneos.
- ✅ Campeones por sub-copa (Oro/Plata/Bronce) + jerarquía; mapa OpenStreetMap.
- ✅ Asignación de equipos a zonas rehecha: draft con inscripción abierta, picker con buscador multiselect, quitar equipo, pool sin zona, zonas colapsables, sin recargar.
- ✅ Escudo transparente (sin fondo naranja), código de equipo 3 letras, alertas por encima de los modales.
- ✅ Scroll-to-top al navegar, editar publicación 404, imagen de noticia más chica.
- ✅ Canchas sin imagen (opcional en backend + quitada del admin).

---

## 3) PENDIENTE / a revisar

### A. Navegación y coherencia (pedido del owner)
- **Árbol de navegación con jerarquía**: "Torneos" NO debe ser un item top-level del nav; debe vivir DENTRO de "Temporadas" (temporada → sus torneos). Rediseñar el sitemap del admin.
- **Tabs del torneo redundantes**: "EQUIPOS" y "EQUIPOS INSCRIPTOS" muestran los mismos equipos. Definir/consolidar el modelo Equipos ↔ Inscriptos y dejar tabs coherentes.
- **Ver vs Editar**: el owner quiere un único botón **"Ver"**, y que la edición esté DENTRO del detalle (no dos acciones separadas Ver/Editar en las tablas). Aplica a torneos, equipos, canchas, jugadores, divisiones, etc.

### B. Robustez / backend
- **500 transitorio al generar el fixture** (al iniciar un torneo con ~40 equipos): "después de un tiempo anduvo". Revisar logs (traceId ejemplo: `00-a05e8d94...`) y perfilar la generación (posible timeout / deadlock / operación pesada no transaccional). Idealmente hacer la generación idempotente y/o en background.
- **Creación de torneo NO es atómica** (Image #12): el wizard hace muchos requests secuenciales (POST tournaments → PUT open-registration → POST divisions → POST stages uno por uno). Debería ser UN endpoint transaccional.

### C. Rutas huérfanas / limpieza
- Quedan rutas globales de fases sin link (`/panel/fases`, `panelStages`/`panelStageCreate`/`panelStage` en `App.tsx`) — borrar en una limpieza (el ABM de fases ya se sacó del detalle de división).

### D. Features / contenido
- **Staff de equipo (cuerpo técnico)**: feature incompleto (el worker cayó 3× por límite de sesión). Hay una migración iniciada en un worktree. Rehacer.
- **README y MANUAL DE USUARIO**: actualizar con el flujo real (crear temporada → crear torneo dentro de la temporada → inscribir equipos → asignar a zonas → iniciar → jugar) y con las nuevas pantallas.

### E. Misceláneos observados en E2E
- El renderer se colgó una vez en la tab Divisiones (CDP timeout) — vigilar si se repite con muchos datos.
- Copa cruzada: colorear en la tabla los que clasifican a playoff.
- Wizard de playoff: permitir configurar "partido por el 3er puesto" (hoy no se puede).

---

## 4) Notas de verificación

- Frontend: `tsc` + `eslint --max-warnings 0` + `vitest` (497 tests) + `vite build`, todo verde en cada PR.
- Backend: `dotnet build` 0 warnings + `dotnet test` (728 tests) verde.
- E2E: hecho con Chrome sobre la sesión logueada del owner + consultas directas a la API con el token de sesión.
