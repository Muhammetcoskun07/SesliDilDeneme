using AutoMapper;
using Microsoft.EntityFrameworkCore;
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
        private readonly IRepository<Session> _sessionRepository;

        public SessionService(IRepository<Session> repository, IMapper mapper)
            : base(repository, mapper)
        {
            _sessionRepository = repository;
        }

        public async Task CreateSessionAsync(string userId, string accessToken, string refreshToken, DateTime accessExp, DateTime refreshExp)
        {
            var session = new Session
            {
                SessionId = Guid.NewGuid().ToString(),
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                AccessTokenExpiresAt = accessExp,
                RefreshTokenExpiresAt = refreshExp
            };

            await _sessionRepository.AddAsync(session);
            await _sessionRepository.SaveChangesAsync();
        }
        public async Task<Session> GetByRefreshTokenAsync(string refreshToken)
        {
            return await _sessionRepository.Query()
                .FirstOrDefaultAsync(s => s.RefreshToken == refreshToken);
        }

    }

}
