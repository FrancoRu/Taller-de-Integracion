# Club 12 — Reglas de negocio y consistencias (para conocer y respetar en toda la UI/UX)

Este documento captura las reglas de negocio del dominio para que la UI las respete de forma consistente (qué se muestra/permite en cada estado) y para no re-descubrirlas bug por bug. Complementa `Docs/REVISION-FRANCO.md` (changelog + bugs).

## 1) Ciclo de vida del TORNEO (state machine)

`Scheduled → OpenForRegistration → RegistrationClosed → Ongoing → Finished`
(+ `Canceled` desde cualquier no-terminal; `Finished`/`Canceled` son terminales).
**Excepción agregada (#85):** `Ongoing → RegistrationClosed` ("Revertir a borrador"), sólo si NO hay partidos jugados; borra el fixture y conserva asignaciones.

Qué se puede/muestra en cada estado (regla → dónde se aplica):

| Acción / UI | Scheduled | OpenForReg | RegClosed | Ongoing | Finished |
|---|---|---|---|---|---|
| Editar datos del torneo | ✅ | ✅ | ✅ | ✅ (limitado) | ❌ |
| Abrir inscripción | ✅→ | — | — | — | — |
| Inscribir/dar de baja equipos | ❌ | ✅ | ❌ | ❌ | ❌ |
| **Nueva División** / editar estructura (fases) | ✅ | ✅ | ✅ | ❌ (#87) | ❌ |
| **Asignar equipos a zonas** (draft) | ❌ | ✅ | ✅ | ❌ | ❌ |
| **Iniciar torneo** (cierra insc. + genera fixture) | ❌ | ✅* | ✅ | — | — |
| **Revertir a borrador** (borra fixture) | — | — | — | ✅ (si 0 jugados) | ❌ |
| Cargar resultados / goleadores | ❌ | ❌ | ❌ | ✅ | ✅ (correcciones) |
| Campeones resueltos | — | — | — | parcial | ✅ |

*Iniciar desde OpenForRegistration: el botón cierra la inscripción y arranca en cadena (#78).

**Regla clave:** el fixture (partidos) se genera SÓLO al pasar a Ongoing. Antes de eso NO hay partidos. Por eso mover equipos de zona antes de iniciar es limpio (no hay fixture que regenerar).

**Inconsistencias a auditar en la UI (deben respetar la tabla):**
- Ocultar/deshabilitar según estado: Nueva División ✅(#87), Asignación ✅, Inscriptos ✅. Falta revisar: "Editar división" (¿permitir cambiar puntos/estructura en Ongoing?), acciones de equipos/jugadores, etc.
- "Volver" de una división debe ir a SU torneo, no a la lista global (arreglado #86).

## 2) División / Zonas

- Una **División = un "tier"** (Zona A, B, C, D, Reserva, Primera…). El equipo juega en UNA zona regular (regla "un equipo, una zona"). La copa cruzada es membresía paralela.
- Cada división corre una **fase de grupos** (round-robin, `RoundRobinLegs` 1 o 2) y luego **playoffs por sub-copa** (Copa Oro, Plata, Bronce…), seedeadas por rango de posición (`DivisionPlayoffMapping`, ej. 1-4 → Oro, 5-9 → Plata).
- Las **standings** siembran a TODOS los equipos asignados en 0-0 desde que arranca (#81), no sólo los que jugaron.
- La tabla colorea las filas que clasifican a cada copa (público y panel #82). Multi-grupo (copa cruzada) todavía NO pasa `qualificationRanges` por grupo → sin coloreo ahí (pendiente).

## 3) Brackets / Playoffs — REGLA DE BYES (bug abierto)

Un bracket de eliminación con N equipos que NO es potencia de 2 debe usar **byes**: los mejores seeds descansan la primera ronda.

- **byes = próxima_potencia_de_2(N) − N.** Los `byes` mejores seeds pasan directo; el resto juega la primera ronda.
- **Ejemplo del owner (Copa Plata, Zona A, N=5, posiciones 5-9):**
  - byes = 8 − 5 = 3 → seeds **5, 6, 7 van directo a SEMIFINALES**.
  - seeds **8 y 9 juegan CUARTOS** (1 partido). El ganador entra a semis.
  - Semis: (5 vs [8/9]) y (6 vs 7). Final: los dos ganadores.
- **Estado actual (BUG):** la generación crea una primera ronda "llena" (ej. Copa Plata muestra "4 partidos" en Cuartos) sin byes. Hay que corregir la **generación de stages/partidos de eliminación** (qualifiersToStageTypes + seeding con byes) en backend, y el render del bracket.
- Además el bracket **se corta** visualmente en el detalle de división (overflow del componente `PlayoffBracket`).

## 4) Equipos vs Equipos inscriptos (a definir)

Hoy conviven "Equipos" (creados en el torneo, `Team.TournamentId`) y "Equipos inscriptos" (registro). En la práctica muestran lo mismo → el owner pide **coherencia**: definir el modelo y dejar una sola noción/tab clara. La asignación y la completabilidad razonan sobre los inscriptos.

## 5) Navegación (pedido del owner, IMPORTANTE)

El árbol del admin debe tener jerarquía: **Torneos DENTRO de Temporadas** (Temporada → Torneos → Divisiones → …). "Torneos" no debe ser un item top-level suelto. Esto además evita la lista global de divisiones que mezcla torneos.

## 6) Loading / estados vacíos (regla transversal)

SIEMPRE mostrar loading/skeleton hasta tener los datos. El texto de "no hay nada" sólo después de que el fetch resolvió y realmente está vacío — nunca antes del fetch. (Varias públicas lo hacen mal, ej. página pública de equipos.)
