using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;

namespace Skid_Labels
{
    public static class DataClass
    {
        //static int Vendor = 70166;
        //static string VendorList = "70166";
        //public static string GetBaseModel(string Item)
        //{
        //    string BaseModel;
        //    string sqlCommandSrc;
        //    SqlDataReader sdrDMTarget;
        //    SqlConnection scDMTarget = new SqlConnection(ConfigurationManager.ConnectionStrings["DMTargetConnectionString"].ConnectionString);

        //    string Line = "";
        //    bool error = false;

        //    scDMTarget.Open();

        //    // Add transactions to remove existing labels for this line first
        //    sqlCommandSrc = string.Concat("SELECT POBCLBP.[PBCTL#] FROM[DMTarget].[dbo].[POBCLBP] where ((pbpono = '", PO, "') and (PBOLIN='", Line, "'))");
        //    SqlCommand command = new SqlCommand(sqlCommandSrc, sqlConnection1);
        //    srcDataReader = command.ExecuteReader();
        //    if (srcDataReader.HasRows)
        //    {
        //        while (srcDataReader.Read())
        //        {
        //            decimal tmpSN;
        //            tmpSN = (decimal)srcDataReader["PBCTL#"];
        //            // sqlCommandRel = string.Concat("Insert into SL_Operations (SerialNo, PO, Line, Item, Component, Description, ShipQty, Operation, CartonNo, SkidNo, Status) Values (", tmpSN.ToString(), ", '", PO, "', '", Line, "', '", Item, "', '', '', 0, 'D', 0, 0, 'N') ");
        //            // relCommand = new SqlCommand(sqlCommandRel, sqlConnection2);
        //            // if (relCommand.ExecuteNonQuery() > 0)
        //        }
        //    }
        //    srcDataReader.Close();

        //    scDMTarget.Close();

        //    BaseModel = Item;

        //    return BaseModel;
        //}

        public static string GetVendorList()
        {
#if DEBUG 
            // string Domain = "Global\\";
            string Domain = "Global\\";
#else
            string Domain = "GlobalDMZ\\";
#endif
            string VendorList = "";
            string sUsername;
            string sqlCommandSrc;
            SqlConnection sqlConnection2 = new SqlConnection(ConfigurationManager.ConnectionStrings["DMSourceConnectionString"].ConnectionString);
            SqlDataReader sdr;
            SqlCommand command;

            sUsername = HttpContext.Current.User.Identity.Name.Remove(0, Domain.Length);


            // Get the PO/Line # for this Carton
            sqlConnection2.Open();
            sqlCommandSrc = string.Concat("Select * from SL_Accounts where Login='", sUsername, "'");
            command = new SqlCommand(sqlCommandSrc, sqlConnection2);
            sdr = command.ExecuteReader();
            if (sdr.HasRows)
            {
                sdr.Read();
                VendorList = sdr["Account"].ToString();
            }
            else
            {
                VendorList = "0";
            }
            sdr.Close();
            sqlConnection2.Close();
            return VendorList;
        }
        public static void MarkPrinted(string CartonSN)
        {
            string sqlCommandSrc;
            SqlConnection sqlConnection2 = new SqlConnection(ConfigurationManager.ConnectionStrings["DMSourceConnectionString"].ConnectionString);
            SqlDataReader sdr;
            // SqlTransaction sqlTransaction;
            decimal sequence;
            string LineNo;
            string PONo;
            string VendorList = GetVendorList();

            sqlConnection2.Open();

            try
            {
                // Mark the SL_Labels record as printed
                sqlCommandSrc = string.Concat("Update SL_Labels Set PrintStatus='P' where LabelSN=", CartonSN);
                SqlCommand command = new SqlCommand(sqlCommandSrc, sqlConnection2);
                command.ExecuteScalar();

                // Get the PO/Line # for this Carton
                sqlCommandSrc = string.Concat("Select * from SL_Labels where LabelSN=", CartonSN);
                command = new SqlCommand(sqlCommandSrc, sqlConnection2);
                sdr = command.ExecuteReader();
                if (sdr.HasRows)
                {
                    sdr.Read();
                    PONo = (string)sdr["PO"];
                    LineNo = (string)sdr["Line"];

                    // Do we have any unprinted labels?
                    sqlCommandSrc = string.Concat("Select * from SL_Labels where Vendor=", VendorList, " and PO='", PONo, "' and Line='", LineNo, "' and PrintStatus<>'P'");
                    command = new SqlCommand(sqlCommandSrc, sqlConnection2);
                    sdr = command.ExecuteReader();
                    if(sdr.HasRows)
                    {
                        // Partially Printed
                        sqlCommandSrc = string.Concat("Update SL_Detail Set PrintStatus='P', PrintTime='", DateTime.Now.ToString(), "' where Vendor=", VendorList, " and PO='", PONo, "' and Line='", LineNo, "'");
                        command = new SqlCommand(sqlCommandSrc, sqlConnection2);
                        command.ExecuteScalar();
                    }
                    else
                    {
                        // Completely printed
                        sqlCommandSrc = string.Concat("Update SL_Detail Set PrintStatus='C', PrintTime='", DateTime.Now.ToString(), "' where Vendor=", VendorList, " and PO='", PONo, "' and Line='", LineNo, "'");
                        command = new SqlCommand(sqlCommandSrc, sqlConnection2);
                        command.ExecuteScalar();
                    }

                }
            }
            catch (Exception ex)
            {

            }
            sqlConnection2.Close();
        }

