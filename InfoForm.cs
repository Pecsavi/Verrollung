
using System.Drawing;
using System.Reflection;
using System.Resources;
using System.Windows.Forms;

namespace Verrollungsnachweis
{
    public static class InfoForm
    {
        public static DialogResult InfoWindow(string title, string resources)
        {
            Form form = new Form();
            form.Size = new Size(500, 300);
            form.Text = title;

            PictureBox pictureBox = new PictureBox();
            Assembly asm = Assembly.GetExecutingAssembly();
            var rm = new ResourceManager("Verrollungsnachweis.Properties.Resources", asm);
            pictureBox.Image = (Bitmap)rm.GetObject(resources);

            pictureBox.Dock = DockStyle.Fill;
            pictureBox.SizeMode = PictureBoxSizeMode.Zoom;
            form.Controls.Add(pictureBox);

            return form.ShowDialog();
        }
    }


}
