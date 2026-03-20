
public interface IInteractable
{
    /// <summary>Called when the player presses the interact key.</summary>
    void Interact(PlayerInteraction player);

    /// <summary>Optional custom prompt text. Return empty string to use default.</summary>
    string GetPromptText();
}