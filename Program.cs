
using PokeApp.Services;

var builder = WebApplication.CreateBuilder(args);

// --- CONFIGURACIÓN DE SERVICIOS ---

// 1. Añade la política de CORS
var uiAppUrl = "https://localhost:7175";
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(
        policy =>
        {
            policy.WithOrigins(uiAppUrl)
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
    options.AddPolicy("AllowAll",
       policy =>
       {
           policy.AllowAnyOrigin() 
                 .AllowAnyHeader()
                 .AllowAnyMethod();
       });
});

builder.Services.AddControllers()
    .AddNewtonsoftJson(); 

builder.Services.AddMemoryCache();

builder.Services.AddHttpClient<PokeApiService>();
builder.Services.AddScoped<PokeApiService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


// --- CONSTRUCCIÓN Y CONFIGURACIÓN DE LA APLICACIÓN ---

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseCors("AllowAll");


app.UseHttpsRedirection();
app.UseCors();
app.UseAuthorization();
app.MapControllers();

app.Run();