using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using UnityEngine;
using MonoMod.RuntimeDetour;
using MoreSlugcats;
using System.Collections.Generic;
using System.Linq;
using RWCustom;

namespace Pupifier;

public partial class Pupifier
{
    private void PlayerHooks()
    {
        if (IsModEnabled("henpemaz.rainmeadow"))
        {
            Log("Detected Rain Meadow");
        }
        
        On.Player.Update += Player_Update;

        On.Player.setPupStatus += Player_SetPupStatus;

        // Fix saint head
        On.PlayerGraphics.DrawSprites += PlayerGraphics_DrawSprites;

        // TODO: Make an onhook instead
        //IL.SlugcatStats.SlugcatFoodMeter += Player_AppendPupCheck;

        // we set isSlugpup, RenderAsPup to playerstate
        new Hook(typeof(Player).GetProperty("isSlugpup").GetGetMethod(), (Func<Player, bool> orig, Player self) => orig(self) || (!self.isNPC && self.playerState.isPup));
        //new Hook(typeof(PlayerGraphics).GetProperty("RenderAsPup").GetGetMethod(), (Func<PlayerGraphics, bool> orig, PlayerGraphics self) => orig(self) || (!self.player.isNPC && self.player.playerState.isPup));

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
        // To have persistent body size
        On.Player.MovementUpdate += Player_MovementUpdate;
        
        // False in SlugcatGrab if we have using both arms enabled or if were spearmaster and we want to pick up a spear
        IL.Player.SlugcatGrab += Player_SlugcatGrabAppendToIsSlugpupCheck;

        // Add so we get correct hand positions
        IL.SlugcatHand.Update += Player_AppendPupCheck;
        // Fix original slugpup animations
        On.SlugcatHand.Update += Player_SlugcatHandUpdate;

        // Allows grabbing other players
        IL.Player.Grabability += Player_AppendPupCheckGrabability;
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
            var MatchIteration = 0;
            var MatchList = new List<int>() {0,1,2};
            var c = new ILCursor(il);
            // Match the IL sequence for `SlugCatClass == Slugpup`
            while (c.TryGotoNext(MoveType.AfterLabel,
                       i => i.MatchLdfld(typeof(Player).GetField("SlugCatClass")), // Load SlugCatClass
                       i => i.MatchLdsfld(typeof(MoreSlugcatsEnums.SlugcatStatsName).GetField("Slugpup")), // Load Slugpup
                       i => i.MatchCall(typeof(ExtEnum<SlugcatStats.Name>).GetMethod("op_Equality")) // Call ExtEnum op_Equality
                   ))
            {
                if (!MatchList.Contains(MatchIteration))
                {
                    c.Emit(OpCodes.Dup); // Duplicate Player
                    c.Index += 3; // Move to delegate
                    c.EmitDelegate((Player player, bool isSlugpup) => isSlugpup || (!player.isNPC && player.playerState.isPup));
                }
                MatchIteration++;
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
            
                float scale = slugpupEnabled ? 0.8f : 1f;
                for (int i = 0; i < playerGraphics.tail.Length; i++)
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

    public bool slugpupEnabled = false;
    bool _localPlayer = false;
    private void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
    {
        Player_ChangeMode(self);
        orig(self, eu);
    }

    private void Player_ManualPupChange(Player self)
    {
        if (self.isNPC || slugpupEnabled == self.playerState.isPup) return;
        
        float newMass = (0.7f + (slugpupEnabled ? 0.05f : 0f) * self.slugcatStats.bodyWeightFac +
                         (self.bool1 && slugpupEnabled ? 0.18f : 0f) * Options.SizeModifier.Value) / 2f;
        Log($"ManualPupChange: Changing mass to {newMass}, reducing tail size and trying to reduce connection distance");
        
        // Manual body size (mass)
        // base + 0.05 if slugpup
        // 0.18 if slugpup and mysterious "bool1"
        // and then divided by 2f
        self.bodyChunks[0].mass = newMass;
        self.bodyChunks[1].mass = newMass;
        
        // I would love to make this work but it'll take some time to find what is overriding this when we assign it
        // distance 17f normal, 12f slugpup
        //self.bodyChunkConnections = new []{new PhysicalObject.BodyChunkConnection(self.bodyChunks[0], self.bodyChunks[1], slugpupEnabled ? 12f : 17f, PhysicalObject.BodyChunkConnection.Type.Normal, 1f, 0.5f)};
        self.bodyChunkConnections[0].distance = slugpupEnabled ? 12f : 17f;
    }

    private void Player_ChangeMode(Player self)
    {
        if (self.isNPC || slugpupEnabled == self.playerState.isPup) return;
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
        public readonly float bodyWeightFac;
        public readonly float generalVisibilityBonus;
        public readonly float visualStealthInSneakMode;
        public readonly float loudnessFac;
        public readonly float lungsFac;
        public readonly float poleClimbSpeedFac;
        public readonly float corridorClimbSpeedFac;
        public readonly float runspeedFac;

        public SlugBaseStats(SlugcatStats stats)
        {
            bodyWeightFac = stats.bodyWeightFac;
            generalVisibilityBonus = stats.generalVisibilityBonus;
            visualStealthInSneakMode = stats.visualStealthInSneakMode;
            loudnessFac = stats.loudnessFac;
            lungsFac = stats.lungsFac;
            poleClimbSpeedFac = stats.poleClimbSpeedFac;
            corridorClimbSpeedFac = stats.corridorClimbSpeedFac;
            runspeedFac = stats.runspeedFac;
        }
    }
    
    private void Player_SetMode(Player self)
    {
        // setPupStatus sets isPup and also updates body proportions
        // we multiply by survivor -> slugpup values (aka difference between survivor and slugpup)
        // Change body size using setPupStatus
        if (!BaseStatsCache.TryGetValue(self.SlugCatClass, out SlugBaseStats baseStats))
        {
            var tempStats = new SlugcatStats(self.SlugCatClass, false);
            baseStats = new SlugBaseStats(tempStats);
            BaseStatsCache.Add(self.SlugCatClass, baseStats);
        }

        if (!MalnourishedBaseStatsCache.TryGetValue(self.SlugCatClass, out SlugBaseStats malnourishedBaseStats))
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
        };
        
        // Set relative stats on status
        if (!Options.UseSlugpupStatsToggle.Value) return;
        LogStats(self, false);
        if (slugpupEnabled)
        {
            self.slugcatStats.bodyWeightFac = activeBaseStats.bodyWeightFac * 0.65f * Options.BodyWeightFac.Value * Options.GlobalModifier.Value;
            self.slugcatStats.generalVisibilityBonus = activeBaseStats.generalVisibilityBonus * 0.8f * Options.VisibilityBonus.Value * Options.GlobalModifier.Value;
            self.slugcatStats.visualStealthInSneakMode = activeBaseStats.visualStealthInSneakMode * 1.2f * Options.VisualStealthInSneakMode.Value * Options.GlobalModifier.Value;
            self.slugcatStats.loudnessFac = activeBaseStats.loudnessFac * 0.5f * Options.LoudnessFac.Value * Options.GlobalModifier.Value;
            self.slugcatStats.lungsFac = activeBaseStats.lungsFac * 0.8f * Options.LungsFac.Value * Options.GlobalModifier.Value;
            self.slugcatStats.poleClimbSpeedFac = activeBaseStats.poleClimbSpeedFac * 0.8f * Options.PoleClimbSpeedFac.Value * Options.GlobalModifier.Value;
            self.slugcatStats.corridorClimbSpeedFac = activeBaseStats.corridorClimbSpeedFac * 0.8f * Options.CorridorClimbSpeedFac.Value * Options.GlobalModifier.Value;
            self.slugcatStats.runspeedFac = activeBaseStats.runspeedFac * 0.8f * Options.RunSpeedFac.Value * Options.GlobalModifier.Value;
        }
        else
        {
            // Direct assignment from value type ensures no reference issues
            self.slugcatStats.bodyWeightFac = activeBaseStats.bodyWeightFac;
            self.slugcatStats.generalVisibilityBonus = activeBaseStats.generalVisibilityBonus;
            self.slugcatStats.visualStealthInSneakMode = activeBaseStats.visualStealthInSneakMode;
            self.slugcatStats.loudnessFac = activeBaseStats.loudnessFac;
            self.slugcatStats.lungsFac = activeBaseStats.lungsFac;
            self.slugcatStats.poleClimbSpeedFac = activeBaseStats.poleClimbSpeedFac;
            self.slugcatStats.corridorClimbSpeedFac = activeBaseStats.corridorClimbSpeedFac;
            self.slugcatStats.runspeedFac = activeBaseStats.runspeedFac;
        }
        LogStats(self, true);
    }

    private void LogStats(Player self, bool prepost)
    {
        if (!Options.LoggingStatusEnabled.Value) return;
        Log($"Stats {(prepost ? "post" : "pre")}-change");
        Log($"bodyWeightFac: {self.slugcatStats.bodyWeightFac}");
        Log($"generalVisibilityBonus: {self.slugcatStats.generalVisibilityBonus}");
        Log($"visualStealthInSneakMode: {self.slugcatStats.visualStealthInSneakMode}");
        Log($"loudnessFac: {self.slugcatStats.loudnessFac}");
        Log($"lungsFac: {self.slugcatStats.lungsFac}");
        Log($"poleClimbSpeedFac: {self.slugcatStats.poleClimbSpeedFac}");
        Log($"corridorClimbSpeedFac: {self.slugcatStats.corridorClimbSpeedFac}");
        Log($"runspeedFac: {self.slugcatStats.runspeedFac}");
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
        //if (player.bodyMode == Player.BodyModeIndex.Crawl) {};

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