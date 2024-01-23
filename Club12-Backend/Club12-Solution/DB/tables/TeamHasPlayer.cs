using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DB.tables
{
    public class TeamHasPlayer
    {
        [Key]
        public Guid TeamHasPlayerID { get; set; }

        public Guid PlayerID { get; set; }

        public Guid TeamID { get; set; }

        public int ShirtNumber { get; set; }

        public DateTime Date { get; set; }
        [Required]
        [ForeignKey("TeamID")]
        public virtual Team Team { get; set; }
        [Required]
        [ForeignKey("PlayerID")]
        public virtual Player Player { get; set; }
    }
}
