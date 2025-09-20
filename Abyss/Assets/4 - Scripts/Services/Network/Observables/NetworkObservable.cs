using System;
using Core.Singleton;
using R3;
using Unity.Netcode;

namespace Services.Network.Observables
{
    public class NetworkObservable : NetworkService<NetworkObservable>
    {
        public Observable<Unit> OnClientStarted { get; private set; }
        public Observable<bool> OnClientStopped { get; private set; }

        public Observable<ulong> OnClientConnected { get; private set; }
        public Observable<ulong> OnClientDisconnect { get; private set; }

        public Observable<Unit> OnPreShutdown { get; private set; }

        protected override void OnNetworkPreSpawn(NetworkManager manager)
        {
            base.OnNetworkPreSpawn(manager);

            OnClientStarted = Observable.FromEvent<Action, Unit>(
                handler => () => handler(Unit.Default),
                handler => NetworkManager.Singleton.OnClientStarted += handler,
                handler => NetworkManager.Singleton.OnClientStarted -= handler,
                ServiceCancellationToken
            );

            OnClientStopped = Observable.FromEvent<Action<bool>, bool>(
                handler => handler,
                handler => NetworkManager.Singleton.OnClientStopped += handler,
                handler => NetworkManager.Singleton.OnClientStopped -= handler,
                ServiceCancellationToken
            );

            OnClientConnected = Observable.FromEvent<Action<ulong>, ulong>(
                handler => handler,
                handler => NetworkManager.Singleton.OnClientConnectedCallback += handler,
                handler => NetworkManager.Singleton.OnClientConnectedCallback -= handler,
                ServiceCancellationToken
            );

            OnClientDisconnect = Observable.FromEvent<Action<ulong>, ulong>(
                handler => handler,
                handler => NetworkManager.Singleton.OnClientDisconnectCallback += handler,
                handler => NetworkManager.Singleton.OnClientDisconnectCallback -= handler,
                ServiceCancellationToken
            );

            OnPreShutdown = Observable.FromEvent<Action, Unit>(
                handler => () => handler(Unit.Default),
                handler => NetworkManager.Singleton.OnPreShutdown += handler,
                handler => NetworkManager.Singleton.OnPreShutdown -= handler,
                ServiceCancellationToken
            );
        }
    }
}