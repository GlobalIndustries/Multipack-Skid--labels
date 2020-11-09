using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;

namespace Skid_Labels
{
    public partial class Components : Page
    {
        //int VendorID = 70166;
        //string VendorList = "70166";
        string VendorList;

        protected void Page_Load(object sender, EventArgs e)
        {
            string PO= "";
            string Line = "";
            string Item = "";
            int Qty = 0;

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
                lblMessage.Text = "Missing Line #";
                return ;
            }



            if (!IsPostBack)
            {
                DataClass.GetLineInfo(PO, Line, ref Item, ref Qty);
                // Populate header fields
                lblPO.Text = PO;
                lblLineNo.Text = Line;
                lblItemNo.Text = Item;
                lblPOQty.Text = Qty.ToString();

                ShowNextCarton();

                // If we are looking at a component line, select the components for display
                if (DataClass.CheckComponents(Item))
                {
                    lblItemType.Text = "C";
                    // Added LineType 'W' 3/8/2020 for WriteIns
                    gvPacking.Columns[2].Visible = false;
                    gvPacking.Columns[3].Visible = true;
                    lbWriteIn.Visible = true;
                }
                else
                {
                    lblItemType.Text = "I";
                    gvPacking.Columns[2].Visible = true;
                    gvPacking.Columns[3].Visible = false;
                    lbWriteIn.Visible = false;
                }
                SetupSelect();
                lblCalcCartons.Text = "0";
                gvPacking.DataBind();
                hlPOByItem.NavigateUrl = string.Concat("./Default.aspx?PO=", PO);
                hlPOBySkid.NavigateUrl = string.Concat("./LinePacking.aspx?PO=", PO, "&Line=", Line);
            }
            else
            {
                lblMessage.Text = "";
                SetupSelect();
            }
        }

        protected void SetupSelect()
        {
            // If we are looking at a component line, select the components for display
            if (lblItemType.Text == "C")
            {
                // Added LineType 'W' 3/8/2020 for WriteIns
                sdsDetail.SelectCommand = string.Concat("SELECT * from SL_Detail where ((Vendor=", VendorList, ") and (PO='", lblPO.Text, "') and (Line='", lblLineNo.Text, "') and (LineType in ('C', 'W')))");
            }
            else
            {
                sdsDetail.SelectCommand = string.Concat("SELECT * from SL_Detail where ((Vendor=", VendorList, ") and (PO='", lblPO.Text, "') and (Line='", lblLineNo.Text, "') and (LineType='I'))");
            }
        }

        protected void ShowNextCarton()
        {
            int NextCarton = 0;

            // Grab the next available carton #, start by reading current highest value
            SqlDataReader sdrDMSource;
            SqlConnection scDMSource = new SqlConnection(ConfigurationManager.ConnectionStrings["DMSourceConnectionString"].ConnectionString);
            SqlCommand sqlCmdDMSource;
            string strDMSource;

            scDMSource.Open();
            strDMSource = string.Concat("Select Max(Carton) as EndCtn from SL_Packing where (Vendor=", VendorList, ") and (PO='", lblPO.Text, "')");
            sqlCmdDMSource = new SqlCommand(strDMSource, scDMSource);
            sdrDMSource = sqlCmdDMSource.ExecuteReader();
            if (sdrDMSource.HasRows)
            {
                sdrDMSource.Read();

                if (int.TryParse(sdrDMSource["EndCtn"].ToString(), out NextCarton))
                {
                    NextCarton++;           // Increase to next AVAILABLE carton #
                }
                else
                {
                    NextCarton = 1;
                }

            }
            else
            {
                NextCarton = 1;
            }
            scDMSource.Close();
            lblNextCarton.Text = NextCarton.ToString();
        }

