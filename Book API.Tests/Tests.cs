using System.Text.Json;
using AutoMapper;
using Book_API.Controllers;
using Book_API.DBContexts;
using Book_API.Entities;
using Book_API.Models;
using Book_API.Profiles;
using Book_API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using System.IdentityModel.Tokens.Jwt;

namespace Book_API.Tests;

static class Helpers
{
    public static BookContext NewContext()
    {
        var options = new DbContextOptionsBuilder<BookContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        return new BookContext(options);
    }

    public static IMapper Mapper()
    {
        var cfg = new MapperConfiguration(c => c.AddProfile<BookProfile>(), NullLoggerFactory.Instance);
        return cfg.CreateMapper();
    }

    public static IConfiguration Config() => new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["Authentication:SecretKey"] = "unit-test-signing-key-0123456789-abcdef",
        ["Authentication:Issuer"] = "issuer",
        ["Authentication:Audience"] = "aud",
        ["MailSettings:MailToAddress"] = "to@example.com",
        ["MailSettings:MailFromAddress"] = "from@example.com",
    }).Build();
}

public class RepositoryTests
{
    [Fact]
    public void Ctor_NullContext_Throws() =>
        Assert.Throws<ArgumentNullException>(() => new BookInfoRepository(null!));

    [Fact]
    public async Task GetBooks_ReturnsSeedData()
    {
        using var ctx = Helpers.NewContext();
        await ctx.Database.EnsureCreatedAsync();
        var repo = new BookInfoRepository(ctx);
        Assert.Equal(3, (await repo.GetBooksAsync()).Count());
    }

    [Fact]
    public async Task GetBooksPaged_FiltersAndPaginates()
    {
        using var ctx = Helpers.NewContext();
        await ctx.Database.EnsureCreatedAsync();
        var repo = new BookInfoRepository(ctx);

        var (all, meta) = await repo.GetBooksAsync(null, null, 1, 2);
        Assert.Equal(2, all.Count());
        Assert.Equal(3, meta.TotalItemCount);
        Assert.Equal(2, meta.TotalPageCount);

        var (byName, _) = await repo.GetBooksAsync(" Robinson Crusoe ", null, 1, 10);
        Assert.Single(byName);

        var (byAuthor, _) = await repo.GetBooksAsync(null, "Dickens", 1, 10);
        Assert.Single(byAuthor);

        var (byYear, _) = await repo.GetBooksAsync(null, "1897", 1, 10);
        Assert.Single(byYear);
    }

    [Fact]
    public async Task AddGetExistsDelete_RoundTrip()
    {
        using var ctx = Helpers.NewContext();
        var repo = new BookInfoRepository(ctx);
        await repo.AddBookAsync(new Book { Title = "T", Author = "A", YearOfPublish = 2000 });
        var added = (await repo.GetBooksAsync()).Single();
        Assert.True(await repo.BookExistsAsync(added.BookId));
        Assert.NotNull(await repo.GetBookAsync(added.BookId));
        await repo.DeleteBookAsync(added);
        Assert.False(await repo.BookExistsAsync(added.BookId));
        Assert.Null(await repo.GetBookAsync(added.BookId));
    }

    [Fact]
    public async Task Update_ExistingAndMissing()
    {
        using var ctx = Helpers.NewContext();
        var repo = new BookInfoRepository(ctx);
        await repo.AddBookAsync(new Book { Title = "T", Author = "A", YearOfPublish = 2000 });
        var existing = (await repo.GetBooksAsync()).Single();

        var updated = await repo.UpdateBookAsync(new Book { Title = "N", Author = "B", YearOfPublish = 2001 }, existing);
        Assert.Equal("N", updated.Title);
        Assert.Equal(2001, updated.YearOfPublish);

        var missing = await repo.UpdateBookAsync(new Book(), new Book { BookId = 999 });
        Assert.Null(missing);
    }
}

public class PaginationMetadataTests
{
    [Theory]
    [InlineData(0, 10, 0)]
    [InlineData(10, 10, 1)]
    [InlineData(11, 10, 2)]
    public void TotalPageCount_IsCeiling(int items, int size, int pages)
    {
        var m = new PaginationMetadata(items, size, 1);
        Assert.Equal(pages, m.TotalPageCount);
        Assert.Equal(1, m.CurrentPage);
        Assert.Equal(size, m.PageSize);
    }
}

public class MailServiceTests
{
    [Fact]
    public void Send_WritesToConsole()
    {
        var sw = new StringWriter();
        var orig = Console.Out;
        Console.SetOut(sw);
        try { new LocalMailService(Helpers.Config()).Send("subj", "msg"); }
        finally { Console.SetOut(orig); }
        var o = sw.ToString();
        Assert.Contains("to@example.com", o);
        Assert.Contains("subj", o);
        Assert.Contains("msg", o);
    }
}

public class MappingTests
{
    [Fact]
    public void Profile_IsValid_AndMaps()
    {
        var mapper = Helpers.Mapper();
        var dto = mapper.Map<BookDto>(new Book { BookId = 1, Title = "T", Author = "A", YearOfPublish = 5 });
        Assert.Equal("T", dto.Title);
    }
}

