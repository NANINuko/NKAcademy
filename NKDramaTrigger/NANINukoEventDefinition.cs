using System;

namespace NANINuko.Framework.Data
{
    [Serializable]
    public sealed class NANINukoEventDefinition
    {
        public string Id = string.Empty;

        public bool Enabled = true;

        public NANINukoTriggerType TriggerType = NANINukoTriggerType.Key;

        public string PreFlags = string.Empty;

        public string ZoneId = string.Empty;

        public string KeyName = string.Empty;

        public string CharaId = string.Empty;

        public NANINukoCharaDetectType CharaDetectType = NANINukoCharaDetectType.Dead;

        public int XMin;

        public int XMax;

        public int ZMin;

        public int ZMax;

        public string DramaSheet = string.Empty;

        public string DramaStep = string.Empty;

        public string SetFlag = string.Empty;

        public string Memo = string.Empty;
    }

    public enum NANINukoTriggerType
    {
        Key,
        ZoneEnter,
        Position,
        Chara
    }

    public enum NANINukoCharaDetectType
    {
        Dead,
        Missing
    }
}