        public static string GetVendor(decimal VendorNo)
        {
            string sqlCommandSrc;
            SqlConnection sqlConnection2 = new SqlConnection(ConfigurationManager.ConnectionStrings["DMTargetConnectionString"].ConnectionString);
            SqlDataReader sdr;
            string VendorName;

            sqlConnection2.Open();
            sqlCommandSrc = string.Concat("Select * from GIVXRFP where [$LVEN1]=", VendorNo.ToString());
            SqlCommand command = new SqlCommand(sqlCommandSrc, sqlConnection2);
            sdr = command.ExecuteReader();
            if (sdr.HasRows)
            {
                sdr.Read();
                VendorName = (string)sdr["$LDSC1"];
            }
            else
            {
                VendorName = "";
            }
            sqlConnection2.Close();
            return VendorName;
        }

        public static decimal GetNextSkid(string PO, int SkidNo)
        {
            decimal SkidSN = 0;
            string sqlCmdDMSource;
            SqlCommand scmdDMSource;
            SqlConnection scDMSource = new SqlConnection(ConfigurationManager.ConnectionStrings["DMSourceConnectionString"].ConnectionString);
            SqlDataReader sdr;
            string VendorList = GetVendorList();

            scDMSource.Open();
            sqlCmdDMSource = string.Concat("Insert Into SL_Skids (Vendor, PO, Skid) values (", VendorList, ", ", PO, ", ", SkidNo.ToString(), ")");
            scmdDMSource = new SqlCommand(sqlCmdDMSource, scDMSource);
            if (scmdDMSource.ExecuteNonQuery() < 1)
            {
                return 0;
            }
            sqlCmdDMSource = string.Concat("Select @@Identity as newId from SL_Skids");
            scmdDMSource = new SqlCommand(sqlCmdDMSource, scDMSource);
            SkidSN = Convert.ToInt32(scmdDMSource.ExecuteScalar());
            return SkidSN;
        }

        public static decimal GetNextBulk(string PO, int BulkNo)
        {
            decimal BulkSN = 0;
            string sqlCmdDMSource;
            SqlCommand scmdDMSource;
            SqlConnection scDMSource = new SqlConnection(ConfigurationManager.ConnectionStrings["DMSourceConnectionString"].ConnectionString);
            SqlDataReader sdr;
            string VendorList = GetVendorList();

            scDMSource.Open();
            sqlCmdDMSource = string.Concat("Insert Into SL_Bulk (Vendor, PO, BulkCarton) values (", VendorList, ", ", PO, ", ", BulkNo.ToString(), ")");
            scmdDMSource = new SqlCommand(sqlCmdDMSource, scDMSource);
            if (scmdDMSource.ExecuteNonQuery() < 1)
            {
                return 0;
            }
            sqlCmdDMSource = string.Concat("Select @@Identity as newId from SL_Bulk");
            scmdDMSource = new SqlCommand(sqlCmdDMSource, scDMSource);
            BulkSN = Convert.ToInt32(scmdDMSource.ExecuteScalar());
            return BulkSN;
        }


