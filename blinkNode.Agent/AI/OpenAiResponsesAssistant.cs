using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace blinkNode.Agent.AI;

/// <summary>
/// Sends an explicitly addressed chat conversation to OpenAI's Responses API.
/// The bearer credential belongs to the signed-in user and must never be logged.
/// </summary>
public sealed class OpenAiResponsesAssistant : IChatAssistant
{
    private static readonly Uri ResponsesEndpoint = new("https://api.openai.com/v1/responses");
    private static readonly HashSet<string> AllowedRoles = ["user", "assistant"];

    private readonly HttpClient _httpClient;
    private readonly string _bearerCredential;
    private readonly string _model;
    private readonly string _instructions;

    public OpenAiResponsesAssistant(HttpClient httpClient, string bearerCredential, string model, string instructions)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentException.ThrowIfNullOrWhiteSpace(bearerCredential);
        ArgumentException.ThrowIfNullOrWhiteSpace(model);
        ArgumentException.ThrowIfNullOrWhiteSpace(instructions);

        _httpClient = httpClient;
        _bearerCredential = bearerCredential;
        _model = model;
        _instructions = instructions;
    }

    public async Task<string> ReplyAsync(IReadOnlyList<ChatTurn> conversation, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(conversation);
        if (conversation.Count == 0)
            throw new ArgumentException("At least one user message is required.", nameof(conversation));

        var input = conversation.Select(turn =>
        {
            if (!AllowedRoles.Contains(turn.Role))
                throw new ArgumentException($"Unsupported conversation role '{turn.Role}'.", nameof(conversation));
            ArgumentException.ThrowIfNullOrWhiteSpace(turn.Text);
            return new { role = turn.Role, content = turn.Text };
        }).ToArray();

        using var request = new HttpRequestMessage(HttpMethod.Post, ResponsesEndpoint)
        {
            Content = JsonContent.Create(new
            {
                model = _model,
                instructions = _instructions,
                input,
                store = false,
                max_output_tokens = 600
            })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _bearerCredential);
        using var response = await _httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            // Do not include response bodies: API errors can contain request metadata.
            throw new HttpRequestException($"OpenAI Responses API returned HTTP {(int)response.StatusCode}.");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        if (!document.RootElement.TryGetProperty("output", out var output))
            throw new InvalidOperationException("OpenAI response did not contain output items.");

        var text = new System.Text.StringBuilder();
        foreach (var item in output.EnumerateArray())
        {
            if (!item.TryGetProperty("content", out var content))
                continue;

            foreach (var block in content.EnumerateArray())
            {
                if (block.TryGetProperty("type", out var type) && type.GetString() == "output_text" &&
                    block.TryGetProperty("text", out var value))
                {
                    if (text.Length > 0)
                        text.AppendLine();
                    text.Append(value.GetString());
                }
            }
        }

        if (text.Length == 0)
            throw new InvalidOperationException("OpenAI response contained no text output.");

        return text.ToString();
    }
}
