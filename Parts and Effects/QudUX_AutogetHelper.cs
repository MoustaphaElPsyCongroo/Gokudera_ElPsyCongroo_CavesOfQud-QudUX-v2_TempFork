using System;
using XRL.Language;
using XRL.UI;
using QudUX_Constants = QudUX.Concepts.Constants;

namespace XRL.World.Parts
{
    [Serializable]
    public class QudUX_AutogetHelper : IPart
    {
        private static bool TemporarilyIgnoreQudUXSettings;
        private static NameValueBag _AutogetSettings;
        public static NameValueBag AutogetSettings
        {
            get
            {
                if (_AutogetSettings == null)
                {
                    _AutogetSettings = new NameValueBag(QudUX_Constants.AutogetDataFilePath);
                    _AutogetSettings.Load();
                }
                return _AutogetSettings;
            }
        }
        public static readonly string CmdDisableAutoget = "QudUX_DisableItemAutoget";
        public static readonly string CmdEnableAutoget = "QudUX_EnableItemAutoget";

        public static bool IsAutogetDisabledByQudUX(GameObject thing)
        {
            if (TemporarilyIgnoreQudUXSettings)
            {
                return false;
            }
            else if (thing.Understood() == false) //use default behavior if it hasn't been identified yet
            {
                return false;
            }
            return AutogetSettings.GetValue($"ShouldAutoget:{thing.Blueprint}", "").EqualsNoCase("No");
        }

        public bool ShouldAutoget(GameObject O)
        {
            if (!O.CanAutoget())
            {
                return false;
            }
            if (Options.AutogetSpecialItems && O.IsSpecialItem())
            {
                return true;
            }
            if (Options.AutogetArtifacts && O.GetPart("Examiner") is Examiner examiner)
            {
                return examiner.Complexity > 0;
            }
            string InventoryCategory = O.GetInventoryCategory();
            if (InventoryCategory == "Trade Goods")
            {
                return Options.AutogetTradeGoods;
            }
            if (InventoryCategory == "Food")
            {
                return Options.AutogetFood;
            }
            if (InventoryCategory == "Books")
            {
                return Options.AutogetBooks;
            }

            bool flag = false;
            double num = 0.0;
            if (Options.AutogetFreshWater && O.ContainsFreshWater())
            {
                if (!flag)
                {
                    num = O.GetWeight();
                    flag = true;
                }
                if (num <= 1.0)
                {
                    return true;
                }
            }
            if (Options.AutogetZeroWeight)
            {
                if (!flag)
                {
                    num = O.GetWeight();
                    flag = true;
                }
                if (num <= 0.0)
                {
                    return true;
                }
            }
            if (Options.AutogetNuggets && O.HasTagOrProperty("Nugget"))
            {
                return true;
            }
            if (Options.AutogetScrap && XRL.World.Tinkering.TinkeringHelpers.ConsiderScrap(O, ThePlayer))
            {
                return true;
            }
            return false;
        }

        public override bool WantEvent(int ID, int cascade)
        {
            return base.WantEvent(ID, cascade) || ID == OwnerGetInventoryActionsEvent.ID || ID == InventoryActionEvent.ID;
        }

        public override bool HandleEvent(OwnerGetInventoryActionsEvent E)
        {
            if (!QudUX.Concepts.Options.UI.EnableAutogetExclusions)
            {
                return base.HandleEvent(E);
            }
            bool wasDropped = E.Object.HasIntProperty("DroppedByPlayer");
            if (wasDropped)
            {
                //temporarily remove property so it doesn't affect ShouldAutoget() logic
                E.Object.RemoveIntProperty("DroppedByPlayer");
            }
            TemporarilyIgnoreQudUXSettings = true;
            // Replaced original ShouldAutoget method since it doesn't take
            // artifacts into account
            // bool isAutogetItem = E.Object.ShouldAutoget();
            bool isAutogetItem = ShouldAutoget(E.Object);

            TemporarilyIgnoreQudUXSettings = false;
            if (wasDropped)
            {
                E.Object.SetIntProperty("DroppedByPlayer", 1);
            }
            if (isAutogetItem && E.Object.Understood())
            {
                if (IsAutogetDisabledByQudUX(E.Object))
                {
                    E.AddAction(
                        "Re-enable auto-pickup for this item",
                        "re-enable auto-pickup",
                        CmdEnableAutoget,
                        FireOnActor: true
                    );
                }
                else
                {
                    E.AddAction("Disable auto-pickup for this item", "disable auto-pickup", CmdDisableAutoget, FireOnActor: true);
                }
            }
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(InventoryActionEvent E)
        {
            if (E.Command == CmdDisableAutoget)
            {
                bool bInfoboxShown = AutogetSettings.GetValue("Metadata:InfoboxWasShown", "").EqualsNoCase("Yes");
                if (!bInfoboxShown)
                {
                    DialogResult choice = DialogResult.Cancel;
                    while (choice != DialogResult.Yes && choice != DialogResult.No)
                    {
                        choice = Popup.ShowYesNo(
                            "Disabling auto-pickup for "
                                + Grammar.Pluralize(E.Item.DisplayNameOnly)
                                + ".\n\n"
                                + "Changes to auto-pickup preferences will apply to ALL of your characters. "
                                + "If you proceed, this message will not be shown again.\n\nProceed?",
                            false,
                            DialogResult.Cancel
                        );
                    }
                    if (choice == DialogResult.Yes)
                    {
                        AutogetSettings.SetValue("Metadata:InfoboxWasShown", "Yes", FlushToFile: false);
                        AutogetSettings.SetValue($"ShouldAutoget:{E.Item.Blueprint}", "No");
                    }
                }
                else
                {
                    AutogetSettings.SetValue($"ShouldAutoget:{E.Item.Blueprint}", "No");
                }
            }
            if (E.Command == CmdEnableAutoget)
            {
                AutogetSettings.Bag.Remove($"ShouldAutoget:{E.Item.Blueprint}");
                AutogetSettings.Flush();
            }
            return base.HandleEvent(E);
        }
    }
}
