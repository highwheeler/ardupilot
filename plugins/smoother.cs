using System;
using System.Linq;
using System.Windows.Forms;
using MissionPlanner;
using MissionPlanner.Controls;
using MissionPlanner.Utilities;
using System.Drawing;

namespace Shortcuts
{
    public class Plugin : MissionPlanner.Plugin.Plugin
    {
        ToolStripMenuItem smootherMenu;
        MyDataGridView commands;
        System.Windows.Forms.TextBox edDistance;
        System.Windows.Forms.TextBox edAngle;

        int count = 0;

        public override string Name => "Waypoint Smoother Plugin";
        public override string Version => "0.11";
        public override string Author => "John Harlow";

        public override bool Init() => true;

        public override bool Loaded()
        {

            smootherMenu = new ToolStripMenuItem("Smoother", null, SmootherClick);

            Host.FPMenuMap.Items.Insert(0, smootherMenu);

            commands = Host.MainForm.FlightPlanner.Controls.Find("Commands", true)
                .FirstOrDefault() as MyDataGridView;
            return true;
        }

        public override bool Loop() => true;
        public override bool Exit() => true;

        void SmootherClick(object sender, EventArgs e)
        {
            Form form = new Form
            {
                AutoScaleMode = AutoScaleMode.Font
            };
            SizeF dialogUnits;
            dialogUnits = form.AutoScaleDimensions;
            form.FormBorderStyle = FormBorderStyle.FixedDialog;
            form.MinimizeBox = false;
            form.MaximizeBox = false;
            form.Text = "Waypoint Smoother";
            form.ClientSize = new Size(180, 170);
            form.StartPosition = FormStartPosition.CenterScreen;
            System.Windows.Forms.Label lblDist = new System.Windows.Forms.Label
            {
                Parent = form,
                AutoSize = true,
                Left = 10,
                Top = 20,
                Text = "Trim dist (meters)"
            };
            edDistance = new System.Windows.Forms.TextBox
            {
                Parent = form,
                Left = lblDist.Left + 110,
                Top = lblDist.Top,
                Width = 30,
                Text = "3"
            };
            edDistance.SelectAll();

            System.Windows.Forms.Label lblAngle = new System.Windows.Forms.Label
            {
                Parent = form,
                AutoSize = true,
                Left = 10,
                Top = 40,
                Text = "Max Angle (degrees)"
            };
            edAngle = new System.Windows.Forms.TextBox
            {
                Parent = form,
                Left = lblAngle.Left + 110,
                Top = lblAngle.Top,
                Width = 30,
                Text = "60"
            };
            edAngle.SelectAll();

            Size buttonSize = new Size(50 * (int)dialogUnits.Width / 6, 14 * (int)dialogUnits.Height / 6);
            System.Windows.Forms.Button bbOk = new()
            {
                Parent = form,
                Text = "OK",
                DialogResult = DialogResult.OK,
                Location = new Point(15, 130),
                Size = buttonSize
            };
            form.AcceptButton = bbOk;
            bbOk.Click += DoSmoother;

            System.Windows.Forms.Button bbCancel = new()
            {
                Parent = form,
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Location = new Point(65, 130),
                Size = buttonSize
            };
            form.ShowDialog();
        }


