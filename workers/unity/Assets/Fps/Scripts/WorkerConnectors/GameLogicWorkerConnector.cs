using System.Collections;
using Fps.Config;
using Fps.Guns;
using Fps.Health;
using Fps.Metrics;
using Improbable.Gdk.Core;
using Improbable.Gdk.GameObjectCreation;
using Improbable.Gdk.PlayerLifecycle;
using Improbable.Worker.CInterop;
using UnityEngine;

namespace Fps.WorkerConnectors
{
    public class GameLogicWorkerConnector : WorkerConnectorBase
    {
        public bool DisableRenderers = true;

        protected async void Start()
        {
            Application.targetFrameRate = 60;

            await Connect(GetConnectionHandlerBuilder(), new ForwardingDispatcher());
            await LoadWorld();

            if (DisableRenderers)
            {
                foreach (var childRenderer in LevelInstance.GetComponentsInChildren<Renderer>())
                {
                    childRenderer.enabled = false;
                }
            }
        }

        private IConnectionHandlerBuilder GetConnectionHandlerBuilder()
        {
            IConnectionFlow connectionFlow;
            ConnectionParameters connectionParameters;

            var workerId = CreateNewWorkerId(WorkerUtils.UnityGameLogic);

            if (Application.isEditor)
            {
                connectionFlow = new ReceptionistFlow(workerId);
                connectionParameters = CreateConnectionParameters(WorkerUtils.UnityGameLogic);
            }
            else
            {
                connectionFlow = new ReceptionistFlow(workerId, new CommandLineConnectionFlowInitializer());
                connectionParameters = CreateConnectionParameters(WorkerUtils.UnityGameLogic,
                    new CommandLineConnectionParameterInitializer());
            }

            return new SpatialOSConnectionHandlerBuilder()
                .SetConnectionFlow(connectionFlow)
                .SetConnectionParameters(connectionParameters);
        }

        protected override void HandleWorkerConnectionEstablished()
        {
            var world = Worker.World;

            PlayerLifecycleHelper.AddServerSystems(world);
            GameObjectCreationHelper.EnableStandardGameObjectCreation(world);

            // Shooting
            world.GetOrCreateSystem<ServerShootingSystem>();

            // Metrics
            world.GetOrCreateSystem<MetricSendSystem>();

            // Health
            world.GetOrCreateSystem<ServerHealthModifierSystem>();
            world.GetOrCreateSystem<HealthRegenSystem>();
        }
    }
}
