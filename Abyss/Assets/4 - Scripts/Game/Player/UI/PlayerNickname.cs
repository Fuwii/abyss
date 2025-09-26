using UnityEngine;
using TMPro;
namespace Game.Player.UI
{
    public class PlayerNickname : MonoBehaviour
    {
        [SerializeField] private TMP_Text nicknameText;
        [SerializeField] private float showDistance = 15f;
        [SerializeField] private Transform playerRoot;

        private Transform localCamera;

        private void Start()
        {
            if (Camera.main != null)
                localCamera = Camera.main.transform;
        }

        private void Update()
        {
            if (localCamera == null) return;

            float distance = Vector3.Distance(localCamera.position, playerRoot.position);
            Debug.Log(distance);
            bool isShouldShow = distance <= showDistance;

            if (nicknameText.gameObject.activeSelf != isShouldShow)
                nicknameText.gameObject.SetActive(isShouldShow);

            if (!isShouldShow) return;

            Vector3 lookDirection = localCamera.position - nicknameText.transform.position;

            Quaternion lookRot = Quaternion.LookRotation(lookDirection);
            nicknameText.transform.rotation = Quaternion.Euler(0, lookRot.eulerAngles.y, 0);
        }

        public void SetNickname(string nickname)
        {
            nicknameText.text = nickname;
        }
    }
}
