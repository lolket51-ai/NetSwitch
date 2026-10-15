using System;
using System.Diagnostics;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Net.NetworkInformation;
using System.Linq;

namespace NetSwitch
{
    public partial class Form1 : Form
    {
        private int timeLeft = 0;
        private Keys hotKey = Keys.F9;
        //private Keys stopKey = Keys.F10;
        private bool isListeningForKey = false;

        private DateTime lastActionTime = DateTime.MinValue;
        private const int SPAM_DELAY_MS = 1000;

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const int START_ID = 9000;
        private const int STOP_ID = 9001;
        private const int WM_HOTKEY = 0x0312;

        // Логирование в твою новую консоль
        private async void Log(string message)
        {
            if (txtConsole.InvokeRequired)
            {
                txtConsole.Invoke(new Action(() => Log(message)));
                return;
            }

            string time = $"[{DateTime.Now:HH:mm:ss}] ";
            string fullText = time + message;

            // 1. Сначала создаем пустую строку, чтобы "прыгнуть" вниз
            txtConsole.AppendText(Environment.NewLine);

            // 2. Печатаем само сообщение по буквам
            foreach (char c in fullText)
            {
                txtConsole.AppendText(c.ToString());
                await System.Threading.Tasks.Task.Delay(20); // Чуть быстрее для приятного эффекта

                // Автоскролл
                txtConsole.SelectionStart = txtConsole.Text.Length;
                txtConsole.ScrollToCaret();
            }
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_HOTKEY)
            {
                int id = m.WParam.ToInt32();
                if (id == START_ID)
                {
                    if (timer1.Enabled) StopProcessLogic("Сброс (F9)");
                    else StartNetworkCycle();
                }
                if (id == STOP_ID) StopProcessLogic("Экстренно (F10)");
            }
            base.WndProc(ref m);
        }

        public Form1()
        {
            InitializeComponent();
            this.KeyPreview = true;
            RefreshAdapters();

            RegisterHotKey(this.Handle, START_ID, 0, (uint)hotKey);
            //RegisterHotKey(this.Handle, STOP_ID, 0, (uint)stopKey);

            Log("Система готова.");
        }

        private void RefreshAdapters()
        {
            guna2ComboBox1.Items.Clear();
            var adapters = NetworkInterface.GetAllNetworkInterfaces()
                .Where(nic => nic.NetworkInterfaceType == NetworkInterfaceType.Ethernet ||
                             nic.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)
                .Select(nic => nic.Name).ToList();

            if (adapters.Count > 0)
            {
                guna2ComboBox1.Items.AddRange(adapters.ToArray());
                guna2ComboBox1.SelectedIndex = 0;
            }
            else Log("Адаптеры не найдены!");
        }

        private string GetSelectedInterface() => guna2ComboBox1.SelectedItem?.ToString() ?? "Ethernet";

        private void StartNetworkCycle()
        {
            if ((DateTime.Now - lastActionTime).TotalMilliseconds < SPAM_DELAY_MS) return;

            lastActionTime = DateTime.Now;
            timeLeft = (int)guna2NumericUpDown1.Value;
            guna2CircleProgressBar1.Maximum = timeLeft;

            RunCommand($"interface set interface \"{GetSelectedInterface()}\" admin=disable");
            timer1.Start();

            guna2GradientButton1.Text = "STOP";
            Log($"OFF: {GetSelectedInterface()}");
        }

        private void StopProcessLogic(string reason = "Таймер")
        {
            timer1.Stop();
            RunCommand($"interface set interface \"{GetSelectedInterface()}\" admin=enable");

            timeLeft = 0;
            guna2CircleProgressBar1.Value = 0;
            guna2CircleProgressBar1.Text = "OK";
            guna2GradientButton1.Text = "СТАРТ";

            Log($"ON: {reason}");
            System.Media.SystemSounds.Beep.Play();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (timeLeft > 0)
            {
                timeLeft--;
                guna2CircleProgressBar1.Value = timeLeft;
                guna2CircleProgressBar1.Text = timeLeft.ToString();
            }
            else StopProcessLogic("Таймер");
        }

        private void RunCommand(string args)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo("netsh", args)
                {
                    UseShellExecute = true,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    Verb = "runas"
                };
                Process.Start(psi);
            }
            catch (Exception ex) { Log("Ошибка прав: " + ex.Message); }
        }

        private void guna2GradientButton1_Click(object sender, EventArgs e)
        {
            if (timer1.Enabled) StopProcessLogic("Кнопка");
            else StartNetworkCycle();
        }

        private void guna2Button1_Click(object sender, EventArgs e)
        {
            isListeningForKey = true;
            guna2Button1.Text = "???";
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (isListeningForKey)
            {
                UnregisterHotKey(this.Handle, START_ID);
                hotKey = e.KeyCode;
                guna2Button1.Text = "Клавиша: " + hotKey.ToString();
                RegisterHotKey(this.Handle, START_ID, 0, (uint)hotKey);
                isListeningForKey = false;
                Log($"Кнопка: {hotKey}");
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            UnregisterHotKey(this.Handle, START_ID);
            UnregisterHotKey(this.Handle, STOP_ID);
            base.OnFormClosing(e);
        }

        // Заглушки, чтобы не было ошибок при компиляции
        private void guna2CircleProgressBar1_ValueChanged(object sender, EventArgs e) { }
        private void guna2ComboBox1_SelectedIndexChanged(object sender, EventArgs e) { }
        private void guna2HtmlLabel1_Click(object sender, EventArgs e) { }
        private void guna2TextBox1_TextChanged(object sender, EventArgs e) { }
    }
}