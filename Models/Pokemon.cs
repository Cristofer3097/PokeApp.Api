using Newtonsoft.Json;
using System.Collections.Generic;
namespace PokeApp.Models
{
    public class EmailRequest
    {
        // Datos básicos del correo
        public string? EmailAddress { get; set; }
        public string? Subject { get; set; }
        public string? Body { get; set; }

        // Datos opcionales para enviar detalles de un Pokémon
        public string? PokemonName { get; set; }
        public int PokemonId { get; set; }
        public string? PokemonTypes { get; set; }
        public string? PokemonImage { get; set; }

        // Datos opcionales para adjuntar la lista filtrada
        public string? NameFilter { get; set; }
        public string? SpeciesFilter { get; set; }
    }
    //Obtencion de datos pokemon
    public class Pokemon
    {
        [JsonProperty("id")]
        public int Id { get; set; }
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;
        [JsonProperty("sprites")]
        public PokemonSprites Sprites { get; set; } = new PokemonSprites();
        [JsonProperty("types")]
        public List<PokemonType> Types { get; set; } = new List<PokemonType>();
        [JsonProperty("stats")]
        public List<Stat> Stats { get; set; } = new List<Stat>();
        [JsonProperty("height")]
        public int Height { get; set; }
        [JsonProperty("weight")]
        public int Weight { get; set; }
        public string? Description { get; set; }

        [JsonProperty("abilities")]
        public List<PokemonAbility> Abilities { get; set; } = new List<PokemonAbility>();

        public List<EggGroup> EggGroups { get; set; } = new List<EggGroup>();

    }
    public class PokemonSprites
    {
        [JsonProperty("front_default")]
        public string? FrontDefault { get; set; }

        [JsonProperty("front_female")]
        public string? FrontFemale { get; set; }

        [JsonProperty("front_shiny")]
        public string? FrontShiny { get; set; }

        [JsonProperty("front_shiny_female")]
        public string? FrontShinyFemale { get; set; }

        [JsonProperty("other")]
        public OtherSprites? Other { get; set; }

        [JsonProperty("versions")]
        public VersionsSprites? Versions { get; set; }
    }
    public class VersionsSprites
    {
        [JsonProperty("generation-v")]
        public GenerationV? GenerationV { get; set; }
    }
    public class GenerationV
    {
        [JsonProperty("black-white")]
        public BlackWhiteSprites? BlackWhite { get; set; }
    }
    public class BlackWhiteSprites
    {
        [JsonProperty("animated")]
        public AnimatedSprites? Animated { get; set; }
    }

    public class AnimatedSprites
    {
        [JsonProperty("front_default")]
        public string? FrontDefault { get; set; }
    }
    public class OtherSprites
    {
        [JsonProperty("official-artwork")]
        public OfficialArtwork? OfficialArtwork { get; set; }
    }

    public class OfficialArtwork
    {
        [JsonProperty("front_default")]
        public string? FrontDefault { get; set; }
    }


    public class PokemonType
    {
        public TypeInfo? Type { get; set; } // Puede ser nulo
    }

    public class TypeInfo
    {
        public string Name { get; set; } = string.Empty; // Inicializa
        public string Url { get; set; } = string.Empty; // Inicializa
    }

    public class PokemonListResponse
    {
        public int Count { get; set; }
        public string? Next { get; set; } // Puede ser nulo
        public string? Previous { get; set; } // Puede ser nulo
        public List<PokemonListItem> Results { get; set; } = new List<PokemonListItem>(); // Inicializa

    }

    public class PokemonListItem
    {
        public string Name { get; set; } = string.Empty; // Inicializa
        public string Url { get; set; } = string.Empty; // Inicializa
    }

    public class PokemonSpecies
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        [JsonProperty("flavor_text_entries")]
        public List<FlavorTextEntry> FlavorTextEntries { get; set; } = new List<FlavorTextEntry>();

        [JsonProperty("evolution_chain")]
        public EvolutionChainUrl? EvolutionChain { get; set; }

        [JsonProperty("egg_groups")]
        public List<EggGroup> EggGroups { get; set; } = new List<EggGroup>();
    }

    public class EvolutionChainUrl
    {
        public string Url { get; set; } = string.Empty;
    }
    public class FlavorTextEntry
    {
        [JsonProperty("flavor_text")]
        public string FlavorText { get; set; } = string.Empty; // Inicializa
        public Language? Language { get; set; } // Puede ser nulo
    }

    public class Language
    {
        public string Name { get; set; } = string.Empty; // Inicializa
    }

    public class GenerationResponse
    {
        [JsonProperty("pokemon_species")]
        public List<PokemonSpeciesSummary> PokemonSpecies { get; set; } = new List<PokemonSpeciesSummary>();
    }

    public class PokemonSpeciesSummary
    {
        public string Name { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
    }

    // --- Clases para STATS ---
    public class Stat
    {
        [JsonProperty("base_stat")]
        public int BaseStat { get; set; }

        [JsonProperty("effort")]
        public int Effort { get; set; }

        [JsonProperty("stat")] 
        public StatInfo? StatInfo { get; set; }
    }
    public class StatInfo
    {
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;
    }
    // --- Clases para EVOLUCIONES ---
    public class EvolutionChainResponse
    {
        public ChainLink? Chain { get; set; }
    }
    public class EvolutionStep
    {
        public Pokemon ?Pokemon { get; set; }    // El Pokémon en esta etapa
        public EvolutionDetail? EvolutionDetail { get; set; } // Cómo se evoluciona desde el anterior (puede ser null para el primero)
    }

    public class ChainLink
    {
        public PokemonSpeciesSummary? Species { get; set; }

        [JsonProperty("evolves_to")]
        public List<ChainLink> EvolvesTo { get; set; } = new List<ChainLink>();

        [JsonProperty("evolution_details")]
        public List<EvolutionDetail> EvolutionDetails { get; set; } = new List<EvolutionDetail>();
    }

    public class EvolutionDetail
    {
        [JsonProperty("min_level")]
        public int? MinLevel { get; set; }

        [JsonProperty("min_happiness")]
        public int? MinHappiness { get; set; }

        public Item? Item { get; set; }

        [JsonProperty("trigger")]
        public Trigger? Trigger { get; set; }
    }

    public class Item
    {
        public string Name { get; set; } = string.Empty;
    }

    public class Trigger
    {
        public string Name { get; set; } = string.Empty;
    }

    // --- Clases para HABILIDADES ---
    public class PokemonAbility
    {
        [JsonProperty("ability")]
        public AbilityInfo? Ability { get; set; }

        [JsonProperty("is_hidden")]
        public bool IsHidden { get; set; }
    }

    public class AbilityInfo
    {
        public string Name { get; set; } = string.Empty;
    }

    // --- Clases para el DETALLE de una habilidad ---
    public class AbilityDetail
    {
        public string Name { get; set; } = string.Empty;

        [JsonProperty("flavor_text_entries")]
        public List<AbilityFlavorTextEntry> FlavorTextEntries { get; set; } = new List<AbilityFlavorTextEntry>();
    }

    public class AbilityFlavorTextEntry
    {
        [JsonProperty("flavor_text")]
        public string FlavorText { get; set; } = string.Empty;
        public Language? Language { get; set; }
    }
    public class EggGroup
    {
        public string Name { get; set; } = string.Empty;
    }
    public class GenerationResult
    {
        public int TotalCount { get; set; }
        public List<Pokemon> Pokemons { get; set; } = new List<Pokemon>();
    }
}