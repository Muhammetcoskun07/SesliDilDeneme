using AutoMapper;
using SesliDil.Core.Entities;
using SesliDil.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SesliDil.Service.Services
{
    public class SessionService : Service<Session>
    {
        public SessionService(IRepository<Session> repository, IMapper mapper)
            : base(repository, mapper)
        {
        }

    }

}
