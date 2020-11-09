<%@ Page Title="Home Page" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="Skid_Labels._Default" %>
<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <br />
<br />
<table style="width: 100%;">
    <tr>
        <td colspan="5" style="font-weight:700;text-align:center;">Print/Pack Purchase Orders</td>
    </tr>
    <tr>
        <td style="width:20%;">Search:</td>
        <td style="width:20%;">
            <asp:TextBox ID="txtSearch" runat="server"></asp:TextBox><asp:Button ID="btnSearch" runat="server" Text="Find" OnClick="btnSearch_Click" /></td>
        <td style="width:20%;"></td>
        <td style="width:20%;">Vendor:</td>
        <td style="width:20%;"><asp:Label ID="lblVendorList" runat="server" Text=""></asp:Label></td>
    </tr>
    <tr>
        <td>Vendor:</td>
        <td>
            <asp:Label ID="lblVendor" runat="server"></asp:Label></td>
        <td></td>
        <td></td>
        <td></td>
    </tr>
    <tr>
        <td>Purchase Order:</td>
        <td>
            <asp:Label ID="lblPO" runat="server"></asp:Label>&nbsp;&nbsp;&nbsp;
            <asp:Label ID="lblPOStatus" runat="server" Text=""></asp:Label>
        </td>
        <td></td>
        <td>Vendor Reference:</td>
        <td><asp:TextBox ID="txtVendRef" runat="server" OnTextChanged="RefChanged" AutoPostBack="true"></asp:TextBox></td>
    </tr>
    <tr>
        <td>Purchase Order Date:</td>
        <td>
            <asp:Label ID="lblPODate" runat="server"></asp:Label></td>
        <td></td>
        <td>Completion Date</td>
        <td><asp:Label ID="lblCompletionDate" runat="server"></asp:Label></td>
    </tr>
    <tr>
        <td>Ship-to Name/Address:</td>
        <td>
            <asp:Label ID="lblShipName" runat="server"></asp:Label></td>
        <td></td>
        <td></td>
        <td></td>
    </tr>
    <tr>
        <td></td>
        <td>
            <asp:Label ID="lblShipAddr1" runat="server"></asp:Label></td>
        <td></td>
        <td></td>
        <td></td>
    </tr>
    <tr>
        <td></td>
        <td>
            <asp:Label ID="lblShipAddr2" runat="server"></asp:Label></td>
        <td></td>
        <td></td>
        <td></td>
    </tr>
    <tr>
        <td></td>
        <td>
            <asp:Label ID="lblShipCity" runat="server"></asp:Label>,&nbsp;
            <asp:Label ID="lblShipState" runat="server"></asp:Label>&nbsp;
            <asp:Label ID="lblShipZip" runat="server"></asp:Label>
        </td>
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
            <asp:GridView ID="gvItems" runat="server" AutoGenerateColumns="False" DataSourceID="sdsDetail" OnRowDataBound="gvItems_Databound">
                <Columns>
                    <asp:BoundField DataField="Line" HeaderText="Line" SortExpression="Line" />
                    <asp:BoundField DataField="LineType" HeaderText="Type" SortExpression="LineType" />
                    <asp:BoundField DataField="BOMQty" HeaderText="PO Qty" SortExpression="BOMQty" />
                    <asp:TemplateField>
                        <HeaderTemplate>
                            Ship<br />Qty
                        </HeaderTemplate>
                        <ItemTemplate>
                            <asp:TextBox ID="txtShipQty" Width="80px" runat="server" OnTextChanged="QtyChanged" AutoPostBack="true" Text='<%# DataBinder.Eval(Container, "DataItem.ShipQty") %>'></asp:TextBox>
                            <asp:ImageButton ID="ibEdit" runat="server" Visible="false" ImageUrl="~/Images/EditProperties_32.png" OnClick="EditQty" OnClientClick="if ( ! UserConfirmation()) return false;" />
                        </ItemTemplate>
                    </asp:TemplateField>
                    <asp:HyperLinkField DataTextField="ITEM" HeaderText="Item" SortExpression="Item" />
                    <asp:BoundField DataField="Description" HeaderText="Description" SortExpression="Description" />
                    <asp:BoundField DataField="ShipLabels" HeaderText="Labels#" SortExpression="ShipLabels#" />
                    <asp:BoundField DataField="PackStatus" HeaderText="Pack Status" SortExpression="PackStatus" />
                    <asp:BoundField DataField="PrintStatus" HeaderText="Print Status" SortExpression="PrintStatus" />
                </Columns>
            </asp:GridView>
            <asp:SqlDataSource ID="sdsDetail" runat="server" ConnectionString="<%$ ConnectionStrings:DMSourceConnectionString %>" SelectCommand="SELECT SL_Detail.* where PO='0'"></asp:SqlDataSource>
            <asp:SqlDataSource ID="sdsPO" runat="server" ConnectionString="<%$ ConnectionStrings:DMTargetConnectionString %>" SelectCommand="SELECT POORDDP.* where PDORD#=0"></asp:SqlDataSource>
            <asp:SqlDataSource ID="sdsReference" runat="server" ConnectionString="<%$ ConnectionStrings:DMSourceConnectionString %>" SelectCommand="SELECT * FROM [SkidLabel_Status]"></asp:SqlDataSource>
        </td>
    </tr>
    <tr>
        <td >
            <asp:LinkButton ID="hlPrint" Text="Print Labels" visible="false" runat="server" OnClick="PrintClick"></asp:LinkButton>
            <asp:LinkButton ID="hlPOBySkid" Text="Bulk Pack Cartons/Skids" Visible="false" runat="server" OnClick="PackPO"></asp:LinkButton>

        </td>
        <td></td>
        <td colspan="3"></td>
    </tr>

</table>
</asp:Content>
