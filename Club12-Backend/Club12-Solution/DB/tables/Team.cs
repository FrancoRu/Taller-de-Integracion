using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DB.tables
{
    public class Team
    {
        [Key]
        public Guid TeamID { get; set; }

        public Guid DivisionID { get; set; }

        public string TeamName { get; set; }
        [Required]
        [ForeignKey("DivisionID")]
        public virtual Division Division { get; set; }

    }
}
