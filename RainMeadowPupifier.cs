using System;
using System.Security.Permissions;
using BepInEx;
using System.Reflection;
using UnityEngine;


[assembly: AssemblyVersion(PluginInfo.PluginVersion)]
#pragma warning disable CS0618
#pragma warning disable CS8618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
namespace RainMeadowPupifier
{
    [BepInPlugin(PluginInfo.PluginGUID, PluginInfo.PluginName, PluginInfo.PluginVersion)]
    public partial class RainMeadowPupifier : BaseUnityPlugin
    {
        public static RainMeadowPupifier instance;
        public static RainMeadowPupifierOptions Options;

        public void OnEnable()
        {
            instance = this;
            Options = new RainMeadowPupifierOptions();

            On.RainWorld.OnModsInit += RainWorldOnOnModsInit;
        }

        private void Update()
        {
            if (!IsInit || Options == null) return;
            if (Input.GetKeyDown(Options.SlugpupKey.Value))
            {
                Options.SlugpupEnabled = !Options.SlugpupEnabled;
                Options.SlugpupKeyPressed++;
            }
        }

        private bool IsInit;
        private void RainWorldOnOnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {
            orig(self);
            try
            {
                if (IsInit) return;

                if (!BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("elumenix.pupify"))
                {
                    PlayerHooks();
                    Log("Hooked into methods...");
                }
                else
                {
                    Log("Pupify is installed, we do not support this mod.");
                }

                On.RainWorldGame.ShutDownProcess += RainWorldGameOnShutDownProcess;
                On.GameSession.ctor += GameSessionOnctor;

                MachineConnector.SetRegisteredOI(PluginInfo.PluginGUID, Options);
                Log($"Registered OI...");
                IsInit = true;
                Log($"Fully initialized!");
            }
            catch (Exception ex)
            {
                LogError(ex, $"Failed to initialize mod {PluginInfo.PluginGUID}");
                throw;
            }
        }

        private void RainWorldGameOnShutDownProcess(On.RainWorldGame.orig_ShutDownProcess orig, RainWorldGame self)
        {
            orig(self);
            ClearMemory();
        }
        private void GameSessionOnctor(On.GameSession.orig_ctor orig, GameSession self, RainWorldGame game)
        {
            orig(self, game);
            ClearMemory();
        }

        private void ClearMemory()
        {
            //List/Dict.Clear();
        }
    }
}