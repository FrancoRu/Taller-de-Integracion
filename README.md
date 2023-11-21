# Introducción
El proyecto consiste en un sistema web basado en el modelo cliente-servidor. Este sistema está diseñado para ser multiusuario. Además, se implementa el patrón Modelo-Vista-Controlador (MVC) para organizar y estructurar de manera eficiente la lógica de la aplicación, la presentación de datos y el control de la interfaz de usuario.

## Justificación del Proyecto

### Justificación Técnica
Dado que la infraestructura del proyecto es de naturaleza web y relativamente pequeña en cuanto a recursos de almacenamiento y operaciones, se implementará a través de un servidor web de terceros. Esto se debe a que el proyecto no requiere una infraestructura considerable para alojar y servir el sitio web.

### Justificación Económica
- No se requerirá comprar licencias costosas de software, ya que las tecnologías seleccionadas, C# y React, son de código abierto.
- El precio del dominio para "ligaclub12.com.ar" es de $860 (ochocientos sesenta pesos argentinos), sin IVA, al año. (DonWeb 1)
- Para el Hosting que ofrece el plan SINGLE para una sola web al valor de $3.300 (tres mil trescientos pesos argentinos) con 50 GB y un rendimiento estándar.

### Justificación Operativa
Desde el punto de vista de los usuarios, el sistema le ofrecerá una solución más eficiente, segura y versátil en comparación con el uso de planillas de Excel. Se anticipa una aceptación positiva al cambio, ya que el sistema mejorará significativamente la gestión de torneos de baloncesto. Se llevará a cabo una capacitación a los "usuarios administradores del sistema" como también a los "usuarios administradores del torneo", mostrando las funcionalidades que posee cada uno asegurando de que puedan utilizar el sistema de manera efectiva, facilitando así la transición y la adopción del nuevo sistema.

## Conclusión
En la organización ya existe la necesidad de implementar un sistema que les permita realizar de una manera eficiente la gestión del torneo. Además, están de acuerdo en cubrir los costos asociados a la implementación de este nuevo sistema, lo que lo hace una opción viable el desarrollo del mismo.

# Metodología de Desarrollo

Se optó por esta metodología debido a la descomposición del proyecto en módulos, lo que permitirá la distribución de tareas entre el equipo. Aun así, esta metodología será adaptada al proyecto ya que solo se cuenta con un equipo de 3 desarrolladores. Los sprints están definidos para durar 4 semanas cada uno, dando un total de 7 meses. Durante estos sprints, el desarrollo del Frontend y Backend se llevará a cabo de manera simultánea, junto con el desarrollo de la Base de Datos.

Cada Sprint se dividirá en 4 secciones, las cuales serán de documentación de manual, sobre qué debería hacer el software, implementación, testing y por último la generación de documentación técnica, con información del funcionamiento interno del módulo finalizado. A excepción del sprint 7 que se enfocará exclusivamente en actividades de integración, despliegue y documentación del manual.

## Sprint 1 | 27/11/2023 - 24/12/2023

### Frontend
- **Diseño de Interfaz:** Durante esta etapa, se llevará a cabo la definición de la apariencia visual y la estructura general de la interfaz de usuario del sistema. Esto comprenderá la creación del esquema de la página, la elaboración de prototipos y la selección de paleta de colores y tipografía.

### Backend
- **Definición de Entidades:** En esta fase, se establecerán las entidades y modelos de datos que representarán las diferentes estructuras de información del sistema.
- **Gestión de Base de Datos - Creación:** Implementación y configuración de las tablas necesarias para asegurar el correcto funcionamiento del sitio web.

## Sprint 2 | 08/01/2024 - 04/02/2024

### Frontend
- **Desarrollo de Componentes en React:** Se crearán componentes reutilizables en React para integrar la interfaz de usuario.

### Backend
- **Implementación de Medidas de Seguridad:** Establecimiento de medidas de seguridad para prevenir la inyección de SQL.
- **Creación de la Base de Datos - Seguridad:** Desarrollo de vistas que simplificarán el acceso a datos específicos y consultas complejas, mejorando la interacción con la base de datos.

## Sprint 3 | 05/02/2024 - 03/03/2024

### Frontend
- **Rutas de Navegación:** Implementación de las rutas que permiten la navegación entre las diversas vistas de la aplicación.

### Backend
- **Configuración de API:** Configuración de la información en la API, incluyendo la gestión de entornos y bases de datos.
- **Gestión de Usuarios:** Desarrollo de funciones relacionadas con la gestión de usuarios y visualización de perfiles de usuario.

## Sprint 4 | 04/03/2024 - 31/03/2024

### Frontend
- **Persistencia de Contexto:** Configuración del contexto y el estado global de la aplicación para gestionar la información compartida entre distintos componentes de manera eficiente.

### Backend
- **Gestión de Jugadores:** Desarrollo de funciones relacionadas con la gestión de jugadores, incluyendo operaciones de alta, baja y modificación de la información de los jugadores.
- **Gestión de Equipos:** Implementación de funciones destinadas a la gestión de equipos, comprendiendo operaciones de alta, baja y modificación de datos relacionados con los equipos.

## Sprint 5 | 01/04/2024 - 28/04/2024

### Frontend
- **Servicios de Comunicación:** Desarrollo de los servicios que permitan la comunicación con la API. Implementación de funcionalidades para la gestión de información compartida entre diferentes componentes del sistema.

### Backend
- **Gestión de Estadísticas:** Desarrollo de funciones relacionadas con la gestión de estadísticas. Operaciones de alta, baja y modificación de la información estadística.
- **Gestión de Sanciones:** Desarrollo de funciones relacionadas con la gestión de sanciones, abarcando operaciones de alta, baja y modificación de la información de sanciones en el sistema.

## Sprint 6 | 29/04/2024 - 26/05/2024

### Backend
- **Gestión de Partidos:** Implementación de funciones relacionadas con la administración de partidos. Operaciones de alta, baja y modificación de información relacionada con los partidos.
- **Gestión de Torneos:** Desarrollo de funciones relacionadas con la administración de torneos, comprendiendo operaciones de alta, baja y modificación de datos relacionados con los torneos.
- **Integración:** Integración de los módulos desarrollados hasta el sprint anterior. Despliegue parcial de la aplicación consolidando las funcionalidades implementadas hasta el momento.

## Sprint 7 | 27/05/2024 - 23/06/2024

### Integración
- **Integración Total:** Integración completa de todos los módulos desarrollados, permitiendo desplegar el sistema en su totalidad.

### Testing
- Pruebas unitarias e integración para verificar el correcto funcionamiento del módulo desarrollado.
- Generación de documentación detallada con informe completo de los resultados obtenidos durante las pruebas.

### Documentación
- **Documentación de manual:** Instrucciones detalladas, consejos y ejemplos para que los usuarios comprendan y utilicen el software eficientemente.
- **Documentación técnica:** Visión detallada y técnica de todo el sistema. Arquitectura, diseño, interfaces de programación, estructuras de datos y controladores.
