using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShipExecNavigator.BusinessLogic.ResponseModel
{
    public class GetResponseBase
    {

        public string TotalRecords { get; set; }

        public string ErrorCode { get; set; }

        public string TransactionId { get; set; }

        public string ErrorMessage { get; set; } = "";
    }
}
