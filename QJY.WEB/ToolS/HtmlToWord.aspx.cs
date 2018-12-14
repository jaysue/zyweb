using QJY.Data;
using QJY.API;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace QJY.WEB
{
    public partial class HtmlToWord : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            int id = 0;
            int.TryParse(Request["ID"], out id);
            SZHL_JFBX jfbx = new SZHL_JFBXB().GetEntity(d => d.ID == id);
            if (jfbx != null)
            {
                //  ClientScript.RegisterStartupScript(ClientScript.GetType(), "myscript", "<script>SetTotalDX(" + jfbx.BXZJE + ");</script>");
                lblSQR.Text = jfbx.ShenQingRen;
                lblRemark.Text = jfbx.BXContent;
                lblBranch.Text = jfbx.BranchName;
                lblDate.Text = jfbx.BXDate.Value.ToString("yyyy年MM月dd日");
                lblTotalDX.Text = Arabia_to_Chinese(jfbx.BXZJE.ToString());
                lblTitle.Text = jfbx.JFBXTitle;

                if (jfbx.XMID != null)
                {
                    lblXM.Text = new SZHL_XMGLB().GetEntity(d => d.ID == jfbx.XMID).XMMC;
                }
                else {
                    lblXM.Text = "无关联项目";
                }
                DataTable dt = new SZHL_JFBXITEMB().GetDTByCommand("SELECT item.*,zd.TypeName from SZHL_JFBXITEM item inner join JH_Auth_ZiDian  zd on zd.ID=item.LeiBie and zd.Class=23 where JFBXID=" + id);
                repItem.DataSource = dt;
                repItem.DataBind();

                //设置Http的头信息,编码格式  
                HttpContext.Current.Response.Buffer = true;
                HttpContext.Current.Response.Clear();
                HttpContext.Current.Response.ClearContent();
                HttpContext.Current.Response.ClearHeaders();

                HttpContext.Current.Response.AppendHeader("Content-Disposition", "attachment;filename=" + HttpUtility.UrlEncode("费用报销单" + DateTime.Now.ToString("yyyyMMddHHmm") + ".doc", System.Text.Encoding.UTF8));
                HttpContext.Current.Response.ContentType = "application/ms-word";
                HttpContext.Current.Response.Charset = "GB2312";
                HttpContext.Current.Response.ContentEncoding = System.Text.Encoding.GetEncoding("GB2312");
                //关闭控件的视图状态  
                Page.EnableViewState = false;
                //初始化HtmlWriter  
                System.IO.StringWriter writer = new System.IO.StringWriter();
                System.Web.UI.HtmlTextWriter htmlWriter = new System.Web.UI.HtmlTextWriter(writer);
                Page.RenderControl(htmlWriter);
                //输出  
                string pageHtml = writer.ToString();
                int startIndex = pageHtml.IndexOf("<div class=\"fybx\" style=\"text-align: center;\">");
                int endIndex = pageHtml.LastIndexOf("</div>");
                int lenth = endIndex - startIndex;
                pageHtml = pageHtml.Substring(startIndex, lenth);
                HttpContext.Current.Response.Write(pageHtml.ToString());
                HttpContext.Current.Response.End();
            }
        }
        public string Arabia_to_Chinese(string money)
        {
            for (int i = money.Length - 1; i >= 0; i--)
            {
                money = money.Replace(",", "");//替换tomoney()中的“,”
                money = money.Replace(" ", "");//替换tomoney()中的空格
            }
            //将小写金额转换成大写金额           
            double MyNumber = Convert.ToDouble(money);
            String[] MyScale = { "分", "角", "元", "拾", "佰", "仟", "万", "拾", "佰", "仟", "亿", "拾", "佰", "仟", "兆", "拾", "佰", "仟" };
            String[] MyBase = { "零", "壹", "贰", "叁", "肆", "伍", "陆", "柒", "捌", "玖" };
            String M = "";
            bool isPoint = false;
            if (money.IndexOf(".") != -1)
            {
                money = money.Remove(money.IndexOf("."), 1);
                isPoint = true;
            }
            for (int i = money.Length; i > 0; i--)
            {
                int MyData = Convert.ToInt16(money[money.Length - i].ToString());
                M += MyBase[MyData];
                if (isPoint == true)
                {
                    M += MyScale[i - 1];
                }
                else
                {
                    M += MyScale[i + 1];
                }
            }

            if (M.IndexOf("零零") != -1)
            {
                M = M.Replace("零零", "零");
            }
            M = M.Replace("零亿", "亿");
            M = M.Replace("亿万", "亿");
            M = M.Replace("零万", "万");
            M = M.Replace("零元", "元");
            M = M.Replace("零角", "");
            M = M.Replace("零分", "");
            return M;
        }

    }
}