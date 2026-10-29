using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CourseRegistration.Domain.Entities
{
    public  class CourseOffering
    {
        public int CourseId { get; private set; }
        public int Capacity { get; private set; }
        public CourseOffering(int courseId, int capacity)
        {
            if(courseId <= 0) throw new ArgumentOutOfRangeException(nameof(courseId),"Course ID must be greater than zero.");
            if (capacity <= 0) throw new ArgumentException("Capacity must be greater than zero.",nameof(capacity));
            CourseId = courseId;
            Capacity = capacity;
        }
    }
}
