using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO.Ports;

namespace 美团PCBA过站上传工具
{
    public partial class SetupView : Form
    {
        public SetupView()
        {
            InitializeComponent();
            Text = "设置界面";
            Load += Load_Action;
            Save_Btn.Click += Save_Action;
            Cancel_Btn.Click += Cancel_Action;
        }

        private void Load_Action(object sender,EventArgs e)
        {
            Com.DataSource = SerialPort.GetPortNames().ToList();
            Model.Text = Properties.Settings.Default.Model;
            Field.Text = Properties.Settings.Default.Field;
            Station.Text = Properties.Settings.Default.Station;
            Com.Text = Properties.Settings.Default.Com;
            Fixid.Text = Properties.Settings.Default.Fixid;
        }

        private void Save_Action(object sender, EventArgs e)
        {
            Properties.Settings.Default.Com = Com.Text;
            Properties.Settings.Default.Model = Model.Text;
            Properties.Settings.Default.Field = Field.Text;
            Properties.Settings.Default.Station = Station.Text;
            Properties.Settings.Default.Fixid = Fixid.Text;
            Properties.Settings.Default.Save();
            DialogResult = DialogResult.OK;
            Close();
        }

        private void Cancel_Action(object sender, EventArgs e)
        {
            Close();
        }
    }
}
