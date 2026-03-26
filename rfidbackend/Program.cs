using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Rfid.WebApi.Data;
using Rfid.WebApi.Entities;
using Rfid.WebApi.Repositories;
using Rfid.WebApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<RfidDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("RfidDb")));
//var cs = builder.Configuration.GetConnectionString("RfidDb");
//try
//{
//    using var sql = new SqlConnection(cs);
//    sql.Open();
//    Console.WriteLine("RfidDb: Conexión a BD OK.");
//}
//catch (Exception ex)
//{
//    Console.WriteLine($"RfidDb: ERROR al conectar a BD: {ex.Message}");
//    Environment.Exit(1); // detiene la app si no hay conexión
//}
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IToolRepository, ToolRepository>();
builder.Services.AddScoped<ITicketRepository, TicketRepository>();
builder.Services.AddScoped<IToolAssignmentRepository, ToolAssignmentRepository>();
builder.Services.AddScoped<IToolRemovalRepository, ToolRemovalRepository>();
builder.Services.AddScoped<IRfidScanRecordRepository, RfidScanRecordRepository>();

builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IToolService, ToolService>();
builder.Services.AddScoped<ITicketService, TicketService>();
builder.Services.AddScoped<IToolAssignmentService, ToolAssignmentService>();
builder.Services.AddScoped<IRfidScanRecordService, RfidScanRecordService>();
builder.Services.AddScoped(typeof(ICatalogService<>), typeof(CatalogService<>));

builder.Services.AddControllers()
.AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? ["http://localhost:5173"];
        policy.WithOrigins(origins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowFrontend");

app.UseAuthorization();

app.MapControllers();

app.Run();
