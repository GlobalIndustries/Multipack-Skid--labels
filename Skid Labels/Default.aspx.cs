using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using IronPdf;

namespace Skid_Labels
{
    public partial class _Default : Page
    {
        int PackedLines = 0;
        int TotalLines = 0;
        //int VendorID = 70166;
        //string VendorList = "70166";
        string VendorList;

        protected void Page_Load(object sender, EventArgs e)
        {
            string PO = "";

            // Get our current Vendor #
            VendorList = DataClass.GetVendorList();
            lblVendorList.Text = VendorList.ToString();

            // Check Query Strings
            if (Request.QueryString["PO"] != null)
            {
                PO = Request.QueryString["PO"];
            }


            if (!IsPostBack)
            {

#if DEBUG

#else
                IronPdf.License.LicenseKey = "IRONPDF-1279607F8E-533141-55638E-E43834FE2B-E70C669F-UExAE84B49B1D649D8-GLOBALINDUSTRIES.IRO200713.6800.41122.PRO.1DEV.1YR.SUPPORTED.UNTIL.14.JUL.2021";
#endif

                // if (!IronPdf.License.IsValidLicense("IRONPDF-1279607F8E-533141-55638E-E43834FE2B-E70C669F-UExAE84B49B1D649D8-GLOBALINDUSTRIES.IRO200713.6800.41122.PRO.1DEV.1YR.SUPPORTED.UNTIL.14.JUL.2021"))
                if (!IronPdf.License.IsLicensed)
                {
                    lblMessage.Text = "License error with PDF Add-in.";
                }

                txtSearch.Text = "";
                if (PO.Length > 0)
                {
                    txtSearch.Text = PO;
                    ReadHeader(PO);
                }

                sdsDetail.SelectCommand = string.Concat("Select * from SL_Detail where LineType in ('H', 'I') and (Vendor=", VendorList, ") and PO='", txtSearch.Text.Trim(), "' order by Line");
                sdsDetail.Select(DataSourceSelectArguments.Empty);
                gvItems.DataBind();
                CheckReady();
            }
            else
            {
                lblMessage.Text = "";
            }
        }

        protected void CheckReady()
        {
            if((PackedLines == TotalLines) && (PackedLines > 0)) 
            {
                hlPOBySkid.Visible = true;
                hlPrint.Visible = true;
               // hlPOBySkid.NavigateUrl = string.Concat("./LinePacking.aspx?PO=", lblPO.Text);
            }
            else
            {
                hlPrint.Visible = false;
                hlPOBySkid.Visible = false;
            }
        }

        protected void btnSearch_Click(object sender, EventArgs e)
        {
            if(txtSearch.Text.Trim().Length > 0 )
            {
                // Read the header for this PO
                if(ReadHeader(txtSearch.Text.Trim()))
                {
                    // Create Detail Records if they don't exist
                    CreateDetail(txtSearch.Text.Trim());

                    // sdsPO.SelectCommand = string.Concat("Select POBCLBP.*, ICITEMP.ITDESC from POBCLBP inner Join ICITEMP on POBCLBP.PBITEM=ICITEMP.ITITEM where PBVEND in (70165, 70169, 70163, 70166, 70167) and PBPONO=", txtSearch.Text);
                    sdsDetail.SelectCommand = string.Concat("Select * from SL_Detail where LineType in ('H', 'I') and (Vendor=", VendorList, ") and PO='", txtSearch.Text.Trim(), "' order by Line");
                    sdsDetail.Select(DataSourceSelectArguments.Empty);
                    gvItems.DataBind();
                    CheckReady();
                }
            }
            else
            {
                ClearFields();
                lblMessage.Text = "No Search Results Found.";
            }
        }

