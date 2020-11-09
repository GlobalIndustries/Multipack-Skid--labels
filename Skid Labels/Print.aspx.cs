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
using IronPdf;
using Microsoft.SqlServer.Server;

namespace Skid_Labels
{
    public partial class Print : Page
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

                //sdsLabels.SelectCommand = string.Concat("SELECT SL_Labels.LabelSN, SL_Labels.SkidSN, SL_Labels.PackingSeq, SL_Labels.PO, SL_Labels.Description, SL_Labels.Seq, SL_Labels.Skid, SL_Labels.Carton, SL_Labels.Status, SL_Labels.PrintStatus, SL_Labels.Line, SL_Packing.Component, SL_Packing.Item, SL_Packing.ShipQty, SL_Packing.BOMQty FROM SL_Labels FULL OUTER JOIN SL_Packing ON SL_Labels.PackingSeq = SL_Packing.Seq WHERE((SL_Labels.Vendor=", VendorList, ") and SL_Labels.PO = '", lblPO.Text, "') ORDER BY SL_Labels.LabelSN");
                //gvLabels.DataBind();
                // Check if this item has components
                sdsPrinting.SelectCommand = string.Concat("SELECT SL_Packing.PO, SL_Packing.Line, SL_Packing.Seq, SL_Packing.ShipQty, SL_Packing.BOMQty, SL_Packing.Item, SL_Packing.Component, SL_Labels.Description, SL_Labels.Carton, SL_Labels.BulkCarton, SL_Labels.Skid, SL_Labels.LabelSN, SL_Labels.BulkSN, SL_Labels.SkidSN, SL_Labels.PackingSeq, SL_Labels.Status, SL_Labels.PrintStatus FROM SL_Packing FULL OUTER JOIN SL_Labels ON SL_Packing.Vendor = SL_Labels.Vendor and SL_Packing.PO = SL_Labels.PO and SL_Packing.Line = SL_Labels.Line and SL_Packing.Carton = SL_Labels.Carton WHERE (SL_Labels.Vendor=", VendorList, ") and SL_Labels.PO='", lblPO.Text, "' ORDER BY Line,Carton");
                // sdsPrinting.SelectCommand = string.Concat("SELECT SL_Labels.LabelSN, SL_Labels.SkidSN, SL_Labels.PackingSeq, SL_Labels.PO, SL_Labels.Description, SL_Labels.Seq, SL_Labels.Skid, SL_Labels.Carton, SL_Labels.Status, SL_Labels.PrintStatus, SL_Packing.Line, SL_Packing.Component, SL_Packing.Item, SL_Packing.ShipQty, SL_Packing.BOMQty FROM SL_Labels FULL OUTER JOIN SL_Packing ON SL_Labels.PackingSeq = SL_Packing.Seq WHERE((SL_Labels.Vendor=", VendorList, ") and SL_Labels.PO = '", lblPO.Text, "') ORDER BY SL_Labels.Seq");
                gvPrinting.DataBind();

