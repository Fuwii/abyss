using System;
using UnityEngine;

namespace Game.Player.Movement.Climbing
{
    public class ClimbingService
    {
        private IClimbHandler _activeHandler;

        // High-level events/hooks. PullUp passes data through PullUpRequest.
        public event Action<PullUpRequest> OnPullUpRequested;
        public event Action OnTransitionToFallingRequested;
        public event Action OnNoStaminaRequested; // if needed
        private readonly ClimbHandlerFactory _factory;

        public ClimbingService(ClimbHandlerFactory factory)
        {
            _factory = factory;
        }

        public void HandleClimbing(ClimbingContext ctx)
        {
            if (ctx == null) return;

            var handler = _factory.GetHandler(ctx);

            // Bind external delegates so that the handler can request actions.
            // If the caller already set them, they will be overwritten here.
            ctx.RequestPullUp = (req) => OnPullUpRequested?.Invoke(req);
            ctx.RequestTransitionToFalling = () => OnTransitionToFallingRequested?.Invoke();
            ctx.RequestNoStamina = () => OnNoStaminaRequested?.Invoke();
            Debug.Log(handler);

            if (handler != null && handler != _activeHandler)
            {
                _activeHandler = handler;

                // можешь вызвать OnEnter/OnExit у handler'ов при необходимости
            }

            if (_activeHandler != null)
            {
                var alive = _activeHandler.Handle(ctx);
                if (!alive)
                {
                    _activeHandler = null;
                }
            }
        }
    }
}

// Context object passed into ClimbingService for a single FixedUpdate