using Menu.Remix.MixedUI;
using System;
using UnityEngine;

namespace Pupifier;

public partial class Pupifier
{
    public class PupifierOptions : OptionInterface
    {
        private int _curTab;
        public readonly Configurable<KeyCode> SlugpupKey;
        public readonly Configurable<bool> UseSecondaryKeyToggle;
        public readonly Configurable<KeyCode> SlugpupSecondaryKey;
        public readonly Configurable<bool> UseSlugpupStatsToggle;
        public readonly Configurable<bool> ModAutoDisabledToggle;
        public readonly Configurable<float> GlobalModifier;
        public readonly Configurable<float> SizeModifier;
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
        public readonly Configurable<float> ActionJumpPowerFac;
        public readonly Configurable<float> TailSize;
        public readonly Configurable<int> PupFoodPips;
        public readonly Configurable<int> PupHibernationFoodPips;
        public readonly Configurable<bool> DisableBeingGrabbed;
        public readonly Configurable<bool> UseBothHands;
        public readonly Configurable<bool> SpearmasterTwoHanded;
        public readonly Configurable<bool> ManualPupChange;
        public readonly Configurable<bool> LoggingPupEnabled;
        public readonly Configurable<bool> LoggingStatusEnabled;
        public readonly Configurable<bool> EnableInMeadowGamemode;
        public readonly Configurable<bool> EnableWhenSlugpupClass;
        public readonly Configurable<bool> ChangeFoodPips;
        public readonly Configurable<bool> ChangeFoodPipsPercentage;
        public readonly Configurable<bool> ChangeFoodPipsPercentageIgnoreDenominator;
        public readonly Configurable<bool> ChangeFoodPipsSubtraction;
        public readonly Configurable<bool> ChangeThrowingSkill;
        public readonly Configurable<int> throwingSkill;
        public readonly Configurable<bool> AddStaticDamage;
        public readonly Configurable<float> StaticDamage;

