using NLog;
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Linq;





namespace Verrollungsnachweis
{
    public partial class Form1 : Form
    {
        public string _Case;
        private int _selectedEk;
        public int? selectedEK;
        
        HelperFunc Rstabfv =  HelperFunc.Instance; //only 1 instance of HelperFunc
        
       
        public Form1()
        {
            try
            {
                LogManager.Setup().LoadConfigurationFromFile("nlog.config");
                LoggerService.Info("Application started");
                LoggerService.UserActivity($"User {Environment.UserName} started the program on {Environment.MachineName} at {DateTime.Now}");
                InitializeComponent();
                Rstabfv.GetConnect();
                this.FormClosing += new FormClosingEventHandler(Form_FormClosing);
                Application.ApplicationExit += new EventHandler(OnApplicationExit);
                List<string> Ergebniskomb = Rstabfv.GetEKName();
                comboBox1.Items.AddRange(Ergebniskomb.ToArray());
            }
            catch (Exception e)
            {
                DialogResult result = MessageBox.Show($"Fehler bei Inizialisierung.Ich soll das Programlauf beenden\n{e.Message}", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
                LoggerService.Error(e, "Error during initialization");
                Rstabfv.TheEnd();
            }
        }

        private void comboBox1_SelectedIndexChanged_1(object sender, EventArgs e)
        {
            _Case = comboBox1.SelectedItem.ToString();
            if (!string.IsNullOrEmpty(_Case))
            {
                selectedEK = Rstabfv.AtItemEkToAtNoEK(comboBox1);
            }
            

        }
        private void InfoFahrwerk_Click(object sender, EventArgs e)
        {
            InfoForm("Diese Elemente soll ausgewählt werden:", "2022_11_06_20_51_56_Window");
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (selectedEK==null)
            {
                MessageBox.Show("Bitte wählen Sie einen Lastfall aus.", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            _selectedEk = (int)selectedEK;
            VerrollungsProzess(_selectedEk);

        }

        private void VerrollungsProzess(int Ek)
        {
            
            if (!Rstabfv.IsConnected())  return ;
            Rstabfv._case = _Case;
            if (!Rstabfv.Lastfaelle(_selectedEk)) {   return; };
            CheckConterweight();
            if (Rstabfv.SelectElement(Elem.linies))
            {
                Rstabfv.ShortOfMembers(Rstabfv.selected);
                Rstabfv.VerrollFindSupport(Rstabfv.supportParts);
                Rstabfv.Get_Forces();
                Rstabfv.Verrollung_MaxTKs(Rstabfv.tangentialSupport, _selectedEk);
                Rstabfv.CompareExcerRow();
                Export excel = new Export(Rstabfv);
                excel.ExportToXls();
                LoggerService.UserActivity("No Problem - " + DateTime.Now);
                Rstabfv.TheEnd();
                System.Environment.Exit(0);
            }

        }
        private void CheckConterweight()
        {
            var result = MessageBox.Show(
            "Gibt es das Gegengewicht schon unter den bestehenden Lastfällen? \n(Zur Überprüfung oder Auswahl bitte ‚Ja‘ drücken.)",
            "Frage",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);
            List<string> lista = new List<string>();
            if (result == DialogResult.No) return;
            
                lista.AddRange(Rstabfv.gq_lastenList
                    .Select(e => e.ToString()));
           
            using (var form = new AuswahlForm(lista))
            {
                var dialogResult = form.ShowDialog();

                if (dialogResult == DialogResult.OK)
                {
                    int? gegengewicht_localIndex = form.selectedIndex;
                    if (gegengewicht_localIndex.HasValue)
                    {
                        Rstabfv.GegengewichtIndex= 
                         Rstabfv.gq_lastenList [(int)gegengewicht_localIndex].Index;
                   
                    }
                }
            }
        }

        private void OnApplicationExit(object sender, EventArgs e)
        {
            Form_FormClosing(this, null);
        }

        private void Form_FormClosing(object sender, FormClosingEventArgs e)
        {
            LoggerService.Info("Form Closing");
            Rstabfv.TheEnd();
        }

    }
}
