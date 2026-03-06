using LTUDAPI.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// 1. Cấu hình Database (SQL Server)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. Cấu hình CORS - Cho phép Frontend (Next.js) truy cập API
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowNextJS", policy =>
    {
        policy.WithOrigins("http://localhost:3000") // Port mặc định của Next.js
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// 3. Cấu hình Controllers và xử lý JSON (tránh vòng lặp Reference)
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });

// 4. Cấu hình Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "PBL3 Reminder API", Version = "v1" });
});

var app = builder.Build();

// 5. Cấu hình HTTP request pipeline
// Luôn bật Swagger kể cả trong môi trường Production để bạn dễ code
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "PBL3 API V1");
    c.RoutePrefix = "swagger"; // Truy cập tại http://localhost:5000/swagger
});

// Kích hoạt CORS
app.UseCors("AllowNextJS");

app.UseAuthorization();

app.MapControllers();

// Chạy ứng dụng
app.Run();