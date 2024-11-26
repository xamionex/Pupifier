using Menu.Remix.MixedUI;
using System;
using UnityEngine;

namespace Pupifier
{
    public partial class Pupifier
    {
        public partial class PupifierOptions : OptionInterface
        {
            public int curTab;
            public readonly Configurable<KeyCode> SlugpupKey;
            public readonly Configurable<bool> UseSecondaryKeyToggle;
            public readonly Configurable<KeyCode> SlugpupSecondaryKey;
            public readonly Configurable<bool> UseSlugpupStatsToggle;
            public readonly Configurable<bool> ModAutoDisabledToggle;
            public readonly Configurable<float> GlobalModifier;
            public readonly Configurable<float> BodyWeightFac;
            public readonly Configurable<float> VisibilityBonus;
            public readonly Configurable<float> VisualStealthInSneakMode;
            public readonly Configurable<float> LoudnessFac;
            public readonly Configurable<float> LungsFac;
            public readonly Configurable<float> PoleClimbSpeedFac;
            public readonly Configurable<float> CorridorClimbSpeedFac;
            public readonly Configurable<float> RunSpeedFac;
            public readonly Configurable<float> JumpPowerFac;
            public readonly Configurable<float> WallJumpPowerFac;
            public readonly Configurable<bool> DisableBeingGrabbed;

            public PupifierOptions()
            {
                // Pupifier tab
                SlugpupKey = config.Bind(nameof(SlugpupKey), KeyCode.Minus, new ConfigurableInfo("Key to toggle pup mode.", null, "", "Keybind for toggling between pup mode"));
                UseSecondaryKeyToggle = config.Bind(nameof(UseSecondaryKeyToggle), true, new ConfigurableInfo("If true, the secondary key will be used to toggle pup mode.", null, "", "Use Secondary Key to Toggle Pup Mode"));
                SlugpupSecondaryKey = config.Bind(nameof(SlugpupSecondaryKey), KeyCode.JoystickButton3, new ConfigurableInfo("Secondary Key to toggle pup mode, useful for controllers.", null, "", "Secondary Keybind for toggling between pup mode, useful for controllers"));

                // Stats tab
                UseSlugpupStatsToggle = config.Bind(nameof(UseSlugpupStatsToggle), true, new ConfigurableInfo("If true, stats will be changed to a slugpup's equivalent.", null, "", "Use Relative Slugpup Stats"));
                GlobalModifier = config.Bind(nameof(GlobalModifier), 1f, new ConfigurableInfo("Multiplies all stats by this value.", null, "", "Global Modifier"));
                BodyWeightFac = config.Bind(nameof(BodyWeightFac), 0.65f, new ConfigurableInfo("Factor affecting body weight.", null, "", "Body Weight"));
                VisibilityBonus = config.Bind(nameof(VisibilityBonus), 0.8f, new ConfigurableInfo("Factor affecting visibility.", null, "", "Visibility"));
                VisualStealthInSneakMode = config.Bind(nameof(VisualStealthInSneakMode), 1.2f, new ConfigurableInfo("Factor affecting visual stealth when sneaking.", null, "", "Visual Stealth In Sneak Mode"));
                LoudnessFac = config.Bind(nameof(LoudnessFac), 0.5f, new ConfigurableInfo("Factor affecting how loud you are.", null, "", "Loudness"));
                LungsFac = config.Bind(nameof(LungsFac), 0.8f, new ConfigurableInfo("Factor affecting lung capacity.", null, "", "Lung Capacity"));
                PoleClimbSpeedFac = config.Bind(nameof(PoleClimbSpeedFac), 0.8f, new ConfigurableInfo("Factor affecting pole climb speed.", null, "", "Pole Climb Speed"));
                CorridorClimbSpeedFac = config.Bind(nameof(CorridorClimbSpeedFac), 0.8f, new ConfigurableInfo("Factor affecting corridor climb speed.", null, "", "Corridor Climb Speed"));
                RunSpeedFac = config.Bind(nameof(RunSpeedFac), 0.8f, new ConfigurableInfo("Factor affecting run speed.", null, "", "Run Speed"));
                JumpPowerFac = config.Bind(nameof(JumpPowerFac), 0.5f, new ConfigurableInfo("Factor affecting jump power.", null, "", "Jump Power"));
                WallJumpPowerFac = config.Bind(nameof(WallJumpPowerFac), 1f, new ConfigurableInfo("Factor affecting wall jump power. (Additive, i.e. you set 1.2 to be 20% better at wall jumping)", null, "", "Wall Jump Power Multiplier"));

                // Toggles tab
                DisableBeingGrabbed = config.Bind(nameof(DisableBeingGrabbed), false, new ConfigurableInfo("If enabled, you can't be grabbed", null, "", "Disable being Grabbed"));

                // Experimental tab
                ModAutoDisabledToggle = config.Bind(nameof(ModAutoDisabledToggle), false, new ConfigurableInfo("If true, Pupifier will not disable itself when other mods are found. This requires a restart", null, "", "Allow Incompatible Mods (Requires Restart)"));
            }

