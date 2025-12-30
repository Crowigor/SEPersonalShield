using System;
using System.Collections.Generic;
using CSE.Data.Scripts.PersonalShield;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Game;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRageMath;

namespace CSEPersonalShield
{
    public enum RechargeState : byte
    {
        Off = 0,
        Charging = 1,
        BlockedLowEnergy = 2
    }

    public sealed class ShieldRechargeLogic
    {
        private readonly ShieldsComponent _shieldsComponent;
        private readonly List<IMyPlayer> _players = new List<IMyPlayer>(8);
        private readonly Dictionary<long, int> _rechargeDelayTicksByPlayer = new Dictionary<long, int>();
        private readonly Dictionary<long, int> _rechargeBlockedDebugCooldownByPlayer = new Dictionary<long, int>();
        private readonly Dictionary<long, RechargeState> _rechargeStateByPlayer = new Dictionary<long, RechargeState>();

        public Action<long, RechargeState> OnRechargeStateChanged;

        public ShieldRechargeLogic(ShieldsComponent shieldsComponent)
        {
            _shieldsComponent = shieldsComponent;
        }

        public void Clear()
        {
            _rechargeDelayTicksByPlayer.Clear();
            _rechargeBlockedDebugCooldownByPlayer.Clear();
            _rechargeStateByPlayer.Clear();
            _players.Clear();
        }

        public void OnDamaged(long identityId, string subtypeId)
        {
            int current;
            var hadDelay = _rechargeDelayTicksByPlayer.TryGetValue(identityId, out current) && current > 0;

            _rechargeDelayTicksByPlayer[identityId] = Config.RechargeDelayTicks;

            SetRechargeState(identityId, RechargeState.Off);

            if (hadDelay)
            {
                return;
            }

            var seconds = Config.RechargeDelayTicks / (float)Config.TicksPerSecond;
            DebugService.Recharge("Recharge delay started. Shield: " + subtypeId + " | delay: " +
                                  seconds.ToString("0.##") + "s");
        }

