# Manual de Usuario — Liga Club12

Guía de uso del sistema para administradores y visitantes. Para información técnica (arquitectura, instalación, requisitos cubiertos) ver [README.md](./README.md).

## 1. Perfiles de usuario

El sistema tiene dos tipos de acceso:

- **Visitante (sin cuenta)**: puede consultar toda la información pública del torneo desde la página principal, sin necesidad de registrarse ni iniciar sesión.
- **Usuario administrador (con cuenta)**: accede al panel privado para cargar y mantener toda la información del torneo. Los roles definen qué secciones puede administrar cada usuario.

## 2. Vista pública (sin iniciar sesión)

Desde la página principal, cualquier visitante puede:

- Ver la lista de **torneos** activos y finalizados.
- Consultar los **equipos** participantes de cada torneo, con su plantel de jugadores.
- Ver el **fixture** y los **resultados** de los partidos jugados y a jugarse.
- Consultar la **tabla de posiciones** de cada división/etapa.
- Ver la **tabla de goleadores**.
- Ver las **llaves de eliminación** ("Llaves") como un árbol de bracket, con los cruces de cada ronda hasta la final.
- **Imprimir** la tabla de posiciones y/o de goleadores de una división.
- Consultar las **sanciones vigentes** aplicadas a jugadores.
- Leer las **novedades** publicadas en el blog del torneo.

No se requiere ninguna acción de registro para acceder a esta información.

### 2.1 Ver las llaves de eliminación

1. Entrar a la página del torneo y seleccionar la división deseada.
2. Ir a la pestaña **"Llaves"** (junto a "Partidos" y "Posiciones").
3. El sistema muestra el árbol de la fase eliminatoria: cuartos, semifinal, tercer puesto y final, con los equipos y el resultado de cada cruce.
4. Si un partido todavía no se jugó, ese cruce se muestra como "A definir" (TBD). Si el sistema no puede determinar con certeza qué equipo avanza a la siguiente ronda, muestra las rondas en columnas sin línea de conexión en vez de arriesgar una conexión incorrecta — esto es un comportamiento esperado, no un error.

### 2.2 Imprimir posiciones o goleadores

1. Entrar a la vista de **posiciones** de una división.
2. Usar el botón **"Imprimir"**.
3. Elegir qué tabla incluir: posiciones, goleadores, o ambas.
4. Se abre el diálogo de impresión del navegador (permite imprimir en papel o guardar como PDF desde ahí); la hoja se genera sin el menú ni otros elementos de navegación, solo la tabla elegida.

## 3. Iniciar sesión

1. Hacer clic en "Iniciar sesión" desde la página principal.
2. Ingresar correo electrónico y contraseña.
3. Si es la primera vez que se ingresa con una cuenta creada por un administrador, el sistema puede solicitar cambiar la contraseña antes de continuar.
4. Si se olvidó la contraseña, usar la opción "Recuperar contraseña": el sistema envía un correo con las instrucciones para restablecerla.

Al iniciar sesión correctamente, el sistema habilita el menú del panel administrativo según el rol del usuario.

## 4. Panel administrativo

### 4.1 Torneos y divisiones

1. **Crear un torneo**: cargar nombre, fecha de inicio y demás datos generales.
2. **Crear divisiones** dentro del torneo (por ejemplo, categorías por edad o nivel).
3. **Inscribir equipos** a cada división.
4. **Generar la etapa** del torneo: el sistema arma automáticamente la fase de grupos y, según la cantidad de equipos inscriptos (8, 16, 32 o 64), las llaves de eliminación directa (cuartos, semifinal, tercer puesto y final).

### 4.2 Equipos

- Alta de un equipo nuevo: nombre, código de tres letras, color de camiseta, escudo.
- Edición o baja de equipos existentes.
- Búsqueda y filtrado por nombre.

### 4.3 Jugadores

- Alta de un jugador: datos personales, DNI (único por jugador), equipo al que pertenece.
- Edición o baja de jugadores.
- Búsqueda y filtrado.

### 4.4 Partidos

- Los partidos de fase de grupos y de eliminación se generan automáticamente al crear la etapa.
- Cargar el **resultado** de un partido una vez jugado; el sistema actualiza automáticamente la tabla de posiciones.
- Consultar el estado de cada partido (pendiente / jugado).

### 4.5 Estadísticas y goleadores

- Cargar las estadísticas de cada jugador en un partido (goles, etc.).
- El sistema arma automáticamente la tabla de goleadores del torneo a partir de esta carga.

### 4.6 Sanciones

1. Registrar una sanción a un jugador (tipo, motivo, duración).
2. El jugador o su equipo puede presentar una **apelación** desde el sistema.
3. Un administrador revisa la apelación y la marca como **aceptada** o **rechazada**.
4. Si es aceptada, la sanción se levanta; si es rechazada, la sanción sigue vigente.

### 4.7 Canchas (Venues)

- Alta, edición y baja de las canchas/sedes donde se juegan los partidos.

### 4.8 Usuarios

- Alta de nuevos usuarios administradores, con asignación de rol.
- Activar o desactivar cuentas existentes.
- Un usuario puede cambiar su propia contraseña desde su perfil.

### 4.9 Blog / Novedades

- Publicar noticias o crónicas de partidos, visibles en la vista pública.
- Editar o eliminar publicaciones existentes.

## 5. Copias de seguridad

El sistema incluye un mecanismo de respaldo automático programado de la base de datos. Esta función es de uso interno (no requiere acción del usuario administrador) y se activa desde la configuración del servidor por el equipo técnico responsable del despliegue.

## 6. Resolución de problemas frecuentes

| Situación | Qué hacer |
|---|---|
| No puedo iniciar sesión | Verificar correo y contraseña; usar "Recuperar contraseña" si es necesario. Si la cuenta fue desactivada, contactar a un administrador. |
| No veo la opción para administrar cierta sección | El rol asignado a la cuenta no tiene permiso sobre esa sección; contactar a un administrador para revisar el rol. |
| Un partido no aparece en el fixture | Verificar que la etapa del torneo haya sido generada y que el equipo esté correctamente inscripto en la división. |
| Cargué un resultado y la tabla de posiciones no cambió | Revisar que el resultado se haya guardado correctamente (no quedó en estado pendiente); recargar la página. |

## 7. Soporte técnico

Para consultas sobre instalación, configuración del servidor o errores técnicos, ver la documentación técnica en [README.md](./README.md) o contactar al equipo de desarrollo del proyecto.
