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
// Mission Planner reverser plugin. Work in progress.
// adds a Reverse right click option in the planner.
// Use at your own risk. 
// 6/24/2025 J Harlow
namespace Shortcuts
{
    public class Plugin : MissionPlanner.Plugin.Plugin
    {
        System.Windows.Forms.TextBox edInput;
        ToolStripMenuItem but;
        MissionPlanner.Controls.MyDataGridView commands;
        public override string Name
        {
            get { return "Waypoint Reverser Plugin"; }
        }

        public override string Version
        {
            get { return "0.11"; }
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
            but = new ToolStripMenuItem("Reverser");
            ToolStripMenuItem but2 = new ToolStripMenuItem("Add Reversing commands");
            but2.Click += add_Click;
            but.DropDownItems.Add(but2);
            ToolStripMenuItem but3 = new ToolStripMenuItem("Remove Reversing commands");
            but3.Click += remove_Click;
            but.DropDownItems.Add(but3);
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

        void add_Click(object sender, EventArgs e)
        {
            DataGridViewRow lastWpt2 = null;
            DataGridViewRow lastWpt1 = null;

            bool lastDir = true;
            int num = 0;
            // remove existing entries first
            remove_Click(sender,e);
            // insert a do_set_reverse as first command
            DataGridViewRow startCmd = new DataGridViewRow();
            startCmd.CreateCells(commands);
            startCmd.Cells[0].Value = MAVLink.MAV_CMD.DO_SET_REVERSE.ToString();
            startCmd.Cells[1].Value = 0;
            commands.Rows.Insert(0, startCmd);
            // iterate through the waypoints
            for (int i = 1; i < commands.Rows.Count; i++)
            {
                DataGridViewRow row = commands.Rows[i];
                // if this is a waypoint
                if (row.Cells[0].Value.ToString() == MAVLink.MAV_CMD.WAYPOINT.ToString())
                {
                    if (lastWpt2 != null && lastWpt1 != null)  // and we have previous 2 waypoints
                    {
                        //if the heading angle to this waypoint is acute, insert a reverse.

                        double lat1 = Double.Parse(lastWpt2.Cells[5].Value.ToString());
                        double lon1 = Double.Parse(lastWpt2.Cells[6].Value.ToString());
                        PointLatLngAlt p1 = new PointLatLngAlt(lat1, lon1, 0, null);

                        double lat2 = Double.Parse(lastWpt1.Cells[5].Value.ToString());
                        double lon2 = Double.Parse(lastWpt1.Cells[6].Value.ToString());
                        PointLatLngAlt p2 = new PointLatLngAlt(lat2, lon2, 0, null);

                        double bearing = p1.GetBearing(p2);
                        double lat3 = Double.Parse(row.Cells[5].Value.ToString());
                        double lon3 = Double.Parse(row.Cells[6].Value.ToString());
                        PointLatLngAlt p3 = new PointLatLngAlt(lat3, lon3, 0, null);

                        double ang = Math.Abs(p2.GetAngle(p3, bearing));
                        if (ang > 90) // if the abs val of the angle to the next WP is > 90 then it is acute
                        {

                            DataGridViewRow doReverse = new DataGridViewRow();
                            doReverse.CreateCells(commands);

                            doReverse.Cells[0].Value = MAVLink.MAV_CMD.DO_SET_REVERSE.ToString();
                            doReverse.Cells[1].Value = (lastDir ? 1 : 0);
                            // alternate directions
                            lastDir = !lastDir;
                            commands.Rows.Insert(i, doReverse);
                            i++;
                        }

                    }
                    lastWpt2 = lastWpt1;
                    lastWpt1 = row;
                }
            }
            // make sure last cmd is do_set_reverse =0
            DataGridViewRow endCmd = new DataGridViewRow();
            endCmd.CreateCells(commands);
            endCmd.Cells[0].Value = MAVLink.MAV_CMD.DO_SET_REVERSE.ToString();
            endCmd.Cells[1].Value = 0;
            commands.Rows.Add(endCmd);
            // redraw the map
            Host.MainForm.FlightPlanner.writeKML();
        }
   
        void remove_Click(object sender, EventArgs e)
        {
             // iterate through the waypoints
            for (int i = 0; i < commands.Rows.Count; i++)
            {
                DataGridViewRow row = commands.Rows[i];
                // remove reversing entries
                if (row.Cells[0].Value.ToString() == MAVLink.MAV_CMD.DO_SET_REVERSE.ToString())
                {
                    commands.Rows.Remove(commands.Rows[i]);
                    i--;
                }
            }
            // redraw the map
            Host.MainForm.FlightPlanner.writeKML();
        }
    }    
}
