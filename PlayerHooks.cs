using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using UnityEngine;
using MonoMod.RuntimeDetour;
using MoreSlugcats;

namespace RainMeadowPupifier;

public partial class RainMeadowPupifier
{
    private void PlayerHooks()
    {
        On.Player.Update += Player_Update;
    }

    private void PlayerOnEnabledHooks()
    {
        On.Player.setPupStatus += Player_SetPupStatus;
        On.PlayerGraphics.DrawSprites += PlayerGraphics_DrawSprites;
        On.SlugcatHand.Update += Player_SlugcatHandUpdate;

        // TODO: Make an onhook instead
        //IL.SlugcatStats.SlugcatFoodMeter += Player_AppendPupCheck;

        // we set isSlugpup, RenderAsPup to playerstate
        new Hook(typeof(Player).GetProperty("isSlugpup").GetGetMethod(), (Func<Player, bool> orig, Player self) => orig(self) || (!self.isNPC && self.playerState.isPup));

        // patch because it checks isSlugpup and tries getting npcStats
        new Hook(typeof(Player).GetProperty("slugcatStats").GetGetMethod(), (Func<Player, SlugcatStats> orig, Player self) => (self.isSlugpup && !self.isNPC) ? self.abstractCreature.world.game.session.characterStats : orig(self));

        // Change isSlugpup in specific methods, because changing them in jump and movement will break movement
        IL.Player.Jump += Player_FixIsSlugpupOnJump;
        IL.Player.MovementUpdate += Player_AppendToIsSlugpupCheck;

        // Add this so we get correct hand positions
        IL.SlugcatHand.Update += Player_AppendPupCheck;
    }

    private void Player_FixIsSlugpupOnJump(ILContext il)
    {
        // 136	017F	ldarg.0
        // 137	0180	call	instance bool Player::get_isSlugpup()
        // 138	0185	brfalse.s	164 (01BF) ldc.i4.1 
        try
        {
            var c = new ILCursor(il);

            // Match the IL sequence for `call instance bool Player::get_isSlugpup()`
            int matchCount = 0;
            while (c.TryGotoNext(MoveType.AfterLabel,
                i => i.MatchLdarg(0), // Match ldarg.0 instruction
                i => i.MatchCall(typeof(Player).GetMethod("get_isSlugpup")) // Match call to get_isSlugpup()
            )) // Match the branch instruction after get_isSlugpup
            {
                matchCount++;
                if (
                    matchCount == 1 ||
                    matchCount == 2 ||
                    matchCount == 3 ||
                    matchCount == 4 ||
                    matchCount == 5
                    )
                {
                    Log($"On.Player.Jump isSlugpup {matchCount} is going to be skipped for players");
                    c.Index += 2;
                    // Insert the condition directly after get_isSlugpup
                    c.Emit(OpCodes.Ldarg_0);
                    c.EmitDelegate((Player player) => player.isNPC);
                    c.Emit(OpCodes.And);
                }

            }
        }
        catch (Exception ex)
        {
            LogError(ex, "Error in Player_AppendToIsSlugpupCheck");
        }
    }

    private void Player_AppendToIsSlugpupCheck(ILContext il)
    {
        // 136	017F	ldarg.0
        // 137	0180	call	instance bool Player::get_isSlugpup()
        // 138	0185	brfalse.s	164 (01BF) ldc.i4.1 
        try
        {
            var c = new ILCursor(il);

            // Match the IL sequence for `call instance bool Player::get_isSlugpup()`
            while (c.TryGotoNext(MoveType.AfterLabel,
                i => i.MatchLdarg(0), // Match ldarg.0 instruction
                i => i.MatchCall(typeof(Player).GetMethod("get_isSlugpup")) // Match call to get_isSlugpup()
            )) // Match the branch instruction after get_isSlugpup
            {
                c.Index += 2;
                // Insert the condition directly after get_isSlugpup
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate((Player player) => player.isNPC);
                c.Emit(OpCodes.And);
            }
        }
        catch (Exception ex)
        {
            LogError(ex, "Error in Player_AppendToIsSlugpupCheck");
        }
    }

