using AutoMapper;
using SesliDil.Core.DTOs;
using SesliDil.Core.Entities;
using SesliDil.Core.Interfaces;
using SesliDil.Service.Interfaces;

namespace SesliDil.Service.Services
{
    public class UserService : Service<User>, IService<User>
    {
        private readonly IRepository<User> _userRepository;
        private readonly IMapper _mapper;

        public UserService(IRepository<User> userRepository, IMapper mapper)
            : base(userRepository)
        {
            _userRepository = userRepository;
            _mapper = mapper;
        }

        public async Task<User> GetUserByIdAsync(int id)
        {
            if (id <= 0) throw new ArgumentException("Invalid user ID");

            var user = await _userRepository.GetByIdAsync(id);
            if (user == null) throw new Exception("User not found");

            return user;
        }
    }
    //public class UserService : Service<User>, IUserService
    //{
    //    private readonly IRepository<User> _userRepository;
    //    private readonly IMapper _mapper;

    //    public UserService(IRepository<User> userRepository, IMapper mapper)
    //        : base(userRepository, mapper)
    //    {
    //        _userRepository = userRepository;
    //        _mapper = mapper;
    //    }

    //    public async Task<UserDto> GetUserByIdAsync(int userId)
    //    {
    //        var user = await _userRepository.GetByIdAsync(userId);
    //        if (user == null)
    //            throw new Exception("User not found");

    //        return _mapper.Map<UserDto>(user);
    //    }
    //}
}
