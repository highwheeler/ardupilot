using System;
using System.Linq;
using System.Windows.Forms;
using MissionPlanner;
using MissionPlanner.Controls;
using MissionPlanner.Utilities;

namespace Shortcuts
{
    public class Plugin : MissionPlanner.Plugin.Plugin
    {
        ToolStripMenuItem reverserMenu;
        MyDataGridView commands;

        public override string Name => "Waypoint Reverser Plugin";
        public override string Version => "0.11";
        public override string Author => "John Harlow";

        public override bool Init() => true;

        public override bool Loaded()
        {
            reverserMenu = new ToolStripMenuItem("Reverser");
            reverserMenu.DropDownItems.Add("Add Reversing commands", null, AddReversingCommands);
            reverserMenu.DropDownItems.Add("Remove Reversing commands", null, RemoveReversingCommands);
            Host.FPMenuMap.Items.Insert(0, reverserMenu);

            commands = Host.MainForm.FlightPlanner.Controls.Find("Commands", true)
                .FirstOrDefault() as MyDataGridView;
            return true;
        }

        public override bool Loop() => true;
        public override bool Exit() => true;

        void AddReversingCommands(object sender, EventArgs e)
        {
            RemoveReversingCommands(sender, e);

            // Insert initial reverse command
            InsertReverseCommand(0, 0);

            DataGridViewRow prev2 = null, prev1 = null;
            bool reverseDir = true;

            for (int i = 1; i < commands.Rows.Count; i++)
            {
                var row = commands.Rows[i];
                if (IsWaypoint(row))
                {
                    if (prev2 != null && prev1 != null)
                    {
                        double ang = GetAngleBetween(prev2, prev1, row);
                        if (ang > 90)
                        {
                            InsertReverseCommand(i, reverseDir ? 1 : 0);
                            reverseDir = !reverseDir;
                            i++;
                        }
                    }
                    prev2 = prev1;
                    prev1 = row;
                }
            }
            InsertReverseCommand(commands.Rows.Count, 0);
            Host.MainForm.FlightPlanner.writeKML();
        }

        void RemoveReversingCommands(object sender, EventArgs e)
        {
            for (int i = 0; i < commands.Rows.Count; i++)
            {
                if (IsReverseCommand(commands.Rows[i]))
                {
                    commands.Rows.RemoveAt(i);
                    i--;
                }
            }
            Host.MainForm.FlightPlanner.writeKML();
        }

        bool IsWaypoint(DataGridViewRow row) =>
            row.Cells[0].Value?.ToString() == MAVLink.MAV_CMD.WAYPOINT.ToString();

        bool IsReverseCommand(DataGridViewRow row) =>
            row.Cells[0].Value?.ToString() == MAVLink.MAV_CMD.DO_SET_REVERSE.ToString();

        void InsertReverseCommand(int index, int value)
        {
            var cmd = new DataGridViewRow();
            cmd.CreateCells(commands);
            cmd.Cells[0].Value = MAVLink.MAV_CMD.DO_SET_REVERSE.ToString();
            cmd.Cells[1].Value = value;
            commands.Rows.Insert(index, cmd);
        }

        double GetAngleBetween(DataGridViewRow a, DataGridViewRow b, DataGridViewRow c)
        {
            double lat1 = double.Parse(a.Cells[5].Value.ToString());
            double lon1 = double.Parse(a.Cells[6].Value.ToString());
            var p1 = new PointLatLngAlt(lat1, lon1, 0, null);

            double lat2 = double.Parse(b.Cells[5].Value.ToString());
            double lon2 = double.Parse(b.Cells[6].Value.ToString());
            var p2 = new PointLatLngAlt(lat2, lon2, 0, null);

            double lat3 = double.Parse(c.Cells[5].Value.ToString());
            double lon3 = double.Parse(c.Cells[6].Value.ToString());
            var p3 = new PointLatLngAlt(lat3, lon3, 0, null);

            double bearing = p1.GetBearing(p2);
            return Math.Abs(p2.GetAngle(p3, bearing));
        }
    }
}