        protected void CreateDetail(string strPO)
        {
            string sqlCmdDMSource, sqlCmdDMTarget, sqlCommandRel;
            SqlDataReader srcDataReader, tgtDataReader, relDataReader, outerDR;
            SqlConnection scDMTarget = new SqlConnection(ConfigurationManager.ConnectionStrings["DMTargetConnectionString"].ConnectionString);
            SqlConnection scDMSource = new SqlConnection(ConfigurationManager.ConnectionStrings["DMSourceConnectionString"].ConnectionString);
            SqlCommand tgtCommand, relCommand;
            SqlTransaction srcTransaction;
            bool hasComponents = false;
            bool Replacement = false;
            bool Fallback = false;
            bool HasDetail = false;
            string strComponent;
            string GCOrder = "";
            string Line = "";
            string LastLine = "";
            string OrderLine = "";
            string Description;
            string Item;
            string itemLookup;
            string Warehouse;
            decimal OrderQty;
            string CurrentPO;
            string OriginalPO;
            string LastPO = "";

            scDMTarget.Open();
            scDMSource.Open();

            OriginalPO = strPO;
            // Create a list of the possible PO/Replacement POs

            // 7/15/2020 changing loop from ONLY distinct purchase orders to all fields.
            // sqlCmdDMTarget = string.Concat("SELECT distinct(PBPONO) from POBCLBP where (PBVEND=", VendorList, ") and ((PBPONO='", strPO, "') or (PBORPO='", strPO, "'))");

            sqlCmdDMTarget = string.Concat("SELECT * from POBCLBP where (PBVEND=", VendorList, ") and ((PBPONO='", strPO,"') or (PBORPO='", strPO, "')) Order By PBORPO, PBORLN, PBPONO, PBLINE");
            SqlCommand command = new SqlCommand(sqlCmdDMTarget, scDMTarget);
            outerDR = command.ExecuteReader();
            if (!outerDR.HasRows)
            {
                // Only occurs if there is no detail for the PO and no replacement POs
                lblMessage.Text = "This PO cannot be located.";
            }
            else
            {
                // Mark having detail as false, will update when we find some
                while(outerDR.Read())
                {
                    Item = "";
                    Line = "";

                    CurrentPO = outerDR["PBPONO"].ToString();
                    Replacement = (string.Compare(CurrentPO, strPO) != 0) ? true : false;

                    // Read necessary fields from AS400 Detail
                    if (Replacement)
                    {
                        Line = outerDR["PBORLN"].ToString();
                        // Line = srcDataReader["PDOLIN"].ToString();
                    }
                    else
                    {
                        Line = outerDR["PBLINE"].ToString();
                        // Line = srcDataReader["PDLINE"].ToString();
                    }

                    // Move to the next record if we are processing the PO line
                    if((string.Compare(Line, LastLine) == 0) & (string.Compare(LastPO, CurrentPO) == 0)) continue;
                    LastLine = Line;
                    LastPO = CurrentPO;
                    OrderLine = outerDR["PBLINE"].ToString();
                    Item = outerDR["PBITEM"].ToString();

                    // Pull this specific line from the order detail file
                    sqlCmdDMTarget = string.Concat("SELECT * from POORDDP where (PDPONO = '", CurrentPO, "') and (PDLINE= '", OrderLine, "') and (PDITEM <> '')");
                    command = new SqlCommand(sqlCmdDMTarget, scDMTarget);
                    srcDataReader = command.ExecuteReader();
                    if (srcDataReader.HasRows)
                    {
                        HasDetail = true;

                        // Begin a SQL transaction 
                        srcTransaction = scDMSource.BeginTransaction();
                        try
                        {
                            while (srcDataReader.Read())
                            {
                                string DetailItem;
                                Description = "";
                                OrderQty = 0;

                                Fallback = false;
                                itemLookup = "";

                                OrderQty = (decimal)srcDataReader["PDORQY"];
                                Description = srcDataReader["PDDESC"].ToString();
                                Warehouse = srcDataReader["PDWHSI"].ToString();
                                DetailItem = srcDataReader["PDITEM"].ToString();

                                // First step is to cross reference the vendor for this Item against ICITMBP
                                string sVen1 = "";
                                sqlCommandRel = string.Concat("Select * from ICITMBP where IBCONO=1 and IBITEM='", DetailItem, "' and IBWHSI='", Warehouse, "'");
                                relCommand = new SqlCommand(sqlCommandRel, scDMTarget);
                                relDataReader = relCommand.ExecuteReader();
                                if(relDataReader.HasRows)
                                {
                                    relDataReader.Read();
                                    sVen1 = relDataReader["IBVEN1"].ToString();
                                }
                                relDataReader.Close();
                                // If the Vendor for this item isn't the current vendor, move to the next item from the detail
                                if(string.Compare(sVen1, VendorList) != 0)
                                {
                                    continue;
                                }

                                // Specific to Global Contract
                                // Check if this row is a Item or Component
                                // Next check in GCORDHP
                                sqlCommandRel = string.Concat("Select * from GCORDHP where CUSPON in ('", OriginalPO, "', '", string.Concat(strPO, "-RT"), "')");
                                relCommand = new SqlCommand(sqlCommandRel, scDMTarget);
                                relDataReader = relCommand.ExecuteReader();


                                if (relDataReader.HasRows)
                                {
                                    hasComponents = true;
                                    relDataReader.Read();
                                    GCOrder = relDataReader["OrdNo"].ToString();
                                    relDataReader.Close();

                                    // Get the detail for this particular line
                                    sqlCommandRel = string.Concat("Select * from GCORDDP where ORDNO='", GCOrder, "' and [LineNo]=", Line);
                                    relCommand = new SqlCommand(sqlCommandRel, scDMTarget);
                                    relDataReader = relCommand.ExecuteReader();
                                    if (relDataReader.HasRows)
                                    {
                                        // Get the product # for lookup
                                        relDataReader.Read();
                                        itemLookup = relDataReader["PRODNO"].ToString();
                                        relDataReader.Close();
                                    }
                                    else
                                    {
                                        relDataReader.Close();
                                        Fallback = true;
                                    }
                                }
                                else
                                {
                                    relDataReader.Close();
                                    Fallback = true;
                                }

                                // Not in GC's files, check the patterns of LP, HP, etc.
                                if (Fallback)
                                {
                                    string ItemList;
                                    itemLookup = Item.Trim();
                                    ItemList = string.Concat(" In ('", itemLookup, "', '", string.Concat(itemLookup, "LP"), "', '", string.Concat(itemLookup, "HP"), "', '", string.Concat(itemLookup, "B"), "', '", string.Concat(itemLookup, "E"), "', '", string.Concat(itemLookup, "FV"), "', '", string.Concat(itemLookup, "KL"), "', '", string.Concat(itemLookup, "KV"), "', '", string.Concat(itemLookup, "W"), "')");
                                    sqlCommandRel = string.Concat("Select * from GCBMPRP where GCPROD ", ItemList, " Order by GCPROD");
                                }
                                else
                                {
                                    sqlCommandRel = string.Concat("Select * from GCBMPRP where GCPROD='", itemLookup, "'");
                                }
                                relCommand = new SqlCommand(sqlCommandRel, scDMTarget);
                                relDataReader = relCommand.ExecuteReader();

                                if (relDataReader.HasRows)
                                {
                                    hasComponents = true;
                                }

                                // Read existing detail rows for updating
                                sqlCmdDMSource = string.Concat("Select * from SL_Detail where (Vendor=", VendorList, ") and PO='", OriginalPO, "' and Line='", Line, "'");
                                tgtCommand = new SqlCommand(sqlCmdDMSource, scDMSource);
                                tgtCommand.Transaction = srcTransaction;
                                tgtDataReader = tgtCommand.ExecuteReader();
                                if (!tgtDataReader.HasRows)
                                {
                                    sqlCmdDMSource = string.Concat("Insert into SL_Detail (Vendor, PO, Line, LineType, Item, Description, ComponentQty, BOMQty, ShipQty, PackStatus, CalcLabels) Values (", VendorList, ", '", OriginalPO, "','", Line, "','I','", Item.Trim(), "','", Description, "', 1, ", OrderQty.ToString(), ",", OrderQty.ToString(), ", 'S', 0)");
                                    tgtCommand = new SqlCommand(sqlCmdDMSource, scDMSource);
                                    tgtCommand.Transaction = srcTransaction;
                                    tgtCommand.ExecuteNonQuery();

                                    // Component Row?  Add Components to SL_Detail
                                    if (hasComponents)
                                    {
                                        string LastItem = "", ThisItem = "";
                                        // Loop through components and add detail into SL_Detail
                                        while (relDataReader.Read())
                                        {
                                            ThisItem = (string)relDataReader["GCPROD"];
                                            if((ThisItem == LastItem) || (LastItem == ""))
                                            {
                                                strComponent = (string)relDataReader["GCCMPN"];
                                                Description = (string)relDataReader["GCDSC1"];
                                                decimal PartQty = (decimal)relDataReader["GCQUAN"];
                                                decimal PartVol = (decimal)relDataReader["GCVOLM"];
                                                sqlCmdDMSource = string.Concat("Insert into SL_Detail (Vendor, PO, Line, LineType, Item, Component, Description, ComponentQty, BOMQty, ShipQty, PackStatus, CalcLabels) Values (", VendorList, ", '", OriginalPO, "','", Line, "', 'C', '", Item.Trim(), "','", strComponent, "','", Description, "',", PartQty.ToString(), ", ", (OrderQty * PartQty).ToString(), ", ", (OrderQty * PartQty).ToString(), ", 'S', 0)");
                                                tgtCommand = new SqlCommand(sqlCmdDMSource, scDMSource);
                                                tgtCommand.Transaction = srcTransaction;
                                                tgtCommand.ExecuteNonQuery();
                                            }
                                            else
                                            {
                                                if (LastItem != "") break;
                                            }
                                            LastItem = (string)relDataReader["GCPROD"];
                                        }
                                    }
                                }
                                tgtDataReader.Close();
                                relDataReader.Close();

                            }   // End While Loop
                            srcDataReader.Close();
                            srcTransaction.Commit();
                        }
                        catch (Exception ex)
                        {
                            srcTransaction.Rollback();
                            lblMessage.Text = "Critical error generating PO detail.";
                        }


                    }
                }   // End Outer Loop
                outerDR.Close();

                // We should be finished processing
                if(!HasDetail)
                {
                    lblMessage.Text += "No detail records exist for this PO.";
                }

            }

            scDMSource.Close();
            scDMTarget.Close();
        }


