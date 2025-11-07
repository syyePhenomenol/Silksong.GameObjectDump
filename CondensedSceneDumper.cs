using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using QuestPlaymakerActions;
using Silksong.GameObjectDump.Logging;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Silksong.GameObjectDump;

/// <summary>
/// Dumps "useful" Components and FsmStateActions in the scene.
/// Includes anything that directly reads or sets PlayerData and SceneData.
/// Also includes other useful stuff like transition points, respawn points, quests and tools.
/// </summary>
public static class CondensedSceneDumper
{
    private static readonly BufferedYamlLogger _logger = new("condensedScenes.yaml");

    public static void DumpScene(Scene scene)
    {
        Silksong_GameObjectDumpPlugin.Log($"Dumping {scene.name}");
        var sceneGOs = GameObjectUtils.GetAllGameObjectsInScene(scene);
        Silksong_GameObjectDumpPlugin.Log($"{scene.name} has {sceneGOs.Count} objects");

        NestedLog sceneLog = new();
        NestedLog gosLog = new();

        foreach ((var path, var go) in sceneGOs)
        {
            gosLog.Add(path, Dump(go));
        }

        sceneLog.Add(scene.name, gosLog.Entries.Any() ? gosLog : "No logged objects in this scene.");
        _logger.Log(sceneLog);
    }

