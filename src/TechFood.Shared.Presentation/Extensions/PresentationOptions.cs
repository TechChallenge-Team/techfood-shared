namespace TechFood.Shared.Presentation.Extensions;

public class PresentationOptions
{
    public bool AddSwagger { get; set; } = true;

    public bool AddHealthChecks { get; set; } = true;

    public bool AddJwtAuthentication { get; set; }

    public string? SwaggerTitle { get; set; }

    public string? SwaggerDescription { get; set; }
}
