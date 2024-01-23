using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DB.tables
{
    public class PlayerHasSanction
    {
        [Key]
        public Guid PlayerHasSanctionID { get; set; }
        public Guid GameID { get; set; }
        public double Duration { get; set; }
        public DateTime Date {  get; set; }
        [Required]
        [ForeignKey("GameID")]
        public virtual Game Game { get; set; }

    }
}
