using Game.Mechanics.Interactables.Tools;
using UnityEngine;

namespace Game.Player
{
    public class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] private Camera playerCamera;
        [SerializeField] private float interactDistance = 2f;
        [SerializeField] private KeyCode interactKey = KeyCode.E;

        private IInteractable _currentTarget;

        void Update()
        {
            CheckFocus();
            if (_currentTarget != null && UnityEngine.Input.GetKeyDown(interactKey))
            {
                _currentTarget.Interact(gameObject);
            }
        }

        void CheckFocus()
        {
            var ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

            if (Physics.Raycast(ray, out var hit, interactDistance))
            {
                var interactable = hit.collider.GetComponentInParent<IInteractable>();
                if (interactable != null)
                {
                    if (interactable != _currentTarget)
                    {
                        _currentTarget?.OnFocusExit(gameObject);
                        _currentTarget = interactable;
                        _currentTarget.OnFocusEnter(gameObject);
                    }

                    return;
                }
            }

            if (_currentTarget != null)
            {
                _currentTarget.OnFocusExit(gameObject);
                _currentTarget = null;
            }
        }
    }
}