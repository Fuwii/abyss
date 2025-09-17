using UnityEngine;

public class PlayerInteractor : MonoBehaviour
{
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float interactDistance = 2f;
    [SerializeField] private KeyCode interactKey = KeyCode.E;

    private IInteractable currentTarget;

    void Update()
    {
        CheckFocus();
        if (currentTarget != null && Input.GetKeyDown(interactKey))
        {
            currentTarget.Interact(gameObject);
        }
    }

    void CheckFocus()
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance))
        {
            var interactable = hit.collider.GetComponentInParent<IInteractable>();

            Debug.Log("OBject"+ interactable);
            if (interactable != null)
            {
               
                if (interactable != currentTarget)
                {
                    currentTarget?.OnFocusExit(gameObject);
                    currentTarget = interactable;
                    currentTarget.OnFocusEnter(gameObject);
                }
                return;
            }
        }

        if (currentTarget != null)
        {
            currentTarget.OnFocusExit(gameObject);
            currentTarget = null;
        }
    }
}
