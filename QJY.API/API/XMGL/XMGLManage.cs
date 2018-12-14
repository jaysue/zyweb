using QJY.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;
using FastReflectionLib;
using QJY.Data;
using Newtonsoft.Json;
using System.Data;
using Senparc.Weixin.QY.Entities;
using QJY.Common;

namespace QJY.API
{
    public class XMGLManage : IWsService
    {
        public void ProcessRequest(HttpContext context, ref Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            MethodInfo methodInfo = typeof(XMGLManage).GetMethod(msg.Action.ToUpper());
            XMGLManage model = new XMGLManage();
            methodInfo.FastInvoke(model, new object[] { context, msg, P1, P2, UserInfo });
        }
        /// <summary>
        /// 查看项目的详细
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void GETXMGLMODEL(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            int Id = 0;
            int.TryParse(P1, out Id);
            DataTable Model = new SZHL_XMGLB().GetDTByCommand("SELECT xm.*,zd.TypeName from SZHL_XMGL xm left join JH_Auth_ZiDian zd on convert(varchar,YXJ)=convert(varchar,zd.ID) WHERE  xm.ID=" + Id + "");
            msg.Result = Model;
            if (Model != null && Model.Rows.Count > 0)
            {
                DataTable dtpl = new JH_Auth_TLB().GetTL("XMGL", Model.Rows[0]["ID"].ToString());
                msg.Result1 = dtpl;

                DataTable filedt = dtpl.Copy();
                if (filedt != null && filedt.Rows.Count > 0)
                {
                    foreach (DataRow dr in filedt.Rows)
                    {
                        if (dr["MSGisHasFiles"] == null || dr["MSGisHasFiles"].ToString() == "")
                        {
                            dr.Delete();
                        }
                    }
                    filedt.AcceptChanges();
                    msg.Result3 = filedt;
                }

                if (Model.Rows[0]["Files"].ToString() != "")
                {
                    int[] fileIDs = Model.Rows[0]["Files"].ToString().SplitTOInt(',');
                    List<FT_File> FileList = new FT_FileB().GetEntities(d => fileIDs.Contains(d.ID)).ToList();
                    msg.Result2 = FileList;
                }
            }

        }
        /// <summary>
        /// 添加项目管理
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="strParamData"></param>
        public void ADDXMGL(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            try
            {
                SZHL_XMGL xmgl = JsonConvert.DeserializeObject<SZHL_XMGL>(P1);
                if (string.IsNullOrEmpty(xmgl.XMMC))
                {
                    msg.ErrorMsg = "项目名称不能为空";
                    return;
                }
                if (string.IsNullOrEmpty(xmgl.YXJ))
                {
                    msg.ErrorMsg = "请选择项目类型";
                    return;
                }
                if (string.IsNullOrEmpty(xmgl.XMFZR))
                {
                    msg.ErrorMsg = "请选择负责人";
                    return;
                }
                if (xmgl.StartDate != null && xmgl.EndDate != null)
                {
                    if (xmgl.StartDate >= xmgl.EndDate)
                    {
                        msg.ErrorMsg = "结束时间必须大于开始时间";
                        return;
                    }
                }



                #region
                if (P2 != null && P2 != "") // 处理微信上传的图片
                {

                    string fids = new FT_FileB().ProcessWxIMG(P2, "QYHD", UserInfo);

                    if (!string.IsNullOrEmpty(xmgl.Files))
                    {
                        xmgl.Files += "," + fids;
                    }
                    else
                    {
                        xmgl.Files = fids;
                    }
                }
                #endregion
                if (xmgl.ID == 0)
                {
                    xmgl.UpdateDate = DateTime.Now;
                    xmgl.CRDate = DateTime.Now;
                    xmgl.CRUser = UserInfo.User.UserName;
                    xmgl.ComId = UserInfo.User.ComId;
                    xmgl.CRUserName = UserInfo.User.UserRealName;
                    new SZHL_XMGLB().Insert(xmgl);


                    string jsr = xmgl.XMFZR + (string.IsNullOrEmpty(xmgl.XMCYR) ? "" : "," + xmgl.XMCYR);
                    if (!string.IsNullOrEmpty(jsr))
                    {
                        SZHL_TXSX TX = new SZHL_TXSX();
                        TX.Date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                        TX.APIName = "XMGL";
                        TX.ComId = UserInfo.User.ComId;
                        TX.FunName = "XMGLMSG";
                        TX.TXMode = "XMGL";
                        TX.CRUserRealName = UserInfo.User.UserRealName;
                        TX.MsgID = xmgl.ID.ToString();
                        TX.TXContent = UserInfo.User.UserRealName + "发起了一个项目，等待您处理";
                        TX.TXUser = jsr;
                        TX.CRUser = UserInfo.User.UserName;
                        TXSX.TXSXAPI.AddALERT(TX); //时间为发送时间
                    }
                }
                else
                {
                    xmgl.UpdateDate = DateTime.Now;
                    xmgl.UpdateUser = UserInfo.User.UserName;
                    xmgl.UpdateUserName = UserInfo.User.UserRealName;
                    new SZHL_XMGLB().Update(xmgl);
                }
                msg.Result = xmgl;

            }
            catch (Exception)
            {

                msg.ErrorMsg = "添加项目失败";
            }
        }
        public void DELXMGL(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            int Id = 0;
            int.TryParse(P2, out Id);
            new SZHL_XMGLB().Delete(D => D.ID == Id);
        }
        public void GETXMGLDATA(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            string strWhere = "  xm.ComId=" + UserInfo.User.ComId;
            if (P1 != "")
            {
                strWhere += string.Format(" And xm.Status='{0}'", P1);
            }
            if (P2 != "")
            {

                switch (P2)
                {
                    case "0":
                        strWhere += string.Format(" And (xm.CRUser='{0}' or  ','+XMFZR+','  like '%,{0},%'  or ','+XMCYR+','  like '%,{0},%' )", UserInfo.User.UserName);
                        break;
                    case "1":
                        strWhere += string.Format(" And xm.CRUser='{0}'", UserInfo.User.UserName);
                        break;
                    case "2":
                        strWhere += string.Format(" And (','+XMFZR+','  like '%," + UserInfo.User.UserName + ",%' )");
                        break;
                    case "3":
                        strWhere += string.Format(" And (','+XMCYR+','  like '%," + UserInfo.User.UserName + ",%' )");
                        break;
                }
            }
            string lb = context.Request["lb"] ?? "";
            if (lb != "")
            {
                strWhere += string.Format(" And YXJ='{0}'", lb);
            }

            string strContent = context.Request["Content"] ?? "";
            strContent = strContent.TrimEnd();
            if (strContent != "")
            {
                strWhere += string.Format(" And ( XMMC like '%{0}%' )", strContent);
            }
            //根据创建时间查询
            string time = context.Request["time"] ?? "";
            if (time != "")
            {
                if (time == "1")   //近一周
                {
                    strWhere += string.Format(" And datediff(day,xm.CRDate,getdate())<7");
                }
                else if (time == "2")
                {  //近一月
                    strWhere += string.Format(" And datediff(day,xm.CRDate,getdate())<30");
                }
                else if (time == "4")
                {  //今年
                    strWhere += string.Format(" And datediff(year,xm.CRDate,getdate())=0");
                }
                else if (time == "5")
                {  //上一年
                    strWhere += string.Format(" And datediff(year,xm.CRDate,getdate())=1");
                }
                else if (time == "3")  //自定义时间
                {
                    string strTime = context.Request["starTime"] ?? "";
                    string endTime = context.Request["endTime"] ?? "";
                    if (strTime != "")
                    {
                        strWhere += string.Format(" And convert(varchar(10),xm.CRDate,120) >='{0}'", strTime);
                    }
                    if (endTime != "")
                    {
                        strWhere += string.Format(" And convert(varchar(10),xm.CRDate,120) <='{0}'", endTime);
                    }
                }
            }
            int DataID = -1;
            int.TryParse(context.Request["ID"] ?? "-1", out DataID);//记录Id
            if (DataID != -1)
            {
                string strIsHasDataQX = new JH_Auth_QY_ModelB().ISHASDATAREADQX("CRM", DataID, UserInfo);
                if (strIsHasDataQX == "Y")
                {
                    strWhere += string.Format(" And xm.ID = '{0}'", DataID);
                }

            }


            int page = 0;
            int pagecount = 8;
            int.TryParse(context.Request["p"] ?? "1", out page);
            int.TryParse(context.Request["pagecount"] ?? "8", out pagecount);//页数
            page = page == 0 ? 1 : page;
            int total = 0;
            DataTable dtList = new SZHL_XMGLB().GetDataPager("SZHL_XMGL xm left join JH_Auth_ZiDian zd on convert(varchar,YXJ)=convert(varchar,zd.ID)", "xm.* ,zd.TypeName", pagecount, page, " xm.CRDate desc,xm.Status ", strWhere, ref total);

            dtList.Columns.Add("PLList", Type.GetType("System.Object"));
            dtList.Columns.Add("FileList", Type.GetType("System.Object"));
            for (int i = 0; i < dtList.Rows.Count; i++)
            {
                dtList.Rows[i]["PLList"] = new JH_Auth_TLB().GetTL("XMGL", dtList.Rows[i]["ID"].ToString());
                if (dtList.Rows[i]["Files"].ToString() != "")
                    dtList.Rows[i]["FileList"] = new FT_FileB().GetEntities(" ID in (" + dtList.Rows[i]["Files"].ToString() + ")");
            }


            msg.Result = dtList;
            msg.Result1 = total;
        }

