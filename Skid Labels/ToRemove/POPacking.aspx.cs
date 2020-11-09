using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;

namespace Skid_Labels
{
    public partial class POPacking : Page
    {
        //int VendorID = 70166;
        //string VendorList = "70166";
        string VendorList;
        decimal LastSN = 0;

        protected void Page_Load(object sender, EventArgs e)
        {
            string PO= "";

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

            // Update the return to main screen hyperlink

            hlPOByItem.NavigateUrl = string.Concat("./Default.aspx?PO=", PO);

            LastSN = 0;
            if (!IsPostBack)
            {
                // Populate header fields
                lblPO.Text = PO;

                sdsLabels.SelectCommand = string.Concat("SELECT SL_Labels.LabelSN, SL_Labels.SkidSN, SL_Labels.PackingSeq, SL_Labels.PO, SL_Labels.Description, SL_Labels.Seq, SL_Labels.Skid, SL_Labels.Carton, SL_Labels.Status, SL_Labels.PrintStatus, SL_Labels.Line, SL_Packing.Component, SL_Packing.Item, SL_Packing.ShipQty, SL_Packing.BOMQty FROM SL_Labels FULL OUTER JOIN SL_Packing ON SL_Labels.PackingSeq = SL_Packing.Seq WHERE((SL_Labels.Vendor=", VendorList, ") and SL_Labels.PO = '", lblPO.Text, "') ORDER BY SL_Labels.LabelSN");
                gvLabels.DataBind();
                // Check if this item has components
                sdsPrinting.SelectCommand = string.Concat("SELECT SL_Packing.PO, SL_Packing.Line, SL_Packing.Seq, SL_Packing.ShipQty, SL_Packing.BOMQty, SL_Packing.Item, SL_Packing.Component, SL_Labels.Description, SL_Labels.Carton, SL_Labels.Skid, SL_Labels.LabelSN, SL_Labels.SkidSN, SL_Labels.PackingSeq, SL_Labels.Status, SL_Labels.PrintStatus FROM SL_Packing FULL OUTER JOIN SL_Labels ON SL_Packing.Vendor = SL_Labels.Vendor and SL_Packing.PO = SL_Labels.PO and SL_Packing.Line = SL_Labels.Line and SL_Packing.Carton = SL_Labels.Carton WHERE (SL_Labels.Vendor=", VendorList, ") and SL_Labels.PO='", lblPO.Text, "' ORDER BY Line,Carton");
                // sdsPrinting.SelectCommand = string.Concat("SELECT SL_Labels.LabelSN, SL_Labels.SkidSN, SL_Labels.PackingSeq, SL_Labels.PO, SL_Labels.Description, SL_Labels.Seq, SL_Labels.Skid, SL_Labels.Carton, SL_Labels.Status, SL_Labels.PrintStatus, SL_Packing.Line, SL_Packing.Component, SL_Packing.Item, SL_Packing.ShipQty, SL_Packing.BOMQty FROM SL_Labels FULL OUTER JOIN SL_Packing ON SL_Labels.PackingSeq = SL_Packing.Seq WHERE((SL_Labels.Vendor=", VendorList, ") and SL_Labels.PO = '", lblPO.Text, "') ORDER BY SL_Labels.Seq");
                gvPrinting.DataBind();

                lbPrintAll.OnClientClick = string.Concat("PrintLabel(null, ", gvLabels.Rows.Count,");");
                //hlPOByItem.NavigateUrl = string.Concat("/Default.aspx?PO=", PO);
            }
            else
            {
                lblMessage.Text = "";
            }
        }


