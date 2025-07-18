using AutoMapper;
using SesliDil.Core.DTOs;
using SesliDil.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SesliDil.Core.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<User, UserDto>().ReverseMap();
            CreateMap<Progress, ProgressDto>().ReverseMap();
            CreateMap<AIAgent, AIAgentDto>().ReverseMap();
            CreateMap<Conversation, ConversationDto>().ReverseMap();
            CreateMap<Message, MessageDto>().ReverseMap();
            CreateMap<FileStorage, FileStorageDto>().ReverseMap();
        }
    }
}
