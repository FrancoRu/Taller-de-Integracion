# Manual de Usuario — Liga Club12

Guía de uso del sistema para administradores y visitantes. Para información técnica (arquitectura, instalación, requisitos cubiertos) ver [README.md](./README.md).

## 1. Perfiles de usuario

El sistema tiene dos tipos de acceso:

- **Visitante (sin cuenta)**: puede consultar toda la información pública de la liga desde la página principal, sin necesidad de registrarse ni iniciar sesión.
- **Usuario administrador (con cuenta)**: accede al panel privado para cargar y mantener toda la información de las temporadas y torneos. Los roles definen qué secciones puede administrar cada usuario.

## 2. Vista pública (sin iniciar sesión)

El menú principal tiene las secciones: **Inicio**, **Temporadas**, **Campeones**, **Sanciones**, **Novedades**, **Quiénes somos** e **Información** (con las sub-páginas **Ficha médica** y **Reglamento**).

La navegación de la competición sigue el orden **Temporada → Torneo → División**: no hay un listado plano de "torneos" — se entra por la temporada, se elige el torneo dentro de ella y ahí se accede a cada división.

Desde estas secciones, cualquier visitante puede:

- Ver la lista de **temporadas** y, dentro de cada una, sus **torneos** (activos y finalizados).
- Consultar los **equipos** participantes de cada división, con su plantel de jugadores.
- Ver el **fixture** y los **resultados** de los partidos jugados y a jugarse.
- Consultar la **tabla de posiciones** de cada división/etapa. El equipo en 1er puesto se destaca con una corona apenas toma la punta — no hace falta que termine la fase para verlo.
- Ver la **tabla de goleadores** (limitada a los 10 primeros en la vista pública).
- Ver las **llaves de eliminación** ("Llaves") como un árbol de bracket, con los cruces de cada ronda hasta la final.
- **Imprimir** la tabla de posiciones de una división.
- Consultar las **sanciones vigentes** aplicadas a jugadores.
- Leer las **novedades** publicadas en el blog de la liga.
- Ver la sección **Campeones**: el historial de campeones por división/copa de los torneos ya finalizados.
- Leer **Quiénes somos** y, dentro de **Información**, la página de **Ficha médica** (qué es y por qué se pide) y el **Reglamento** de la liga.

No se requiere ninguna acción de registro para acceder a esta información.

### 2.1 Ver las llaves de eliminación

1. Entrar a la división deseada dentro de un torneo.
2. Ir a la pestaña **"Llaves"** (junto a "Partidos" y "Posiciones").
3. El sistema muestra el árbol de la fase eliminatoria, incluyendo — cuando el torneo tiene más de una copa (por ejemplo Copa Oro y Copa Plata, según el rango de posiciones de la fase de grupos) — cada copa por separado.
4. Si un partido todavía no se jugó, ese cruce se muestra como "A definir" (TBD). Cuando la cantidad de equipos no es potencia de 2 (por ejemplo 5, 6 o 7 equipos), los mejores puestos de la fase de grupos reciben "bye" (pasan directo a la siguiente ronda) — es un comportamiento esperado, no un error. Si el sistema no puede determinar con certeza qué equipo avanza a la siguiente ronda, muestra las rondas en columnas sin línea de conexión en vez de arriesgar una conexión incorrecta.

### 2.2 Imprimir posiciones

1. Entrar a la vista de **posiciones** de una división.
2. Usar el botón **"Imprimir"**.
3. Se abre el diálogo de impresión del navegador (permite imprimir en papel o guardar como PDF desde ahí); la hoja se genera sin el menú ni otros elementos de navegación, solo la tabla de posiciones.

## 3. Iniciar sesión

1. Hacer clic en "Iniciar sesión" desde la página principal.
2. Ingresar correo electrónico y contraseña.
3. Si es la primera vez que se ingresa con una cuenta creada por un administrador, el sistema puede solicitar cambiar la contraseña antes de continuar.
4. Si se olvidó la contraseña, usar la opción "Recuperar contraseña": el sistema envía un correo con las instrucciones para restablecerla.

