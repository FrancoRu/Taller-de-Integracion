# Club 12 — QA / UX Overhaul Backlog

Pedido del owner tras el E2E en vivo. Dos lados siempre presentes: **visitante** (público) y **admin** (gestiona todo). Todo el texto de cara al usuario en **español**, consistente. Slugs siempre (no UUIDs). HTML semántico.

## Fase 1 — Bugs funcionales ("todo tiene que andar")
- [ ] **Subir archivos**: falla con "Unsupported Media Type" (ficha médica / logo / imagen blog). Content-Type / multipart.
- [ ] **Descargar ficha médica** subida de un jugador falla.
- [ ] **GET por slug**: muchos GET (jugadores y demás) no andan por slug; todo está por id. Deben andar por slug en todas las entidades.
- [ ] **Quedan UUIDs por todos lados** en rutas/links — reemplazar por slugs.
- [ ] **Contador de vistas de blogpost** no anda bien.
- [ ] **Sanción a un equipo**: el nombre del equipo no aparece en la lista de sanciones.
- [ ] **Navegación torneos**: "Ver" en la lista lleva al DETAIL editable (se puede cambiar el estado — no debería, es view). "Volver" no lleva a la lista sino al edit de ese torneo.
- [ ] **Botón "editar divisiones"** dentro del tab de torneos: deshabilitado sin razón.
- [ ] **Fases de división editables**: no deberían editarse; solo ver los partidos de esa fase para modificarlos.
- [ ] **Detalle de partido (admin)**: no se puede ver el detalle de un partido (jugado o programado) para cargar resultado, goleadores, etc.
- [ ] **Cache/stale**: el loading a veces se cachea y las listas quedan mal.

## Fase 2 — Validaciones
- [ ] Validaciones bien hechas: **teléfono, email**, y demás donde se pida (front + back).

## Fase 3 — Feedback de respaldo/restauración
- [ ] Al hacer o aplicar un respaldo: **popup** explicando el proceso → **spinner** → **bloquear navegación** (no dejar moverse a otras páginas) hasta terminar.

## Fase 4 — Design system + UX (rework grande)
- [ ] **Design system propio** (usar herramienta/enfoque de diseño), aplicarlo con **consistencia** en toda la app.
- [ ] **Skeletons/spinners** mientras cargan las cosas.
- [ ] **Height del main content constante** entre carga y con datos (sin saltos), uniforme.
- [ ] **HTML semánticamente correcto** + buena estructura y navegación.
- [ ] **Filtros** de todas las pantallas: que tengan sentido, coherentes por lado (visitante vs admin).
- [ ] **Todos los mensajes de error/alertas y todo el texto en español**, en todos lados.
- [ ] **Camiseta**: mostrar un **SVG de camiseta** con el color/estilo del equipo (en vez de un color plano).
- [ ] **Vista view de equipo (visitante)**: fondo con el **escudo del club** semi-transparente + tintado con el **color del equipo**.

## Fase 5 — Seed más real
- [ ] Datos más reales de jugadores: **DNIs, obras sociales, nombres** realistas.

## Fase 6 — Auditoría E2E final (QA)
- [ ] Full audit E2E de toda la página, verificar cada fix, cazar bugs de navegación restantes.
