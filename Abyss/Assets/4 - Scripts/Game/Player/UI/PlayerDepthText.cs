using UnityEngine;
using TMPro;

namespace Game.Player.UI
{
    public class PlayerDepthText : MonoBehaviour
    {
        private TMP_Text _heightText;
        private Transform _player;

        private void Start()
        {
            _player = transform;
            _heightText = GameObject.FindWithTag("DepthText").GetComponent<TMP_Text>();
        }

        private void Update()
        {
            if (!_heightText) return;

            var y = _player.position.y;

            if (y > 0f)
            {
                _heightText.text = "0 m.";
            }
            else
            {
                int meters = Mathf.FloorToInt(y);
                _heightText.text = meters + " m.";
            }
        }
    }
}