        public static decimal GetCartonSN(string PO, int Carton)
        {
            decimal CartonSN;
            string sqlCommandSrc;
            SqlConnection sqlConnection2 = new SqlConnection(ConfigurationManager.ConnectionStrings["DMSourceConnectionString"].ConnectionString);
            SqlDataReader sdr;
            string VendorList = GetVendorList();

            sqlConnection2.Open();
            sqlCommandSrc = string.Concat("SELECT LabelSN from SL_Labels where Vendor=", VendorList," and PO='", PO, "' and Carton=", Carton.ToString());
            SqlCommand command = new SqlCommand(sqlCommandSrc, sqlConnection2);
            sdr = command.ExecuteReader();
            if (sdr.HasRows)
            {
                sdr.Read();
                CartonSN = (decimal)sdr["LabelSN"];
            }
            else
            {
                CartonSN = 0;
            }
            sqlConnection2.Close();
            return CartonSN;

        }

        public static int GetFirstCarton(string PO, string Line)
        {
            int FirstCarton;
            string sqlCommandSrc;
            SqlConnection sqlConnection2 = new SqlConnection(ConfigurationManager.ConnectionStrings["DMSourceConnectionString"].ConnectionString);
            SqlDataReader sdr;
            string VendorList = GetVendorList();

            sqlConnection2.Open();
            sqlCommandSrc = string.Concat("SELECT Top(1) Carton from SL_Packing where Vendor=", VendorList, " and PO='", PO, "' and Line='", Line, "' order by Carton Asc");
            SqlCommand command = new SqlCommand(sqlCommandSrc, sqlConnection2);
            sdr = command.ExecuteReader();
            if(sdr.HasRows)
            {
                sdr.Read();
                FirstCarton = (int)sdr["Carton"];
            }
            else
            {
                FirstCarton = 0;
            }
            sqlConnection2.Close();
            return FirstCarton;
        }

        public static int GetCartonCount(string PO, string Line)
        {
            int MaxCarton;
            string sqlCommandSrc;
            SqlConnection sqlConnection2 = new SqlConnection(ConfigurationManager.ConnectionStrings["DMSourceConnectionString"].ConnectionString);
            SqlDataReader sdr;
            string VendorList = GetVendorList();

            sqlConnection2.Open();
            sqlCommandSrc = string.Concat("SELECT ShipLabels from SL_Detail where Vendor=", VendorList, " and PO='", PO, "' and Line='", Line, "'");
            // and LineType='I'");
            SqlCommand command = new SqlCommand(sqlCommandSrc, sqlConnection2);
            sdr = command.ExecuteReader();
            if (sdr.HasRows)
            {
                sdr.Read();
                MaxCarton = (int)sdr["ShipLabels"];
            }
            else
            {
                MaxCarton = 0;
            }
            sqlConnection2.Close();
            return MaxCarton;
        }

        public static int CheckSkidLabel(string PO, int SkidNo)
        {
            bool bFound = false;
            decimal SkidSN = 0;
            string VendorList = GetVendorList();

            string sqlCommandSrc;
            SqlDataReader srcDataReader;
            SqlConnection sqlConnection2 = new SqlConnection(ConfigurationManager.ConnectionStrings["DMSourceConnectionString"].ConnectionString);

            sqlConnection2.Open();

            // Add transactions to remove existing labels for this line first
            sqlCommandSrc = string.Concat("SELECT * from SL_Skids where Vendor=", VendorList, " and PO='", PO, "' and Skid=", SkidNo.ToString());
            SqlCommand command = new SqlCommand(sqlCommandSrc, sqlConnection2);
            srcDataReader = command.ExecuteReader();
            if (srcDataReader.HasRows)
            {
                srcDataReader.Read();
                SkidSN = (decimal)srcDataReader["SkidSN"];
                srcDataReader.Close();
            }
            srcDataReader.Close();
            sqlConnection2.Close();
            return Decimal.ToInt32(SkidSN);
        }

