using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using UnityEngine;
using MonoMod.RuntimeDetour;
using MoreSlugcats;
using System.Collections.Generic;
using RWCustom;

namespace Pupifier;

public partial class Pupifier
{

    private void PlayerHooks()
    {
        On.Player.Update += Player_Update;

        On.Player.setPupStatus += Player_SetPupStatus;

        // Fix saint head
        On.PlayerGraphics.DrawSprites += PlayerGraphics_DrawSprites;

        // TODO: Make an onhook instead
        //IL.SlugcatStats.SlugcatFoodMeter += Player_AppendPupCheck;

        // we set isSlugpup, RenderAsPup to playerstate
        new Hook(typeof(Player).GetProperty("isSlugpup").GetGetMethod(), (Func<Player, bool> orig, Player self) => orig(self) || (!self.isNPC && self.playerState.isPup));

        // patch because it checks isSlugpup and tries getting npcStats
        new Hook(typeof(Player).GetProperty("slugcatStats").GetGetMethod(), (Func<Player, SlugcatStats> orig, Player self) => (self.isSlugpup && !self.isNPC) ? self.abstractCreature.world.game.session.characterStats : orig(self));

        // Change isSlugpup in specific methods
        // In jump if isSlugpup is true, it breaks jumping off pipes, disables for players by adding isNPC
        IL.Player.Jump += Player_AppendToIsSlugpupCheck;

        // For assistance and stats
        On.Player.Jump += Player_Jump;
        On.Player.WallJump += Player_WallJump;

        // In movement if it's true we can keep walking into walls, which shouldn't happen
        IL.Player.MovementUpdate += Player_AppendToIsSlugpupCheck;
        
        // False in SlugcatGrab if we have using both arms enabled
        IL.Player.SlugcatGrab += Player_SlugcatGrabAppendToIsSlugpupCheck;

        // Add so we get correct hand positions
        IL.SlugcatHand.Update += Player_AppendPupCheck;
        // Fix original slugpup animations
        On.SlugcatHand.Update += Player_SlugcatHandUpdate;

        // Allows grabbing other players
        IL.Player.Grabability += Player_AppendPupCheckGrabability;
    }

    private void Player_WallJump(On.Player.orig_WallJump orig, Player self, int direction)
    {
        orig(self, direction);
        if (!self.playerState.isPup || self.isNPC) return;
        self.bodyChunks[0].vel.y *= Options.WallJumpPowerFac.Value * Options.GlobalModifier.Value;
        self.bodyChunks[1].vel.y *= Options.WallJumpPowerFac.Value * Options.GlobalModifier.Value;
        self.bodyChunks[0].vel.x *= Options.WallJumpPowerFac.Value * Options.GlobalModifier.Value;
        self.bodyChunks[1].vel.x *= Options.WallJumpPowerFac.Value * Options.GlobalModifier.Value;
    }

