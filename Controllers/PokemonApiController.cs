[ApiController]
[Route("api/pokemon")] // La ruta base para este controlador
public class PokemonApiController : ControllerBase
{
    private readonly PokeApiService _pokeApiService;

    public PokemonApiController(PokeApiService pokeApiService)
    {
        _pokeApiService = pokeApiService;
    }

    // GET: api/pokemon
    [HttpGet]
    public async Task<IActionResult> GetPokemons([FromQuery] string? nameFilter, [FromQuery] string? speciesFilter, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
    {
        // ... (Aqu� va la misma l�gica que ten�as en Index para obtener y filtrar la lista de Pok�mon)

        // La �nica diferencia es lo que devuelves al final:
        var pokemonsResponse = await _pokeApiService.GetPokemons(pageSize, (pageNumber - 1) * pageSize);
        // ... l�gica de filtrado ...

        if (pokemonsResponse == null)
        {
            return NotFound();
        }

        // Devuelves los datos en formato JSON
        return Ok(pokemonsResponse.Results);
    }

    // GET: api/pokemon/pikachu
    [HttpGet("{name}")]
    public async Task<IActionResult> GetPokemonDetails(string name)
    {
        var details = await _pokeApiService.GetPokemonDetails(name);
        if (details == null)
        {
            return NotFound();
        }
        return Ok(details);
    }
}