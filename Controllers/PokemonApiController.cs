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
        // ... (Aquí va la misma lógica que tenías en Index para obtener y filtrar la lista de Pokémon)

        // La única diferencia es lo que devuelves al final:
        var pokemonsResponse = await _pokeApiService.GetPokemons(pageSize, (pageNumber - 1) * pageSize);
        // ... lógica de filtrado ...

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