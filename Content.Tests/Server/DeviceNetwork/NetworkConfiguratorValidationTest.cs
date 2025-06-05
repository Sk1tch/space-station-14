using System.Collections.Generic;
using System.IO;
using Content.Server.DeviceNetwork.Systems;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.DeviceNetwork;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;

namespace Content.Tests.Server.DeviceNetwork
{
    [TestFixture]
    public sealed class NetworkConfiguratorValidationTest : ContentUnitTest
    {
        private const string Prototypes = @"
- type: sourcePort
  id: Src
  name: src
  description: src
- type: sinkPort
  id: Sink
  name: sink
  description: sink";

        [Test]
        public void InvalidPortsAreIgnored()
        {
            var entManager = IoCManager.Resolve<IEntityManager>();
            IoCManager.Resolve<ISerializationManager>().Initialize();
            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();
            prototypeManager.Initialize();
            prototypeManager.LoadFromStream(new StringReader(Prototypes));
            prototypeManager.ResolveResults();

            var factory = IoCManager.Resolve<IComponentFactory>();
            factory.RegisterClass<NetworkConfiguratorComponent>();
            factory.RegisterClass<DeviceLinkSourceComponent>();
            factory.RegisterClass<DeviceLinkSinkComponent>();

            var sysMan = entManager.EntitySysManager;
            sysMan.LoadExtraSystemType<NetworkConfiguratorSystem>();
            sysMan.LoadExtraSystemType<DeviceLinkSystem>();
            var configSystem = sysMan.GetEntitySystem<NetworkConfiguratorSystem>();

            var source = entManager.SpawnEntity(null, MapCoordinates.Nullspace);
            var srcComp = entManager.AddComponent<DeviceLinkSourceComponent>(source);
            srcComp.Ports = new() { "Src" };

            var sink = entManager.SpawnEntity(null, MapCoordinates.Nullspace);
            var sinkComp = entManager.AddComponent<DeviceLinkSinkComponent>(sink);
            sinkComp.Ports = new() { "Sink" };

            var configurator = entManager.SpawnEntity(null, MapCoordinates.Nullspace);
            var confComp = entManager.AddComponent<NetworkConfiguratorComponent>(configurator);
            confComp.ActiveDeviceLink = source;
            confComp.DeviceLinkTarget = sink;

            var msg = new NetworkConfiguratorToggleLinkMessage("Invalid", "Invalid");
            configSystem.GetType().GetMethod("OnToggleLinks", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                .Invoke(configSystem, new object?[] { configurator, confComp, msg });

            Assert.That(srcComp.LinkedPorts.Count, Is.EqualTo(0));

            var links = new List<(string source, string sink)>{ ("Src", "Invalid") };
            var saveMsg = new NetworkConfiguratorLinksSaveMessage(links);
            configSystem.GetType().GetMethod("OnSaveLinks", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                .Invoke(configSystem, new object?[] { configurator, confComp, saveMsg });

            Assert.That(srcComp.LinkedPorts.Count, Is.EqualTo(0));
        }
    }
}
