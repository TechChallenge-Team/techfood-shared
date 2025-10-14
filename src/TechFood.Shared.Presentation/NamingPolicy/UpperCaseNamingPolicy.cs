using System.Text.Json;

namespace TechFood.Shared.Presentation.NamingPolicy;

public class UpperCaseNamingPolicy : JsonNamingPolicy
{
  public override string ConvertName(string name) => name.ToUpper();
}
