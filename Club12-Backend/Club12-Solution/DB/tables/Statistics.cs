using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DB.tables
{
    public class Statistics
    {
        [Key]
        public Guid StatisticsID { get; set; }

        public string StatisticsName { get; set;}
    }
}
