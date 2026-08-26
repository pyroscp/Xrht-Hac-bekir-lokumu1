using System.Collections.Generic;

using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Features.Items;
using Exiled.API.Features.Pickups;
using Exiled.API.Features.Spawn;
using Exiled.CustomItems.API;
using Exiled.CustomItems.API.Features;
using Exiled.Events.EventArgs.Player;

using MEC;

using UnityEngine;

using ExiledLight = Exiled.API.Features.Toys.Light;
using Player = Exiled.API.Features.Player;

namespace LokumPlugin
{
    public sealed class HaciBekirLokumu : CustomItem
    {
        private const string HintText = "<size=60%><color=red>Hacı Bekir lokumu aldın</color></size>";

        private readonly Dictionary<Player, int> _useCounts = new();
        private readonly Dictionary<Player, CoroutineHandle> _hintLoops = new();
        private readonly List<ExiledLight> _lights = new();

        public Config Config => LokumPlugin.Instance.Config;

        public override uint Id { get; set; } = 7777;
        public override string Name { get; set; } = "Hacı Bekir Lokumu";
        public override string Description { get; set; } =
            "Sağ tıkla ve ye. İçinde patlar ama bir şey olmaz, sadece yavaşlarsın. 3. kullanımda %50 ölüm şansı!";
        public override float Weight { get; set; } = 0.3f;
        public override ItemType Type { get; set; } = ItemType.SCP500;
        public override SpawnProperties SpawnProperties { get; set; }

        protected override void SubscribeEvents()
        {
            base.SubscribeEvents();
            Exiled.Events.Handlers.Player.UsedItem += OnUsed;
            Exiled.Events.Handlers.Player.Left += OnLeft;
            Exiled.Events.Handlers.Server.WaitingForPlayers += ResetState;
            Exiled.Events.Handlers.Server.RoundStarted += OnRoundStarted;
        }

        protected override void UnsubscribeEvents()
        {
            Exiled.Events.Handlers.Server.RoundStarted -= OnRoundStarted;
            Exiled.Events.Handlers.Server.WaitingForPlayers -= ResetState;
            Exiled.Events.Handlers.Player.Left -= OnLeft;
            Exiled.Events.Handlers.Player.UsedItem -= OnUsed;
            base.UnsubscribeEvents();
        }

        protected override void OnPickingUp(PickingUpItemEventArgs ev)
        {
            base.OnPickingUp(ev);
            RemoveNearbyLight(ev.Pickup.Position);
            ev.Player.ShowHint(HintText, 5f);
        }

        protected override void OnChanging(ChangingItemEventArgs ev)
        {
            base.OnChanging(ev);

            if (ev.Item == null || !Check(ev.Item))
            {
                StopHintLoop(ev.Player);
                return;
            }

            StartHintLoop(ev.Player);
        }

        protected override void OnDroppingItem(DroppingItemEventArgs ev)
        {
            base.OnDroppingItem(ev);

            if (!Check(ev.Item))
                return;

            StopHintLoop(ev.Player);
            ev.Player.ShowHint(HintText, 5f);
        }

        private void OnUsed(UsedItemEventArgs ev)
        {
            if (!Check(ev.Item))
                return;

            HandleUse(ev.Player, ev.Player.Position);
        }

        private void HandleUse(Player player, Vector3 position)
        {
            int uses = _useCounts.TryGetValue(player, out int current) ? current + 1 : 1;
            _useCounts[player] = uses;

            Utils.ExplosionUtils.ServerSpawnEffect(position, ItemType.GrenadeHE);
            player.EnableEffect(EffectType.Slowness, Config.SlownessDurationSeconds);
            StopHintLoop(player);

            bool isLastUse = uses >= Config.MaxUses;

            if (!isLastUse)
            {
                player.ShowHint($"<color=orange>BOM! Ama bir şey olmadı ({uses}/{Config.MaxUses})</color>", 4f);

                Timing.CallDelayed(0.4f, () =>
                {
                    if (player.IsConnected && player.IsAlive)
                        Give(player, false);
                });
            }
            else
            {
                _useCounts.Remove(player);
                float roll = Random.Range(0f, 100f);

                if (roll <= Config.DeathChancePercent)
                {
                    player.ShowHint("<color=red>BOM! Bu sefer... patladın!</color>", 5f);
                    Timing.CallDelayed(0.6f, () =>
                    {
                        if (player.IsConnected && player.IsAlive)
                            player.Kill(DamageType.Explosion, "Hacı Bekir lokumu patladı");
                    });
                }
                else
                {
                    player.ShowHint($"<color=green>SON KULLANIM! Şanslıydın, hayatta kaldın! (%{100f - Config.DeathChancePercent} şans)</color>", 5f);
                }
            }
        }

        private void StartHintLoop(Player player)
        {
            StopHintLoop(player);
            _hintLoops[player] = Timing.RunCoroutine(HintLoop(player));
        }

        private void StopHintLoop(Player player)
        {
            if (_hintLoops.TryGetValue(player, out CoroutineHandle handle))
            {
                Timing.KillCoroutines(handle);
                _hintLoops.Remove(player);
            }
        }

        private IEnumerator<float> HintLoop(Player player)
        {
            while (player.IsConnected && player.IsAlive && Check(player))
            {
                player.ShowHint(HintText, 6f);
                yield return Timing.WaitForSeconds(4f);
            }
        }

        private void OnRoundStarted()
        {
            SpawnAroundMap(Config.SpawnCount);
        }

        public void SpawnAroundMap(int count)
        {
            List<Room> rooms = new(Room.List);

            if (rooms.Count == 0)
                return;

            for (int i = 0; i < count; i++)
            {
                Room room = rooms[Random.Range(0, rooms.Count)];
                Vector3 offset = new(Random.Range(-3f, 3f), 1f, Random.Range(-3f, 3f));
                Vector3 position = room.Position + offset;

                Pickup pickup = Spawn(position);

                if (pickup == null)
                    continue;

                pickup.Scale = new Vector3(1.7f, 1.7f, 1.7f);

                ExiledLight light = ExiledLight.Create(
                    position,
                    null,
                    null,
                    true,
                    new Color(1f, 0.05f, 0.05f));
                light.Intensity = 2f;
                light.Range = 5f;
                _lights.Add(light);
            }

            Log.Debug($"{count} adet Hacı Bekir lokumu haritaya dağıtıldı.");
        }

        private void RemoveNearbyLight(Vector3 position)
        {
            for (int i = _lights.Count - 1; i >= 0; i--)
            {
                ExiledLight light = _lights[i];

                if (light == null || light.Base == null)
                {
                    _lights.RemoveAt(i);
                    continue;
                }

                if (Vector3.Distance(light.Position, position) <= 4f)
                {
                    light.Destroy();
                    _lights.RemoveAt(i);
                }
            }
        }

        private void ResetState()
        {
            foreach (CoroutineHandle handle in _hintLoops.Values)
                Timing.KillCoroutines(handle);

            _hintLoops.Clear();
            _useCounts.Clear();
            _lights.Clear();
        }

        private void OnLeft(LeftEventArgs ev)
        {
            StopHintLoop(ev.Player);
            _useCounts.Remove(ev.Player);
        }
    }
}
