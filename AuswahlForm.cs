using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Verrollungsnachweis
{
    public class AuswahlForm : Form
    {
        private List<string> elemek;
        private int? kivalasztottIndex = null;

        public int? selectedIndex => kivalasztottIndex;

        public AuswahlForm(List<string> elemek)
        {
            this.elemek = elemek;
            InitUI();

            this.FormClosing += AuswahlForm_FormClosing;


        }

        private void InitUI()
        {
            
            this.Text = "Lastfall auswählen";
            this.Size = new Size(400, 500);

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 3,
                ColumnCount = 1,
                AutoSize = true
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50)); // Label
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // ListBox
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50)); // Button

            var label = new Label
            {
                Text = "Wenn vorhanden, bitte das Gegengewicht auswählen.\n(Wenn es nicht da, muss du nichts machen)",

                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font(FontFamily.GenericSansSerif, 11)
            };
            layout.Controls.Add(label, 0, 0);

            var listBox = new ListBox
            {
                Dock = DockStyle.Fill,
                SelectionMode = SelectionMode.One,
                IntegralHeight = false,
                Font = new Font(FontFamily.GenericSansSerif, 13)
            

        };
            listBox.Items.AddRange(elemek.ToArray());
            listBox.SelectedIndexChanged += (s, e) =>
            {
                if (listBox.SelectedIndex == kivalasztottIndex)
                {
                    listBox.ClearSelected();
                    kivalasztottIndex = null;
                }
                else
                {
                    kivalasztottIndex = listBox.SelectedIndex;
                }
            };
            layout.Controls.Add(listBox, 0, 1);

            var btnConfirm = new Button
            {
                Text = "Bestätigen",
                Dock = DockStyle.Fill,
                Height = 40,
                Font = new Font(FontFamily.GenericSansSerif, 11)

            };
            btnConfirm.Click += (s, e) =>
            {

                if (!kivalasztottIndex.HasValue)
                {
                    var result = MessageBox.Show(
                    "Du hast kein Gegengewicht ausgewählt. Ist das korrekt?",
                    "Keine Auswahl",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                    if (result == DialogResult.Yes)
                    {
                        this.DialogResult = DialogResult.OK;
                        this.Close();
                    }
                    else return;
                }
                else
                {
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
            };
            layout.Controls.Add(btnConfirm, 0, 2);

            var btnCancel = new Button
            {
                Text = "Abbrechen",
                Visible = false, // nem kell megjeleníteni, csak a CancelButton-hoz kell
            };
            
                this.Controls.Add(btnCancel);

            // Beállítások: Enter = OK, Esc vagy X = Cancel
            this.AcceptButton = btnConfirm;
            this.CancelButton = btnCancel;

            this.Controls.Add(layout);

        }

        private void AuswahlForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (this.DialogResult != DialogResult.OK)
            {
                kivalasztottIndex = null;
                var result = MessageBox.Show(
                "Klar, kein Gegengewicht vorhanden",
                "Bestätigung"
               );

            }
        }


    }

}