        protected void btnPrint(object sender, EventArgs e)
        {
            string sqlCommandSrc;
            SqlCommand command;
            SqlDataReader sdr, sdrDMTarget;
            LinkButton lb;
            lb = (LinkButton)sender;
            DataClass.MarkPrinted(lb.ToolTip);


            SqlConnection sqlConnection2 = new SqlConnection(ConfigurationManager.ConnectionStrings["DMTargetConnectionString"].ConnectionString);
            SqlConnection sqlConnection1 = new SqlConnection(ConfigurationManager.ConnectionStrings["DMSourceConnectionString"].ConnectionString);

            // Get the PO/Line # for this Carton
            sqlCommandSrc = string.Concat("Select * from SL_Labels where LabelSN=", lb.ToolTip.ToString());
            sqlCommandSrc = string.Concat("SELECT SL_Labels.LabelSN, SL_Labels.SkidSN, SL_Labels.PackingSeq, SL_Labels.PO, SL_Labels.Description, SL_Labels.Seq, SL_Labels.Skid, SL_Labels.Carton, SL_Labels.Status, SL_Labels.PrintStatus, SL_Labels.Line, SL_Packing.Component, SL_Packing.Item, SL_Packing.ShipQty, SL_Packing.BOMQty FROM SL_Labels FULL OUTER JOIN SL_Packing ON SL_Labels.PackingSeq = SL_Packing.Seq WHERE((SL_Labels.Vendor = ", VendorList, ") and SL_Labels.PO = '", lblPO.Text, "') ORDER BY SL_Labels.LabelSN");
              command = new SqlCommand(sqlCommandSrc, sqlConnection1);
            sdr = command.ExecuteReader();
            if (sdr.HasRows)
            {
                while(sdr.Read())
                {
                    int SkidNo, CurrentCarton, CartonCount, MinCarton, BoxNo;
                    decimal CartonSN, SkidSN;
                    string LineNo, PrintCarton;
                    SkidNo = (int)sdr["Skid"];
                    CartonSN = (decimal)sdr["LabelSN"];
                    SkidSN = (decimal)sdr["SkidSN"];
                    LineNo = sdr["Line"].ToString();
                    CurrentCarton = (int)sdr["Carton"];
                    // Seq = (int)drv["Seq"];

                    // Check if this is a skid label, pre-pend 'S' if it is
                    if ((SkidNo > 0) && (CurrentCarton == 0))
                    {
                        PrintCarton = string.Concat("S", SkidSN.ToString("000000000"));
                    }
                    else
                    {
                        PrintCarton = CartonSN.ToString("000000000");
                    }

                    CartonCount = DataClass.GetCartonCount(lblPO.Text, LineNo);
                    MinCarton = DataClass.GetFirstCarton(lblPO.Text, LineNo);
                    BoxNo = CurrentCarton - MinCarton + 1;
                    // If we are printint a Skid label, set the box # to 0 as well.
                    if (CartonCount == 0)
                    {
                        BoxNo = 0;
                    }

                    sqlConnection2.Open();
                    sqlCommandSrc = string.Concat("Select * from POORDHP where PHPONO = '", lblPO.Text, "'");
                    command = new SqlCommand(sqlCommandSrc, sqlConnection2);
                    sdrDMTarget = command.ExecuteReader();
                    if (sdrDMTarget.HasRows)
                    {
                        sdrDMTarget.Read();

                        string addr2 = (string)sdr["PHSAD2"];
                        // string Model;

                        // hl.NavigateUrl = String.Concat("/Component.aspx?PO=", txtSearch.Text, "&Item=", hl.Text.Trim);
                    }
                    else
                    {
                        sqlConnection1.Close();
                        sqlConnection2.Close();
                        lblMessage.Text = "PO Not Found";
                        return;
                    }
                }
            }
            sdr.Close();
            sqlConnection1.Close();
            sqlConnection2.Close();
        }

            protected void btnPrintAll_Click(object sender, EventArgs e)
        {
            LinkButton lb;
            GridViewRow gvr;

            for(int i = 0; i < gvPrinting.Rows.Count; i++)
            {
                gvr = gvPrinting.Rows[i];
                lb = (LinkButton)gvr.FindControl("btnPrint");
                if(lb != null)
                {
                    DataClass.MarkPrinted(lb.ToolTip);
                }
            }
        }


        protected void gvPrinting_Databound(object sender, GridViewRowEventArgs e)
        {
            LinkButton lb;
            DataRowView drv;
            // int Seq;
            string divID = "divLabel_";
            decimal LabelSN;
            bool found;
            GridViewRow gvr;
            Literal SN;

            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                drv = (DataRowView)e.Row.DataItem;

                LabelSN = (decimal)drv["LabelSN"];
                lb = (LinkButton)e.Row.FindControl("btnPrint");
                if (LabelSN != LastSN)
                {
                    // Set the last label
                    LastSN = LabelSN;
                    lb.ToolTip = drv["LabelSN"].ToString();
                    found = false;
                    for(int i = 0; i < gvLabels.Rows.Count -1; i++)
                    {
                        gvr = gvLabels.Rows[i];

                        SN = (Literal)gvr.FindControl("SN");
                        if(string.Compare(SN.Text, lb.ToolTip) == 0)
                        {
                            found = true;
                            divID = string.Concat("divLabel_", i.ToString());
                        }
                    }
                    // divID = string.Concat("divLabel_", e.Row.DataItemIndex.ToString());
                    if (String.Compare((string)drv["PrintStatus"], "P") == 0)
                    {
                        lb.Text = "Re-Print";
                    }
                    else
                    {
                        lb.Text = "Print";
                    }
                    if(found)
                    {
                        lb.OnClientClick = string.Concat("PrintLabel('MainContent_gvLabels_", divID, "', 1); return true;");
                    }
                }
                else
                {
                    lb.Visible = false;
                }
                // Display the company name in italics.
                // Seq = (int)drv["Seq"];
            }
        }