Al iniciar sesión correctamente, el sistema habilita el menú del panel administrativo según el rol del usuario.

## 4. Panel administrativo

El menú del panel se organiza en los siguientes grupos:

- **Competición**: Temporadas, Sanciones, Canchas.
- **Gestión de equipos**: Equipos, Jugadores.
- **Novedades** (blog).
- **Usuarios**.
- **Sistema**: Estadísticas, Registro de auditoría, Administración de datos.
- **Configuración**: Cambiar contraseña, Editar perfil.

Los torneos, divisiones, fases y partidos no tienen un ítem propio en el menú: se llega a ellos entrando a una temporada y navegando hacia adentro (Temporada → Torneo → División → Fase/Partido).

### 4.1 Temporadas, torneos y divisiones

1. **Crear una temporada**: agrupa uno o más torneos (por ejemplo, Apertura y Clausura del mismo año).
2. **Crear un torneo** dentro de la temporada: cargar nombre, fecha de inicio y demás datos generales.
3. **Crear divisiones** dentro del torneo (por ejemplo, categorías por edad, nivel o género).
4. **Inscribir equipos** a cada división.
5. **Generar la etapa** del torneo: el sistema arma automáticamente la fase de grupos y, una vez que termina, las llaves de eliminación directa según la cantidad de equipos clasificados (con "bye" a los mejores puestos si esa cantidad no es potencia de 2). Si el torneo define más de una copa (por ejemplo Copa Oro para los primeros puestos y Copa Plata para el resto), el sistema arma cada bracket por separado, tomando la posición de arranque de cada copa como el "1er sembrado" de su propio cuadro.
6. El torneo no puede pasar a **"En curso"** si alguna zona tiene menos de 2 equipos o si algún equipo inscripto tiene menos de 4 jugadores habilitados (ver 4.3) — el sistema avisa qué falta corregir antes de poder arrancar.

### 4.2 Equipos

- Alta de un equipo nuevo: nombre, código de tres letras, color de camiseta, escudo.
- Edición o baja de equipos existentes.
- Búsqueda y filtrado por nombre.
- Cuerpo técnico: cargar el director técnico y demás staff de cada equipo para la temporada.

### 4.3 Jugadores

- Alta de un jugador: datos personales, DNI (único por jugador), equipo al que pertenece.
- Edición o baja de jugadores.
- Búsqueda y filtrado.
- Número de camiseta (dorsal): se asigna por equipo y temporada, no es fijo para el jugador — puede cambiar si pasa a otro equipo o temporada.

**Ficha médica y habilitación**: para que un jugador pueda sumar puntos en un partido tiene que estar **habilitado** para esa temporada:

1. Subir el PDF de la ficha médica del jugador (por equipo y temporada).
2. Un administrador la revisa y la **aprueba** o **rechaza**. Solo queda habilitado cuando está aprobada y tiene un archivo real cargado — una aprobación sin archivo no habilita a nadie.
3. Una vez aprobada, la ficha no se puede volver a subir (queda de solo lectura); si hace falta corregirla, primero hay que rechazarla.
4. La habilitación es específica de esa temporada: un jugador que jugó habilitado el año pasado empieza la temporada nueva sin habilitar, aunque no haya cambiado de equipo.

### 4.4 Partidos

- Los partidos de fase de grupos y de eliminación se generan automáticamente al crear la etapa.
- Cargar el **resultado** de un partido una vez jugado, planilla por planilla: se cargan los puntos de cada jugador de ambos equipos y el resultado final se calcula sumándolos (no se tipea aparte). El sistema actualiza automáticamente la tabla de posiciones y de goleadores.
- Reglas al cargar el resultado:
  - Solo pueden sumar puntos jugadores **habilitados** y sin sanción activa. Si algún jugador cargado no cumple esto, el sistema rechaza el guardado y lista, con nombre y equipo, a todos los jugadores con problema (no solo el primero).
  - Un equipo necesita **al menos 4 jugadores habilitados** para que se le pueda cargar un resultado normal; si no llega a ese mínimo, el partido tiene que cargarse como **walkover** ("Marcar W.O.") en lugar de una planilla común.
  - No se permiten empates: en fase de grupos el desempate en la tabla es PTS → PG (partidos ganados) → DG (diferencia de gol) → resultado entre los propios equipos empatados → DG de esos cruces; en fase eliminatoria, si el partido termina empatado, se juega tiempo suplementario.
