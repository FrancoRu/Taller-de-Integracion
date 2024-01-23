using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DB.tables
{
    public class Sanction
    {
        [Key]
        public Guid SanctionID { get; set; }

        public string SanctionName { get; set; }
    }
}
