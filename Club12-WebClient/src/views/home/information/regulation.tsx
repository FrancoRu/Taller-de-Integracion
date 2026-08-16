import React from 'react';
import {
  Container,
  Typography,
  Box,
  Paper,
  List,
  ListItem,
  ListItemText,
} from '@mui/material';

const Regulation: React.FC = () => {
  return (
    <Container maxWidth="lg">
      <Box
        sx={{
          display: 'flex',
          flexDirection: 'column',
          alignItems: 'center',
          mt: 4,
        }}
      >
        <Paper elevation={3} sx={{ p: 4, width: '100%' }}>
          <Typography
            variant="h2"
            component="h1"
            gutterBottom
            sx={{ fontWeight: 'bold' }}
          >
            Reglamento
          </Typography>
          <Typography variant="body1" component="div" sx={{ mt: 2 }}>
            <blockquote>
              <Typography paragraph>
                <strong>
                  <u>Liga de Básquet Libre Club 12 "La Vuelta"</u>
                </strong>
              </Typography>
            </blockquote>
            <Typography paragraph>
              <strong>
                <em>
                  <u>REGLAMENTO</u>
                </em>
              </strong>
            </Typography>
            <Typography paragraph>
              Los torneos consisten en la realización, armonización,
              confraternización desinteresada y leal, y principalmente el
              acercamiento entre quienes integran distintos equipos y por
              consiguiente sabedores de las responsabilidades y actitudes que
              van a desarrollar hechos que deberán tener en consideración
              quienes pretendan adoptar posturas antideportivas e incitaciones
              provocativas que tiendan a engendrar la violencia dentro y/o fuera
              del campo deportivo los que serán severamente sancionados para
              sostener incólumes los principios fundamentales de quienes fueron
              los hacedores de esta liga y mantener en vigencia el contenido
              esencial de una amistad cosechada y duradera, mas una continuidad
              deportiva sana. Así mismo se tendrá en cuenta las conformaciones
              de cada equipo a los efectos de llegar a eliminar, si es
              necesario, de las listas de buena fe aquellos considerados
              elementos provocativos, de acuerdo a los antecedentes obrantes en
              esta liga.
            </Typography>

            <Typography variant="h5" component="h2" gutterBottom>
              <u>REGLAMENTO GENERAL</u>
            </Typography>

            <Typography variant="h5" component="h2" gutterBottom>
              <u>AFILIACIONES</u>
            </Typography>
            <Typography paragraph>
              <strong>Art.1:</strong> En cualquier época del año una agrupación
              y/o equipo puede solicitar su afiliación al Torneo de Básquet.
              Para que dicha afiliación acuerde derecho a inscribir un equipo en
              el torneo, esta debe ser presentada de forma correcta en base a lo
              expuesto en el Artículo 3 quedando a consideración de los
              organizadores si es viable la incorporación.
            </Typography>
            <Typography paragraph>
              <strong>Art.2:</strong> La solicitud de inscripción debe ser
              presentada por escrito y ser rubricada por las autoridades de la
              agrupación y/o equipo, indicando en la misma:
            </Typography>
            <List>
              <ListItem>
                <ListItemText primary="a) El nombre de la agrupación y/o equipo." />
              </ListItem>
              <ListItem>
                <ListItemText primary="b) Apellido y nombre de los delegados de la agrupación y/o equipo (como mínimo dos delegados)" />
              </ListItem>
              <ListItem>
                <ListItemText primary="c) Apellido y nombre del director técnico." />
              </ListItem>
            </List>

            <Typography variant="h5" component="h2" gutterBottom>
              <u>INSCRIPCIONES</u>
            </Typography>
            <Typography paragraph>
              <strong>Art.3:</strong> La inscripción de equipos deberá hacerse
              al comienzo de cada torneo de Club 12 "La Vuelta" y en caso de
              nuevos equipos de acuerdo al Art.1 adjuntando a la misma:
            </Typography>
            <List>
              <ListItem>
                <ListItemText primary="a) Una nómina de siete (7) jugadores de mínima y catorce (14) de máxima, con apellido, nombres, tipos y número de documento y fecha de nacimiento." />
              </ListItem>
              <ListItem>
                <ListItemText primary="b) Fotocopia de 1º, 2º hoja de los documentos de identidad de cada integrante de la agrupación y/o equipo." />
              </ListItem>
              <ListItem>
                <ListItemText primary="c) Adjuntar arancel correspondiente según lo dispuesto por los organizadores del torneo." />
              </ListItem>
              <ListItem>
                <ListItemText primary="d) Certificado de Aptitud Física" />
              </ListItem>
              <ListItem>
                <ListItemText primary="e) Foto carnet 4 x 4" />
              </ListItem>
            </List>

            <Typography variant="h5" component="h2" gutterBottom>
              <u>DE LAS AGRUPACIONES Y/O EQUIPOS</u>
            </Typography>
            <Typography paragraph>
              <strong>Art.4:</strong>
            </Typography>
            <List>
              <ListItem>
                <ListItemText primary="a) Función de los Delegados: Hacer conocer a todos los jugadores de su equipo el presente reglamento. Serán designados dos por agrupación y/o equipo y son los responsables ante los organizadores del Torneo de Básquet del accionar de las agrupaciones y/o equipos, debiendo concurrir obligatoriamente cuando sean citados." />
              </ListItem>
              <ListItem>
                <ListItemText primary="b) Función del Director Técnico (DT): Es responsable por el ingreso de los jugadores en el campo de juego, cambio de jugadores y banco de suplentes." />
              </ListItem>
            </List>
            <Typography paragraph>
              Los delegados y el DT podrán, aparte de sus funciones específicas,
              integrar sus respectivos equipos como jugadores.
            </Typography>

            <Typography paragraph>
              <strong>Art.5:</strong>
            </Typography>
            <List>
              <ListItem>
                <ListItemText primary="a) Las agrupaciones y/o equipos y sus autoridades están obligadas a cumplir las disposiciones que emanen de los organizadores del Torneo de Básquet, Reglamento General y Código de penas, y respetar y acatar las resoluciones de sus autoridades." />
              </ListItem>
              <ListItem>
                <ListItemText primary="b) Absteniéndose de protestar públicamente, formular declaraciones o quejas ante las autoridades contra tales resoluciones. Si las juzgaran injusta podrán interponer por escrito los argumentos ante los organizadores del Torneo de Básquet." />
              </ListItem>
            </List>

            <Typography paragraph>
              <strong>Art.6:</strong> Las agrupaciones y/o equipos no podrán
              alegar ignorancia con respecto a las resoluciones que se hagan
              conocer por medio de este reglamento y sus actualizaciones las
              cuales serán publicadas en las respectivas redes sociales (página
              oficial de la liga, facebook y twitter) como así también a los
              delegados de cada equipo en las reuniones.
            </Typography>
            <Typography paragraph>
              Las agrupaciones y/o equipos podrán presentar dentro de la cancha
              3 jugadores federados y 5 en la lista de buena fe (en actividad),
              el resto se completará con jugadores de 20 años en adelante. Los
              jugadores que cumplan los 20 años durante el trascurso del año
              desde Julio a Diciembre del 2013 podrán participar del torneo no
              así los que cumplan con anterioridad que no serán tenidos en
              cuenta.
            </Typography>
            <Typography paragraph>
              Los jugadores que estén federados pero no se encuentren en
              actividad no serán tenidos en cuenta como federados y podrán ser
              parte de la agrupación y/o equipo de manera normal.
            </Typography>
            <Typography paragraph>
              Si se contata que se esta jugando con 4 federados en cancha, el
              árbitro sancionara con una falta técnica al banco.
            </Typography>
            <Typography paragraph>
              El equipo que presente más jugadores que lo reglamentado (dentro
              de la cancha) perderá el partido en disputa otorgándosele los
              puntos al otro equipo. En caso de reincidir será expulsado del
              campeonato.
            </Typography>
            <Typography paragraph>
              El equipo que saque una ventaja antirreglamentaria (ganar el
              partido), ya sea por incluir mal un jugador, jugar con mas de 3
              federados en cancha, jugar con un jugador sancionado, etc. Perderá
              los puntos del partido, como así también sera sancionado con una
              fecha de suspensión su delegado.
            </Typography>

            <Typography paragraph>
              <strong>Art.7:</strong>
            </Typography>
            <List>
              <ListItem>
                <ListItemText primary="a) La agrupación y/o equipo que no se presentara a disputar dos partidos en el torneo será eliminado de la Liga sin perjuicio de las medidas punitivas y arancelarias que le correspondieran." />
              </ListItem>
              <ListItem>
                <ListItemText primary="b) Toda agrupación y/o equipo que incluya un jugador sin estar habilitado o con documentación falsa será sancionado con 1 partido al jugador, en caso de reincidencia perderá el partido en disputa sancionándose también al director técnico y en caso de una tercera reincidencia al equipo completo." />
              </ListItem>
            </List>

            <Typography paragraph>
              <strong>Art.8:</strong> Los partidos se jugarán con las leyes del
              organismo internacional del Basquet (FIBA), AAB Y Liga Paranaense
              y por las reglas que estos reglamentos determinen, no así la forma
              de clasificación, en caso de igualdad de puntos entre dos equipos
              en algunas de las posiciones de cualquier zona e instancia del
              torneo, se tomará el partido jugado entre sí. Si la igualdad fuera
              de tres o mas equipos, se determinará por diferencia de puntos, en
              caso de haber dos equipos con igual diferencia se tomará el
              partido jugado entre si.
            </Typography>
            <List>
              <ListItem>
                <ListItemText primary="a) En la etapa de Playoffs, cualquier equipo que no se presente a jugar quedará eliminado directamente por default, más allá de haber ganado el 1er encuentro por más de 20 puntos." />
              </ListItem>
              <ListItem>
                <ListItemText primary="b) En partidos de vuelta, la diferencia de puntos del 1er partido estará colocada en el tablero." />
              </ListItem>
            </List>

            <Typography paragraph>
              <strong>Art.9:</strong>
            </Typography>
            <List>
              <ListItem>
                <ListItemText primary="a) Es facultad de los organizadores de la Liga de Basquet fijar la hora de inicio de los partidos de los torneos que organice, como así la modificación de los mismos cuando lo estime conveniente. Desde la hora fijada para el inicio del primer partido habrá tolerancia de 15'. Cuando uno o ambos equipos no se presentaren en el campo de juego, el árbitro dará por finalizado el encuentro, siendo la pena la pérdida de puntos." />
              </ListItem>
              <ListItem>
                <ListItemText primary="b) En caso de no presentación de un equipo al momento del partido, el resultado será veinte (20) a cero (0) a favor del adversario. Haciéndose cargo el equipo infractor de una multa equivalente a la mitad del partido jugado (825 $).- Además el equipo no sumará 1 (un) punto, como partido perdido." />
              </ListItem>
              <ListItem>
                <ListItemText primary="c) Para que cada equipo pueda presentarse en el campo de juego y disputar el partido deberá tener un mínimo de cuatro (4) jugadores dentro del campo de juego, si para el comienzo del segundo tiempo no se han completado los cinco (5) jugadores en cancha, el partido será suspendido quedando el marcador como termino el primer tiempo (si este no supera los 20 pts, el resultado final será 20 a 0)." />
              </ListItem>
              <ListItem>
                <ListItemText primary="d) Será estricto el horario dispuesto para el inicio de los partidos, no habrá tolerancia bajo ningún concepto." />
              </ListItem>
              <ListItem>
                <ListItemText primary="e) En caso de que ningún equipo se presente a jugar su partido, ambos deberán abonar la multa correspondiente (el precio del partido por equipo), el partido se dará por jugado y ninguno de los equipos sumará puntos." />
              </ListItem>
            </List>

            <Typography paragraph>
              <strong>Art.10:</strong> Quince minutos antes de la hora designada
              por el fixture para el inicio del partido deberán presentarse los
              jugadores con los correspondientes carnets, siendo un mínimo de
              cuatro (4) y un máximo de catorce (14), conforme a lo determinado
              por este reglamento, el equipo que no lo hiciera perderá el
              partido que debía disputarse.
            </Typography>

            <Typography paragraph>
              <strong>Art.11:</strong> Los partidos tendrán una duración de
              Cuarenta (40) minutos divididos en cuatro (4) cuartos de diez (10)
              minutos cada uno, donde no se parara el reloj en ningún momento
              salvo en el último minuto del 1er, 2do y 3er cuarto, mientras que
              en el último cuarto se para en los últimos cinco (5) minutos.
            </Typography>
            <List>
              <ListItem>
                <ListItemText primary="a) Si por causas ajenas a las respectivas agrupaciones y/o equipos se suspendiera un partido y fuera responsabilidad de los organizadores de la Liga Club 12 'La Vuelta', el encuentro se jugará cuando lo decida los organizadores de dicha liga, informando a través de los delegados y las diferentes vías de comunicación la nueva fecha, hora y lugar del encuentro." />
              </ListItem>
            </List>

            <Typography paragraph>
              <strong>Art.12:</strong> Si por causas ajenas a las respectivas
              agrupaciones y/o equipos se suspendiera un partido una vez
              empezado, subsistirá el resultado que se hubiera producido y se
              jugará en otra fecha el tiempo que faltare para su terminación,
              cuando así lo dispongan los organizadores de la Liga Club 12 "La
              Vuelta", pero en caso de que faltaren menos de diez minutos para
              finalizar el encuentro y la diferencia sea mayor a 20 pts, el
              mismo se dará por finalizado.
            </Typography>

            <Typography paragraph>
              <strong>Art.13:</strong> Si la suspensión de un partido resultare
              imputable a una de las agrupaciones y/o equipos actuante, ya sea
              por acción de sus jugadores, directivos, público, etc. se
              declarará terminado y perderá los puntos el equipo responsable,
              sin perjuicio de las otras medidas que pueden alcanzar a los
              responsables y que determinará el tribunal de penas o quién lo
              reemplace de acuerdo al reglamento.
            </Typography>

            <Typography paragraph>
              <strong>Art.14:</strong> Si la suspensión de un partido resultare
              imputable a las dos agrupaciones y/o equipos actuantes, ya sea por
              acción de sus jugadores, directivos, público, etc., se declarará
              terminado y perderán los puntos ambos equipos, sin perjuicio de
              las otras medidas que pueden alcanzar a los responsables y que
              determinará el tribunal de penas o quién lo reemplace de acuerdo
              al reglamento.
            </Typography>

            <Typography paragraph>
              <strong>Art.15:</strong> Cuando se complete el tiempo
              reglamentario de un partido suspendido del cual el referee hubiera
              expulsado uno o mas jugadores, los equipos deberán continuar con
              el mismo número de jugadores que tenían en la cancha al momento de
              la suspensión, ya sea los que actuaron u otros. Los jugadores
              expulsados no podrán integrar el equipo aunque hayan cumplido la
              pena.
            </Typography>

            <Typography paragraph>
              <strong>Art.16:</strong> Para suspender un partido deberá
              solicitarse por escrito con cinco (5) días de anticipación,
              debiendo hacerse cargo de los gastos que demande el equipo que lo
              solicite y debiendo abonar dichos gastos al momento de presentar
              la solicitud. Quedando supeditado a la decisión que tomen los
              miembros organizadores de la Liga Club 12 "La Vuelta".
            </Typography>
            <List>
              <ListItem sx={{ display: 'block' }}>
                <Typography paragraph>
                  a) Si una agrupación abandona el torneo una vez comenzado, se
                  precederá de la siguiente manera:
                </Typography>
                <List>
                  <ListItem>
                    <ListItemText primary="- Los partidos que ya disputó esa agrupación quedarán tal cual el resultado final." />
                  </ListItem>
                  <ListItem>
                    <ListItemText primary="- Los partidos que deba disputar esa agrupación se les dará por ganado al equipo rival por un tanteador de 20 a 0 a favor. Abonando el equipo ganador la suma total del partido, dado que de igual manera el partido se hubiese disputado si el equipo contrario no abandonaba el torneo." />
                  </ListItem>
                </List>
              </ListItem>
            </List>

            <Typography variant="h5" component="h2" gutterBottom>
              <u>DE LOS JUGADORES</u>
            </Typography>

            <Typography paragraph>
              <strong>Art.17:</strong> Los organizadores de la Liga Club 12 "La
              Vuelta", tendrán un registro en el que deberán inscribirse todos
              los jugadores debiendo para ello estar físicamente habilitados
              para la práctica de Basquet, haciéndose responsables las
              agrupaciones y/o equipos de los accidentes que pudieran
              ocasionarse durante la disputa de la Liga de Basquet Club 12 "La
              Vuelta".
            </Typography>
            <Typography paragraph>
              Deberán cumplir previamente las exigencias que a continuación se
              detallan:
            </Typography>
            <List>
              <ListItem>
                <ListItemText primary="a) Concurrir voluntariamente al lugar y horario que se determine para realizar las reuniones." />
              </ListItem>
              <ListItem>
                <ListItemText primary="b) Deberán presentar carnets emitidos por los organizadores de la Liga Club 12 'La Vuelta'." />
              </ListItem>
              <ListItem>
                <ListItemText primary="c) Durante la Fase regular, el/los jugadores que no tengan su carnet al momento de disputar el partido, podrán presentar el DNI, Licencia de conducir o alguna identificación que contenga el nombre completo y su foto. Durante las etapas de definición (Final Four, Cuadrangular Descenso, Playoffs, Reclasificación) deberán presentar el carnet de Club 12 únicamente, jugador que así NO lo hiciera, no podrá disputar el partido." />
              </ListItem>
              <ListItem>
                <ListItemText primary="d) Manifestar con claridad la agrupación y/o equipo para la que desean jugar." />
              </ListItem>
              <ListItem>
                <ListItemText primary="e) Es de carácter obligatorio estar asegurado en la compañía que los organizadores designen." />
              </ListItem>
            </List>

            <Typography paragraph>
              <strong>Art.18:</strong> Todo jugador que haya registrado su firma
              en la Liga Club 12 "La Vuelta", no podrá alegar que hubo error y
              será jugador de la agrupación y/o equipo por la cual haya
              declarado querer actuar y no podrá hacerlo por otro sin antes
              llenar los requisitos del pedido de pase a otra agrupación y/o
              equipo.
            </Typography>
            <List>
              <ListItem>
                <ListItemText primary="a) Los organizadores de la Liga Club 12 'La Vuelta', tendrán en cuenta a los jugadores registrados en las conformaciones de las listas de buena fe de cada agrupación, eliminando a aquellos que se considere elementos provocativos, de acuerdo a los antecedentes y conceptos formados obrantes por los organizadores de la Liga Club 12 'La Vuelta'; esto es para evitar problemas que son inadmisibles, como los suscitados en años anteriores con actitudes antideportivas, violencia, gresca agresiones verbales y/o de hecho, etc." />
              </ListItem>
            </List>

            <Typography paragraph>
              <strong>Art.19:</strong>
            </Typography>
            <List>
              <ListItem>
                <ListItemText primary="a) Todo jugador que hubiere actuado por una agrupación y/o equipo en un año deportivo y quisiera actuar en otro año deportivo por otra agrupación podrá pedir pase, no pudiendo la agrupación negar el mismo siempre y cuando el jugador estuviera 'libre de deuda'." />
              </ListItem>
              <ListItem>
                <ListItemText primary="b) Todo jugador que acumule tres faltas antideportivas y/o 4 faltas técnicas será sancionados con una fecha de suspensión. La reiteración de esta sanción sumaran una fecha mas de suspensión." />
              </ListItem>
              <ListItem>
                <ListItemText primary="c) A la lista de buena fe podrán integrarla hasta 5 (cinco) jugadores que hayan participado o participen en planteles de primera división durante el año en curso, pudiendo estar solo 3 en cancha solamente y sin excepción." />
              </ListItem>
              <ListItem>
                <ListItemText primary="d) Se podrán realizar cambios en la lista de buena fe hasta la tercera fecha, luego sin excepción cuando un jugador sea dado de baja por lesión, presentando el certificado médico que demuestre que no puede continuar durante el resto del torneo." />
              </ListItem>
              <ListItem>
                <ListItemText primary="e) Los jugadores profesionales, que hayan jugado o estén jugando en ligas profesionales (Liga A, TNA y Torneo Federal) podrán jugar en Club 12 bajo las mismas condiciones administrativas que el resto de los jugadores, siendo federados estén o no en actividad. Al momento de disputar los Playoffs deben tener disputados el 50 % de los partidos de la fase regular. Caso contrario no podrán disputar dicha fase." />
              </ListItem>
            </List>

            <Typography paragraph>
              <strong>Art.20:</strong>
            </Typography>
            <List>
              <ListItem>
                <ListItemText primary="a) Todo jugador que en la lista de buena fe presentada por la agrupación no figurase en ella será declarado libre, teniendo hasta quince días corridos después de iniciado el campeonato para inscribirse en otra agrupación que tuviera cupo." />
              </ListItem>
              <ListItem>
                <ListItemText primary="b) Todo jugador que haya integrado una agrupación y esta adeude dinero a los organizadores de esta Liga de Basquet Club 12 'La Vuelta', tendrá que pagar el monto de la deuda que los organizadores determinen mas los daños y/o perjuicios económicos que ésta hubiera ocasionado." />
              </ListItem>
            </List>

            <Typography paragraph>
              <strong>Art.21:</strong> En los partidos oficiales es obligatorio
              el uso del correspondiente uniforme, la camisa o camiseta deberá
              llevar el número correspondiente en forma bien visible, la
              vestimenta se completará con pantalón de basquet no permitiéndose
              malla o short de baño, los calzados de básquet adecuados, no
              pudiendo jugar de alpargatas, ojotas, zapatos o algún calzado
              similar.
            </Typography>

            <Typography paragraph>
              <strong>Art.22:</strong> Los capitanes de equipo
              independientemente de los derechos y deberes que le corresponden
              según los reglamentos del juego y disposiciones pertinentes de
              este reglamento, están autorizados a presentar nota de las
              observaciones que estimen necesarias y que servirán de constancia
              para trámites posteriores, tiene además el deber de velar que los
              jugadores cumplan con todas las obligaciones debiendo observar
              severamente toda infracción, bajo pena de ser personalmente
              responsable.
            </Typography>
            <Typography paragraph>Está prohibido a los jugadores:</Typography>
            <List>
              <ListItem>
                <ListItemText primary="a) Agredir a algún miembro de otra agrupación con insultos, amenazas y/o agresión física, siendo la sanción correspondiente fechas de suspensión y hasta la expulsión permanente de esta Liga." />
              </ListItem>
              <ListItem>
                <ListItemText primary="b) Protestar con palabras o ademanes o realizar gestos, ofensas o agresiones contra los jueces, veedor, mesa de control y/o miembro organizador de la Liga, antes, durante o después de los partidos." />
              </ListItem>
              <ListItem>
                <ListItemText primary="c) Realizar gestos obscenos, palabras incorrectas, ofender al público, autoridades o miembros de otras agrupaciones y/o equipos." />
              </ListItem>
              <ListItem>
                <ListItemText primary="d) Realizar cualquier acto que atente contra la moral, buenas costumbres y el Fair Play." />
              </ListItem>
              <ListItem>
                <ListItemText primary="e) Ingerir bebidas alcohólicas antes y durante el partido." />
              </ListItem>
              <ListItem>
                <ListItemText primary="f) Agredir al árbitro con insultos, amenazas y/o agresión física, siendo la sanción correspondiente fechas de suspensión y hasta la expulsión permanente de esta Liga." />
              </ListItem>
              <ListItem>
                <ListItemText primary="g) Participar de un partido estando suspendido. El castigo a esta infracción será una nueva suspensión." />
              </ListItem>
              <ListItem>
                <ListItemText primary="h) Solicitar los informes a la mesa de control (son reservados y de exclusiva competencia de los organizadores y del tribunal de disciplina)." />
              </ListItem>
            </List>

            <Typography paragraph>
              <strong>Art.22 Bis:</strong> Club 12 "La Vuelta" se reserva el
              derecho de admisión a todo jugador que protagonice o participe en
              cualquier hecho de violencia en sus respectivos clubes dentro del
              ámbito local y/o provincial. El alcance de este artículo va desde
              sanciones hasta expulsión de la liga.
            </Typography>

            <Typography variant="h5" component="h2" gutterBottom>
              <u>SUSTITUCIÓN DE JUGADORES</u>
            </Typography>

            <Typography paragraph>
              <strong>Art.23:</strong> En encuentros de certámenes oficiales se
              podrán hacer tantas sustituciones como jugadores suplentes haya en
              el banco.
            </Typography>

            <Typography paragraph>
              <strong>Art.24:</strong> Se permitirá en cualquier momento antes
              de finalizar el encuentro registrar jugadores en la planilla y
              participar del partido.
            </Typography>

            <Typography paragraph>
              <strong>Art.25:</strong> El equipo que antes de haber comenzado el
              partido no haya presentado la totalidad de los carnets de los
              jugadores registrados en la planilla perderá puntos. La
              presentación de los mencionados carnets se debe hacer de forma
              personal.
            </Typography>

            <Typography variant="h5" component="h2" gutterBottom>
              <u>ACTA Y REUNIONES</u>
            </Typography>

            <Typography paragraph>
              <strong>Art.26:</strong> Los organizadores de la Liga Club 12 "La
              Vuelta", se reunirán junto con los delegados cuando estos lo crean
              conveniente para poder evacuar dudas, consultas, pago de
              aranceles, etc.
            </Typography>
            <List>
              <ListItem>
                <ListItemText primary="a) Aquellas agrupaciones que deseen presentar alguna inquietud deberán comunicarse con los organizadores de la liga para poder reunirse en otro día y horario." />
              </ListItem>
              <ListItem>
                <ListItemText primary="b) Cuando la organización de la liga lo considere necesario citará a los delegados a reunión a realizarse en fecha y horario que la misma establezca, siendo la asistencia de carácter obligatorio. En caso de ausencia de ambos Delegados no podrán efectuar reclamo alguno de lo citado en la reunión." />
              </ListItem>
              <ListItem>
                <ListItemText primary="c) Cuando los delegados requieran una reunión extraordinaria con los organizadores de la Liga Club 12 'La Vuelta', deberán presentar la solicitud por escrito y la firma de la mitad mas uno de las agrupaciones participantes." />
              </ListItem>
              <ListItem>
                <ListItemText primary="d) Las informaciones estarán a disposición de los delegados una vez que lo pidan a los organizadores y estos lo creen convenientes." />
              </ListItem>
            </List>

            <Typography paragraph>
              <strong>Art.27:</strong> Forma de disputa: La Liga se realizará
              con el sistema de todos contra todos solamente ida, y definiendo
              en la modalidad de Play Off.
            </Typography>
            <Typography paragraph>
              En caso de igualdad en alguna de las posiciones prevalecerá el
              partido disputado entre ellos resultando el ganador el que acceda
              a la mejor posición.
            </Typography>

            <Typography paragraph>
              <strong>Art.28:</strong> Las jornadas que se adelanten por
              feriados posteriores a los días habituales -domingos- y no
              existiendo posibilidad de reunirse el tribunal de disciplina, el
              jugador que fuera expulsado o tenga que cumplir con alguna sanción
              quedará automáticamente suspendido para el siguiente cotejo.
            </Typography>

            <Typography variant="h5" component="h2" gutterBottom>
              <u>PLANILLA Y VEEDOR</u>
            </Typography>

            <Typography paragraph>
              <strong>Art.29:</strong> La mesa de control y el veedor
              presentarán, en forma separada, ante los organizadores de la Liga,
              exclusivamente, un informe con respecto a lo acontecido en la
              jornada, haciendo hincapié en todo hecho anormal que se produzca,
              ya sea por comportamiento de los jugadores, simpatizantes
              (jugadores), y/o cualquier hecho que ellos consideren necesario
              informar.
            </Typography>
            <Typography paragraph>
              Bajo ningún punto de vista el informe de veedor y/o mesa de
              control podrá modificar los fallos de los jueces.
            </Typography>

            <Typography paragraph>
              <strong>Art.30:</strong> Una vez finalizada la jornada el árbitro
              presentará ante los organizadores de la Liga Club 12 "La Vuelta",
              un informe de los hechos ocurridos en el desarrollo de los
              partidos.
            </Typography>

            <Typography paragraph>
              <strong>Art.31:</strong> El jugador expulsado por el árbitro podrá
              presentar un descargo personal ante el Tribunal de Disciplina.
            </Typography>

            <Typography paragraph>
              <strong>Art.32:</strong> El jugador informado por los jueces,
              veedor, mesa de control u organizadores de la Liga será publicado
              en los sitios oficiales de la liga y sancionados; podrá presentar
              un descargo personal ante los organizadores y luego estos
              determinaran junto con el tribunal de disciplina la sanción
              correspondiente. El escrito podrá presentarse hasta el día
              miércoles siguiente a la publicación. Vencido dicho plazo caducará
              todo derecho a presentar el mismo y el Tribunal de Disciplina dará
              a conocer en forma definitiva la sanción impuesta.
            </Typography>

            <Typography variant="h5" component="h2" gutterBottom>
              <u>PROTESTAS</u>
            </Typography>

            <Typography paragraph>
              <strong>Art.33:</strong> Se considera protesta toda solicitud
              formal escrita de agrupación y/o equipo que tiendan a modificar el
              resultado de un partido en que hubiera intervenido.
            </Typography>

            <Typography paragraph>
              <strong>Art.34:</strong> El escrito deberá presentarse hasta el
              segundo día hábil siguiente al del que se jugó el partido. Vencido
              dicho plazo caducará todo derecho a presentar el mismo.
            </Typography>

            <Typography paragraph>
              <strong>Art.35:</strong> El escrito de protesta deberá contener:
            </Typography>
            <List>
              <ListItem>
                <ListItemText primary="a) La explicación clara y precisa de los motivos en que se fundamente." />
              </ListItem>
              <ListItem>
                <ListItemText primary="b) La petición en términos precisos, debiendo constar la trasgresión reglamentaria en que se funda, indicando norma que ha sido incumplida por el adversario." />
              </ListItem>
              <ListItem>
                <ListItemText primary="c) Las firmas del delegado titular o suplente de la agrupación." />
              </ListItem>
            </List>

            <Typography paragraph>
              <strong>Art.36:</strong> Todas las solicitudes de protesta deberán
              estar acompañadas de una fotocopia la cual será entregada y
              quedará otra a disposición de la agrupación y/o equipo que la
              presente.
            </Typography>

            <Typography paragraph>
              <strong>Art.37:</strong> Las resoluciones del árbitro, en lo que
              se refiere al juego no podrán ser causa de protestas.
            </Typography>

            <Typography paragraph>
              <strong>Art.38:</strong> La presentación que reúna los requisitos
              exigidos será considerada por los organizadores del Torneo de
              Basquet Club 12 "La Vuelta" quiénes trasladará por el término de
              tres días corridos vista a la agrupación y/o equipo, bajo
              apercibimiento que si dentro del plazo mencionado no fuera
              evacuada la vista corrida se considerará decaído el derecho de
              apelar. Recibida la respuesta o no a la vista concedida, producirá
              despacho y dictamen.
            </Typography>

            <Typography variant="h5" component="h2" gutterBottom>
              <u>PREMIOS</u>
            </Typography>

            <Typography paragraph>
              <strong>Art.39:</strong> Los organizadores de la Liga Club 12 "La
              Vuelta" establecerán los premios a otorgarse en el presente
              torneo.
            </Typography>
            <List>
              <ListItem sx={{ display: 'block' }}>
                <Typography paragraph>* Campeón Copa de Oro</Typography>
                <List>
                  <ListItem>
                    <ListItemText primary="- Inscripción Gratuita para la próxima temporada." />
                  </ListItem>
                  <ListItem>
                    <ListItemText primary="- 10 Kg de Carne, más 2 cajones de cervezas." />
                  </ListItem>
                  <ListItem>
                    <ListItemText primary="- Trofeo" />
                  </ListItem>
                </List>
              </ListItem>

              <ListItem sx={{ display: 'block' }}>
                <Typography paragraph>* Sub-Campeón Copa de Oro</Typography>
                <List>
                  <ListItem>
                    <ListItemText primary="- 8 Kg de Carne, más 1 cajón de cervezas." />
                  </ListItem>
                </List>
              </ListItem>

              <ListItem sx={{ display: 'block' }}>
                <Typography paragraph>* Campeón Copa de Plata</Typography>
                <List>
                  <ListItem>
                    <ListItemText primary="- 24 Chorizos, más 1 cajón de cervezas." />
                  </ListItem>
                  <ListItem>
                    <ListItemText primary="- Trofeo" />
                  </ListItem>
                </List>
              </ListItem>

              <ListItem sx={{ display: 'block' }}>
                <Typography paragraph>* Campeón Zona Ascenso</Typography>
                <List>
                  <ListItem>
                    <ListItemText primary="- 24 Chorizos, más 1 cajón de cervezas." />
                  </ListItem>
                  <ListItem>
                    <ListItemText primary="- Trofeo" />
                  </ListItem>
                </List>
              </ListItem>

              <ListItem sx={{ display: 'block' }}>
                <Typography paragraph>* Campeón Final Four</Typography>
                <List>
                  <ListItem>
                    <ListItemText primary="- 2 Combos de Fernet con Coca" />
                  </ListItem>
                </List>
              </ListItem>
            </List>

            <Typography variant="h5" component="h2" gutterBottom>
              <u>ARANCELES</u>
            </Typography>

            <Typography paragraph>
              <strong>Art.40:</strong> Deberá abonarse en concepto de arancel el
              importe que oportunamente determinen los organizadores de la Liga
              Club 12 "La Vuelta". El no cumplimiento de los pagos
              correspondientes dará lugar a las sanciones que al efecto
              determinen los organizadores.
            </Typography>

            <Typography paragraph>
              <strong>Art.40 bis:</strong> Cada agrupación y/o equipo deberá
              abonar una inscripción correspondiente a la estipulada por la
              organización de Club 12 por equipo, por única vez al comienzo de
              cada torneo, pudiéndose abonar hasta el primer mes de torneo, de
              lo contrario el equipo podrá quedar suspendido de algún partido
              con pérdida de puntos hasta que paguen lo adeudado.
            </Typography>

            <Typography paragraph>
              <strong>Art.41:</strong> El arancel será abonado abonadas del 1 al
              15 de cada mes (lugar a confirmar). El pago fuera de término
              producirá automáticamente la suspensión del equipo y la
              consecuente pérdida de puntos si así lo determinen los
              organizadores del torneo.
            </Typography>

            <Typography paragraph>
              <strong>Art.42:</strong> Bajo ningún concepto se devolverá el
              dinero abonado a las agrupaciones y/o equipos que ya se hayan
              comprometido a jugar y a los cuales se los haya fixturado.
            </Typography>

            <Typography paragraph>
              <strong>Art.43:</strong> La organización del Torneo se reserva el
              derecho de reglamentar todo lo que no esté contemplado en este
              reglamento.
            </Typography>

            <Typography paragraph>
              <strong>Art.44:</strong> En caso de surgir anomalías antes de
              iniciarse el partido, por cualquier causa de las contempladas en
              el presente reglamento, el Árbitro a solicitud del Delegado del
              equipo supuestamente perjudicado, deberá dejar constancia en la
              planilla del motivo invocado y que el mismo tiene carácter de
              protesta, con lo cual se cerrará la planilla, la que será firmada
              por el Planillero, el Árbitro y el Delegado. En caso de que ambos
              equipos a través de sus Delegados y con la autorización del
              Árbitro, decidieran jugar el partido sin dejar constancia en la
              planilla, quedará firme el resultado del partido y el equipo
              supuestamente perjudicado perderá cualquier derecho a la protesta.
            </Typography>

            <Typography paragraph>
              <strong>Art.45:</strong> Ante la situación de que al momento de
              iniciarse el encuentro los dos equipos tengan vestimentas de
              colores similares, el Árbitro llamará a los Delegados y sorteará
              quien debe cambiar de camiseta, ya que todas las agrupaciones
              deben tener el correspondiente juego alternativo.
            </Typography>

            <Typography paragraph>
              <strong>Art.46:</strong> Se deja habilitado el pase de un jugador
              de un equipo a otro entre un torneo a otro una vez finalizado, o
              durante el mismo en el caso que no haya disputado ningún encuentro
              con dicho equipo. El mismo se hará con el consentimiento del
              equipo anterior (dándolo de baja).
            </Typography>

            <Typography paragraph>
              <strong>Art.47:</strong> Queda habilitada la incorporación después
              del comienzo del Torneo en reemplazo de un jugador lesionado
              (lesión temporaria comprobable).
            </Typography>
            <List>
              <ListItem>
                <ListItemText primary="a) A partir de la 3er fecha de comenzado el torneo, no se podrán hacer mas sustituciones en los equipos a menos que sea por un jugador lesionado como lo indica el artículo anterior." />
              </ListItem>
            </List>

            <Typography paragraph>
              <strong>Art.48:</strong> Toda Agrupación que transgreda la
              Reglamentación de este Torneo, haciendo jugar a personas que no
              integran la lista de buena fe (falsificando firma y carnet) será
              EXPULSADA DEL TORNEO.
            </Typography>

            <Typography paragraph>
              <strong>Art.49:</strong> Los jugadores que hayan integrado
              agrupaciones que fueron expulsadas del Torneo, no podrán integrar
              otra agrupación durante el transcurso del mismo. Pudiendo elevar
              el pedido de reincorporación para el Torneo del año siguiente, la
              que quedará a criterio de los organizadores.
            </Typography>

            <Typography paragraph>
              <strong>Art.50:</strong> Las agrupaciones y/o equipos no podrán
              alegar ignorancia con respecto a las resoluciones que se hagan
              conocer por medio de los sitios oficiales, disponible
              semanalmente.
            </Typography>

            <Typography paragraph>
              <strong>Art.51:</strong> Jugador que pierda o rompa su carnet
              correspondiente a la liga, deberá abonar 20 $ para la realización
              de uno nuevo.
            </Typography>

            <Typography variant="h5" component="h2" gutterBottom>
              <u>MODALIDAD DE JUEGO</u>
            </Typography>

            <Typography paragraph>
              <strong>Art.52:</strong> Ascenso y Descenso
            </Typography>

            <List>
              <ListItem>
                <ListItemText primary="- Fase Regular: Todos contra Todos" />
              </ListItem>
              <ListItem>
                <ListItemText primary="- Reclasificación: 5-12; 6-11; 7-10; 8-9 (Ida y Vuelta)" />
              </ListItem>
              <ListItem>
                <ListItemText primary="- Final Four: 1 vs 4 (a); 2 vs 3 (b); (Playoffs a un partido)" />
              </ListItem>
              <ListItem>
                <ListItemText primary="- Ganador del a vs Ganador del b (Final)" />
              </ListItem>
              <ListItem>
                <ListItemText primary="- Playoffs: 1 vs Ganador 8 vs 9; 2 vs Ganador 7-10; 3 vs Ganador 6-11; 4 vs Ganador 5-12 (Cuartos de Final)" />
              </ListItem>
              <ListItem>
                <ListItemText primary="- (Esto equivale para las dos zonas)" />
              </ListItem>
              <ListItem>
                <ListItemText primary="- Perdedores Cuartos de final Zona Campeonato, juegan Copa de Plata" />
              </ListItem>
            </List>

            {/* Sección de Descensos */}
            <Typography paragraph>
              <u>Descensos:</u>
            </Typography>
            <Typography paragraph>
              Los perdedores de la reclasificación de la A, jugarán un
              cuadrangular todos contra todos a un solo partido donde los dos
              peores posicionados descienden.
            </Typography>
            <List>
              <ListItem>
                <ListItemText primary="1. En la zona Ascenso, los perdedores de la reclasificación finalizan su participación." />
              </ListItem>
            </List>

            {/* Sección de Ascensos */}
            <Typography paragraph>
              <u>Ascenso:</u>
            </Typography>
            <Typography paragraph>
              El Campeón y el Sub Campeón de la zona Ascenso.
            </Typography>
          </Typography>
        </Paper>
      </Box>
    </Container>
  );
};

export default Regulation;
