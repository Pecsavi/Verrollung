// ============================================================================
// This project uses NLog (BSD-2-Clause License).
// Copyright (c) 2004-2023 Jaroslaw Kowalski et al.
// See: https://nlog-project.org/


// This project uses Newtonsoft.Json (MIT License).
// Copyright (c) James Newton-King.
// See: https://www.newtonsoft.com/json
// ============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Verrollungsnachweis
{
    static class Program
    {
        /// <summary>
        /// Der Haupteinstiegspunkt für die Anwendung.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}
