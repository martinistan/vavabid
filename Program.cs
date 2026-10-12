// ============================================================================
// MARTINISTAN VAVABID ENTRY POINT
// Developed by Martinistan / Martiny (@martinistan)
// ============================================================================

using System;
using System.Windows.Forms;

namespace VakantieVeilingenBieder
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            System.IO.Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
