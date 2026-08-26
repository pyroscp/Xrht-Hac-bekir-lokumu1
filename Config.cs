using Exiled.API.Interfaces;

namespace LokumPlugin
{
    public sealed class Config : IConfig
    {
        public bool IsEnabled { get; set; } = true;
        public bool Debug { get; set; } = false;

        public int SpawnCount { get; set; } = 10;
        public int MaxUses { get; set; } = 3;
        public float DeathChancePercent { get; set; } = 50f;
        public float SlownessDurationSeconds { get; set; } = 15f;
    }
}