    /// <summary>
    /// Dumps the input object as a NestedLog.
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    private static object? Dump(object? obj)
    {
        NestedLog? log = new();

        switch (obj)
        {
            case NamedVariable nv:
                return Dump(nv.RawValue);

            case bool or int or string:
                return obj;
                
            case IEnumerable ie when obj is not Component:
                if (ie.GetCount() is 1)
                {
                    var enumerator = ie.GetEnumerator();
                    enumerator.MoveNext();
                    return Dump(enumerator.Current);
                }

                int idx = 0;
                foreach (var element in ie)
                {
                    log.Add(idx.ToString(), Dump(element));
                    idx++;
                }
                break;

            case GameObject go:
                foreach (var c in go.GetComponents<Component>())
                {
                    try
                    {
                        log.Add(c.GetType().Name, Dump(c));
                    }
                    catch (Exception e)
                    {
                        Silksong_GameObjectDumpPlugin.LogError($"Could not dump component {c.GetType()?.Name ?? null}");
                    }
                }
                break;

            case PlayMakerFSM pmfsm:
                foreach (var state in pmfsm.FsmStates)
                {
                    log.Add(state.Name, Dump(state));
                }
                break;

            case FsmState fsmStates:
                foreach (var action in fsmStates.Actions)
                {
                    log.Add(action.GetType().Name, Dump(action));
                }
                break;

            case FsmStateAction fsmStateAction:
                if (!fsmStateAction.enabled)
                {
                    return null;
                }
                switch (fsmStateAction)
                {
                    case AddScenesMapped:
                    case AddScenesVisited:
                    case AutoEquipCrest aec:
                        DumpAllFields(fsmStateAction);
                        break;
                    // case ApplyMusicCue amc:
                    //     AddByReflection(amc, nameof(amc.musicCue));
                    //     break;
                    case AutoEquipCrestV2 aecv2:
                        DumpByReflection(aecv2, nameof(aecv2.Crest));
                        break;
                    case AutoEquipCrestV3 aecv3:
                        DumpByReflection(aecv3, nameof(aecv3.Crest));
                        DumpByReflection(aecv3, nameof(aecv3.IsTemp));
                        break;
                    case AutoEquipCrestV4 aecv4:
                        DumpByReflection(aecv4, nameof(aecv4.Crest));
                        DumpByReflection(aecv4, nameof(aecv4.IsTemp));
                        DumpByReflection(aecv4, nameof(aecv4.RemoveTools));
                        break;
                    case AutoEquipTool:
                    case CaravanLocationSwitch:
                    case CheckIfCrestEquipped:
                        DumpAllFields(fsmStateAction);
                        break;
                    case CheckIfToolEquipped cite:
                        DumpByReflection(cite, nameof(cite.Tool));
                        break;
                    case CheckIfToolUnlocked:
                    case CheckHasVisitedScene:
                        DumpAllFields(fsmStateAction);
                        break;
                    case CheckIsRespawningOnMarker cirom:
                        GameObject safe = cirom.MarkerObject.GetSafe(cirom);
                        log.Add(nameof(cirom.MarkerObject), Dump(safe ? safe.name : null));
                        break;
                    case CheckQuestPdSceneBool:
                    case CollectableItemAction:
                        DumpAllFields(fsmStateAction);
                        break;
                    case CountCrestUnlockPoints ccup:
                        DumpByReflection(ccup, nameof(ccup.CrestList));
                        break;
                    case DialogueYesNoItem dyni:
                        DumpByReflection(dyni, nameof(dyni.RequiredItem));
                        DumpByReflection(dyni, nameof(dyni.WillGetItem));
                        break;
                    case DialogueYesNoItemV2 dyniv2:
                        DumpByReflection(dyniv2, nameof(dyniv2.RequiredItem));
                        DumpByReflection(dyniv2, nameof(dyniv2.WillGetItem));
                        break;
                    case DialogueYesNoItemV3 dyniv3:
                        DumpByReflection(dyniv3, nameof(dyniv3.RequiredItem));
                        DumpByReflection(dyniv3, nameof(dyniv3.WillGetItem));
                        break;
                    case DialogueYesNoItemV4 dyniv4:
                        DumpByReflection(dyniv4, nameof(dyniv4.RequiredItem));
                        DumpByReflection(dyniv4, nameof(dyniv4.WillGetItem));
                        break;
                    case DialogueYesNoItemV5 dyniv5:
                        DumpByReflection(dyniv5, nameof(dyniv5.RequiredItems));
                        DumpByReflection(dyniv5, nameof(dyniv5.WillGetItem));
                        break;
                    case GetIsCrestUnlocked:
                        DumpAllFields(fsmStateAction);
                        break;
                    case GetPersistentBoolFromSaveData gpbfsd:
                        DumpByReflection(gpbfsd, nameof(gpbfsd.SceneName));
                        DumpByReflection(gpbfsd, nameof(gpbfsd.ID));
                        break;
                    case GetPersistentIntFromSaveData gpifsd:
                        DumpByReflection(gpifsd, nameof(gpifsd.SceneName));
                        DumpByReflection(gpifsd, nameof(gpifsd.ID));
                        break;
                    case GetPlayerDataBool gpdb:
                        if (string.IsNullOrEmpty(gpdb.boolName.Value))
                            // || gpdb.boolName.Value is "blackThreadWorld")
                        {
                            return null;
                        }
                        DumpByReflection(gpdb, nameof(gpdb.boolName));
                        break;
                    case GetPlayerDataFloat gpdf:
                        DumpByReflection(gpdf, nameof(gpdf.floatName));
                        break;
                    case GetPlayerDataInt gpdi:
                        if (string.IsNullOrEmpty(gpdi.intName.Value)
                            || gpdi.intName.Value is "geo")
                        {
                            return null;
                        }
                        DumpByReflection(gpdi, nameof(gpdi.intName));
                        break;
                    case GetPlayerDataString gpds:
                        DumpByReflection(gpds, nameof(gpds.stringName));
                        break;     
                    case GetPlayerDataVariable gpdv:
                        DumpByReflection(gpdv, nameof(gpdv.VariableName));
                        break;
                    case GetQuestReward gqr:
                        DumpByReflection(gqr, nameof(gqr.StoreReward));
                        break;
                    case GetQuestRewardV2 gqrv2:
                        DumpByReflection(gqrv2, nameof(gqrv2.StoreReward));
                        break;
                    case GetToolEquipInfo gtei:
                        DumpByReflection(gtei, nameof(gtei.Tool));
                        break;
                    case IncrementPlayerDataInt:
                        DumpAllFields(fsmStateAction);
                        break;
                    case PlayerDataBoolMultiTest pdbmt:
                        DumpByReflection(pdbmt, nameof(pdbmt.boolTests));
                        break;
                    case PlayerDataBoolTest pdbt:
                        if (string.IsNullOrEmpty(pdbt.boolName.Value)
                            || pdbt.boolName.Value is "isInvincible" or "atBench")
                            // || (pdbt.boolName.Value is "blackThreadWorld" && !pdbt.boolCheck)) // very spammy
                        {
                            return null;
                        }
                        DumpByReflection(pdbt, nameof(pdbt.boolName));
                        DumpByReflection(pdbt, nameof(pdbt.boolCheck));
                        break;
                    case PlayerDataBoolTrueAndFalse pdbtaf:
                        DumpByReflection(pdbtaf, nameof(pdbtaf.trueBool));
                        DumpByReflection(pdbtaf, nameof(pdbtaf.falseBool));
                        break;
                    case PlayerDataIntAdd:
                        DumpAllFields(fsmStateAction);
                        break;
                    case PlayerdataIntCompare pdic:
                        DumpByReflection(pdic, nameof(pdic.playerdataInt));
                        DumpByReflection(pdic, nameof(pdic.compareTo));
                        break;
                    case PlayerDataVariableTest pdvt:
                        if (string.IsNullOrEmpty(pdvt.VariableName.Value))
                        {
                            return null;
                        }
                        DumpByReflection(pdvt, nameof(pdvt.VariableName));
                        log.Add(nameof(pdvt.ExpectedValue), Dump(pdvt.ExpectedValue.GetValue()));
                        break;
                    case QuestCompleteYesNo qcyn:
                        DumpByReflection(qcyn, nameof(qcyn.Quest));
                        break;
                    case QuestYesNo qyn:
                        DumpByReflection(qyn, nameof(qyn.Quest));
                        break;
                    case QuestYesNoV2 qynv2:
                        DumpByReflection(qynv2, nameof(qynv2.Quest));
                        break;
                    case QuestFsmAction qfa:
                        if (qfa.Quest.Value is null)
                        {
                            return null;
                        }
                        DumpAllFields(fsmStateAction);
                        break;
                    case RecordJournalKill rjk:
                        DumpAllFields(fsmStateAction);
                        break;
                    case RecordJournalKillV2 rjkv2:
                        DumpByReflection(rjkv2, nameof(rjkv2.Record));
                        break;
                    case SavedItemCanGetMore sicgm:
                        DumpByReflection(sicgm, nameof(sicgm.Item));
                        break;
                    case SavedItemGet sig:
                        DumpByReflection(sig, nameof(sig.Item));
                        break;
                    case SavedItemGetV2 sigv2:
                        DumpByReflection(sigv2, nameof(sigv2.Item));
                        break;
                    case SetCollectablePickupItem scpi:
                        DumpByReflection(scpi, nameof(scpi.Item));
                        break;
                    case SetCollectablePickupItemV2 scpiv2:
                        DumpByReflection(scpiv2, nameof(scpiv2.Item));
                        break;
                    case SetCurrentRaceTrack scrt:
                        DumpByReflection(scrt, nameof(scrt.GetReward));
                        break;
                    case SetCustomToolOverride scto:
                        DumpByReflection(scto, nameof(scto.Tool));
                        break;
                    case SetDeathRespawnOtherScene:
                        DumpAllFields(fsmStateAction);
                        break;
                    case SetDeathRespawn sdr:
                        DumpByReflection(sdr, nameof(sdr.respawnMarkerName));
                        DumpByReflection(sdr, nameof(sdr.respawnType));
                        DumpByReflection(sdr, nameof(sdr.respawnFacingRight));
                        break;
                    case SetDeathRespawnNonLethal sdrnl:
                        DumpByReflection(sdrnl, nameof(sdrnl.respawnMarkerName));
                        DumpByReflection(sdrnl, nameof(sdrnl.respawnType));
                        DumpByReflection(sdrnl, nameof(sdrnl.respawnFacingRight));
                        break;
                    case SetDeathRespawnV2 sdrv2:
                        DumpAllFields(sdrv2);
                        break;
                    case SetDeathRespawnMarker sdrm:
                        GameObject safe2 = sdrm.Target.GetSafe(sdrm);
                        if ((bool)safe2)
                        {
                            RespawnMarker component = safe2.GetComponent<RespawnMarker>();
                            log.Add("Respawn Marker", Dump(sdrm.Target));
                        }
                        break;
                    case SetHazardRespawn shr:
                        DumpByReflection(shr, nameof(shr.hazardRespawnMarker));
                        break;
                    case SetPersistentBoolSaveData spbsd:
                        DumpAllFields(spbsd);
                        break;
                    case SetPersistentIntSaveData spisd:
                        DumpAllFields(spisd);
                        break;
                    case HutongGames.PlayMaker.Actions.SetPlayerDataBool spdb:
                        if (string.IsNullOrEmpty(spdb.boolName.Value)
                            || spdb.boolName.Value is "disablePause" or "disableInventory" or "disableSaveQuit")
                        {
                            return null;
                        }
                        DumpByReflection(spdb, nameof(spdb.boolName));
                        DumpByReflection(spdb, nameof(spdb.value));
                        break;
                    case SetPlayerDataFloat spdf:
                        DumpByReflection(spdf, nameof(spdf.floatName));
                        DumpByReflection(spdf, nameof(spdf.value));
                        break;
                    case SetPlayerDataInt spdi:
                        DumpByReflection(spdi, nameof(spdi.intName));
                        DumpByReflection(spdi, nameof(spdi.value));
                        break;
                    case SetPlayerDataString spds:
                        DumpByReflection(spds, nameof(spds.stringName));
                        DumpByReflection(spds, nameof(spds.value));
                        break;
                    case SetPlayerDataVariable spdv:
                        if (string.IsNullOrEmpty(spdv.VariableName.Value)
                            || spdv.VariableName.Value is "disablePause" or "disableInventory" or "disableSaveQuit")
                        {
                            return null;
                        }
                        DumpByReflection(spdv, nameof(spdv.VariableName));
                        log.Add(nameof(spdv.SetValue), Dump(spdv.SetValue.GetValue()));
                        break;
                    case SetPoisonTintReadFromTool sptrft:
                        DumpByReflection(sptrft, nameof(sptrft.Tool));
                        break;
                    case SetToolLocked stl:
                        DumpByReflection(stl, nameof(stl.Tool));
                        break;
                    case SetToolUnlocked stu:
                        DumpByReflection(stu, nameof(stu.Tool));
                        break;
                    case ShowControlReminderSingle scrs:
                        DumpByReflection(scrs, nameof(scrs.PlayerDataBool));
                        break;
                    case ShowControlReminderSingleGroup scrsg:
                        DumpByReflection(scrsg, nameof(scrsg.PlayerDataBool));
                        break;
                    case ShowToolCrestUIMsg stcuim:
                        DumpByReflection(stcuim, nameof(stcuim.Crest));
                        break;
                    case SpawnSkillGetMsg ssgm:
                        DumpByReflection(ssgm, nameof(ssgm.Skill));
                        break;
                    case UnlockCrest uc:
                        DumpByReflection(uc, nameof(uc.Crest));
                        break;
                    case UpdateGameMapIfQueued ugmiq:
                        DumpByReflection(ugmiq, nameof(ugmiq.delay));
                        break;
                    default:
                        break;
                }
                break;

            case CaravanLocationSwitch.LocationSwitch clsls:
                DumpByReflection(clsls, nameof(clsls.Location));
                break;

            case PlayerDataBoolMultiTest.BoolTest pdbmtbt:
                DumpByReflection(pdbmtbt, nameof(pdbmtbt.boolName));
                DumpByReflection(pdbmtbt, nameof(pdbmtbt.inputBool));
                DumpByReflection(pdbmtbt, nameof(pdbmtbt.expectedValue));
                break;

            // Components
            case ActivateAllChildren aac:
                DumpAllFields(aac);
                break;

            case ActivateIfPlayerdataFalse aipf:
                DumpByReflection(aipf, nameof(aipf.boolName));
                log.Add(nameof(aipf.objectToActivate), aipf.objectToActivate != null ? aipf.objectToActivate.name : null);
                break;

            case ActivateIfPlayerdataTrue aipt:
                DumpByReflection(aipt, nameof(aipt.boolName));
                log.Add(nameof(aipt.objectToActivate), aipt.objectToActivate != null ? aipt.objectToActivate.name : null);
                break;

            case AdditiveLoreSceneController alsc:
                DumpByReflection(alsc, nameof(alsc.activePDTest));
                DumpByReflection(alsc, nameof(alsc.startWaitTime));
                DumpByReflection(alsc, nameof(alsc.sceneName));
                log.Add(nameof(alsc.loreSceneOnly), alsc.loreSceneOnly != null ? alsc.loreSceneOnly.name : null);
                break;

            case AreaTitleController atc:
                DumpByReflection(atc, nameof(atc.waitForEvent));
                DumpByReflection(atc, nameof(atc.orderedAreas));
                DumpByReflection(atc, nameof(atc.area));
                DumpByReflection(atc, nameof(atc.displayRight));
                DumpByReflection(atc, nameof(atc.doorTrigger));
                DumpByReflection(atc, nameof(atc.doorException));
                DumpByReflection(atc, nameof(atc.onlyOnRevisit));
                DumpByReflection(atc, nameof(atc.recordVisitedOnSkip));
                DumpByReflection(atc, nameof(atc.unvisitedPause));
                DumpByReflection(atc, nameof(atc.visitedPause));
                DumpByReflection(atc, nameof(atc.waitForTrigger));
                DumpByReflection(atc, nameof(atc.ignoreFirstSceneCheckIfUnvisited));
                DumpByReflection(atc, nameof(atc.alwaysBlockInteractIfUnvisited));
                DumpByReflection(atc, nameof(atc.onlyIfUnvisited));
                break;

            case AreaTitleController.Area area:
                DumpAllFields(area);
                break;

            case AtmosRegion ar:
                DumpByReflection(ar, nameof(ar.enterAtmosCue));
                DumpByReflection(ar, nameof(ar.exitAtmosCue));
                break;

            case BattleScene bs:
                DumpByReflection(bs, nameof(bs.setPDBoolOnEnd));
                DumpByReflection(bs, nameof(bs.setExtraPDBoolOnEnd));
                // AddByReflection(bs, nameof(bs.musicCueStart));
                // AddByReflection(bs, nameof(bs.musicCueNone));
                // AddByReflection(bs, nameof(bs.musicCueEnd));
                break;

            case BellBench bb:
                DumpByReflection(bb, nameof(bb.fixedPDBool));
                break;

            case BellShrineGateLock bsgl:
                DumpByReflection(bsgl, nameof(bsgl.bellLocks));
                break;

            case BellShrineGateLock.BellLock bsglbl:
                DumpByReflection(bsglbl, nameof(bsglbl.PdBool));
                break;

            case Breakable breakable:
                DumpByReflection(breakable, nameof(breakable.itemDropGroups));
                break;

            case Breakable.ItemDropGroup bidg:
                DumpByReflection(bidg, nameof(bidg.Drops));
                break;

            case Breakable.ItemDropProbability bidp:
                DumpByReflection(bidp, nameof(bidp.item));
                break;

            case CollectableItemPickup cip:
                DumpByReflection(cip, nameof(cip.item));
                DumpByReflection(cip, nameof(cip.playerDataBool));
                break;

            case CollectionGramaphone cg:
                log ??= new();
                DumpByReflection(cg, nameof(cg.playingPdField));
                break;

            case CorpseItems ci:
                DumpByReflection(ci, nameof(ci.itemPickupGroups));
                break;

            case CorpseItems.ItemPickupGroup ciipg:
                DumpByReflection(ciipg, nameof(ciipg.Drops));
                break;

            case CorpseItems.ItemPickupProbability ciipp:
                DumpByReflection(ciipp, nameof(ciipp.item));
                break;

            case CrossSceneWalker csw:
                DumpByReflection(csw, nameof(csw.activeCondition));
                break;

            case CurrencyObjectBase cob:
                log.Add("Type", Dump(cob.GetType().Name));
                DumpByReflection(cob, nameof(cob.magnetTool));
                DumpByReflection(cob, nameof(cob.magnetBuffTool));
                // DumpByReflection(cob, nameof(cob.firstGetPDBool));
                // DumpByReflection(cob, nameof(cob.popupPDBool));

                break;

            case CurrencyCounterAppearRegion ccar:
                DumpByReflection(ccar, nameof(ccar.showCounters));
                break;

            case CurrencyCounterAppearRegion.CounterInfo ccarci:
                DumpByReflection(ccarci, nameof(ccarci.CollectableItem));
                break;

            case DamageEnemies de:
                DumpByReflection(de, nameof(de.representingTool));
                break;

            case DeactivateIfPlayerdataFalse dipdf:
                DumpByReflection(dipdf, nameof(dipdf.boolName));
                break;

            case DeactivateIfPlayerdataTrue dipdt:
                DumpByReflection(dipdt, nameof(dipdt.boolName));
                break;

            case DeactivateOnHeroRespawnPoint dohrp:
                DumpByReflection(dohrp, nameof(dohrp.respawnPointName));
                DumpByReflection(dohrp, nameof(dohrp.respawnSceneName));
                break;

            case DeactivatePlayerDataTest dpdt:
                DumpByReflection(dpdt, nameof(dpdt.test));
                break;

            case DeactivateSavedItemCondition dsic:
                DumpByReflection(dsic, nameof(dsic.item));
                break;

            case DoorTargetCondition dtc:
                DumpByReflection(dtc, nameof(dtc.condition));
                break;

            case EnemyDeathEffects ede:
                if (string.IsNullOrEmpty(ede.setPlayerDataBool))
                {
                    return null;
                }
                // log.Add("Type", Dump(ede.GetType().Name));
                DumpByReflection(ede, nameof(ede.setPlayerDataBool));
                break;

            // case EnviroRegionListener erl:
            //     AddByReflection(erl, nameof(erl.insideRegions));
            //     break;

            case FastTravelMapButton ftmb:
                DumpByReflection(ftmb, nameof(ftmb.playerDataBool));
                break;

            case FastTravelMapPiece ftmp:
                DumpByReflection(ftmp, nameof(ftmp.pairedButton));
                break;

            case HealthManager hm:
                DumpByReflection(hm, nameof(hm.itemDropGroups));
                break;

            case HealthManager.ItemDropGroup hiidg:
                DumpByReflection(hiidg, nameof(hiidg.Drops));
                break;

            case HealthManager.ItemDropProbability hiidp:
                DumpByReflection(hiidp, nameof(hiidp.item));
                break;

            // case HazardRespawnMarker hrm:
            //     log.Add("Position", Dump(hrm.transform != null ? hrm.transform.position : null));
            //     AddByReflection(hrm, nameof(hrm.respawnFacingDirection));
            //     break;

            // case HazardRespawnTrigger hrt:
            //     AddByReflection(hrt, nameof(hrt.respawnMarker));
            //     break;

            case HeroCorpseMarkerProxy hcmp:
                DumpByReflection(hcmp, nameof(hcmp.targetGuid));
                DumpByReflection(hcmp, nameof(hcmp.readScenePosFromStaticVar));
                break;

            case InteractableBase ib:
                DumpByReflection(ib, nameof(ib.interactLabel));
                switch (ib)
                {
                    case BasicNPC bnpc:
                        DumpByReflection(bnpc, nameof(bnpc.giveOnFirstTalkItems));
                        break;
                    case BasicNPCBoolTest bnbt:
                        DumpByReflection(bnbt, nameof(bnbt.stateTracker));
                        DumpByReflection(bnbt, nameof(bnbt.talks));
                        break;
                    case CollectionViewerDesk cvd:
                        log.Add(nameof(cvd.mementosParent), Dump(cvd.mementosParent != null ? cvd.mementosParent.name : null));
                        log.Add(nameof(cvd.heartMementosGroup), Dump(cvd.heartMementosGroup != null ? cvd.heartMementosGroup.name : null));
                        log.Add(nameof(cvd.heartMementosParent), Dump(cvd.heartMementosParent != null ? cvd.heartMementosParent.name : null));
                        DumpByReflection(cvd, nameof(cvd.sections));
                        break;
                    case InspectDoor id:
                        DumpByReflection(id, nameof(id.promptText));
                        break;
                    case ItemReceptacle ir:
                        DumpByReflection(ir, nameof(ir.requiredItem));
                        DumpByReflection(ir, nameof(ir.playerDataBool));
                        break;
                    case QuestBoardInteractable qbi:
                        DumpByReflection(qbi, nameof(qbi.activeCondition));
                        break;
                    case SteelSoulQuestSpot ssqs:
                        DumpByReflection(ssqs, nameof(ssqs.quest));
                        break;
                    case TransitionPoint tp:
                        log.Add("scene", Dump(tp.gameObject != null ? tp.gameObject.scene.name : null));
                        log.Add("BoxCollider enabled", Dump(tp.gameObject.GetComponent<BoxCollider2D>() is BoxCollider2D boxCollider && boxCollider != null && boxCollider.enabled));
                        DumpByReflection(tp, nameof(tp.isInactive));
                        DumpByReflection(tp, nameof(tp.isADoor));
                        DumpByReflection(tp, nameof(tp.dontWalkOutOfDoor));
                        DumpByReflection(tp, nameof(tp.entryDelay));
                        DumpByReflection(tp, nameof(tp.alwaysEnterRight));
                        DumpByReflection(tp, nameof(tp.alwaysEnterLeft));
                        DumpByReflection(tp, nameof(tp.hardLandOnExit));
                        DumpByReflection(tp, nameof(tp.targetScene));
                        DumpByReflection(tp, nameof(tp.entryPoint));
                        DumpByReflection(tp, nameof(tp.skipSceneMapCheck));
                        DumpByReflection(tp, nameof(tp.entryOffset));
                        DumpByReflection(tp, nameof(tp.alwaysUnloadUnusedAssets));
                        break;
                    default:
                        break;
                }
                break;

            case BasicNPCBoolTest.ConditionalTalk ct:
                if (string.IsNullOrEmpty(ct.BoolTest))
                {
                    return null;
                }
                DumpByReflection(ct, nameof(ct.BoolTest));
                DumpByReflection(ct, nameof(ct.ExpectedBoolValue));
                break;

            case CollectionViewerDesk.Section cvds:
                DumpByReflection(cvds, nameof(cvds.UnlockTest));
                DumpByReflection(cvds, nameof(cvds.UnlockItem));
                DumpByReflection(cvds, nameof(cvds.UnlockSaveBool));
                break;

            case Lever_tk2d ltk2d:
                DumpByReflection(ltk2d, nameof(ltk2d.setPlayerDataBool));
                break;

            case Lever lever:
                DumpByReflection(lever, nameof(lever.playerDataBool));
                break;

            case MazeController mc:
                DumpByReflection(mc, nameof(mc.isCapScene));
                log.Add(nameof(mc.entryDoors), Dump(mc.entryDoors.Select(tp => tp.name)));
                // DumpByReflection(mc, nameof(mc.entryDoors));
                DumpByReflection(mc, nameof(mc.sceneNames));
                DumpByReflection(mc, nameof(mc.neededCorrectDoors));
                DumpByReflection(mc, nameof(mc.allowedIncorrectDoors));
                DumpByReflection(mc, nameof(mc.restScenePoint));
                DumpByReflection(mc, nameof(mc.restSceneName));
                DumpByReflection(mc, nameof(mc.exitSceneName));
                DumpByReflection(mc, nameof(mc.entryMatchExit));
                break;

            case MazeController.EntryMatch mcem:
                DumpByReflection(mcem, nameof(mcem.EntryScene));
                DumpByReflection(mcem, nameof(mcem.EntryDoorDir));
                DumpByReflection(mcem, nameof(mcem.ExitDoorDir));
                break;

            case MazeCorpseSpawner mcs:
                log.Add(nameof(mcs.readScenesFromController), mcs.readScenesFromController != null ? Dump(mcs.readScenesFromController.sceneNames) : null);
                break;

            case MemoryOrbGroup mog:
                DumpByReflection(mog, nameof(mog.pdBitmask));
                DumpByReflection(mog, nameof(mog.readPdBool));
                break;

            case MemoryOrbSceneMapConditional mosmc:
                DumpByReflection(mosmc, nameof(mosmc.pdBool));
                DumpByReflection(mosmc, nameof(mosmc.pdBitmask));
                break;

            // case MusicRegion mr:
            //     AddByReflection(mr, nameof(mr.enterMusicCue));
            //     AddByReflection(mr, nameof(mr.exitMusicCue));
            //     break;

            case NPCEncounterStateController npcesc:
                DumpByReflection(npcesc, nameof(npcesc.encounterStateName));
                break;

            case PersistentBoolItem pbi:
                DumpByReflection(pbi, nameof(pbi.saveCondition));
                DumpByReflection(pbi, nameof(pbi.itemData));
                break;

            case PersistentEnemyItemDrop peid:
                DumpByReflection(peid, nameof(peid.item));
                DumpByReflection(peid, nameof(peid.enemyItemData));
                break;

            case PersistentIntItem pii:
                DumpByReflection(pii, nameof(pii.saveCondition));
                DumpByReflection(pii, nameof(pii.itemData));
                break;

            case PersistentItem<bool> piBool:
                DumpByReflection(piBool, nameof(piBool.itemData));
                break;

            case PersistentItem<int> pbInt:
                DumpByReflection(pbInt, nameof(pbInt.itemData));
                break;

            case PersistentItemData<bool> pidb:
                DumpByReflection(pidb, nameof(pidb.ID));
                DumpByReflection(pidb, nameof(pidb.SceneName));
                if (pidb.IsSemiPersistent)
                {
                    log.Add(nameof(pidb.IsSemiPersistent), true);
                }
                break;

            case PersistentItemData<int> pidi:
                DumpByReflection(pidi, nameof(pidi.ID));
                DumpByReflection(pidi, nameof(pidi.SceneName));
                if (pidi.IsSemiPersistent)
                {
                    log.Add(nameof(pidi.IsSemiPersistent), true);
                }
                break;

            case PersistentPressurePlate ppp:
                log.Add(nameof(ppp.persistent), Dump(ppp.persistent != null ? ppp.persistent.name : null));
                DumpByReflection(ppp, nameof(ppp.playerDataBool));
                break;

            case PlayerDataStringResponse pdsr:
                DumpByReflection(pdsr, nameof(pdsr.fieldName));
                break;

            case PlayerDataTestResponse pdtr:
                DumpByReflection(pdtr, nameof(pdtr.test));
                break;

            case QuestRewardHolder qrh:
                DumpByReflection(qrh, nameof(qrh.quest));
                DumpByReflection(qrh, nameof(qrh.pickupPdBool));
                break;

            case RecordDoorEntry rde:
                DumpByReflection(rde, nameof(rde.pdFromSceneName));
                break;

            case RelicBoardOwner rbo:
                DumpByReflection(rbo, nameof(rbo.completedBool));
                break;

            case RespawnMarker rm:
                DumpByReflection(rm, nameof(rm.name));
                DumpByReflection(rm, nameof(rm.respawnFacingRight));
                log.Add("Position", Dump(rm.transform.position));
                break;

            case SavedFleaActivator sfa:
                DumpByReflection(sfa, nameof(sfa.pdBoolTemplate));
                break;

            case SavedItemTrackerMarker sitm:
                DumpByReflection(sitm, nameof(sitm.items));
                break;

            case SceneAdditiveLoadConditional salc:
                DumpByReflection(salc, nameof(salc.tests));
                DumpByReflection(salc, nameof(salc.questTests));
                break;

            case ScenePreloader sp:
                DumpByReflection(sp, nameof(sp.sceneNameToLoad));
                log.Add(nameof(sp.entryGateWhiteList), Dump(sp.entryGateWhiteList.Select(tp => tp.name)));
                log.Add(nameof(sp.entryGateBlackList), Dump(sp.entryGateBlackList.Select(tp => tp.name)));
                DumpByReflection(sp, nameof(sp.test));
                break;

            case SetPlayerDataBool spdb:
                if (string.IsNullOrEmpty(spdb.boolName))
                {
                    return null;
                }
                DumpByReflection(spdb, nameof(spdb.boolName));
                DumpByReflection(spdb, nameof(spdb.value));
                break;

            case ShopItemStats sis:
                DumpByReflection(sis, nameof(sis.shopItem));
                break;

            case ShopOwnerBase sob:
                DumpByReflection(sob, nameof(sob.Stock));
                break;

            case SilkGrubCocoon sgc:
                DumpByReflection(sgc, nameof(sgc.dropItem));
                DumpByReflection(sgc, nameof(sgc.setPDBoolOnBreak));
                DumpByReflection(sgc, nameof(sgc.unsetPDBoolOnBreak));
                break;

            case SimpleShopMenuOwner ssmo:
                log.Add("Type", Dump(ssmo.GetType().Name));
                switch (ssmo)
                {
                    case SimpleQuestsShopOwner sqso:
                        DumpByReflection(sqso, nameof(sqso.quests));
                        DumpByReflection(sqso, nameof(sqso.purchasedDlgBitmask));
                        DumpByReflection(sqso, nameof(sqso.genericQuestsList));
                        DumpByReflection(sqso, nameof(sqso.genericQuestCap));
                        break;
                    default:
                        break;
                }
                break;

            case SimpleQuestsShopOwner.ShopItemInfo sqsosii:
                DumpByReflection(sqsosii, nameof(sqsosii.Quest));
                DumpByReflection(sqsosii, nameof(sqsosii.AppearCondition));
                DumpByReflection(sqsosii, nameof(sqsosii.AppearAfterCompleted));
                break;

            case SmashableTile st:
                DumpByReflection(st, nameof(st.startSmashedCondition));
                break;

            case SprintRaceController src:
                DumpByReflection(src, nameof(src.reward));
                break;

            // case SprintRaceLapMarker srlm:
            //     AddByReflection(srlm, nameof(srlm.hazardRespawnMarker));
            //     break;

            case StateChangeSequence scs:
                DumpByReflection(scs, nameof(scs.isCompleteBool));
                break;

            case TempGate tg:
                DumpByReflection(tg, nameof(tg.brokenPDBool));
                break;

            case TestGameObjectActivator tgoa:
                log.Add(nameof(tgoa.activateGameObject), Dump(tgoa.activateGameObject != null ? tgoa.activateGameObject.name : null));
                log.Add(nameof(tgoa.deactivateGameObject), Dump(tgoa.deactivateGameObject != null ? tgoa.deactivateGameObject.name : null));
                DumpByReflection(tgoa, nameof(tgoa.activateEventRegister));
                DumpByReflection(tgoa, nameof(tgoa.deactivateEventRegister));
                DumpByReflection(tgoa, nameof(tgoa.playerDataTest));
                DumpByReflection(tgoa, nameof(tgoa.questTests));
                DumpByReflection(tgoa, nameof(tgoa.equipTests));
                DumpByReflection(tgoa, nameof(tgoa.entryGateWhitelist));
                DumpByReflection(tgoa, nameof(tgoa.entryGateBlacklist));
                break;

            case Tk2dSpriteSetKeywordsConditional tk2dsskc:
                DumpByReflection(tk2dsskc, nameof(tk2dsskc.tests));
                break;
            
            case TubeTravelMapButton ttmb:
                DumpByReflection(ttmb, nameof(ttmb.playerDataBool));
                break;

            case TubeTravelMapPiece ttmp:
                DumpByReflection(ttmp, nameof(ttmp.pairedButton));
                break;

            // Non-components
            case AtmosCue atmosCue:
                DumpByReflection(atmosCue, nameof(atmosCue.alternatives));
                break;

            case AtmosCue.Alternative alt:
                DumpByReflection(alt, nameof(alt.Condition));
                break;

            case BasicQuestBase bqb:
                log.Add("Type", Dump(bqb.GetType().Name));

                switch (bqb)
                {
                    case FullQuestBase fqb:
                        DumpByReflection(fqb, nameof(fqb.name));
                        DumpByReflection(fqb, nameof(fqb.QuestType));
                        DumpByReflection(fqb, nameof(fqb.targets));
                        DumpByReflection(fqb, nameof(fqb.rewardItem));
                        DumpByReflection(fqb, nameof(fqb.getTargetCondition));
                        DumpByReflection(fqb, nameof(fqb.persistentBoolTests));
                        DumpByReflection(fqb, nameof(fqb.requiredCompleteQuests));
                        DumpByReflection(fqb, nameof(fqb.requiredUnlockedTools));
                        DumpByReflection(fqb, nameof(fqb.requiredCompleteTotalGroups));

                        switch (fqb)
                        {
                            case MainQuest mq:
                                DumpByReflection(mq, nameof(mq.subQuests));
                                DumpByReflection(mq, nameof(mq.altTargets));
                                break;
                            default:
                                break;
                        }
                        break;
                    case QuestRumour qr:
                        DumpByReflection(qr, nameof(qr.playerDataTest));
                        break;
                    case SubQuest sq:
                        DumpByReflection(sq, nameof(sq.linkedBool));
                        DumpByReflection(sq, nameof(sq.targetCounter));
                        DumpByReflection(sq, nameof(sq.seenBool));
                        DumpByReflection(sq, nameof(sq.nextSubQuest));
                        break;
                    default:
                        break;
                }
                break;
            
            case FullQuestBase.QuestTarget qt:
                DumpByReflection(qt, nameof(qt.AltTest));
                break;

            case MainQuest.AltQuestTarget mqaqt:
                DumpByReflection(mqaqt, nameof(mqaqt.Counter));
                DumpByReflection(mqaqt, nameof(mqaqt.AltTest));
                break;

            case EnemyJournalRecord ejr:
                DumpByReflection(ejr, nameof(ejr.name));
                break;

            // case EnviroRegion er:
            //     AddByReflection(er, nameof(er.environmentType));
            //     AddByReflection(er, nameof(er.priority));
            //     break;

            case HitSequence hs:
                DumpByReflection(hs, nameof(hs.requireHitWith));
                break;

            case MusicCue mc:
                DumpByReflection(mc, nameof(mc.alternatives));
                break;

            case MusicCue.Alternative mca:
                DumpByReflection(mca, nameof(mca.Condition));
                break;

            case PersistentBoolTest pbt:
                DumpByReflection(pbt, nameof(pbt.ID));
                DumpByReflection(pbt, nameof(pbt.SceneName));
                DumpByReflection(pbt, nameof(pbt.ExpectedValue));
                break;

            case PlayerDataBoolOperation pdbo:
                if (pdbo.variableName is "HasSeenGeo" or "HasSeenGeoMid" or "HasSeenGeoBig" or "HasSeenShellShards")
                {
                    return null;
                }
                DumpByReflection(pdbo, nameof(pdbo.operation));
                DumpByReflection(pdbo, nameof(pdbo.variableName));
                DumpByReflection(pdbo, nameof(pdbo.value));
                break;

            case PlayerDataBoolCollectable pdbc:
                DumpByReflection(pdbc, nameof(pdbc.boolName));
                break;

            case PlayerDataCollectable pdc:
                DumpByReflection(pdc, nameof(pdc.linkedPDBool));
                DumpByReflection(pdc, nameof(pdc.linkedPDInt));
                DumpByReflection(pdc, nameof(pdc.setPlayerDataBools));
                DumpByReflection(pdc, nameof(pdc.setPlayerDataInts));
                break;

            case PlayerDataIntOperation pdio:
                DumpByReflection(pdio, nameof(pdio.operation));
                DumpByReflection(pdio, nameof(pdio.variableName));
                DumpByReflection(pdio, nameof(pdio.number));
                break;

            case PlayerDataTest pdt:
                DumpByReflection(pdt, nameof(pdt.TestGroups));
                break;

            case PlayerDataTest.TestGroup tg:
                DumpByReflection(tg, nameof(tg.Tests));
                break;

            case PlayerDataTest.Test t:
                DumpByReflection(t, nameof(t.FieldName));
                switch (t.Type)
                {
                    case PlayerDataTest.TestType.Bool:
                        DumpByReflection(t, nameof(t.BoolValue));
                        break;
                    case PlayerDataTest.TestType.Enum:
                    case PlayerDataTest.TestType.Int:
                        DumpByReflection(t, nameof(t.NumType));
                        DumpByReflection(t, nameof(t.IntValue));
                        break;
                    case PlayerDataTest.TestType.Float:
                        DumpByReflection(t, nameof(t.NumType));
                        DumpByReflection(t, nameof(t.FloatValue));
                        break;
                    case PlayerDataTest.TestType.String:
                        DumpByReflection(t, nameof(t.StringType));
                        DumpByReflection(t, nameof(t.StringValue));
                        break;
                    default:
                        break;
                }
                break;

            case QuestCompleteTotalGroup qctg:
                DumpByReflection(qctg, nameof(qctg.quests));
                DumpByReflection(qctg, nameof(qctg.additionalTest));
                break;

            case QuestCompleteTotalGroup.CompleteQuest ct:
                DumpByReflection(ct, nameof(ct.Quest));
                break;

            case QuestTest questTest:
                DumpByReflection(questTest, nameof(questTest.Quest));
                if (questTest.CheckAvailable)
                {
                    DumpByReflection(questTest, nameof(questTest.IsAvailable));
                }
                if (questTest.CheckAccepted)
                {
                    DumpByReflection(questTest, nameof(questTest.IsAccepted));
                }
                if (questTest.CheckCompletedAmount)
                {
                    DumpByReflection(questTest, nameof(questTest.CompletedAmount));
                }
                if (questTest.CheckCompletable)
                {
                    DumpByReflection(questTest, nameof(questTest.IsCompletable));
                }
                if (questTest.CheckCompleted)
                {
                    DumpByReflection(questTest, nameof(questTest.IsCompleted));
                }
                if (questTest.CheckWasEverCompleted)
                {
                    DumpByReflection(questTest, nameof(questTest.WasEverCompleted));
                }
                break;

            case QuestType questType:
                DumpByReflection(questType, nameof(questType.removeQuestFromListOnComplete));
                break;

            case SavedItem savedItem:
                log.Add("Type", Dump(savedItem.GetType().Name));

                switch (savedItem)
                {
                    case CollectableItem ci:
                        DumpByReflection(ci, nameof(ci.name));
                        switch (ci)
                        {
                            case CollectableItemBasic cib:
                                DumpByReflection(cib, nameof(cib.uniqueCollectBool));
                                DumpByReflection(cib, nameof(cib.setExtraPlayerDataBools));
                                DumpByReflection(cib, nameof(cib.setExtraPlayerDataInts));
                                break;
                            case CollectableItemGrower cig:
                                DumpByReflection(cig, nameof(cig.growStatePdInt));
                                break;
                            case CollectableItemPlayerDataStack cipds:
                                DumpByReflection(cipds, nameof(cipds.stackItems));
                                break;
                            case CollectableItemStates cis:
                                log.Add(nameof(cis.states), Dump(cis.appends));
                                break;
                            // case DeliveryQuestItem dqi:
                            //     break;
                            default:
                                break;
                        }
                        break;
                    case MateriumItem mi:
                        DumpByReflection(mi, nameof(mi.name));
                        DumpByReflection(mi, nameof(mi.itemQuests));
                        DumpByReflection(mi, nameof(mi.playerDataCondition));
                        break;
                    // case QuestGroup qg:
                    //     DumpAllFields(qg);
                    //     break;
                    case QuestTargetCounter qtc:
                        switch (qtc)
                        {
                            case CollectableRelic cr:
                                DumpByReflection(cr, nameof(cr.eventConditionItem));
                                break;
                            case FakeCollectable fc:
                                DumpByReflection(fc, nameof(fc.setItemUpdated));
                                break;
                            case QuestTargetPlayerDataBools qtpdb:
                                DumpByReflection(qtpdb, nameof(qtpdb.pdBools));
                                DumpByReflection(qtpdb, nameof(qtpdb.orderListPd));
                                DumpByReflection(qtpdb, nameof(qtpdb.pdFieldTemplate));
                                // AddByReflection(qtpdb, nameof(qtpdb.linkedAchievementHalf));
                                // AddByReflection(qtpdb, nameof(qtpdb.linkedAchievementFull));
                                break;
                            case QuestTargetPlayerDataInt qtpdi:
                                DumpByReflection(qtpdi, nameof(qtpdi.playerDataInt));
                                break;
                            case ToolBase toolBase:
                                DumpByReflection(toolBase, nameof(toolBase.name));
                                switch (toolBase)
                                {
                                    case ToolCrest tc:
                                        break;
                                    case ToolItem ti:
                                        DumpByReflection(ti, nameof(ti.countKey));
                                        DumpByReflection(ti, nameof(ti.getReplaces));
                                        DumpByReflection(ti, nameof(ti.alternateUnlockedTest));
                                        switch (ti)
                                        {
                                            case ToolItemStatesLiquid tisl:
                                                DumpByReflection(tisl, nameof(tisl.infiniteRefillsBool));
                                                break;
                                            case ToolItemToggleState tits:
                                                DumpByReflection(tits, nameof(tits.statePdBool));
                                                break;
                                            default:
                                                break;
                                        }
                                        break;
                                    default:
                                        break;
                                }
                                break;
                            default:
                                break;
                        }
                        break;
                    default:
                        break;
                }
                break;

            case CollectableItemPlayerDataStack.StackItemInfo sii:
                DumpByReflection(sii, nameof(sii.PlayerDataBool));
                break;

            case CollectableItemStates.ItemState cisis:
                DumpByReflection(cisis, nameof(cisis.Test));
                break;

            case CollectableItemStates.AppendDesc cisad:
                DumpByReflection(cisad, nameof(cisad.Condition));
                break;

            case QuestTargetPlayerDataBools.BoolInfo qtpdbbi:
                DumpByReflection(qtpdbbi, nameof(qtpdbbi.BoolName));
                break;

            case ShopItem si:
                DumpByReflection(si, nameof(si.name));
                DumpByReflection(si, nameof(si.purchaseType));
                DumpByReflection(si, nameof(si.typeFlags));
                DumpByReflection(si, nameof(si.currencyType));
                DumpByReflection(si, nameof(si.costReference));
                DumpByReflection(si, nameof(si.cost));
                DumpByReflection(si, nameof(si.requiredItem));
                DumpByReflection(si, nameof(si.requiredItemAmount));
                DumpByReflection(si, nameof(si.requiredTools));
                DumpByReflection(si, nameof(si.upgradeFromItem));
                DumpByReflection(si, nameof(si.extraAppearConditions));
                DumpByReflection(si, nameof(si.questsAppearConditions));
                DumpByReflection(si, nameof(si.playerDataBoolName));
                DumpByReflection(si, nameof(si.savedItem));
                DumpByReflection(si, nameof(si.playerDataIntName));
                DumpByReflection(si, nameof(si.subItems));
                DumpByReflection(si, nameof(si.spawnOnPurchaseConditionals));
                DumpByReflection(si, nameof(si.setExtraPlayerDataBools));
                DumpByReflection(si, nameof(si.setExtraPlayerDataInts));
                break;

            case ShopItem.ConditionalSpawn sics:
                DumpByReflection(sics, nameof(sics.Condition));
                break;

            default:
                break;
        }

        return log.Entries.Any() ? log : null;

        void DumpAllFields(object target)
        {
            var type = target.GetType();
            foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (field.DeclaringType != type) continue; // skip base class fields

                var value = field.GetValue(target);
                log.Add(field.Name, Dump(value));
            }
        }

        void DumpByReflection(object target, string name)
        {
            if (target == null)
                return;

            var type = target.GetType();
            FieldInfo? field = null;
            PropertyInfo? prop = null;

            // Walk up the inheritance chain to find the field or property
            while (type != null)
            {
                field = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null)
                    break;

                prop = type.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (prop != null)
                    break;

                type = type.BaseType;
            }

            if (field != null)
            {
                object? value = null;
                try
                {
                    value = field.GetValue(target);
                }
                catch (Exception e)
                {
                    Silksong_GameObjectDumpPlugin.LogError($"Could not get field '{name}' on {target.GetType().Name}: {e}");
                }
                log.Add(name, Dump(value));
                return;
            }

            if (prop != null)
            {
                object? value = null;
                try
                {
                    value = prop.GetValue(target);
                }
                catch (Exception e)
                {
                    Silksong_GameObjectDumpPlugin.LogError($"Could not get property '{name}' on {target.GetType().Name}: {e}");
                }
                log.Add(name, Dump(value));
                return;
            }

            Silksong_GameObjectDumpPlugin.LogWarning($"No field or property named '{name}' found on {target.GetType().Name}");
        }

    }
}
