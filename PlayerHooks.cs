using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using UnityEngine;
using MonoMod.RuntimeDetour;
using MoreSlugcats;
using RainMeadow;

namespace RainMeadowPupifier;

public partial class RainMeadowPupifier
{
    private void PlayerHooks()
    {
        On.Player.ctor += Player_ctor;
        On.Player.Update += Player_Update;

        // Slugpup methods
        IL.MoreSlugcats.MSCRoomSpecificScript.ArtificerDream_3.SceneSetup += Player_AppendPupCheck;
        IL.MoreSlugcats.MSCRoomSpecificScript.ArtificerDream_4.SceneSetup += Player_AppendPupCheck;
        IL.MoreSlugcats.PlayerNPCState.ctor += Player_AppendPupCheck;
        IL.MoreSlugcats.PlayerNPCState.CycleTick += Player_AppendPupCheck;
        IL.OverWorld.LoadFirstWorld += Player_AppendPupCheck;
        IL.Player.CanIPickThisUp += Player_AppendPupCheck;
        IL.Player.CanIPutDeadSlugOnBack += Player_AppendPupCheck;
        IL.Player.ctor += Player_AppendPupCheck;
        IL.Player.GetInitialSlugcatClass += Player_AppendPupCheck;
        IL.Player.Grabability += Player_AppendPupCheck;
        IL.Player.NPCStats.ctor += Player_AppendPupCheck;
        IL.PlayerGraphics.ApplyPalette += Player_AppendPupCheck;
        IL.PlayerGraphics.ctor += Player_AppendPupCheck;
        IL.PlayerGraphics.DefaultFaceSprite_float += Player_AppendPupCheck;
        IL.PlayerGraphics.DefaultFaceSprite_float_int += Player_AppendPupCheck;
        IL.PlayerGraphics.DrawSprites += Player_AppendPupCheck;
        IL.PlayerGraphics.TailSpeckles.DrawSprites += Player_AppendPupCheck;
        IL.PlayerGraphics.Update += Player_AppendPupCheck;
        IL.RainWorld.BuildTokenCache += Player_AppendPupCheck;
        IL.RainWorldGame.ArtificerDreamEnd += Player_AppendPupCheck;
        IL.RainWorldGame.ctor += Player_AppendPupCheck;
        IL.RainWorldGame.SpawnPlayers_bool_bool_bool_bool_WorldCoordinate += Player_AppendPupCheck;
        IL.RainWorldGame.SpawnPlayers_int_WorldCoordinate += Player_AppendPupCheck;
        IL.SaveState.SessionEnded += Player_AppendPupCheck;
        IL.SlugcatHand.Update += Player_AppendPupCheck;
        On.SlugcatHand.Update += Player_SlugcatHandUpdate;
        // we apply these in player_ctor in here
        //IL.SlugcatStats.ctor += Player_AppendPupCheck;
        IL.SlugcatStats.HiddenOrUnplayableSlugcat += Player_AppendPupCheck;
        IL.SlugcatStats.SlugcatFoodMeter += Player_AppendPupCheck;

        // we set isSlugpup, RenderAsPup to playerstate
        new Hook(typeof(Player).GetProperty("isSlugpup").GetGetMethod(), (Func<Player, bool> orig, Player self) => orig(self) || (!self.isNPC && self.playerState.isPup));
        new Hook(typeof(PlayerGraphics).GetProperty("RenderAsPup").GetGetMethod(), (Func<PlayerGraphics, bool> orig, PlayerGraphics self) => orig(self) || (!self.player.isNPC && self.player.playerState.isPup));

        // patch because it checks isSlugpup and tries getting npcStats
        new Hook(typeof(Player).GetProperty("slugcatStats").GetGetMethod(), (Func<Player, SlugcatStats> orig, Player self) => (self.isSlugpup && !self.isNPC) ? self.abstractCreature.world.game.session.characterStats : orig(self));
    }

    private void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
    {
        if (!self.isNPC && Options.SlugpupEnabled != self.playerState.isPup && self.IsLocal() && Options.UseSlugpupStatsToggle.Value)
        {
            // we multiply by survivor -> slugpup values (aka difference between survivor and slugpup)
            if (self.playerState.isPup = Options.SlugpupEnabled)
            {
                self.slugcatStats.bodyWeightFac *= Options.BodyWeightFac.Value;
                self.slugcatStats.generalVisibilityBonus *= Options.VisibilityBonus.Value;
                self.slugcatStats.visualStealthInSneakMode *= Options.VisualStealthInSneakMode.Value;
                self.slugcatStats.loudnessFac *= Options.LoudnessFac.Value;
                self.slugcatStats.lungsFac *= Options.LungsFac.Value;
                self.slugcatStats.poleClimbSpeedFac *= Options.PoleClimbSpeedFac.Value;
                self.slugcatStats.corridorClimbSpeedFac *= Options.CorridorClimbSpeedFac.Value;
                self.slugcatStats.runspeedFac *= Options.RunSpeedFac.Value;
            }
            else
            {
                self.slugcatStats.bodyWeightFac /= Options.BodyWeightFac.Value != 0 ? Options.BodyWeightFac.Value : 0.65f;
                self.slugcatStats.generalVisibilityBonus /= Options.VisibilityBonus.Value != 0 ? Options.VisibilityBonus.Value : 0.8f;
                self.slugcatStats.visualStealthInSneakMode /= Options.VisualStealthInSneakMode.Value != 0 ? Options.VisualStealthInSneakMode.Value : 1.2f;
                self.slugcatStats.loudnessFac /= Options.LoudnessFac.Value != 0 ? Options.LoudnessFac.Value : 0.5f;
                self.slugcatStats.lungsFac /= Options.LungsFac.Value != 0 ? Options.LungsFac.Value : 0.8f;
                self.slugcatStats.poleClimbSpeedFac /= Options.PoleClimbSpeedFac.Value != 0 ? Options.PoleClimbSpeedFac.Value : 0.8f;
                self.slugcatStats.corridorClimbSpeedFac /= Options.CorridorClimbSpeedFac.Value != 0 ? Options.CorridorClimbSpeedFac.Value : 0.8f;
                self.slugcatStats.runspeedFac /= Options.RunSpeedFac.Value != 0 ? Options.RunSpeedFac.Value : 0.8f;
            }
        }
        orig(self, eu);
        try
        {
            int x = 0;
            int y = 5 / x;
        }
        catch (Exception ex)
        {
            LogError(ex, "Error in Player_Update");
        }
    }

    private void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
    {
        orig(self, abstractCreature, world);
        // Slugpup Player NPC stats initialization
        if (!self.isNPC)
        {
            self.npcStats ??= new Player.NPCStats(self);
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
        catch (Exception e)
        {
            LogError(e, "Error in Player_AppendPupCheck");
        }
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