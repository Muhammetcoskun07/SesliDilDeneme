
using AutoMapper;
using SesliDil.Core.DTOs;
using SesliDil.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SesliDil.Core.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<User, UserDto>()
                .ForMember(dest => dest.LearningGoals,
                    opt => opt.MapFrom(src => JsonToArray(src.LearningGoals)))
                .ForMember(dest => dest.ImprovementGoals,
                    opt => opt.MapFrom(src => JsonToArray(src.ImprovementGoals)))
                .ForMember(dest => dest.TopicInterests,
                    opt => opt.MapFrom(src => JsonToArray(src.TopicInterests)))
                .ReverseMap()
                .ForMember(dest => dest.LearningGoals,
                    opt => opt.MapFrom(src => ArrayToJson(src.LearningGoals)))
                .ForMember(dest => dest.ImprovementGoals,
                    opt => opt.MapFrom(src => ArrayToJson(src.ImprovementGoals)))
                .ForMember(dest => dest.TopicInterests,
                    opt => opt.MapFrom(src => ArrayToJson(src.TopicInterests)));

            CreateMap<Progress, ProgressDto>().ReverseMap();
            CreateMap<AIAgent, AIAgentDto>().ReverseMap();
            CreateMap<Conversation, ConversationDto>().ReverseMap();
            CreateMap<Message, MessageDto>().ReverseMap();
            CreateMap<FileStorage, FileStorageDto>().ReverseMap();
            CreateMap<Session, SessionDto>().ReverseMap();
            CreateMap<Conversation, ConversationSummaryDto>().ReverseMap();

        }

        private static string[] JsonToArray(JsonDocument? json)
        {
            if (json == null) return Array.Empty<string>();
            return json.RootElement.EnumerateArray().Select(e => e.GetString()).Where(s => s != null).ToArray()!;
        }

        private static JsonDocument ArrayToJson(string[] array)
        {
            var json = JsonSerializer.Serialize(array);
            return JsonDocument.Parse(json);
        }
    }

}
