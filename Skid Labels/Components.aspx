<%@ Page Title="Home Page" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Components.aspx.cs" Inherits="Skid_Labels.Components" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
<br />
<br />
<table style="width: 100%;">
    <tr>
        <td colspan="5" style="font-weight:700;text-align:center;">Pack Purchase Order</td>
    </tr>
    <tr>
        <td colspan="5" style="font-weight:700;text-align:center;">View by Component</td>
    </tr>
    <tr>
        <td style="width:20%;">Purchase Order:</td>
        <td style="width:20%;">
            <asp:Label ID="lblPO" runat="server"></asp:Label></td>
        <td style="width:20%;"></td>
        <td style="width:20%;">Line #:</td>
        <td style="width:20%;"><asp:Label ID="lblLineNo" runat="server"></asp:Label></td>
    </tr>
    <tr>
        <td>Item #::</td>
        <td>
            <asp:Label ID="lblItemNo" runat="server"></asp:Label></td>
        <td></td>
        <td>&nbsp;</td>
        <td>
            <asp:Label ID="lblItemType" runat="server" Text="" Visible="false"></asp:Label>
        </td>
    </tr>
    <tr>
        <td>PO Quantity:</td>
        <td>
            <asp:Label ID="lblPOQty" runat="server"></asp:Label></td>
        <td></td>
        <td>Calculated Carton Count:</td>
        <td><asp:Label ID="lblCalcCartons" runat="server"></asp:Label></td>
    </tr>
    <tr>
        <td colspan="3">
            <asp:Label ID="lblMessage" runat="server" Text="" ForeColor="Red"></asp:Label>
        </td>
        <td>Next Available Carton #</td>
        <td><asp:Label ID="lblNextCarton" runat="server" Text=""></asp:Label></td>
    </tr>
    <tr>
        <td colspan="5">
            <asp:GridView ID="gvPacking" runat="server" AutoGenerateColumns="False" DataSourceID="sdsDetail" OnRowDataBound="gvComponents_Databound" DataKeyNames="PO,Line,LineType,Item,Component" >
                <Columns>
                    <asp:TemplateField HeaderText="BOM Qty" SortExpression="BOMQty">
                        <ItemTemplate>
                            <asp:Label ID="lblBOMQty" runat="server" Text='<%# Bind("BOMQty") %>'></asp:Label>
                        </ItemTemplate>
                        <FooterTemplate>
                            <asp:TextBox ID="txtBOMQty" runat="server" Text='<%# Bind("BOMQty") %>'></asp:TextBox>
                        </FooterTemplate>
                    </asp:TemplateField>
                    <asp:TemplateField>
                        <HeaderTemplate>Ship Qty</HeaderTemplate>
                        <ItemTemplate>
                            <asp:TextBox ID="ShipQty" width="80px" runat="server" ></asp:TextBox>
                        </ItemTemplate>
                        <FooterTemplate>
                            <asp:TextBox ID="txtShipQty" width="80px" runat="server" ></asp:TextBox>
                        </FooterTemplate>
                    </asp:TemplateField>
                    <asp:TemplateField HeaderText="Item" SortExpression="Item">
                        <ItemTemplate>
                            <asp:Label ID="lblItem" runat="server" Text='<%# Bind("Item") %>'></asp:Label>
                        </ItemTemplate>
                        <FooterTemplate>
                            <asp:Label ID="lblItem" runat="server" Text='<%# Eval("Item") %>'></asp:Label>
                        </FooterTemplate>
                    </asp:TemplateField>
                    <asp:TemplateField HeaderText="Component" SortExpression="Component">
                        <ItemTemplate>
                            <asp:Label ID="lblComponent" runat="server" Text='<%# Bind("Component") %>'></asp:Label>
                        </ItemTemplate>
                        <FooterTemplate>
                            <asp:TextBox ID="txtComponent" runat="server" Text='<%# Eval("Component") %>'></asp:TextBox>
                        </FooterTemplate>
                    </asp:TemplateField>
                    <asp:TemplateField HeaderText="Description" SortExpression="Description">
                        <ItemTemplate>
                            <asp:Label ID="lblDescription" runat="server" Text='<%# Bind("Description") %>'></asp:Label>
                        </ItemTemplate>
                        <FooterTemplate>
                            <asp:TextBox ID="txtDescription" runat="server" Text='<%# Bind("Description") %>'></asp:TextBox>
                        </FooterTemplate>
                    </asp:TemplateField>
                    <asp:TemplateField>
                        <HeaderTemplate>From Ctn</HeaderTemplate>
                        <ItemTemplate>
                            <asp:TextBox ID="FromCarton" width="80px" runat="server" ></asp:TextBox>
                        </ItemTemplate>
                    </asp:TemplateField>
                    <asp:TemplateField>
                        <HeaderTemplate>To Ctn</HeaderTemplate>
                        <ItemTemplate>
                            <asp:TextBox ID="ToCarton" width="80px" runat="server" ></asp:TextBox>
                        </ItemTemplate>
                    </asp:TemplateField>
                    <asp:TemplateField>
                        <ItemTemplate>
                            <asp:LinkButton ID="btnPack" runat="server" OnClick="btnPack" Text="Pack"></asp:LinkButton>
                            <asp:LinkButton ID="btnUnpack" runat="server" OnClick="btnUnpack" Text="Unpack"></asp:LinkButton>
                        </ItemTemplate>
                        <FooterTemplate>
                            <asp:LinkButton ID="btnSave" runat="server" OnClick="btnSave" Text="Save"></asp:LinkButton>
                            <asp:LinkButton ID="btnCancel" runat="server" OnClick="btnCancel" Text="Cancel"></asp:LinkButton>
                        </FooterTemplate>
                    </asp:TemplateField>
                </Columns>
            </asp:GridView>
            <asp:SqlDataSource ID="sdsDetail" runat="server" ConnectionString="<%$ ConnectionStrings:DMSourceConnectionString %>" SelectCommand="SELECT * FROM [SL_Detail] where PO='0'"></asp:SqlDataSource>
        </td>
    </tr>
    <tr>
        <td colspan="5">
            <asp:LinkButton ID="lbWriteIn" runat="server" OnClick="btnWriteIn">Add Write-In Item</asp:LinkButton>
        </td>
    </tr>
    <tr>
        <td>
            <asp:HyperLink ID="hlPOByItem" Text="View PO by Item" NavigateUrl="./Default.aspx?PO=" runat="server"></asp:HyperLink>
        </td>
        <td>
            <asp:HyperLink ID="hlPOBySkid" Text="Place Cartons on Skids" runat="server"></asp:HyperLink>
        </td>
        <td colspan="3"></td>
    </tr>

</table>
</asp:Content>
