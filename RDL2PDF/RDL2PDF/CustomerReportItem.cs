using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RDL2PDF
{
    public class CustomerReportItem
    {
        public CustomerReportItem() { }
        public String Name { get; set; }
        public String FirstName { get; set; }
        public String LastName { get; set; }
        public String Department { get; set; }
        public decimal Salary { get; set; }
    }
}
