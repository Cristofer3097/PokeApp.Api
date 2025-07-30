namespace PokeApp.Models;

public class ErrorViewModel
{
    public string? RequestId { get; set; }

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}
public class EvolutionChainResult
{
    public bool IsLinear { get; set; } = true; // Asumimos que es lineal hasta que se demuestre lo contrario
    public List<EvolutionStep> Steps { get; set; } = new List<EvolutionStep>();
}