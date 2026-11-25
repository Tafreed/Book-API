using System.Reflection;
using System.Text;
using Book_API.DBContexts;
using Book_API.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("logs/bookslogs.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddControllers(configure =>
    configure.ReturnHttpNotAcceptable = true
).AddXmlDataContractSerializerFormatters();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
    {
        var xmlCommentsFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlCommentsFullPath = Path.Combine(AppContext.BaseDirectory, xmlCommentsFile);

        options.IncludeXmlComments(xmlCommentsFullPath);
        options.AddSecurityDefinition("BookAPIBearerAuth", new OpenApiSecurityScheme()
        {
            Type = SecuritySchemeType.Http,
            Scheme = "Bearer",
            Description = "Input a valid token to access this API"
        });

        options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("BookAPIBearerAuth", document)] = new List<string>()
        });
    });

builder.Services.AddDbContext<BookContext>(dbContextOptions =>
    dbContextOptions.UseSqlServer(builder.Configuration.GetConnectionString("BookContext")));

builder.Services.AddScoped<IBookInfo, BookInfoRepository>();

# if DEBUG
builder.Services.AddTransient<IMailService, LocalMailService>();
# else
//builder.Services.AddTransient<IMailService, CloudMailService>();
# endif

builder.Services.AddAutoMapper(cfg => cfg.AddMaps(typeof(Program).Assembly));

builder.Services.AddCors();

builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new()
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = builder.Configuration["Authentication:Issuer"],
                ValidAudience = builder.Configuration["Authentication:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.ASCII.GetBytes(builder.Configuration["Authentication:SecretKey"]))
            };
        });

builder.Services.AddApiVersioning(options =>
    {
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.DefaultApiVersion = new Microsoft.AspNetCore.Mvc.ApiVersion(1, 0);
        options.ReportApiVersions = true;
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();      //Middleware to generate OpenAPI specification
    app.UseSwaggerUI();    //Generates the swagger UI
}

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.UseCors(
        options => options.WithOrigins("http://localhost:4200").AllowAnyMethod().AllowAnyHeader()
    );

app.Run();