        protected void PrintClick(object sender, EventArgs e)
        {
            string sqlCmdDMSource, sqlCmdDMTarget, sqlCommandRel;
            SqlDataReader srcDataReader, tgtDataReader, relDataReader;
            SqlConnection scDMTarget = new SqlConnection(ConfigurationManager.ConnectionStrings["DMTargetConnectionString"].ConnectionString);
            SqlConnection scDMSource = new SqlConnection(ConfigurationManager.ConnectionStrings["DMSourceConnectionString"].ConnectionString);
            SqlCommand tgtCommand, relCommand, srcCommand;
            SqlTransaction srcTransaction;
            string strComponent;
            string itemLookup;
            string GCOrder = "";
            string strPO;

            strPO = lblPO.Text;
            scDMTarget.Open();
            scDMSource.Open();

            // Start a transaction
            srcTransaction = scDMSource.BeginTransaction();

            // Purge out any incomplete operations on the PO for this vendor
            try
            {
                sqlCmdDMSource = string.Concat("Delete from SL_Operations where (Vendor=", VendorList, ") and PO='", strPO, "' and Status='N'");
                srcCommand = new SqlCommand(sqlCmdDMSource, scDMSource);
                srcCommand.Transaction = srcTransaction;
                srcCommand.ExecuteNonQuery();
            }
            catch(Exception ex)
            {

            }

            // Loop through the labels created for this PO, we need to ignore the Skid Labels and Bulk Labels
            // Add new records to the SL_Operations table for each label that doesn't exist on the AS400
            sqlCmdDMSource = string.Concat("Select * from SL_Labels where (Vendor=", VendorList, ") and PO='", strPO, "' and SkidSN=0 and BulkSN=0 Order By Line, Carton");
            srcCommand = new SqlCommand(sqlCmdDMSource, scDMSource);
            srcCommand.Transaction = srcTransaction;
            srcDataReader = srcCommand.ExecuteReader();
            if(srcDataReader.HasRows)
            {
                string CurrentLine = "";
                string LastLine = "";
                string Item = "";
                string Component = "";
                string Description = "";
                decimal SkidSN = 0;
                decimal BulkSN = 0;
                int CartonNum = 0, CartonCount = 0;
                int SkidNo = 0;
                int BulkNo = 0;
                int ShipQty = 0;
                bool NewLine = false;

                while (srcDataReader.Read())
                {
                    // If we are creating a skid 
                    SkidNo = (int)srcDataReader["Skid"];
                    BulkNo = (int)srcDataReader["BulkCarton"];
                    CurrentLine = srcDataReader["Line"].ToString();
                    if(string.Compare(CurrentLine, LastLine) != 0)
                    {
                        NewLine = true;
                        CartonNum = 1;
                        LastLine = CurrentLine;
                    }
                    else
                    {
                        // Increase the carton number
                        NewLine = false;
                        CartonNum++;
                    }

                    // Search for this specific label
                    sqlCommandRel = string.Concat("SELECT POBCLBP.[PBCTL#] FROM[DMTarget].[dbo].[POBCLBP] where (((pbpono = '", strPO, "') or (pborpo = '", strPO, "')) and (pbvend=", VendorList, ") and (PBCTL#=", srcDataReader["LabelSN"].ToString(), "))");
                    relCommand = new SqlCommand(sqlCommandRel, scDMTarget);
                    relDataReader = relCommand.ExecuteReader();

                    // If this label doesn't exist, create a record in SL_Operations to add it
                    if(!relDataReader.HasRows)
                    {
                        // First figure out hwo many cartons exist for this Line/Item/Component
                        sqlCmdDMSource = string.Concat("Select Count(distinct Carton) as Ctns from SL_Packing where Vendor=", VendorList, " and PO='", strPO, "' and Line='", CurrentLine, "'");
                        tgtCommand = new SqlCommand(sqlCmdDMSource, scDMSource);
                        tgtCommand.Transaction = srcTransaction;
                        tgtDataReader = tgtCommand.ExecuteReader();
                        if (tgtDataReader.HasRows)
                        {
                            tgtDataReader.Read();
                            CartonCount = (int)tgtDataReader["Ctns"];
                        }
                        else
                        {
                            CartonCount = 0;
                        }
                        tgtDataReader.Close();

                        // Next check for the Skid Serial # if this item is on a skid
                        if(SkidNo != 0)
                        {
                            sqlCmdDMSource = string.Concat("Select SkidSN from SL_Labels where Vendor=", VendorList, " and PO='", strPO, "' and Skid=", SkidNo.ToString(), " and SkidSN<>0");
                            tgtCommand = new SqlCommand(sqlCmdDMSource, scDMSource);
                            tgtCommand.Transaction = srcTransaction;
                            tgtDataReader = tgtCommand.ExecuteReader();
                            if (tgtDataReader.HasRows)
                            {
                                tgtDataReader.Read();
                                SkidSN = (decimal)tgtDataReader["SkidSN"];
                            }
                            else
                            {
                                lblMessage.Text = "Critical Error Reading Skid #.";
                                srcTransaction.Rollback();
                                tgtDataReader.Close();
                                scDMSource.Close();
                                scDMTarget.Close();
                                return;
                            }
                            tgtDataReader.Close();
                        }
                        else
                        {
                            // We are not on a skid, Set the Skid serial # to 0
                            SkidSN = 0;
                        }

                        // Check for the bulk SN if we are on a bulk carton
                        if(BulkNo != 0)
                        {
                            sqlCmdDMSource = string.Concat("Select BulkSN from SL_Labels where Vendor=", VendorList, " and PO='", strPO, "' and BulkCarton=", BulkNo.ToString(), " and BulkSN<>0");
                            tgtCommand = new SqlCommand(sqlCmdDMSource, scDMSource);
                            tgtCommand.Transaction = srcTransaction;
                            tgtDataReader = tgtCommand.ExecuteReader();
                            if (tgtDataReader.HasRows)
                            {
                                tgtDataReader.Read();
                                BulkSN = (decimal)tgtDataReader["BulkSN"];
                            }
                            else
                            {
                                lblMessage.Text = "Critical Error Reading Bulk #.";
                                srcTransaction.Rollback();
                                tgtDataReader.Close();
                                scDMSource.Close();
                                scDMTarget.Close();
                                return;
                            }
                            tgtDataReader.Close();
                        }
                        else
                        {
                            // We are not on a skid, Set the Skid serial # to 0
                            BulkSN = 0;
                        }


                        // Get Item information from SL_Detail or SL_Packing
                        sqlCmdDMSource = string.Concat("Select * from SL_Detail where Vendor=", VendorList, " and PO='", strPO, "' and Line='", CurrentLine, "' and LineType='I'");
                        tgtCommand = new SqlCommand(sqlCmdDMSource, scDMSource);
                        tgtCommand.Transaction = srcTransaction;
                        tgtDataReader = tgtCommand.ExecuteReader();
                        if(tgtDataReader.HasRows)
                        {
                            tgtDataReader.Read();
                            Item = tgtDataReader["Item"].ToString();
                            Description = tgtDataReader["Description"].ToString();
                            ShipQty = (int)tgtDataReader["ShipQty"];
                        }
                        else
                        {
                            lblMessage.Text = "Critical Error Reading PO Detail.";
                            srcTransaction.Rollback();
                            tgtDataReader.Close();
                            scDMSource.Close();
                            scDMTarget.Close();
                            return;
                        }
                        tgtDataReader.Close();

                        sqlCmdDMSource = string.Concat("Insert into SL_Operations (SerialNo, Vendor, PO, Line, Item, Component, Description, ShipQty, Operation, CartonNo, OfCartons, SkidSerialNo, Status, BulkSerialNo) Values (", srcDataReader["LabelSN"].ToString(), ", ", VendorList, ", '", strPO, "', '", CurrentLine, "', '", Item, "', '', '", Description, "', ", ShipQty.ToString(), ", 'A', ", CartonNum.ToString(), ", ", CartonCount.ToString(), ", ", SkidSN.ToString(), ", 'N', ", BulkSN.ToString(), ")");
                        tgtCommand = new SqlCommand(sqlCmdDMSource, scDMSource);
                        tgtCommand.Transaction = srcTransaction;
                        tgtCommand.ExecuteNonQuery();
                    }   // Done checking POBCLBP for this specific label
                    relDataReader.Close();
                }       // Outer loop reading through SL_Labels
            }
            srcDataReader.Close();
            // Add to SL_Operations section complete


            // Check that existing labels match Label file, remove if they don't
            // Add transactions to remove existing labels for this line first
            sqlCommandRel = string.Concat("SELECT POBCLBP.[PBCTL#] FROM[DMTarget].[dbo].[POBCLBP] where (((pbpono = '", strPO, "') or (pborpo = '", strPO, "')) and (pbvend=", VendorList, "))");
            relCommand = new SqlCommand(sqlCommandRel, scDMTarget);
            relDataReader = relCommand.ExecuteReader();
            if (relDataReader.HasRows)
            {
                while (relDataReader.Read())
                {
                    sqlCmdDMSource = string.Concat("Select * from SL_Labels where (Vendor=", VendorList, ") and PO='", strPO, "' and LabelSN=", relDataReader["PBCTL#"].ToString());
                    tgtCommand = new SqlCommand(sqlCmdDMSource, scDMSource);
                    tgtCommand.Transaction = srcTransaction;
                    tgtDataReader = tgtCommand.ExecuteReader();
                    if (!tgtDataReader.HasRows)
                    {
                        try
                        {
                            sqlCmdDMSource = string.Concat("Insert into SL_Operations (SerialNo, Vendor, PO, Line, Item, Component, Description, ShipQty, Operation, CartonNo, OfCartons, SkidSerialNo, Status, BulkSerialNo) Values (", relDataReader["PBCTL#"].ToString(), ", ", VendorList, ", '", strPO, "', '', '', '', '', 0, 'D', 0, 0, 0, 'N', 0)");
                            // if (relCommand.ExecuteNonQuery() > 0)
                            tgtCommand = new SqlCommand(sqlCmdDMSource, scDMSource);
                            tgtCommand.Transaction = srcTransaction;
                            tgtCommand.ExecuteNonQuery();
                        }
                        catch (Exception ex)
                        {

                        }
                    }
                    tgtDataReader.Close();
                }
            }
            relDataReader.Close();
            // Finished creating the "Delete Label" records in SL_Operations

            // Commit the operations
            srcTransaction.Commit();

            scDMSource.Close();
            scDMTarget.Close();

            Response.Redirect(string.Concat("./Print.aspx?PO=", lblPO.Text));
        }

