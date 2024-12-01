using System;
using System.IO;
using Ionic.Zip;
using System.Drawing;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net.Http;
using Microsoft.Web.WebView2.WinForms;

namespace Launcher
{
    public partial class Form1 : Form
    {
        private Button _btnLaunch;
        private ProgressBar _progressBar;
        private Label _labelStatus;
        private PictureBox _logo;
        private WebView2 _webBrowser;
        private const string RemoteVersionUrl = "https://patch.jibestudio.net/version";
        private readonly string _localVersionPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "version");
        private readonly string _testBatPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "test.bat");
        private readonly StreamWriter _writer = new StreamWriter("log.txt", true);
        

        public Form1()
        {

            try
            {
                InitializeComponent();
                this.Width = 800;
                this.Height = 600;
                this.Icon = Properties.Resources.icon2;
                SetBackgroundImage();
                InitializeLogo();
                InitializeWebBrowser();
                InitializeBtnLaunch();
                InitializeProgressBar();
                InitializeStatusLabel();
                Task.Run(CheckForUpdates); // Appeler la méthode de mise à jour
            } catch (Exception ex)
            {
                _writer.WriteLine(DateTime.Now.ToString(CultureInfo.InvariantCulture) + ex);
            }
        }

        private void InitializeStatusLabel()
        {
            _labelStatus = new Label();
            _labelStatus.Location = new Point(20, 480);
            _labelStatus.Size = new Size(400, 40);
            _labelStatus.ForeColor = Color.White;
            _labelStatus.BackColor = Color.Transparent;
            _labelStatus.Font = new Font(_labelStatus.Font.FontFamily, 10, FontStyle.Bold);
            this.Controls.Add(_labelStatus);
            _labelStatus.Text = @"Checking for updates...";
        }

        private void InitializeProgressBar()
        {
            _progressBar = new ProgressBar();
            _progressBar.Location = new Point(20, 500);
            _progressBar.Size = new Size(620, 30);
            this.Controls.Add(_progressBar);
        }

        private void InitializeBtnLaunch()
        {
            _btnLaunch = new Button();
            _btnLaunch.Location = new Point(650, 485);
            _btnLaunch.Text = @"PLAY";
            _btnLaunch.Size = new Size(110, 60);
            _btnLaunch.Font = new Font(_btnLaunch.Font.FontFamily, 18, FontStyle.Bold);
            _btnLaunch.FlatStyle = FlatStyle.Flat;
            _btnLaunch.FlatAppearance.BorderSize = 1;
            _btnLaunch.FlatAppearance.BorderColor = Color.Black;
            _btnLaunch.FlatAppearance.MouseOverBackColor = Color.LightGray;
            _btnLaunch.FlatAppearance.MouseDownBackColor = Color.Gray;
            _btnLaunch.Enabled = false;
            _btnLaunch.Click += btnLaunch_Click;

            this.Controls.Add(_btnLaunch);
        }

        private void SetBackgroundImage()
        {
            try
            {
                // Charger l'image de fond à partir du répertoire du projet
                this.BackgroundImage = Properties.Resources.background;

                // Optionnel : ajuster la disposition de l'image de fond
                this.BackgroundImageLayout = ImageLayout.Stretch; // Options: Tile, Center, Stretch, Zoom
            } catch (Exception ex)
            {
                _writer.WriteLine(DateTime.Now.ToString(CultureInfo.InvariantCulture) + ex);
                MessageBox.Show($@"Erreur : lors de la création du background", @"Erreur du processus", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void InitializeLogo()
        {
            try
            {
                _logo = new PictureBox
                {
                    Image = Properties.Resources.Rose_Logo,
                    SizeMode = PictureBoxSizeMode.StretchImage,
                    Size = new Size(160, 120),
                    Location = new Point((400-80), -10),
                    BackColor = Color.Transparent
                };

                this.Controls.Add(_logo);
            }
            catch (Exception ex)
            {
                _writer.WriteLine(DateTime.Now.ToString(CultureInfo.InvariantCulture) + ex);
                MessageBox.Show($@"Erreur : lors de la création du logo", @"Erreur du processus", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void InitializeWebBrowser()
        {
          try
          {
              _webBrowser = new WebView2();

              this.Controls.Add(_webBrowser);

              await _webBrowser.EnsureCoreWebView2Async(null);
              _webBrowser.Location = new Point(20, 100);
              _webBrowser.Size = new Size(740, 350);
              _webBrowser.CoreWebView2.Navigate("https://www.jibestudio.net/rose_online_patches_notes/");
              _webBrowser.BringToFront();
              _webBrowser.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = false;
              _webBrowser.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;

          } catch (Exception ex)
          {
              await _writer.WriteLineAsync(DateTime.Now.ToString(CultureInfo.InvariantCulture) + ex);
              MessageBox.Show($@"Erreur : lors de la création du browser : {ex.Message}", @"Erreur du processus", MessageBoxButtons.OK, MessageBoxIcon.Error);
          }
        }

 
        private void btnLaunch_Click(object sender, EventArgs e)
        {
            const string arguments = "@TRIGGER_SOFT@ _server 158.220.110.65";

            // Crée un nouvel objet ProcessStartInfo pour configurer le processus
            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "trose.exe", // Nom du jeu
                Arguments = arguments, // Arguments (@TRIGGER_SOFT@...)
                UseShellExecute = false, // Permet d'exécuter le fichier .bat avec le shell
                RedirectStandardOutput = true, // Pour lire la sortie du processus (si nécessaire)
                RedirectStandardError = true,  // Pour lire les erreurs du processus (si nécessaire)
                CreateNoWindow = true // Pour ne pas créer de fenêtre de console
            };

            try
            {
                using (System.Diagnostics.Process.Start(startInfo))
                {
                    UpdateStatusLabel("Launching game !");
                    Application.Exit();
                    
                }
            }
            catch (Exception ex)
            {
                // Afficher un message d'erreur ou enregistrer le message d'exception
                MessageBox.Show($@"Erreur lors du lancement : {ex.Message}", @"Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task CheckForUpdates()
        {
            string zipDownloadUrl;
            string zipFilePath;
            _progressBar.Value = 0;

            var progress = new Progress<int>(value =>
            {
                // Utiliser Invoke pour mettre à jour la ProgressBar sur le thread principal
                if (_progressBar.InvokeRequired)
                {
                    _progressBar.Invoke(new Action(() => _progressBar.Value = value));
                }
                else
                {
                    _progressBar.Value = value;
                }
            });

            try
            {
                var localVersion = File.ReadAllText(_localVersionPath).Trim();
                using (var client = new HttpClient())
                {
                    var remoteVersion = await client.GetStringAsync(RemoteVersionUrl);
                    remoteVersion = remoteVersion.Trim();
                    zipFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"update-{remoteVersion}.zip");
                    zipDownloadUrl = $"https://patch.jibestudio.net/update-{remoteVersion}.zip";

                    if (IsNewVersionAvailable(localVersion, remoteVersion))
                    {
                        try
                        {
                            await Task.Run(() => DownloadAndUnzip(zipDownloadUrl, zipFilePath, progress));
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($@"Erreur en téléchargeant : {ex.Message}");
                        }
                        finally
                        {
                            File.Delete(zipFilePath); // Supprime le fichier zip après utilisation
                        }
                    } else
                    {
                        UpdateStatusLabel("Your game is up to date !");
                        UpdateButtonStatus();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($@"An error occured while verifying version : {ex.Message}", @"Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }

        private async Task DownloadAndUnzip(string zipDownloadUrl, string zipFilePath, IProgress<int> progress)
        {
            // Télécharger le fichier zip
            try
            {
                await Task.Run(() => DownloadFileAsync(zipDownloadUrl, zipFilePath, progress));
            }
            catch (Exception ex)
            {
                MessageBox.Show($@"Erreur au lancement du téléchargement : {ex.Message}", @"Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            
            UpdateStatusLabel("Unzipping !");
            // Décompresser le fichier zip dans le dossier courant
            var extractPath = AppDomain.CurrentDomain.BaseDirectory;

            using (var zip = ZipFile.Read(zipFilePath))
            {
                var totalFiles = zip.Entries.Count;
                var extractedFiles = 0;
                foreach (var entry in zip.Entries)
                {
                    if (entry.FileName == "") continue;
                    entry.Extract(extractPath, ExtractExistingFileAction.OverwriteSilently);
                    extractedFiles++;
                    progress.Report(extractedFiles * 100 / totalFiles);
                }
                UpdateStatusLabel("Update completed, enjoy !");
                UpdateButtonStatus();
            }

            if(File.Exists(_testBatPath))
            {
                File.Delete(_testBatPath);
            }

            if (File.Exists(zipFilePath))
            {
                File.Delete(zipFilePath);
            }
            
            if (File.Exists(RemoteVersionUrl))
            {
                File.Copy(RemoteVersionUrl, _localVersionPath, true); // 'true' pour écraser l'ancien fichier version
            }
        }

        private static bool IsNewVersionAvailable(string localVersion, string remoteVersion)
        {
            var local = new Version(localVersion);
            var remote = new Version(remoteVersion);

            // Retourne true si la version distante est supérieure
            return remote.CompareTo(local) > 0;
        }

        private async Task DownloadFileAsync(string fileUrl, string destinationPath, IProgress<int> progress)
        {
            using (var client = new HttpClient())
            {
                using (var response = await client.GetAsync(fileUrl, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();
                    var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                    var downloadedBytes = 0L;
                    UpdateStatusLabel("Downloading...");

                    using (var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        var buffer = new byte[8192];
                        using (var contentStream = await response.Content.ReadAsStreamAsync())
                        {
                            int readBytes;
                            while ((readBytes = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                            {
                                try
                                {
                                    await fileStream.WriteAsync(buffer, 0, readBytes);
                                    downloadedBytes += readBytes;
                                }
                                catch (Exception ex)
                                {
                                    MessageBox.Show($@"Erreur en ecrivant : {ex.Message}", @"Erreur",
                                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                                }

                                if (totalBytes <= 0) continue;
                                var progressPercentage = (int)((downloadedBytes * 100) / totalBytes);
                                progress.Report(progressPercentage);
                            }
                        }
                    }
                }
            }
        }
        

        // Méthode pour mettre à jour le label de statut de manière thread-safe
        private void UpdateStatusLabel(string message)
        {
            if (_labelStatus.InvokeRequired)
            {
                _labelStatus.Invoke(new Action(() => _labelStatus.Text = message));
            }
            else
            {
                _labelStatus.Text = message;
            }
        }

        private void UpdateButtonStatus()
        {
            if (!_btnLaunch.InvokeRequired) return;
            _btnLaunch.Invoke(_btnLaunch.Enabled
                ? new Action(() => _btnLaunch.Enabled = false)
                : () => _btnLaunch.Enabled = true);
        }
    }
}
