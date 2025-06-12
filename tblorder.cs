using System.ComponentModel.DataAnnotations;

namespace econest.Models
{
    public class tblorder
    {
        [Key]
        public int oid { get; set; }

        public int uid { get; set; }

        public int pid { get; set; }

        public string name { get; set; }

        public DateTime o_date { get; set; }

        //public int qty { get; set; }

        public decimal total_amt { get; set; }

        public string p_status { get; set; }

        public string p_method { get; set; }

        public string address { get; set; }

        public string o_status { get; set; }

        public string contact { get; set; }
    }
}