        void DoSmoother(object sender, EventArgs e)
        {
            double dist = Double.Parse(edDistance.Text);

            while (SmoothWaypoints(dist) && count < 1000)
            {
                count++;
            }
        }
        bool SmoothWaypoints(double dist)
        {
            double angle = Double.Parse(edAngle.Text);

            DataGridViewRow prev2 = null, prev1 = null;

            for (int i = 0; i < commands.Rows.Count; i++)
            {
                var row = commands.Rows[i];
                if (IsWaypoint(row))
                {
                    if (prev2 != null && prev1 != null)
                    {
                        // if the angle between the previous two waypoints and the current one is greater than X degrees

                        if (GetAngleBetween(prev2, prev1, row) > angle)
                        {


                            double lat1 = double.Parse(prev2.Cells[5].Value.ToString());
                            double lon1 = double.Parse(prev2.Cells[6].Value.ToString());
                            double alt1 = double.Parse(prev2.Cells[7].Value.ToString());
                            var p1 = new PointLatLngAlt(lat1, lon1, alt1, null);

                            double lat2 = double.Parse(prev1.Cells[5].Value.ToString());
                            double lon2 = double.Parse(prev1.Cells[6].Value.ToString());
                            double alt2 = double.Parse(prev1.Cells[7].Value.ToString());
                            var p2 = new PointLatLngAlt(lat2, lon2, alt2, null);

                            double lat3 = double.Parse(row.Cells[5].Value.ToString());
                            double lon3 = double.Parse(row.Cells[6].Value.ToString());
                            double alt3 = double.Parse(row.Cells[7].Value.ToString());
                            var p3 = new PointLatLngAlt(lat3, lon3, alt3, null);

                            double length = p2.GetDistance(p1) / 2;
                            if (-length > dist)
                            {
                                length = -dist;
                            }
                            else if (length > dist)
                            {
                                length = dist;
                            }
                            double length2 = p2.GetDistance(p3) / 2;
                            if (-length2 > dist)
                            {
                                length2 = -dist;
                            }
                            else if (length2 > dist)
                            {
                                length2 = dist;
                            }
                            if (Math.Abs(length) >= dist && Math.Abs(length2) >= dist)
                            {
                                InsertWaypoint(i - 1, p2.newpos(p2.GetBearing(p1), length));
                                var p4 = p2.newpos(p2.GetBearing(p3), length2);
                                prev1.Cells[5].Value = p4.Lat;
                                prev1.Cells[6].Value = p4.Lng;
                                Host.MainForm.FlightPlanner.writeKML();
                                return true;
                            }
                        }
                    }
                    prev2 = prev1;
                    prev1 = row;
                }
            }
            return false;
        }

        bool IsWaypoint(DataGridViewRow row) =>
            row.Cells[0].Value?.ToString() == MAVLink.MAV_CMD.WAYPOINT.ToString();


        void InsertWaypoint(int index, PointLatLngAlt p)
        {
            var cmd = new DataGridViewRow();
            cmd.CreateCells(commands);
            cmd.Cells[0].Value = MAVLink.MAV_CMD.WAYPOINT;
            cmd.Cells[5].Value = p.Lat;
            cmd.Cells[6].Value = p.Lng;
            cmd.Cells[7].Value = p.Alt;
            commands.Rows.Insert(index, cmd);
        }

        double GetAngleBetween(DataGridViewRow a, DataGridViewRow b, DataGridViewRow c)
        {
            double lat1 = double.Parse(a.Cells[5].Value.ToString());
            double lon1 = double.Parse(a.Cells[6].Value.ToString());
            double alt1 = double.Parse(a.Cells[7].Value.ToString());
            var p1 = new PointLatLngAlt(lat1, lon1, alt1, null);

            double lat2 = double.Parse(b.Cells[5].Value.ToString());
            double lon2 = double.Parse(b.Cells[6].Value.ToString());
            double alt2 = double.Parse(b.Cells[7].Value.ToString());
            var p2 = new PointLatLngAlt(lat2, lon2, alt2, null);

            double lat3 = double.Parse(c.Cells[5].Value.ToString());
            double lon3 = double.Parse(c.Cells[6].Value.ToString());
            double alt3 = double.Parse(c.Cells[7].Value.ToString());
            var p3 = new PointLatLngAlt(lat3, lon3, alt3, null);

            double bearing = p1.GetBearing(p2);
            return Math.Abs(p2.GetAngle(p3, bearing));
        }
        double GetDistanceBetween(DataGridViewRow a, DataGridViewRow b)
        {
            double lat1 = double.Parse(a.Cells[5].Value.ToString());
            double lon1 = double.Parse(a.Cells[6].Value.ToString());
            double alt1 = double.Parse(a.Cells[7].Value.ToString());
            var p1 = new PointLatLngAlt(lat1, lon1, alt1, null);

            double lat2 = double.Parse(b.Cells[5].Value.ToString());
            double lon2 = double.Parse(b.Cells[6].Value.ToString());
            double alt2 = double.Parse(b.Cells[7].Value.ToString());
            var p2 = new PointLatLngAlt(lat2, lon2, alt2, null);

            return p1.GetDistance(p2);

        }


    }
}
