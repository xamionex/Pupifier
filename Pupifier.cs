using System;
using System.Security.Permissions;
using BepInEx;
using System.Reflection;
using UnityEngine;
using UnityEngine.Serialization;


[assembly: AssemblyVersion(PluginInfo.PluginVersion)]
#pragma warning disable CS0618
#pragma warning disable CS8618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
namespace Pupifier
{
    [BepInDependency("henpemaz.rainmeadow", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("elumenix.pupify", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInPlugin(PluginInfo.PluginGUID, PluginInfo.PluginName, PluginInfo.PluginVersion)]
    public partial class Pupifier : BaseUnityPlugin
    {
        public static Pupifier Instance;
        public static PupifierOptions Options;
        public static PlayerData PlayerData;
        [FormerlySerializedAs("RainMeadowEnabled")] public bool rainMeadowEnabled;

        public void OnEnable()
        {
            Instance = this;
            Options = new PupifierOptions();
            PlayerData = new PlayerData();

            On.RainWorld.OnModsInit += RainWorldOnOnModsInit;
        }

        private void Update()
        {
            if (!_isInit || Options == null) return;
            if (Input.GetKeyDown(Options.SlugpupKey.Value) || (Options.UseSecondaryKeyToggle.Value && Input.GetKeyDown(Options.SlugpupSecondaryKey.Value)))
            {
                slugpupEnabled = !slugpupEnabled;
                if (Options.LoggingPupEnabled.Value)
                {
                    Log($"Key pressed, will change to {(slugpupEnabled ? "pup" : "adult")}");
                }
            }
        }

        private bool _isInit;
        private void RainWorldOnOnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {
            orig(self);
            try
            {
                if (_isInit) return;

                    if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("henpemaz.rainmeadow"))
                    {
                        Log("RainMeadow is installed. Enabling compatibility");
                        rainMeadowEnabled = true;
                    }
                    else
                    {
                        Log("RainMeadow isn't installed. Skipping checks.");
                        rainMeadowEnabled = false;
                    }
                    if (!BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("elumenix.pupify") || Options.ModAutoDisabledToggle.Value)
                    {
                        PlayerHooks();
                        Log("Hooked into methods...");
                    }
                    else
                    {
                        Log("Pupify is installed, we do not support this mod.");
                        Log("Check the remix options if you want to enable this mod anyway. (Experimental Tab)");
                        Log("Skipped hooking into methods.");
                    }

                On.RainWorldGame.ShutDownProcess += RainWorldGameOnShutDownProcess;
                On.GameSession.ctor += GameSessionOnctor;

                MachineConnector.SetRegisteredOI(PluginInfo.PluginGUID, Options);
                Log($"Registered OI...");
                _isInit = true;
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