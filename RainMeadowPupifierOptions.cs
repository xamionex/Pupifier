using Kittehface.Build;
using Menu.Remix.MixedUI;
using System;
using System.ComponentModel;
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

            public RainMeadowPupifierOptions()
            {
                SlugpupKey = config.Bind(nameof(SlugpupKey), KeyCode.P, new ConfigurableInfo("Key to toggle between slug and slugpup.", null, "", "KeyBind"));
                UseSlugpupStatsToggle = config.Bind(nameof(UseSlugpupStatsToggle), false, new ConfigurableInfo("If true, stats will be changed to a slugpup's equivalent.", null, "", "Toggle"));
            }

            public override void Initialize()
            {
                try
                {
                    base.Initialize();

                    Tabs = new OpTab[]
                    {
                    new OpTab(this, "Pupifier")
                    };

                    /**************** General ****************/
                    curTab = 0;
                    AddTitle();
                    float x = 90f;
                    float y = 460f;
                    float sepr = 40f;
                    AddKeyBinder(SlugpupKey, new Vector2(x, y));
                    AddCheckbox(UseSlugpupStatsToggle, new Vector2(x, y -= sepr));

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
                OpLabel title = new OpLabel(new Vector2(150f, 560f), new Vector2(300f, 30f), PluginInfo.PluginName, bigText: true);
                OpLabel version = new OpLabel(new Vector2(150f, 540f), new Vector2(300f, 30f), $"Version {PluginInfo.PluginVersion}");

                Tabs[curTab].AddItems(new UIelement[]
                {
                    title,
                    version
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

                OpCheckBox checkbox = new OpCheckBox(option, pos)
                {
                    description = option.info.description,
                    colorEdge = (Color)c
                };

                OpLabel label = new OpLabel(pos.x + 40f, pos.y + 2f, option.info.Tags[0] as string)
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

                OpKeyBinder keyBinder = new OpKeyBinder(option, pos, new Vector2(100f, 30f), false)
                {
                    description = option.info.description,
                    colorEdge = (Color)c
                };

                OpLabel label = new OpLabel(pos.x + 100f + 16f, pos.y + 5f, option.info.Tags[0] as string)
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
                OpComboBox box = new OpComboBox(option, pos, width, array)
                {
                    description = option.info.description
                };

                Vector2 offset = new Vector2();
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

                OpLabel label = new OpLabel(pos + offset, box.size, option.info.Tags[0] as string)
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
                OpTextBox component = new OpTextBox(option, pos, width)
                {
                    allowSpace = true,
                    description = option.info.description
                };

                OpLabel label = new OpLabel(pos.x + width + 18f, pos.y + 2f, option.info.Tags[0] as string)
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