public class AuthControllerTests
{
    [Fact]
    public void Ctor_NullConfig_Throws() =>
        Assert.Throws<ArgumentNullException>(() => new AuthController(null!));

    [Fact]
    public void Authenticate_ReturnsJwt()
    {
        var c = new AuthController(Helpers.Config());
        var result = c.Authenticate(new AuthenticationRequestBody { UserName = "bob", Password = "x" });
        var token = Assert.IsType<string>(Assert.IsType<OkObjectResult>(result.Result).Value);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        Assert.Equal("issuer", jwt.Issuer);
        Assert.Contains(jwt.Claims, x => x.Type == "given_name" && x.Value == "bob");
    }

    [Fact]
    public void Authenticate_NoUserName_UsesDefault()
    {
        var c = new AuthController(Helpers.Config());
        var result = c.Authenticate(new AuthenticationRequestBody());
        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public void ApiUser_StoresValues()
    {
        var u = new ApiUser(7, "n");
        Assert.Equal(7, u.UserId);
        Assert.Equal("n", u.UserName);
    }
}

public class BookApiControllerTests
{
    private readonly IBookInfo _repo = Substitute.For<IBookInfo>();
    private readonly IMailService _mail = Substitute.For<IMailService>();

    private BookApiController Create()
    {
        var c = new BookApiController(NullLogger<BookApiController>.Instance, _repo, _mail, Helpers.Mapper());
        c.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        return c;
    }

    [Fact]
    public void Ctor_NullArgs_Throw()
    {
        var m = Helpers.Mapper();
        var l = NullLogger<BookApiController>.Instance;
        Assert.Throws<ArgumentNullException>(() => new BookApiController(null!, _repo, _mail, m));
        Assert.Throws<ArgumentNullException>(() => new BookApiController(l, null!, _mail, m));
        Assert.Throws<ArgumentNullException>(() => new BookApiController(l, _repo, null!, m));
        Assert.Throws<ArgumentNullException>(() => new BookApiController(l, _repo, _mail, null!));
    }

    [Fact]
    public async Task GetBooks_CapsPageSizeAndSetsHeader()
    {
        _repo.GetBooksAsync(null, null, 1, 20).Returns((new[] { new Book { Title = "A" } }.AsEnumerable(), new PaginationMetadata(1, 20, 1)));
        var c = Create();
        var result = await c.GetBooks(null, null, 1, 500);
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Single(Assert.IsAssignableFrom<IEnumerable<BookDto>>(ok.Value));
        Assert.True(c.Response.Headers.ContainsKey("Pagination"));
        var meta = JsonSerializer.Deserialize<PaginationMetadata>(c.Response.Headers["Pagination"].ToString());
        Assert.Equal(1, meta!.TotalItemCount);
    }

    [Fact]
    public async Task GetBook_FoundAndNotFound()
    {
        _repo.GetBookAsync(1).Returns(new Book { BookId = 1 });
        _repo.GetBookAsync(2).Returns((Book?)null);
        var c = Create();
        Assert.IsType<OkObjectResult>((await c.GetBook(1)).Result);
        Assert.IsType<NotFoundResult>((await c.GetBook(2)).Result);
    }

    [Fact]
    public async Task AddBook_SavesAndSendsMail()
    {
        var c = Create();
        var r = await c.AddBook(new BookForCreationDto { Title = "T", Author = "A", YearOfPublish = 1 });
        Assert.IsType<OkObjectResult>(r.Result);
        await _repo.Received(1).AddBookAsync(Arg.Any<Book>());
        _mail.Received(1).Send(Arg.Any<string>(), Arg.Is<string>(s => s.Contains("T")));
    }

    [Fact]
    public async Task UpdateBook_NotFound()
    {
        _repo.BookExistsAsync(1).Returns(false);
        Assert.IsType<NotFoundResult>(await Create().UpdateBook(1, new BookForUpdateDto()));
    }

    [Fact]
    public async Task UpdateBook_Success()
    {
        _repo.BookExistsAsync(1).Returns(true);
        _repo.GetBookAsync(1).Returns(new Book { BookId = 1 });
        Assert.IsType<NoContentResult>(await Create().UpdateBook(1, new BookForUpdateDto { Title = "x" }));
        await _repo.Received(1).UpdateBookAsync(Arg.Any<Book>(), Arg.Any<Book>());
    }

    [Fact]
    public async Task UpdateBook_Exception_Returns500()
    {
        _repo.BookExistsAsync(1).Returns<bool>(_ => throw new InvalidOperationException());
        var r = Assert.IsType<ObjectResult>(await Create().UpdateBook(1, new BookForUpdateDto()));
        Assert.Equal(500, r.StatusCode);
    }

    [Fact]
    public async Task DeleteBook_FoundAndNotFound()
    {
        var b = new Book { BookId = 1 };
        _repo.GetBookAsync(1).Returns(b);
        _repo.GetBookAsync(2).Returns((Book?)null);
        var c = Create();
        Assert.IsType<NoContentResult>(await c.DeleteBook(1));
        await _repo.Received(1).DeleteBookAsync(b);
        Assert.IsType<NotFoundObjectResult>(await c.DeleteBook(2));
    }
}
