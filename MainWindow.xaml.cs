using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace GPhysStore
{
    public static class GTLocator
    {
        private const string GorillaTagAppId = "1533390";
        private const string SteamRegKey32 = @"SOFTWARE\Valve\Steam";
        private const string SteamRegKey64 = @"SOFTWARE\Wow6432Node\Valve\Steam";
        public static string TryFindGorillaTag()
        {
            string steamPath = Registry.GetValue(Registry.LocalMachine.Name + "\\" + SteamRegKey64,
                                                  "InstallPath", null) as string
                              ?? Registry.GetValue(Registry.LocalMachine.Name + "\\" + SteamRegKey32,
                                                  "InstallPath", null) as string;

            if (!string.IsNullOrEmpty(steamPath))
            {
                string steamApps = Path.Combine(steamPath, "steamapps");
                string lf = Path.Combine(steamApps, "libraryfolders.vdf");
                if (File.Exists(lf))
                {
                    foreach (var line in File.ReadAllLines(lf))
                    {
                        var trimmed = line.Trim();
                        if (trimmed.StartsWith("\"") && trimmed.Contains("\"path\""))
                        {
                            int idx = trimmed.IndexOf('"', 1);
                            idx = trimmed.IndexOf('"', idx + 1);
                            idx = trimmed.IndexOf('"', idx + 1);
                            idx++;
                            int idx2 = trimmed.IndexOf('"', idx);
                            if (idx2 > idx)
                            {
                                string lib = trimmed.Substring(idx, idx2 - idx);
                                string candidate = Path.Combine(lib, "steamapps", "common", "Gorilla Tag");
                                if (Directory.Exists(candidate))
                                    return candidate;
                            }
                        }
                    }

                    string def = Path.Combine(steamApps, "common", "Gorilla Tag");
                    if (Directory.Exists(def))
                        return def;
                }
            }

            string oculusCandidate = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                                                   "Oculus", "Software", "Software", "another-axiom-gorilla-tag");
            if (Directory.Exists(oculusCandidate))
                return oculusCandidate;

            return null;
        }
    }

    public class AddonInfo : INotifyPropertyChanged
    {
        private bool _isInstalled;
        public event PropertyChangedEventHandler PropertyChanged;

        public string Author { get; set; }
        public string Description { get; set; }
        public string DownloadPath { get; set; }
        public string InstallButtonText => IsInstalled ? "Uninstall" : "Install";
        public bool IsInstalled
        {
            get => _isInstalled;
            set
            {
                _isInstalled = value;
                OnPropertyChanged(nameof(IsInstalled));
                OnPropertyChanged(nameof(InstallButtonText));
            }
        }

        public string Name { get; set; }
        public string Version { get; set; }
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    public partial class MainWindow : Window
    {
        public const string BundleVersionURL = "https://quantumleapstudios.org/gt_bundles/gphysversion.json";
        public static string path = "";
        private const string AddonsJsonUrl = "https://quantumleapstudios.org/gphys_serv/adns/addons.json.php";
        private const string BundleBase = "https://quantumleapstudios.org/gt_bundles/versions/";
        private const string ServerUrl = "https://quantumleapstudios.org/gt_bundles/download.php";
        private ObservableCollection<AddonInfo> availableAddons = new ObservableCollection<AddonInfo>();

        public MainWindow()
        {
            InitializeComponent();
            AddonsListView.ItemsSource = availableAddons;

            string gamePath = GTLocator.TryFindGorillaTag();
            if (gamePath == null)
            {
                StatusBlock.Text = "Could not auto-detect Gorilla Tag path — please locate Gorilla Tag.exe manually.";
                GamePathBlock.Text = "Game path: Not detected";

                var ofd = new Microsoft.Win32.OpenFileDialog();
                ofd.Filter = "Gorilla Tag Executable (Gorilla Tag.exe)|Gorilla Tag.exe";
                ofd.Title = "Locate Gorilla Tag Executable";
                var result = ofd.ShowDialog();
                if (result == true)
                {
                    path = System.IO.Path.GetDirectoryName(ofd.FileName);
                    StatusBlock.Text = "Gorilla Tag path set manually.";
                    GamePathBlock.Text = $"Game path: {path}";
                }
                else
                {
                    StatusBlock.Text = "Gorilla Tag path not set. Application will exit.";
                    Application.Current.Shutdown();
                    return;
                }
            }
            else
            {
                path = gamePath;
                StatusBlock.Text = "Ready to install GPhys Core.";
                GamePathBlock.Text = $"Game path: {path}";
            }

            LoadAddons();
        }

        private static async Task DownloadFileWithProgressAsync(string url, string destination, ProgressBar progressBar, TextBlock statusBlock)
        {
            var client = new HttpClient();
            using (var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
            {
                response.EnsureSuccessStatusCode();
                var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                progressBar.Minimum = 0;
                progressBar.Maximum = totalBytes;
                progressBar.Value = 0;
                progressBar.IsIndeterminate = totalBytes <= 0;

                using (var contentStream = await response.Content.ReadAsStreamAsync())
                using (var fileStream = new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true))
                {
                    var buffer = new byte[81920];
                    long totalRead = 0;
                    int read;
                    while ((read = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await fileStream.WriteAsync(buffer, 0, read);
                        totalRead += read;

                        if (totalBytes > 0)
                        {
                            progressBar.IsIndeterminate = false;
                            progressBar.Value = totalRead;
                            statusBlock.Text = $"Downloading bundle… {totalRead / 1024 / 1024:N1} MB / {totalBytes / 1024 / 1024:N1} MB";
                        }
                    }
                }
            }
        }

        private async void AddonActionBtn_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var addon = button.Tag as AddonInfo;

            button.IsEnabled = false;
            button.Content = addon.IsInstalled ? "Uninstalling..." : "Installing...";

            try
            {
                if (addon.IsInstalled)
                {
                    await UninstallAddon(addon);
                }
                else
                {
                    await InstallAddon(addon);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Addon Operation Failed",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                button.IsEnabled = true;
            }
        }

        private void AddonsTabBtn_Click(object sender, RoutedEventArgs e) // this also sucks!
        {
            MainContent.Visibility = Visibility.Collapsed;
            AddonsContent.Visibility = Visibility.Visible;
            MainTabBtn.Style = (Style)FindResource("TabButton");
            AddonsTabBtn.Style = (Style)FindResource("ActiveTabButton");
        }

        private async Task InstallAddon(AddonInfo addon)
        {
            string pluginsDir = Path.Combine(path, "BepInEx", "plugins", "GPhysPlugins");
            if (!Directory.Exists(pluginsDir))
                Directory.CreateDirectory(pluginsDir);

            string fileName = Path.GetFileName(new Uri(addon.DownloadPath).LocalPath);
            string destPath = Path.Combine(pluginsDir, fileName);

            using (var client = new HttpClient())
            {
                byte[] data = await client.GetByteArrayAsync(addon.DownloadPath);
                File.WriteAllBytes(destPath, data);
                addon.IsInstalled = true;
            }
        }

        // ik its shit, you dont gotta tell me
        private async void InstallBtn_Click(object sender, RoutedEventArgs e)
        {
            InstallBtn.IsEnabled = false;
            InstallProgress.Visibility = Visibility.Visible;
            InstallProgress.IsIndeterminate = true;
            StatusBlock.Text = "Checking key…";

            try
            {
                var key = KeyBox.Text.Trim();
                if (string.IsNullOrWhiteSpace(key))
                {
                    StatusBlock.Text = "Invalid key.";
                    InstallBtn.IsEnabled = true;
                    InstallProgress.Visibility = Visibility.Collapsed;
                    return;
                }

                StatusBlock.Text = "Contacting server…";
                var payload = "key=" + Uri.EscapeDataString(key);
                var data = System.Text.Encoding.UTF8.GetBytes(payload);

                var request = (HttpWebRequest)WebRequest.Create(ServerUrl);
                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded";
                request.ContentLength = data.Length;

                using (var stream = await request.GetRequestStreamAsync())
                    await stream.WriteAsync(data, 0, data.Length);

                using (var response = (HttpWebResponse)await request.GetResponseAsync())
                {
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        StatusBlock.Text = "Server rejected the request.";
                        return;
                    }

                    using (var ms = new MemoryStream())
                    {
                        await response.GetResponseStream().CopyToAsync(ms);
                        var dllData = ms.ToArray();

                        string versionHeader = response.GetResponseHeader("X-GPhys-Version");
                        if (string.IsNullOrWhiteSpace(versionHeader))
                        {
                            StatusBlock.Text = "Missing version.";
                            return;
                        }

                        StatusBlock.Text = "Version " + versionHeader + " detected. Downloading bundle…";

                        // wait 1 second to let user see the version
                        await Task.Delay(1000);

                        string ver = versionHeader.ToLower();
                        string bundleURL = BundleBase + ver + "/gphys";
                        string versionURL = BundleBase + ver + "/gphysversion.json";

                        string bepinexPath = Path.Combine(path, "BepInEx", "plugins", "GPhys");
                        string configPath = Path.Combine(path, "BepInEx", "config");

                        Directory.CreateDirectory(bepinexPath);
                        var installPath = Path.Combine(bepinexPath, "GPhys.dll");

                        using (var dllStream = new FileStream(installPath, FileMode.Create, FileAccess.Write, FileShare.None))
                            await dllStream.WriteAsync(dllData, 0, dllData.Length);

                        var bundlePath = Path.Combine(configPath, "gphysbundle.dat");
                        if (File.Exists(bundlePath)) File.Delete(bundlePath);
                        await DownloadFileWithProgressAsync(bundleURL, bundlePath, InstallProgress, StatusBlock);

                        var configData = await new HttpClient().GetByteArrayAsync(versionURL);
                        Directory.CreateDirectory(configPath);
                        var cfg = Path.Combine(configPath, "gphysversion.json");
                        if (File.Exists(cfg)) File.Delete(cfg);
                        using (var cs = new FileStream(cfg, FileMode.Create, FileAccess.Write, FileShare.None))
                            await cs.WriteAsync(configData, 0, configData.Length);

                        StatusBlock.Text = "Installation complete!";
                    }
                }
            }
            finally
            {
                InstallProgress.IsIndeterminate = false;
                InstallProgress.Visibility = Visibility.Collapsed;
                InstallBtn.IsEnabled = true;
            }
        }

        private async void LoadAddons()
        {
            try
            {
                AddonsStatusBlock.Text = "Loading addons...";
                RefreshAddonsBtn.IsEnabled = false;

                using (var client = new HttpClient())
                {
                    string json = await client.GetStringAsync(AddonsJsonUrl);

                    if (string.IsNullOrWhiteSpace(json) || json.Trim() == "[]")
                    {
                        AddonsStatusBlock.Text = "No addons available.";
                        return;
                    }

                    var addons = JsonConvert.DeserializeObject<AddonInfo[]>(json);

                    availableAddons.Clear();
                    string pluginsPath = Path.Combine(path, "BepInEx", "plugins", "GPhysPlugins");

                    if (!Directory.Exists(pluginsPath))
                        Directory.CreateDirectory(pluginsPath);

                    foreach (var addon in addons)
                    {
                        string fileName = Path.GetFileName(new Uri(addon.DownloadPath).LocalPath);
                        string addonPath = Path.Combine(pluginsPath, fileName);
                        addon.IsInstalled = File.Exists(addonPath);

                        availableAddons.Add(addon);
                    }

                    AddonsStatusBlock.Text = $"{addons.Length} addons available";
                }
            }
            catch (Exception ex)
            {
                AddonsStatusBlock.Text = $"Error loading addons: {ex.Message}";
            }
            finally
            {
                RefreshAddonsBtn.IsEnabled = true;
            }
        }

        private void MainTabBtn_Click(object sender, RoutedEventArgs e) // this sucks!
        {
            MainContent.Visibility = Visibility.Visible;
            AddonsContent.Visibility = Visibility.Collapsed;
            MainTabBtn.Style = (Style)FindResource("ActiveTabButton");
            AddonsTabBtn.Style = (Style)FindResource("TabButton");
        }
        private void RefreshAddonsBtn_Click(object sender, RoutedEventArgs e) => LoadAddons();
        private async Task UninstallAddon(AddonInfo addon)
        {
            await Task.Run(() =>
            {
                string pluginsDir = Path.Combine(path, "BepInEx", "plugins", "GPhysPlugins");
                string fileName = Path.GetFileName(new Uri(addon.DownloadPath).LocalPath);
                string filePath = Path.Combine(pluginsDir, fileName);

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    addon.IsInstalled = false;
                }
            });
        }
    }
}