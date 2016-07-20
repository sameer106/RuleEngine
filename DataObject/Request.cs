using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RuleEngine.DataObject
{
    public class Request
    {
        public int FromState { get; set; }
        public int ToState { get; set; }
        public int CorpId { get; set; }
        public int SchemeId { get; set; }
        public string CustomerEmail { get; set; }
        public string CustomerContact { get; set; }

        public string HREmail { get; set; }
        public string HRContact { get; set; }

        public string HospitalEmail { get; set; }
        public string HospitalContact { get; set; }

        public string PolicyHolderEmail { get; set; }
        public string PolicyHolderContact { get; set; }
        
        public decimal Amount { get; set; }

    }
}
