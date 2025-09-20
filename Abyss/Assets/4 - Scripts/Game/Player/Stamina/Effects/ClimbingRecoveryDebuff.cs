namespace Game.Player.Stamina.Effects
{
    public class ClimbingRecoveryDebuff : StaminaEffect
    {
        // �� ��������� ��������� ���������� (�� �������)
        public override bool AllowMultipleSources => false;
        public override bool DefaultRefreshExisting => true;

        // ���� ������ ������ ������� ������� ����� (per stack).
        // �� ���������� ������������� ��������, ����� currentRecoveryRate = defaultRecoveryRate + sum(modifiers)
        public override float GetRecoveryModifier(PlayerStamina s)
        {
            // ��������� �� ������� ��������. ���� ����� ��������� ������, ������ �� factor (0..1).
            return -s.DefaultRecoveryRate;
        }

        protected override void OnExpired(PlayerStamina s) { }
    }
}