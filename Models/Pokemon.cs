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
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public PokemonSprites Sprites { get; set; } = new PokemonSprites(); 
        public List<PokemonType> Types { get; set; } = new List<PokemonType>();

        public string? Description { get; set; }
        public int Height { get; set; } 
        public int Weight { get; set; }
    }
    public class PokemonSprites
    {
        [JsonProperty("front_default")]
        public string? FrontDefault { get; set; }

        // Añadimos la propiedad 'Other' que contiene las imágenes de alta calidad
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
}