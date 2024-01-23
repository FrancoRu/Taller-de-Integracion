using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DB.tables
{
    public class Record
    {
        [Key]
        public Guid RecordID { get; set; }
        public Guid TournamentID { get; set; }
        public Guid TeamId { get; set; }
        [Required]
        [ForeignKey("TournamentID")]
        public virtual Tournament Tournament { get; set; }
        [Required]
        [ForeignKey("TeamId")]
        public virtual Team Team { get; set; }

    }
}
