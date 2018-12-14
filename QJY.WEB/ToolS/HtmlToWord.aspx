<%@ Page Language="C#" AutoEventWireup="true" EnableEventValidation="false" CodeBehind="HtmlToWord.aspx.cs" Inherits="QJY.WEB.HtmlToWord" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <meta http-equiv="Content-Type" content="text/html; charset=utf-8" />
    <title></title>
    <script src="/ViewV5/JS/jquery-1.11.2.min.js"></script>
    <script src="/ViewV5/JS/SZHLCommon.js?jsver=20160915"></script>
    <script>
        function SetTotalDX(num) {
            $("#TotalDX").html(ComFunJS.Arabia_to_Chinese(num));
        }
    </script>
</head>
<body>
    <form id="form1" runat="server">
        <div class="fybx" style="text-align: center;">

            <span style="font-size: 24px; padding: 20px 0;">费用报销单</span>
            <p style="padding-bottom: 15px;">
                <asp:Label ID="lblDate" runat="server"></asp:Label>
            </p>
            <table border="1" style="margin: 0 auto; border-collapse: collapse; border-spacing: 0; width: 100%">

                <tr>
                    <td style="width: 12.5%; padding: 10px 0; text-align: center">标题</td>
                    <td style="padding: 10px 20px; text-align: left" colspan="5">
                        <asp:Label ID="lblTitle" runat="server"></asp:Label></td>
                </tr>
                <tr>
                    <td style="width: 12.5%; padding: 10px 0; text-align: center">申请人</td>
                    <td style="padding: 10px 20px; text-align: left" colspan="5">
                        <asp:Label ID="lblSQR" runat="server"></asp:Label></td>
                </tr>
                <tr>
                    <td style="width: 12.5%; padding: 10px 0; text-align: center">申请部门</td>
                    <td style="padding: 10px 20px; text-align: left" colspan="5">
                        <asp:Label ID="lblBranch" runat="server"></asp:Label></td>

                </tr>
                
                <tr>
                    <td style="width: 12.5%; padding: 10px 0; text-align: center">关联项目</td>
                    <td style="padding: 10px 20px; text-align: left" colspan="5">
                        <asp:Label ID="lblXM" runat="server"></asp:Label></td>

                </tr>
                <tr>
                    <td style="width: 12.5%; padding: 10px 0; text-align: center">描述</td>
                    <td style="padding: 10px 20px; text-align: left" colspan="5">
                        <asp:Label ID="lblRemark" runat="server"></asp:Label></td>

                </tr>
                <tr>
                    <td style="padding: 10px 0; text-align: center;" colspan="6">费用清单</td>
                </tr>
                <tr>
                    <td style="width: 50px; padding: 10px 0; text-align: center">序号</td>
                    <td style="width: 100px; padding: 10px 0; text-align: center">费用类型</td>
                    <td style="width: 100px; padding: 10px 0; text-align: center">金额(元)</td>
                    <td style="width: 100px; padding: 10px 0; text-align: center">时间</td>
                    <td style="width: 100px; padding: 10px 0; text-align: center">是否有票</td>
                    <td style=" padding: 10px 0; text-align: center">事由</td>
                </tr>
                <asp:Repeater ID="repItem" runat="server">
                    <ItemTemplate>
                        <tr>
                            <td style="width: 50px; padding: 10px 0; text-align: center"><%#Container.ItemIndex+1 %></td>
                            <td style="width: 100px; padding: 10px 0; text-align: center"><%#Eval("TypeName") %></td>
                            <td style="width: 100px; padding: 10px 0; text-align: center"><%#Eval("BXJE") %></td>
                            <td style="width: 100px; padding: 10px 0; text-align: center"><%#Eval("BXDate","{0:yyyy-MM-dd}") %></td>
                            <td style="width: 100px; padding: 10px 0; text-align: center"><%#Eval("IsHasFP").ToString()=="1"?"是":"否" %> </td>
                            <td style="padding: 10px 0; text-align: center;word-break:break-all"><%#Eval("BXContent") %></td>
                        </tr>
                    </ItemTemplate>
                </asp:Repeater>
                <tr>
                    <td style="width: 12.5%; padding: 10px 0; text-align: center">合&nbsp;&nbsp;计</td>
                    <td colspan="5" id="TotalDX" style="text-align: left; padding-left: 20px;">
                        <asp:Label ID="lblTotalDX" runat="server"></asp:Label></td>
                </tr>
            </table>
        </div>
    </form>
</body>
</html>
