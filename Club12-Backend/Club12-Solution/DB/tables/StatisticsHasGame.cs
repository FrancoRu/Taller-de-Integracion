using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DB.tables
{
    public class StatisticsHasGame
    {
        [Key]
        public Guid StatisticsHasGameID { get; set; }
        public Guid PlayerId { get; set; }
        public Guid StatisticsID { get; set; }
        public Guid GameID { get; set; }
        public int value { get; set; }
        [Required]
        [ForeignKey("PlayerID")]
        public virtual Player Player { get; set; }
        [Required]
        [ForeignKey("StatisticsID")]
        public virtual Statistics Statistics { get; set; }
        [Required]
        [ForeignKey("GameID")]
        public virtual Game Game { get; set; }

    }
}