            public override void Initialize()
            {
                try
                {
                    base.Initialize();

                    Tabs = new OpTab[] { new(this, "Pupifier"), new(this, "Stats"), new(this, "Toggles"), new(this, "Experimental") };

                    /**************** Pupifier ****************/
                    curTab = 0;
                    AddTitle();
                    float x = 80f;
                    float y = 540f;
                    float sepr = 30f;

                    if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("elumenix.pupify"))
                    {
                        AddText("elumenix.pupify is installed, we do not support this mod.", new Vector2(x, y -= sepr));
                        return;
                    }

                    AddKeyBinder(SlugpupKey, new Vector2(x, y -= sepr), Color.green, Color.green);
                    AddCheckbox(UseSecondaryKeyToggle, new Vector2(x, y -= sepr), Color.yellow, Color.yellow);
                    AddKeyBinder(SlugpupSecondaryKey, new Vector2(x, y -= sepr + 6f), Color.yellow, Color.yellow);

                    /**************** Stats ****************/
                    curTab++;
                    AddTitle();
                    x = 150f;
                    y = 540f;
                    sepr = 30f;
                    AddCheckbox(UseSlugpupStatsToggle, new Vector2(x, y -= sepr), Color.green, Color.green);
                    AddText("The following stats will multiply our current slugcat stats by the value here", new Vector2(x, y -= sepr), Color.gray);
                    AddText("(current slugcat stat * stat option)", new Vector2(x, y -= sepr), Color.gray);
                    x = 0f;
                    sepr = 30f;
                    AddFloatSlider(GlobalModifier, new Vector2(x, y -= sepr), 0.01f, 10f, 400, Color.red, Color.red, Color.red);
                    AddFloatSlider(BodyWeightFac, new Vector2(x, y -= sepr), 0.01f, 5f, 400, Color.yellow, Color.yellow, Color.yellow);
                    AddFloatSlider(VisibilityBonus, new Vector2(x, y -= sepr), 0.01f, 5f, 400, Color.gray, Color.gray, Color.gray);
                    AddFloatSlider(VisualStealthInSneakMode, new Vector2(x, y -= sepr), 0.01f, 5f, 400, Color.gray, Color.gray, Color.gray);
                    AddFloatSlider(LoudnessFac, new Vector2(x, y -= sepr), 0.01f, 5f, 400, Color.magenta, Color.magenta, Color.magenta);
                    AddFloatSlider(LungsFac, new Vector2(x, y -= sepr), 0.01f, 5f, 400, Color.cyan, Color.cyan, Color.cyan);
                    AddFloatSlider(PoleClimbSpeedFac, new Vector2(x, y -= sepr), 0.01f, 5f, 400, Color.blue, Color.blue, Color.blue);
                    AddFloatSlider(CorridorClimbSpeedFac, new Vector2(x, y -= sepr), 0.01f, 5f, 400, Color.blue, Color.blue, Color.blue);
                    AddFloatSlider(RunSpeedFac, new Vector2(x, y -= sepr), 0.01f, 5f, 400, Color.blue, Color.blue, Color.blue);
                    AddFloatSlider(JumpPowerFac, new Vector2(x, y -= sepr), 0.01f, 5f, 400, Color.blue, Color.blue, Color.blue);
                    AddFloatSlider(WallJumpPowerFac, new Vector2(x, y -= sepr), 0.01f, 5f, 400, Color.blue, Color.blue, Color.blue);

                    /**************** Toggles ****************/
                    curTab++;
                    AddTitle();
                    x = 150f;
                    y = 540f;
                    sepr = 30f;
                    AddCheckbox(DisableBeingGrabbed, new Vector2(x, y -= sepr), Color.yellow, Color.yellow, Color.yellow);

                    /**************** Experimental ****************/
                    curTab++;
                    AddTitle();
                    x = 150f;
                    y = 540f;
                    sepr = 30f;
                    AddCheckbox(ModAutoDisabledToggle, new Vector2(x, y -= sepr), Color.red, Color.red, Color.red);

                    Log("Added all options...");
                }
                catch (Exception ex)
                {
                    LogError(ex, "Error opening Pupifier Options Menu");
                }
            }

