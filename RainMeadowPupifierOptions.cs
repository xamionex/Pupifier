using Menu.Remix.MixedUI;
using System;
using UnityEngine;

namespace RainMeadowPupifier
{
    public partial class RainMeadowPupifier
    {
        public partial class RainMeadowPupifierOptions : OptionInterface
        {
            public int curTab;
            public bool SlugpupEnabled = false;
            public readonly Configurable<KeyCode> SlugpupKey;
            public readonly Configurable<bool> UseSlugpupStatsToggle;
            public readonly Configurable<float> BodyWeightFac;
            public readonly Configurable<float> VisibilityBonus;
            public readonly Configurable<float> VisualStealthInSneakMode;
            public readonly Configurable<float> LoudnessFac;
            public readonly Configurable<float> LungsFac;
            public readonly Configurable<float> PoleClimbSpeedFac;
            public readonly Configurable<float> CorridorClimbSpeedFac;
            public readonly Configurable<float> RunSpeedFac;

            public RainMeadowPupifierOptions()
            {
                // Pupifier tab
                SlugpupKey = config.Bind(nameof(SlugpupKey), KeyCode.P, new ConfigurableInfo("Key to toggle between slug and slugpup.", null, "", "Keybind for toggling between pup mode"));

                // Stats tab
                UseSlugpupStatsToggle = config.Bind(nameof(UseSlugpupStatsToggle), true, new ConfigurableInfo("If true, stats will be changed to a slugpup's equivalent.", null, "", "Use Relative Slugpup Stats"));
                BodyWeightFac = config.Bind(nameof(BodyWeightFac), 0.65f, new ConfigurableInfo("Factor affecting body weight.", null, "", "Body Weight"));
                VisibilityBonus = config.Bind(nameof(VisibilityBonus), 0.8f, new ConfigurableInfo("Factor affecting visibility.", null, "", "Visibility Bonus"));
                VisualStealthInSneakMode = config.Bind(nameof(VisualStealthInSneakMode), 1.2f, new ConfigurableInfo("Factor affecting visual stealth when sneaking.", null, "", "Visual Stealth In Sneak Mode"));
                LoudnessFac = config.Bind(nameof(LoudnessFac), 0.5f, new ConfigurableInfo("Factor affecting loudness.", null, "", "Loudness"));
                LungsFac = config.Bind(nameof(LungsFac), 0.8f, new ConfigurableInfo("Factor affecting lung capacity.", null, "", "Lung Capacity"));
                PoleClimbSpeedFac = config.Bind(nameof(PoleClimbSpeedFac), 0.8f, new ConfigurableInfo("Factor affecting pole climb speed.", null, "", "Pole Climb Speed"));
                CorridorClimbSpeedFac = config.Bind(nameof(CorridorClimbSpeedFac), 0.8f, new ConfigurableInfo("Factor affecting corridor climb speed.", null, "", "Corridor Climb Speed"));
                RunSpeedFac = config.Bind(nameof(RunSpeedFac), 0.8f, new ConfigurableInfo("Factor affecting run speed.", null, "", "Run Speed"));
            }

            public override void Initialize()
            {
                try
                {
                    base.Initialize();

                    Tabs = new OpTab[] { new(this, "Pupifier"), new(this, "Stats") };

                    /**************** Pupifier ****************/
                    curTab = 0;
                    AddTitle();
                    // Center after title
                    float x = 150f;
                    float y = 540f;
                    float sepr = 40f;
                    AddKeyBinder(SlugpupKey, new Vector2(x, y -= sepr));

                    /**************** Stats ****************/
                    curTab++;
                    AddTitle();
                    x = 150f;
                    y = 500f;
                    sepr = 40f;
                    AddCheckbox(UseSlugpupStatsToggle, new Vector2(x, y -= sepr));
                    AddText("The following stats will multiply our current slugcat stats by the value here", new Vector2(x, y -= sepr));
                    AddText("(current slugcat stat * stat option)", new Vector2(x, y -= sepr));
                    AddFloatSlider(BodyWeightFac, new Vector2(x, y -= sepr), -2f, 2f, 250);
                    AddFloatSlider(VisibilityBonus, new Vector2(x, y -= sepr), -2f, 2f, 250);
                    AddFloatSlider(VisualStealthInSneakMode, new Vector2(x, y -= sepr), -2f, 2f, 250);
                    AddFloatSlider(LoudnessFac, new Vector2(x, y -= sepr), -2f, 2f, 250);
                    AddFloatSlider(LungsFac, new Vector2(x, y -= sepr), -2f, 2f, 250);
                    AddFloatSlider(PoleClimbSpeedFac, new Vector2(x, y -= sepr), -2f, 2f, 250);
                    AddFloatSlider(CorridorClimbSpeedFac, new Vector2(x, y -= sepr), -2f, 2f, 250);
                    AddFloatSlider(RunSpeedFac, new Vector2(x, y -= sepr), -2f, 2f, 250);

                    Log("Added all options...");
                }
                catch (Exception ex)
                {
                    LogError(ex, "Error opening RainMeadowPupifier Options Menu");
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

            private void AddText(string text, Vector2 pos)
            {
                OpLabel label = new(pos, new Vector2(300f, 30f), text, FLabelAlignment.Center);

                Tabs[curTab].AddItems(new UIelement[]
                {
                    label
                });
            }

            private void AddFloatSlider(Configurable<float> option, Vector2 pos, float min = 0, float max = 1, int width = 150)
            {
                OpFloatSlider slider = new(option, pos, width)
                {
                    description = option.info.description,
                    min = min,
                    max = max
                };

                OpLabel label = new(pos.x + width + 18f, pos.y + 2f, option.info.Tags[0] as string)
                {
                    description = option.info.description
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


            private void AddCheckbox(Configurable<bool> option, Vector2 pos, Color? c = null)
            {
                if (c == null)
                    c = Menu.MenuColorEffect.rgbMediumGrey;

                OpCheckBox checkbox = new(option, pos)
                {
                    description = option.info.description,
                    colorEdge = (Color)c
                };

                OpLabel label = new(pos.x + 40f, pos.y + 2f, option.info.Tags[0] as string)
                {
                    description = option.info.description,
                    color = (Color)c
                };

                Tabs[curTab].AddItems(new UIelement[]
                {
                    checkbox,
                    label
                });
            }


            private void AddKeyBinder(Configurable<KeyCode> option, Vector2 pos, Color? c = null)
            {
                if (c == null)
                    c = Menu.MenuColorEffect.rgbMediumGrey;

                OpKeyBinder keyBinder = new(option, pos, new Vector2(100f, 30f), false, OpKeyBinder.BindController.AnyController)
                {
                    description = option.info.description,
                    colorEdge = (Color)c
                };

                OpLabel label = new(pos.x + 100f + 16f, pos.y + 5f, option.info.Tags[0] as string)
                {
                    description = option.info.description,
                    color = (Color)c
                };

                Tabs[curTab].AddItems(new UIelement[]
                {
                    keyBinder,
                    label
                });
            }


            private void AddComboBox(Configurable<string> option, Vector2 pos, string[] array, float width = 80f, FLabelAlignment alH = FLabelAlignment.Center, OpLabel.LabelVAlignment alV = OpLabel.LabelVAlignment.Center)
            {
                OpComboBox box = new(option, pos, width, array)
                {
                    description = option.info.description
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


            private void AddTextBox<T>(Configurable<T> option, Vector2 pos, float width = 150f)
            {
                OpTextBox component = new(option, pos, width)
                {
                    allowSpace = true,
                    description = option.info.description
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