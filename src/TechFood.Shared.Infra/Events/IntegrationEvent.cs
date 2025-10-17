namespace TechFood.Shared.Infra.Events;

internal class IntegrationEvent
{
    public string Name { get; set; } = null!;

    public string Payload { get; set; } = null!;
}
