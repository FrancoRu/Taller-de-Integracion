export type RegulationBlock =
  | { kind: 'section'; text: string }
  | { kind: 'article'; label: string; text: string }
  | { kind: 'list'; items: string[] };

export const regulationIntro =
  'Los torneos consisten en la realización, armonización, confraternización desinteresada y leal, y principalmente el acercamiento entre quienes integran distintos equipos y por consiguiente sabedores de las responsabilidades y actitudes que van a desarrollar hechos que deberán tener en consideración quienes pretendan adoptar posturas antideportivas e incitaciones provocativas que tiendan a engendrar la violencia dentro y/o fuera del campo deportivo los que serán severamente sancionados para sostener incólumes los principios fundamentales de quienes fueron los hacedores de esta liga y mantener en vigencia el contenido esencial de una amistad cosechada y duradera, mas una continuidad deportiva sana. Así mismo se tendrá en cuenta las conformaciones de cada equipo a los efectos de llegar a eliminar, si es necesario, de las listas de buena fe aquellos considerados elementos provocativos, de acuerdo a los antecedentes obrantes en esta liga.';

export const regulationBlocks: RegulationBlock[] = [
  { kind: 'section', text: 'REGLAMENTO GENERAL' },

  { kind: 'section', text: 'AFILIACIONES' },
  {
    kind: 'article',
    label: 'Art. 1',
    text: 'En cualquier época del año una agrupación y/o equipo puede solicitar su afiliación al Torneo de Básquet. Para que dicha afiliación acuerde derecho a inscribir un equipo en el torneo, esta debe ser presentada de forma correcta en base a lo expuesto en el Artículo 3 quedando a consideración de los organizadores si es viable la incorporación.',
  },
  {
    kind: 'article',
    label: 'Art. 2',
    text: 'La solicitud de inscripción debe ser presentada por escrito y ser rubricada por las autoridades de la agrupación y/o equipo, indicando en la misma:',
  },
  {
    kind: 'list',
    items: [
      'El nombre de la agrupación y/o equipo.',
      'Apellido y nombre de los delegados de la agrupación y/o equipo (como mínimo dos delegados).',
      'Apellido y nombre del director técnico.',
    ],
  },

  { kind: 'section', text: 'INSCRIPCIONES' },
  {
    kind: 'article',
    label: 'Art. 3',
    text: 'La inscripción de equipos deberá hacerse al comienzo de cada torneo de Club 12 "La Vuelta" y en caso de nuevos equipos de acuerdo al Art. 1 adjuntando a la misma:',
  },
  {
    kind: 'list',
    items: [
      'Una nómina de siete (7) jugadores de mínima y catorce (14) de máxima, con apellido, nombres, tipos y número de documento y fecha de nacimiento.',
      'Fotocopia de 1º, 2º hoja de los documentos de identidad de cada integrante de la agrupación y/o equipo.',
      'Adjuntar arancel correspondiente según lo dispuesto por los organizadores del torneo.',
      'Certificado de Aptitud Física.',
      'Foto carnet 4 x 4.',
    ],
  },

  { kind: 'section', text: 'DE LAS AGRUPACIONES Y/O EQUIPOS' },
  {
    kind: 'article',
    label: 'Art. 4',
    text: 'Función de los Delegados: hacer conocer a todos los jugadores de su equipo el presente reglamento. Serán designados dos por agrupación y/o equipo y son los responsables ante los organizadores del Torneo de Básquet del accionar de las agrupaciones y/o equipos, debiendo concurrir obligatoriamente cuando sean citados. Función del Director Técnico (DT): es responsable por el ingreso de los jugadores en el campo de juego, cambio de jugadores y banco de suplentes. Los delegados y el DT podrán, aparte de sus funciones específicas, integrar sus respectivos equipos como jugadores.',
  },
  {
    kind: 'article',
    label: 'Art. 5',
    text: 'Las agrupaciones y/o equipos y sus autoridades están obligadas a cumplir las disposiciones que emanen de los organizadores del Torneo de Básquet, Reglamento General y Código de penas, y respetar y acatar las resoluciones de sus autoridades, absteniéndose de protestar públicamente, formular declaraciones o quejas ante las autoridades contra tales resoluciones. Si las juzgaran injustas podrán interponer por escrito los argumentos ante los organizadores del Torneo de Básquet.',
  },
  {
    kind: 'article',
    label: 'Art. 6',
    text: 'Las agrupaciones y/o equipos no podrán alegar ignorancia con respecto a las resoluciones que se hagan conocer por medio de este reglamento y sus actualizaciones, las cuales serán publicadas en las respectivas redes sociales (página oficial de la liga, Facebook y Twitter) como así también a los delegados de cada equipo en las reuniones. Las agrupaciones y/o equipos podrán presentar dentro de la cancha 3 jugadores federados y 5 en la lista de buena fe (en actividad); el resto se completará con jugadores de 20 años en adelante. Los jugadores que estén federados pero no se encuentren en actividad no serán tenidos en cuenta como federados y podrán ser parte de la agrupación y/o equipo de manera normal. Si se constata que se está jugando con 4 federados en cancha, el árbitro sancionará con una falta técnica al banco. El equipo que presente más jugadores que lo reglamentado (dentro de la cancha) perderá el partido en disputa otorgándosele los puntos al otro equipo; en caso de reincidir será expulsado del campeonato. El equipo que saque una ventaja antirreglamentaria (ganar el partido), ya sea por incluir mal un jugador, jugar con más de 3 federados en cancha, jugar con un jugador sancionado, etc., perderá los puntos del partido, además de que su delegado será sancionado con una fecha de suspensión.',
  },
  {
    kind: 'article',
    label: 'Art. 7',
    text: 'La agrupación y/o equipo que no se presentara a disputar dos partidos en el torneo será eliminado de la Liga sin perjuicio de las medidas punitivas y arancelarias que le correspondieran. Toda agrupación y/o equipo que incluya un jugador sin estar habilitado o con documentación falsa será sancionado con 1 partido al jugador; en caso de reincidencia perderá el partido en disputa sancionándose también al director técnico, y en caso de una tercera reincidencia, al equipo completo.',
  },
  {
    kind: 'article',
    label: 'Art. 8',
    text: 'Los partidos se jugarán con las leyes del organismo internacional del Básquet (FIBA), AAB y Liga Paranaense, por las reglas que estos reglamentos determinen, no así la forma de clasificación. En caso de igualdad de puntos entre dos equipos en alguna de las posiciones de cualquier zona e instancia del torneo, se tomará el partido jugado entre sí; si la igualdad fuera de tres o más equipos, se determinará por diferencia de puntos, y en caso de haber dos equipos con igual diferencia se tomará el partido jugado entre sí. En la etapa de Playoffs, cualquier equipo que no se presente a jugar quedará eliminado directamente por default, más allá de haber ganado el 1er encuentro por más de 20 puntos. En partidos de vuelta, la diferencia de puntos del 1er partido estará colocada en el tablero.',
  },
  {
    kind: 'article',
    label: 'Art. 9',
    text: 'Es facultad de los organizadores de la Liga de Básquet fijar la hora de inicio de los partidos de los torneos que organice, así como la modificación de los mismos cuando lo estime conveniente. Desde la hora fijada para el inicio del primer partido habrá tolerancia de 15 minutos; cuando uno o ambos equipos no se presentaren en el campo de juego, el árbitro dará por finalizado el encuentro, siendo la pena la pérdida de puntos. En caso de no presentación de un equipo al momento del partido, el resultado será veinte (20) a cero (0) a favor del adversario, haciéndose cargo el equipo infractor de una multa equivalente a la mitad del partido jugado, además de no sumar 1 (un) punto, como partido perdido. Para que cada equipo pueda presentarse en el campo de juego y disputar el partido deberá tener un mínimo de cuatro (4) jugadores dentro del campo de juego; si para el comienzo del segundo tiempo no se han completado los cinco (5) jugadores en cancha, el partido será suspendido quedando el marcador como terminó el primer tiempo (si este no supera los 20 puntos, el resultado final será 20 a 0). Será estricto el horario dispuesto para el inicio de los partidos, no habrá tolerancia bajo ningún concepto. En caso de que ningún equipo se presente a jugar su partido, ambos deberán abonar la multa correspondiente, el partido se dará por jugado y ninguno de los equipos sumará puntos.',
  },
  {
    kind: 'article',
    label: 'Art. 10',
    text: 'Quince minutos antes de la hora designada por el fixture para el inicio del partido deberán presentarse los jugadores con los correspondientes carnets, siendo un mínimo de cuatro (4) y un máximo de catorce (14), conforme a lo determinado por este reglamento; el equipo que no lo hiciera perderá el partido que debía disputarse.',
  },
  {
    kind: 'article',
    label: 'Art. 11',
    text: 'Los partidos tendrán una duración de cuarenta (40) minutos divididos en cuatro (4) cuartos de diez (10) minutos cada uno, donde no se parará el reloj en ningún momento salvo en el último minuto del 1er, 2do y 3er cuarto, mientras que en el último cuarto se para en los últimos cinco (5) minutos. Si por causas ajenas a las respectivas agrupaciones y/o equipos se suspendiera un partido y fuera responsabilidad de los organizadores de la Liga Club 12 "La Vuelta", el encuentro se jugará cuando lo decidan los organizadores de dicha liga, informando a través de los delegados y las diferentes vías de comunicación la nueva fecha, hora y lugar del encuentro.',
  },
  {
    kind: 'article',
    label: 'Art. 12',
    text: 'Si por causas ajenas a las respectivas agrupaciones y/o equipos se suspendiera un partido una vez empezado, subsistirá el resultado que se hubiera producido y se jugará en otra fecha el tiempo que faltare para su terminación, cuando así lo dispongan los organizadores de la Liga Club 12 "La Vuelta"; pero en caso de que faltaren menos de diez minutos para finalizar el encuentro y la diferencia sea mayor a 20 puntos, el mismo se dará por finalizado.',
  },
  {
    kind: 'article',
    label: 'Art. 13',
    text: 'Si la suspensión de un partido resultare imputable a una de las agrupaciones y/o equipos actuantes, ya sea por acción de sus jugadores, directivos, público, etc., se declarará terminado y perderá los puntos el equipo responsable, sin perjuicio de las otras medidas que pueden alcanzar a los responsables y que determinará el tribunal de penas o quien lo reemplace de acuerdo al reglamento.',
  },
  {
    kind: 'article',
    label: 'Art. 14',
    text: 'Si la suspensión de un partido resultare imputable a las dos agrupaciones y/o equipos actuantes, ya sea por acción de sus jugadores, directivos, público, etc., se declarará terminado y perderán los puntos ambos equipos, sin perjuicio de las otras medidas que pueden alcanzar a los responsables y que determinará el tribunal de penas o quien lo reemplace de acuerdo al reglamento.',
  },
  {
    kind: 'article',
    label: 'Art. 15',
    text: 'Cuando se complete el tiempo reglamentario de un partido suspendido del cual el referee hubiera expulsado uno o más jugadores, los equipos deberán continuar con el mismo número de jugadores que tenían en la cancha al momento de la suspensión, ya sea los que actuaron u otros. Los jugadores expulsados no podrán integrar el equipo aunque hayan cumplido la pena.',
  },
  {
    kind: 'article',
    label: 'Art. 16',
    text: 'Para suspender un partido deberá solicitarse por escrito con cinco (5) días de anticipación, debiendo hacerse cargo de los gastos que demande el equipo que lo solicite y debiendo abonar dichos gastos al momento de presentar la solicitud, quedando supeditado a la decisión que tomen los miembros organizadores de la Liga Club 12 "La Vuelta". Si una agrupación abandona el torneo una vez comenzado: los partidos que ya disputó esa agrupación quedarán tal cual el resultado final; los partidos que deba disputar esa agrupación se le darán por ganados al equipo rival por un tanteador de 20 a 0 a favor, abonando el equipo ganador la suma total del partido, dado que de igual manera el partido se hubiese disputado si el equipo contrario no abandonaba el torneo.',
  },

  { kind: 'section', text: 'DE LOS JUGADORES' },
  {
    kind: 'article',
    label: 'Art. 17',
    text: 'Los organizadores de la Liga Club 12 "La Vuelta" tendrán un registro en el que deberán inscribirse todos los jugadores, debiendo para ello estar físicamente habilitados para la práctica de básquet, haciéndose responsables las agrupaciones y/o equipos de los accidentes que pudieran ocasionarse durante la disputa de la Liga. Deberán cumplir previamente las siguientes exigencias:',
  },
  {
    kind: 'list',
    items: [
      'Concurrir voluntariamente al lugar y horario que se determine para realizar las reuniones.',
      'Deberán presentar carnets emitidos por los organizadores de la Liga Club 12 "La Vuelta".',
      'Durante la Fase Regular, el/los jugadores que no tengan su carnet al momento de disputar el partido podrán presentar el DNI, licencia de conducir o alguna identificación que contenga el nombre completo y su foto. Durante las etapas de definición deberán presentar el carnet de Club 12 únicamente; el jugador que no lo hiciera no podrá disputar el partido.',
      'Manifestar con claridad la agrupación y/o equipo para la que desean jugar.',
      'Es de carácter obligatorio estar asegurado en la compañía que los organizadores designen.',
    ],
  },
  {
    kind: 'article',
    label: 'Art. 18',
    text: 'Todo jugador que haya registrado su firma en la Liga Club 12 "La Vuelta" no podrá alegar que hubo error y será jugador de la agrupación y/o equipo por la cual haya declarado querer actuar, no pudiendo hacerlo por otro sin antes llenar los requisitos del pedido de pase a otra agrupación y/o equipo. Los organizadores tendrán en cuenta a los jugadores registrados en las conformaciones de las listas de buena fe de cada agrupación, eliminando a aquellos que se consideren elementos provocativos, de acuerdo a los antecedentes y conceptos formados obrantes por los organizadores; esto es para evitar problemas inadmisibles, como los suscitados en años anteriores con actitudes antideportivas, violencia, gresca, agresiones verbales y/o de hecho, etc.',
  },
  {
    kind: 'article',
    label: 'Art. 19',
    text: 'Todo jugador que hubiere actuado por una agrupación y/o equipo en un año deportivo y quisiera actuar en otro año deportivo por otra agrupación podrá pedir pase, no pudiendo la agrupación negar el mismo siempre y cuando el jugador estuviera "libre de deuda". Todo jugador que acumule tres faltas antideportivas y/o 4 faltas técnicas será sancionado con una fecha de suspensión; la reiteración de esta sanción sumará una fecha más de suspensión. A la lista de buena fe podrán integrarla hasta 5 (cinco) jugadores que hayan participado o participen en planteles de primera división durante el año en curso, pudiendo estar solo 3 en cancha sin excepción. Se podrán realizar cambios en la lista de buena fe hasta la tercera fecha; luego, sin excepción, solo cuando un jugador sea dado de baja por lesión, presentando el certificado médico que demuestre que no puede continuar durante el resto del torneo. Los jugadores profesionales que hayan jugado o estén jugando en ligas profesionales (Liga A, TNA y Torneo Federal) podrán jugar en Club 12 bajo las mismas condiciones administrativas que el resto de los jugadores, siendo federados estén o no en actividad; al momento de disputar los Playoffs deben tener disputados el 50% de los partidos de la fase regular, caso contrario no podrán disputar dicha fase.',
  },
  {
    kind: 'article',
    label: 'Art. 20',
    text: 'Todo jugador que en la lista de buena fe presentada por la agrupación no figurase en ella será declarado libre, teniendo hasta quince días corridos después de iniciado el campeonato para inscribirse en otra agrupación que tuviera cupo. Todo jugador que haya integrado una agrupación y esta adeude dinero a los organizadores de la Liga tendrá que pagar el monto de la deuda que los organizadores determinen, más los daños y/o perjuicios económicos que esta hubiera ocasionado.',
  },
  {
    kind: 'article',
    label: 'Art. 21',
    text: 'En los partidos oficiales es obligatorio el uso del correspondiente uniforme; la camisa o camiseta deberá llevar el número correspondiente en forma bien visible, la vestimenta se completará con pantalón de básquet no permitiéndose malla o short de baño, y calzados de básquet adecuados, no pudiendo jugar de alpargatas, ojotas, zapatos o algún calzado similar.',
  },
  {
    kind: 'article',
    label: 'Art. 22',
    text: 'Los capitanes de equipo, independientemente de los derechos y deberes que le corresponden según los reglamentos del juego y disposiciones pertinentes de este reglamento, están autorizados a presentar nota de las observaciones que estimen necesarias y que servirán de constancia para trámites posteriores; tienen además el deber de velar que los jugadores cumplan con todas las obligaciones, debiendo observar severamente toda infracción, bajo pena de ser personalmente responsables. Está prohibido a los jugadores:',
  },
  {
    kind: 'list',
    items: [
      'Agredir a algún miembro de otra agrupación con insultos, amenazas y/o agresión física, siendo la sanción correspondiente fechas de suspensión y hasta la expulsión permanente de esta Liga.',
      'Protestar con palabras o ademanes o realizar gestos, ofensas o agresiones contra los jueces, veedor, mesa de control y/o miembro organizador de la Liga, antes, durante o después de los partidos.',
      'Realizar gestos obscenos, palabras incorrectas, ofender al público, autoridades o miembros de otras agrupaciones y/o equipos.',
      'Realizar cualquier acto que atente contra la moral, buenas costumbres y el Fair Play.',
      'Ingerir bebidas alcohólicas antes y durante el partido.',
      'Agredir al árbitro con insultos, amenazas y/o agresión física, siendo la sanción correspondiente fechas de suspensión y hasta la expulsión permanente de esta Liga.',
      'Participar de un partido estando suspendido; el castigo a esta infracción será una nueva suspensión.',
      'Solicitar los informes a la mesa de control (son reservados y de exclusiva competencia de los organizadores y del tribunal de disciplina).',
    ],
  },
  {
    kind: 'article',
    label: 'Art. 22 bis',
    text: 'Club 12 "La Vuelta" se reserva el derecho de admisión a todo jugador que protagonice o participe en cualquier hecho de violencia en sus respectivos clubes dentro del ámbito local y/o provincial. El alcance de este artículo va desde sanciones hasta expulsión de la liga.',
  },

  { kind: 'section', text: 'SUSTITUCIÓN DE JUGADORES' },
  {
    kind: 'article',
    label: 'Art. 23',
    text: 'En encuentros de certámenes oficiales se podrán hacer tantas sustituciones como jugadores suplentes haya en el banco.',
  },
  {
    kind: 'article',
    label: 'Art. 24',
    text: 'Se permitirá en cualquier momento antes de finalizar el encuentro registrar jugadores en la planilla y participar del partido.',
  },
  {
    kind: 'article',
    label: 'Art. 25',
    text: 'El equipo que antes de haber comenzado el partido no haya presentado la totalidad de los carnets de los jugadores registrados en la planilla perderá puntos. La presentación de los mencionados carnets se debe hacer de forma personal.',
  },

  { kind: 'section', text: 'ACTA Y REUNIONES' },
  {
    kind: 'article',
    label: 'Art. 26',
    text: 'Los organizadores de la Liga Club 12 "La Vuelta" se reunirán junto con los delegados cuando estos lo crean conveniente para poder evacuar dudas, consultas, pago de aranceles, etc. Aquellas agrupaciones que deseen presentar alguna inquietud deberán comunicarse con los organizadores de la liga para poder reunirse en otro día y horario. Cuando la organización de la liga lo considere necesario citará a los delegados a reunión a realizarse en fecha y horario que la misma establezca, siendo la asistencia de carácter obligatorio; en caso de ausencia de ambos delegados no podrán efectuar reclamo alguno de lo citado en la reunión. Cuando los delegados requieran una reunión extraordinaria con los organizadores, deberán presentar la solicitud por escrito y la firma de la mitad más uno de las agrupaciones participantes. Las informaciones estarán a disposición de los delegados una vez que lo pidan a los organizadores y estos lo crean conveniente.',
  },
  {
    kind: 'article',
    label: 'Art. 27',
    text: 'Forma de disputa: el formato de cada torneo (fase de grupos, playoffs, u otra modalidad) será establecido por los organizadores de la Liga Club 12 "La Vuelta" al momento de su inscripción. En caso de igualdad en alguna de las posiciones prevalecerá el partido disputado entre ellos, resultando ganador el que acceda a la mejor posición.',
  },
  {
    kind: 'article',
    label: 'Art. 28',
    text: 'Las jornadas que se adelanten por feriados posteriores a los días habituales (domingos) y no existiendo posibilidad de reunirse el tribunal de disciplina, el jugador que fuera expulsado o tenga que cumplir con alguna sanción quedará automáticamente suspendido para el siguiente cotejo.',
  },

  { kind: 'section', text: 'PLANILLA Y VEEDOR' },
  {
    kind: 'article',
    label: 'Art. 29',
    text: 'La mesa de control y el veedor presentarán, en forma separada, ante los organizadores de la Liga, exclusivamente, un informe con respecto a lo acontecido en la jornada, haciendo hincapié en todo hecho anormal que se produzca, ya sea por comportamiento de los jugadores, simpatizantes, y/o cualquier hecho que consideren necesario informar. Bajo ningún punto de vista el informe del veedor y/o mesa de control podrá modificar los fallos de los jueces.',
  },
  {
    kind: 'article',
    label: 'Art. 30',
    text: 'Una vez finalizada la jornada el árbitro presentará ante los organizadores de la Liga Club 12 "La Vuelta" un informe de los hechos ocurridos en el desarrollo de los partidos.',
  },
  {
    kind: 'article',
    label: 'Art. 31',
    text: 'El jugador expulsado por el árbitro podrá presentar un descargo personal ante el Tribunal de Disciplina.',
  },
  {
    kind: 'article',
    label: 'Art. 32',
    text: 'El jugador informado por los jueces, veedor, mesa de control u organizadores de la Liga será publicado en los sitios oficiales de la liga y sancionado; podrá presentar un descargo personal ante los organizadores y luego estos determinarán junto con el tribunal de disciplina la sanción correspondiente. El escrito podrá presentarse hasta el día miércoles siguiente a la publicación; vencido dicho plazo caducará todo derecho a presentar el mismo y el Tribunal de Disciplina dará a conocer en forma definitiva la sanción impuesta.',
  },

  { kind: 'section', text: 'PROTESTAS' },
  {
    kind: 'article',
    label: 'Art. 33',
    text: 'Se considera protesta toda solicitud formal escrita de agrupación y/o equipo que tienda a modificar el resultado de un partido en que hubiera intervenido.',
  },
  {
    kind: 'article',
    label: 'Art. 34',
    text: 'El escrito deberá presentarse hasta el segundo día hábil siguiente al que se jugó el partido. Vencido dicho plazo caducará todo derecho a presentar el mismo.',
  },
  {
    kind: 'article',
    label: 'Art. 35',
    text: 'El escrito de protesta deberá contener:',
  },
  {
    kind: 'list',
    items: [
      'La explicación clara y precisa de los motivos en que se fundamente.',
      'La petición en términos precisos, debiendo constar la trasgresión reglamentaria en que se funda, indicando la norma que ha sido incumplida por el adversario.',
      'Las firmas del delegado titular o suplente de la agrupación.',
    ],
  },
  {
    kind: 'article',
    label: 'Art. 36',
    text: 'Todas las solicitudes de protesta deberán estar acompañadas de una fotocopia, la cual será entregada, quedando otra a disposición de la agrupación y/o equipo que la presente.',
  },
  {
    kind: 'article',
    label: 'Art. 37',
    text: 'Las resoluciones del árbitro, en lo que se refiere al juego, no podrán ser causa de protestas.',
  },
  {
    kind: 'article',
    label: 'Art. 38',
    text: 'La presentación que reúna los requisitos exigidos será considerada por los organizadores del Torneo de Básquet Club 12 "La Vuelta", quienes trasladarán por el término de tres días corridos vista a la agrupación y/o equipo, bajo apercibimiento de que si dentro del plazo mencionado no fuera evacuada la vista corrida se considerará decaído el derecho de apelar. Recibida la respuesta o no a la vista concedida, producirá despacho y dictamen.',
  },

  { kind: 'section', text: 'PREMIOS' },
  {
    kind: 'article',
    label: 'Art. 39',
    text: 'Los organizadores de la Liga Club 12 "La Vuelta" establecerán los premios a otorgarse en el presente torneo, los que serán comunicados a las agrupaciones y/o equipos al inicio de cada temporada.',
  },

  { kind: 'section', text: 'ARANCELES' },
  {
    kind: 'article',
    label: 'Art. 40',
    text: 'Deberá abonarse en concepto de arancel el importe que oportunamente determinen los organizadores de la Liga Club 12 "La Vuelta". El no cumplimiento de los pagos correspondientes dará lugar a las sanciones que al efecto determinen los organizadores.',
  },
  {
    kind: 'article',
    label: 'Art. 40 bis',
    text: 'Cada agrupación y/o equipo deberá abonar una inscripción correspondiente a la estipulada por la organización de Club 12 por equipo, por única vez al comienzo de cada torneo, pudiéndose abonar hasta el primer mes de torneo; de lo contrario el equipo podrá quedar suspendido de algún partido con pérdida de puntos hasta que paguen lo adeudado.',
  },
  {
    kind: 'article',
    label: 'Art. 41',
    text: 'El arancel será abonado en los plazos que determinen los organizadores. El pago fuera de término producirá automáticamente la suspensión del equipo y la consecuente pérdida de puntos si así lo determinan los organizadores del torneo.',
  },
  {
    kind: 'article',
    label: 'Art. 42',
    text: 'Bajo ningún concepto se devolverá el dinero abonado a las agrupaciones y/o equipos que ya se hayan comprometido a jugar y a los cuales se los haya fixturado.',
  },
  {
    kind: 'article',
    label: 'Art. 43',
    text: 'La organización del Torneo se reserva el derecho de reglamentar todo lo que no esté contemplado en este reglamento.',
  },
  {
    kind: 'article',
    label: 'Art. 44',
    text: 'En caso de surgir anomalías antes de iniciarse el partido, por cualquier causa de las contempladas en el presente reglamento, el árbitro, a solicitud del delegado del equipo supuestamente perjudicado, deberá dejar constancia en la planilla del motivo invocado y que el mismo tiene carácter de protesta, con lo cual se cerrará la planilla, la que será firmada por el planillero, el árbitro y el delegado. En caso de que ambos equipos, a través de sus delegados y con la autorización del árbitro, decidieran jugar el partido sin dejar constancia en la planilla, quedará firme el resultado del partido y el equipo supuestamente perjudicado perderá cualquier derecho a la protesta.',
  },
  {
    kind: 'article',
    label: 'Art. 45',
    text: 'Ante la situación de que al momento de iniciarse el encuentro los dos equipos tengan vestimentas de colores similares, el árbitro llamará a los delegados y sorteará quién debe cambiar de camiseta, ya que todas las agrupaciones deben tener el correspondiente juego alternativo.',
  },
  {
    kind: 'article',
    label: 'Art. 46',
    text: 'Se deja habilitado el pase de un jugador de un equipo a otro entre un torneo y otro una vez finalizado, o durante el mismo en el caso de que no haya disputado ningún encuentro con dicho equipo. El mismo se hará con el consentimiento del equipo anterior (dándolo de baja).',
  },
  {
    kind: 'article',
    label: 'Art. 47',
    text: 'Queda habilitada la incorporación después del comienzo del Torneo en reemplazo de un jugador lesionado (lesión temporaria comprobable). A partir de la 3ra fecha de comenzado el torneo, no se podrán hacer más sustituciones en los equipos a menos que sea por un jugador lesionado como lo indica este artículo.',
  },
  {
    kind: 'article',
    label: 'Art. 48',
    text: 'Toda agrupación que transgreda la reglamentación de este torneo, haciendo jugar a personas que no integran la lista de buena fe (falsificando firma y carnet), será expulsada del torneo.',
  },
  {
    kind: 'article',
    label: 'Art. 49',
    text: 'Los jugadores que hayan integrado agrupaciones que fueron expulsadas del torneo no podrán integrar otra agrupación durante el transcurso del mismo, pudiendo elevar el pedido de reincorporación para el torneo del año siguiente, la que quedará a criterio de los organizadores.',
  },
  {
    kind: 'article',
    label: 'Art. 50',
    text: 'Las agrupaciones y/o equipos no podrán alegar ignorancia con respecto a las resoluciones que se hagan conocer por medio de los sitios oficiales, disponible semanalmente.',
  },
  {
    kind: 'article',
    label: 'Art. 51',
    text: 'Jugador que pierda o rompa su carnet correspondiente a la liga deberá abonar el arancel que determinen los organizadores para la realización de uno nuevo.',
  },
];