    private void Player_Jump(On.Player.orig_Jump orig, Player self)
    {
        float actionJumpMultiplier = Options.UseSlugpupStatsToggle.Value ? Options.ActionJumpPowerFac.Value : 1f;
        Player.AnimationIndex origAnimation = self.animation;
        BodyChunk origbodyChunks0 = self.bodyChunks[0];
        BodyChunk origbodyChunks1 = self.bodyChunks[1];
        float origrollCounter = self.rollCounter;
        float origrollDirection = self.rollDirection;
        float origaerobicLevel = self.aerobicLevel;
        bool origwhiplashJump = self.whiplashJump;
        bool origlongBellySlide = self.longBellySlide;
        float origslideCounter = self.slideCounter;
        float origsuperLaunchJump = self.superLaunchJump;
        Player.InputPackage originput0 = self.input[0];
        orig(self);
        if (!self.playerState.isPup || self.isNPC) return;

        if (origAnimation == Player.AnimationIndex.ClimbOnBeam)
        {
            self.bodyChunks[0].vel.y *= 0.875f * actionJumpMultiplier;
            self.bodyChunks[1].vel.y *= 0.8571f * actionJumpMultiplier;
            self.bodyChunks[0].vel.x *= 0.8333f * actionJumpMultiplier;
            self.bodyChunks[1].vel.x *= 0.9f * actionJumpMultiplier;
        }
        else if (origAnimation == Player.AnimationIndex.Roll)
        {
            float massMultiplier = GetPlayerMassMultiplier(self);
            float num3 = Mathf.InverseLerp(0f, 25f, origrollCounter);
            self.bodyChunks[0].vel = Custom.DegToVec(origrollDirection * Mathf.Lerp(60f, 35f, num3)) * Mathf.Lerp(9.5f, 13.1f, num3) * massMultiplier * 0.65f * actionJumpMultiplier;
            self.bodyChunks[1].vel = Custom.DegToVec(origrollDirection * Mathf.Lerp(60f, 35f, num3)) * Mathf.Lerp(9.5f, 13.1f, num3) * massMultiplier * 0.65f * actionJumpMultiplier;
        }
        else if (origAnimation == Player.AnimationIndex.BellySlide)
        {
            float massMultiplier = GetPlayerMassMultiplier(self);
            float num4 = 9f;
            if (self.isRivulet)
            {
                num4 = 18f;
                if (self.isGourmand && ModManager.Expedition && Custom.rainWorld.ExpeditionMode && Expedition.ExpeditionGame.activeUnlocks.Contains("unl-agility"))
                {
                    num4 = Mathf.Lerp(14f, 9f, origaerobicLevel);
                }
            }
            // confirms slugpups are works of the devil
            num4 = Mathf.Ceil(num4 * 0.666f);
            if (!origwhiplashJump && originput0.x != -origrollDirection)
            {
                float num5 = 8.5f;
                if (self.isRivulet)
                {
                    num5 = 10f;
                }
                //if (self.isSlugpup)
                //{
                //    num5 = 6f;
                //}
                num5 = Mathf.Ceil(num5 * 0.705f);
                self.bodyChunks[1].vel = new Vector2(origrollDirection * num4, num5) * massMultiplier * (origlongBellySlide ? 1.2f : 1f) * actionJumpMultiplier;
                self.bodyChunks[0].vel = new Vector2(origrollDirection * num4, num5) * massMultiplier * (origlongBellySlide ? 1.2f : 1f) * actionJumpMultiplier;
                return;
            }
        }
        if (self.bodyMode != Player.BodyModeIndex.CorridorClimb &&
            self.animation != Player.AnimationIndex.ClimbOnBeam &&
            self.animation != Player.AnimationIndex.BellySlide &&
            !(self.animation == Player.AnimationIndex.ZeroGSwim || self.animation == Player.AnimationIndex.ZeroGPoleGrab) &&
            !(self.animation == Player.AnimationIndex.DownOnFours &&
            self.bodyChunks[1].ContactPoint.y < 0 &&
            self.input[0].downDiagonal == self.flipDirection))
        {
            float additionalModifier;
            int num9 = self.input[0].x;
            if (self.standing)
            {
                if (origslideCounter > 0 && origslideCounter < 10)
                {
                    // self.jumpBoost = 5f;
                    // if (self.isRivulet)
                    // {
                    //     self.jumpBoost = 9f;
                    //     if (self.isGourmand && ModManager.Expedition && Custom.rainWorld.ExpeditionMode)
                    //     {
                    //         self.jumpBoost = Mathf.Lerp(8f, 2f, self.aerobicLevel);
                    //     }
                    // }

                    // originally 3
                    additionalModifier = 0.1f;
                }
                else
                {
                    // originally 7
                    additionalModifier = 0.375f;
                }
            }
            else
            {
                // superjump
                //float num10 = 1.5f;
                if (origsuperLaunchJump >= 20)
                {
                    /*
                    num10 = 9f;
                    if (self.PainJumps)
                    {
                        num10 = 2.5f;
                    }
                    else if (self.isRivulet)
                    {
                        num10 = 12f;
                        if (self.isGourmand && ModManager.Expedition && Custom.rainWorld.ExpeditionMode && Expedition.ExpeditionGame.activeUnlocks.Contains("unl-agility"))
                        {
                            num10 = Mathf.Lerp(8f, 3f, origaerobicLevel);
                        }
                    }
                    */
                    //else if (self.isSlugpup)
                    //{
                    //    num10 = 5.5f;
                    //}
                    num9 = (origbodyChunks0.pos.x > origbodyChunks1.pos.x) ? 1 : (-1);
                    if (num9 != 0 && origbodyChunks0.pos.x > origbodyChunks1.pos.x == num9 > 0)
                    {
                        // should modify only superjump/rocketjump
                        self.bodyChunks[0].vel.x *= 0.611f * actionJumpMultiplier;
                        self.bodyChunks[1].vel.x *= 0.611f * actionJumpMultiplier;
                    }
                }
                // originally 6
                additionalModifier = 0.25f;
            }
            // originally 4
            additionalModifier += Options.UseSlugpupStatsToggle.Value ? (0.5f * Options.JumpPowerFac.Value) : 0.5f;
            self.jumpBoost *= additionalModifier * Options.GlobalModifier.Value;
        }
    }

