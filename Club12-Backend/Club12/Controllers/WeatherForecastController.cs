using Club12.Entities.Users;
using Microsoft.AspNetCore.Mvc;

namespace Club12.Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private static List<User> usuarios = [];

        private readonly ILogger<WeatherForecastController> _logger;

        public WeatherForecastController(ILogger<WeatherForecastController> logger)
        {
            _logger = logger;
        }

        [HttpGet(Name = "GetWeatherForecast")]
        public IEnumerable<WeatherForecast> Get()
        {
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
        }

        [HttpGet("Saludar", Name = "Saludar")]
        public string Saludar()
        {
            _logger.LogWarning("Hola, ¿cómo estás? Soy el logger");
            var newRandom = new Random();
            return $"Hola profesaki {newRandom.NextInt64()}";
        }
        [HttpGet("SaludarConNombre", Name = "SaludarConNombre")]
        public string SaludarConNombre(string? nombre )
        {
            _logger.LogWarning($"hola {nombre}");
            return string.IsNullOrEmpty(nombre) ? "Si no me pasas nombre me da amsiedad y me rompere" : $"Hola {nombre}";
        }

        [HttpPost("CreateUser", Name = "CreateUser")]
        public ActionResult CreateUser(User user) {
            if (usuarios.Contains(user)) {
                return BadRequest("Usuario ya existe");
            }
            var newUser = new User() { Nombre = user.Nombre, Apellido = user.Apellido } ;
            usuarios.Add(newUser) ;
            return Ok(newUser);
        }

        [HttpGet("GetUsers", Name = "GetUsers")]
        public ActionResult GetUsers() {
            return Ok(usuarios);
        }

        [HttpGet("GetUserByName", Name = "GetUserByName")]
        public ActionResult GetUsersByName(string name) {
            var usersearch = usuarios.FirstOrDefault(user =>  string.Equals( user.Nombre, name));
            if(usersearch is null) return NotFound("Comedor de trabas serial noi localizado");
            return Ok(usersearch);
        }
    }
}
