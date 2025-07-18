using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using UnityEngine;
using MonoMod.RuntimeDetour;
using MoreSlugcats;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RWCustom;
using Random = UnityEngine.Random;

namespace Pupifier;

public partial class Pupifier
{
    private void PlayerHooks()
    {
        if (IsModEnabled("henpemaz.rainmeadow"))
        {
            Log("Detected Rain Meadow");
        }
        
        On.SlugcatStats.ctor += SlugcatStats_ctor;
        On.Player.ThrownSpear += PlayerOnThrownSpear;
        On.Player.Update += Player_Update;

        On.Player.setPupStatus += Player_SetPupStatus;

        // Fix saint head
        On.PlayerGraphics.DrawSprites += PlayerGraphics_DrawSprites;

        // TODO: Make an onhook instead
        //IL.SlugcatStats.SlugcatFoodMeter += Player_AppendPupCheck;

        // ReSharper disable twice UnusedVariable

        // we set isSlugpup, RenderAsPup to playerstate
        var isSlugpupHook = new Hook(typeof(Player).GetProperty("isSlugpup")?.GetGetMethod(), (Func<Player, bool> orig, Player self) => orig(self) || (!self.isNPC && self.playerState.isPup));
        
        // Unneeded, but kept just in case
        //var RenderAsPupHook = new Hook(typeof(PlayerGraphics).GetProperty("RenderAsPup").GetGetMethod(), (Func<PlayerGraphics, bool> orig, PlayerGraphics self) => orig(self) || (!self.player.isNPC && self.player.playerState.isPup));

        // patch because it checks isSlugpup and tries getting npcStats
        var antiIsSlugpupHook = new Hook(typeof(Player).GetProperty("slugcatStats")?.GetGetMethod(), (Func<Player, SlugcatStats> orig, Player self) => self is { isSlugpup: true, isNPC: false } ? self.abstractCreature.world.game.session.characterStats : orig(self));

        // Change isSlugpup in specific methods
        // In jump if isSlugpup is true, it breaks jumping off pipes, disables for players by adding isNPC
        IL.Player.Jump += Player_AppendToIsSlugpupCheck;

        // For assistance and stats
        On.Player.Jump += Player_Jump;
        On.Player.WallJump += Player_WallJump;

        // In movement if it's true we can keep walking into walls, which shouldn't happen
        IL.Player.MovementUpdate += Player_AppendToIsSlugpupCheck;
        // To have persistent body size
        On.Player.MovementUpdate += Player_MovementUpdate;
        
        // False in SlugcatGrab if we have using both arms enabled or if we're spearmaster, and we want to pick up a spear
        IL.Player.SlugcatGrab += Player_SlugcatGrabAppendToIsSlugpupCheck;

        // Allow switching hands
        IL.Player.GrabUpdate += Player_AppendToIsSlugpupCheck;

        // Add so we get correct hand positions
        IL.SlugcatHand.Update += Player_AppendPupCheck;

        // Fix original slugpup animations
        On.SlugcatHand.Update += Player_SlugcatHandUpdate;

        // Allows grabbing other players
        IL.Player.Grabability += Player_AppendPupCheckGrabability;
        // Allow grabbing other slugpups when pup
        On.Player.Grabability += Player_Grabability;
    }

    private Player.ObjectGrabability Player_Grabability(On.Player.orig_Grabability orig, Player self, PhysicalObject obj)
    {
        if (self.isNPC || !self.playerState.isPup) return orig(self, obj);
        if (obj is Player npc && npc.playerState.alive && npc != self && npc.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Slugpup && !npc.playerState.forceFullGrown ||
            obj is Player plr && plr.playerState.alive && plr != self && !plr.isNPC && plr.playerState.isPup && !plr.playerState.forceFullGrown)
            return Player.ObjectGrabability.OneHand;
        return orig(self, obj);
    }

    private void PlayerOnThrownSpear(On.Player.orig_ThrownSpear orig, Player self, Spear spear)
    {
        orig(self, spear);
        if (Options.AddStaticDamage.Value) spear.spearDamageBonus *= Options.StaticDamage.Value;
    }

