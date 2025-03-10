using Microsoft.PowerToys.Settings.UI.Library;

namespace Community.PowerToys.Run.Plugin.HexInspector
{
    public class SettingsHelper
    {
        public bool SplitBinary;
        public Endian InputEndian;
        public Endian OutputEndian;
        
        public void UpdateSettings(PowerLauncherPluginSettings settings)
        {
            var _splitBinary = true;
            var _inputEndian = Endian.LittleEndian;
            var _outputEndian = Endian.BigEndian;

            if (settings != null && settings.AdditionalOptions != null)
            {
                var optionSplitBin = settings.AdditionalOptions.FirstOrDefault(x => x.Key == "SplitBinary");
                _splitBinary = optionSplitBin?.Value ?? SplitBinary;

                var optionInputEndian = settings.AdditionalOptions.FirstOrDefault(x => x.Key == "InputEndian");
                _inputEndian = (Endian)optionInputEndian.ComboBoxValue;

                var optionOutputEndian = settings.AdditionalOptions.FirstOrDefault(x => x.Key == "OutputEndian");
                _outputEndian = (Endian)optionOutputEndian.ComboBoxValue;
            }

            SplitBinary = _splitBinary;
            InputEndian = _inputEndian;
            OutputEndian = _outputEndian;
            return;
        }
    }
}