        public static int CheckBulkLabel(string PO, int BulkNo)
        {
            bool bFound = false;
            decimal BulkSN = 0;
            string VendorList = GetVendorList();

            string sqlCommandSrc;
            SqlDataReader srcDataReader;
            SqlConnection sqlConnection2 = new SqlConnection(ConfigurationManager.ConnectionStrings["DMSourceConnectionString"].ConnectionString);

            sqlConnection2.Open();

            // Add transactions to remove existing labels for this line first
            sqlCommandSrc = string.Concat("SELECT * from SL_Bulk where Vendor=", VendorList, " and PO='", PO, "' and BulkCarton=", BulkNo.ToString());
            SqlCommand command = new SqlCommand(sqlCommandSrc, sqlConnection2);
            srcDataReader = command.ExecuteReader();
            if (srcDataReader.HasRows)
            {
                srcDataReader.Read();
                BulkSN = (decimal)srcDataReader["BulkSN"];
                srcDataReader.Close();
            }
            srcDataReader.Close();
            sqlConnection2.Close();
            return Decimal.ToInt32(BulkSN);
        }


        public static int GetNextLabelSeq(string PO, string Line)
        {
            int CurrentSeq= 0;
            string VendorList = GetVendorList();

            string sqlCommandSrc;
            SqlDataReader srcDataReader;
            SqlConnection sqlConnection2 = new SqlConnection(ConfigurationManager.ConnectionStrings["DMSourceConnectionString"].ConnectionString);

            sqlConnection2.Open();

            // Add transactions to remove existing labels for this line first
            sqlCommandSrc = string.Concat("SELECT Seq from SL_Labels where Vendor=", VendorList, " and PO='", PO, "' and Line='", Line, "' order by Seq Desc");
            SqlCommand command = new SqlCommand(sqlCommandSrc, sqlConnection2);
            srcDataReader = command.ExecuteReader();
            if (srcDataReader.HasRows)
            {
                srcDataReader.Read();
                CurrentSeq = (int)srcDataReader["Seq"];
            }
            srcDataReader.Close();
            sqlConnection2.Close();
            return (CurrentSeq + 1);
        }

        public static void GetLineInfo(string PO, string Line, ref string Item, ref int Qty)
        {
            string VendorList = GetVendorList();
            string sqlCommandSrc;
            SqlDataReader srcDataReader;
            SqlConnection sqlConnection2 = new SqlConnection(ConfigurationManager.ConnectionStrings["DMSourceConnectionString"].ConnectionString);

            sqlConnection2.Open();

            // Add transactions to remove existing labels for this line first
            sqlCommandSrc = string.Concat("SELECT * from SL_Detail where Vendor=", VendorList, " and PO='", PO,"' and Line='", Line, "' and LineType='I'");
            SqlCommand command = new SqlCommand(sqlCommandSrc, sqlConnection2);
            srcDataReader = command.ExecuteReader();
            if (srcDataReader.HasRows)
            {
                while (srcDataReader.Read())
                {

                    Qty = (int)srcDataReader["ShipQty"];
                    Item = (string)srcDataReader["Item"];
                    // sqlCommandRel = string.Concat("Insert into SL_Operations (SerialNo, PO, Line, Item, Component, Description, ShipQty, Operation, CartonNo, SkidNo, Status) Values (", tmpSN.ToString(), ", '", PO, "', '", Line, "', '", Item, "', '', '', 0, 'D', 0, 0, 'N') ");
                    // relCommand = new SqlCommand(sqlCommandRel, sqlConnection2);
                    // if (relCommand.ExecuteNonQuery() > 0)
                }
            }
            srcDataReader.Close();
            sqlConnection2.Close();
        }

