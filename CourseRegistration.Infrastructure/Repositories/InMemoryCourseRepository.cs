using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CourseRegistration.Application.Interfaces;
using CourseRegistration.Application.Models;

namespace CourseRegistration.Infrastructure.Repositories
{
    public class InMemoryCourseRepository : ICourseRepository
    {
        private readonly object _lock = new object();
        private readonly CourseRegistrationState _state;

        public InMemoryCourseRepository(CourseRegistrationState state)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
        }

        public TResult ExecuteAtomically<TResult>(Func<CourseRegistrationState, TResult> operation)
        {
            if (operation == null) throw new ArgumentNullException(nameof(operation));

            lock (_lock)
            {
                return operation(_state);
            }
        }
    }
}
