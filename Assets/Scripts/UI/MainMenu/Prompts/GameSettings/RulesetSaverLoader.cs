using NSMB.Networking;
using NSMB.UI.MainMenu;
using NSMB.UI.MainMenu.Submenus.Prompts;
using NUnit.Framework;
using Quantum;
using UnityEditor.Build.Content;
using UnityEngine;

public class RulesetSaverLoader : MonoBehaviour
{
    [SerializeField] private MainMenuCanvas Canvas;
    private const string CODE_SEPARATOR = "-";//-
    private const string CODE_List_SEPARATOR = ".";//.
    private const string CODE_List_SEPARATOR2 = ",";//,
    private const string CODE_List_SEPARATOR3 = "$";//new
    private const string CODE_VERSION = "K0";

    private const int MEGALIST_MAX = 64, EXTRALISTMAX = 10;

    public void OnSavePressed() {
        GUIUtility.systemCopyBuffer = RulesetToCode();
        Canvas.PlaySound(SoundEffect.UI_Decide);
    }

    public unsafe void OnLoadPressed() {
        QuantumGame game = NetworkHandler.Game;
        PlayerRef host = game.Frames.Predicted.Global->Host;
        if (!game.PlayerIsLocal(host)) {
            Canvas.PlaySound(SoundEffect.UI_Error);
        }
        if (CodeToRuleset(GUIUtility.systemCopyBuffer.ToUpper())) {
            // succeeded...
            Canvas.PlaySound(SoundEffect.UI_Decide);
            Canvas.GoBack();
            Canvas.GoBack();
        } else {
            // failed!!
            Canvas.PlaySound(SoundEffect.UI_Error);
        }
    }