    private float GetPlayerMassMultiplier(Player player)
    {
        float massMultiplier = Mathf.Lerp(1f, 1.15f, player.Adrenaline);
        if (player.grasps[0] != null && player.HeavyCarry(player.grasps[0].grabbed) && !(player.grasps[0].grabbed is Cicada))
        {
            massMultiplier += Mathf.Min(Mathf.Max(0f, player.grasps[0].grabbed.TotalMass - 0.2f) * 1.5f, 1.3f);
        }
        return massMultiplier;
    }

    private void Player_AppendPupCheckGrabability(ILContext il)
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
                c.EmitDelegate((Player player, bool isSlugpup) => isSlugpup || (!player.isNPC && player.playerState.isPup && Player_CheckGrabability(player)));
            }
        }
        catch (Exception ex)
        {
            LogError(ex, "Error in Player_AppendPupCheckGrabability");
        }
    }

    public bool Player_CheckGrabability(Player player)
    {
        if (RainMeadowEnabled) return Player_CheckGrababilityMeadow(player);
        return !Options.DisableBeingGrabbed.Value;
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

    private void Player_SlugcatGrabAppendToIsSlugpupCheck(ILContext il)
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
                c.EmitDelegate((Player player) => !Options.UseBothHands.Value);
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

        try
        {
            if (self.graphicsModule is PlayerGraphics playerGraphics)
            {
                Log("Changed our tail.");
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
        catch (Exception ex)
        {
            LogError(ex, "Error in Player_SetPupStatus");
        }
    }

    private void PlayerGraphics_DrawSprites(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        // Call the original DrawSprites method to handle all default rendering
        orig(self, sLeaser, rCam, timeStacker, camPos);
        if (self.player.isNPC || !self.player.playerState.isPup) return;

        try
        {
            if (self.player.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Saint)
            {
                sLeaser.sprites[3].element = Futile.atlasManager.GetElementWithName($"HeadB0");
            }
        }
        catch (Exception ex)
        {
            LogError(ex, "Error in PlayerGraphics_DrawSprites");
        }
    }

    public bool SlugpupEnabled = false;
    bool LocalPlayer = false;
    bool RanOnce = false;
    private void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
    {
        if (RainMeadowEnabled && !RanOnce)
        {
            if (PlayerIsLocal(self) && GameIsMeadow())
            {
                ToggleGrabbable(self);
                RanOnce = true;
            };
        };
        Player_ChangeMode(self);
        orig(self, eu);
    }

    private void Player_ChangeMode(Player self)
    {
        if (self.isNPC || SlugpupEnabled == self.playerState.isPup || self == null) return;
        if (RainMeadowEnabled) LocalPlayer = PlayerIsLocal(self);
        else LocalPlayer = false;
        if (RainMeadowEnabled && !LocalPlayer) return;

        try
        {
            Player_SetMode(self);
        }
        catch (Exception ex)
        {
            LogError(ex, "Error in Player_Update");
        }
    }

    private static readonly Dictionary<SlugcatStats.Name, SlugcatStats> slugcatStatsDictionary = new() { };
    private void Player_SetMode(Player self)
    {
        // setPupStatus sets isPup and also updates body proportions
        // we multiply by survivor -> slugpup values (aka difference between survivor and slugpup)
        // Change body size using setPupStatus
        if (!slugcatStatsDictionary.TryGetValue(self.SlugCatClass, out SlugcatStats newStats))
        {
            newStats = new(self.SlugCatClass, self.Malnourished);
            slugcatStatsDictionary.Add(self.SlugCatClass, newStats);
        }
        Log($"Set pup status for {(LocalPlayer ? "local " : "non-local")} player to {SlugpupEnabled}, RainMeadow is {(RainMeadowEnabled ? "enabled" : "disabled")}");
        // Change body size using setPupStatus
        self.setPupStatus(SlugpupEnabled);
        if (RainMeadowEnabled)
        {
            if (GameIsMeadow()) ToggleGrabbable(self);
        };
        // Set relative stats on status
        if (!Options.UseSlugpupStatsToggle.Value) return;
        if (SlugpupEnabled)
        {
            self.slugcatStats.bodyWeightFac = newStats.bodyWeightFac * 0.65f * Options.BodyWeightFac.Value * Options.GlobalModifier.Value;
            self.slugcatStats.generalVisibilityBonus = newStats.generalVisibilityBonus * 0.8f * Options.VisibilityBonus.Value * Options.GlobalModifier.Value;
            self.slugcatStats.visualStealthInSneakMode = newStats.visualStealthInSneakMode * 1.2f * Options.VisualStealthInSneakMode.Value * Options.GlobalModifier.Value;
            self.slugcatStats.loudnessFac = newStats.loudnessFac * 0.5f * Options.LoudnessFac.Value * Options.GlobalModifier.Value;
            self.slugcatStats.lungsFac = newStats.lungsFac * 0.8f * Options.LungsFac.Value * Options.GlobalModifier.Value;
            self.slugcatStats.poleClimbSpeedFac = newStats.poleClimbSpeedFac * 0.8f * Options.PoleClimbSpeedFac.Value * Options.GlobalModifier.Value;
            self.slugcatStats.corridorClimbSpeedFac = newStats.corridorClimbSpeedFac * 0.8f * Options.CorridorClimbSpeedFac.Value * Options.GlobalModifier.Value;
            self.slugcatStats.runspeedFac = newStats.runspeedFac * 0.8f * Options.RunSpeedFac.Value * Options.GlobalModifier.Value;
        }
        else
        {
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

    private void Player_SlugcatHandUpdate(On.SlugcatHand.orig_Update orig, SlugcatHand self)
    {
        // Call the original method to keep the original behavior
        orig(self);

        // Scale the hands (arms) position relative to its connection
        // In original pups have long arms, which looks goofy
        // (extensively tested, 3 different setups)
        if (self.owner.owner is not Player player || (!player.isNPC && !player.playerState.isPup)) return;

        // I don't know how to fix arms when crawling, it's not even noticable so I'm just not gonna fix it
        if (player.bodyMode == Player.BodyModeIndex.Crawl) return;

        if (player.animation == Player.AnimationIndex.HangUnderVerticalBeam)
        {
            // Fixes arms when hanging from vertical pipes
            Vector2 offset = self.absoluteHuntPos - self.owner.owner.bodyChunks[0].pos;
            self.absoluteHuntPos = self.owner.owner.bodyChunks[0].pos + offset * 0.5f;
        }

        if (player.animation == Player.AnimationIndex.HangUnderVerticalBeam ||
            player.animation == Player.AnimationIndex.StandOnBeam ||
            player.animation == Player.AnimationIndex.BeamTip
        )
        {
            // Works for standing on pipes (balancing) (that includes: standing on horizontal pipes, beam tips)
            // also required for the above fix (hanging)
            // probably doesn't fix crawling arms being too long, but im gonna keep it since it doesnt break anything
            self.relativeHuntPos *= 0.5f;
        }
    }
}