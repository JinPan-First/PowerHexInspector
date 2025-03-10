using Wox.Plugin;
using Wox.Plugin.Logger;
using Microsoft.PowerToys.Settings.UI.Library;
using System.Windows.Controls;
using ManagedCommon;

namespace Community.PowerToys.Run.Plugin.HexInspector
{
    public class Main : IPlugin, IDisposable, ISettingProvider
    {
        public string Name => "HexInspector";
        public string Description => "A simple powertoys run plugin provides fast and easy way to peek other forms of an input value";
        public static string PluginID => "JSAKDJKALSJDIWDI1872Hdhad139319A";

        private string IconPath { get; set; }
        private PluginInitContext Context { get; set; }
        private bool _disposed;
        private readonly SettingsHelper settings;
        private readonly Convert converter;

        public Main()
        {
            settings = new SettingsHelper();
            converter = new Convert(settings);
        }

        private List<Result> ProduceResults(Query query)
        {
            var results = new List<Result>();
            var conversions = new List<(ConvertResult, Base)>();
            bool isKeywordSearch = !string.IsNullOrEmpty(query.ActionKeyword);
            bool isEmptySearch = string.IsNullOrEmpty(query.Search);

            if (isEmptySearch && isKeywordSearch)
            {
                results.Add
                (
                    new Result
                    {
                        Title = $"Usage 1: {query.ActionKeyword} [value]",
                        SubTitle = "[value]: A C-style value, e.g. 65, 0x41, 0b01000001, 0101, \"A\"",
                        IcoPath = IconPath,
                        Action = (e) => true
                    }
                );
                return results;
            }

            QueryInterpretHelper.QueryInterpret(query, out Base queryBase, out string queryValue, out bool isUpper);
            (bool vaild, List<Result> checkRes) = CheckInput(queryBase, queryValue);
            if (!vaild)
            {
                return checkRes;
            }

            converter.is_upper = true;
            conversions.Add((converter.UniversalConvert(queryValue, queryBase, Base.Oct), Base.Oct));
            conversions.Add((converter.UniversalConvert(queryValue, queryBase, Base.Dec), Base.Dec));
            conversions.Add((converter.UniversalConvert(queryValue, queryBase, Base.Hex), Base.Hex));
            conversions.Add((converter.UniversalConvert(queryValue, queryBase, Base.Bin), Base.Bin));
            conversions.Add((converter.UniversalConvert(queryValue, queryBase, Base.Ascii), Base.Ascii));

            // Create result list
            foreach ((ConvertResult res, Base type) in conversions)
            {
                results.Add
                (
                    new Result
                    {
                        Title = res.Formated,
                        SubTitle = $"{type.ToString().ToUpper()} "
                                 + $"{(type == Base.Bin || type == Base.Hex || type == Base.Ascii ? $" ({settings.OutputEndian})" : "")}",
                        IcoPath = IconPath,
                        Action = (e) =>
                        {
                            UtilsFunc.SetClipboardText(res.Raw);
                            return true;
                        }
                    }
                );
            }
            return results;
        }

        private static (bool vaild, List<Result> checkRes) CheckInput(Base queryBase, string queryValue)
        {
            if (queryBase == Base.Invalid || (queryBase == Base.Ascii && queryValue.Length == 0))
            {
                return (vaild: false, checkRes: []);
            }

            return (vaild: true, checkRes: null);
        }

        public List<Result> Query(Query query)
        {
            var results = new List<Result>();

            try
            {
                results = ProduceResults(query);
            }
            catch (Exception e)
            {
                Log.Info($"Unhandled Exception: {e.Message} {e.StackTrace}", typeof(Main));
                // Return Error message
                return [
                    new Result
                    {
                        Title = "Unhandled Exception",
                        SubTitle = @"Check log(%LOCALAPPDATA%\Microsoft\PowerToys\PowerToys Run\Logs) for more information",
                        IcoPath = IconPath,
                        Action = (e) => 
                        {
                            UtilsFunc.SetClipboardText(@"%LOCALAPPDATA%\Microsoft\PowerToys\PowerToys Run\Logs");
                            return true;
                        }
                    }
                ];
            }

            return results;
        }

        public void Init(PluginInitContext context)
        {
            Log.Info("HexInspector plugin is initializeing", typeof(Main));
            Context = context ?? throw new ArgumentNullException(paramName: nameof(context));

            Context.API.ThemeChanged += OnThemeChanged;
            UpdateIconPath(Context.API.GetCurrentTheme());
            Log.Info("HexInspector plugin is initialized", typeof(Main));
        }

        public Control CreateSettingPanel()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<PluginAdditionalOption> AdditionalOptions { get; } = new List<PluginAdditionalOption>()
        {
            new() {
                Key = "SplitBinary",
                DisplayLabel = "Split Binary",
                DisplayDescription = "Split binary into 4-bit groups",
                Value = true,
            },
            new() {
                Key = "InputEndian",
                DisplayLabel = "Input Endian",
                DisplayDescription = "Little or Big Endian setting for input, only for binary and hexadecimal",
                PluginOptionType = PluginAdditionalOption.AdditionalOptionType.Combobox,
                ComboBoxValue = 0,
                ComboBoxItems =
                [
                    new KeyValuePair<string, string>("Little Endian", "0"),
                    new KeyValuePair<string, string>("Big Endian", "1"),
                ]
            },
            new() {
                Key = "OutputEndian",
                DisplayLabel = "Output Endian",
                DisplayDescription = "Little or Big Endian setting for output, only for binary and hexadecimal",
                PluginOptionType = PluginAdditionalOption.AdditionalOptionType.Combobox,
                ComboBoxValue = (int)Endian.LittleEndian,
                ComboBoxItems =
                [
                    new KeyValuePair<string, string>("Little Endian", "0"),
                    new KeyValuePair<string, string>("Big Endian", "1"),
                ]
            }
        };

        public void UpdateSettings(PowerLauncherPluginSettings settings)
        {
            this.settings.UpdateSettings(settings);
            return;
        }

        private void OnThemeChanged(Theme currentTheme, Theme newTheme)
        {
            UpdateIconPath(newTheme);
        }

        private void UpdateIconPath(Theme theme)
        {
            if (theme == Theme.Light || theme == Theme.HighContrastWhite)
            {
                IconPath = "Images/HexInspector.light.png";
            }
            else
            {
                IconPath = "Images/HexInspector.dark.png";
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    if (Context != null && Context.API != null)
                    {
                        Context.API.ThemeChanged -= OnThemeChanged;
                    }

                    _disposed = true;
                }
            }
        }
    }
}