    public static unsafe string RulesetToCode() {
        var code = "";
        Frame f = NetworkHandler.Game.Frames.Predicted;
        GameRules rules = f.Global->Rules;
        var disabledstages = f.ResolveHashSet(rules.RandomDisabledStages);
        var items = f.ResolveList(rules.Items);
        var hazards = f.ResolveList(rules.Hazards);

        //code += f.SimulationConfig.AllGamemodes.IndexOf(rules.Gamemode) + CODE_SEPARATOR; //gamemode doesn't exist
        /*code += rules.Stage.Id + CODE_SEPARATOR;
        code += (int) rules.ChooseMode + CODE_SEPARATOR;
        var disabledstagesCount = disabledstages.Count;
        foreach (var disabledstage in disabledstages) {
            code += disabledstage.Id + CODE_List_SEPARATOR2;
            disabledstagesCount--;
            if (disabledstagesCount > 0) code += CODE_List_SEPARATOR;
        }*/

        code += rules.StarsToWin + CODE_SEPARATOR;
        code += rules.CoinsForPowerup + CODE_SEPARATOR;
        code += rules.Lives + CODE_SEPARATOR;
        code += rules.TimerMinutes + CODE_SEPARATOR;
        code += BTI(rules.TeamsEnabled) + CODE_SEPARATOR;
        code += BTI(rules.ModifierHazardsEnabled) + CODE_SEPARATOR;

        code += (int) rules.TeamAttack + CODE_SEPARATOR;
        code += rules.StarFountain + CODE_SEPARATOR;

        code += rules.StarFrequency + CODE_SEPARATOR;

        code += BTI(rules.RouletteBlocksEnabled) + CODE_SEPARATOR;
        var itemCount = items.Count;
        if (itemCount <= MEGALIST_MAX) {
            foreach (var item in items) {
                code += item.PrototypeRef + CODE_List_SEPARATOR2;
                var specialValues = f.ResolveList(item.Extra.Extra);
                if (specialValues.Count <= EXTRALISTMAX) {
                    foreach (var extra in specialValues) {
                        code += extra + CODE_List_SEPARATOR3;
                    }
                }
                //code += item.Team + CODE_List_SEPARATOR2;//unused
                itemCount--;
                if (itemCount > 0) 
                    code += CODE_List_SEPARATOR;
            }
        }
        code += CODE_SEPARATOR;

        code += rules.MaxHazards + CODE_SEPARATOR;
        code += rules.HazardFrequency + CODE_SEPARATOR;
        code += rules.HeftyPercentage + CODE_SEPARATOR;
        code += rules.HazardLifetime + CODE_SEPARATOR;
        var hazardCount = hazards.Count;
        if (hazardCount <= MEGALIST_MAX) {
            foreach (var hazard in hazards) {
                //code += hazard.Name + CODE_List_SEPARATOR2;//this isn't needed to save, just makes it more readable
                code += hazard.PrototypeRef + CODE_List_SEPARATOR2;
                var specialValues = f.ResolveList(hazard.Extra.Extra);
                if (specialValues.Count <= EXTRALISTMAX) {
                    foreach (var extra in specialValues) {
                        code += extra + CODE_List_SEPARATOR3;
                    }
                }
                code += BTI(hazard.SpawnHazard) + CODE_List_SEPARATOR2;
                //code += hazard.Team + CODE_List_SEPARATOR2;//unused
                //code += BTI(hazard.SpawnFridge) + CODE_List_SEPARATOR2;//unused
                hazardCount--;
                if (hazardCount > 0) 
                    code += CODE_List_SEPARATOR;
            }
        }
        code += CODE_SEPARATOR;

        code += BTI(rules.DisableStageRestrictions) + CODE_SEPARATOR;
        code += BTI(rules.DisableComplexStageRestrictions) + CODE_SEPARATOR;
        code += BTI(rules.EveryItemHasTheSameChance) + CODE_SEPARATOR;

        //code += rules.CoinDeathPenalty + CODE_SEPARATOR; //coinrunners doesn't exist
        //dictionary<AssetRef<CoinItemAsset>, FP> CoinItemCustomSpawnWeights; //do not save.

        //bulb isn't implemented
        //code += rules.ModifierBulbEnabled + CODE_SEPARATOR;
        //code += rules.BulbAbilityCount + CODE_SEPARATOR;
        //code += rules.HostControl + CODE_SEPARATOR;

        code += CODE_VERSION + CODE_SEPARATOR;

        var sum = 0;
        foreach (var c in code) {
            sum += c;
        }
        code += (sum % 256).ToString("X2");

        return code;

        string BTI(bool boolean) {
            return boolean ? "1" : "0";
        }
    }

