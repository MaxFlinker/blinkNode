namespace blinkNode.Agent.AI;

public interface IChatAssistant
{
    Task<string> ReplyAsync(IReadOnlyList<ChatTurn> conversation, CancellationToken cancellationToken = default);
}
