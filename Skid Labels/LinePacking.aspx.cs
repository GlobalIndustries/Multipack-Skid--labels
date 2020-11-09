using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using System.Web.UI.WebControls.Expressions;

namespace Skid_Labels
{
    public partial class LinePacking : Page
    {
        //int VendorID = 70166;
        //string VendorList = "70166";
        string VendorList;

        protected void Page_Load(object sender, EventArgs e)
        {
            string PO= "";
            string Line = "";
            string Item = "";
            string Qty = "0";

            VendorList = DataClass.GetVendorList();

            // Check Query Strings
            if (Request.QueryString["PO"] != null)
            {
                PO = Request.QueryString["PO"];
            }
            else
            {
                lblMessage.Text = "Missing PO #";
                return ;
            }

            if (Request.QueryString["Line"] != null)
            {
                Line = Request.QueryString["Line"];
            }
            else
            {
                // lblMessage.Text = "Missing Line #";
                // return;
            }

            if (!IsPostBack)
            {
                // Populate header fields
                lblPO.Text = PO;
                lblLineNo.Text = Line;


                // Are we packing a specific line?
                if(Line.Trim() == "")
                {
                    sdsPacking.SelectCommand = string.Concat("SELECT SL_Detail.PO, SL_Detail.Line, SL_Detail.Item, SL_Detail.Description, SL_Detail.ShipQty, SL_Packing.Carton, SL_Packing.BulkCarton, SL_Packing.Skid FROM SL_Packing INNER JOIN SL_Detail ON SL_Packing.Vendor = SL_Detail.Vendor and SL_Packing.PO = SL_Detail.PO AND SL_Packing.Line = SL_Detail.Line WHERE (SL_Detail.LineType = 'I') and (SL_Detail.PO='", PO, "') GROUP BY SL_Detail.PO, SL_Detail.Line, SL_Detail.Item, SL_Detail.ShipQty, SL_Detail.Description, SL_Packing.Carton, SL_Packing.BulkCarton, SL_Packing.Skid order by SL_Packing.Carton");
                }
                else
                {
                    sdsPacking.SelectCommand = string.Concat("SELECT SL_Detail.PO, SL_Detail.Line, SL_Detail.Item, SL_Detail.Description, SL_Detail.ShipQty, SL_Packing.Carton, SL_Packing.BulkCarton, SL_Packing.Skid FROM SL_Packing INNER JOIN SL_Detail ON SL_Packing.Vendor = SL_Detail.Vendor and SL_Packing.PO = SL_Detail.PO AND SL_Packing.Line = SL_Detail.Line WHERE (SL_Detail.LineType = 'I') and (SL_Detail.PO='", PO, "') and (SL_Detail.Line='", Line, "') GROUP BY SL_Detail.PO, SL_Detail.Line, SL_Detail.Item, SL_Detail.ShipQty, SL_Detail.Description, SL_Packing.Carton, SL_Packing.BulkCarton, SL_Packing.Skid order by SL_Packing.Carton");
                }

                // Check if this item has components
                // sdsPacking.SelectCommand = string.Concat("SELECT SL_Detail.PO, SL_Detail.Line, SL_Detail.Item, SL_Detail.Description, SL_Detail.ShipQty, SL_Packing.Carton, SL_Packing.BulkCarton, SL_Packing.Skid FROM SL_Packing INNER JOIN SL_Detail ON SL_Packing.Vendor = SL_Detail.Vendor and SL_Packing.PO = SL_Detail.PO AND SL_Packing.Line = SL_Detail.Line WHERE (SL_Detail.LineType = 'I') and (SL_Detail.PO='", PO, "') and (SL_Detail.Line='", Line, "') GROUP BY SL_Detail.PO, SL_Detail.Line, SL_Detail.Item, SL_Detail.ShipQty, SL_Detail.Description, SL_Packing.Carton, SL_Packing.BulkCarton, SL_Packing.Skid order by SL_Packing.Carton");
                DataView dv = (DataView)sdsPacking.Select(DataSourceSelectArguments.Empty);
                if(dv.Count != 0)
                {

                }
                else
                {
                    lblMessage.Text = "No packing information found.";
                }

                gvPacking.DataBind();
                hlComponents.NavigateUrl = string.Concat("./Components.aspx?PO=", PO, "&Line=", Line);
                hlPOByItem.NavigateUrl = string.Concat("./Default.aspx?PO=", PO);
            }
            else
            {
                // Make sure select command is up to date
                // Are we packing a specific line?
                if (Line.Trim() == "")
                {
                    sdsPacking.SelectCommand = string.Concat("SELECT SL_Detail.PO, SL_Detail.Line, SL_Detail.Item, SL_Detail.Description, SL_Detail.ShipQty, SL_Packing.Carton, SL_Packing.BulkCarton, SL_Packing.Skid FROM SL_Packing INNER JOIN SL_Detail ON SL_Packing.Vendor = SL_Detail.Vendor and SL_Packing.PO = SL_Detail.PO AND SL_Packing.Line = SL_Detail.Line WHERE (SL_Detail.LineType = 'I') and (SL_Detail.PO='", PO, "') GROUP BY SL_Detail.PO, SL_Detail.Line, SL_Detail.Item, SL_Detail.ShipQty, SL_Detail.Description, SL_Packing.Carton, SL_Packing.BulkCarton, SL_Packing.Skid order by SL_Packing.Carton");
                }
                else
                {
                    sdsPacking.SelectCommand = string.Concat("SELECT SL_Detail.PO, SL_Detail.Line, SL_Detail.Item, SL_Detail.Description, SL_Detail.ShipQty, SL_Packing.Carton, SL_Packing.BulkCarton, SL_Packing.Skid FROM SL_Packing INNER JOIN SL_Detail ON SL_Packing.Vendor = SL_Detail.Vendor and SL_Packing.PO = SL_Detail.PO AND SL_Packing.Line = SL_Detail.Line WHERE (SL_Detail.LineType = 'I') and (SL_Detail.PO='", PO, "') and (SL_Detail.Line='", Line, "') GROUP BY SL_Detail.PO, SL_Detail.Line, SL_Detail.Item, SL_Detail.ShipQty, SL_Detail.Description, SL_Packing.Carton, SL_Packing.BulkCarton, SL_Packing.Skid order by SL_Packing.Carton");
                }

                // sdsPacking.SelectCommand = string.Concat("SELECT SL_Detail.PO, SL_Detail.Line, SL_Detail.Item, SL_Detail.Description, SL_Detail.ShipQty, SL_Packing.Carton, SL_Packing.BulkCarton, SL_Packing.Skid FROM SL_Packing INNER JOIN SL_Detail ON SL_Packing.Vendor = SL_Detail.Vendor and SL_Packing.PO = SL_Detail.PO AND SL_Packing.Line = SL_Detail.Line WHERE (SL_Detail.LineType = 'I') and (SL_Detail.PO='", lblPO.Text, "') and (SL_Detail.Line='", lblLineNo.Text, "') GROUP BY SL_Detail.PO, SL_Detail.Line, SL_Detail.Item, SL_Detail.ShipQty, SL_Detail.Description, SL_Packing.Carton, SL_Packing.BulkCarton, SL_Packing.Skid order by SL_Packing.Carton");
                lblMessage.Text = "";
            }
        }