- **Walkover**: un administrador puede marcar un partido como walkover indicando qué equipo se presentó — el sistema carga el resultado reglamentario a favor de ese equipo.
- Consultar el estado de cada partido (programado / jugado / walkover).

### 4.5 Estadísticas y goleadores

- Las estadísticas de puntos por jugador se cargan como parte de la planilla del resultado del partido (ver 4.4), no por separado.
- El sistema arma automáticamente la tabla de goleadores del torneo a partir de esta carga.

### 4.6 Sanciones

1. Registrar una sanción a un jugador (tipo, motivo, duración).
2. El jugador o su equipo puede presentar una **apelación** desde el sistema.
3. Un administrador revisa la apelación y la marca como **aceptada** o **rechazada**.
4. Si es aceptada, la sanción se levanta; si es rechazada, la sanción sigue vigente.
5. Un jugador sancionado no puede sumar puntos en un partido mientras la sanción esté activa (ver 4.4).

### 4.7 Canchas (Venues)

- Alta, edición y baja de las canchas/sedes donde se juegan los partidos.
- Al programar un partido, dos partidos en la misma cancha necesitan al menos 2 horas de diferencia entre sí.

### 4.8 Usuarios

- Alta de nuevos usuarios administradores, con asignación de rol.
- Activar o desactivar cuentas existentes.
- Un usuario puede cambiar su propia contraseña o editar su perfil desde **Configuración**.

### 4.9 Novedades (Blog)

- Publicar noticias o crónicas de partidos, visibles en la vista pública.
- Editar o eliminar publicaciones existentes.
- Una publicación puede guardarse como borrador antes de hacerla visible al público.

### 4.10 Sistema

- **Estadísticas**: panel con métricas generales de uso/carga de la liga.
- **Registro de auditoría**: historial de acciones administrativas relevantes (quién hizo qué y cuándo), para trazabilidad.
- **Administración de datos**: herramientas internas de mantenimiento de datos, de uso ocasional por el equipo técnico.

## 5. Copias de seguridad

El sistema incluye un mecanismo de respaldo automático programado de la base de datos. Esta función es de uso interno (no requiere acción del usuario administrador) y se activa desde la configuración del servidor por el equipo técnico responsable del despliegue.

## 6. Resolución de problemas frecuentes

| Situación | Qué hacer |
|---|---|
| No puedo iniciar sesión | Verificar correo y contraseña; usar "Recuperar contraseña" si es necesario. Si la cuenta fue desactivada, contactar a un administrador. |
| No veo la opción para administrar cierta sección | El rol asignado a la cuenta no tiene permiso sobre esa sección; contactar a un administrador para revisar el rol. |
| Un partido no aparece en el fixture | Verificar que la etapa del torneo haya sido generada y que el equipo esté correctamente inscripto en la división. |
| Cargué un resultado y la tabla de posiciones no cambió | Revisar que el resultado se haya guardado correctamente (no quedó en estado pendiente); recargar la página. |
| No puedo cargar el resultado de un partido | Revisar que todos los jugadores cargados estén habilitados (ficha médica aprobada con archivo) y sin sanción activa; si el equipo tiene menos de 4 jugadores habilitados, hay que cargar el partido como walkover en lugar de una planilla común. |
| No puedo iniciar el torneo | Revisar que cada zona tenga al menos 2 equipos y que cada equipo inscripto tenga al menos 4 jugadores habilitados; el sistema indica qué falta corregir. |

## 7. Soporte técnico

Para consultas sobre instalación, configuración del servidor o errores técnicos, ver la documentación técnica en [README.md](./README.md) o contactar al equipo de desarrollo del proyecto.