        public PupifierOptions()
        {
            // Pupifier tab
            SlugpupKey = config.Bind(nameof(SlugpupKey), KeyCode.Minus, new ConfigurableInfo("Key to toggle pup mode.", null, "", "Keybind for toggling between pup mode"));
            UseSecondaryKeyToggle = config.Bind(nameof(UseSecondaryKeyToggle), true, new ConfigurableInfo("If true, the secondary key will be used to toggle pup mode.", null, "", "Use Secondary Key to Toggle Pup Mode"));
            SlugpupSecondaryKey = config.Bind(nameof(SlugpupSecondaryKey), KeyCode.JoystickButton9, new ConfigurableInfo("Secondary Key to toggle pup mode, useful for controllers.", null, "", "Secondary Keybind for toggling between pup mode, useful for controllers"));

            // Stats tab
            UseSlugpupStatsToggle = config.Bind(nameof(UseSlugpupStatsToggle), true, new ConfigurableInfo("If true, stats will be changed to a slugpup's equivalent.", null, "", "Use Relative Slugpup Stats"));
            GlobalModifier = config.Bind(nameof(GlobalModifier), 1f, new ConfigurableInfo("Multiplies all stats by this value.", null, "", "Global Modifier"));
            BodyWeightFac = config.Bind(nameof(BodyWeightFac), 1f, new ConfigurableInfo("Factor affecting body weight. This influences body size a small amount", null, "", "Body Weight"));
            VisibilityBonus = config.Bind(nameof(VisibilityBonus), 1f, new ConfigurableInfo("Factor affecting visibility.", null, "", "Visibility"));
            VisualStealthInSneakMode = config.Bind(nameof(VisualStealthInSneakMode), 1f, new ConfigurableInfo("Factor affecting visual stealth when sneaking.", null, "", "Visual Stealth In Sneak Mode"));
            LoudnessFac = config.Bind(nameof(LoudnessFac), 1f, new ConfigurableInfo("Factor affecting how loud you are.", null, "", "Loudness"));
            LungsFac = config.Bind(nameof(LungsFac), 1f, new ConfigurableInfo("Factor affecting lung capacity.", null, "", "Lung Capacity"));
            PoleClimbSpeedFac = config.Bind(nameof(PoleClimbSpeedFac), 1f, new ConfigurableInfo("Factor affecting pole climb speed.", null, "", "Pole Climb Speed"));
            CorridorClimbSpeedFac = config.Bind(nameof(CorridorClimbSpeedFac), 1f, new ConfigurableInfo("Factor affecting corridor climb speed.", null, "", "Corridor Climb Speed"));
            RunSpeedFac = config.Bind(nameof(RunSpeedFac), 1f, new ConfigurableInfo("Factor affecting run speed.", null, "", "Run Speed"));
            JumpPowerFac = config.Bind(nameof(JumpPowerFac), 1f, new ConfigurableInfo("Factor affecting jump power.", null, "", "Jump Power"));
            WallJumpPowerFac = config.Bind(nameof(WallJumpPowerFac), 1f, new ConfigurableInfo("Factor affecting wall jump power. (Additive, i.e. you set 1.2 to be 20% better at wall jumping)", null, "", "Wall Jump Power Multiplier"));
            ActionJumpPowerFac = config.Bind(nameof(ActionJumpPowerFac), 1f, new ConfigurableInfo("Factor affecting jump power in actions like rolling, rocket jumping...", null, "", "Action Jump Power"));
            TailSize = config.Bind(nameof(TailSize), 0.75f, new ConfigurableInfo("Factor affecting how big your tail is when switching to a pup", null, "", "Tail Size"));
            
            // Damage tab
            ChangeThrowingSkill = config.Bind(nameof(ChangeThrowingSkill), false, new ConfigurableInfo("If true, damage will be modified.", null, "", "Change throwing skill (Damage)"));
            throwingSkill = config.Bind(nameof(throwingSkill), 1, new ConfigurableInfo("Factor affecting how much damage you deal", null, "", "Throwing Skill (Damage)"));
            AddStaticDamage = config.Bind(nameof(AddStaticDamage), false, new ConfigurableInfo("If true, will multiply your damage by this amount.", null, "", "Multiply Damage (recommended)"));
            StaticDamage = config.Bind(nameof(StaticDamage), 1f, new ConfigurableInfo("Factor affecting how much damage you deal", null, "", "Damage"));
            
            // FoodPips tab
            ChangeFoodPips = config.Bind(nameof(ChangeFoodPips), false, new ConfigurableInfo("If enabled, you will use the below set food pips requirement", null, "", "Enable food pips change"));
            ChangeFoodPipsPercentage = config.Bind(nameof(ChangeFoodPipsPercentage), true, new ConfigurableInfo("If enabled, this will make the mod multiply by the value you set, like a percentage", null, "", "Change with percentage of original"));
            ChangeFoodPipsPercentageIgnoreDenominator = config.Bind(nameof(ChangeFoodPipsPercentageIgnoreDenominator), false, new ConfigurableInfo("If enabled, this will make percentage ignore the denominator aka 1.6 -> 1 regardless of what the denominator is", null, "", "Percentage Ignore Denominator (Disable Rounding)"));
            ChangeFoodPipsSubtraction = config.Bind(nameof(ChangeFoodPipsSubtraction), false, new ConfigurableInfo("If enabled, this will make the mod subtract by the value you set", null, "", "Change with subtraction"));
            PupFoodPips = config.Bind(nameof(PupFoodPips), 6, new ConfigurableInfo("How many pips can you have (maximum)", null, "", "Maximum Food Pips"));
            PupHibernationFoodPips = config.Bind(nameof(PupHibernationFoodPips), 6, new ConfigurableInfo("How many pips should you need to sleep", null, "", "Food Pips Required"));
            
            // Toggles tab
            LoggingPupEnabled = config.Bind(nameof(LoggingPupEnabled), true, new ConfigurableInfo("If enabled, console will log pup changes", null, "", "Enable logging for pup change"));
            LoggingStatusEnabled = config.Bind(nameof(LoggingStatusEnabled), false, new ConfigurableInfo("If enabled, console will log stats upon changes", null, "", "Enable logging for stats when changing"));
            EnableInMeadowGamemode = config.Bind(nameof(EnableInMeadowGamemode), false, new ConfigurableInfo("If enabled, you will locally be a pup in the meadow gamemode, since you are blocked in the meadow gamemode from being a pup, it will only show locally, and for anyone else that has the mod", null, "", "Enable in MeadowGamemode (read desc)"));
            DisableBeingGrabbed = config.Bind(nameof(DisableBeingGrabbed), false, new ConfigurableInfo("If enabled, you can't be grabbed", null, "", "Disable being Grabbed"));
            UseBothHands = config.Bind(nameof(UseBothHands), false, new ConfigurableInfo("If enabled, you can use both hands as a pup", null, "", "Enable using both hands"));
            SpearmasterTwoHanded = config.Bind(nameof(SpearmasterTwoHanded), true, new ConfigurableInfo("If enabled, you can use both hands for spears as spearmaster", null, "", "Spearmaster can hold 2 spears"));

            // Experimental tab
            ModAutoDisabledToggle = config.Bind(nameof(ModAutoDisabledToggle), false, new ConfigurableInfo("If true, Pupifier will not disable itself when other mods are found. This requires a restart", null, "", "Allow Incompatible Mods (Requires Restart)"));
            ManualPupChange = config.Bind(nameof(ManualPupChange), false, new ConfigurableInfo("If enabled, the base game method for changing pup status won't be used and instead mine will, probably doesn't work well", null, "", "Manual Pup Change, required for body size"));
            SizeModifier = config.Bind(nameof(SizeModifier), 1f, new ConfigurableInfo("Factor affecting body size. (Only with manual pup toggle, probably doesn't work)", null, "", "Body Size"));
            EnableWhenSlugpupClass = config.Bind(nameof(EnableWhenSlugpupClass), false, new ConfigurableInfo("By default the mod doesn't modify slugpup (the class), like the one you can select in Meadow, this is for compatibility sake for some mods like DMS", null, "", "Enable modifying slugpup class (Read Desc)"));
        }

