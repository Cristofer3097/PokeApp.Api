using Microsoft.AspNetCore.Mvc; // Para [ApiController], ControllerBase, IActionResult, etc.
using PokeApp.Models;           // Para tus clases de modelos como Pokemon
using PokeApp.Services;         // Para tu clase PokeApiService
using MimeKit;
using MailKit.Net.Smtp;
using ClosedXML.Excel;
using Newtonsoft.Json;
using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;


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
        // 1. Obtenemos la lista completa de TODOS los pokémon
        var allPokemonsResponse = await _pokeApiService.GetPokemons(2000, 0);
        IEnumerable<PokemonListItem> pokemonListItems = allPokemonsResponse?.Results ?? new List<PokemonListItem>();

        // 2. Filtramos por nombre si es necesario
        if (!string.IsNullOrEmpty(nameFilter))
        {
            pokemonListItems = pokemonListItems
                .Where(p => p.Name.Contains(nameFilter, StringComparison.OrdinalIgnoreCase));
        }

        // (El filtro por especie se mantiene igual, pero lo aplicaremos después)
        var filteredList = pokemonListItems.ToList();

        // 3. Aplicamos paginación a la lista ya filtrada
        var totalFiltered = filteredList.Count;
        var pagedItems = filteredList.Skip((page - 1) * limit).Take(limit).ToList();

        // 4. Obtenemos los detalles COMPLETOS (incluyendo descripción) solo para la página actual
        var detailTasks = pagedItems.Select(async p =>
        {
            var details = await _pokeApiService.GetPokemonDetails(p.Name);
            if (details != null)
            {
                var species = await _pokeApiService.GetPokemonSpecies(p.Name);
                var description = species?.FlavorTextEntries
                                          .FirstOrDefault(f => f.Language?.Name == "es")?.FlavorText ??
                                  species?.FlavorTextEntries
                                          .FirstOrDefault(f => f.Language?.Name == "en")?.FlavorText ??
                                  "Descripción no disponible.";

                details.Description = description.Replace("\n", " ").Replace("\f", " ");
            }
            return details;
        });

        var fullDetailsOfPagedPokemons = (await Task.WhenAll(detailTasks)).Where(p => p != null).ToList();

        // 5. Si hay un filtro de especie, lo aplicamos ahora sobre los detalles completos
        if (!string.IsNullOrEmpty(speciesFilter) && speciesFilter.ToLower() != "all")
        {
            fullDetailsOfPagedPokemons = fullDetailsOfPagedPokemons
                .Where(p => p.Types.Any(t => t.Type.Name.Equals(speciesFilter, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

        var result = new
        {
            count = totalFiltered,
            totalPages = (int)Math.Ceiling((double)totalFiltered / limit),
            currentPage = page,
            results = fullDetailsOfPagedPokemons
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
        var details = await _pokeApiService.GetPokemonDetails(name);
        if (details == null)
        {
            return NotFound();
        }

        var species = await _pokeApiService.GetPokemonSpecies(name);

        var descriptionEntry = species?.FlavorTextEntries?
            .FirstOrDefault(f => f.Language?.Name?.ToLower() == "es" && !string.IsNullOrWhiteSpace(f.FlavorText))
            ?? species?.FlavorTextEntries?
            .FirstOrDefault(f => f.Language?.Name?.ToLower() == "en" && !string.IsNullOrWhiteSpace(f.FlavorText));

        var description = descriptionEntry?.FlavorText
            .Replace("\n", " ")
            .Replace("\f", " ")
            .Replace("\r", " ")
            .Replace("", " ")
            .Trim()
            ?? "Descripción no disponible.";

      
        details.Description = description;

        return Ok(details);
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

    [HttpGet("generation/{generationNumber}")]
    public async Task<IActionResult> GetPokemonsByGeneration(int generationNumber, [FromQuery] int limit = 40, [FromQuery] int offset = 0)
    {
        // 1. Obtenemos la lista COMPLETA de especies de la generación
        var generationData = await _pokeApiService.GetGeneration(generationNumber);
        if (generationData == null)
        {
            return NotFound($"No se encontró la generación {generationNumber}.");
        }

        var allSpeciesInGeneration = generationData.PokemonSpecies
            .OrderBy(s => {
                // Extraemos el ID de la URL para ordenar correctamente
                var parts = s.Url.TrimEnd('/').Split('/');
                return int.TryParse(parts.LastOrDefault(), out var id) ? id : int.MaxValue;
            })
            .ToList();

        var totalCount = allSpeciesInGeneration.Count;

        // 2. Aplicamos el 'limit' y 'offset' a la LISTA DE ESPECIES, no a los detalles
        var speciesForThisPage = allSpeciesInGeneration.Skip(offset).Take(limit).ToList();

        // 3. Obtenemos los detalles SÓLO para el lote actual de Pokémon
        var throttler = new SemaphoreSlim(10);
        var detailTasks = speciesForThisPage.Select(async species =>
        {
            await throttler.WaitAsync();
            try
            {
                var details = await _pokeApiService.GetPokemonDetails(species.Name);
                if (details != null)
                {
                    var speciesDetails = await _pokeApiService.GetPokemonSpecies(species.Name);
                    var description = speciesDetails?.FlavorTextEntries.FirstOrDefault(f => f.Language?.Name == "es")?.FlavorText ?? "Descripción no disponible.";
                    details.Description = description.Replace("\n", " ").Replace("\f", " ");
                    details.EggGroups = speciesDetails?.EggGroups ?? new List<EggGroup>();
                    details.Abilities = details.Abilities ?? new List<PokemonAbility>();
                }
                return details;
            }
            finally
            {
                throttler.Release();
            }
        });

        var pokemonsWithDetails = (await Task.WhenAll(detailTasks)).Where(p => p != null).ToList();

        // 4. Devolvemos el resultado paginado
        var result = new GenerationResult
        {
            TotalCount = totalCount,
            Pokemons = pokemonsWithDetails
        };

        return Ok(result);
    }


    [HttpGet("evolution-chain/{pokemonId}")]
    public async Task<IActionResult> GetEvolutionChain(int pokemonId)
    {
        // 1. Obtenemos la especie del Pokémon para encontrar la URL de su cadena evolutiva
        var species = await _pokeApiService.GetPokemonSpecies(pokemonId.ToString());
        if (string.IsNullOrEmpty(species?.EvolutionChain?.Url))
        {
            return Ok(new List<EvolutionStep>()); // Devuelve una lista vacía si no hay cadena
        }

        // 2. Obtenemos la cadena de evolución completa desde su URL
        var evolutionChain = await _pokeApiService.GetEvolutionChain(species.EvolutionChain.Url);
        if (evolutionChain?.Chain == null)
        {
            return Ok(new List<EvolutionStep>());
        }

        var evolutionSteps = new List<EvolutionStep>();
        var currentLink = evolutionChain.Chain;

        // 3. Recorremos la cadena de evolución de forma recursiva
        while (currentLink != null && currentLink.Species != null)
        {
            var pokemonDetails = await _pokeApiService.GetPokemonDetails(currentLink.Species.Name);
            if (pokemonDetails != null)
            {
                // El primer Pokémon no tiene detalles de cómo evolucionó, así que es null
                EvolutionDetail? evolutionDetailForThisStage = currentLink.EvolutionDetails.FirstOrDefault();

                evolutionSteps.Add(new EvolutionStep
                {
                    Pokemon = pokemonDetails,
                    EvolutionDetail = evolutionDetailForThisStage
                });
            }

            // Pasamos al siguiente eslabón de la cadena
            currentLink = currentLink.EvolvesTo.FirstOrDefault();
        }

        return Ok(evolutionSteps);
    }
    [HttpGet("ability/{name}")]
    public async Task<IActionResult> GetAbility(string name)
    {
        var abilityDetails = await _pokeApiService.GetAbilityDetails(name);
        if (abilityDetails == null)
        {
            return NotFound("Habilidad no encontrada.");
        }

        // Buscamos la descripción en español o inglés
        var description = abilityDetails.FlavorTextEntries
                            .FirstOrDefault(f => f.Language?.Name == "es")?.FlavorText
                          ?? abilityDetails.FlavorTextEntries
                            .FirstOrDefault(f => f.Language?.Name == "en")?.FlavorText
                          ?? "Descripción no disponible.";

        return Ok(new { name = abilityDetails.Name, description = description.Replace("\n", " ") });
    }

}