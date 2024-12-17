using Newtonsoft.Json;
using System.Text.Json;

namespace WebApplication1.Data.RancherDTO;

public class RancherClucterRegistrationDTO
{
    [JsonProperty("baseType")]
    public string? BaseType { get; set; }

    [JsonProperty("clusterId")]
    public string? ClusterId { get; set; }

    [JsonProperty("command")]
    public string? Command { get; set; }

    [JsonProperty("created")]
    public DateTime Created { get; set; }

    [JsonProperty("creatorId")]
    public string? CreatorId { get; set; }

    [JsonProperty("id")]
    public string? Id { get; set; }

    [JsonProperty("insecureCommand")]
    public string? InsecureCommnd { get; set; }

    [JsonProperty("insecureNodeCommand")]
    public string? InsecureNodeCommand { get; set; }

    [JsonProperty("insecureWindowsNodeCommand")]
    public string InsecureWindowsNodeCommand { get; set; }

    [JsonProperty("links")]
    public Dictionary<string, string> Links { get; set; }

    [JsonProperty("minifestUrl")]
    public string? ManifestUrl { get; set; }

    [JsonProperty("name")]
    public string? Name { get; set; }

    [JsonProperty("namespaceId")]
    public string? NamespaceId { get; set; }

    [JsonProperty("nodeCommand")]
    public string? NodeCommand { get; set; }

    [JsonProperty("state")]
    public string? State { get; set; }

    [JsonProperty("token")]
    public string? Token { get; set; }

    [JsonProperty("transitioning")]
    public string? Transitioning { get; set; }

    [JsonProperty("transitioningMessage")]
    public string? TransitioningMessage { get; set; }

    [JsonProperty("type")]
    public string? Type { get; set; }

    [JsonProperty("uuid")]
    public string? Uuid { get; set; }

    [JsonProperty("windowsNodeCommand")]
    public string? WindowsNodeCommand { get; set; }
}
