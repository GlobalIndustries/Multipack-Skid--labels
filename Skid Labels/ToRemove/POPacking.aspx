<%@ Page Title="Home Page" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="POPacking.aspx.cs" Inherits="Skid_Labels.POPacking" %>
<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <br />
<br />
<table style="width: 100%;">
    <tr>
        <td colspan="5" style="font-weight: 700" text-align="center">Print Purchase Order</td>
    </tr>
    <tr>
        <td>Purchase Order:</td>
        <td>
            <asp:Label ID="lblPO" runat="server"></asp:Label></td>
        <td></td>
        <td>Line #:</td>
        <td><asp:Label ID="lblLineNo" runat="server"></asp:Label></td>
    </tr>
    <tr>
        <td colspan="5">
            <asp:Label ID="lblMessage" runat="server" Text="" ForeColor="Red"></asp:Label>
        </td>
    </tr>
    <tr>
        <td colspan="5">
            <asp:GridView ID="gvPrinting" runat="server" AutoGenerateColumns="False" DataSourceID="sdsPrinting" OnRowDataBound="gvPrinting_Databound">
                <Columns>
                    <asp:BoundField DataField="Skid" HeaderText="Skid" SortExpression="Skid" />
                    <asp:BoundField DataField="Carton" HeaderText="Carton" SortExpression="Carton" />
                    <asp:BoundField DataField="ShipQty" HeaderText="ShipQty" SortExpression="ShipQty" />
                    <asp:BoundField DataField="Item" HeaderText="Item" SortExpression="Item" />
                    <asp:BoundField DataField="Component" HeaderText="Component" SortExpression="Component" />
                    <asp:BoundField DataField="Description" HeaderText="Description" SortExpression="Description" />
                    <asp:BoundField DataField="BOMQty" HeaderText="BOMQty" SortExpression="BOMQty" />
                    <asp:TemplateField>
                        <ItemTemplate>
                            <asp:LinkButton ID="btnPrint" runat="server" OnClientClick="" Text="Print" OnClick="btnPrint"></asp:LinkButton>
                        </ItemTemplate>
                    </asp:TemplateField>
                </Columns>
            </asp:GridView>
            <asp:GridView ID="gvLabels" runat="server" AutoGenerateColumns="false" DataSourceID="sdsLabels" OnRowDataBound="gvLabels_Databound" ShowHeader="false" >
                <Columns>
                    <asp:TemplateField>
                        <ItemTemplate>
                            <div id="divLabel" class="Label" runat="server">
                                <asp:Literal ID="innerHtml" runat="server"></asp:Literal>
                                <asp:Literal ID="SN" runat="server" Visible="false"></asp:Literal>
                            </div>
                        </ItemTemplate>
                    </asp:TemplateField>
                </Columns>
            </asp:GridView>
            <asp:SqlDataSource ID="sdsPrinting" runat="server" ConnectionString="<%$ ConnectionStrings:DMSourceConnectionString %>" SelectCommand="SELECT SL_Labels.LabelSN, SL_Labels.SkidSN, SL_Labels.PackingSeq, SL_Labels.PO, SL_Labels.Description, SL_Labels.Seq, SL_Labels.Skid, SL_Labels.Carton, SL_Labels.Status, SL_Labels.PrintStatus, SL_Packing.Line, SL_Packing.Component, SL_Packing.Item, SL_Packing.ShipQty, SL_Packing.BOMQty FROM SL_Labels FULL OUTER JOIN SL_Packing ON SL_Labels.PackingSeq = SL_Packing.Seq WHERE (SL_Labels.PO = '0') ORDER BY SL_Labels.Seq"></asp:SqlDataSource>
            <asp:SqlDataSource ID="sdsLabels" runat="server" ConnectionString="<%$ ConnectionStrings:DMSourceConnectionString %>" SelectCommand="SELECT * from SL_Labels where PO=0"></asp:SqlDataSource>
        </td>
    </tr>
    <tr>
        <td colspan="5">
            <asp:HyperLink ID="hlPOByItem" Text="View PO by Item" NavigateUrl="./Default.aspx?PO=" runat="server"></asp:HyperLink>
        </td>
    </tr>
    <tr>
        <td>
            <asp:LinkButton ID="lbPrintAll" Text="Print All Labels" runat="server" OnClientClick="PrintLabel(null); return true;" OnClick="btnPrintAll_Click"></asp:LinkButton>
        </td>
        <td>
            <asp:LinkButton ID="lbPrintPacking" Text="Print Packing List" runat="server" OnClientClick="PrintPackingList();"></asp:LinkButton>
        </td>
        <td colspan="3"></td>
    </tr>

</table>
</asp:Content>