        protected void gvLabels_Databound(object sender, GridViewRowEventArgs e)
        {
            // LinkButton lb;
            DataRowView drv;
            Literal innerHtml, SN;
            TableCell cell;
            string LineNo;
            int CartonCount, MinCarton, CurrentCarton, BoxNo;
            // int Seq;
            int SkidNo;
            string divID;
            decimal CartonSN;
            decimal SkidSN;
            string PrintCarton;

            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                drv = (DataRowView)e.Row.DataItem;
                // Display the company name in italics.
                innerHtml = (Literal)e.Row.FindControl("innerHtml");
                SN = (Literal)e.Row.FindControl("SN");
                SkidNo = (int)drv["Skid"];
                CartonSN = (decimal)drv["LabelSN"];
                SkidSN = (decimal)drv["SkidSN"];
                LineNo = drv["Line"].ToString();
                CurrentCarton = (int)drv["Carton"];
                // Seq = (int)drv["Seq"];
                cell = e.Row.Cells[0];
                divID = string.Concat("divLabel_", e.Row.DataItemIndex.ToString());
                // divID = string.Concat("divLabel_", CartonSN.ToString());
                cell.Controls[0].ID = divID;
                SN.Text = CartonSN.ToString();

                // Check if this is a skid label, pre-pend 'S' if it is
                if ((SkidNo > 0) && (CurrentCarton == 0))
                {
                    PrintCarton = string.Concat("S", SkidSN.ToString("000000000"));
                }
                else
                {
                    PrintCarton = CartonSN.ToString("000000000");
                }

                CartonCount = DataClass.GetCartonCount(lblPO.Text, LineNo);
                MinCarton = DataClass.GetFirstCarton(lblPO.Text, LineNo);
                BoxNo = CurrentCarton - MinCarton + 1;
                // If we are printint a Skid label, set the box # to 0 as well.
                if (CartonCount == 0)
                {
                    BoxNo = 0;
                }

                // Read Header Fields
                string sqlCommandSrc;
                SqlConnection sqlConnection2 = new SqlConnection(ConfigurationManager.ConnectionStrings["DMTargetConnectionString"].ConnectionString);
                SqlDataReader sdr;

                sqlConnection2.Open();
                sqlCommandSrc = string.Concat("Select * from POORDHP where PHPONO = '", lblPO.Text, "'");
                SqlCommand command = new SqlCommand(sqlCommandSrc, sqlConnection2);
                sdr = command.ExecuteReader();
                if (sdr.HasRows)
                {
                    sdr.Read();

                    string addr2 = (string)sdr["PHSAD2"];
                    string Model;

                    innerHtml.Text = string.Concat("Vendor: ", DataClass.GetVendor((decimal)sdr["PHVEN1"]), "<br/><hr/>");
                    // Check if our Item is null, will be a skid label in this instance.
                    if (System.DBNull.Value.Equals(drv["Item"]))
                    {
                        Model = (string)drv["Description"];
                        innerHtml.Text += string.Concat(Model, "<br/>");
                    }
                    else
                    {
                        Model = (string)drv["Item"];
                        innerHtml.Text += string.Concat("Model: ", Model, "<br/>");
                    }

                    innerHtml.Text += "Finishes: <br/>";
                    innerHtml.Text += string.Concat("Qty: ", drv["ShipQty"], "<br/>");
                    innerHtml.Text += string.Concat("Box: ", BoxNo.ToString(), " of ", CartonCount.ToString());
                    if ((SkidNo != 0) && (BoxNo != 0)) {
                        innerHtml.Text += string.Concat(" - On Skid: ", SkidNo.ToString());
                    }
                    innerHtml.Text += string.Concat("<br/><hr/>");
                    innerHtml.Text += "Ship To: <br/>";
                    innerHtml.Text += string.Concat((string)sdr["PHSNAM"], "<br/>");
                    innerHtml.Text += string.Concat((string)sdr["PHSAD1"], "<br/>");
                    if (addr2.Trim().Length > 0)
                    {
                        innerHtml.Text += string.Concat(addr2, "<br/>");
                    }
                    innerHtml.Text += string.Concat(sdr["PHSCTY"], ", ", sdr["PHSSTA"], " ", sdr["PHSZIP"], "<br />");
                    innerHtml.Text += "Load: <br/>";
                    innerHtml.Text += string.Concat("PO NUMBER/LINE#: <B>", lblPO.Text, " / ", LineNo.ToString(), "<B/><br/>");
                    innerHtml.Text += string.Concat("ORDER /LINE#: <br/><hr/>");
                    innerHtml.Text += string.Concat("<center><img alt='Barcode Generator TEC-IT' src='https://barcode.tec-it.com/barcode.ashx?data=", PrintCarton, "&code=Code39&dpi=96&dataseparator=' /></center> ");

                    // hl.NavigateUrl = String.Concat("/Component.aspx?PO=", txtSearch.Text, "&Item=", hl.Text.Trim);
                }
                else
                {
                    sqlConnection2.Close();
                    lblMessage.Text = "PO Not Found";
                    return;
                }
                sqlConnection2.Close();

            }
        }

    }
}