        public override void Initialize()
        {
            // ReSharper disable RedundantAssignment
            try
            {
                base.Initialize();

                // Colors
                Color softRed = new(0.7f, 0f, 0f);
                Color softGreen = new(0f, 0.7f, 0f);
                Color softYellow = new(0.7f, 0.7f, 0f);
                Color softBlue = new(0f, 0f, 0.7f);
                Color softMagenta = new(0.7f, 0f, 0.7f);
                Color softCyan = new(0f, 0.7f, 0.7f);
                Color softGray = new(0.7f, 0.7f, 0.7f);

                Tabs = new OpTab[] { new(this, "Pupifier"), new(this, "Stats"), new(this, "Damage"), new(this, "Food Pips"), new(this, "Toggles"), new(this, "Experimental") };

                /**************** Pupifier ****************/
                _curTab = 0;
                AddTitle();
                var x = 80f;
                var y = 540f;
                var sepr = 30f;

                if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("elumenix.pupify"))
                {
                    AddText("elumenix.pupify is installed, we do not support this mod. (Experimental Tab)", new Vector2(150f, y -= sepr));
                }
                if (IsModEnabled("henpemaz.rainmeadow"))
                {
                    AddText("henpemaz.rainmeadow is installed, note that they disabled pups in the meadow gamemode.", new Vector2(150f, y -= sepr));
                }

                AddKeyBinder(SlugpupKey, new Vector2(x, y -= sepr), softGreen, softGreen);
                AddCheckbox(UseSecondaryKeyToggle, new Vector2(x, y -= sepr), softYellow, softYellow);
                AddKeyBinder(SlugpupSecondaryKey, new Vector2(x, y -= sepr + 6f), softYellow, softYellow);

                /**************** Stats ****************/
                _curTab++;
                AddTitle();
                x = 150f;
                y = 540f;
                sepr = 30f;
                AddCheckbox(UseSlugpupStatsToggle, new Vector2(x + 50f, y -= sepr), softGreen, softGreen);
                AddText("The following stats will multiply our current slugcat stats by the value here", new Vector2(x, y -= sepr), softGray);
                AddText("(current slugcat stat * stat option)", new Vector2(x, y -= sepr), softGray);
                x = 25f;
                sepr = 30f;
                AddFloatSlider(GlobalModifier, new Vector2(x, y -= sepr), 0.01f, 10f, 400, softRed, softRed, softRed);
                AddFloatSlider(BodyWeightFac, new Vector2(x, y -= sepr), 0.01f, 5f, 400, softYellow, softYellow, softYellow);
                AddFloatSlider(VisibilityBonus, new Vector2(x, y -= sepr), 0.01f, 5f, 400, softGray, softGray, softGray);
                AddFloatSlider(VisualStealthInSneakMode, new Vector2(x, y -= sepr), 0.01f, 5f, 400, softGray, softGray, softGray);
                AddFloatSlider(LoudnessFac, new Vector2(x, y -= sepr), 0.01f, 5f, 400, softMagenta, softMagenta, softMagenta);
                AddFloatSlider(LungsFac, new Vector2(x, y -= sepr), 0.01f, 5f, 400, softCyan, softCyan, softCyan);
                AddFloatSlider(PoleClimbSpeedFac, new Vector2(x, y -= sepr), 0.01f, 5f, 400, softBlue, softBlue, softBlue);
                AddFloatSlider(CorridorClimbSpeedFac, new Vector2(x, y -= sepr), 0.01f, 5f, 400, softBlue, softBlue, softBlue);
                AddFloatSlider(RunSpeedFac, new Vector2(x, y -= sepr), 0.01f, 5f, 400, softBlue, softBlue, softBlue);
                AddFloatSlider(JumpPowerFac, new Vector2(x, y -= sepr), 0.01f, 5f, 400, softBlue, softBlue, softBlue);
                AddFloatSlider(WallJumpPowerFac, new Vector2(x, y -= sepr), 0.01f, 5f, 400, softBlue, softBlue, softBlue);
                AddFloatSlider(ActionJumpPowerFac, new Vector2(x, y -= sepr), 0.01f, 5f, 400, softBlue, softBlue, softBlue);
                AddFloatSlider(TailSize, new Vector2(x, y -= sepr), 0.01f, 5f, 400, softBlue, softBlue, softBlue);

                /**************** ThrowingSkill ****************/
                _curTab++;
                AddTitle();
                x = 150f;
                y = 540f;
                sepr = 30f;
                AddCheckbox(ChangeThrowingSkill, new Vector2(x, y -= sepr), softMagenta, softMagenta, softMagenta);
                AddText("0 = Weak throw, low damage, reduced speed", new Vector2(x, y -= sepr), softMagenta);
                AddText("1 = Standard throw, upward throw slightly nerfed (if enabled)", new Vector2(x, y -= sepr), softMagenta);
                AddText("2 = Strong throw, 1.25x damage, faster speed", new Vector2(x, y -= sepr), softMagenta);
                AddText("   Gourmand (if 2 or higher): 3x damage, special animations, momentum boost", new Vector2(x, y -= sepr), softMagenta);
                AddText("   Exhausted Gourmand: 0.3x damage", new Vector2(x, y -= sepr), softMagenta);
                x = 25f;
                AddSlider(throwingSkill, new Vector2(x, y -= sepr), 0, 2, 400, softMagenta, softMagenta, softMagenta);
                x = 150f;
                y -= sepr;
                AddCheckbox(AddStaticDamage, new Vector2(x, y -= sepr), softMagenta, softMagenta, softMagenta);
                x = 25f;
                AddFloatSlider(StaticDamage, new Vector2(x, y -= sepr), 0.01f, 5f, 400, softMagenta, softMagenta, softMagenta);

                /**************** FoodPips ****************/
                _curTab++;
                AddTitle();
                x = 150f;
                y = 540f;
                sepr = 30f;
                AddCheckbox(ChangeFoodPips, new Vector2(x, y -= sepr), softYellow, softYellow, softYellow);
                AddText("None: Food = X", new Vector2(x, y -= sepr), softYellow);
                AddText("Percentage: Food * (X * 0.1)", new Vector2(x, y -= sepr), softYellow);
                AddText("Subtraction: Food - X", new Vector2(x, y -= sepr), softYellow);
                AddCheckbox(ChangeFoodPipsPercentage, new Vector2(x, y -= sepr), softYellow, softYellow, softYellow);
                AddCheckbox(ChangeFoodPipsPercentageIgnoreDenominator, new Vector2(x, y -= sepr), softYellow, softYellow, softYellow);
                AddCheckbox(ChangeFoodPipsSubtraction, new Vector2(x, y -= sepr), softYellow, softYellow, softYellow);
                x = 25f;
                AddSlider(PupFoodPips, new Vector2(x, y -= sepr), 1, 20, 400, softYellow, softYellow, softYellow);
                AddSlider(PupHibernationFoodPips, new Vector2(x, y -= sepr), 1, 20, 400, softYellow, softYellow, softYellow);
                
                /**************** Toggles ****************/
                _curTab++;
                AddTitle();
                x = 150f;
                y = 540f;
                sepr = 30f;
                AddCheckbox(LoggingPupEnabled, new Vector2(x, y -= sepr), softGray, softGray, softGray);
                AddCheckbox(LoggingStatusEnabled, new Vector2(x, y -= sepr), softGray, softGray, softGray);
                AddCheckbox(EnableInMeadowGamemode, new Vector2(x, y -= sepr), softYellow, softYellow, softYellow);
                AddCheckbox(DisableBeingGrabbed, new Vector2(x, y -= sepr), softYellow, softYellow, softYellow);
                AddCheckbox(UseBothHands, new Vector2(x, y -= sepr), softYellow, softYellow, softYellow);
                AddCheckbox(SpearmasterTwoHanded, new Vector2(x, y -= sepr), softYellow, softYellow, softYellow);

                /**************** Experimental ****************/
                _curTab++;
                AddTitle();
                x = 150f;
                y = 540f;
                sepr = 30f;
                AddCheckbox(ModAutoDisabledToggle, new Vector2(x, y -= sepr), softRed, softRed, softRed);
                AddCheckbox(ManualPupChange, new Vector2(x, y -= sepr), softRed, softRed, softRed);
                AddFloatSlider(SizeModifier, new Vector2(20f, y -= sepr + 10f), 0.01f, 5f, 400, softRed, softRed, softRed);
                AddText("Note: Size modifier will change your hitbox, and not visually, if you want a good experience do +/- 0.30", new Vector2(x, y -= sepr), softRed);
                AddText("I kept this unrestricted because I know some of you want to screw around :p", new Vector2(x, y -= sepr), softRed);
                AddCheckbox(EnableWhenSlugpupClass, new Vector2(x, y -= sepr), softRed, softRed, softRed);

                Log("Added all options...");
                // ReSharper restore RedundantAssignment
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

            Tabs[_curTab].AddItems(title, version);
        }