        // Check if model has components
        public static bool CheckComponents(string Item)
        {

            string sqlCommandTgt;
            SqlDataReader sdrDMSource;
            SqlConnection scDMTarget = new SqlConnection(ConfigurationManager.ConnectionStrings["DMTargetConnectionString"].ConnectionString);

            SqlCommand tgtCommand;
            bool Results = false;
            scDMTarget.Open();

            // Grab all pre-staged cartons for this 
            string itemLookup, ItemList;
            itemLookup = Item.Trim();
            ItemList = string.Concat(" In ('", itemLookup, "', '", string.Concat(itemLookup, "LP"), "', '", string.Concat(itemLookup, "HP"), "', '", string.Concat(itemLookup, "B"), "', '", string.Concat(itemLookup, "E"), "', '", string.Concat(itemLookup, "FV"), "', '", string.Concat(itemLookup, "KL"), "', '", string.Concat(itemLookup, "KV"), "', '", string.Concat(itemLookup, "W"), "')");

            sqlCommandTgt = string.Concat("Select * from GCBMPRP where GCPROD ", ItemList);
            tgtCommand = new SqlCommand(sqlCommandTgt, scDMTarget);
            sdrDMSource = tgtCommand.ExecuteReader();
            if (sdrDMSource.HasRows)
            {
                Results = true;
            }
            scDMTarget.Close();

            return Results;
        }


        //public static bool CommitLabels(string PO)
        //{
        //    string sqlCommandSrc;
        //    SqlDataReader srcDataReader;
        //    SqlConnection sqlConnection1 = new SqlConnection(ConfigurationManager.ConnectionStrings["DMTargetConnectionString"].ConnectionString);
        //    SqlConnection sqlConnection2 = new SqlConnection(ConfigurationManager.ConnectionStrings["DMSourceConnectionString"].ConnectionString);

        //    string Line = "";
        //    bool error = false;

        //    sqlConnection1.Open();
        //    sqlConnection2.Open();

        //    // Add transactions to remove existing labels for this line first
        //    sqlCommandSrc = string.Concat("SELECT POBCLBP.[PBCTL#] FROM[DMTarget].[dbo].[POBCLBP] where ((pbpono = '", PO, "') and (PBLINE='", Line, "'))");
        //    SqlCommand command = new SqlCommand(sqlCommandSrc, sqlConnection1);
        //    srcDataReader = command.ExecuteReader();
        //    if (srcDataReader.HasRows)
        //    {
        //        while (srcDataReader.Read())
        //        {
        //            decimal tmpSN;
        //            tmpSN = (decimal)srcDataReader["PBCTL#"];
        //            // sqlCommandRel = string.Concat("Insert into SL_Operations (SerialNo, PO, Line, Item, Component, Description, ShipQty, Operation, CartonNo, SkidNo, Status) Values (", tmpSN.ToString(), ", '", PO, "', '", Line, "', '", Item, "', '', '', 0, 'D', 0, 0, 'N') ");
        //            // relCommand = new SqlCommand(sqlCommandRel, sqlConnection2);
        //            // if (relCommand.ExecuteNonQuery() > 0)
        //        }
        //    }
        //    srcDataReader.Close();

        //    sqlConnection2.Close();
        //    sqlConnection1.Close();
        //    return !error;
        //}

        //public static bool CancelLabels(string PO, string Line)
        //{
        //    string sqlCommandDMT;
        //    string sqlCommandDMS;
        //    SqlDataReader srcDataReader;
        //    SqlConnection sqlConnection1 = new SqlConnection(ConfigurationManager.ConnectionStrings["DMTargetConnectionString"].ConnectionString);
        //    SqlConnection sqlConnection2 = new SqlConnection(ConfigurationManager.ConnectionStrings["DMSourceConnectionString"].ConnectionString);
        //    SqlCommand scCommandDMS;

        //    bool error = false;

        //    sqlConnection1.Open();
        //    sqlConnection2.Open();

        //    // Add transactions to remove existing labels for this line first
        //    sqlCommandDMT = string.Concat("SELECT POBCLBP.[PBCTL#] FROM[DMTarget].[dbo].[POBCLBP] where ((pbpono = '", PO, "') and (pbline='", Line, "'))");
        //    SqlCommand command = new SqlCommand(sqlCommandDMT, sqlConnection1);
        //    srcDataReader = command.ExecuteReader();
        //    if (srcDataReader.HasRows)
        //    {
        //        while (srcDataReader.Read())
        //        {