    private void Player_AppendPupCheck(ILContext il)
    {
        //351	0328	isinst	Player
        //352	032D	ldfld	class SlugcatStats/Name Player::SlugCatClass
        //353	0332	ldsfld	class SlugcatStats/Name MoreSlugcats.MoreSlugcatsEnums/SlugcatStatsName::Slugpup
        //354	0337	call	bool class ExtEnum`1<class SlugcatStats/Name>::op_Equality(class ExtEnum`1<!0>, class ExtEnum`1<!0>)
        //355	033C	brfalse.s	363 (0352) ldsfld bool ModManager::CoopAvailable
        try
        {
            var c = new ILCursor(il);
            // Match the IL sequence for `SlugCatClass == Slugpup`
            while (c.TryGotoNext(MoveType.AfterLabel,
                i => i.MatchLdfld(typeof(Player).GetField("SlugCatClass")), // Load SlugCatClass
                i => i.MatchLdsfld(typeof(MoreSlugcatsEnums.SlugcatStatsName).GetField("Slugpup")), // Load Slugpup
                i => i.MatchCall(typeof(ExtEnum<SlugcatStats.Name>).GetMethod("op_Equality")) // Call ExtEnum op_Equality
            ))
            {
                c.Emit(OpCodes.Dup); // Duplicate Player
                c.Index += 3; // Move to delegate
                c.EmitDelegate((Player player, bool isSlugpup) => isSlugpup || (!player.isNPC && player.playerState.isPup));
            }
        }
        catch (Exception ex)
        {
            LogError(ex, "Error in Player_AppendPupCheck");
        }
    }

    private void Player_SetPupStatus(On.Player.orig_setPupStatus orig, Player self, bool set)
    {
        orig(self, set);

        if (self.graphicsModule is PlayerGraphics playerGraphics)
        {
            float tail = 0.85f + 0.3f * Mathf.Lerp(1, 0.5f, self.playerState.isPup ? 0.5f : 0f);
            float tailConnection = (0.75f + 0.5f * 1) * (self.playerState.isPup ? 0.5f : 1f);
            playerGraphics.tail[0].rad = 6f * tail;
            playerGraphics.tail[0].connectionRad = 4f * tailConnection;
            playerGraphics.tail[1].rad = 4f * tail;
            playerGraphics.tail[1].connectionRad = 7f * tailConnection;
            playerGraphics.tail[2].rad = 2.5f * tail;
            playerGraphics.tail[2].connectionRad = 7f * tailConnection;
            playerGraphics.tail[3].rad = 1f * tail;
            playerGraphics.tail[3].connectionRad = 7f * tailConnection;
        }
    }

    private void PlayerGraphics_DrawSprites(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        // Call the original DrawSprites method to handle all default rendering
        orig(self, sLeaser, rCam, timeStacker, camPos);

        if (!self.player.isNPC && self.player.playerState.isPup)
        {
            if (self.player.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Saint)
            {
                sLeaser.sprites[3].element = Futile.atlasManager.GetElementWithName($"HeadB0");
            }
        }
    }

