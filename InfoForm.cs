
using System.Drawing;

using System.Windows.Forms;

using System.Reflection;

namespace Verrollungsnachweis
{
    public partial class Form1 : Form
    {
       
        public static DialogResult InfoForm(string title, string resources)
        {

            Form form = new Form();
            form.Size= new Size(500, 300);
            /*
            PictureBox pictureBox = new PictureBox();
            form.Text = title;
            Assembly asm = System.Reflection.Assembly.GetExecutingAssembly();

           

            //System.Resources.ResourceManager rm = new System.Resources.ResourceManager("Verrollungsnachweis.Properties.Resources", asm);
            //pictureBox.Image  = (Bitmap) rm.GetObject(resources);
          
            pictureBox.Dock = System.Windows.Forms.DockStyle.Fill;
            pictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            form.Controls.Add(pictureBox);*/
            DialogResult dialogResult = form.ShowDialog();
            
            
            return dialogResult;
        }



    }
    
}