        protected void btnPack(object sender, EventArgs e)
        {
            LinkButton lb;
            Label lbl;
            TextBox tbFrom, tbTo, tbShipQty;
            GridViewRow gvr;
            int TotalRows = 0;
            int ShipQty = 0;
            int NewID = 0;
            int NewSN = 0;
            int Seq = 0;
            int CartonStart, CartonEnd, TotalCartons;
            string strDescription, strComponent, strBOMQty, strShipQty;
            string sqlCommandSrc;
            bool bComponent = true;
            SqlConnection scDMSource = new SqlConnection(ConfigurationManager.ConnectionStrings["DMSourceConnectionString"].ConnectionString);
            SqlCommand srcCommand;
            SqlTransaction srcTransaction;
            SqlDataReader srcDataReader;

            lb = (LinkButton)sender;
            gvr = (GridViewRow)((LinkButton)sender).NamingContainer;
            tbFrom = (TextBox)gvr.FindControl("FromCarton");
            tbTo = (TextBox)gvr.FindControl("ToCarton");
            tbShipQty = (TextBox)gvr.FindControl("ShipQty");
            lbl = (Label)gvr.FindControl("lblBOMQty");
            strBOMQty = lbl.Text;
            lbl = (Label)gvr.FindControl("lblComponent");
            strComponent = lbl.Text.Trim();
            lbl = (Label)gvr.FindControl("lblDescription");
            strDescription = HttpUtility.HtmlDecode(lbl.Text);

            // If this is an Item, clear out the Component gathered from the Item/Component cell
            if (strComponent.Length == 0) 
            {
                bComponent = false;
            }
    
            // Validate Ship Qty
            if (int.TryParse(tbShipQty.Text, out ShipQty)) {
                strShipQty = ShipQty.ToString();
            }
            else
            {
                lblMessage.Text = "Invalid Ship Qty.";
                return;
            }

            if((tbFrom.Text.Trim().Length == 0) && (tbTo.Text.Trim().Length == 0))
            {
                int BaseCarton;

                int.TryParse(lblNextCarton.Text, out BaseCarton);
                tbFrom.Text = lblNextCarton.Text;
                tbTo.Text = (BaseCarton + ShipQty - 1).ToString();
            }
            if (int.TryParse(tbFrom.Text, out CartonStart))
            {
                if(int.TryParse(tbTo.Text, out CartonEnd))
                {
                    if(CartonEnd < CartonStart)
                    {
                        lblMessage.Text = "To carton must larger than From carton.";
                        return;
                    }
                }
                else
                {
                    CartonEnd = CartonStart;
                }

                // Open the connection, lets start moving some data!!!
                scDMSource.Open();

                // Validate these carton #s are not in use on another line in this PO
                for (int i = CartonStart; i <= CartonEnd; i++)
                {
                    sqlCommandSrc = string.Concat("Select * from SL_Packing Where (Vendor=", VendorList, ") and PO='", lblPO.Text, "' and Carton=", i.ToString(), " and Line<>'", lblLineNo.Text, "'");
                    srcCommand = new SqlCommand(sqlCommandSrc, scDMSource);
                    srcDataReader = srcCommand.ExecuteReader();
                    if(srcDataReader.HasRows)
                    {
                        lblMessage.Text = string.Concat("Error: Carton #", i.ToString(), " used on another line for this Purchase Order.");
                        srcDataReader.Close();
                        scDMSource.Close();
                        return;
                    }
                    srcDataReader.Close();
                }

                // Create packing records, to be used to create carton labels
                TotalCartons = CartonEnd - CartonStart + 1;
                srcTransaction = scDMSource.BeginTransaction();
                try
                {
                    Seq = DataClass.GetNextLabelSeq(lblPO.Text, lblLineNo.Text);
                    for (int i = CartonStart; i <= CartonEnd; i++)
                    {
                        // Create information about cartons/skids for this Line #
                        decimal dShipQty = (decimal)ShipQty / (decimal)TotalCartons;
                        decimal dRemainder = (decimal)ShipQty % (decimal)TotalCartons;
                        if((dRemainder > 0) && (dRemainder > (i - CartonStart)))
                        {
                            dShipQty++;
                        }
                        sqlCommandSrc = string.Concat("Insert into SL_Packing (Vendor, PO, Line, BOMQty, ShipQty, Item, Component, Description, Carton, Skid) Values(", VendorList, ", '", lblPO.Text, "', '", lblLineNo.Text, "', ", strBOMQty, ", ", dShipQty.ToString(), ", '", lblItemNo.Text, "', '", strComponent, "', '", strDescription, "', ", i.ToString(), ", ", 0, ")");
                        srcCommand = new SqlCommand(sqlCommandSrc, scDMSource);
                        srcCommand.Transaction = srcTransaction;
                        TotalRows += srcCommand.ExecuteNonQuery();

                        // Get the CartonID rom last insert
                        sqlCommandSrc = string.Concat("Select @@Identity as newId from SL_Packing");
                        srcCommand = new SqlCommand(sqlCommandSrc, scDMSource);
                        srcCommand.Transaction = srcTransaction;
                        NewID = Convert.ToInt32(srcCommand.ExecuteScalar());

                        // Create Label for last created carton
                        string sDescription;
                        decimal InsertSeq;
                        if(bComponent)
                        {
                            sDescription = string.Concat("Carton Label: ", lblItemNo.Text);
                            InsertSeq = 0;
                        }
                        else
                        {
                            sDescription = lblItemNo.Text;
                            InsertSeq = NewID;
                        }

                        // Check to see if a Label already exists for this PO/Line/Carton #
                        // Do not create duplicate labels
                        sqlCommandSrc = string.Concat("Select * from SL_Labels where (Vendor=", VendorList, ") and PO='", lblPO.Text, "' and Line='", lblLineNo.Text, "' and Carton=", i.ToString());
                        srcCommand = new SqlCommand(sqlCommandSrc, scDMSource);
                        srcCommand.Transaction = srcTransaction;
                        srcDataReader = srcCommand.ExecuteReader();
                        // We need to create this carton label.
                        // It will be with a 0 PackingSeq
                        if(!srcDataReader.HasRows)
                        {
                            sqlCommandSrc = string.Concat("Insert Into SL_Labels (PackingSeq, Vendor, PO, Line, Description, Seq, Skid, Carton, Status, PrintStatus) Values (", InsertSeq.ToString(), ",", VendorList, ",'", lblPO.Text, "','", lblLineNo.Text, "','", sDescription, "',", Seq.ToString(), ",0,", i.ToString(), ",'A','')");
                            srcCommand = new SqlCommand(sqlCommandSrc, scDMSource);
                            srcCommand.Transaction = srcTransaction;
                            srcCommand.ExecuteNonQuery();

                            // Get the CartonID rom last insert
                            sqlCommandSrc = string.Concat("Select @@Identity as newId from SL_Labels");
                            srcCommand = new SqlCommand(sqlCommandSrc, scDMSource);
                            srcCommand.Transaction = srcTransaction;
                            NewSN = Convert.ToInt32(srcCommand.ExecuteScalar());

                            // Add to our AS400
                            int ThisCarton = (i - CartonStart) + 1;
                            //sqlCommandSrc = string.Concat("Insert into SL_Operations (SerialNo, Vendor, PO, Line, Item, Component, Description, ShipQty, Operation, CartonNo, OfCartons, SkidSerialNo, Status) Values (", NewSN.ToString(), ", ", VendorList, ", '", lblPO.Text, "', '", lblLineNo.Text, "', '", lblItemNo.Text, "', '", strComponent, "', '", strDescription, "', ", dShipQty.ToString(), ", 'A', ", ThisCarton.ToString(), ", ", TotalCartons.ToString(), ", 0, 'N') ");
                            //srcCommand = new SqlCommand(sqlCommandSrc, scDMSource);
                            //srcCommand.Transaction = srcTransaction;
                            //srcCommand.ExecuteNonQuery();
                        }
                        srcDataReader.Close();
                        Seq++;
                    }

                    // Update this row in the detail and mark as Packed
                    sqlCommandSrc = string.Concat("Update SL_Detail set ShipQty=", strShipQty, ", ShipLabels=", TotalCartons.ToString(),", PrintStatus='', PackStatus='P', PackTime='", DateTime.Now, "' where (Vendor=", VendorList, ") and PO='", lblPO.Text, "' and Line='", lblLineNo.Text, "' and Item='", lblItemNo.Text, "' and Component='", strComponent, "'");
                    srcCommand = new SqlCommand(sqlCommandSrc, scDMSource);
                    srcCommand.Transaction = srcTransaction;
                    srcCommand.ExecuteNonQuery();

                    // If this is a component and all pieces are packed, mark the "Item" as packed
                    if(bComponent)
                    {
                        sqlCommandSrc = string.Concat("Select * from SL_Detail where (Vendor=", VendorList, ") and PO='", lblPO.Text, "' and Line='", lblLineNo.Text, "' and Item='", lblItemNo.Text, "' and LineType='C' and PackStatus='S'");
                        srcCommand = new SqlCommand(sqlCommandSrc, scDMSource);
                        srcCommand.Transaction = srcTransaction;
                        srcDataReader = srcCommand.ExecuteReader();
                        if (!srcDataReader.HasRows)
                        {
                            int TotalLabels = 0;

                            srcDataReader.Close();
                            // Get total Component label count
                            sqlCommandSrc = string.Concat("Select count(distinct Carton) as TotalLabels from SL_Packing where (Vendor=", VendorList, ") and PO='", lblPO.Text, "' and Line='", lblLineNo.Text, "'");
                            srcCommand = new SqlCommand(sqlCommandSrc, scDMSource);
                            srcCommand.Transaction = srcTransaction;
                            srcDataReader = srcCommand.ExecuteReader();
                            if(srcDataReader.HasRows)
                            {
                                srcDataReader.Read();
                                int.TryParse(srcDataReader["TotalLabels"].ToString(), out TotalLabels);
                                srcDataReader.Close();
                            }
                            else
                            {
                                lblMessage.Text = "Critical error counting labels";
                                srcDataReader.Close();
                                srcTransaction.Rollback();
                                scDMSource.Close();
                                return;
                            }

                            // Update this Item as Packed in the detail
                            sqlCommandSrc = string.Concat("Update SL_Detail set ShipLabels=", TotalLabels.ToString(), ", PrintStatus='', PackStatus='P', PackTime='", DateTime.Now, "' where (Vendor=", VendorList, ") and PO='", lblPO.Text, "' and Line='", lblLineNo.Text, "' and Item='", lblItemNo.Text, "' and LineType='I'");
                            srcCommand = new SqlCommand(sqlCommandSrc, scDMSource);
                            srcCommand.Transaction = srcTransaction;
                            srcCommand.ExecuteNonQuery();
                        }
                        else
                        {
                            // There are still rows
                            srcDataReader.Close();
                        }
                    }

                    srcTransaction.Commit();
                    // gvr = (GridViewRow)((TextBox)sender).NamingContainer;
                    //hl = (HyperLink)gvr.Cells[4].Controls[0];

                    // Close SQL Connection
                    scDMSource.Close();

                    tbShipQty.Enabled = false;
                    tbFrom.Enabled = false;
                    tbTo.Enabled = false;
                    lb.Visible = false;
                    lb = (LinkButton)gvr.FindControl("btnUnpack");
                    lb.Visible = true;
                    lblMessage.Text = string.Concat(TotalRows.ToString(), " Cartons Created.");
                    ShowNextCarton();
                }
                catch (Exception ex)
                {
                    srcTransaction.Rollback();
                    scDMSource.Close();
                    lblMessage.Text = "Error packing cartons.";
                    return;
                }

            }
            else
            {
                lblMessage.Text = "Invalid From Carton #";
            }
        }

