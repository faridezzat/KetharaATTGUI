using System;
using System.Data;
using System.Net.Sockets;
using System.Windows.Forms;
using zkemkeeper;

namespace KetharaATTGUI
{
   public class Machine
    {

        #region members
        public string ipAddress { set; get; }
        public int port { set; get; }
        public int machineNUmber { set; get; }
        public bool isConnected { set; get; }
        public int password { set; get; }
        public CZKEMClass zk { set; get; }
        private DateTime basedate = new DateTime(1900, 1, 1);
        #endregion

        #region constructor
        public Machine(string ip, int prt, int mn, int pwd)
        {
            this.ipAddress = ip;
            this.port = prt;
            this.machineNUmber = mn;
            this.password = pwd;
            this.zk = new CZKEMClass();

            connect();

        }


        #endregion


        #region logError
        private void log(string error)
        {
            Form1 f = Application.OpenForms["Form1"] as Form1;
            int e = 0;
            zk.GetLastError(ref e);
            string x =   ipAddress + " \t" + e + " \t" + error + " \t" + DateTime.Now ;
            f.appendText(x);
        }
        #endregion

        #region connect
        public void connect()
        {

            zk.SetCommPassword(password); 
            isConnected = zk.Connect_Net(ipAddress, port);
            if (!isConnected)
            {
                log("Unable to connect");
            }


        }




        #endregion

        #region redBuffer
        public bool readGData()
        {

            return zk.ReadGeneralLogData(this.machineNUmber);
        }
        #endregion

        #region flag
        private char flag(int x)
        {
            switch (x)
            {
                case 0:
                    return 'I';
                case 1:
                    return 'O';
                default:
                    return 'Z';
            }
        }
        #endregion

        #region save Logs To DB
        public bool saveLogs(string code, DateTime date, int inOut)
        {
            try
            {
                AttendanceDataSet ds = new AttendanceDataSet();
                AttendanceDataSetTableAdapters.CHECKINOUTTableAdapter adapter = new AttendanceDataSetTableAdapters.CHECKINOUTTableAdapter();
                DataRow dr = ds.CHECKINOUT.NewRow();
                dr["USERID"] = code;
                dr["SENSORID"] = machineNUmber;
                dr["CHECKTIME"] = date;
                dr["CHECKTYPE"] = inOut;
                dr["isDone"] = false;
                ds.CHECKINOUT.Rows.Add(dr);
                adapter.Update(ds.CHECKINOUT);
                return true;
            }
            catch (Exception e)
            {
                log(e.Message);
                return false;
            }
        }
        #endregion
        public bool isAlive()
        {
            using (TcpClient tcp = new TcpClient())
            {
                return tcp.ConnectAsync(ipAddress, port).Wait(1000);
            }
        }

        #region Getting Logs

        public void getMachineLogs()
        {
            if (!isAlive())
            {
                isConnected = false;
                log("device unreachable");
                return;
            }
            if (!isConnected)
            {
                connect();
                return;
            }

            string enrollNO;
            int verifyMode;
            int inoutMode;
            int year;
            int month;
            int DayOfWeek;
            int hour;
            int minute;
            int second;
            int workerCode = 1;

            if (!readGData())
            {
                log("No New Data");
                return;
            }

            while (zk.SSR_GetGeneralLogData(this.machineNUmber, out enrollNO, out verifyMode, out inoutMode, out year, out month, out DayOfWeek, out hour, out minute, out second, ref workerCode))
            {
                DateTime date = new DateTime(year, month, DayOfWeek, hour, minute, second);
                bool isDeletable = Properties.Settings.Default.delete;
                if (saveLogs(enrollNO, date, inoutMode))
                {
                    if (isDeletable)
                    {
                        zk.ClearGLog(machineNUmber);
                    }
                }
            }

        }

        #endregion

    }
}
