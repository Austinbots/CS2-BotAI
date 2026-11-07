namespace BotAI;

internal static class LinuxPatchDefinitions
{
    internal static IReadOnlyDictionary<string, (string signature, string patch, string expectedOriginal, int patchOffset)> All { get; } =
        new Dictionary<string, (string signature, string patch, string expectedOriginal, int patchOffset)>()
        {
        // Verified against libserver.so SHA256:
        // ac63a1513aac0d7716aa7920d5ee8728feb9953600aea79f3c2c92780ba8eae7.
        // Keep instruction/field context fixed; wildcard RIP-relative addresses and branch targets.
        // Historical keys are retained; comments describe the verified native behavior.

        // CCSBot::Reset: initialize schema-confirmed m_hasVisitedEnemySpawn (+0x5F5).
        ["HasVisitedEnemySpawn"] = (
            signature:        "48 C7 83 20 02 00 00 00 00 00 00 0F 2E A3 00 06 00 00 C6 83 F5 05 00 00 00 0F 8A ? ? ? ? 0F 85 ? ? ? ?",
            patch:            "C6 83 F5 05 00 00 01",
            expectedOriginal: "C6 83 F5 05 00 00 00",
            patchOffset:      18
        ),

        // NOP the BombState reset in CSGameState::Reset() (linux-specific bytes).
        ["GameState_Reset"] = (
            signature:        "0F 2E 43 18 C7 43 0C 00 00 00 00 0F 8A ? ? ? ? 0F 85 ? ? ? ? 48 8B 05 ? ? ? ? C6 43 6C 00 C7 43 68 FF FF FF FF",
            patch:            "0F 1F 80 00 00 00 00",
            expectedOriginal: "C7 43 0C 00 00 00 00",
            patchOffset:      4
        ),

        // IdleState::OnUpdate: skip the safe-time grenade/knife selection branch.
        ["Idle_IsSafeAlwaysFalse"] = (
            signature:        "E8 ? ? ? ? 84 C0 0F 85 ? ? ? ? 4C 8D B3 F8 50 00 00 4C 89 F7 E8 ? ? ? ? 84 C0 0F 84 ? ? ? ? 80 BB 88 52 00 00 00",
            patch:            "90 90 90 90 90 90",
            expectedOriginal: "0F 85 ? ? ? ?",
            patchOffset:      7
        ),

        // EscapeFromBombState::OnEnter tail-call to EquipKnife() -> ret.
        ["EscapeFromBomb_OnEnter_NoEquipKnife"] = (
            signature:        "C6 83 4C 4F 00 00 00 48 89 DF C6 83 74 4F 00 00 00 48 8B 5D F8 C9 E9 ? ? ? ?",
            patch:            "C3 90 90 90 90",
            expectedOriginal: "E9 ? ? ? ?",
            patchOffset:      22
        ),

        // EscapeFromBombState::OnUpdate call to EquipKnife() -> NOP.
        ["EscapeFromBomb_OnUpdate_NoEquipKnife"] = (
            signature:        "48 85 C0 0F 84 ? ? ? ? 48 89 DF 49 89 C4 E8 ? ? ? ? 31 F6 48 89 DF E8 ? ? ? ?",
            patch:            "90 90 90 90 90",
            expectedOriginal: "E8 ? ? ? ?",
            patchOffset:      15
        ),

        // EscapeFromFlamesState::OnEnter call to EquipKnife() -> NOP.
        ["EscapeFromFlames_OnEnter_NoEquipKnife"] = (
            signature:        "C6 83 4C 4F 00 00 00 48 89 DF C6 83 74 4F 00 00 00 E8 ? ? ? ? F3 0F 10 1D ? ? ? ? 49 C7 44 24 20 00 00 00 00 41 0F 2E 5C 24 14",
            patch:            "90 90 90 90 90",
            expectedOriginal: "E8 ? ? ? ?",
            patchOffset:      17
        ),

        ["PlantBombLookAtPriorityLow"] = (
            signature:        "48 8D 55 C8 4C 89 E7 45 31 C9 F3 0F 10 40 08 45 31 C0 B9 02 00 00 00 48 89 5D C8 F3 0F 10 0D ? ? ? ? 48 8D 35 ? ? ? ? F3 0F 11 45 D0",
            patch:            "B9 00 00 00 00",
            expectedOriginal: "B9 02 00 00 00",
            patchOffset:      18
        ),

        ["DefuseBombLookAtPriorityLow"] = (
            signature:        "4C 89 E2 45 31 C9 45 31 C0 F3 0F 10 05 ? ? ? ? B9 02 00 00 00 48 89 DF 48 8D 35 ? ? ? ? E8 ? ? ? ?",
            patch:            "B9 00 00 00 00",
            expectedOriginal: "B9 02 00 00 00",
            patchOffset:      17
        ),

        // MoveToState::OnUpdate - DefuseBomb IsVisible gate removal.
        ["DefuseBomb_SkipIsVisibleCheck"] = (
            signature:        "0F 2F C8 0F 86 ? ? ? ? 31 C9 31 D2 4C 89 E6 48 89 DF E8 ? ? ? ? 84 C0 0F 84 ? ? ? ? 48 83 C4 68 48 89 DF",
            patch:            "90 90 90 90 90 90",
            expectedOriginal: "0F 84 ? ? ? ?",
            patchOffset:      26
        ),

        // CBtActionAttack: bypass the next-shot timestamp gate (+0xAC), matching Windows.
        ["AttackState_SkipFireRateCheck"] = (
            signature:        "0F 2F 8B AC 00 00 00 0F 82 ? ? ? ? 48 89 DF E8 ? ? ? ? 84 C0 0F 84 ? ? ? ? 48 8B 7B 18",
            patch:            "90 90 90 90 90 90",
            expectedOriginal: "0F 82 ? ? ? ?",
            patchOffset:      7
        ),

        // CBtActionAttack: clear the release-between-shots flag (r12b),
        // the counterpart of Windows' bpl flag. Preserve the trigger output write.
        ["SprayAllDistances_ForceHoldTrigger"] = (
            signature:        "F3 0F 10 83 A0 00 00 00 66 0F EF C9 0F 2F C1 76 0C 49 8B 45 00 0F 2F 40 30 41 0F 97 C4 48 8B 83 98 00 00 00",
            patch:            "45 31 E4 90",
            expectedOriginal: "41 0F 97 C4",
            patchOffset:      25
        ),

        // AttackState::OnUpdate: bypass the target visibility gate before firing.
        ["AttackState_SkipSteadyFireShortcut"] = (
            signature:        "BA 01 00 00 00 48 89 DF 48 89 C6 E8 ? ? ? ? 84 C0 0F 84 ? ? ? ? 48 89 DF E8 ? ? ? ?",
            patch:            "90 90 90 90 90 90",
            expectedOriginal: "0F 84 ? ? ? ?",
            patchOffset:      18
        ),

        // AttackState::OnUpdate: bypass the nearby-enemy-gunfire gate.
        ["AttackState_SkipZoomFireShortcut"] = (
            signature:        "F3 0F 10 05 ? ? ? ? 48 89 DF E8 ? ? ? ? 84 C0 0F 84 ? ? ? ? 83 BB C8 05 00 00 14",
            patch:            "90 90 90 90 90 90",
            expectedOriginal: "0F 84 ? ? ? ?",
            patchOffset:      18
        ),

        // FireWeaponAtEnemy: ignore projected sniper spread (range * inaccuracy).
        // Keep the preceding weapon/target validity checks and fire command.
        ["AttackState_SkipSniperSpreadCheck"] = (
            signature:        "F3 0F 10 05 ? ? ? ? 66 41 0F 6E EE 0F 2F E8 0F 87 ? ? ? ? 48 89 DF E8 ? ? ? ? 48 89 DF F3 0F 10 40 08",
            patch:            "90 90 90 90 90 90",
            expectedOriginal: "0F 87 ? ? ? ?",
            patchOffset:      16
        ),

        // AttackState::OnEnter: always take the high-skill dodge chance path.
        ["AttackState_DodgeChance100_Always"] = (
            signature:        "48 89 DF F3 0F 11 85 48 FE FF FF E8 ? ? ? ? 84 C0 0F 84 ? ? ? ? 44 8B 2D ? ? ? ? 45 89 EE",
            patch:            "90 90 90 90 90 90",
            expectedOriginal: "0F 84 ? ? ? ?",
            patchOffset:      18
        ),

        // AttackState::OnUpdate: ignore CanSeeSniper as a reason to retreat.
        // Keep the independent pinned-down and outnumbered conditions.
        ["AttackState_RetreatOnSniper_Disable"] = (
            signature:        "80 BB 99 5C 00 00 00 0F 85 ? ? ? ? 48 89 DF E8 ? ? ? ? 84 C0",
            patch:            "90 90 90 90 90 90",
            expectedOriginal: "0F 85 ? ? ? ?",
            patchOffset:      7
        ),

        // CBtActionAttack: bypass the trace-result gate before firing, matching
        // Windows' historical CanStrafe key; this is not a retreat/look-at gate.
        ["AttackState_CanStrafe_jne"] = (
            signature:        "0F 2F 8B AC 00 00 00 0F 82 ? ? ? ? 48 89 DF E8 ? ? ? ? 84 C0 0F 84 ? ? ? ? 48 8B 7B 18",
            patch:            "90 90 90 90 90 90",
            expectedOriginal: "0F 84 ? ? ? ?",
            patchOffset:      23
        ),

        // Keep bot movement behavior when seeing enemies.
        ["AllSkill_KeepMoving_WhenSeeSniper"] = (
            signature:        "0F 2F 05 ? ? ? ? 76 0D 80 BB C4 05 00 00 00 0F 85 ? ? ? ? 0F B6 05 ? ? ? ? 84 C0 0F 84 ? ? ? ?",
            patch:            "90 90",
            expectedOriginal: "76 0D",
            patchOffset:      7
        ),

        // AttackState::OnEnter: force the reload-dodge chance flag true.
        ["AttackState_DodgeDuringReload"] = (
            signature:        "E8 ? ? ? ? 48 8B 43 08 0F 28 C8 66 41 0F 6E C5 F3 0F 59 40 08 0F 2F C8 41 0F 97 44 24 44 48 81 C4 A8 01 00 00",
            patch:            "41 C6 44 24 44 01",
            expectedOriginal: "41 0F 97 44 24 44",
            patchOffset:      25
        ),

        // AttackState::OnEnter: force the crouch-dodge chance flag true.
        ["SniperCrouchDodge_jb"] = (
            signature:        "66 41 0F 6E FE F3 0F 10 0D ? ? ? ? 0F 2F F8 66 0F EF C0 41 0F 93 44 24 42 E8 ? ? ? ? 48 8B 43 08 0F 28 C8",
            patch:            "41 C6 44 24 42 01",
            expectedOriginal: "41 0F 93 44 24 42",
            patchOffset:      20
        ),

        // AttackState::OnEnter: don't require the current weapon to be a sniper for dodge A.
        ["SniperDodge_SkipIsSniper_DodgeA"] = (
            signature:        "48 89 DF E8 ? ? ? ? 84 C0 0F 84 ? ? ? ? 44 8B 35 ? ? ? ? F3 0F 10 0D ? ? ? ? 66 0F EF C0 E8 ? ? ? ? 66 41 0F 6E D6 0F 2F D0 77 ?",
            patch:            "90 90 90 90 90 90",
            expectedOriginal: "0F 84 ? ? ? ?",
            patchOffset:      10
        ),

        // AttackState::Dodge: RandomInt(0, 3) -> RandomInt(0, 2), excluding JUMP.
        // This leaves navigation jumps and other jump callers unchanged.
        ["LowSKill_JumpChance0"] = (
            signature:        "F3 41 0F 5C 86 00 06 00 00 0F 2F C5 0F 83 ? ? ? ? BE 03 00 00 00 31 FF E8 ? ? ? ? E9 ? ? ? ?",
            patch:            "BE 02 00 00 00",
            expectedOriginal: "BE 03 00 00 00",
            patchOffset:      18
        ),

        // Historical Vision_* keys actually control AttackState::Dodge on Windows.
        // Use the same native behavior here, without UpdateLookAround code caves.
        // Enter dodge selection regardless of m_isEnemySniperVisible; this also
        // bypasses the distance and IsEnemyLookingAtMe tests below.
        ["Vision_AlwaysWatchApproachPoints"] = (
            signature:        "41 80 BE 99 5C 00 00 00 4C 8D 6D A8 0F 85 ? ? ? ? 0F 2F 05 ? ? ? ? 0F 86 ? ? ? ? 48 C7 43 08 00 00 00 00",
            patch:            "E9 00 00 00 00 90",
            expectedOriginal: "0F 85 ? ? ? ?",
            patchOffset:      12
        ),

        // Dodge direction selection: bypass skill > 0.5, not a vision check.
        ["Vision_ApproachBody_SkipSkillCheck"] = (
            signature:        "49 8B 46 08 F3 0F 10 48 0C 0F 2F 0D ? ? ? ? 0F B6 43 43 0F 86 ? ? ? ? 41 80 BE 99 5C 00 00 00 74 27",
            patch:            "90 90 90 90 90 90",
            expectedOriginal: "0F 86 ? ? ? ?",
            patchOffset:      20
        ),

        // Dodge direction selection: bypass m_isEnemySniperVisible,
        // not a hiding-spot check. Preserve the following first-dodge flag.
        ["Vision_ApproachBody_SkipHidingSpotCheck"] = (
            signature:        "41 80 BE 99 5C 00 00 00 74 27 84 C0 0F 85 ? ? ? ? 83 7B 08 01 B8 02 00 00 00",
            patch:            "90 90",
            expectedOriginal: "74 27",
            patchOffset:      8
        ),

        // Dodge: bypass the 2000-unit enemy-distance limit, not speed.
        ["Vision_SkipIsMovingGate"] = (
            signature:        "41 80 BE 99 5C 00 00 00 4C 8D 6D A8 0F 85 ? ? ? ? 0F 2F 05 ? ? ? ? 0F 86 ? ? ? ? 48 C7 43 08 00 00 00 00",
            patch:            "E9 00 00 00 00 90",
            expectedOriginal: "0F 86 ? ? ? ?",
            patchOffset:      25
        ),

        // Dodge: do not clear the action when the inlined IsEnemyLookingAtMe
        // dot-product test fails. Keep the timer and movement feasibility checks.
        ["Vision_AlwaysEnterApproachBody"] = (
            signature:        "F3 0F 59 4D C4 F3 0F 58 C1 66 0F EF C9 0F 2F C8 0F 86 ? ? ? ? 66 0F 1F 44 00 00 4C 8D 25 ? ? ? ?",
            patch:            "90 90 90 90 90 90",
            expectedOriginal: "0F 86 ? ? ? ?",
            patchOffset:      16
        ),

        // IsNoticable(player, visibleParts), called after IsVisible succeeds:
        // always notice a visible enemy. Do not return true from IsVisible itself.
        ["IsNoticable_AlwaysTrue"] = (
            signature:        "55 48 89 E5 41 56 41 89 D6 41 55 41 54 49 89 FC 48 89 F7 53 48 89 F3 48 83 EC 20 E8 ? ? ? ? 48 85 C0 74 2D",
            patch:            "B0 01 C3 90",
            expectedOriginal: "55 48 89 E5",
            patchOffset:      0
        ),

        // CCSBot::Upkeep: remove the additive yaw/pitch drift, matching Windows.
        // Replace only the BotCOS(33 * curtime) / BotSIN(13 * curtime) calls
        // with 0.0f; preserve the shared trig helpers and all other aiming logic.
        ["Upkeep_BotCOS_ZeroDrift"] = (
            signature:        "F3 0F 10 05 ? ? ? ? 49 8B 45 00 F3 0F 59 40 30 E8 ? ? ? ? 49 8B 45 00 F3 0F 59 45 ? F3 0F 58 83 64 59 00 00 F3 0F 11 83 64 59 00 00",
            patch:            "0F 57 C0 90 90",
            expectedOriginal: "E8 ? ? ? ?",
            patchOffset:      17
        ),

        ["Upkeep_BotSIN_ZeroDrift"] = (
            signature:        "F3 0F 10 05 ? ? ? ? F3 0F 59 40 30 E8 ? ? ? ? F3 0F 59 45 ? F3 0F 58 83 5C 59 00 00 F3 0F 11 83 5C 59 00 00",
            patch:            "0F 57 C0 90 90",
            expectedOriginal: "E8 ? ? ? ?",
            patchOffset:      13
        ),

        // AttendCurrentNoise: bypass the noise-priority gate, not disposition.
        ["InvestigateNoise_SkipSelfDefenseCheck"] = (
            signature:        "83 BB F0 52 00 00 02 0F 84 ? ? ? ? F3 0F 10 83 D8 52 00 00 31 C0 66 0F EF DB",
            patch:            "90 90 90 90 90 90",
            expectedOriginal: "0F 84 ? ? ? ?",
            patchOffset:      7
        ),

        // CCSBot::OnAudibleEvent: accept sounds regardless of distance.
        ["OnAudibleEvent_GlobalHearRange"] = (
            signature:        "F3 0F 51 D2 0F 2F F2 0F 86 ? ? ? ? 4C 89 EF F3 0F 11 8D 28 FF FF FF F3 0F 11 95 2C FF FF FF",
            patch:            "90 90 90 90 90 90",
            expectedOriginal: "0F 86 ? ? ? ?",
            patchOffset:      7
        ),

        // Idle fallback: GetNextBombsiteToSearch() -> GetPlantedBombsite().
        // Build-specific rel32: keep expectedOriginal exact and recheck both callees.
        ["TBot_BombsiteSearch_UseKnownPlantedSite"] = (
            signature:        "48 8B BB 00 5E 00 00 E8 ? ? ? ? 4C 89 F7 E8 ? ? ? ?",
            patch:            "E8 FC B1 F6 FF",   // call GetPlantedBombsite
            expectedOriginal: "E8 EC B4 F6 FF",   // call GetNextBombsiteToSearch
            patchOffset:      15
        ),

        // OnBombPickedUp: remove only the CT pickup hearing-distance gate.
        ["BombPickup_CT_GlobalHearRange"] = (
            signature:        "F3 0F 58 C2 F3 0F 58 C1 F3 0F 10 0D ? ? ? ? 0F 2F C8 0F 86 ? ? ? ? 48 8B 05 ? ? ? ?",
            patch:            "90 90 90 90 90 90",
            expectedOriginal: "0F 86 ? ? ? ?",
            patchOffset:      19
        ),

        // OnBombBeep: ignore the 1500-unit hear range and update the bombsite from any distance.
        ["BombBeep_CT_GlobalHearRange"] = (
            signature:        "F3 0F 58 C2 F3 0F 58 C1 F3 0F 10 0D ? ? ? ? 0F 2F C8 0F 86 ? ? ? ? 48 8B 43 18",
            patch:            "90 90 90 90 90 90",
            expectedOriginal: "0F 86 ? ? ? ?",
            patchOffset:      19
        ),

        // CSGameState::OnBombPlanted: all bot-owned game states learn the planted site.
        // 1.8.9: rel32 computed at load as origRel32 + 1 (jz rel32 is 6 bytes, jmp rel32
        // is 5 — same target, instruction shrinks by one). expectedOriginal wildcards the
        // displacement so validation survives game updates.
        ["OnBombPlanted_AllBotsLearnSite"] = (
            signature:        "48 8B 83 F8 50 00 00 48 8B 40 18 80 B8 24 06 00 00 02 0F 84 ? ? ? ? 48 8B 7B 18 48 8D 15 ? ? ? ? 48 8B 07 48 8B 80 48 05 00 00 48 39 D0",
            patch:            "E9 00 00 00 00 90",
            expectedOriginal: "0F 84 ? ? ? ?",
            patchOffset:      18
        ),

        // CT defuse task path: SetDisposition(SELF_DEFENSE) -> ENGAGE_AND_INVESTIGATE.
        ["CT_Defuse_EngageAndInvestigate"] = (
            signature:        "48 8B 05 ? ? ? ? BE 02 00 00 00 48 89 DF 48 89 83 C8 05 00 00 E8 ? ? ? ? BA 02 00 00 00 4C 89 EE E9 ? ? ? ?",
            patch:            "BE 00 00 00 00",
            expectedOriginal: "BE 02 00 00 00",
            patchOffset:      7
        ),

        // DefuseBombState::OnUpdate: SetDisposition(SELF_DEFENSE) -> ENGAGE_AND_INVESTIGATE.
        ["DefuseBombState_OnUpdate_EngageAndInvestigate"] = (
            signature:        "55 48 8D BE ? ? 00 00 48 89 E5 41 54 53 48 89 F3 E8 ? ? ? ? BE 02 00 00 00 48 89 DF 49 89 C4 E8 ? ? ? ?",
            patch:            "BE 00 00 00 00",
            expectedOriginal: "BE 02 00 00 00",
            patchOffset:      22
        ),

        // DefuseBombState::OnEnter: SetDisposition(SELF_DEFENSE) -> ENGAGE_AND_INVESTIGATE.
        ["DefuseBombState_OnEnter_EngageAndInvestigate"] = (
            signature:        "55 48 89 E5 41 54 53 48 89 F3 BE 02 00 00 00 48 89 DF E8 ? ? ? ? 4C 8B A3 ? ? 00 00",
            patch:            "BE 00 00 00 00",
            expectedOriginal: "BE 02 00 00 00",
            patchOffset:      10
        ),

        // Disable flashbang avoidance SetLookAt/StopAiming block.
        ["FlashbangAvoidance_Disable"] = (
            signature:        "0F 5C C2 48 8D 35 ? ? ? ? F3 0F 11 4D ? F3 0F 10 0D ? ? ? ? 0F 13 45 ? F3 0F 10 05 ? ? ? ? E8 ? ? ? ? C6 83 ? ? 00 00 00",
            patch:            "90 90 90 90 90 90 90 90 90 90 90 90",
            expectedOriginal: "E8 ? ? ? ? C6 83 ? ? 00 00 00",
            patchOffset:      35
        )
    };
}
