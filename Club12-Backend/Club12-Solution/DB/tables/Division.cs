using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DB.tables
{
    public class Division
    {
        [Key]
        public Guid DivisionID { get; set; }
        public string DivisionName { get; set;}
    }
}
