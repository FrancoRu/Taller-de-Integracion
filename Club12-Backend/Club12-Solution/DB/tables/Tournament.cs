using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DB.tables
{
    public class Tournament
    {
        [Key]
        public Guid TournamentID { get; set; }
        public string TournamentDescription { get; set; }
        public int Year { get; set; }
    }
}
