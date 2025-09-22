using UnityEngine;
using TMPro;

namespace Game.Player.UI
{
    public class PlayerDepthtText : MonoBehaviour
    {
        private TMP_Text heightText;
        private Transform player;

        private void Awake()
        {
            player = transform;
            heightText = GameObject.FindWithTag("DepthText").GetComponent<TMP_Text>();
        }

        private void Update()
        {
            if (heightText == null) return;

            float y = player.position.y;

            if (y > 0f)
            {
                heightText.text = "0 m.";
            }
            else
            {
                int meters = Mathf.FloorToInt(y);
                heightText.text = meters + " m.";
            }
        }
    }

}

