using Draygo.API;

namespace avaness.GridSpawner.Settings
{
    public class SettingsHud
    {
        private readonly MapSettings config;
        private readonly HudAPIv2 hud;
        private HudAPIv2.MenuRootCategory category;
        private HudAPIv2.MenuTextInput menuBlockBuildTime;
        private HudAPIv2.MenuTextInput menuComponentCostModifier;
        private HudAPIv2.MenuTextInput menuMinBlocks;
        private HudAPIv2.MenuTextInput menuMaxBlocks;
        private HudAPIv2.MenuTextInput menuPowerModifier;
        private HudAPIv2.MenuItem menuSubgrids;

        public SettingsHud()
        {
            hud = new HudAPIv2(OnHudReady);
            config = IPSession.Instance.MapSettings;
        }

        public void Unload()
        {
            hud.Unload();
        }

        private void OnHudReady()
        {
            category = new HudAPIv2.MenuRootCategory("Instant Projector", HudAPIv2.MenuRootCategory.MenuFlag.AdminMenu, "Instant Projector");
            menuBlockBuildTime = new HudAPIv2.MenuTextInput("Block Build Time - " + config.BlockBuildTime, category, 
                "Enter build time per block in seconds.", OnBlockTimeSubmit);
            config.OnBlockBuildTimeChanged += Config_OnBlockBuildTimeChanged;
            menuComponentCostModifier = new HudAPIv2.MenuTextInput("Component Cost Modifier - " + config.ComponentCostModifier, category,
                "Enter the component cost modifier.", OnCompCostSubmit);
            config.OnComponentCostModifierChanged += Config_OnComponentCostModifierChanged;
            menuMinBlocks = new HudAPIv2.MenuTextInput("Min Blocks - " + config.MinBlocks, category,
                "Enter the minimum number of blocks allowed to build.", OnMinBlocksSubmit);
            config.OnMinBlocksChanged += Config_OnMinBlocksChanged;
            menuMaxBlocks = new HudAPIv2.MenuTextInput("Max Blocks - " + config.MaxBlocks, category,
                "Enter the maximum number of blocks allowed to build.", OnMaxBlocksSubmit);
            config.OnMaxBlocksChanged += Config_OnMaxBlocksChanged;
            menuPowerModifier = new HudAPIv2.MenuTextInput("Power Modifier - " + config.PowerModifier, category,
                "Enter the power modifier.", OnPowerSubmit);
            config.OnPowerModifierChanged += Config_OnPowerModifierChanged;
            menuSubgrids = new HudAPIv2.MenuItem("Subgrids - " + config.Subgrids, category, OnSubgridsClick);
            config.OnSubgridsChanged += Config_OnSubgridsChanged;
        }

        private void Config_OnPowerModifierChanged(float num)
        {
            menuPowerModifier.Text = "Power Modifier - " + num;
        }

        private void OnPowerSubmit(string s)
        {
            float num;
            if (float.TryParse(s, out num) && num >= 0 && !float.IsInfinity(num) && !float.IsNaN(num))
                config.PowerModifier = num;
        }

        private void OnSubgridsClick()
        {
            config.Subgrids = !config.Subgrids;
        }

        private void Config_OnSubgridsChanged(bool b)
        {
            menuSubgrids.Text = "Subgrids - " + b;
        }

        private void OnMaxBlocksSubmit(string s)
        {
            int num;
            if (int.TryParse(s, out num) && num > 0 && num > config.MinBlocks)
                config.MaxBlocks = num;
        }

        private void Config_OnMaxBlocksChanged(int num)
        {
            menuMaxBlocks.Text = "Max Blocks - " + num;
        }

        private void OnMinBlocksSubmit(string s)
        {
            int num;
            if (int.TryParse(s, out num) && num > 0 && num < config.MaxBlocks)
                config.MinBlocks = num;
        }

        private void Config_OnMinBlocksChanged(int num)
        {
            menuMinBlocks.Text = "Min Blocks - " + num;
        }

        private void OnCompCostSubmit(string s)
        {
            float num;
            if (float.TryParse(s, out num) && num >= 0 && !float.IsInfinity(num) && !float.IsNaN(num))
                config.ComponentCostModifier = num;
        }

        private void Config_OnComponentCostModifierChanged(float num)
        {
            menuComponentCostModifier.Text = "Component Cost Modifier - " + num;
        }

        private void OnBlockTimeSubmit(string s)
        {
            float num;
            if (float.TryParse(s, out num) && num >= 0 && !float.IsInfinity(num) && !float.IsNaN(num))
                config.BlockBuildTime = num;
        }

        private void Config_OnBlockBuildTimeChanged(float num)
        {
            menuBlockBuildTime.Text = "Block Build Time - " + num;
        }
    }
}
