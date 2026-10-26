using UnityEngine;
[System.Serializable]public sealed class LegacyBattleRuleValues {public float knockdownSeconds=.35f;public float downHoldSeconds=.55f;public float getUpSeconds=.9f;public bool allowDownFollowup=false;public int maxDownFollowups=1;public float downFollowupDamageScale=.25f;public float receiverGain=4f;public float dashGainPerSecond=12f;public float tensionLevelGainRate=.1f;}
[CreateAssetMenu(menuName="Shioka/Compatibility/Legacy battle rules")]public sealed class LegacyBattleRules:ScriptableObject {public LegacyBattleRuleValues values=new LegacyBattleRuleValues();}
