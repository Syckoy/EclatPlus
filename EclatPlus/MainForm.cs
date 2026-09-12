namespace EclatPlus;

internal sealed class MainForm : Form
{
    private readonly AppSettings _settings;
    private readonly DriverVibrance _vibrance;
    private readonly TrackBar _eclat;
    private readonly Label _eclatValue;
    private readonly CheckBox _startup;
    private readonly CheckBox _limiterJeux;
    private readonly NotifyIcon _tray;
    private readonly Label _status;
    private readonly Icon _trayIcon;
    private bool _exit;

    public MainForm(DriverVibrance vibrance, bool startInTray)
    {
        _vibrance = vibrance;
        _settings = AppSettings.Load();
        _trayIcon = LogoArt.CreateTrayIcon();

        Text = "Yeshua";
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        MinimizeBox = true;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(520, 560);
        BackColor = Color.FromArgb(14, 12, 20);
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 10f);

        Controls.Add(new TitleBanner { Dock = DockStyle.Top, Height = 78 });

        var body = new Panel
        {
            Location = new Point(0, 78),
            Size = new Size(520, 482),
            BackColor = Color.FromArgb(14, 12, 20)
        };
        Controls.Add(body);

        body.Controls.Add(new Label
        {
            Text = "Éclat",
            Location = new Point(22, 16),
            Size = new Size(120, 24),
            ForeColor = Color.FromArgb(255, 236, 210),
            Font = new Font("Segoe UI Semibold", 12f)
        });

        _eclatValue = new Label
        {
            Location = new Point(400, 16),
            Size = new Size(90, 24),
            TextAlign = ContentAlignment.MiddleRight,
            ForeColor = Color.FromArgb(255, 204, 70),
            Font = new Font("Segoe UI Semibold", 12f)
        };
        body.Controls.Add(_eclatValue);

        int startValue = Math.Clamp(_settings.Eclat == 0 ? 50 : _settings.Eclat, 0, 200);
        _eclat = new TrackBar
        {
            Location = new Point(18, 44),
            Size = new Size(476, 45),
            Minimum = 0,
            Maximum = 200,
            TickFrequency = 10,
            Value = startValue,
            BackColor = Color.FromArgb(14, 12, 20)
        };
        _eclat.ValueChanged += (_, _) => OnChanged();
        body.Controls.Add(_eclat);

        body.Controls.Add(new Label
        {
            Text = "50 = naturel   ·   100 = max NVIDIA/AMD   ·   140–200 = plus d’éclat",
            Location = new Point(22, 88),
            Size = new Size(476, 24),
            ForeColor = Color.FromArgb(190, 180, 160)
        });

        body.Controls.Add(MakePreset("Naturel", 50, 22, 124));
        body.Controls.Add(MakePreset("Plus coloré", 90, 270, 124));
        body.Controls.Add(MakePreset("Éclat fort", 150, 22, 180));
        body.Controls.Add(MakePreset("Extrême", 190, 270, 180));

        _limiterJeux = new CheckBox
        {
            Text = "Limiter au pilote (Valorant / Apex / Fortnite)",
            Location = new Point(22, 242),
            AutoSize = true,
            ForeColor = Color.FromArgb(230, 220, 200),
            Checked = _settings.LimiterAuPilote
        };
        _limiterJeux.CheckedChanged += (_, _) =>
        {
            _settings.LimiterAuPilote = _limiterJeux.Checked;
            _settings.Save();
            ApplyCurrent();
        };
        body.Controls.Add(_limiterJeux);

        _startup = new CheckBox
        {
            Text = "Démarrer avec Windows",
            Location = new Point(22, 274),
            AutoSize = true,
            ForeColor = Color.FromArgb(230, 220, 200),
            Checked = _settings.DemarrerAvecWindows
        };
        _startup.CheckedChanged += (_, _) =>
        {
            _settings.DemarrerAvecWindows = _startup.Checked;
            Startup.SetEnabled(_startup.Checked);
            _settings.Save();
        };
        body.Controls.Add(_startup);

        var quit = new BrightButton
        {
            Text = "Quitter et restaurer",
            Location = new Point(22, 318),
            Size = new Size(476, 48)
        };
        quit.Click += (_, _) => ExitApp();
        body.Controls.Add(quit);

        _status = new Label
        {
            Location = new Point(22, 380),
            Size = new Size(476, 80),
            ForeColor = Color.FromArgb(140, 220, 160)
        };
        body.Controls.Add(_status);

        _tray = new NotifyIcon
        {
            Text = "Yeshua",
            Visible = true,
            Icon = _trayIcon
        };
        _tray.DoubleClick += (_, _) => ShowFromTray();

