using UnityEngine;
using UnityEngine.Events;
public class InteractableNPC : MonoBehaviour, IInteractable
{
    [SerializeField] public string npcName = "Villager";
    [SerializeField] private UnityEvent onTalkTo;

    public void Interact(PlayerInteraction player)
    {
        onTalkTo?.Invoke();
    }

    public string GetPromptText() => $"[E] Talk to {npcName}";
}
