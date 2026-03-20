using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] private float interactRadius = 2.5f;
    [SerializeField] private LayerMask interactMask = ~0;
    [SerializeField] private KeyCode interactKey = KeyCode.E;

    [Header("UI Prompt")]
    [Tooltip("Assign a UI Text or TMP element to show the interact prompt.")]
    [SerializeField] private GameObject promptUI;
    [SerializeField] private string promptText = "[E] Interact";

    private IInteractable _nearestInteractable;
    private Camera _cam;

    private void Awake()
    {
        _cam = Camera.main;
        if (promptUI != null)
            promptUI.SetActive(false);
    }

    private void Update()
    {
        FindNearestInteractable();
        UpdatePrompt();

        if (Input.GetKeyDown(interactKey) && _nearestInteractable != null)
            _nearestInteractable.Interact(this);
    }

    private void FindNearestInteractable()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, interactRadius,
                                                interactMask, QueryTriggerInteraction.Collide);
        float closestDist = float.MaxValue;
        IInteractable closest = null;

        foreach (var hit in hits)
        {
            if (hit.gameObject == gameObject) continue;

            IInteractable interactable = hit.GetComponentInParent<IInteractable>();
            if (interactable == null) continue;

            float dist = Vector3.Distance(transform.position, hit.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = interactable;
            }
        }

        _nearestInteractable = closest;
    }

    private void UpdatePrompt()
    {
        if (promptUI == null) return;

        bool show = _nearestInteractable != null;
        promptUI.SetActive(show);

        // If using TextMeshPro
        if (show)
        {
            TMPro.TMP_Text tmp = promptUI.GetComponentInChildren<TMPro.TMP_Text>();
            if (tmp != null)
            {
                string label = _nearestInteractable.GetPromptText();
                tmp.text = string.IsNullOrEmpty(label) ? promptText : label;
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }
}