        protected void gvPacking_Databound(object sender, GridViewRowEventArgs e)
        {
            TextBox tb;

            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                tb = (TextBox)e.Row.FindControl("tbSkid");
                tb.Text = DataBinder.Eval(e.Row.DataItem, "Skid").ToString();
                tb.ToolTip = tb.Text;
                if(tb.Text == "0")
                {
                    tb.Text = "";
                }
            }

            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                tb = (TextBox)e.Row.FindControl("tbBulk");
                tb.Text = DataBinder.Eval(e.Row.DataItem, "BulkCarton").ToString();
                tb.ToolTip = tb.Text;
                if (tb.Text == "0")
                {
                    tb.Text = "";
                }
            }

        }

        protected bool ProcessSkid(GridViewRow gvr)
        {
            ImageButton ib;
            string s;
            int SkidNo, BulkNo, CartonNo, Seq;
            int CurrentSkid = 0, CurrentSkidSN = 0;
            int CurrentBulk = 0, CurrentBulkSN = 0;
            decimal CartonSN, SkidSN, BulkSN;
            TextBox tb;
            HyperLink hl;
            Label lbl;
            string sqlCmdDMSource;
            SqlConnection scDMSource = new SqlConnection(ConfigurationManager.ConnectionStrings["DMSourceConnectionString"].ConnectionString);
            SqlCommand scmdDMSource;
            SqlTransaction srcTransaction;
            SqlDataReader sdrDMSource;
            bool bSkidExists = false;
            bool bBulkExists = false;


            tb = (TextBox)gvr.FindControl("tbSkid");
            lbl = (Label)gvr.FindControl("lblStatus");

            // Get the current Skid for this row
            if (int.TryParse(tb.ToolTip, out CurrentSkid))
            {

            }
            else
            {
                CurrentSkid = 0;
                lblMessage.Text = "Error Reading Skid #";
                return true;
            }

            if (int.TryParse(tb.Text, out SkidNo))
            {

            }
            else
            {
                SkidNo = 0;
                // return false;
            }

            tb = (TextBox)gvr.FindControl("tbBulk");
            // Get the current Skid for this row
            if (int.TryParse(tb.ToolTip, out CurrentBulk))
            {

            }
            else
            {
                CurrentBulk = 0;
                lbl.Text = "Error reading Bulk Carton #";
                return true;
            }

            if (int.TryParse(tb.Text, out BulkNo))
            {

            }
            else
            {
                BulkNo = 0;
                // return false;
            }


            // Are we re-assigning the skid # / Bulk # here?
            if ((CurrentSkid != SkidNo) || (CurrentBulk != BulkNo))
            {
                if (SkidNo > 0)
                {
                    CurrentSkidSN = DataClass.CheckSkidLabel(lblPO.Text, SkidNo);
                    if (CurrentSkidSN > 0) bSkidExists = true;
                }
                else
                {
                    // If Skid is 0, we aren't going to create it.
                    bSkidExists = true;
                }

                if (BulkNo > 0)
                {
                    CurrentBulkSN = DataClass.CheckBulkLabel(lblPO.Text, BulkNo);
                    if (CurrentBulkSN > 0) bBulkExists = true;
                }
                else
                {
                    // If Bulk is 0, we aren't going to create it.
                    bBulkExists = true;
                }

                // Read out carton #
                if (int.TryParse(gvr.Cells[2].Text, out CartonNo))
                {

                }
                else
                {
                    lbl.Text = "Error reading Carton #";
                    return true;
                }

                CartonSN = DataClass.GetCartonSN(lblPO.Text, CartonNo);
                scDMSource.Open();
                srcTransaction = scDMSource.BeginTransaction();
                try
                {

                    sqlCmdDMSource = string.Concat("Update SL_Packing set Skid=", SkidNo.ToString(), ", BulkCarton=", BulkNo.ToString(), " Where (Vendor=", VendorList, ") and PO='", lblPO.Text, "' and Carton=", CartonNo);
                    scmdDMSource = new SqlCommand(sqlCmdDMSource, scDMSource);
                    scmdDMSource.Transaction = srcTransaction;
                    if (scmdDMSource.ExecuteNonQuery() < 1)
                    {
                        tb.Text = "";
                        lbl.Text = "Critical Error Updating Packing Records.";
                        srcTransaction.Rollback();
                        scDMSource.Close();
                        return true;
                    }
                    else
                    {
                        // Then update skid # on this CartonNo
                        sqlCmdDMSource = string.Concat("Update SL_Labels set Skid=", SkidNo.ToString(), ", BulkCarton=", BulkNo.ToString(), " where (Vendor=", VendorList, ") and PO='", lblPO.Text, "' and Carton=", CartonNo.ToString());
                        scmdDMSource = new SqlCommand(sqlCmdDMSource, scDMSource);
                        scmdDMSource.Transaction = srcTransaction;
                        if (scmdDMSource.ExecuteNonQuery() < 1)
                        {
                            tb.Text = "";
                            lbl.Text = string.Concat("Critical Error : Skid/Bulk on Carton # ", CartonNo.ToString());
                            srcTransaction.Rollback();
                            scDMSource.Close();
                            return true;
                        }

                        if (!bBulkExists)
                        {
                            BulkSN = DataClass.GetNextBulk(lblPO.Text, BulkNo);
                            // Now insert Label record into SL_Labels table
                            sqlCmdDMSource = string.Concat("Insert Into SL_Labels (SkidSN, BulkSN, PackingSeq, Vendor, PO, Description, Seq, Skid, BulkCarton, Carton, Status, PrintStatus) values (0, ", BulkSN.ToString(), ", 0, ", VendorList, ", '", lblPO.Text, "', 'Bulk Label ", BulkNo.ToString(), "', 0, ", SkidNo.ToString(), ", ", BulkNo.ToString(), ",0,'A','')");
                            scmdDMSource = new SqlCommand(sqlCmdDMSource, scDMSource);
                            scmdDMSource.Transaction = srcTransaction;
                            if (scmdDMSource.ExecuteNonQuery() < 1)
                            {
                                tb.Text = "";
                                lbl.Text = "Critical Error Creating Bulk Label Record.";
                                srcTransaction.Rollback();
                                scDMSource.Close();
                                return true;
                            }
                        }
                        else
                        {
                            // We are removing from a skid, check if anything is left on the skid and remove the label if necessary.
                            sqlCmdDMSource = string.Concat("Select * from SL_Labels where (Vendor=", VendorList, ") and PO='", lblPO.Text, "' and BulkCarton=", CurrentBulk.ToString(), " and Carton<>0");
                            scmdDMSource = new SqlCommand(sqlCmdDMSource, scDMSource);
                            scmdDMSource.Transaction = srcTransaction;
                            sdrDMSource = scmdDMSource.ExecuteReader();
                            if (!sdrDMSource.HasRows)
                            {
                                sdrDMSource.Close();
                                // There is nothing else on this BulkCarton, time to remove it's label
                                sqlCmdDMSource = string.Concat("Delete from SL_Labels where (Vendor=", VendorList, ") and PO='", lblPO.Text, "' and BulkCarton=", CurrentBulk.ToString(), " and Carton=0");
                                scmdDMSource = new SqlCommand(sqlCmdDMSource, scDMSource);
                                scmdDMSource.Transaction = srcTransaction;
                                scmdDMSource.ExecuteNonQuery();

                                // Now delete the skid
                                sqlCmdDMSource = string.Concat("Delete from SL_Bulk where (Vendor=", VendorList, ") and PO='", lblPO.Text, "' and BulkCarton=", CurrentBulk.ToString());
                                scmdDMSource = new SqlCommand(sqlCmdDMSource, scDMSource);
                                scmdDMSource.Transaction = srcTransaction;
                                scmdDMSource.ExecuteNonQuery();
                            }
                            else
                            {
                                sdrDMSource.Close();
                            }

                        }

                        // Updated Packing information with the Skid, create the label record
                        if (!bSkidExists)
                        {
                            //// OK, Skid doesn't exist, get Seq from the Carton we are updating
                            //sqlCmdDMSource = string.Concat("Select Seq from SL_Labels where (Vendor=", VendorList, ") and PO='", lblPO.Text, "' and Carton=", CartonNo.ToString());
                            //scmdDMSource = new SqlCommand(sqlCmdDMSource, scDMSource);
                            //scmdDMSource.Transaction = srcTransaction;
                            //sdrDMSource = scmdDMSource.ExecuteReader();
                            //if (sdrDMSource.HasRows)
                            //{
                            //    sdrDMSource.Read();
                            //    Seq = (int)sdrDMSource["Seq"];
                            //    sdrDMSource.Close();
                            //}
                            //else
                            //{
                            //    Seq = 1;
                            //}
                            //// Then update sequence of all cartons >= selected CartonNo
                            //sqlCmdDMSource = string.Concat("Update SL_Labels set Seq=Seq+1 where (Vendor=", VendorList, ") and PO='", lblPO.Text, "' and Seq>=", Seq.ToString());
                            //scmdDMSource = new SqlCommand(sqlCmdDMSource, scDMSource);
                            //scmdDMSource.Transaction = srcTransaction;
                            //if (scmdDMSource.ExecuteNonQuery() < 1)
                            //{
                            //    tb.Text = "";
                            //    lbl.Text = "Critical Error Updating Carton Label Records: Sequence Insert.";
                            //    srcTransaction.Rollback();
                            //    scDMSource.Close();
                            //    return true;
                            //}
                            //else
                            //{
                                SkidSN = DataClass.GetNextSkid(lblPO.Text, SkidNo);
                                // Now insert Label record into SL_Labels table
                                sqlCmdDMSource = string.Concat("Insert Into SL_Labels (SkidSN, BulkSN, PackingSeq, Vendor, PO, Description, Seq, Skid, BulkCarton, Carton, Status, PrintStatus) values (", SkidSN.ToString(), ", 0, 0, ", VendorList, ", '", lblPO.Text, "', 'Skid Label ", SkidNo.ToString(), "', 0, ", SkidNo.ToString(), ", ", BulkNo.ToString(), ",0,'A','')");
                                scmdDMSource = new SqlCommand(sqlCmdDMSource, scDMSource);
                                scmdDMSource.Transaction = srcTransaction;
                                if (scmdDMSource.ExecuteNonQuery() < 1)
                                {
                                    tb.Text = "";
                                    lbl.Text = "Critical Error Creating Skid Label Record.";
                                    srcTransaction.Rollback();
                                    scDMSource.Close();
                                    return true;
                                }

                                // Update Skid # on Label
                                // Commented 6/18/2020, will insert upon printing
                                //sqlCmdDMSource = string.Concat("Insert into SL_Operations (SerialNo, Vendor, PO, Line, Item, Component, Description, ShipQty, Operation, CartonNo, OfCartons, SkidSerialNo, Status) Values (", CartonSN.ToString(), ", ", VendorList, ", '", lblPO.Text, "', '', '', '', '', 0, 'U', 0, 0, ", SkidSN.ToString(), ", 'N') ");
                                //scmdDMSource = new SqlCommand(sqlCmdDMSource, scDMSource);
                                //scmdDMSource.Transaction = srcTransaction;
                                //scmdDMSource.ExecuteNonQuery();
                            // }
                        }
                        else
                        {
                            // We aren't creating a skid, assign the existing found Skid #
                            int UpdateSkidSN = 0;
                            if (SkidNo != 0)
                            {
                                UpdateSkidSN = CurrentSkidSN;
                                // If we aren't creating the Skid, find it's serial # and then update the label
                                // Now insert Label record into SL_Labels table
                                // Update Skid # on Label
                            }
                            else
                            {
                                // We are removing from a skid, check if anything is left on the skid and remove the label if necessary.
                                sqlCmdDMSource = string.Concat("Select * from SL_Labels where (Vendor=", VendorList, ") and PO='", lblPO.Text, "' and Skid=", CurrentSkid.ToString(), " and Carton<>0");
                                scmdDMSource = new SqlCommand(sqlCmdDMSource, scDMSource);
                                scmdDMSource.Transaction = srcTransaction;
                                sdrDMSource = scmdDMSource.ExecuteReader();
                                if (!sdrDMSource.HasRows)
                                {
                                    sdrDMSource.Close();

                                    // Nothing left on the skid, lets remove and resequence
                                    // First get the sequence of this skid #
                                    //sqlCmdDMSource = string.Concat("Select * from SL_Labels where (Vendor=", VendorList, ") and PO='", lblPO.Text, "' and Skid=", CurrentSkid.ToString(), " and Carton=0");
                                    //scmdDMSource = new SqlCommand(sqlCmdDMSource, scDMSource);
                                    //scmdDMSource.Transaction = srcTransaction;
                                    //sdrDMSource = scmdDMSource.ExecuteReader();
                                    //if (sdrDMSource.HasRows)
                                    //{
                                    //    sdrDMSource.Read();
                                    //    Seq = (int)sdrDMSource["Seq"];

                                    //    // Then update sequence of all cartons >= selected CartonNo
                                    //    sqlCmdDMSource = string.Concat("Update SL_Labels set Seq=Seq-1 where (Vendor=", VendorList, ") and PO='", lblPO.Text, "' and Seq>", Seq.ToString());
                                    //    scmdDMSource = new SqlCommand(sqlCmdDMSource, scDMSource);
                                    //    scmdDMSource.Transaction = srcTransaction;
                                    //    if (scmdDMSource.ExecuteNonQuery() < 1)
                                    //    {
                                    //        tb.Text = "";
                                    //        lbl.Text = "Critical Error Updating Carton Label Records: Sequence Removal.";
                                    //        srcTransaction.Rollback();
                                    //        scDMSource.Close();
                                    //        return true;
                                    //    }
                                    //}
                                    //sdrDMSource.Close();


                                    // There is nothing else on this skid, time to remove it's label
                                    sqlCmdDMSource = string.Concat("Delete from SL_Labels where (Vendor=", VendorList, ") and PO='", lblPO.Text, "' and Skid=", CurrentSkid.ToString(), " and Carton=0");
                                    scmdDMSource = new SqlCommand(sqlCmdDMSource, scDMSource);
                                    scmdDMSource.Transaction = srcTransaction;
                                    scmdDMSource.ExecuteNonQuery();

                                    // Now delete the skid
                                    sqlCmdDMSource = string.Concat("Delete from SL_Skids where (Vendor=", VendorList, ") and PO='", lblPO.Text, "' and Skid=", CurrentSkid.ToString());
                                    scmdDMSource = new SqlCommand(sqlCmdDMSource, scDMSource);
                                    scmdDMSource.Transaction = srcTransaction;
                                    scmdDMSource.ExecuteNonQuery();
                                }
                                else
                                {
                                    sdrDMSource.Close();
                                }
                            }
                            // Commented 6/18/2020, SL_Operations will be updated upon printing
                            //sqlCmdDMSource = string.Concat("Insert into SL_Operations (SerialNo, Vendor, PO, Line, Item, Component, Description, ShipQty, Operation, CartonNo, OfCartons, SkidSerialNo, Status) Values (", CartonSN.ToString(), ", ", VendorList, ", '", lblPO.Text, "', '', '', '', '', 0, 'U', 0, 0, ", UpdateSkidSN.ToString(), ", 'N') ");
                            //scmdDMSource = new SqlCommand(sqlCmdDMSource, scDMSource);
                            //scmdDMSource.Transaction = srcTransaction;
                            //scmdDMSource.ExecuteNonQuery();
                        }
                    }
                    // Close SQL Connection
                    srcTransaction.Commit();
                    scDMSource.Close();
                    if ((SkidNo == 0) && (BulkNo == 0))
                    {
                        lbl.Text = string.Concat("Carton ", CartonNo.ToString(), " removed from Bulk/Skid.");
                    }
                    else
                    {
                        lbl.Text = string.Concat("Carton ", CartonNo.ToString(), " added to Bulk # ", BulkNo.ToString(), " Skid # ", SkidNo.ToString());
                    }
                }
                catch (Exception ex)
                {
                    lbl.Text = "Critical Unhandled Exception.";
                    srcTransaction.Rollback();
                    scDMSource.Close();
                }
                tb.ToolTip = SkidNo.ToString();
            }
            else
            {
                lbl.Text = "";
            }
            return false;
        }

        protected void BulkChanged(object sender, EventArgs e)
        {
            int i;
            GridViewRow gvr;
            // TextBox tb;

            //tb = (TextBox)gvr.FindControl("tbSkid");
            //lbl = (Label)gvr.FindControl("lblStatus");

            //// Get the current Skid for this row
            //if (int.TryParse(tb.ToolTip, out CurrentSkid))
            //{

            //}
            //else
            //{
            //    lbl.Text = "Error reading Skid #";
            //    return true;
            //}

            //if (int.TryParse(tb.Text, out SkidNo))
            //{

            //}
            //else
            //{
            //    SkidNo = 0;
            //    // return false;
            //}

            //tb = (TextBox)gvr.FindControl("tbBulk");
            //// Get the current Skid for this row
            //if (int.TryParse(tb.ToolTip, out CurrentBulk))
            //{

            //}
            //else
            //{
            //    CurrentBulk = 0;
            //    lbl.Text = "Error reading Bulk Carton #";
            //    return true;
            //}

            //if (int.TryParse(tb.Text, out BulkNo))
            //{

            //}
            //else
            //{
            //    BulkNo = 0;
            //    // return false;
            //}

            //for (i = 0; i < gvPacking.Rows.Count; i++)
            //{
            //    gvr = gvPacking.Rows[i];
            //    if (gvr.RowType == DataControlRowType.DataRow)
            //    {

            //    }
            //}
        }

        protected void SkidChanged(object sender, EventArgs e)
        {
            int i;
            int RowIndex;
            GridViewRow gvr;
            TextBox tb;
            Label lbl;
            int SkidNo, CurrentSkid;
            int BulkNo, CurrentBulk;

            gvr = ((TextBox)sender).NamingContainer as GridViewRow;
            RowIndex = gvr.RowIndex;

            // Grab our skid information for this row
            tb = (TextBox)gvr.FindControl("tbSkid");
            lbl = (Label)gvr.FindControl("lblStatus");

            // Get the current Skid for this row
            if (int.TryParse(tb.ToolTip, out CurrentSkid))
            {

            }
            else
            {
                lbl.Text = "Error reading Skid #";
                return;
            }
            if (int.TryParse(tb.Text, out SkidNo))
            {

            }
            else
            {
                SkidNo = 0;
                // return false;
            }

            // If the skid hasn't changed, exit
            if (CurrentSkid == SkidNo) return;


            // Grab our bulk carton # for this row
            tb = (TextBox)gvr.FindControl("tbBulk");
            // Get the current Bulk Ctn for this row
            if (int.TryParse(tb.Text, out CurrentBulk))
            {

            }
            else
            {
                CurrentBulk = 0;
                lbl.Text = "Error reading Bulk Carton #";
                return;
            }

            // If we aren't in a bulk carton, return
            if (CurrentBulk == 0) return;

            // Loop through the rows, find any other Items on the same Bulk Carton and place on the same skid
            for (i = 0; i < gvPacking.Rows.Count; i++)
            {
                gvr = gvPacking.Rows[i];
                if (gvr.RowType == DataControlRowType.DataRow)
                {
                    // Grab our bulk carton # for this row
                    tb = (TextBox)gvr.FindControl("tbBulk");
                    // Get the current Bulk Ctn for this row
                    if (int.TryParse(tb.Text, out BulkNo))
                    {
                        // Same Bulk carton, re-assign to a new skid
                        if(BulkNo == CurrentBulk)
                        {
                            tb = (TextBox)gvr.FindControl("tbSkid");
                            if(tb != null)
                            {
                                tb.Text = SkidNo.ToString();    
                            }
                        }
                    }
                }
            }
        }

        protected void SaveSkids(Object sender, EventArgs e)
        {
            int i;
            GridViewRow gvr;
            bool bError;

            for(i = 0; i < gvPacking.Rows.Count; i++)
            {
                gvr = gvPacking.Rows[i];
                if (gvr.RowType == DataControlRowType.DataRow)
                {
                    bError = ProcessSkid(gvr);
                    if(bError)
                    {
                        lblMessage.Text += "Errors occurred, halting Assignment.";
                        break;
                    }
                }
            }
        }

        //protected void SkidChanged(object sender, EventArgs e)
        //{

        //    ImageButton ib;
        //    GridViewRow gvr;
        //    DataRowView drv;
        //    string s;
        //    int SkidNo, CartonNo, Seq;
        //    decimal CartonSN, SkidSN;
        //    TextBox tb;
        //    HyperLink hl;
        //    string sqlCmdDMSource;
        //    SqlConnection scDMSource = new SqlConnection(ConfigurationManager.ConnectionStrings["DMSourceConnectionString"].ConnectionString);
        //    SqlCommand scmdDMSource;
        //    SqlTransaction srcTransaction;
        //    SqlDataReader sdrDMSource;
        //    bool bSkidExists = false;


        //    tb = (TextBox)sender;
        //    if (int.TryParse(tb.Text, out SkidNo))
        //    {
        //        gvr = (GridViewRow)((TextBox)sender).NamingContainer;

        //        // Read out carton #
        //        if (int.TryParse(gvr.Cells[1].Text, out CartonNo))
        //        {
        //            if(SkidNo > 0)
        //            {
        //                int CurrentSkidNo = DataClass.CheckSkidLabel(lblPO.Text, SkidNo);
        //                if (CurrentSkidNo != 0) bSkidExists = true;
        //            }
        //            else
        //            {
        //                // If Skid is 0, we aren't going to create it.
        //                bSkidExists = true;
        //            }

        //            CartonSN = DataClass.GetCartonSN(lblPO.Text, CartonNo);

        //            scDMSource.Open();
        //            srcTransaction = scDMSource.BeginTransaction();
        //            try
        //            {
        //                sqlCmdDMSource = string.Concat("Update SL_Packing set Skid=", SkidNo.ToString(), " Where PO='", lblPO.Text, "' and Carton=", CartonNo);
        //                scmdDMSource = new SqlCommand(sqlCmdDMSource, scDMSource);
        //                scmdDMSource.Transaction = srcTransaction;
        //                if (scmdDMSource.ExecuteNonQuery() < 1)
        //                {
        //                    tb.Text = "";
        //                    lblMessage.Text = "Critical Error Updating Packing Records.";
        //                    srcTransaction.Rollback();
        //                    scDMSource.Close();
        //                    return;
        //                }
        //                else
        //                {
        //                    // Then update skid # on this CartonNo
        //                    sqlCmdDMSource = string.Concat("Update SL_Labels set Skid=", SkidNo.ToString(), " where PO='", lblPO.Text, "' and Carton=", CartonNo.ToString());
        //                    scmdDMSource = new SqlCommand(sqlCmdDMSource, scDMSource);
        //                    scmdDMSource.Transaction = srcTransaction;
        //                    if (scmdDMSource.ExecuteNonQuery() < 1)
        //                    {
        //                        tb.Text = "";
        //                        lblMessage.Text = string.Concat("Critical Error Skid on Carton # ", CartonNo.ToString());
        //                        srcTransaction.Rollback();
        //                        scDMSource.Close();
        //                        return;
        //                    }


        //                    // Updated Packing information with the Skid, create the label record
        //                    if (!bSkidExists)
        //                    {
        //                        // OK, Skid doesn't exist, get Seq from the Carton we are updating
        //                        sqlCmdDMSource = string.Concat("Select Seq from SL_Labels where PO='", lblPO.Text, "' and Carton=", CartonNo.ToString());
        //                        scmdDMSource = new SqlCommand(sqlCmdDMSource, scDMSource);
        //                        scmdDMSource.Transaction = srcTransaction;
        //                        sdrDMSource = scmdDMSource.ExecuteReader();
        //                        if(sdrDMSource.HasRows)
        //                        {
        //                            sdrDMSource.Read();
        //                            Seq = (int)sdrDMSource["Seq"];
        //                            sdrDMSource.Close();
        //                        }
        //                        else
        //                        {
        //                            Seq = 1;
        //                        }

        //                        // Then update sequence of all cartons >= selected CartonNo
        //                        sqlCmdDMSource = string.Concat("Update SL_Labels set Seq=Seq+1 where PO='", lblPO.Text, "' and Seq>=", Seq.ToString());
        //                        scmdDMSource = new SqlCommand(sqlCmdDMSource, scDMSource);
        //                        scmdDMSource.Transaction = srcTransaction;
        //                        if (scmdDMSource.ExecuteNonQuery() < 1)
        //                        {
        //                            tb.Text = "";
        //                            lblMessage.Text = "Critical Error Updating Carton Label Records.";
        //                            srcTransaction.Rollback();
        //                            scDMSource.Close();
        //                            return;
        //                        }
        //                        else
        //                        {
        //                            // Get next Skid Serial #
        //                            SkidSN = DataClass.GetNextSkid(lblPO.Text, SkidNo);
        //                            if(SkidSN == 0)
        //                            {
        //                                lblMessage.Text = "Critical Error Generating Skid Label Record.";
        //                                srcTransaction.Rollback();
        //                                scDMSource.Close();
        //                                return;
        //                            }
        //                            // Now insert Label record into SL_Labels table
        //                            sqlCmdDMSource = string.Concat("Insert Into SL_Labels (SkidSN, PackingSeq, PO, Line, Description, Seq, Skid, Carton, Status, PrintStatus) values (", SkidSN.ToString(), ", 0, '", lblPO.Text, "', '0', 'Skid Label',", Seq.ToString(), ",", SkidNo.ToString(), ",0,'A','')");
        //                            scmdDMSource = new SqlCommand(sqlCmdDMSource, scDMSource);
        //                            scmdDMSource.Transaction = srcTransaction;
        //                            if (scmdDMSource.ExecuteNonQuery() < 1)
        //                            {
        //                                tb.Text = "";
        //                                lblMessage.Text = "Critical Error Creating Skid Label Record.";
        //                                srcTransaction.Rollback();
        //                                scDMSource.Close();
        //                                return;
        //                            }
        //                            // The following snippet isn't used because we are generating the Skid SN above
        //                            //// Get the CartonID rom last insert
        //                            //sqlCmdDMSource = string.Concat("Select @@Identity as newId from SL_Labels");
        //                            //scmdDMSource = new SqlCommand(sqlCmdDMSource, scDMSource);
        //                            //scmdDMSource.Transaction = srcTransaction;
        //                            //SkidSN = Convert.ToInt32(scmdDMSource.ExecuteScalar());

        //                            // Update Skid # on Label
        //                            sqlCmdDMSource = string.Concat("Insert into SL_Operations (SerialNo, PO, Line, Item, Component, Description, ShipQty, Operation, CartonNo, OfCartons, SkidSerialNo, Status) Values (", CartonSN.ToString(), ", '", lblPO.Text, "', '', '', '', '', 0, 'U', 0, 0, ", SkidSN.ToString(), ", 'N') ");
        //                            scmdDMSource = new SqlCommand(sqlCmdDMSource, scDMSource);
        //                            scmdDMSource.Transaction = srcTransaction;
        //                            scmdDMSource.ExecuteNonQuery();
        //                        }
        //                    }
        //                    else
        //                    {
        //                        // If we are removing from a Skid, check that there are still products on that skid
        //                        if (SkidNo == 0)
        //                        {

        //                        }
        //                    }
        //                }
        //                // Close SQL Connection
        //                srcTransaction.Commit();
        //                scDMSource.Close();
        //                if(SkidNo == 0)
        //                {
        //                    lblMessage.Text = string.Concat("Carton ", CartonNo.ToString(), " removed from Skid.");
        //                }
        //                else
        //                {
        //                    lblMessage.Text = string.Concat("Carton ", CartonNo.ToString(), " added to Skid # ", SkidNo.ToString());
        //                }
        //            }
        //            catch (Exception ex)
        //            {
        //                lblMessage.Text = "Critical Unhandled Exception.";
        //                srcTransaction.Rollback();
        //                scDMSource.Close();
        //            }
        //        }
        //        else
        //        {
        //            lblMessage.Text = "Error reading Carton #";
        //            return;
        //        }

        //    }
        //    else
        //    {
        //        lblMessage.Text = "Error reading Skid #";
        //        return;
        //    }
        //}

    }
}