        protected void PackPO(object sender, EventArgs e)
        {
            Response.Redirect(string.Concat("./LinePacking.aspx?PO=", lblPO.Text));
        }

        protected void ClearFields()
        {
            lblShipName.Text = "";
            lblShipAddr1.Text = "";
            lblShipAddr2.Text = "";
            lblShipCity.Text = "";
            lblShipState.Text = "";
            lblShipZip.Text = "";
            lblPO.Text = "";
            lblPODate.Text = "";
            lblCompletionDate.Text = "";
            txtVendRef.Text = "";
        }

        protected bool ReadHeader(string strPO)
        {
            string sqlCmdDMSource, sqlCmdDMTarget;
            SqlDataReader srcDataReader, tgtDataReader;
            SqlConnection scDMTarget = new SqlConnection(ConfigurationManager.ConnectionStrings["DMTargetConnectionString"].ConnectionString);
            SqlConnection scDMSource = new SqlConnection(ConfigurationManager.ConnectionStrings["DMSourceConnectionString"].ConnectionString);
            SqlCommand srcCommand, tgtCommand;
            DataView dv;
            string sVendor;

            // Reset line counts
            PackedLines = 0;
            TotalLines = 0;

            // Create Reference Record if it doesn't exist
            try
            {
                scDMTarget.Open();
                // Loop through detail records, create new working status records
                sqlCmdDMTarget = string.Concat("Select * from POORDHP where PHPONO='", strPO, "'");
                tgtCommand = new SqlCommand(sqlCmdDMTarget, scDMTarget);
                tgtDataReader = tgtCommand.ExecuteReader();
                if(tgtDataReader.HasRows)
                {
                    tgtDataReader.Read();
                    sVendor = tgtDataReader["PHVEN1"].ToString();
                    tgtDataReader.Close();
                }
                else
                {
                    lblMessage.Text = string.Concat("PO ", strPO, " Not Found");
                    tgtDataReader.Close();
                    scDMSource.Close();
                    scDMTarget.Close();
                    return false;
                }
                scDMTarget.Close();


                scDMSource.Open();
                sqlCmdDMSource = string.Concat("SELECT * from SL_Status where (Vendor=", VendorList, ") and PO='", strPO, "'");
                srcCommand = new SqlCommand(sqlCmdDMSource, scDMSource);
                srcDataReader = srcCommand.ExecuteReader();
                if(!srcDataReader.HasRows)
                {
                    sqlCmdDMSource = string.Concat("Insert into SL_Status (PO, Vendor, Reference, Status) Values('", strPO, "', ", VendorList, ", '', 'A')");
                    srcCommand = new SqlCommand(sqlCmdDMSource, scDMSource);
                    srcCommand.ExecuteNonQuery();
                }
                srcDataReader.Close();
                scDMSource.Close();
            }
            catch (Exception ex)
            {
                lblMessage.Text = "Critical error creating PO Reference.";
                return (false);
            }

            // Read Header Fields
            sdsPO.SelectCommand = string.Concat("Select POORDHP.*, Ref.Reference from POORDHP inner join [DMSource].[dbo].[SL_Status] as Ref on (POORDHP.PHPONO=Ref.PO) where PHPONO='", strPO, "'");
            dv = (DataView)sdsPO.Select(DataSourceSelectArguments.Empty);
            if(dv.Count > 0)
            {
                lblShipName.Text = dv.Table.Rows[0].Field<string>("PHSNAM");
                lblShipAddr1.Text = dv.Table.Rows[0].Field<string>("PHSAD1");
                lblShipAddr2.Text = dv.Table.Rows[0].Field<string>("PHSAD2");
                lblShipCity.Text = dv.Table.Rows[0].Field<string>("PHSCTY");
                lblShipState.Text = dv.Table.Rows[0].Field<string>("PHSSTA");
                lblShipZip.Text = dv.Table.Rows[0].Field<string>("PHSZIP");
                lblPO.Text = dv.Table.Rows[0].Field<string>("PHPONO");
                lblPODate.Text = string.Format(dv.Table.Rows[0].Field<DateTime>("PHDATE").ToString(), "MM/dd/yyyy");
                lblCompletionDate.Text = string.Format(dv.Table.Rows[0].Field<DateTime>("PHEDAT").ToString(), "MM/dd/yyyy");
                txtVendRef.Text = dv.Table.Rows[0].Field<string>("Reference");
                lblVendor.Text = DataClass.GetVendor(dv.Table.Rows[0].Field<decimal>("PHVEN1"));
                return (true);
            }
            else
            {
                lblMessage.Text = "PO Not Found";
                return (false);
            }
        }

