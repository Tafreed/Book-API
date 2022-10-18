using Book_API.DBContexts;
using Book_API.Entities;
using Book_API.Models;
using Microsoft.EntityFrameworkCore;

namespace Book_API.Services
{
    public class BookInfoRepository : IBookInfo
    {
        private readonly BookContext _context;

        public BookInfoRepository(BookContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<IEnumerable<Book>> GetBooksAsync()
        {
            return await _context.Books.ToListAsync();
        }

        public async Task<(IEnumerable<Book>, PaginationMetadata)> GetBooksAsync(string? name, string? searchQuery, int pageNumber, int pageSize)
        {
            var collection = _context.Books as IQueryable<Book>;

            if (!string.IsNullOrEmpty(name))
            {
                name = name.Trim();
                collection = collection.Where(book => book.Title == name);
            }

            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                searchQuery = searchQuery.Trim();
                collection = collection.Where(book => book.Title.Contains(searchQuery)
                            || (book.Author.Contains(searchQuery))
                            || (book.YearOfPublish.ToString() == searchQuery));
            }

            var totalItemCount = await collection.CountAsync();
            var paginationMetadata = new PaginationMetadata(totalItemCount,pageSize,pageNumber);

            var collectionToReturn =  await collection.OrderBy(book => book.BookId)
                        .Skip(pageSize * (pageNumber-1))
                        .Take(pageSize)
                        .ToListAsync();

            return (collectionToReturn, paginationMetadata);
        }

        public async Task<Book?> GetBookAsync(int bookId)
        {
            return await _context.Books.FirstOrDefaultAsync(b => b.BookId == bookId);
        }

        public async Task AddBookAsync(Book book)
        {
            await _context.AddAsync(book);
            await SaveChangesAsync();
        }

        public async Task<Book> UpdateBookAsync(Book newBook, Book bookToUpdate)
        {
            var result = await _context.Books.FirstOrDefaultAsync(book => book.BookId == bookToUpdate.BookId);
            if (result != null)
            {
                result.Author = newBook.Author;
                result.Title = newBook.Title;
                result.YearOfPublish = newBook.YearOfPublish;

                await SaveChangesAsync();
                return result;
            }
            return null;
        }

        public async Task<bool> BookExistsAsync(int bookId)
        {
            return await _context.Books.AnyAsync(book => book.BookId == bookId);
        }

        public async Task DeleteBookAsync(Book bookToDelete)
        {
            _context.Remove(bookToDelete);
            await SaveChangesAsync();
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
