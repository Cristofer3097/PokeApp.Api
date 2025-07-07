using PokeApp.Services;
var builder = WebApplication.CreateBuilder(args);

// Agrega los servicios para los controladores de la API
builder.Services.AddControllers();
builder.Services.AddMemoryCache(); // Registra el servicio de caché en memoria que usa tu PokeApiService.
builder.Services.AddHttpClient<PokeApiService>();

// Agrega la configuración de CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowMyFrontend", policy =>
    {
        // Acepta peticiones de AMBAS URLs del frontend
        policy.WithOrigins("https://localhost:7175", "http://localhost:5089")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseCors("AllowMyFrontend");
app.UseAuthorization();


app.MapControllers();

app.Run();
