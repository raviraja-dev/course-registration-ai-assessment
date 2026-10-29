using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CourseRegistration.Application.Models;
using CourseRegistration.Domain.Entities;

namespace CourseRegistration.Infrastructure.SampleData
{
    public static class Data
    {
        public static CourseRegistrationState CreateInitialState()
        {
            var course = new CourseOffering(1, 2);

            var state = new CourseRegistrationState(course);

            state.Learners.Add(101, new Learner(101, true));
            state.Learners.Add(102, new Learner(102, true));
            state.Learners.Add(103, new Learner(103, true));
            state.Learners.Add(104, new Learner(104, true));
            state.Learners.Add(105, new Learner(105, false));

            return state;
        }
    }
}
