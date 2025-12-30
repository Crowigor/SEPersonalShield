using CSE.Data.Scripts.PersonalShield;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.Game.ModAPI;

namespace CSEPersonalShield
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class SessionComponent : MySessionComponentBase
    {
        private bool _registered;
        private bool _isServer;
        private int _tick;

        private readonly NetworkComponent _network = new NetworkComponent();
        private readonly FxComponent _fx = new FxComponent();

        private HudComponent _hudComponent;
        private GroupsComponent _groupsComponent;
        private ShieldsComponent _shieldsComponent;
        private ShieldDamageLogic _damageLogic;
        private ShieldRechargeLogic _rechargeLogic;

        private ChatCommandsComponent _chatCommands;

        public override void UpdateAfterSimulation()
        {
            if (!_registered)
            {
                TryRegister();
            }

            if (!_registered)
            {
                return;
            }

            _tick++;

            if (_isServer && _tick % Config.RechargeStepTicks == 0)
            {
                if (_rechargeLogic != null)
                {
                    _rechargeLogic.Step();
                }
            }

            if (MyAPIGateway.Utilities == null || MyAPIGateway.Utilities.IsDedicated)
            {
                return;
            }

            _fx.Update(_tick);

            if (_hudComponent != null)
            {
                _hudComponent.Update();
            }
        }

        protected override void UnloadData()
        {
            _registered = false;
            _isServer = false;

            if (_chatCommands != null)
            {
                _chatCommands.Dispose();
                _chatCommands = null;
            }

            if (_rechargeLogic != null)
            {
                _rechargeLogic.Clear();
            }

            if (_hudComponent != null)
            {
                _hudComponent.Dispose();
                _hudComponent = null;
            }

            _network.Dispose();
            _fx.Dispose();
        }

        private void TryRegister()
        {
            if (_registered)
            {
                return;
            }

            if (MyAPIGateway.Session == null)
            {
                return;
            }

            _isServer = MyAPIGateway.Session.IsServer;

            _groupsComponent = new GroupsComponent();
            _shieldsComponent = new ShieldsComponent();
            _damageLogic = new ShieldDamageLogic(_groupsComponent, _shieldsComponent);
            _rechargeLogic = new ShieldRechargeLogic(_shieldsComponent);

            _network.Init(OnFxReceived, OnRechargeReceived);
            _fx.Init();

            if (MyAPIGateway.Utilities != null && !MyAPIGateway.Utilities.IsDedicated)
            {
                _hudComponent = new HudComponent(_groupsComponent, _shieldsComponent);

                _chatCommands = new ChatCommandsComponent();
                _chatCommands.Init((x, y) =>
                {
                    if (_hudComponent != null)
                    {
                        _hudComponent.SetOffsets(x, y);
                    }
                });
            }

            if (_rechargeLogic != null)
            {
                _rechargeLogic.OnRechargeStateChanged = OnRechargeStateChanged;
            }

            if (_isServer && MyAPIGateway.Session.DamageSystem != null)
            {
                MyAPIGateway.Session.DamageSystem.RegisterBeforeDamageHandler(0, BeforeDamageHandler);
                DebugService.General("Damage hook registered (SERVER)");
            }

            _registered = true;
        }

        private void OnFxReceived(long characterEntityId, long victimIdentityId, GroupDefinitions.Group group)
        {
            _fx.OnFx(characterEntityId, victimIdentityId, _tick);

            if (_hudComponent != null)
            {
                _hudComponent.NotifyDamage(victimIdentityId, group);
            }
        }

        private void OnRechargeReceived(long victimIdentityId, RechargeState state)
        {
            if (_hudComponent != null)
            {
                _hudComponent.NotifyRecharge(victimIdentityId, state);
            }
        }

        private void OnRechargeStateChanged(long victimIdentityId, RechargeState state)
        {
            _network.SendRechargeState(victimIdentityId, state);

            if (MyAPIGateway.Utilities != null && !MyAPIGateway.Utilities.IsDedicated)
            {
                if (_hudComponent != null)
                {
                    _hudComponent.NotifyRecharge(victimIdentityId, state);
                }
            }
        }

        private void BeforeDamageHandler(object target, ref MyDamageInformation info)
        {
            if (!_isServer)
            {
                return;
            }

            var character = target as IMyCharacter;
            if (character == null)
            {
                return;
            }

            var player = MyAPIGateway.Players != null
                ? MyAPIGateway.Players.GetPlayerControllingEntity(character)
                : null;

            if (player == null)
            {
                return;
            }

            ShieldDamageLogic.DamageHit hit;
            if (_damageLogic == null || !_damageLogic.HandleBeforeDamage(character, player, ref info, out hit))
            {
                return;
            }

            if (!hit.AbsorbedAny)
            {
                return;
            }

            if (_rechargeLogic != null && hit.ShieldSubtypeId != null)
            {
                _rechargeLogic.OnDamaged(hit.VictimIdentityId, hit.ShieldSubtypeId);
            }

            _network.SendFx(hit.CharacterEntityId, hit.VictimIdentityId, hit.Group);

            if (MyAPIGateway.Utilities == null || MyAPIGateway.Utilities.IsDedicated)
            {
                return;
            }

            _fx.OnFx(hit.CharacterEntityId, hit.VictimIdentityId, _tick);

            if (_hudComponent != null)
            {
                _hudComponent.NotifyDamage(hit.VictimIdentityId, hit.Group);
            }
        }
    }
}