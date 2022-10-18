using Book_API.Entities;
using Book_API.Models;

namespace Book_API.Services
{
    public interface IBookInfo
    {
        Task<(IEnumerable<Book>,PaginationMetadata)> GetBooksAsync(string? name, string? searchQuery,int pageNumber, int pageSize);

        Task<Book?> GetBookAsync(int Bookid);

        Task AddBookAsync(Book book);

        Task<Book> UpdateBookAsync(Book newBook, Book bookToUpdate);

        Task<bool> BookExistsAsync(int bookId);

        Task DeleteBookAsync(Book bookToDelete);

        Task SaveChangesAsync();
    }
}