        public void Step()
        {
            if (MyAPIGateway.Players == null)
            {
                return;
            }

            _players.Clear();
            MyAPIGateway.Players.GetPlayers(_players);

            foreach (var player in _players)
            {
                if (player == null || player.Character == null)
                {
                    continue;
                }

                var id = player.IdentityId;
                var character = player.Character;

                TickCooldown(_rechargeBlockedDebugCooldownByPlayer, id);

                MyInventory inventory;
                ShieldsComponent.Shield shield;
                MyObjectBuilder_GasContainerObject bottle;

                if (!_shieldsComponent.GetBestShieldInInventory(character, out inventory, out shield, out bottle))
                {
                    SetRechargeState(id, RechargeState.Off);
                    continue;
                }

                var maxCapacity = shield.Capacity;
                if (maxCapacity <= 0f)
                {
                    SetRechargeState(id, RechargeState.Off);
                    continue;
                }

                var delayTicks = GetRechargeDelayTicks(id);
                if (delayTicks > 0)
                {
                    delayTicks -= Config.RechargeStepTicks;
                    if (delayTicks < 0)
                    {
                        delayTicks = 0;
                    }

                    _rechargeDelayTicksByPlayer[id] = delayTicks;
                    SetRechargeState(id, RechargeState.Off);
                    continue;
                }

                var gasLevel = MathHelper.Clamp(bottle.GasLevel, 0f, 1f);
                if (gasLevel >= 1f)
                {
                    if (GetRechargeState(id) == RechargeState.Charging)
                    {
                        DebugService.Recharge("Recharge complete. Shield: " + shield.SubtypeId + " | capacity: 100%");
                    }

                    SetRechargeState(id, RechargeState.Off);
                    continue;
                }

                var suitEnergy = character.SuitEnergyLevel;
                if (suitEnergy < Config.RechargeMinEnergy)
                {
                    SetRechargeState(id, RechargeState.BlockedLowEnergy);

                    if (CanPrint(_rechargeBlockedDebugCooldownByPlayer, id))
                    {
                        SetCooldown(_rechargeBlockedDebugCooldownByPlayer, id,
                            Config.DebugRechargeBlockedCooldownTicks);

                        DebugService.Recharge(
                            "Recharge blocked (low energy). Shield: " + shield.SubtypeId +
                            " | energy: " + ToInt(suitEnergy * 100f) + "% (need >= " +
                            ToInt(Config.RechargeMinEnergy * 100f) + "%)" +
                            " | capacity: " + ToInt(gasLevel * 100f) + "%");
                    }

                    continue;
                }

                var pointsPerSecond = shield.RechargePerSecond;
                if (pointsPerSecond <= 0f)
                {
                    SetRechargeState(id, RechargeState.Off);
                    continue;
                }

                var pointsToAdd = pointsPerSecond * (Config.RechargeStepTicks / (float)Config.TicksPerSecond);

                var maxAdd = (1f - gasLevel) * maxCapacity;
                if (pointsToAdd > maxAdd)
                {
                    pointsToAdd = maxAdd;
                }

                if (pointsToAdd <= 0f)
                {
                    SetRechargeState(id, RechargeState.Off);
                    continue;
                }

                var energyCostFull = shield.RechargeCost;
                if (energyCostFull <= 0f)
                {
                    SetRechargeState(id, RechargeState.Off);
                    continue;
                }

                var energyCost = pointsToAdd / maxCapacity * energyCostFull;

                var maxSpend = suitEnergy - Config.RechargeMinEnergy;
                if (maxSpend <= 0f)
                {
                    SetRechargeState(id, RechargeState.BlockedLowEnergy);
                    continue;
                }

                if (energyCost > maxSpend)
                {
                    energyCost = maxSpend;
                    pointsToAdd = energyCost / energyCostFull * maxCapacity;

                    if (pointsToAdd <= 0f)
                    {
                        SetRechargeState(id, RechargeState.BlockedLowEnergy);
                        continue;
                    }
                }

                var prevState = GetRechargeState(id);

                var oldGasLevel = gasLevel;
                gasLevel += pointsToAdd / maxCapacity;
                gasLevel = MathHelper.Clamp(gasLevel, 0f, 1f);
                bottle.GasLevel = gasLevel;
                inventory.Refresh();

                var newEnergy = suitEnergy - energyCost;
                newEnergy = MathHelper.Clamp(newEnergy, 0f, 1f);
                MyVisualScriptLogicProvider.SetPlayersEnergyLevel(id, newEnergy);

                if (prevState != RechargeState.Charging)
                {
                    DebugService.Recharge("Recharge started. Shield: " + shield.SubtypeId +
                                          " | capacity: " + ToInt(oldGasLevel * 100f) + "%");
                }

                DebugService.Recharge(
                    "Recharge +" + ToInt(pointsToAdd) +
                    " | Shield: " + shield.SubtypeId +
                    " | capacity: " + ToInt(oldGasLevel * 100f) + "% -> " + ToInt(gasLevel * 100f) + "%" +
                    " | energy: " + ToInt(suitEnergy * 100f) + "% -> " + ToInt(newEnergy * 100f) + "%");

                SetRechargeState(id, RechargeState.Charging);

                if (!(gasLevel >= 1f) || !(oldGasLevel < 1f))
                {
                    continue;
                }

                DebugService.Recharge("Recharge complete. Shield: " + shield.SubtypeId + " | capacity: 100%");
                SetRechargeState(id, RechargeState.Off);
            }
        }

        private void SetRechargeState(long identityId, RechargeState state)
        {
            RechargeState prev;
            _rechargeStateByPlayer.TryGetValue(identityId, out prev);

            if (prev == state)
            {
                _rechargeStateByPlayer[identityId] = state;
                return;
            }

            _rechargeStateByPlayer[identityId] = state;

            var cb = OnRechargeStateChanged;
            cb?.Invoke(identityId, state);
        }

        private RechargeState GetRechargeState(long identityId)
        {
            RechargeState state;
            return _rechargeStateByPlayer.TryGetValue(identityId, out state) ? state : RechargeState.Off;
        }

        private int GetRechargeDelayTicks(long identityId)
        {
            int ticks;
            return _rechargeDelayTicksByPlayer.TryGetValue(identityId, out ticks) ? ticks : 0;
        }

        private static void TickCooldown(Dictionary<long, int> dict, long identityId)
        {
            int ticks;
            if (!dict.TryGetValue(identityId, out ticks))
            {
                return;
            }

            if (ticks <= 0)
            {
                return;
            }

            ticks -= Config.RechargeStepTicks;
            if (ticks < 0)
            {
                ticks = 0;
            }

            dict[identityId] = ticks;
        }

        private static bool CanPrint(Dictionary<long, int> dict, long identityId)
        {
            int ticks;
            if (!dict.TryGetValue(identityId, out ticks))
            {
                return true;
            }

            return ticks <= 0;
        }

        private static void SetCooldown(Dictionary<long, int> dict, long identityId, int ticks)
        {
            if (ticks < 0)
            {
                ticks = 0;
            }

            dict[identityId] = ticks;
        }

        private static int ToInt(float value)
        {
            return (int)Math.Round(value, MidpointRounding.AwayFromZero);
        }
    }
}