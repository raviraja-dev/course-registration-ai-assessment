using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CourseRegistration.Application.Models
{
    public class DropResult
    {
        public DropStatus Status { get;}
        public string Message { get;}
        public int? PromotedLearnerId { get;}

        public DropResult(DropStatus status, string message, int? promotedLearnerId)
        {
            Status = status;
            Message = message;
            PromotedLearnerId = promotedLearnerId;
        }
    }
}
