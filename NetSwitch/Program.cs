using NetSwitch;
using System;
using System.Windows.Forms;

namespace InternetTimer // Должно совпадать с другими файлами
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1()); // Теперь он найдет Form1
        }
    }
}