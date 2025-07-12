// IndexTournament.tsx
import { useEffect } from 'react'

export const IndexTournament = () => {
  // const { getAllTournament, createTournament } = useTournament();
  // const [tournaments, setTournaments] = useState<TournamentResponse[]>([]);
  // const [formData, setFormData] = useState<CreateTournament>({
  //   description: "",
  //   name: "",
  // });

  useEffect(() => {
    // Función para obtener todos los torneos y guardarlos en el estado
    const fetchTournaments = async () => {
      // const data = await getAllTournament();
      // if (false) {
      //   setTournaments(data);
      // }
    }

    fetchTournaments()
  }, [])

  // Manejador para actualizar el estado del formulario
  // const handleInputChange = (event: React.ChangeEvent<HTMLInputElement>) => {
  //   const { name, value } = event.target;
  //   //setFormData({ ...formData, [name]: value });
  // };

  // Manejador para enviar el formulario
  // const handleSubmit = async (event: React.FormEvent) => {
  //   event.preventDefault();
  //   await createTournament(formData);
  // };

  return (
    <div>
      <h1>Gestión de Torneos</h1>

      {/* Botón para mostrar la tabla de torneos */}
      {/* <button onClick={() => getAllTournament()}>Mostrar Torneos</button> */}

      {/* Tabla de torneos */}
      <table>
        <thead>
          <tr>
            <th>Id</th>
            <th>Nombre</th>
            <th>Descripción</th>
            <th>Division</th>
          </tr>
        </thead>
        <tbody>
          {/* {tournaments.map((tournament, index) => (
            <tr key={index}>
              <td>{tournament.id}</td>
              <td>{tournament.name}</td>
              <td>{tournament.description}</td>
              <td>{tournament.division ?? "Sin Division"}</td>
            </tr>
          ))} */}
        </tbody>
      </table>

      {/* Formulario para crear un nuevo torneo */}
      {/* <form onSubmit={handleSubmit}>
        <div>
          <label>Nombre:</label>
          <input
            type="text"
            name="name"
            value={formData.name}
            onChange={handleInputChange}
            required
          />
        </div>
        <div>
          <label>Descripción:</label>
          <input
            type="text"
            name="description"
            value={formData.description}
            onChange={handleInputChange}
            required
          />
        </div>
        <button type="submit">Crear Torneo</button>
      </form> */}
    </div>
  )
}