    private unsafe bool CodeToRuleset(string code) {
        // basically the reverse of above...
        Frame f = NetworkHandler.Game.Frames.Predicted;
        GameRules rules = f.Global->Rules;
        QuantumGame game = QuantumRunner.DefaultGame;
        int slot = game.GetLocalPlayerSlots()[game.GetLocalPlayers().IndexOf(game.Frames.Predicted.Global->Host)];
        int code_version;
        
        var parts = code.Split(CODE_SEPARATOR);
        if (parts.Length != 21) //version 0 has 23 parts
            return false;
        
        var sum = 0;
        for (int i = 0; i < code.Length - 2; i++) {
            sum += code[i];
        }
        if (((sum % 256).ToString("X2")) != parts[^1]) 
            return false;

        //var enabled = CommandChangeRules.Rules.StarsToWin | CommandChangeRules.Rules.CoinsForPowerup | CommandChangeRules.Rules.Lives | 

        CommandChangeRules cmd = new CommandChangeRules {

            EnabledChanges = (CommandChangeRules.Rules) (((CommandChangeRules.Rules) int.MaxValue) - CommandChangeRules.Rules.Gamemode - CommandChangeRules.Rules.Stage - CommandChangeRules.Rules.StageChooseMode),
            Stage = rules.Stage,
            StarsToWin = int.Parse(parts[0]),
            CoinsForPowerup = int.Parse(parts[1]),
            Lives = int.Parse(parts[2]),
            TimerMinutes = int.Parse(parts[3]),
            TeamsEnabled = parts[4] == "1",

            TeamAttack = int.Parse(parts[6]),
            StarFountain = int.Parse(parts[7]),
            ChooseMode = rules.ChooseMode,

            StarFrequency = int.Parse(parts[8]),

            RouletteEnabled = parts[9] == "1",

            HazardEnabled = parts[5] == "1",
            MaxHazards = int.Parse(parts[11]),
            HazardFrequency = int.Parse(parts[12]),
            HeftyPercentage = int.Parse(parts[13]),
            HazardLifetime = int.Parse(parts[14]),

            DisableStageRestrictions = parts[15] == "1",
            DisableComplexStageRestrictions = parts[16] == "1", //can only be edited
            EveryItemHasTheSameChance = parts[17] == "1",

            //code += rules.CoinDeathPenalty + CODE_SEPARATOR; //coinrunners doesn't exist
            //dictionary<AssetRef<CoinItemAsset>, FP> CoinItemCustomSpawnWeights; //do not save.

            //bulb isn't implemented
            //code += rules.ModifierBulbEnabled + CODE_SEPARATOR;
            //code += rules.BulbAbilityCount + CODE_SEPARATOR;
            //code += rules.HostControl + CODE_SEPARATOR;
        };
        game.SendCommand(slot, cmd);

        /*var triggerIndex = 0;
        game.SendCommand(new CommandChangeTriggers { RemoveAll = true });
        foreach (var triggerCode in parts[7].Split('.')) {
            var triggerParts = triggerCode.Split(',');
            if (triggerParts.Length < 12) continue;
            var condition = (TriggerCondition)int.Parse(triggerParts[0]);
            var conditionParameter = "";
            if (TriggerMappings.ConditionParameters.TryGetValue(condition, out var parameters) &&
                int.TryParse(triggerParts[1], out var i) && i >= 0 && i < parameters.Count) {
                conditionParameter = parameters[i];
            }
            var conditionTarget = (TriggerTarget)int.Parse(triggerParts[2]);
            var action = (TriggerAction)int.Parse(triggerParts[3]);
            var actionParameter = "";
            if (TriggerMappings.ActionParameters.TryGetValue(action, out parameters) &&
                int.TryParse(triggerParts[4], out var j) && j >= 0 && j < parameters.Count) {
                actionParameter = parameters[j];
            }
            var actionTarget = (TriggerTarget)int.Parse(triggerParts[5]);
            var constraint = (TriggerConstraint)int.Parse(triggerParts[6]);
            var constraintParameter = "";
            if (int.TryParse(triggerParts[7], out _)) constraintParameter = triggerParts[7];
            else if (TriggerMappings.ConstraintParameters.TryGetValue(constraint, out parameters) &&
                int.TryParse(triggerParts[7], out var k) && k >= 0 && k < parameters.Count) {
                constraintParameter = parameters[k];
            }
            var constraintTarget = (TriggerTarget)int.Parse(triggerParts[8]);
            var delaySeconds = byte.Parse(triggerParts[9]);
            var repeatCount = byte.Parse(triggerParts[10]);
            var chance = byte.Parse(triggerParts[11]);
            game.SendCommand(slot, new CommandChangeTriggers {
                Index = triggerIndex,
                TriggerCondition = (int)condition,
                TriggerConditionParameter = conditionParameter,
                TriggerConditionTarget = (int)conditionTarget,
                TriggerAction = (int)action,
                TriggerActionParameter = actionParameter,
                TriggerActionTarget = (int)actionTarget,
                TriggerConstraint = (int)constraint,
                TriggerConstraintParameter = constraintParameter,
                TriggerConstraintTarget = (int)constraintTarget,
                TriggerDelaySeconds = delaySeconds,
                TriggerRepeatCount = repeatCount,
                TriggerChance = chance,
            });
            triggerIndex++;
        }*/
        
        // c'est fini, everyone clapped.
        // kkt claps in unison
        return true;
    }
}