        //            try
        //            {
        //                sqlCommandDMS = string.Concat("Insert into SL_Operations (SerialNo, PO, Line, Item, Component, Description, ShipQty, Operation, CartonNo, OfCartons, SkidSerialNo, Status) Values (", srcDataReader["PBCTL#"].ToString(), ", '", PO, "', '", Line, "', '', '', '', 0, 'D', 0, 0, 0, 'N')");
        //                // if (relCommand.ExecuteNonQuery() > 0)
        //                scCommandDMS = new SqlCommand(sqlCommandDMS, sqlConnection2);
        //                scCommandDMS.ExecuteNonQuery();

        //            }
        //            catch (Exception ex)
        //            {

        //            }
        //        }
        //    }
        //    srcDataReader.Close();

        //    sqlConnection2.Close();
        //    sqlConnection1.Close();
        //    return !error;
        //}


        //public static bool CreateLabels(string PO, string Line, string Item, int ShipQty, string ItemType)
        //{
        //    string sqlCommandSrc, sqlCommandTgt;
        //    SqlDataReader sdrDMSource;
        //    SqlConnection scDMTarget = new SqlConnection(ConfigurationManager.ConnectionStrings["DMTargetConnectionString"].ConnectionString);
        //    SqlConnection scDMSource = new SqlConnection(ConfigurationManager.ConnectionStrings["DMSourceConnectionString"].ConnectionString);

        //    SqlCommand srcCommand, tgtCommand;
        //    bool error = false;

        //    scDMTarget.Open();
        //    scDMSource.Open();

        //    // Add labels to SL_Packing file
        //    switch (ItemType)
        //    {
        //        case "C":
        //            // Get Component list for this item
        //            sqlCommandSrc = string.Concat("Select * from GCBMPRP where GCPROD='", Item, "'");
        //            srcCommand = new SqlCommand(sqlCommandSrc, scDMTarget);
        //            sdrDMSource = srcCommand.ExecuteReader();
        //            if(sdrDMSource.HasRows)
        //            {
        //                // Loop through the components and create records in SL_Packing based on ShipQty * GCVOLM
        //                while(sdrDMSource.Read())
        //                {
        //                    decimal GCQUAN;
        //                    decimal GCVOLM;
        //                    string GCCMPN;
        //                    string GCDSC1;

        //                    GCQUAN= (decimal)sdrDMSource["GCQUAN"];
        //                    GCVOLM = (decimal)sdrDMSource["GCVOLM"];
        //                    GCCMPN = (string)sdrDMSource["GCCMPN"];
        //                    GCDSC1 = (string)sdrDMSource["GCDSC1"];

        //                    for (int i = 1; i <= ShipQty; i++)
        //                    {
        //                        int cmpnQty = ShipQty * (int)GCQUAN;
        //                        sqlCommandTgt = string.Concat("Insert Into SL_Packing (Vendor, PO, Line, LineType, ShipQty, Item, Component, Description, Carton, Skid) Values(", VendorList, "'", PO, "', '", Line, "', '", ItemType, "', ", cmpnQty.ToString(), ", '", Item, "', '", GCCMPN, "', '", GCDSC1, "', 0, 0) ");
        //                        tgtCommand = new SqlCommand(sqlCommandTgt, scDMSource);
        //                        if (tgtCommand.ExecuteNonQuery() < 1)
        //                        {
        //                            error = true;
        //                        }
        //                    }


        //                }
        //            }
        //            else
        //            {
        //                scDMSource.Close();
        //                scDMTarget.Close();
        //                return false;
        //            }
        //            break;
        //        case "I":           // 1 Label Per Item
        //            for(int i = 1; i <= ShipQty; i++)
        //            {
        //                sqlCommandTgt = string.Concat("Insert Into SL_Packing (Vendor, PO, Line, LineType, ShipQty, Item, Component, Description, Carton, Skid) Values(", VendorList, "'", PO, "', '", Line, "', '", ItemType, "', 1, '", Item, "', '', '', 0, 0)");
        //                tgtCommand = new SqlCommand(sqlCommandTgt, scDMSource);
        //                if(tgtCommand.ExecuteNonQuery() < 1)
        //                {
        //                    error = true;
        //                }
        //            }
        //            break;
        //    }

        //    scDMSource.Close();
        //    scDMTarget.Close();
        //    return !error;
        //}
    }
}