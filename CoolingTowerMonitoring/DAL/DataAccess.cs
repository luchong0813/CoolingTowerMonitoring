using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoolingTowerMonitoring.DAL
{
    public class DataAccess
    {
        string dbConfig = ConfigurationManager.ConnectionStrings["db_config"].ToString();
        SqlConnection conn;
        SqlCommand comm;
        SqlDataAdapter adapter;
        SqlTransaction trans;

        private void Dispose()
        {
            if (adapter != null)
            {
                adapter.Dispose();
                adapter = null;
            }
            if (trans != null)
            {
                trans.Dispose();
                trans = null;
            }
            if (comm != null)
            {
                comm.Dispose();
                comm = null;
            }
            if (conn != null)
            {
                conn.Dispose();
                conn = null;
            }
        }

        private DataTable GetDatas(string sql)
        {
            DataTable dt = new DataTable();
            try
            {
                conn = new SqlConnection(dbConfig);
                conn.Open();

                adapter = new SqlDataAdapter(sql, conn);
                adapter.Fill(dt);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                Dispose();
            }

            return dt;
        }

        public DataTable GetStorageArea()
        {
            string sql = "select * from storage_area";
            return GetDatas(sql);
        }

        public DataTable GetDevices()
        {
            string sql = "select * from devices";
            return GetDatas(sql);
        }

        public DataTable GetMonitorValues()
        {
            string sql = "select * from monitor_value";
            return GetDatas(sql);
        }
    }
}