            public override void Update()
            {
            }

            private void AddTitle()
            {
                OpLabel title = new(new Vector2(150f, 560f), new Vector2(300f, 30f), PluginInfo.PluginName, bigText: true);
                OpLabel version = new(new Vector2(150f, 540f), new Vector2(300f, 30f), $"Version {PluginInfo.PluginVersion}");

                Tabs[curTab].AddItems(new UIelement[]
                {
                    title,
                    version
                });
            }

            private void AddText(string text, Vector2 pos, Color? clr = null)
            {
                if (clr == null)
                    clr = Menu.MenuColorEffect.rgbMediumGrey;

                OpLabel label = new(pos, new Vector2(300f, 30f), text)
                {
                    color = (Color)clr,
                };

                Tabs[curTab].AddItems(new UIelement[]
                {
                    label
                });
            }

            private void AddFloatSlider(Configurable<float> option, Vector2 pos, float min = 0, float max = 1, int width = 150, Color? clr = null, Color? clrline = null, Color? clrtext = null)
            {
                if (clr == null)
                    clr = Menu.MenuColorEffect.rgbMediumGrey;
                if (clrline == null)
                    clrline = Menu.MenuColorEffect.rgbMediumGrey;
                if (clrtext == null)
                    clrtext = Menu.MenuColorEffect.rgbMediumGrey;

                OpFloatSlider slider = new(option, pos, width)
                {
                    description = option.info.description,
                    min = min,
                    max = max,
                    colorEdge = (Color)clr,
                    colorLine = (Color)clrline
                };

                OpLabel label = new(pos.x + width + 18f, pos.y + 6f, option.info.Tags[0] as string)
                {
                    description = option.info.description,
                    color = (Color)clrtext
                };

                Tabs[curTab].AddItems(new UIelement[]
                {
                    slider,
                    label
                });
            }

            private void AddSlider(Configurable<int> option, Vector2 pos, int min = 0, int max = 1, int width = 150, Color? clr = null, Color? clrline = null, Color? clrtext = null)
            {
                if (clr == null)
                    clr = Menu.MenuColorEffect.rgbMediumGrey;
                if (clrline == null)
                    clrline = Menu.MenuColorEffect.rgbMediumGrey;
                if (clrtext == null)
                    clrtext = Menu.MenuColorEffect.rgbMediumGrey;

                OpSlider slider = new(option, pos, width)
                {
                    description = option.info.description,
                    min = min,
                    max = max,
                    colorEdge = (Color)clr,
                    colorLine = (Color)clrline
                };

                OpLabel label = new(pos.x + width + 18f, pos.y + 2f, option.info.Tags[0] as string)
                {
                    description = option.info.description,
                    color = (Color)clrtext
                };

                Tabs[curTab].AddItems(new UIelement[]
                {
                    slider,
                    label
                });
            }

            private void AddIcon(Vector2 pos, string iconName)
            {
                Tabs[curTab].AddItems(new UIelement[]
                {
                    new OpImage(pos, iconName)
                });
            }


