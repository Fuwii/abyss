using Core.Singleton;
using DG.Tweening;
using Unity.Netcode;
using UnityEngine;

namespace Game
{
    public class Elevator : Singleton<Elevator>
    {
        [Header("References")]
        [SerializeField] private Transform elevator;
        [SerializeField] private Transform door;

        [Header("Settings")]
        [SerializeField] private float startHeight = 500.0f;
        [SerializeField, Min(0)] private float summonTime = 3.0f;
        [SerializeField, Min(0)] private float moveTime = 10f;

        [Space]
        [SerializeField] private Vector3 doorOpenRotation;
        [SerializeField] private Vector3 doorCloseRotation;
        [SerializeField, Min(0)] private float doorOpenTime;
        [SerializeField, Min(0)] private float doorCloseTime;

        private bool active;
        private Tween elevatorTween;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.T))
            {
                Summon();
            }

            if (Input.GetKeyDown(KeyCode.Y))
            {
                Up();
            }

            if (Input.GetKeyDown(KeyCode.U))
            {
                Down();
            }
        }

        [Rpc(SendTo.Server)]
        public void Summon()
        {
            if (active)
            {
                return;
            }

            active = true;

            var point = ElevatorSummonPoint.GetComponent();

            var end = point ? point.transform.position : Vector3.zero;
            var start = end + Vector3.up * startHeight;

            elevatorTween?.Kill();
            elevatorTween = DOTween.Sequence()
                .Append(elevator
                    .DOLocalMove(end, summonTime)
                    .From(start)
                    .SetEase(Ease.InQuad))
                .Append(door
                    .DOLocalRotate(doorOpenRotation, doorOpenTime)
                    .From(doorCloseRotation)
                    .SetEase(Ease.InOutSine));
        }

        [Rpc(SendTo.Server)]
        public void Up()
        {
            var end = transform.position + Vector3.up * startHeight;

            elevatorTween?.Kill();
            elevatorTween = elevator
                .DOLocalMove(end, moveTime)
                .SetEase(Ease.InQuad);
        }

        [Rpc(SendTo.Server)]
        public void Down()
        {
            var point = ElevatorSummonPoint.GetComponent();

            var end = point ? point.transform.position : Vector3.zero;
            var start = end + Vector3.up * startHeight;

            elevatorTween?.Kill();
            elevatorTween = elevator
                .DOLocalMove(end, moveTime)
                .From(start)
                .SetEase(Ease.InQuad);
        }
    }
}