    private void SlugcatStats_ctor(On.SlugcatStats.orig_ctor orig, SlugcatStats self, SlugcatStats.Name slugcat, bool malnourished)
    {
        orig(self, slugcat, malnourished);
        if (Options.ChangeFoodPips.Value)
        {
            var oldMaxFood = self.maxFood;
            var oldFoodToHibernate = self.foodToHibernate;
            var method = "SET";
            if (Options.ChangeFoodPipsPercentage.Value)
            {
                method = "PERCENTAGE";

                float foodMultiplier = Options.PupFoodPips.Value * 0.1f;
                float hibernateMultiplier = Options.PupHibernationFoodPips.Value * 0.1f;

                if (Options.ChangeFoodPipsPercentageIgnoreDenominator.Value)
                {
                    self.maxFood = (int)(self.maxFood * foodMultiplier);
                    self.foodToHibernate = (int)(self.foodToHibernate * hibernateMultiplier);
                }
                else
                {
                    self.maxFood = Mathf.RoundToInt(self.maxFood * foodMultiplier);
                    self.foodToHibernate = Mathf.RoundToInt(self.foodToHibernate * hibernateMultiplier);
                }
            }
            else if (Options.ChangeFoodPipsSubtraction.Value)
            {
                method = "SUBTRACTION";
                self.maxFood = Mathf.Max(1, self.maxFood - Options.PupFoodPips.Value);
                self.foodToHibernate = Mathf.Max(1, self.foodToHibernate - Options.PupHibernationFoodPips.Value);
            }
            else
            {
                self.maxFood = Options.PupFoodPips.Value;
                self.foodToHibernate = Options.PupHibernationFoodPips.Value;
            }
            Log($"Changed max food using {method} from {oldMaxFood} to {self.maxFood}");
            Log($"Changed food to hibernate using {method} from {oldFoodToHibernate} to {self.foodToHibernate}");
        }
    }

