using System;

using Exiled.API.Features;
using Exiled.CustomItems.API;

namespace LokumPlugin
{
    public sealed class LokumPlugin : Plugin<Config>
    {
        public static LokumPlugin Instance { get; private set; }

        public HaciBekirLokumu Lokum { get; private set; }

        public override string Author => "Sen";
        public override string Name => "Hacı Bekir Lokumu";
        public override Version Version => new(1, 0, 0);

        public override void OnEnabled()
        {
            Instance = this;
            Lokum = new HaciBekirLokumu();
            Lokum.Register();

            base.OnEnabled();
            Log.Info("Hacı Bekir Lokumu eklentisi yüklendi!");
        }

        public override void OnDisabled()
        {
            Lokum?.Unregister();
            Lokum = null;
            Instance = null;

            base.OnDisabled();
        }
    }
}
