using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DB.tables
{
    public class Player
    {
        [Key]
        public Guid PlayerID { get; set; }

        public string PlayerName { get; set; }

        public string PlayerLastname { get; set; }

        public double PlayerHeight { get; set; }

        public double PlayerWeight { get; set; }
    }
}