        private void AddText(string text, Vector2 pos, Color? clr = null)
        {
            clr ??= Menu.MenuColorEffect.rgbMediumGrey;

            OpLabel label = new(pos, new Vector2(300f, 30f), text)
            {
                color = (Color)clr,
            };

            Tabs[_curTab].AddItems(label);
        }

        private void AddFloatSlider(Configurable<float> option, Vector2 pos, float min = 0, float max = 1, int width = 150, Color? clr = null, Color? clrline = null, Color? clrtext = null)
        {
            clr ??= Menu.MenuColorEffect.rgbMediumGrey;
            clrline ??= Menu.MenuColorEffect.rgbMediumGrey;
            clrtext ??= Menu.MenuColorEffect.rgbMediumGrey;

            OpFloatSlider slider = new(option, pos, width)
            {
                description = option.info.description,
                min = min,
                max = max,
                colorEdge = (Color)clr,
                colorLine = (Color)clrline
            };

            OpLabel label = new(pos.x + width + 15f, pos.y + 5f, option.info.Tags[0] as string)
            {
                description = option.info.description,
                color = (Color)clrtext
            };

            Tabs[_curTab].AddItems(slider, label);
        }

        private void AddSlider(Configurable<int> option, Vector2 pos, int min = 0, int max = 1, int width = 150, Color? clr = null, Color? clrline = null, Color? clrtext = null)
        {
            clr ??= Menu.MenuColorEffect.rgbMediumGrey;
            clrline ??= Menu.MenuColorEffect.rgbMediumGrey;
            clrtext ??= Menu.MenuColorEffect.rgbMediumGrey;

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

            Tabs[_curTab].AddItems(slider, label);
        }

