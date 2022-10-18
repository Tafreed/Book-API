using AutoMapper;

namespace Book_API.Profiles
{
    public class BookProfile : Profile
    {
        public BookProfile()
        {
            CreateMap<Entities.Book, Models.BookDto>();
            CreateMap<Models.BookDto, Entities.Book>();
            CreateMap<Entities.Book, Models.BookForCreationDto>();
            CreateMap<Models.BookForCreationDto, Entities.Book>();
            CreateMap<Entities.Book, Models.BookForUpdateDto>();
            CreateMap<Models.BookForUpdateDto, Entities.Book>();
        }
    }
}
