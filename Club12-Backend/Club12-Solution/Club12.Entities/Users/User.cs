using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Club12.Entities.Users
{
    public class User
    {
       

        [Required]
        public required string Nombre { get; set; }

        [Required]
        public required string Apellido { get; set; }

        
        
    }
}