        private void AddIcon(Vector2 pos, string iconName)
        {
            Tabs[_curTab].AddItems(new OpImage(pos, iconName));
        }
        
        private void AddCheckbox(Configurable<bool> option, Vector2 pos, Color? clr = null, Color? clrfill = null, Color? clrtext = null)
        {
            clr ??= Menu.MenuColorEffect.rgbMediumGrey;
            clrfill ??= Menu.MenuColorEffect.rgbMediumGrey;
            clrtext ??= Menu.MenuColorEffect.rgbMediumGrey;

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

            Tabs[_curTab].AddItems(checkbox, label);
        }


        private void AddKeyBinder(Configurable<KeyCode> option, Vector2 pos, Color? clr = null, Color? clrfill = null, Color? clrtext = null)
        {
            clr ??= Menu.MenuColorEffect.rgbMediumGrey;
            clrfill ??= Menu.MenuColorEffect.rgbMediumGrey;
            clrtext ??= Menu.MenuColorEffect.rgbMediumGrey;

            OpKeyBinder keyBinder = new(option, pos, new Vector2(100f, 30f), false)
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

            Tabs[_curTab].AddItems(keyBinder, label);
        }

        private void AddComboBox(Configurable<string> option, Vector2 pos, string[] array, float width = 80f, FLabelAlignment alH = FLabelAlignment.Center, OpLabel.LabelVAlignment alV = OpLabel.LabelVAlignment.Center, Color? clr = null, Color? clrfill = null)
        {
            clr ??= Menu.MenuColorEffect.rgbMediumGrey;
            clrfill ??= Menu.MenuColorEffect.rgbMediumGrey;

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
                description = option.info.description,
                alignment = alH,
                verticalAlignment = OpLabel.LabelVAlignment.Center
            };

            Tabs[_curTab].AddItems(box, label);
        }


        private void AddTextBox<T>(Configurable<T> option, Vector2 pos, float width = 150f, Color? clr = null, Color? clrfill = null, Color? clrtext = null)
        {
            clr ??= Menu.MenuColorEffect.rgbMediumGrey;
            clrfill ??= Menu.MenuColorEffect.rgbMediumGrey;
            clrtext ??= Menu.MenuColorEffect.rgbMediumGrey;

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

            Tabs[_curTab].AddItems(component, label);
        }
    }
}