        var menu = new ContextMenuStrip();
        menu.Items.Add("Ouvrir", null, (_, _) => ShowFromTray());
        menu.Items.Add("Naturel", null, (_, _) => ApplyPreset(50));
        menu.Items.Add("Éclat fort", null, (_, _) => ApplyPreset(150));
        menu.Items.Add("Quitter", null, (_, _) => ExitApp());
        _tray.ContextMenuStrip = menu;

        FormClosing += OnFormClosing;
        FormClosed += (_, _) =>
        {
            _tray.Visible = false;
            _tray.Dispose();
            _trayIcon.Dispose();
        };

        Shown += async (_, _) =>
        {
            ApplyCurrent();
            if (startInTray)
            {
                HideToTray();
            }

            await CheckForUpdateOnLaunch();
        };

        UpdateLabels();
    }

    private async Task CheckForUpdateOnLaunch()
    {
        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(8));
            var update = await UpdateCheck.TryGetUpdateAsync(timeout.Token);
            if (update is null || IsDisposed)
            {
                return;
            }

            ShowUpdateAvailable(update);
        }
        catch
        {
            // Hors-ligne ou GitHub indisponible : on n’interrompt pas le lancement.
        }
    }

    private void ShowUpdateAvailable(UpdateInfo update)
    {
        var text =
            $"Une nouvelle version est disponible : {update.Remote}\n" +
            $"Tu as actuellement : {UpdateCheck.LocalVersion}\n\n" +
            "Tes réglages ne sont pas modifiés.\n" +
            "Ouvrir la page GitHub ?";

        if (!Visible)
        {
            _tray.ShowBalloonTip(4000, "Yeshua", $"Mise à jour {update.Remote} disponible.", ToolTipIcon.Info);
            return;
        }

        var answer = MessageBox.Show(
            this,
            text,
            "Yeshua — mise à jour",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Information);

        if (answer == DialogResult.Yes)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = update.Url,
                    UseShellExecute = true
                });
            }
            catch
            {
                // ignore
            }
        }
    }

    private BrightButton MakePreset(string name, int value, int x, int y)
    {
        var button = new BrightButton
        {
            Text = name,
            Location = new Point(x, y),
            Size = new Size(228, 48)
        };
        button.Click += (_, _) => ApplyPreset(value);
        return button;
    }

    private void ApplyPreset(int eclat)
    {
        if (_limiterJeux.Checked)
        {
            eclat = Math.Min(eclat, 100);
        }

        _eclat.Value = eclat;
    }

    private void OnChanged()
    {
        UpdateLabels();
        ApplyCurrent();
        _settings.Eclat = _eclat.Value;
        _settings.Save();
    }

    private void UpdateLabels() => _eclatValue.Text = $"{_eclat.Value}";

    private void ApplyCurrent()
    {
        int value = _eclat.Value;
        bool jeux = _limiterJeux.Checked;
        if (jeux)
        {
            value = Math.Min(value, 100);
        }

        int driverPercent = Math.Clamp(value, 0, 100);
        _vibrance.ApplyPercent(driverPercent);

        // Au-dessus de 50, on rebooste vraiment les couleurs (plus fort que le panneau GPU).
        float amount = 1f + Math.Max(0, value - 50) / 75f;
        bool useExtra = !jeux && value > 50;

        if (Magnification.Available)
        {
            Magnification.Apply(useExtra ? ColorScience.DigitalVibrance(amount) : ColorScience.Identity());
        }

        if (value <= 50)
        {
            _status.Text = "Couleurs naturelles.";
        }
        else if (jeux)
        {
            _status.Text = $"Éclat pilote {driverPercent}% — max officiel NVIDIA/AMD, pour les jeux.";
        }
        else
        {
            _status.Text = $"Éclat {value} — plus coloré que NVIDIA/AMD, orange reste orange.\nCoche « Limiter au pilote » avant Valorant / Apex / Fortnite.";
        }

        _status.ForeColor = Color.FromArgb(140, 220, 160);
        _tray.Text = $"Yeshua — {value}";
    }

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        if (!_exit && e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            HideToTray();
        }
    }

    private void ExitApp()
    {
        _exit = true;
        _tray.Visible = false;
        Close();
    }

    private void HideToTray()
    {
        ShowInTaskbar = false;
        Hide();
        _tray.ShowBalloonTip(1600, "Yeshua", "Toujours actif. Double-clic sur l’icône pour rouvrir.", ToolTipIcon.Info);
    }

    private void ShowFromTray()
    {
        ShowInTaskbar = true;
        Show();
        WindowState = FormWindowState.Normal;
        Activate();
    }
}
