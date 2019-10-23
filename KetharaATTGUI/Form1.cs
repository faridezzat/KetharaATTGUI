using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace KetharaATTGUI
{
    public partial class Form1 : DevExpress.XtraEditors.XtraForm
    {
        public List<Machine> machines;
        public Form1()
        {
            InitializeComponent();
        }

     

        public  void appendText(string text)
        {
            if (memoEdit1.MaskBox.InvokeRequired)
            {
                memoEdit1.MaskBox.Invoke(new Action(() => memoEdit1.MaskBox.AppendText(text + " \r\n")));
            }   
        }

 

        private void notifyIcon1_DoubleClick(object sender, EventArgs e)
        {
            Show();
            WindowState = FormWindowState.Normal;
           
        }

        private void Form1_Load(object sender, EventArgs e)
        {
           
            Thread t = new Thread(new ThreadStart(serviceThread));
            t.Start();

        }

        private void serviceThread()
        {
            machines = devices();
            work();

        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Hide();
            e.Cancel = true;
        }

        private void memoEdit1_DoubleClick(object sender, EventArgs e)
        {
           
        }

        public List<Machine> devices()
        {
            List<Machine> x = new List<Machine>();
            AttendanceDataSet ds = new AttendanceDataSet();
            AttendanceDataSetTableAdapters.MachinesTableAdapter Machines = new AttendanceDataSetTableAdapters.MachinesTableAdapter();
            Machines.Fill(ds.Machines);
            foreach (var machin in ds.Machines)
            {

                Machine m = new Machine(machin.IP, machin.Port, machin.MachineNumber, Convert.ToInt32(machin.CommPassword));
                x.Add(m);
            }
            return x;
        }
        private void uploadLogs()
        {
            AttendanceDataSet ds = new AttendanceDataSet();
            AttendanceDataSetTableAdapters.CHECKINOUTTableAdapter adapter = new AttendanceDataSetTableAdapters.CHECKINOUTTableAdapter();
            adapter.ClearBeforeFill = true;
            adapter.Fill(ds.CHECKINOUT);
            var data = ds.CHECKINOUT;

            foreach (var log in data)
            {
                if (uploadRecord(log.USERID, log.CHECKTIME, Convert.ToInt32(log.CHECKTYPE), Properties.Settings.Default.branch))
                {
                    appendText(log.USERID + " uploaded ");
                    log.isDone = true;
                }
                else
                {
                    appendText("api return false " + log.USERID + " - " + log.CHECKTIME);
                }
            }
            adapter.Update(ds);
        }
        private bool uploadRecord(int code, DateTime date, int type, int branch)
        {
            bool x = false;
            HttpWebRequest request = HttpWebRequest.Create(Properties.Settings.Default.API + string.Format("insert.php?code={0}&date={1}&type={2}&branch={3}", code, date.ToString("yyyy-MM-dd HH:mm:ss"), type, branch)) as HttpWebRequest;
            request.Timeout = 50000;
            request.KeepAlive = false;
            request.ServicePoint.ConnectionLeaseTimeout = 50000;
            request.ServicePoint.MaxIdleTime = 50000;
          //  appendText(request.RequestUri.ToString());
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            using (StreamReader sr = new StreamReader(response.GetResponseStream()))
            {
                x = Convert.ToBoolean(sr.ReadToEnd());
            }
            return x;
        }
        private void work()
        {
            appendText("Checking Attendance");
            foreach (var item in machines)
            {
                item.getMachineLogs();
            }
            uploadLogs();
            Thread.Sleep(5000);
            work();
        }
    }
}
