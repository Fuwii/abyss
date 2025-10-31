using UnityEngine;
using UnityEngine.UI;

namespace Game.Player.UI
{
    public class PlayerEffectUI : MonoBehaviour
    {
        [SerializeField] private Image[] imageComponents;

        public void SetColor(Color color)
        {
            foreach (var image in imageComponents)
            {
                image.color = color;
            }
        }
    }
}