using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MissionPlanner.Utilities;
using MissionPlanner.Controls;
using System.IO;
using System.Windows.Forms;
using System.Diagnostics;
using MissionPlanner;
using System.Drawing;
// Mission Planner Nudge plugin. Work in progress.
// adds a Nudge right click option in the planner.
// Use at your own risk. 
namespace Shortcuts
{
    public class Plugin : MissionPlanner.Plugin.Plugin
    {
        System.Windows.Forms.TextBox edInput;
        ToolStripMenuItem but;
        MissionPlanner.Controls.MyDataGridView commands;
        public override string Name
        {
            get { return "Waypoint Nudge Plugin"; }
        }

        public override string Version
        {
            get { return "0.10"; }
        }

        public override string Author
        {
            get { return "John Harlow"; }
        }

        public override bool Init()
        {
            return true;
        }

        public override bool Loaded()
        {
            but = new ToolStripMenuItem("Nudge");
            but.Click += but_Click;
            ToolStripItemCollection col = Host.FPMenuMap.Items;
            col.Insert(0, but);
            commands = Host.MainForm.FlightPlanner.Controls.Find("Commands", true).FirstOrDefault() as MissionPlanner.Controls.MyDataGridView;
            return true;
        }

        public override bool Loop()
        {
            return true;
        }

        public override bool Exit()
        {
            return true;
        }

        void but_Click(object sender, EventArgs e)
        {
            Form form;
            form = new Form();
            form.AutoScaleMode = AutoScaleMode.Font;
            SizeF dialogUnits;
            dialogUnits = form.AutoScaleDimensions;
            form.FormBorderStyle = FormBorderStyle.FixedDialog;
            form.MinimizeBox = false;
            form.MaximizeBox = false;
            form.Text = "Waypoint Nudger";
            form.ClientSize = new Size(180, 170);
            form.StartPosition = FormStartPosition.CenterScreen;
            System.Windows.Forms.Label lblPrompt;
            lblPrompt = new System.Windows.Forms.Label();
            lblPrompt.Parent = form;
            lblPrompt.AutoSize = true;
            lblPrompt.Left = 10;
            lblPrompt.Top = 20;
            lblPrompt.Text = "Nudge Dist (meters)";


            edInput = new System.Windows.Forms.TextBox();
            edInput.Parent = form;
            edInput.Left = lblPrompt.Left + 110;
            edInput.Top = lblPrompt.Top;
            edInput.Width = 30;
            edInput.Text = "0.5";
            edInput.SelectAll();

            Size buttonSize = new Size(50 * (int)dialogUnits.Width / 6, 14 * (int)dialogUnits.Height / 6);
            System.Windows.Forms.Button bbOk = new();
            bbOk.Parent = form;
            bbOk.Text = "Close";
            bbOk.DialogResult = DialogResult.OK;
            form.AcceptButton = bbOk;
            bbOk.Location = new Point(65, 130);
            bbOk.Size = buttonSize;


            Size moveButtonSize = new Size(20 * (int)dialogUnits.Width / 6, 14 * (int)dialogUnits.Height / 6);
            System.Windows.Forms.Button bbLeft = new System.Windows.Forms.Button();
            bbLeft.Parent = form;
            bbLeft.Text = char.ConvertFromUtf32(0x2190);
            bbLeft.Location = new Point(50, 60);
            bbLeft.Size = moveButtonSize;
            bbLeft.Click += moveClick;
            bbLeft.Tag = "L";

            System.Windows.Forms.Button bbRight = new System.Windows.Forms.Button();
            bbRight.Parent = form;
            bbRight.Text = char.ConvertFromUtf32(0x2192);
            bbRight.Location = new Point(110, 60);
            bbRight.Size = moveButtonSize;
            bbRight.Click += moveClick;
            bbRight.Tag = "R";

            System.Windows.Forms.Button bbUp = new System.Windows.Forms.Button();
            bbUp.Parent = form;
            bbUp.Text = char.ConvertFromUtf32(0x2191);
            bbUp.Location = new Point(80, 40);
            bbUp.Size = moveButtonSize;
            bbUp.Click += moveClick;
            bbUp.Tag = "U";

            System.Windows.Forms.Button bbDown = new System.Windows.Forms.Button();
            bbDown.Parent = form;
            bbDown.Text = char.ConvertFromUtf32(0x2193);
            bbDown.Location = new Point(80, 80);
            bbDown.Size = moveButtonSize;
            bbDown.Click += moveClick;
            bbDown.Tag = "D";
            form.ShowDialog();
        }

        void moveClick(object sender, EventArgs e)
        {

            double lat_trans = 0;
            double lon_trans = 0;
            double transVal = Double.Parse(edInput.Text);
            switch (((System.Windows.Forms.Button)sender).Tag)
            {
                case "L":
                    lon_trans = -transVal;
                    break;
                case "R":
                    lon_trans = transVal;
                    break;
                case "D":
                    lat_trans = -transVal;
                    break;
                case "U":
                    lat_trans = transVal;
                    break;

            }

            double earth_radius = 6378.137;
            double m_lat = (1 / ((2 * Math.PI / 360) * earth_radius)) / 1000;
            double m_long = (1 / ((2 * Math.PI / 360) * earth_radius)) / 1000; // # 1 meter in degree

            // iterate through the waypoints
            foreach (DataGridViewRow row in commands.Rows)
            {
                if (row.Cells[0].Value.ToString() == MAVLink.MAV_CMD.WAYPOINT.ToString())
                {
                    double lat = Double.Parse(row.Cells[5].Value.ToString());
                    double lon = Double.Parse(row.Cells[6].Value.ToString());
                    // Calculate top, which is lat_translation_meters above
                    double lat_new = lat + (lat_trans * m_lat);
                    // Calculate right, which is long_translation_meters right
                    double lon_new = lon + (lon_trans * m_long) / Math.Cos(lat * (Math.PI / 180));
                    row.Cells[5].Value = lat_new.ToString();
                    row.Cells[6].Value = lon_new.ToString();
                }
            }

            // redraw the map
            Host.MainForm.FlightPlanner.writeKML();
        }
    }
}