        protected void gvItems_Databound(object sender, GridViewRowEventArgs e)
        {
            HyperLink hl;
            TextBox tb;
            ImageButton ib;
            string PrintStatus, PackStatus, ItemType;

            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                TotalLines++;
                ItemType = e.Row.Cells[1].Text;
                PackStatus = e.Row.Cells[7].Text;
                PrintStatus = e.Row.Cells[8].Text;

                tb = (TextBox)e.Row.FindControl("txtShipQty");
                ib = (ImageButton)e.Row.FindControl("ibEdit");

                // If the item is staged or packed, show the link to the packing page
                if ((string.Compare(PackStatus, "P") == 0) || (string.Compare(PackStatus, "S") == 0))
                {
                    hl = (HyperLink)e.Row.Cells[4].Controls[0];
                    hl.NavigateUrl = String.Concat("./Components.aspx?PO=", txtSearch.Text, "&Line=", e.Row.Cells[0].Text.Trim());  // , "&Item=", hl.Text.Trim(), "&Qty=", e.Row.Cells[2].Text.Trim());
                }

                switch (PackStatus)
                {
                    case "C":
                        e.Row.Cells[7].Text = "Cancelled";
                        ib.Visible = false;
                        tb.Enabled = true;
                        break;
                    case "P":
                        PackedLines++;                  // Increase count of packed items
                        e.Row.Cells[7].Text = "Packed";
                        ib.Visible = true;
                        tb.Enabled = false;
                        break;
                    case "S":
                        e.Row.Cells[7].Text = "Staged";
                        ib.Visible = true;
                        tb.Enabled = false;
                        break;
                    default:
                        e.Row.Cells[7].Text = "Not Packed";
                        ib.Visible = false;
                        tb.Enabled = true;
                        break;
                }

                PrintStatus = e.Row.Cells[8].Text;
                switch(PrintStatus)
                {
                    case "C":
                        e.Row.Cells[8].Text = "Printed";
                        break;
                    case "P":
                        e.Row.Cells[8].Text = "Partially Printed";
                        break;
                    default:
                        e.Row.Cells[8].Text = "Not Printed";
                        break;
                }
            }
        }