                // lbPrintAll.OnClientClick = string.Concat("PrintLabel(null, ", gvLabels.Rows.Count,");");
                // hlPOByItem.NavigateUrl = string.Concat("/Default.aspx?PO=", PO);
            }
            else
            {
                lblMessage.Text = "";
            }
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

        protected void btnPrintSelected_Click(object sender, EventArgs e)
        {
            string strCommand;
            LinkButton lb;
            CheckBox cb;
            GridViewRow gvr;
            string printContent = "";
            string divContent = "";
            bool rowClosed = false;
            bool PrintAll = false;
            int rowNumber = 0;
            int lblCount = 0;
            string VendorLine;
            string ShipAddr2, ShipAddr1, ShipName, ShipCity, ShipZip, ShipState;
            string Model;
//            const string quote = "\"";

            lb = (LinkButton)sender;
            if (string.Compare(lb.ID, "lbPrintAll") == 0)
            {
                PrintAll = true;
            }


            // Get header record information for the labels
            // Read Header Fields
            string sqlCommandSrc;
            SqlConnection sqlConnection2 = new SqlConnection(ConfigurationManager.ConnectionStrings["DMTargetConnectionString"].ConnectionString);
            SqlConnection sqlConnection1 = new SqlConnection(ConfigurationManager.ConnectionStrings["DMSourceConnectionString"].ConnectionString);
            SqlDataReader sdr;

            sqlConnection2.Open();
            sqlConnection1.Open();
            sqlCommandSrc = string.Concat("Select * from POORDHP where PHPONO = '", lblPO.Text, "'");
            SqlCommand command = new SqlCommand(sqlCommandSrc, sqlConnection2);
            sdr = command.ExecuteReader();
            if (sdr.HasRows)
            {
                sdr.Read();

                ShipAddr2 = (string)sdr["PHSAD2"];
                ShipAddr1 = (string)sdr["PHSAD1"];
                ShipName = (string)sdr["PHSNAM"];
                ShipCity = (string)sdr["PHSCTY"];
                ShipState = (string)sdr["PHSSTA"];
                ShipZip = (string)sdr["PHSZIP"];
                VendorLine = string.Concat("Vendor: ", DataClass.GetVendor((decimal)sdr["PHVEN1"]), "<br/><hr/>");
            }
            else
            {
                sdr.Close();
                sqlConnection1.Close();
                sqlConnection2.Close();
                lblMessage.Text = "Critical Error: Order Header Information Not Found";
                return;
            }
            sdr.Close();
            sqlConnection2.Close();

            // Setup print content header
            printContent = string.Concat("<head><link rel=\"stylesheet\" type=\"text/css\" href=\"Labels.css\"></head><body>");
            for (int i = 0; i < gvPrinting.Rows.Count; i++)
            {
                gvr = gvPrinting.Rows[i];
                cb = (CheckBox)gvr.FindControl("cbPrint");
                // If we can't find a checkbox, next row
                if((cb != null) && (cb.Visible == true))
                {
                    // Are we printing this label?
                    if((PrintAll == true) || (cb.Checked == true))
                    {
                        // Generate actual label text into divContent
                        // cb.Tooltip has the SN of the label we are printing

                        strCommand = string.Concat("SELECT SL_Labels.LabelSN, SL_Labels.BulkSN, SL_Labels.SkidSN, SL_Labels.PackingSeq, SL_Labels.PO, SL_Labels.Description, SL_Labels.Seq, SL_Labels.Skid, SL_Labels.BulkCarton, SL_Labels.Carton, SL_Labels.Status, SL_Labels.PrintStatus, SL_Labels.Line, SL_Packing.Component, SL_Packing.Item, SL_Packing.ShipQty, SL_Packing.BOMQty FROM SL_Labels FULL OUTER JOIN SL_Packing ON SL_Labels.PackingSeq = SL_Packing.Seq WHERE ((SL_Labels.Vendor = ", VendorList, ") and SL_Labels.PO = '", lblPO.Text, "' and SL_Labels.LabelSN=", cb.ToolTip.ToString()+ ")");
                        command = new SqlCommand(strCommand, sqlConnection1);
                        sdr = command.ExecuteReader();
                        if(sdr.HasRows)
                        {
                            int CartonCount, MinCarton, CurrentCarton, BoxNo;
                            int SkidNo;
                            int BulkCarton;
                            decimal CartonSN;
                            decimal BulkSN;
                            decimal SkidSN;
                            string PrintCarton;
                            string LineNo;

                            sdr.Read();

                            SkidNo = (int)sdr["Skid"];
                            BulkCarton = (int)sdr["BulkCarton"];
                            CartonSN = (decimal)sdr["LabelSN"];
                            BulkSN = (decimal)sdr["BulkSN"];
                            SkidSN = (decimal)sdr["SkidSN"];
                            LineNo = sdr["Line"].ToString();
                            CurrentCarton = (int)sdr["Carton"];

                            // Check if this is a skid label, pre-pend 'S' if it is
                            if ((SkidNo > 0) && (CurrentCarton == 0) && (BulkCarton == 0))
                            {
                                PrintCarton = string.Concat("S", SkidSN.ToString("000000000"));
                            }
                            else if ((BulkCarton > 0) && (CurrentCarton == 0))
                            {
                                PrintCarton = string.Concat("B", BulkSN.ToString("000000000"));
                            }
                            else
                            {
                                PrintCarton = CartonSN.ToString("000000000");
                            }

                            //// Check if this is a skid label, pre-pend 'S' if it is
                            //if ((SkidNo > 0) && (CurrentCarton == 0))
                            //{
                            //    PrintCarton = string.Concat("S", SkidSN.ToString("000000000"));
                            //}
                            //else
                            //{
                            //    PrintCarton = CartonSN.ToString("000000000");
                            //}

                            CartonCount = DataClass.GetCartonCount(lblPO.Text, LineNo);
                            MinCarton = DataClass.GetFirstCarton(lblPO.Text, LineNo);
                            BoxNo = CurrentCarton - MinCarton + 1;
                            // If we are printint a Skid label, set the box # to 0 as well.
                            if (CartonCount == 0)
                            {
                                BoxNo = 0;
                            }


                            // Start generating the label content
                            divContent = VendorLine;
                            // Check if our Item is null, will be a skid label in this instance.
                            if((SkidNo > 0) && (BulkCarton == 0) && (CurrentCarton == 0))
                            {
                                divContent += "<p style='text-align: center; font-size: 3em; font-weight: bold'>SKID " + SkidNo.ToString()+ "</p><br/><br/>";
                            }
                            else if ((BulkCarton > 0) && (CurrentCarton == 0))
                            {
                                divContent += "<p style='text-align: center; font-size: 3em; font-weight: bold'>BULK CTN " + BulkCarton.ToString() + "</p><br/><br/>";
                            }
                            else
                            {
                                if (System.DBNull.Value.Equals(sdr["Item"]))
                                {
                                    Model = (string)sdr["Description"];
                                    divContent += string.Concat(Model, "<br/>");
                                }
                                else
                                {
                                    Model = (string)sdr["Item"];
                                    divContent += string.Concat("Model: ", Model, "<br/>");
                                }
                                divContent += "Finishes: <br/>";
                                divContent += string.Concat("Qty: ", sdr["ShipQty"], "<br/>");
                                divContent += string.Concat("Box: ", BoxNo.ToString(), " of ", CartonCount.ToString());
                                if ((BulkCarton != 0) && (BoxNo != 0))
                                {
                                    divContent += string.Concat(", Bulk #", BulkCarton.ToString());
                                }
                                if ((SkidNo != 0) && (BoxNo != 0))
                                {
                                    divContent += string.Concat(" - On Skid: ", SkidNo.ToString());
                                }
                            }

                            divContent += string.Concat("<br/><hr/>");
                            divContent += "Ship To: <br/>";
                            divContent += string.Concat(ShipName, "<br/>");
                            divContent  += string.Concat(ShipAddr1, "<br/>");
                            if (ShipAddr2.Trim().Length > 0)
                            {
                                divContent += string.Concat(ShipAddr2, "<br/>");
                            }
                            divContent += string.Concat(ShipCity, ", ", ShipState, " ", ShipZip, "<br />");
                            divContent += "Load: <br/>";
                            divContent += string.Concat("PO NUMBER/LINE#: <B>", lblPO.Text, " / ", LineNo.ToString(), "<B/><br/>");
                            divContent += string.Concat("ORDER /LINE#: <br/><hr/>");
                            divContent += string.Concat("<div style=\"height: 1.5in;\"><center><img alt='Barcode Generator TEC-IT' src='https://barcode.tec-it.com/barcode.ashx?data=", PrintCarton, "&code=Code39&dpi=96&dataseparator=' /></center></div>");
                            divContent += string.Concat("<hr/>");
                        }
                        else
                        {
                            sdr.Close();
                            sqlConnection1.Close();
                            lblMessage.Text = string.Concat("Critical Error Locating Label: ", cb.ToolTip);
                            return;
                        }
                        sdr.Close();

                        // Are we printing in the left or right column?
                        if ((lblCount % 2) == 0)
                        {
                            rowNumber++;
                            if ((rowNumber % 2) == 0)
                            {
                                // Spacer
                                printContent += string.Concat("<div style=\"width: 100%; display: table; min-height: .5in;\"><div style=\"display: table-row;\"><div style=\"display: table-cell;\"><hr /></div></div></div>");
                                printContent += string.Concat("<div style=\"width: 100%; display: table; \"><div style=\"display: table-row; page-break-after: always;\">");
                                //printContent += string.Concat("<div style=\"width: 100%; display: table; \"><div style=\"display: table-row; height: 7.5in; min-height: 7.5in;\">");
                            }
                            else
                            {
                                printContent += string.Concat("<div style=\"width: 100%; display: table; \"><div style=\"display: table-row; height: 7.75in; min-height: 7.75in;\">");
                                //    printContent += string.Concat("<div style=\"width: 100%; display: table; \"><div style=\"display: table-row; height: 7.5in; min-height: 7.5in;\">");
                                //    rowClosed = false;
                            }
                            // Label Row Header
                            rowClosed = false;
                        }

                        printContent += string.Concat("<div class=\"Label\" style=\"display: table-cell; min-width: 4in; \">", divContent, "</div>");

                        // Last, increase our printed label count for positioning the next label, and mark this one printed
                        DataClass.MarkPrinted(cb.ToolTip);
                        lblCount++;
                        cb.Checked = false;     // Also uncheck the print selection
                        cb.Text = "Re-Print";

                        // Close off the row
                        if ((lblCount % 2) == 0)
                        {
                            printContent += "</div></div>";
                            rowClosed = true;
                        }


                    }           // End: if((PrintAll == true) || (cb.Checked == true))
                }               // End: if((cb != null) && (cb.Visible == true))
                //lb = (LinkButton)gvr.FindControl("btnPrint");
                //if (lb != null)
                //{
                //    DataClass.MarkPrinted(lb.ToolTip);
                //}
            }                   // End: for (int i = 0; i < gvPrinting.Rows.Count; i++)

            if (rowClosed == false)
            {
                printContent += string.Concat("<div class=\"Label\" style=\"display: table-cell; min-width: 4in;\">&nbsp;<br/><center>&nbsp;</center><br/></div></div></div>");
            }
            printContent += string.Concat("</body>");
            sqlConnection1.Close();

            // Let's print!!!
            //StreamWriter streamWriter = new StreamWriter("C:\\Temp\\Log.Txt", true);
            //streamWriter.WriteLine("Starting Print");
            //streamWriter.Flush();
            var Renderer = new IronPdf.HtmlToPdf();
            Renderer.PrintOptions.SetCustomPaperSizeInInches(8.5, 13);
            Renderer.PrintOptions.MarginTop = 6;
            Renderer.PrintOptions.MarginBottom = 6;
            Renderer.PrintOptions.MarginLeft = 6;
            Renderer.PrintOptions.MarginRight = 6;
            Renderer.PrintOptions.DPI = 300;
            // Renderer.PrintOptions.RenderDelay = 50;
            Renderer.PrintOptions.CssMediaType = PdfPrintOptions.PdfCssMediaType.Print;

            string FileName = string.Concat("Labels", lblPO.Text.Trim(), "_", DateTime.Now.ToFileTimeUtc().ToString(), ".PDF");
            string Path = string.Concat("C:\\Temp\\", FileName);
            // Renderer.RenderHtmlAsPdf(printContent).SaveAs(FileName);

            //streamWriter.WriteLine(String.Concat("Printing: ", Path));
            //streamWriter.Flush();
            PdfDocument PDF = Renderer.RenderHtmlAsPdf(printContent);
            //streamWriter.WriteLine(String.Concat("Rendered: ", Path));
            //streamWriter.Flush();
            PDF.SaveAs(Path);
            //streamWriter.WriteLine("Finished, closing");
            //streamWriter.Flush();
            //streamWriter.Close();
            System.Web.HttpResponse response = System.Web.HttpContext.Current.Response;
            response.ClearContent();
            response.Clear();
            response.ContentType = "application/pdf";
            response.AddHeader("Content-Disposition", "attachment; filename=" + FileName + ";");
            response.TransmitFile(Path);
            response.Flush();
            response.End();
        }


        protected void gvPrinting_Databound(object sender, GridViewRowEventArgs e)
        {
            // LinkButton lb;
            CheckBox cb;
            DataRowView drv;
            // int Seq;
            // string divID = "divLabel_";
            decimal LabelSN;
            bool found;
            // GridViewRow gvr;
            // Literal SN;
            Label l;

            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                drv = (DataRowView)e.Row.DataItem;

                LabelSN = (decimal)drv["LabelSN"];
                l = (Label)e.Row.FindControl("lblSN");
                if(l != null)
                {
                    int ctn, bulk, skd;
                    decimal skidsn, bulksn;


                    ctn = (int)drv["Carton"];
                    skd = (int)drv["Skid"];
                    bulk = (int)drv["BulkCarton"];
                    skidsn = (decimal)drv["SkidSN"];
                    bulksn = (decimal)drv["BulkSN"];
                    if(ctn != 0) {
                        l.Text = LabelSN.ToString();
                    }
                    else
                    {
                        if (bulk != 0) l.Text = string.Concat("B", bulksn.ToString("000000000"));
                        if ((skd != 0) && (bulk == 0)) l.Text = String.Concat("S", skidsn.ToString("000000000"));
                    }

                }

                cb = (CheckBox)e.Row.FindControl("cbPrint");
                if (LabelSN != LastSN)
                {
                    // Set the last label
                    LastSN = LabelSN;
                    cb.ToolTip = drv["LabelSN"].ToString();
                    found = false;
                    //for(int i = 0; i < gvLabels.Rows.Count -1; i++)
                    //{
                    //    gvr = gvLabels.Rows[i];

                    //    SN = (Literal)gvr.FindControl("SN");
                    //    if(string.Compare(SN.Text, cb.ToolTip) == 0)
                    //    {
                    //        found = true;
                    //        divID = string.Concat("divLabel_", i.ToString());
                    //    }
                    //}
                    // divID = string.Concat("divLabel_", e.Row.DataItemIndex.ToString());
                    if (String.Compare((string)drv["PrintStatus"], "P") == 0)
                    {
                        cb.Text = "Re-Print";
                    }
                    else
                    {
                        cb.Text = "Print";
                    }
                    //if(found)
                    //{
                    //    lb.OnClientClick = string.Concat("PrintLabel('MainContent_gvLabels_", divID, "', 1); return true;");
                    //}
                }
                else
                {
                    cb.Visible = false;
                }
                // Display the company name in italics.
                // Seq = (int)drv["Seq"];
            }
        }

        //protected void gvLabels_Databound(object sender, GridViewRowEventArgs e)
        //{
        //    LinkButton lb;
        //    DataRowView drv;
        //    Literal innerHtml, SN;
        //    TableCell cell;
        //    string LineNo;
        //    int CartonCount, MinCarton, CurrentCarton, BoxNo;
        //    int Seq;
        //    int SkidNo;
        //    string divID;
        //    decimal CartonSN;
        //    decimal SkidSN;
        //    string PrintCarton;

        //    if (e.Row.RowType == DataControlRowType.DataRow)
        //    {
        //        drv = (DataRowView)e.Row.DataItem;
        //        // Display the company name in italics.
        //        innerHtml = (Literal)e.Row.FindControl("innerHtml");
        //        SN = (Literal)e.Row.FindControl("SN");
        //        SkidNo = (int)drv["Skid"];
        //        CartonSN = (decimal)drv["LabelSN"];
        //        SkidSN = (decimal)drv["SkidSN"];
        //        LineNo = drv["Line"].ToString();
        //        CurrentCarton = (int)drv["Carton"];
        //        // Seq = (int)drv["Seq"];
        //        cell = e.Row.Cells[0];
        //        divID = string.Concat("divLabel_", e.Row.DataItemIndex.ToString());
        //        // divID = string.Concat("divLabel_", CartonSN.ToString());
        //        cell.Controls[0].ID = divID;
        //        SN.Text = CartonSN.ToString();

        //        // Check if this is a skid label, pre-pend 'S' if it is
        //        if ((SkidNo > 0) && (CurrentCarton == 0))
        //        {
        //            PrintCarton = string.Concat("S", SkidSN.ToString("000000000"));
        //        }
        //        else
        //        {
        //            PrintCarton = CartonSN.ToString("000000000");
        //        }

        //        CartonCount = DataClass.GetCartonCount(lblPO.Text, LineNo);
        //        MinCarton = DataClass.GetFirstCarton(lblPO.Text, LineNo);
        //        BoxNo = CurrentCarton - MinCarton + 1;
        //        // If we are printint a Skid label, set the box # to 0 as well.
        //        if (CartonCount == 0)
        //        {
        //            BoxNo = 0;
        //        }

        //        // Read Header Fields
        //        string sqlCommandSrc;
        //        SqlConnection sqlConnection2 = new SqlConnection(ConfigurationManager.ConnectionStrings["DMTargetConnectionString"].ConnectionString);
        //        SqlDataReader sdr;

        //        sqlConnection2.Open();
        //        sqlCommandSrc = string.Concat("Select * from POORDHP where PHPONO = '", lblPO.Text, "'");
        //        SqlCommand command = new SqlCommand(sqlCommandSrc, sqlConnection2);
        //        sdr = command.ExecuteReader();
        //        if (sdr.HasRows)
        //        {
        //            sdr.Read();

        //            string addr2 = (string)sdr["PHSAD2"];
        //            string Model;

        //            innerHtml.Text = string.Concat("Vendor: ", DataClass.GetVendor((decimal)sdr["PHVEN1"]), "<br/><hr/>");
        //            // Check if our Item is null, will be a skid label in this instance.
        //            if (System.DBNull.Value.Equals(drv["Item"]))
        //            {
        //                Model = (string)drv["Description"];
        //                innerHtml.Text += string.Concat(Model, "<br/>");
        //            }
        //            else
        //            {
        //                Model = (string)drv["Item"];
        //                innerHtml.Text += string.Concat("Model: ", Model, "<br/>");
        //            }

        //            innerHtml.Text += "Finishes: <br/>";
        //            innerHtml.Text += string.Concat("Qty: ", drv["ShipQty"], "<br/>");
        //            innerHtml.Text += string.Concat("Box: ", BoxNo.ToString(), " of ", CartonCount.ToString());
        //            if ((SkidNo != 0) && (BoxNo != 0)) {
        //                innerHtml.Text += string.Concat(" - On Skid: ", SkidNo.ToString());
        //            }
        //            innerHtml.Text += string.Concat("<br/><hr/>");
        //            innerHtml.Text += "Ship To: <br/>";
        //            innerHtml.Text += string.Concat((string)sdr["PHSNAM"], "<br/>");
        //            innerHtml.Text += string.Concat((string)sdr["PHSAD1"], "<br/>");
        //            if (addr2.Trim().Length > 0)
        //            {
        //                innerHtml.Text += string.Concat(addr2, "<br/>");
        //            }
        //            innerHtml.Text += string.Concat(sdr["PHSCTY"], ", ", sdr["PHSSTA"], " ", sdr["PHSZIP"], "<br />");
        //            innerHtml.Text += "Load: <br/>";
        //            innerHtml.Text += string.Concat("PO NUMBER/LINE#: <B>", lblPO.Text, " / ", LineNo.ToString(), "<B/><br/>");
        //            innerHtml.Text += string.Concat("ORDER /LINE#: <br/><hr/>");
        //            innerHtml.Text += string.Concat("<center><img alt='Barcode Generator TEC-IT' src='https://barcode.tec-it.com/barcode.ashx?data=", PrintCarton, "&code=Code39&dpi=96&dataseparator=' /></center> ");

        //            // hl.NavigateUrl = String.Concat("/Component.aspx?PO=", txtSearch.Text, "&Item=", hl.Text.Trim);
        //        }
        //        else
        //        {
        //            sqlConnection2.Close();
        //            lblMessage.Text = "PO Not Found";
        //            return;
        //        }
        //        sqlConnection2.Close();

        //    }
        //}

    }
}