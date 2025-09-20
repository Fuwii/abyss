using UnityEngine;

namespace Game.Mechanics.Interactables.Tools
{
    public interface IInteractable
    {
        void OnFocusEnter(GameObject player);
        void OnFocusExit(GameObject player);
        void Interact(GameObject player);
    }
}