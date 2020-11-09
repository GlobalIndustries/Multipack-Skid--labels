<%@ Page Title="Home Page" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="LinePacking.aspx.cs" Inherits="Skid_Labels.LinePacking" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <br />
<br />
<table style="width: 100%;">
    <tr>
        <td colspan="5" style="font-weight:700;text-align:center;" >Palletize Purchase Order</td>
    </tr>
    <tr>
        <td style="width:20%;">Purchase Order:</td>
        <td style="width:20%;">
            <asp:Label ID="lblPO" runat="server"></asp:Label></td>
        <td style="width:20%;">Line #:</td>
        <td style="width:20%;"><asp:Label ID="lblLineNo" runat="server"></asp:Label></td>
        <td style="width:20%;"></td>
    </tr>
    <tr>
        <td>Item #::</td>
        <td>
            <asp:Label ID="lblItemNo" runat="server"></asp:Label></td>
        <td></td>
        <td></td>
        <td></td>
    </tr>
    <tr>
        <td colspan="5">
            <asp:Label ID="lblMessage" runat="server" Text="" ForeColor="Red"></asp:Label>
        </td>
    </tr>
    <tr>
        <td colspan="5">
            <asp:GridView ID="gvPacking" runat="server" AutoGenerateColumns="False" DataSourceID="sdsPacking" OnRowDataBound="gvPacking_Databound" DataKeyNames="PO,Line">
                <Columns>
                    <asp:TemplateField>
                        <HeaderTemplate>Skid #</HeaderTemplate>
                        <ItemTemplate>
                            <asp:TextBox ID="tbSkid" runat="server" Text="" Width="80px" OnTextChanged="SkidChanged" AutoPostBack="true"></asp:TextBox>
                        </ItemTemplate>
                    </asp:TemplateField>
                    <asp:TemplateField>
                        <HeaderTemplate>Bulk Ctn #</HeaderTemplate>
                        <ItemTemplate>
                            <asp:TextBox ID="tbBulk" runat="server" Text="" Width="80px" ></asp:TextBox>
                        </ItemTemplate>
                    </asp:TemplateField>
                    <asp:BoundField DataField="Carton" HeaderText="Carton #" SortExpression="Carton" />
                    <asp:BoundField DataField="ShipQty" HeaderText="ShipQty" SortExpression="ShipQty" />
                    <asp:BoundField DataField="Item" HeaderText="Item" SortExpression="Item" />
                    <asp:BoundField DataField="Description" HeaderText="Description" SortExpression="Description" />
                    <asp:TemplateField>
                        <HeaderTemplate>Result</HeaderTemplate>
                        <ItemTemplate>
                            <asp:Label ID="lblStatus" runat="server" Text="" ></asp:Label>
                        </ItemTemplate>
                    </asp:TemplateField>
                </Columns>
            </asp:GridView>
            <asp:SqlDataSource ID="sdsPacking" runat="server" ConnectionString="<%$ ConnectionStrings:DMSourceConnectionString %>" SelectCommand="SELECT SL_Detail.PO, SL_Detail.Line, SL_Detail.Item, SL_Detail.Description, SL_Detail.ShipQty, SL_Packing.Carton
FROM            SL_Packing INNER JOIN SL_Detail ON SL_Packing.Vendor = SL_Detail.Vendor and SL_Packing.PO = SL_Detail.PO AND SL_Packing.Line = SL_Detail.Line
WHERE        (SL_Detail.LineType = 'I') and (SL_Detail.PO='0')
GROUP BY SL_Detail.PO, SL_Detail.Line, SL_Detail.Item, SL_Detail.ShipQty, SL_Detail.Description, SL_Packing.Carton"></asp:SqlDataSource>
        </td>
    </tr>
    <tr>
        <td>
            <asp:LinkButton ID="lbSaveSkids" runat="server" OnClick="SaveSkids">Save Skids</asp:LinkButton></td>
        <td></td>
        <td colspan="3"></td>
    </tr>
    <tr>
        <td>
            <asp:HyperLink ID="hlPOByItem" Text="View PO by Item" NavigateUrl="./Default.aspx?PO=" runat="server"></asp:HyperLink>
        </td>
        <td>
            <asp:HyperLink ID="hlComponents" Text="Return to Cartons" NavigateUrl="./Components.aspx?PO=" runat="server"></asp:HyperLink>
        </td>
        <td colspan="3></td>
    </tr>

</table>
</asp:Content>