            private void AddCheckbox(Configurable<bool> option, Vector2 pos, Color? clr = null, Color? clrfill = null, Color? clrtext = null)
            {
                if (clr == null)
                    clr = Menu.MenuColorEffect.rgbMediumGrey;
                if (clrfill == null)
                    clrfill = Menu.MenuColorEffect.rgbMediumGrey;
                if (clrtext == null)
                    clrtext = Menu.MenuColorEffect.rgbMediumGrey;

                OpCheckBox checkbox = new(option, pos)
                {
                    description = option.info.description,
                    colorEdge = (Color)clr,
                    colorFill = (Color)clrfill
                };

                OpLabel label = new(pos.x + 40f, pos.y + 2f, option.info.Tags[0] as string)
                {
                    description = option.info.description,
                    color = (Color)clrtext
                };

                Tabs[curTab].AddItems(new UIelement[]
                {
                    checkbox,
                    label
                });
            }


            private void AddKeyBinder(Configurable<KeyCode> option, Vector2 pos, Color? clr = null, Color? clrfill = null, Color? clrtext = null)
            {
                if (clr == null)
                    clr = Menu.MenuColorEffect.rgbMediumGrey;
                if (clrfill == null)
                    clrfill = Menu.MenuColorEffect.rgbMediumGrey;
                if (clrtext == null)
                    clrtext = Menu.MenuColorEffect.rgbMediumGrey;

                OpKeyBinder keyBinder = new(option, pos, new Vector2(100f, 30f), false, OpKeyBinder.BindController.AnyController)
                {
                    description = option.info.description,
                    colorEdge = (Color)clr,
                    colorFill = (Color)clrfill
                };

                OpLabel label = new(pos.x + 100f + 16f, pos.y + 5f, option.info.Tags[0] as string)
                {
                    description = option.info.description,
                    color = (Color)clrtext
                };

                Tabs[curTab].AddItems(new UIelement[]
                {
                    keyBinder,
                    label
                });
            }

            private void AddComboBox(Configurable<string> option, Vector2 pos, string[] array, float width = 80f, FLabelAlignment alH = FLabelAlignment.Center, OpLabel.LabelVAlignment alV = OpLabel.LabelVAlignment.Center, Color? clr = null, Color? clrfill = null)
            {
                if (clr == null)
                    clr = Menu.MenuColorEffect.rgbMediumGrey;
                if (clrfill == null)
                    clrfill = Menu.MenuColorEffect.rgbMediumGrey;

                OpComboBox box = new(option, pos, width, array)
                {
                    description = option.info.description,
                    colorEdge = (Color)clr,
                    colorFill = (Color)clrfill
                };

                Vector2 offset = new();
                if (alV == OpLabel.LabelVAlignment.Top)
                {
                    offset.y += box.size.y + 5f;
                }
                else if (alV == OpLabel.LabelVAlignment.Bottom)
                {
                    offset.y += -box.size.y - 5f;
                }
                else if (alH == FLabelAlignment.Right)
                {
                    offset.x += box.size.x + 20f;
                    alH = FLabelAlignment.Left;
                }
                else if (alH == FLabelAlignment.Left)
                {
                    offset.x += -box.size.x - 20f;
                    alH = FLabelAlignment.Right;
                }

                OpLabel label = new(pos + offset, box.size, option.info.Tags[0] as string)
                {
                    description = option.info.description
                };
                label.alignment = alH;
                label.verticalAlignment = OpLabel.LabelVAlignment.Center;

                Tabs[curTab].AddItems(new UIelement[]
                {
                    box,
                    label
                });
            }


            private void AddTextBox<T>(Configurable<T> option, Vector2 pos, float width = 150f, Color? clr = null, Color? clrfill = null, Color? clrtext = null)
            {
                if (clr == null)
                    clr = Menu.MenuColorEffect.rgbMediumGrey;
                if (clrfill == null)
                    clrfill = Menu.MenuColorEffect.rgbMediumGrey;
                if (clrtext == null)
                    clrtext = Menu.MenuColorEffect.rgbMediumGrey;

                OpTextBox component = new(option, pos, width)
                {
                    allowSpace = true,
                    description = option.info.description,
                    colorEdge = (Color)clr,
                    colorFill = (Color)clrfill,
                    colorText = (Color)clrtext
                };

                OpLabel label = new(pos.x + width + 18f, pos.y + 2f, option.info.Tags[0] as string)
                {
                    description = option.info.description
                };

                Tabs[curTab].AddItems(new UIelement[]
                {
                    component,
                    label
                });
            }
        }
    }
}