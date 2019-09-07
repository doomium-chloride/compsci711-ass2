using System;
using System.Drawing;
using System.Windows.Forms;

namespace ass2
{
    public partial class Middleware : Form
    {
        private Button connect;
        private Button button;
        private ListBox listSent;
        private ListBox listReceived;
        private ListBox listReady;
        private int listWidth = 180;
        private int listHeight = 410;
        private Label labelSent;
        private Label labelReceived;
        private Label labelReady;

        public Middleware()
        {
            DisplayGUI();
        }

        int getX(int mult)
        {
            return 5 * (1 + mult) + (5 + listWidth) * mult;
        }

        public void DisplayGUI()
        {
            
            this.Name = "Middleware " + getID();
            this.Text = this.Name;
            this.Size = new Size(600, 600);
            this.StartPosition = FormStartPosition.CenterScreen;

            connect = new Button();
            connect.Name = "connect";
            connect.Text = connect.Name;
            connect.Size = new Size(100, 50);
            connect.Location = new Point(
                5,
                5);
            connect.Click += new System.EventHandler(this.Connect2Network);

            button = new Button();
            button.Name = "button";
            button.Text = "Send message";
            button.Size = new Size(100, 50);
            button.Location = new Point(
                5,
                5);
            button.Click += new System.EventHandler(this.MyButtonClick);

            listSent = new ListBox();
            listSent.Size = new System.Drawing.Size(listWidth, listHeight);
            listSent.Location = new System.Drawing.Point(getX(0), 100);

            listReceived = new ListBox();
            listReceived.Size = new System.Drawing.Size(listWidth, listHeight);
            listReceived.Location = new System.Drawing.Point(getX(1), 100);

            listReady = new ListBox();
            listReady.Size = new System.Drawing.Size(listWidth, listHeight);
            listReady.Location = new System.Drawing.Point(getX(2), 100);

            labelSent = new Label();
            labelSent.Text = "Sent";
            labelSent.Name = "Label Sent";
            labelSent.Location = new System.Drawing.Point(getX(0), 85);

            labelReceived = new Label();
            labelReceived.Text = "Received";
            labelReceived.Name = "Label Received";
            labelReceived.Location = new System.Drawing.Point(getX(1), 85);

            labelReady = new Label();
            labelReady.Text = "Ready";
            labelReady.Name = "Label Ready";
            labelReady.Location = new System.Drawing.Point(getX(2), 85);

            this.Controls.Add(connect);
            this.Controls.Add(listSent);
            this.Controls.Add(listReceived);
            this.Controls.Add(listReady);
            this.Controls.Add(labelSent);
            this.Controls.Add(labelReceived);
            this.Controls.Add(labelReady);

        }

        private void MyButtonClick(object source, EventArgs e)
        {
            //MessageBox.Show("My First WinForm Application");
            sendMessage();
        }

        private void Connect2Network(object source, EventArgs e)
        {
            DoWork();
            this.Controls.Remove(connect);
            this.Controls.Add(button);
        }

        public static int Main(String[] args)
        {
            Middleware m = new Middleware();
            Application.Run(m);
            //m.DoWork();
            return 0;
        }
    }
}