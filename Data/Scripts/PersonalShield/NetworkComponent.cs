using System;
using ProtoBuf;
using Sandbox.ModAPI;

namespace CSEPersonalShield
{
    public sealed class NetworkComponent
    {
        public const ushort NetworkId = 57540;
        public const ushort RechargeNetworkId = 57541;

        private bool _registered;

        private Action<long, long, GroupDefinitions.Group> _onFxReceived;
        private Action<long, RechargeState> _onRechargeReceived;

        [ProtoContract]
        private struct FxPacket
        {
            [ProtoMember(1)] public long CharacterEntityId;
            [ProtoMember(2)] public long VictimIdentityId;
            [ProtoMember(3)] public int DamageGroup;
        }

        [ProtoContract]
        private struct RechargePacket
        {
            [ProtoMember(1)] public long VictimIdentityId;
            [ProtoMember(2)] public byte State;
        }

        public void Init(Action<long, long, GroupDefinitions.Group> onFxReceived, Action<long, RechargeState> onRechargeReceived)
        {
            if (_registered)
            {
                return;
            }

            _onFxReceived = onFxReceived;
            _onRechargeReceived = onRechargeReceived;

            if (MyAPIGateway.Multiplayer != null)
            {
                MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(NetworkId, OnFxMessage);
                MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(RechargeNetworkId, OnRechargeMessage);
                _registered = true;
            }
        }

        public void Dispose()
        {
            if (!_registered)
            {
                return;
            }

            _registered = false;

            if (MyAPIGateway.Multiplayer != null)
            {
                MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(NetworkId, OnFxMessage);
                MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(RechargeNetworkId, OnRechargeMessage);
            }

            _onFxReceived = null;
            _onRechargeReceived = null;
        }

        public void SendFx(long characterEntityId, long victimIdentityId, GroupDefinitions.Group group)
        {
            if (MyAPIGateway.Multiplayer == null || !MyAPIGateway.Multiplayer.MultiplayerActive)
            {
                return;
            }

            var packet = new FxPacket
            {
                CharacterEntityId = characterEntityId,
                VictimIdentityId = victimIdentityId,
                DamageGroup = (int)group
            };

            var data = MyAPIGateway.Utilities.SerializeToBinary(packet);
            MyAPIGateway.Multiplayer.SendMessageToOthers(NetworkId, data);
        }

        public void SendRechargeState(long victimIdentityId, RechargeState state)
        {
            if (MyAPIGateway.Multiplayer == null || !MyAPIGateway.Multiplayer.MultiplayerActive)
            {
                return;
            }

            var packet = new RechargePacket
            {
                VictimIdentityId = victimIdentityId,
                State = (byte)state
            };

            var data = MyAPIGateway.Utilities.SerializeToBinary(packet);
            MyAPIGateway.Multiplayer.SendMessageToOthers(RechargeNetworkId, data);
        }

        private void OnFxMessage(ushort channel, byte[] data, ulong sender, bool fromServer)
        {
            if (data == null || data.Length == 0)
            {
                return;
            }

            FxPacket packet;
            try
            {
                packet = MyAPIGateway.Utilities.SerializeFromBinary<FxPacket>(data);
            }
            catch
            {
                return;
            }

            GroupDefinitions.Group group;
            var groupValue = packet.DamageGroup;

            if (Enum.IsDefined(typeof(GroupDefinitions.Group), groupValue))
            {
                group = (GroupDefinitions.Group)groupValue;
            }
            else
            {
                group = GroupDefinitions.Group.Bullet;
            }

            var cb = _onFxReceived;
            if (cb != null)
            {
                cb(packet.CharacterEntityId, packet.VictimIdentityId, group);
            }
        }

        private void OnRechargeMessage(ushort channel, byte[] data, ulong sender, bool fromServer)
        {
            if (data == null || data.Length == 0)
            {
                return;
            }

            RechargePacket packet;
            try
            {
                packet = MyAPIGateway.Utilities.SerializeFromBinary<RechargePacket>(data);
            }
            catch
            {
                return;
            }

            var raw = packet.State;
            var state = RechargeState.Off;

            if (raw == (byte)RechargeState.Charging)
            {
                state = RechargeState.Charging;
            }
            else if (raw == (byte)RechargeState.BlockedLowEnergy)
            {
                state = RechargeState.BlockedLowEnergy;
            }

            var cb = _onRechargeReceived;
            if (cb != null)
            {
                cb(packet.VictimIdentityId, state);
            }
        }
    }
}