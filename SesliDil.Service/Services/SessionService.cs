using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SesliDil.Core.Entities;
using SesliDil.Core.Interfaces;

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

        // ── Validasyon + Get
        public async Task<Session> GetByIdOrThrowAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Geçersiz id.");

            var session = await GetByIdAsync<string>(id);
            if (session is null)
                throw new KeyNotFoundException("Session bulunamadı.");

            return session;
        }

        // ── Token'lı oluşturma (mevcut kullanımın kalsın)
        public async Task<Session> CreateSessionAsync(
            string userId,
            string accessToken,
            string refreshToken,
            DateTime accessExp,
            DateTime refreshExp)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("UserId zorunludur.");

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
            return session;
        }

        // ── Sade DTO ile oluşturma (Controller/Swagger’dan basit create için)
        public async Task<Session> CreateFromDtoAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("UserId zorunludur.");

            var session = new Session
            {
                SessionId = Guid.NewGuid().ToString(),
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            await _sessionRepository.AddAsync(session);
            await _sessionRepository.SaveChangesAsync();
            return session;
        }

        // ── Validasyon + Silme
        public async Task DeleteByIdAsync(string id)
        {
            var session = await GetByIdOrThrowAsync(id);
            await _sessionRepository.DeleteAsync(session);
            await _sessionRepository.SaveChangesAsync();
        }

        // ── Refresh token sorgusu (mevcut kullanımın kalsın)
        public async Task<Session?> GetByRefreshTokenAsync(string refreshToken)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
                throw new ArgumentException("Refresh token zorunludur.");

            return await _sessionRepository.Query()
                .FirstOrDefaultAsync(s => s.RefreshToken == refreshToken);
        }
    }
}