    private void Player_MovementUpdate(On.Player.orig_MovementUpdate orig, Player self, bool eu)
    {
        orig(self, eu);
        
        if (IsModEnabled("henpemaz.rainmeadow"))
        {
            if (PupifierMeadowCompat.GamemodeIsMeadow()) return;
        }
        if (Options.ManualPupChange.Value && self.rollDirection == 0 && slugpupEnabled) self.bodyChunkConnections[0].distance = 12f * Options.SizeModifier.Value;
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
        var actionJumpMultiplier = Options.UseSlugpupStatsToggle.Value ? Options.ActionJumpPowerFac.Value : 1f;
        var origAnimation = self.animation;
        var origbodyChunks0 = self.bodyChunks[0];
        var origbodyChunks1 = self.bodyChunks[1];
        float origrollCounter = self.rollCounter;
        float origrollDirection = self.rollDirection;
        var origaerobicLevel = self.aerobicLevel;
        var origwhiplashJump = self.whiplashJump;
        var origlongBellySlide = self.longBellySlide;
        float origslideCounter = self.slideCounter;
        float origsuperLaunchJump = self.superLaunchJump;
        var originput0 = self.input[0];
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
            var massMultiplier = GetPlayerMassMultiplier(self);
            var num3 = Mathf.InverseLerp(0f, 25f, origrollCounter);
            self.bodyChunks[0].vel = Custom.DegToVec(origrollDirection * Mathf.Lerp(60f, 35f, num3)) * Mathf.Lerp(9.5f, 13.1f, num3) * massMultiplier * 0.65f * actionJumpMultiplier;
            self.bodyChunks[1].vel = Custom.DegToVec(origrollDirection * Mathf.Lerp(60f, 35f, num3)) * Mathf.Lerp(9.5f, 13.1f, num3) * massMultiplier * 0.65f * actionJumpMultiplier;
        }
        else if (origAnimation == Player.AnimationIndex.BellySlide)
        {
            var massMultiplier = GetPlayerMassMultiplier(self);
            var num4 = 9f;
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
            if (!origwhiplashJump && !Mathf.Approximately(originput0.x, -origrollDirection))
            {
                var num5 = 8.5f;
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
            //int num9 = self.input[0].x;
            if (self.standing)
            {
                /*if (origslideCounter is > 0 and < 10)
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
                }*/

                // The above if statement minimalized to this
                additionalModifier = origslideCounter is > 0 and < 10 ? 0.1f : 0.375f;
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
                    var num9 = origbodyChunks0.pos.x > origbodyChunks1.pos.x ? 1 : -1;
                    if (origbodyChunks0.pos.x > origbodyChunks1.pos.x == num9 > 0)
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
        var massMultiplier = Mathf.Lerp(1f, 1.15f, player.Adrenaline);
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

    private bool Player_CheckGrabability(Player player)
    {
        if (IsModEnabled("henpemaz.rainmeadow")) return PupifierMeadowCompat.Player_CheckGrababilityMeadow(player);
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
                c.Emit(OpCodes.Ldarg_1);
                c.EmitDelegate(GetHandsCanGrabAnyway);
                c.Emit(OpCodes.And);
            }
        }
        catch (Exception ex)
        {
            LogError(ex, "Error in Player_AppendToIsSlugpupCheck");
        }
    }

    // Inverted ifs because we need to not pass the slugpup if check, not go inside it
    private bool GetHandsCanGrabAnyway(Player player, PhysicalObject obj)
    {
        if (Options.SpearmasterTwoHanded.Value && player.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Spear &&
            obj is Spear && IsHoldingSpear(player)) return false;
        return !Options.UseBothHands.Value;
    }

    private bool IsHoldingSpear(Player player)
    {
        return player.grasps[0]?.grabbed is Spear || player.grasps[1]?.grabbed is Spear;
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
    
    // ReSharper disable once UnusedMember.Local
    private void Player_AppendPupCheckGraphics(ILContext il)
    {
        //351	0328	isinst	Player
        //352	032D	ldfld	class SlugcatStats/Name Player::SlugCatClass
        //353	0332	ldsfld	class SlugcatStats/Name MoreSlugcats.MoreSlugcatsEnums/SlugcatStatsName::Slugpup
        //354	0337	call	bool class ExtEnum`1<class SlugcatStats/Name>::op_Equality(class ExtEnum`1<!0>, class ExtEnum`1<!0>)
        //355	033C	brfalse.s	363 (0352) ldsfld bool ModManager::CoopAvailable
        try
        {
            // 1 2 3
            var matchIteration = 0;
            var matchList = new List<int> {0,1,2};
            var c = new ILCursor(il);
            // Match the IL sequence for `SlugCatClass == Slugpup`
            while (c.TryGotoNext(MoveType.AfterLabel,
                       i => i.MatchLdfld(typeof(Player).GetField("SlugCatClass")), // Load SlugCatClass
                       i => i.MatchLdsfld(typeof(MoreSlugcatsEnums.SlugcatStatsName).GetField("Slugpup")), // Load Slugpup
                       i => i.MatchCall(typeof(ExtEnum<SlugcatStats.Name>).GetMethod("op_Equality")) // Call ExtEnum op_Equality
                   ))
            {
                if (!matchList.Contains(matchIteration))
                {
                    c.Emit(OpCodes.Dup); // Duplicate Player
                    c.Index += 3; // Move to delegate
                    c.EmitDelegate((Player player, bool isSlugpup) => isSlugpup || (!player.isNPC && player.playerState.isPup));
                }
                matchIteration++;
            }
        }
        catch (Exception ex)
        {
            LogError(ex, "Error in Player_AppendPupCheck");
        }
    }
    
    private static readonly Dictionary<SlugcatStats.Name, (float[] rads, float[] connectionRads)> TailsDict = new();

    private void Player_SetPupStatus(On.Player.orig_setPupStatus orig, Player self, bool set)
    {
        orig(self, set);

        if (IsModEnabled("henpemaz.rainmeadow"))
        {
            if (PupifierMeadowCompat.GamemodeIsMeadow()) return;
        }

        try
        {
            if (self.graphicsModule is PlayerGraphics playerGraphics)
            {
                if (!TailsDict.TryGetValue(self.SlugCatClass, out var originalValues))
                {
                    var rads = playerGraphics.tail.Select(t => t.rad).ToArray();
                    var connectionRads = playerGraphics.tail.Select(t => t.connectionRad).ToArray();
                    originalValues = (rads, connectionRads);
                    TailsDict.Add(self.SlugCatClass, originalValues);
                }

                Log("Adjusting tail dimensions");
            
                var scale = slugpupEnabled ? Options.TailSize.Value : 1f;
                for (var i = 0; i < playerGraphics.tail.Length; i++)
                {
                    if (i >= originalValues.rads.Length) break;
                
                    playerGraphics.tail[i].rad = originalValues.rads[i] * scale;
                    playerGraphics.tail[i].connectionRad = originalValues.connectionRads[i] * scale;
                }
            }
        }
        catch (Exception ex)
        {
            LogError(ex, "Error in Player_SetPupStatus");
        }
    }
    
    private void PlayerGraphics_DrawSprites(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        orig(self, sLeaser, rCam, timeStacker, camPos);
    
        // Access the private 'player' field using Harmony's Traverse
        var player = Traverse.Create(self).Field("player").GetValue<Player>();
        if (player == null || player.isNPC || !player.playerState.isPup) return;

        try
        {
            if (player.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Saint)
            {
                sLeaser.sprites[3].element = Futile.atlasManager.GetElementWithName($"HeadB0");
            }
        }
        catch (Exception ex)
        {
            LogError(ex, "Error in PlayerGraphics_DrawSprites");
        }
    }

    public bool slugpupEnabled;
    bool _localPlayer;
    private void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
    {
        Player_ChangeMode(self);
        orig(self, eu);
    }

    private void Player_ManualPupChange(Player self)
    {
        if (self.isNPC || slugpupEnabled == self.playerState.isPup) return;
        
        var newMass = (0.7f + (slugpupEnabled ? 0.05f : 0f) * self.slugcatStats.bodyWeightFac +
                       (slugpupEnabled ? 0.18f : 0f) * Options.SizeModifier.Value) / 2f;
        Log($"ManualPupChange: Changing mass to {newMass}, reducing tail size and trying to reduce connection distance");
        
        // Manual body size (mass)
        // base + 0.05 if slugpup
        // 0.18 if slugpup and mysterious "bool1"
        // and then divided by 2f
        self.bodyChunks[0].mass = newMass;
        self.bodyChunks[1].mass = newMass;
        
        // I would love to make this work, but it'll take some time to find what is overriding this when we assign it
        // distance 17f normal, 12f slugpup
        //self.bodyChunkConnections = new []{new PhysicalObject.BodyChunkConnection(self.bodyChunks[0], self.bodyChunks[1], slugpupEnabled ? 12f : 17f, PhysicalObject.BodyChunkConnection.Type.Normal, 1f, 0.5f)};
        self.bodyChunkConnections[0].distance = slugpupEnabled ? 12f : 17f;
    }

    private void Player_ChangeMode(Player self)
    {
        if (self.isNPC || slugpupEnabled == self.playerState.isPup || self.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Slugpup && !Options.EnableWhenSlugpupClass.Value) return;
        if (IsModEnabled("henpemaz.rainmeadow"))
        {
            _localPlayer = PupifierMeadowCompat.PlayerIsLocal(self);
            if (!_localPlayer) return;
            if (PupifierMeadowCompat.GamemodeIsMeadow())
            {
                Log("[DO NOT REPORT THIS] Detected Meadow Gamemode, Henpemaz has disabled pups in this mode and I am respecting that.");
                self.setPupStatus(false);
                slugpupEnabled = false;
                return;
            }
        }

        try
        {
            Player_SetMode(self);
        }
        catch (Exception ex)
        {
            LogError(ex, "Error in Player_SetMode");
        }
    }

    private static readonly Dictionary<SlugcatStats.Name, SlugBaseStats> BaseStatsCache = new();
    private static readonly Dictionary<SlugcatStats.Name, SlugBaseStats> MalnourishedBaseStatsCache = new();

    private readonly struct SlugBaseStats
    {
        public readonly string name;
        public readonly float BodyWeightFac;
        public readonly float GeneralVisibilityBonus;
        public readonly float VisualStealthInSneakMode;
        public readonly float LoudnessFac;
        public readonly float LungsFac;
        public readonly float PoleClimbSpeedFac;
        public readonly float CorridorClimbSpeedFac;
        public readonly float RunspeedFac;
        public readonly int throwingSkill;

        public SlugBaseStats(SlugcatStats stats)
        {
            name = stats.name.value;
            BodyWeightFac = stats.bodyWeightFac;
            GeneralVisibilityBonus = stats.generalVisibilityBonus;
            VisualStealthInSneakMode = stats.visualStealthInSneakMode;
            LoudnessFac = stats.loudnessFac;
            LungsFac = stats.lungsFac;
            PoleClimbSpeedFac = stats.poleClimbSpeedFac;
            CorridorClimbSpeedFac = stats.corridorClimbSpeedFac;
            RunspeedFac = stats.runspeedFac;
            throwingSkill = stats.throwingSkill;
        }
    }
    
    private void Player_SetMode(Player self)
    {
        // setPupStatus sets isPup and also updates body proportions
        // we multiply by survivor -> slugpup values (aka difference between survivor and slugpup)
        // Change body size using setPupStatus
        if (!BaseStatsCache.TryGetValue(self.SlugCatClass, out var baseStats))
        {
            var tempStats = new SlugcatStats(self.SlugCatClass, false);
            baseStats = new SlugBaseStats(tempStats);
            BaseStatsCache.Add(self.SlugCatClass, baseStats);
        }

        if (!MalnourishedBaseStatsCache.TryGetValue(self.SlugCatClass, out var malnourishedBaseStats))
        {
            var tempStats = new SlugcatStats(self.SlugCatClass, true);
            malnourishedBaseStats = new SlugBaseStats(tempStats);
            MalnourishedBaseStatsCache.Add(self.SlugCatClass, malnourishedBaseStats);
        }

        var activeBaseStats = self.slugcatStats.malnourished 
            ? malnourishedBaseStats 
            : baseStats;
        
        if (Options.LoggingPupEnabled.Value) Log($"Set pup status for {(_localPlayer ? "local" : "non-meadow")} player to {slugpupEnabled}, RainMeadow is {(IsModEnabled("henpemaz.rainmeadow") ? "enabled" : "disabled")}");

        // Change body size using setPupStatus
        self.setPupStatus(slugpupEnabled);
        // Change body size manually if toggled on
        if (Options.ManualPupChange.Value) Player_ManualPupChange(self);
        
        // Set grabability for others if in meadow
        if (IsModEnabled("henpemaz.rainmeadow"))
        {
            if (PupifierMeadowCompat.GameIsMeadow()) PupifierMeadowCompat.ToggleGrabbable(self);
        }

        // Set relative stats on status
        if (!Options.UseSlugpupStatsToggle.Value) return;
        LogStats(self, "pre-change");
        if (slugpupEnabled)
        {
            self.slugcatStats.name.value = activeBaseStats.name;
            self.slugcatStats.bodyWeightFac = activeBaseStats.BodyWeightFac * 0.65f * Options.BodyWeightFac.Value * Options.GlobalModifier.Value;
            self.slugcatStats.generalVisibilityBonus = activeBaseStats.GeneralVisibilityBonus * 0.8f * Options.VisibilityBonus.Value * Options.GlobalModifier.Value;
            self.slugcatStats.visualStealthInSneakMode = activeBaseStats.VisualStealthInSneakMode * 1.2f * Options.VisualStealthInSneakMode.Value * Options.GlobalModifier.Value;
            self.slugcatStats.loudnessFac = activeBaseStats.LoudnessFac * 0.5f * Options.LoudnessFac.Value * Options.GlobalModifier.Value;
            self.slugcatStats.lungsFac = activeBaseStats.LungsFac * 0.8f * Options.LungsFac.Value * Options.GlobalModifier.Value;
            self.slugcatStats.poleClimbSpeedFac = activeBaseStats.PoleClimbSpeedFac * 0.8f * Options.PoleClimbSpeedFac.Value * Options.GlobalModifier.Value;
            self.slugcatStats.corridorClimbSpeedFac = activeBaseStats.CorridorClimbSpeedFac * 0.8f * Options.CorridorClimbSpeedFac.Value * Options.GlobalModifier.Value;
            self.slugcatStats.runspeedFac = activeBaseStats.RunspeedFac * 0.8f * Options.RunSpeedFac.Value * Options.GlobalModifier.Value;
            self.slugcatStats.throwingSkill = Options.throwingSkill.Value;
        }
        else
        {
            // Direct assignment from value type ensures no reference issues
            self.slugcatStats.name.value = activeBaseStats.name;
            self.slugcatStats.bodyWeightFac = activeBaseStats.BodyWeightFac;
            self.slugcatStats.generalVisibilityBonus = activeBaseStats.GeneralVisibilityBonus;
            self.slugcatStats.visualStealthInSneakMode = activeBaseStats.VisualStealthInSneakMode;
            self.slugcatStats.loudnessFac = activeBaseStats.LoudnessFac;
            self.slugcatStats.lungsFac = activeBaseStats.LungsFac;
            self.slugcatStats.poleClimbSpeedFac = activeBaseStats.PoleClimbSpeedFac;
            self.slugcatStats.corridorClimbSpeedFac = activeBaseStats.CorridorClimbSpeedFac;
            self.slugcatStats.runspeedFac = activeBaseStats.RunspeedFac;
            self.slugcatStats.throwingSkill = activeBaseStats.throwingSkill;
        }
        LogStats(self, "post-change");
    }

    private void LogStats(Player self, string prepost)
    {
        if (!Options.LoggingStatusEnabled.Value) return;
        Log($"---------------------------------------------------");
        Log($"Stats {prepost}");
		Log($"Class Name: {self.slugcatStats.name.value}");
        Log($"bodyWeightFac: {self.slugcatStats.bodyWeightFac}");
        Log($"generalVisibilityBonus: {self.slugcatStats.generalVisibilityBonus}");
        Log($"visualStealthInSneakMode: {self.slugcatStats.visualStealthInSneakMode}");
        Log($"loudnessFac: {self.slugcatStats.loudnessFac}");
        Log($"lungsFac: {self.slugcatStats.lungsFac}");
        Log($"poleClimbSpeedFac: {self.slugcatStats.poleClimbSpeedFac}");
        Log($"corridorClimbSpeedFac: {self.slugcatStats.corridorClimbSpeedFac}");
        Log($"runspeedFac: {self.slugcatStats.runspeedFac}");
        Log($"---------------------------------------------------");
    }

    private void Player_SlugcatHandUpdate(On.SlugcatHand.orig_Update orig, SlugcatHand self)
    {
        orig(self);
        
        // Get first owner (protected field) via Traverse
        //var limb = Traverse.Create(self).Field("owner").GetValue<GraphicsModule>();
        //if (limb == null) return;

        // Get final owner (Player) from the limb object
        //if (limb.owner is not Player player || player.isNPC || !player.playerState.isPup) return;

        if (self.owner.owner is not Player player || player.isNPC || !player.playerState.isPup) return;
        
        try 
        {
            if (player.animation == Player.AnimationIndex.HangUnderVerticalBeam)
            {
                // Use the retrieved player reference
                var offset = self.absoluteHuntPos - player.bodyChunks[0].pos;
                self.absoluteHuntPos = player.bodyChunks[0].pos + offset * 0.5f;
            }

            if (player.animation == Player.AnimationIndex.HangUnderVerticalBeam ||
                player.animation == Player.AnimationIndex.StandOnBeam ||
                player.animation == Player.AnimationIndex.BeamTip)
            {
                self.relativeHuntPos *= 0.5f;
            }
        }
        catch (Exception ex)
        {
            LogError(ex, "Error in Player_SlugcatHandUpdate");
        }
    }
}