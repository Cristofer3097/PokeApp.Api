using Microsoft.AspNetCore.Mvc; // Para [ApiController], ControllerBase, IActionResult, etc.
using PokeApp.Models;           // Para tus clases de modelos como Pokemon
using PokeApp.Services;         // Para tu clase PokeApiService
using MimeKit;
using MailKit.Net.Smtp;
using ClosedXML.Excel;


[ApiController]
[Route("api/pokemon")] // La ruta base para este controlador
public class PokemonApiController : ControllerBase
{
    private readonly PokeApiService _pokeApiService;
    private readonly IConfiguration _configuration; // Añadir IConfiguration

    public PokemonApiController(PokeApiService pokeApiService, IConfiguration configuration)
    {
        _pokeApiService = pokeApiService;
        _configuration = configuration; // Asignar
    }
    [HttpGet]
    public async Task<IActionResult> GetPokemons(
    [FromQuery] string? nameFilter,
    [FromQuery] string? speciesFilter,
    [FromQuery] int page = 1,
    [FromQuery] int limit = 20)
    {
        var allPokemonsResponse = await _pokeApiService.GetPokemons(2000, 0);
        var pokemonListItems = allPokemonsResponse?.Results ?? new List<PokemonListItem>();

        // 1. Filtrar por nombre desde la lista básica
        if (!string.IsNullOrEmpty(nameFilter))
        {
            pokemonListItems = pokemonListItems
                .Where(p => p.Name.Contains(nameFilter, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        // 2. Obtener las primeras especies desde el nombre
        if (!string.IsNullOrEmpty(speciesFilter) && speciesFilter.ToLower() != "all")
        {
            // Obtener detalles mínimos necesarios (sólo para filtrado)
            var speciesFiltered = new List<PokemonListItem>();
            var tasks = pokemonListItems.Select(async item =>
            {
                var details = await _pokeApiService.GetPokemonDetails(item.Name);
                if (details != null &&
                    details.Types.Any(t => t.Type.Name.Equals(speciesFilter, StringComparison.OrdinalIgnoreCase)))
                {
                    lock (speciesFiltered)
                        speciesFiltered.Add(item);
                }
            });

            await Task.WhenAll(tasks);
            pokemonListItems = speciesFiltered;
        }

        // 3. Paginación sobre lista filtrada
        var totalFiltered = pokemonListItems.Count;
        var pagedItems = pokemonListItems.Skip((page - 1) * limit).Take(limit).ToList();

        // 4. Obtener detalles SOLO de los paginados
        var detailTasks = pagedItems
            .Select(p => _pokeApiService.GetPokemonDetails(p.Name))
            .ToList();

        var details = await Task.WhenAll(detailTasks);
        var finalDetails = details.Where(p => p != null).ToList();

        var result = new
        {
            count = totalFiltered,
            totalPages = (int)Math.Ceiling((double)totalFiltered / limit),
            currentPage = page,
            results = finalDetails
        };

        return Ok(result);
    }

    // MÉTODO PARA LLENAR EL DROPDOWN DE ESPECIES
    [HttpGet("types")]
    public async Task<IActionResult> GetPokemonTypes()
    {
        var types = await _pokeApiService.GetPokemonTypes();
        return Ok(types);
    }



    // GET: api/pokemon
    [HttpGet("{name}")]
    public async Task<IActionResult> GetPokemonDetails(string name)
    {
        // Obtener detalles básicos
        var details = await _pokeApiService.GetPokemonDetails(name);
        if (details == null)
        {
            return NotFound();
        }

        // Obtener detalles de la especie para la descripción
        var species = await _pokeApiService.GetPokemonSpecies(name);

        // Buscar la descripción en español o inglés
        var description = species?.FlavorTextEntries?
                                  .FirstOrDefault(f => f.Language?.Name == "es")?.FlavorText ??
                          species?.FlavorTextEntries?
                                  .FirstOrDefault(f => f.Language?.Name == "en")?.FlavorText ??
                          "Descripción no disponible.";

        // Objeto anónimo para combinar toda la información y enviarla como JSON
        var result = new
        {
            id = details.Id,
            name = details.Name,
            sprites = details.Sprites,
            types = details.Types,
            description = description.Replace("\n", " ").Replace("\f", " ") // Limpiamos saltos de línea
        };

        return Ok(result);
    }
    [HttpGet("export")]
    public async Task<IActionResult> ExportToExcel([FromQuery] string? nameFilter, [FromQuery] string? speciesFilter)
    {
        try
        {
            var allResponse = await _pokeApiService.GetPokemons(2000, 0);
            var baseList = allResponse?.Results ?? new List<PokemonListItem>();

            // 1. Filtro por nombre
            if (!string.IsNullOrEmpty(nameFilter))
            {
                baseList = baseList
                    .Where(p => p.Name.Contains(nameFilter, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            // 2. Filtro por especie (si aplica), evitando cargar detalles de 2000 pokémon
            if (!string.IsNullOrEmpty(speciesFilter) && speciesFilter.ToLower() != "all")
            {
                var filteredBySpecies = new List<PokemonListItem>();
                var throttler = new SemaphoreSlim(5); // Máximo 5 hilos concurrentes
                var tasks = baseList.Select(async item =>
                {
                    await throttler.WaitAsync();
                    try
                    {
                        var details = await _pokeApiService.GetPokemonDetails(item.Name);
                        if (details != null &&
                            details.Types.Any(t => t.Type.Name.Equals(speciesFilter, StringComparison.OrdinalIgnoreCase)))
                        {
                            lock (filteredBySpecies) filteredBySpecies.Add(item);
                        }
                    }
                    finally
                    {
                        throttler.Release();
                    }
                });

                await Task.WhenAll(tasks);
                baseList = filteredBySpecies;
            }

            // 3. Cargar detalles de los filtrados (con concurrencia limitada)
            var pokemonsToExport = new List<Pokemon>();
            var throttled = new SemaphoreSlim(5);
            var detailTasks = baseList.Select(async item =>
            {
                await throttled.WaitAsync();
                try
                {
                    var details = await _pokeApiService.GetPokemonDetails(item.Name);
                    if (details != null)
                    {
                        lock (pokemonsToExport) pokemonsToExport.Add(details);
                    }
                }
                finally
                {
                    throttled.Release();
                }
            });

            await Task.WhenAll(detailTasks);

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Pokémon");

            worksheet.Cell(1, 1).Value = "ID";
            worksheet.Cell(1, 2).Value = "Nombre";
            worksheet.Cell(1, 3).Value = "Especie";

            int row = 2;
            foreach (var p in pokemonsToExport)
            {
                worksheet.Cell(row, 1).Value = p.Id;
                worksheet.Cell(row, 2).Value = p.Name;
                worksheet.Cell(row, 3).Value = string.Join(", ", p.Types.Select(t => t.Type.Name));
                row++;
            }

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var content = stream.ToArray();

            return File(content,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "Pokemons.xlsx");
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error al exportar a Excel: {ex.Message}");
        }
    }
    [HttpPost("send-email")] // Eviar a correo api/pokemon/send-email
    public async Task<IActionResult> SendEmail([FromBody] EmailRequest request)
    {
        try
        {
            var senderEmail = _configuration["SmtpSettings:SenderEmail"];
            var senderPassword = _configuration["SmtpSettings:SenderPassword"];
            var smtpHost = _configuration["SmtpSettings:SmtpHost"];
            var smtpPort = int.Parse(_configuration["SmtpSettings:SmtpPort"]!);

            if (string.IsNullOrEmpty(senderEmail) || string.IsNullOrEmpty(senderPassword))
            {
                return BadRequest(new { message = "Error de configuración SMTP. Revisa appsettings.json." });
            }

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Pokemon App", senderEmail));
            message.To.Add(new MailboxAddress("", request.EmailAddress));
            message.Subject = request.Subject;

            var builder = new BodyBuilder();

            // Caso 1: Enviar detalles de un Pokémon específico
            if (!string.IsNullOrEmpty(request.PokemonName))
            {
                builder.HtmlBody = $@"
                <h1>Detalles de {request.PokemonName}</h1>
                <img src='{request.PokemonImage}' alt='Imagen de {request.PokemonName}' width='150' />
                <p><strong>ID:</strong> {request.PokemonId}</p>
                <p><strong>Especie(s):</strong> {request.PokemonTypes}</p>
                <hr>
                <p>{request.Body}</p>";
            }
            // Caso 2: Enviar la lista completa con un archivo Excel adjunto
            else
            {
                builder.HtmlBody = request.Body;

                // La lógica para generar el Excel es la misma que para la exportación
                var pokemonsResponse = await _pokeApiService.GetPokemons(2000, 0);
                var pokemonList = pokemonsResponse?.Results ?? new List<PokemonListItem>();

                if (!string.IsNullOrEmpty(request.NameFilter))
                {
                    pokemonList = pokemonList.Where(p => p.Name.Contains(request.NameFilter, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                var pokemonsToExport = new List<Pokemon>();
                foreach (var item in pokemonList)
                {
                    var pokemonDetails = await _pokeApiService.GetPokemonDetails(item.Name);
                    if (pokemonDetails != null) pokemonsToExport.Add(pokemonDetails);
                }

                if (!string.IsNullOrEmpty(request.SpeciesFilter) && request.SpeciesFilter != "all")
                {
                    pokemonsToExport = pokemonsToExport.Where(p => p.Types.Any(t => t.Type.Name.Equals(request.SpeciesFilter, StringComparison.OrdinalIgnoreCase))).ToList();
                }

                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("Pokémon");
                    worksheet.Cell(1, 1).Value = "ID";
                    worksheet.Cell(1, 2).Value = "Nombre";
                    worksheet.Cell(1, 3).Value = "Especie";
                    int row = 2;
                    foreach (var p in pokemonsToExport)
                    {
                        worksheet.Cell(row, 1).Value = p.Id;
                        worksheet.Cell(row, 2).Value = p.Name;
                        worksheet.Cell(row, 3).Value = string.Join(", ", p.Types.Select(t => t.Type.Name));
                        row++;
                    }
                    using (var stream = new MemoryStream())
                    {
                        workbook.SaveAs(stream);
                        builder.Attachments.Add("Pokemons.xlsx", stream.ToArray(), ContentType.Parse("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"));
                    }
                }
            }

            message.Body = builder.ToMessageBody();

            using (var client = new SmtpClient())
            {
                client.ServerCertificateValidationCallback = (s, c, h, e) => true;
                await client.ConnectAsync(smtpHost, smtpPort, MailKit.Security.SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(senderEmail, senderPassword);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);
            }

            return Ok(new { message = "Correo enviado exitosamente!" });
        }
        catch (Exception ex)
        {
            // Devolvemos un error 500 con el mensaje para depuración
            return StatusCode(500, new { message = $"Error al enviar el correo: {ex.Message}" });
        }
    }
}