        protected void btnUnpack(object sender, EventArgs e)
        {
            LinkButton lb, lbPack;
            Label lbl;
            TextBox tbFrom, tbTo, tbShipQty;
            GridViewRow gvr;
            string strDescription, strComponent, strBOMQty;
            string sqlCommandSrc;
            bool bComponent = true;
            SqlConnection scDMSource = new SqlConnection(ConfigurationManager.ConnectionStrings["DMSourceConnectionString"].ConnectionString);
            SqlCommand srcCommand;
            SqlTransaction srcTransaction;
            SqlDataReader sdr, sdr2;
            decimal labelSN;
            // int Carton;

            lb = (LinkButton)sender;
            gvr = (GridViewRow)((LinkButton)sender).NamingContainer;
            lbPack = (LinkButton)gvr.FindControl("btnPack");
            tbFrom = (TextBox)gvr.FindControl("FromCarton");
            tbTo = (TextBox)gvr.FindControl("ToCarton");
            tbShipQty = (TextBox)gvr.FindControl("ShipQty");
            lbl = (Label)gvr.FindControl("lblBOMQty");
            strBOMQty = lbl.Text;
            lbl = (Label)gvr.FindControl("lblComponent");
            strComponent = lbl.Text;
            lbl = (Label)gvr.FindControl("lblDescription");
            strDescription = HttpUtility.HtmlDecode(lbl.Text);

            // If this is an Item, clear out the Component gathered from the Item/Component cell
            if (strComponent.Trim().Length == 0)
            {
                bComponent = false;
            }

            scDMSource.Open();
            srcTransaction = scDMSource.BeginTransaction();
            try
            {
                // Check if any more components are in this Carton
                sqlCommandSrc = string.Concat("Select * from SL_Packing where (Vendor=", VendorList, ") and PO='", lblPO.Text, "' and (Carton in (Select Carton from SL_Packing Where (Vendor=", VendorList,") and PO='", lblPO.Text, "' and Line='", lblLineNo.Text, "' and Item='", lblItemNo.Text, "' and Component='", strComponent.Trim(), "')) and (Component<>'", strComponent.Trim(), "')");
                srcCommand = new SqlCommand(sqlCommandSrc, scDMSource);
                srcCommand.Transaction = srcTransaction;
                sdr = srcCommand.ExecuteReader();
                if (!sdr.HasRows)
                {
                    // Last items in this carton, remove serial #s and remove carton labels
                    // Create transactions to remove Labels from AS400
                    sqlCommandSrc = string.Concat("Select LabelSN from SL_Labels Where (Vendor=", VendorList, ") and PO='", lblPO.Text, "' and Carton in (Select Carton from SL_Packing Where (Vendor=", VendorList, ") and PO='", lblPO.Text, "' and Line='", lblLineNo.Text, "' and Item='", lblItemNo.Text, "' and Component='", strComponent.Trim(), "')");
                    srcCommand = new SqlCommand(sqlCommandSrc, scDMSource);
                    srcCommand.Transaction = srcTransaction;
                    sdr2 = srcCommand.ExecuteReader();
                    if (sdr2.HasRows)
                    {
                        while (sdr2.Read())
                        {
                            labelSN = (decimal)sdr2["LabelSN"];
                            //sqlCommandSrc = string.Concat("Insert into SL_Operations (SerialNo, Vendor, PO, Line, Item, Component, Description, ShipQty, Operation, CartonNo, OfCartons, SkidSerialNo, Status) Values (", labelSN.ToString(), ", ", VendorList, ", '", lblPO.Text, "', '', '', '', '', 0, 'D', 0, 0, 0, 'N') ");
                            //srcCommand = new SqlCommand(sqlCommandSrc, scDMSource);
                            //srcCommand.Transaction = srcTransaction;
                            //srcCommand.ExecuteNonQuery();
                            
                            // Remove Label records for these cartons
                            sqlCommandSrc = string.Concat("Delete from SL_Labels Where LabelSN=", labelSN.ToString());
                            srcCommand = new SqlCommand(sqlCommandSrc, scDMSource);
                            srcCommand.Transaction = srcTransaction;
                            srcCommand.ExecuteNonQuery();
                        }
                    }
                    sdr2.Close();
                }
                sdr.Close();

                // Remove packing records
                sqlCommandSrc = string.Concat("Delete from SL_Packing Where (Vendor=", VendorList, ") and PO='", lblPO.Text, "' and Line='", lblLineNo.Text, "' and Item='", lblItemNo.Text, "' and Component='", strComponent, "'");
                srcCommand = new SqlCommand(sqlCommandSrc, scDMSource);
                srcCommand.Transaction = srcTransaction;
                srcCommand.ExecuteNonQuery();
               

                // Update this row in the detail and mark as UnPacked
                sqlCommandSrc = string.Concat("Update SL_Detail set PrintStatus='', ShipLabels=0, PackStatus='S' where (Vendor=", VendorList, ") and PO='", lblPO.Text, "' and Line='", lblLineNo.Text, "' and Item='", lblItemNo.Text, "' and Component='", strComponent, "'");
                srcCommand = new SqlCommand(sqlCommandSrc, scDMSource);
                srcCommand.Transaction = srcTransaction;
                srcCommand.ExecuteNonQuery();



                // If this is a component and all pieces are packed, mark the "Item" as packed
                if (bComponent)
                {
                    // Update this Item as Staged in the detail
                    sqlCommandSrc = string.Concat("Update SL_Detail set PrintStatus='', ShipLabels=0, PackStatus='S' where (Vendor=", VendorList, ") and PO='", lblPO.Text, "' and Line='", lblLineNo.Text, "' and Item='", lblItemNo.Text, "' and LineType='I'");
                    srcCommand = new SqlCommand(sqlCommandSrc, scDMSource);
                    srcCommand.Transaction = srcTransaction;
                    srcCommand.ExecuteNonQuery();
                }

                srcTransaction.Commit();
                // gvr = (GridViewRow)((TextBox)sender).NamingContainer;
                //hl = (HyperLink)gvr.Cells[4].Controls[0];

                // Close SQL Connection
                scDMSource.Close();

                lb.Visible = false;
                lbPack.Visible = true;
                tbShipQty.Enabled = true;
                tbFrom.Enabled = true;
                tbFrom.Text = "";
                tbTo.Text = "";
                tbTo.Enabled = true;
                lblMessage.Text = "Items/Components unpacked from cartons.";
                ShowNextCarton();
            }
            catch (Exception ex)
            {
                srcTransaction.Rollback();
                scDMSource.Close();
                lblMessage.Text = "Error packing cartons.";
                return;
            }
        }


