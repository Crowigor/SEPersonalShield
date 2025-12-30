namespace CSEPersonalShield
{
    public static class Config
    {
        public const int TicksPerSecond = 60;

        // Debug
        public const bool Debug = false;
        public const bool DebugDamage = true;
        public const bool DebugRecharge = false;
        public const int DebugRechargeBlockedCooldownTicks = 180;
        public const string DebugLogSender = "Personal Shield";

        // Recharge
        public const int RechargeDelayTicks = 600;
        public const int RechargeStepTicks = 10;
        public const float RechargeMinEnergy = 0.3f;

        // FX
        public const ushort FxNetId = 48219;
        public const int FxDurationTicks = 60;
        public const int FxRefreshMinTicks = 2;
        public const int FxSoundCooldownTicks = 12;
        public const string FxHitSoundCue = "ArcParticleElectricalDischarge";
    }
}