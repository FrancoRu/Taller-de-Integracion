using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DB.tables
{
    public class Game
    {
        [Key]
        public Guid GameID { get; set; }
        public Guid TeamLocalID { get; set; }
        public Guid TeamVisitorID { get; set; }
        public Guid WinningTeamID {  get; set; }
        public Guid TournamentID { get; set; }
        public DateTime Date { get; set; }
        public int LocalPoints { get; set; }
        public int VisitorPoints { get; set; }
        [Required]
        [ForeignKey("TeamLocalID")]
        public virtual Team LocalTeam { get; set; }
        [Required]
        [ForeignKey("TeamVisitorID")]
        public virtual Team VisitorTeam { get; set;}
        [Required]
        [ForeignKey("WinningTeamID")]
        public virtual Team WinningTeam { get; set; }
        [Required]
        [ForeignKey("TournamentID")]
        public virtual Tournament Tournament { get; set; }

    }
}