        protected void gvComponents_Databound(object sender, GridViewRowEventArgs e)
        {
            // HyperLink hl;
            Label lblComponent;
            LinkButton lbPack, lbUnpack;
            TextBox tbFrom, tbTo, tbShipQty;
            // DataView dv;

            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                // Get the carton list if it exists
                string sqlCommandTgt;
                int iFrom = 0;
                SqlDataReader sdrDMSource;
                SqlConnection scDMSource = new SqlConnection(ConfigurationManager.ConnectionStrings["DMSourceConnectionString"].ConnectionString);
                SqlCommand tgtCommand;
                string strComponent;

                scDMSource.Open();

                lbPack = (LinkButton)e.Row.FindControl("btnPack");
                lbUnpack = (LinkButton)e.Row.FindControl("btnUnpack");
                tbFrom = (TextBox)e.Row.FindControl("FromCarton");
                tbTo = (TextBox)e.Row.FindControl("ToCarton");
                tbShipQty = (TextBox)e.Row.FindControl("ShipQty");
                tbShipQty.Text = DataBinder.Eval(e.Row.DataItem, "ShipQty").ToString();
                lblComponent = (Label)e.Row.FindControl("lblComponent");
                strComponent = lblComponent.Text;

                // Grab all pre-staged cartons for this 
                sqlCommandTgt = string.Concat("Select Min(Carton) as StartCtn, Max(Carton) as EndCtn from SL_Packing where (Vendor=", VendorList, ") and PO='", lblPO.Text, "' and Line='", lblLineNo.Text, "' and Component='", strComponent, "'");
                tgtCommand = new SqlCommand(sqlCommandTgt, scDMSource);
                sdrDMSource = tgtCommand.ExecuteReader();
                if (sdrDMSource.HasRows)
                {
                    sdrDMSource.Read();
                    tbFrom.Text = sdrDMSource["StartCtn"].ToString();
                    tbTo.Text = sdrDMSource["EndCtn"].ToString();
                    if(int.TryParse(tbFrom.Text, out iFrom))
                    {
                        // We have already packed this field, lock down
                        tbFrom.Enabled = false;
                        tbTo.Enabled = false;
                        tbShipQty.Enabled = false;
                        lbPack.Visible = false;
                        lbUnpack.Visible = true;
                    }
                    else
                    {
                        // Occurs when the from/to are blank
                        lbPack.Visible = true;
                        lbUnpack.Visible = false;
                        // lblMessage.Text = "Critical error reading packing state.";
                    }
                }
                else
                {
                    // No cartons exist for this row,
                    lbPack.Visible = true;
                    lbUnpack.Visible = false ;
                }
                scDMSource.Close();
            }
        }

        protected void btnWriteIn(object sender, EventArgs e)
        {
            gvPacking.ShowFooter = true;
            lbWriteIn.Visible = false;
            gvPacking.DataBind();
        }

        protected void btnSave(object sender, EventArgs e)
        {
            GridViewRow gvr;
            TextBox tb;
            // Label lbl;
            int BOMQty, ShipQty;
            string Item;
            string Component;
            string Description;

            gvr = gvPacking.FooterRow;

            SqlConnection scDMSource = new SqlConnection(ConfigurationManager.ConnectionStrings["DMSourceConnectionString"].ConnectionString);
            SqlCommand srcCommand;
            SqlTransaction sqlTransaction;
            string sqlCommandSrc;
            scDMSource.Open();
            sqlTransaction = scDMSource.BeginTransaction();

            try
            {
                Item = lblItemNo.Text;
                tb = (TextBox)gvr.FindControl("txtBOMQty");
                try
                {
                    BOMQty = int.Parse(tb.Text);
                }
                catch(Exception ex)
                {
                    lblMessage.Text = "Invalid BOM Qty";
                    return;
                }
                tb = (TextBox)gvr.FindControl("txtShipQty");
                try
                {
                    ShipQty = int.Parse(tb.Text);
                }
                catch (Exception ex)
                {
                    lblMessage.Text = "Invalid Ship Qty";
                    return;
                }

                tb = (TextBox)gvr.FindControl("txtComponent");
                Component = tb.Text.Trim();
                tb = (TextBox)gvr.FindControl("txtDescription");
                Description = tb.Text.Trim();

                // Insert Write-In into SL_Detail
                sqlCommandSrc = string.Concat("Insert into SL_Detail (Vendor, PO, Line, LineType, Item, Component, Description, BomQty, ShipQty, CalcLabels, ShipLabels, PackStatus) Values (",VendorList,",'", lblPO.Text, "', '", lblLineNo.Text, "', 'W', '", Item, "', '", Component, "', '", Description, "', ", BOMQty.ToString(), ", ", ShipQty.ToString(), ", 0, 0, 'S')");
                srcCommand = new SqlCommand(sqlCommandSrc, scDMSource);
                srcCommand.Transaction = sqlTransaction;
                srcCommand.ExecuteNonQuery();

                // Update this Item as Staged in the detail
                sqlCommandSrc = string.Concat("Update SL_Detail set PrintStatus='', ShipLabels=0, PackStatus='S', PackTime='", DateTime.Now, "' where (Vendor=", VendorList, ") and PO='", lblPO.Text, "' and Line='", lblLineNo.Text, "' and Item='", lblItemNo.Text, "' and LineType='I'");
                srcCommand = new SqlCommand(sqlCommandSrc, scDMSource);
                srcCommand.Transaction = sqlTransaction;
                srcCommand.ExecuteNonQuery();

                sqlTransaction.Commit();
                scDMSource.Close();
                ClearFooter();
            }
            catch (Exception ex)
            {
                sqlTransaction.Rollback();
                scDMSource.Close();
                lblMessage.Text = string.Concat("Fatal Error Adding Row:", ex.Message);
                return;
            }

            gvPacking.ShowFooter = false;
            lbWriteIn.Visible = true;
            gvPacking.DataBind();
        }

        protected void ClearFooter()
        {
            GridViewRow gvr;
            TextBox tb;

            gvr = gvPacking.FooterRow;
            tb = (TextBox)gvr.FindControl("BOMQty");
            if (tb != null) tb.Text = "";
            tb = (TextBox)gvr.FindControl("ShipQty");
            if (tb != null) tb.Text = "";

            tb = (TextBox)gvr.FindControl("TxtComponent");
            if (tb != null) tb.Text = "";
            tb = (TextBox)gvr.FindControl("TxtDescription");
            if (tb != null) tb.Text = "";

            tb = (TextBox)gvr.FindControl("FromCarton");
            if (tb != null) tb.Text = "";
            tb = (TextBox)gvr.FindControl("ToCarton");
            if (tb != null) tb.Text = "";
        }

        protected void btnCancel(object sender, EventArgs e)
        {
            ClearFooter();
            gvPacking.ShowFooter = false;
            lbWriteIn.Visible = true;
            gvPacking.DataBind();
        }

    }
}