    private void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
    {
        try
        {
            if (!self.isNPC && Options.SlugpupEnabled != self.playerState.isPup)
            {
                Log("Getting if the player is local.");
                bool IsLocal = true;
                if (RainMeadowEnabled)
                {
                    IsLocal = PlayerIsLocal(self);
                    Log(IsLocal ? "Player is local, applying." : "Player is not local, not applying.");
                }
                else
                {
                    Log("Applying to all players since Rain Meadow isn't enabled.");
                }
                if (!Options.ModAutoDisabled && !Options.ModChecked && IsLocal)
                {
                    if (!Options.SlugpupKeyPressed && self.playerState.isPup && !Options.ModAutoDisabledToggle.Value)
                    {
                        Log("We detected that you have another mod that is conflicting with Rain Meadow Pupifier. Rain Meadow Pupifier has not changed your slugcat statistics and is effectively disabled.");
                        Options.ModAutoDisabled = true;
                        Options.ModChecked = true;
                    }
                    else
                    {
                        PlayerOnEnabledHooks();
                        Options.ModChecked = true;
                    }
                }

                if (!Options.ModAutoDisabled && IsLocal)
                {
                    // setPupStatus sets isPup and also updates body proportions
                    // we multiply by survivor -> slugpup values (aka difference between survivor and slugpup)
                    // Change body size using setPupStatus
                    SlugcatStats newStats = new(self.SlugCatClass, self.Malnourished);
                    if (self.playerState.isPup = Options.SlugpupEnabled)
                    {
                        // Change body size using setPupStatus
                        self.setPupStatus(Options.SlugpupEnabled);
                        // Set relative stats based on status
                        if (!Options.UseSlugpupStatsToggle.Value) return;
                        self.slugcatStats.bodyWeightFac = newStats.bodyWeightFac * Options.BodyWeightFac.Value * Options.GlobalModifier.Value;
                        self.slugcatStats.generalVisibilityBonus = newStats.generalVisibilityBonus * Options.VisibilityBonus.Value * Options.GlobalModifier.Value;
                        self.slugcatStats.visualStealthInSneakMode = newStats.visualStealthInSneakMode * Options.VisualStealthInSneakMode.Value * Options.GlobalModifier.Value;
                        self.slugcatStats.loudnessFac = newStats.loudnessFac * Options.LoudnessFac.Value * Options.GlobalModifier.Value;
                        self.slugcatStats.lungsFac = newStats.lungsFac * Options.LungsFac.Value * Options.GlobalModifier.Value;
                        self.slugcatStats.poleClimbSpeedFac = newStats.poleClimbSpeedFac * Options.PoleClimbSpeedFac.Value * Options.GlobalModifier.Value;
                        self.slugcatStats.corridorClimbSpeedFac = newStats.corridorClimbSpeedFac * Options.CorridorClimbSpeedFac.Value * Options.GlobalModifier.Value;
                        self.slugcatStats.runspeedFac = newStats.runspeedFac * Options.RunSpeedFac.Value * Options.GlobalModifier.Value;
                    }
                    else
                    {
                        // Change body size using setPupStatus
                        self.setPupStatus(Options.SlugpupEnabled);
                        // Set relative stats based on status
                        if (!Options.UseSlugpupStatsToggle.Value) return;
                        self.slugcatStats.bodyWeightFac = newStats.bodyWeightFac;
                        self.slugcatStats.generalVisibilityBonus = newStats.generalVisibilityBonus;
                        self.slugcatStats.visualStealthInSneakMode = newStats.visualStealthInSneakMode;
                        self.slugcatStats.loudnessFac = newStats.loudnessFac;
                        self.slugcatStats.lungsFac = newStats.lungsFac;
                        self.slugcatStats.poleClimbSpeedFac = newStats.poleClimbSpeedFac;
                        self.slugcatStats.corridorClimbSpeedFac = newStats.corridorClimbSpeedFac;
                        self.slugcatStats.runspeedFac = newStats.runspeedFac;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            LogError(ex, "Failed to update player");
        }
        orig(self, eu);
    }

    private void Player_SlugcatHandUpdate(On.SlugcatHand.orig_Update orig, SlugcatHand self)
    {
        // Call the original method to keep the base behavior
        orig(self);

        // Scale the hands (arms) position relative to its connection
        // In base game pups have long arms, which looks goofy
        // (extensively tested, 3 different setups)
        if (self.owner.owner is not Player player || (!player.isNPC && !player.playerState.isPup)) return;

        // I don't know how to fix arms when crawling, it's not even noticable so I'm just not gonna fix it
        if (player.bodyMode == Player.BodyModeIndex.Crawl) return;

        if (player.animation == Player.AnimationIndex.HangUnderVerticalBeam)
        {
            // this fixes arms when hanging from vertical pipes
            Vector2 offset = self.absoluteHuntPos - self.owner.owner.bodyChunks[0].pos;
            self.absoluteHuntPos = self.owner.owner.bodyChunks[0].pos + offset * 0.5f;
        }

        if (player.animation == Player.AnimationIndex.HangUnderVerticalBeam ||
            player.animation == Player.AnimationIndex.StandOnBeam ||
            player.animation == Player.AnimationIndex.BeamTip
        )
        {
            // this works for standing on pipes (balancing) (that includes: standing on horizontal pipes, beam tips)
            // also required for the above fix (hanging)
            // probably doesn't fix crawling arms being too long, but im gonna keep it since it doesnt break anything
            self.relativeHuntPos *= 0.5f;
        }
    }
}