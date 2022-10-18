using Book_API.Entities;
using Book_API.Models;
using Microsoft.EntityFrameworkCore;

namespace Book_API.DBContexts
{
    public class BookContext : DbContext
    {
        public BookContext(DbContextOptions<BookContext> options)  : base(options) { }
        public DbSet<Book> Books { get; set; } = null!; //Collection of all entities in this context
        protected override void OnModelCreating(ModelBuilder modelbuilder)
        {
            modelbuilder.Entity<Book>().HasData(
                new Book() { BookId = 1, Title = "Robinson Crusoe", Author = "Daniel Dafoe", YearOfPublish = 1719 },
                new Book() { BookId = 2, Title = "A Tale of Two Cities", Author = "Charles Dickens", YearOfPublish = 1895 },
                new Book() { BookId = 3, Title = "The Invisible Man", Author = "H.G. Wells", YearOfPublish = 1897 });
            
            base.OnModelCreating(modelbuilder);
        }
    }
}
