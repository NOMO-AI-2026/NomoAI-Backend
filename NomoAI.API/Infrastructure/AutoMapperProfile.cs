using AutoMapper;
using NomoAI.API.Domain.Entities;
using NomoAI.API.Features.Children.UpdateChildData;

namespace NomoAI.API.Infrastructure
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<Children, UpdateChildRequest>()
                .ReverseMap();
        }
    }
}