        public void GETXMLIST(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            string sql = string.Format("SELECT sx.ID,sx.XMMC FROM SZHL_XMGL sx WHERE sx.ComId={0}", UserInfo.User.ComId);
            msg.Result = new SZHL_XMGLB().GetDTByCommand(sql);
        }

        #region 任务发送消息的接口
        public void XMGLMSG(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            SZHL_TXSX TX = JsonConvert.DeserializeObject<SZHL_TXSX>(P1);
            int rwid = 0;
            int.TryParse(TX.MsgID, out rwid);
            SZHL_XMGL xm = new SZHL_XMGLB().GetEntity(d => d.ID == rwid);

            Article ar0 = new Article();
            ar0.Title = TX.TXContent;
            ar0.Description = xm == null ? "" : xm.XMMC;
            ar0.Url = TX.MsgID;
            List<Article> al = new List<Article>();
            al.Add(ar0);
            if (!string.IsNullOrEmpty(TX.TXUser))

                try
                {
                    //发送PC消息
                    UserInfo = new JH_Auth_UserB().GetUserInfo(TX.ComId.Value, TX.CRUser);
                    new JH_Auth_User_CenterB().SendMsg(UserInfo, TX.TXMode, TX.TXContent, TX.MsgID, TX.TXUser, "B", 0, TX.ISCS);
                }
                catch (Exception)
                {
                }

            //发送微信消息
            WXHelp wx = new WXHelp(UserInfo.QYinfo);
            wx.SendTH(al, TX.TXMode, "B", TX.TXUser);
        }
        #endregion
    }
}