        protected void RefChanged(object sender, EventArgs e)
        {

            if(txtVendRef.Text.Trim().Length > 0)
            {
                int i;

                sdsReference.SelectCommand = string.Concat("Select * from SL_Status where (Vendor=", VendorList, ") and PO=;", lblPO.Text, "'");
                sdsReference.UpdateCommand = string.Concat("Update SL_Status Set Reference='", txtVendRef.Text.Trim(), "' where (Vendor=", VendorList, ") and PO='", lblPO.Text, "'");
                i = sdsReference.Update();
                if(i > 0)
                {
                    lblMessage.Text = "Reference Successfully Update";
                }
                else
                {
                    lblMessage.Text = "Error saving Reference #";
                }
            }
        }

        protected void EditQty(object sender, EventArgs e)
        {
            GridViewRow gvr;
            HyperLink hl;
            ImageButton ib;
            TextBox tb;

            gvr = (GridViewRow)((ImageButton)sender).NamingContainer;

            // Make edit button available
            ib = (ImageButton)gvr.FindControl("ibEdit");
            tb = (TextBox)gvr.FindControl("txtShipQty");
            ib.Visible = false;
            tb.Enabled = true;

        }

        protected void QtyChanged(object sender, EventArgs e)
        {
            ImageButton ib;
            GridViewRow gvr;
            string s;
            int BOMQty, ShipQty;
            TextBox tb;
            HyperLink hl;
            string sqlCommandSrc;
            string strLine, strItem;
            SqlConnection scDMSource  = new SqlConnection(ConfigurationManager.ConnectionStrings["DMSourceConnectionString"].ConnectionString);
            SqlCommand srcCommand;
            SqlTransaction sqlTransaction;
            SqlDataReader sdr;
            decimal labelSN;

            tb = (TextBox)sender;
            if(int.TryParse(tb.Text, out ShipQty))
            {
                gvr = (GridViewRow)((TextBox)sender).NamingContainer;
                hl = (HyperLink)gvr.Cells[4].Controls[0];

                int.TryParse(gvr.Cells[2].Text, out BOMQty);

                // Make edit button available
                ib = (ImageButton)gvr.FindControl("ibEdit");
                ib.Visible = true;
                tb.Enabled = false;

                // Get our item #
                strItem = hl.Text.Trim();
                strLine = gvr.Cells[0].Text.Trim();
                // Update Ship Qty, set status to staged
                scDMSource.Open();
                sqlTransaction = scDMSource.BeginTransaction();
                try
                {
                    if(ShipQty == 0)
                    {
                        // If we aren't shipping this line, set the status to Packed
                        sqlCommandSrc = string.Concat("Update SL_Detail set ShipQty=0, PackStatus='P' Where (Vendor=", VendorList, ") and PO='", lblPO.Text, "' and Line='", strLine, "'");
                    }
                    else
                    {
                        sqlCommandSrc = string.Concat("Update SL_Detail set ShipQty=(ComponentQty * ", ShipQty.ToString(), "), PackStatus='S' Where (Vendor=", VendorList, ") and PO='", lblPO.Text, "' and Line='", strLine, "'");
                    }
                    srcCommand = new SqlCommand(sqlCommandSrc, scDMSource);
                    srcCommand.Transaction = sqlTransaction;
                    if (srcCommand.ExecuteNonQuery() < 1)
                    {
                        lblMessage.Text = "Critical Error Updating Records.";
                        return;
                    }
                    else
                    {
                        if(ShipQty == 0)
                        {
                            gvr.Cells[7].Text = "Packed";

                        }
                        else
                        {
                            gvr.Cells[7].Text = "Staged";
                        }
                        hl.NavigateUrl = String.Concat("./Components.aspx?PO=", txtSearch.Text, "&Line=", strLine);          // , "&Item=", strItem, "&Qty=", ShipQty.ToString());
                                                                                                                            // DataClass.StageLabels(lblPO.Text, strLine, strItem, BOMQty, ShipQty);
                    }

                    // Create records to remove labels on AS400 side
                    // Commented 6/18/2020, All SL_Operations take place upon printing
                    //sqlCommandSrc = string.Concat("Select LabelSN from SL_Labels Where (Vendor=", VendorList, ") and PO='", lblPO.Text, "' and PackingSeq in (Select Seq from SL_Packing where (Vendor=", VendorList, ") and PO='", lblPO.Text, "' and Line='", strLine, "')");
                    //srcCommand = new SqlCommand(sqlCommandSrc, scDMSource);
                    //srcCommand.Transaction = sqlTransaction;
                    //sdr = srcCommand.ExecuteReader();
                    //if (sdr.HasRows)
                    //{
                    //    while (sdr.Read())
                    //    {
                    //        labelSN = (decimal)sdr["LabelSN"];
                    //        sqlCommandSrc = string.Concat("Insert into SL_Operations (SerialNo, Vendor, PO, Line, Item, Component, Description, ShipQty, Operation, CartonNo, OfCartons, SkidSerialNo, Status) Values (", labelSN.ToString(), ", ", VendorList, ", '", lblPO.Text, "', '', '', '', '', 0, 'D', 0, 0, 0, 'N') ");
                    //        srcCommand = new SqlCommand(sqlCommandSrc, scDMSource);
                    //        srcCommand.Transaction = sqlTransaction;
                    //        srcCommand.ExecuteNonQuery();
                    //    }
                    //    sdr.Close();
                    //}

                    // Remove Label records for these cartons
                    sqlCommandSrc = string.Concat("Delete from SL_Labels Where (Vendor=", VendorList, ") and PO='", lblPO.Text, "' and PackingSeq in (Select Seq from SL_Packing where (Vendor=", VendorList, ") and PO='", lblPO.Text, "' and Line='", strLine, "')");
                    srcCommand = new SqlCommand(sqlCommandSrc, scDMSource);
                    srcCommand.Transaction = sqlTransaction;
                    srcCommand.ExecuteNonQuery();

                    // Remove packing records
                    sqlCommandSrc = string.Concat("Delete from SL_Packing Where (Vendor=", VendorList, ") and PO='", lblPO.Text, "' and Line='", strLine, "'");
                    srcCommand = new SqlCommand(sqlCommandSrc, scDMSource);
                    srcCommand.Transaction = sqlTransaction;
                    srcCommand.ExecuteNonQuery();
                    sqlTransaction.Commit();
                }
                catch (Exception ex)
                {
                    sqlTransaction.Rollback();
                    scDMSource.Close();
                    lblMessage.Text = "Critical Error Updating Ship Qty";
                }

                // Close SQL Connection
                scDMSource.Close();
                lblMessage.Text = "Ship Quantity Updated.";

            }
            else
            {
                lblMessage.Text = "Invalid Ship Qty";
            }
        }
    }
}