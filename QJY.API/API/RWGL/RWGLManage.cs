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
    public class RWGLManage : IWsService
    {
        public void ProcessRequest(HttpContext context, ref Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            MethodInfo methodInfo = typeof(RWGLManage).GetMethod(msg.Action.ToUpper());
            RWGLManage model = new RWGLManage();
            methodInfo.FastInvoke(model, new object[] { context, msg, P1, P2, UserInfo });
        }
        //添加任务管理
        public void ADDRWGL(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            SZHL_RWGL rwgl = JsonConvert.DeserializeObject<SZHL_RWGL>(P1);
            if (string.IsNullOrWhiteSpace(rwgl.LeiBie))
            {
                msg.ErrorMsg = "请选择任务类型";
                return;
            }
            if (string.IsNullOrWhiteSpace(rwgl.RWFZR))
            {
                msg.ErrorMsg = "请选择任务负责人";
                return;
            }
            if (string.IsNullOrWhiteSpace(rwgl.RWTitle))
            {
                msg.ErrorMsg = "请填写任务内容";
                return;
            }
            if (P2 != "") // 处理微信上传的图片
            {
                string fids = new FT_FileB().ProcessWxIMG(P2, "RWGL", UserInfo);

                if (!string.IsNullOrEmpty(rwgl.Files))
                {
                    rwgl.Files += "," + fids;
                }
                else
                {
                    rwgl.Files = fids;
                }
            }

            if (rwgl.ID == 0)
            {
                rwgl.CRDate = DateTime.Now;
                rwgl.ComId = UserInfo.User.ComId;
                rwgl.CRUserName = UserInfo.User.UserRealName;
                rwgl.CRUser = UserInfo.User.UserName;
                new SZHL_RWGLB().Insert(rwgl);
                string jsr = rwgl.RWFZR + (string.IsNullOrEmpty(rwgl.RWCYR) ? "" : "," + rwgl.RWCYR);
                if (!string.IsNullOrEmpty(jsr))
                {
                    SZHL_TXSX TX = new SZHL_TXSX();
                    TX.Date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    TX.APIName = "RWGL";
                    TX.ComId = UserInfo.User.ComId;
                    TX.FunName = "RWGLMSG";
                    TX.TXMode = "RWGL";
                    TX.CRUserRealName = UserInfo.User.UserRealName;
                    TX.MsgID = rwgl.ID.ToString();
                    TX.TXContent = UserInfo.User.UserRealName + "发起了一个任务，等待您处理";
                    TX.TXUser = jsr;
                    TX.CRUser = UserInfo.User.UserName;
                    TXSX.TXSXAPI.AddALERT(TX); //时间为发送时间
                }
                if (!string.IsNullOrEmpty(rwgl.KHFXRS))
                {
                    SZHL_TXSX TX = new SZHL_TXSX();
                    TX.Date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    TX.APIName = "RWGL";
                    TX.ComId = UserInfo.User.ComId;
                    TX.FunName = "RWGLMSG";
                    TX.TXMode = "RWGL";
                    TX.CRUserRealName = UserInfo.User.UserRealName;
                    TX.MsgID = rwgl.ID.ToString();
                    TX.TXContent = UserInfo.User.UserRealName + "抄送了一个任务，请您查阅";
                    TX.TXUser = rwgl.KHFXRS;
                    TX.ISCS = "Y";
                    TX.CRUser = UserInfo.User.UserName;
                    TXSX.TXSXAPI.AddALERT(TX); //时间为发送时间
                }
                DateTime jzDate;
                DateTime.TryParse(rwgl.RWJZDate, out jzDate);
                //设置结束日期8点提醒，并且截止日期大于当天
                if (rwgl.IsTX != null && rwgl.IsTX.ToLower() == "true" && jzDate.Date > DateTime.Now.Date)
                {
                    SZHL_TXSX TX = new SZHL_TXSX();
                    TX.Date = jzDate.Date.ToString("yyyy-MM-dd") + " 8:00";
                    TX.APIName = "RWGL";
                    TX.ComId = UserInfo.User.ComId;
                    TX.FunName = "RWGLMSG";
                    TX.TXMode = "RWGL";
                    TX.CRUserRealName = UserInfo.User.UserRealName;
                    TX.MsgID = rwgl.ID.ToString();
                    TX.TXContent = UserInfo.User.UserRealName + "发起的任务即将到期";
                    TX.TXUser = jsr;
                    TX.CRUser = UserInfo.User.UserName;
                    TXSX.TXSXAPI.AddALERT(TX); //时间为发送时间
                }
            }
            else
            {
                //判断是否有新加参与人，有新加参与人给参与人发送消息
                SZHL_RWGL rwglold = new SZHL_RWGLB().GetEntity(d => d.ID == rwgl.ID);
                string jsrNew = "";
                foreach (string jsr in rwgl.RWCYR.Split(','))
                {
                    if (!rwglold.RWCYR.Split(',').Contains(jsr))
                    {
                        jsrNew += jsr + ",";
                    }
                }
                if (jsrNew.Length > 0)
                {
                    jsrNew = jsrNew.Substring(0, jsrNew.Length - 1);
                    SZHL_TXSX TX = new SZHL_TXSX();
                    TX.Date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    TX.APIName = "RWGL";
                    TX.ComId = UserInfo.User.ComId;
                    TX.FunName = "RWGLMSG";
                    TX.TXMode = "RWGL";
                    TX.CRUserRealName = UserInfo.User.UserRealName;
                    TX.MsgID = rwgl.ID.ToString();
                    TX.TXContent = UserInfo.User.UserRealName + "发起了一个任务，等待您处理";
                    TX.TXUser = jsrNew;
                    TX.CRUser = UserInfo.User.UserName;
                    TXSX.TXSXAPI.AddALERT(TX); //时间为发送时间
                }
                new SZHL_RWGLB().Update(rwgl);
            }
        }
        public void SENDTXMSG(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            int Id = 0;
            int.TryParse(P1, out Id);
            SZHL_RWGL rwgl = new SZHL_RWGLB().GetEntity(d => d.ID == Id);
            if (rwgl != null)
            {
                SZHL_TXSX TX = new SZHL_TXSX();
                TX.Date = DateTime.Now.ToString();
                TX.APIName = "RWGL";
                TX.ComId = UserInfo.User.ComId;
                TX.FunName = "RWGLMSG";
                TX.TXMode = "RWGL";
                TX.CRUserRealName = UserInfo.User.UserRealName;
                TX.MsgID = P1;
                TX.TXContent = UserInfo.User.UserRealName + "发起的任务即将到期,请尽快处理";
                TX.TXUser = rwgl.RWFZR;
                TX.CRUser = UserInfo.User.UserName;
                TXSX.TXSXAPI.AddALERT(TX); //时间为发送时间
            }
        }
        public void GETRWGLLIST(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            string userName = UserInfo.User.UserName;
            string strWhere = " bg.ComId =" + UserInfo.User.ComId.Value;
            if (P1 != "")
            {
                if (P1 == "2")
                {
                    //  strWhere += string.Format(" And datediff(day,bg.RWJZDate,getdate())>0");
                    strWhere += string.Format(" And bg.RWJZDate < CONVERT(char(10), GETDATE(), 120) And RWStatus='0'");
                }
                else if (P1 == "1")
                {
                    strWhere += string.Format(" And RWStatus={0}", P1);
                }
                else if (P1 == "0")
                {
                    //strWhere += string.Format(" And bg.RWJZDate >= CONVERT(char(10), GETDATE(), 120) And RWStatus={0}", P1);
                    strWhere += string.Format(" And RWStatus={0}", P1);
                }
            }
            if (P2 != "")
            {

                switch (P2)
                {
                    case "0":
                        strWhere += string.Format(" And (bg.CRUser='{0}' or  ','+RWFZR+','  like '%,{0},%'  or ','+RWCYR+','  like '%,{0},%'  or ','+KHFXRS+','  like '%,{0},%' )", userName);
                        break;
                    case "1":
                        strWhere += string.Format(" And (','+RWFZR+','  like '%," + userName + ",%' )");
                        break;
                    case "2":
                        strWhere += string.Format(" And bg.CRUser='{0}'", UserInfo.User.UserName);
                        break;
                    case "3":
                        strWhere += string.Format(" And (','+RWCYR+','  like '%," + userName + ",%' )");
                        break;
                    case "4":
                        strWhere += string.Format(" And (','+KHFXRS+','  like '%," + userName + ",%' )");
                        break;
                }
            }
            string leibie = context.Request["lb"] ?? "";
            if (leibie != "")
            {
                strWhere += string.Format(" And LeiBie='{0}' ", leibie);
            }
            string strContent = context.Request["Content"] ?? "";
            strContent = strContent.TrimEnd();
            if (strContent != "")
            {
                strWhere += string.Format(" And ( RWTitle like '%{0}%' )", strContent);
            }
            //根据时间查询数据
            string time = context.Request["time"] ?? "";
            if (time != "")
            {
                if (time == "1")   //近一周
                {
                    strWhere += string.Format(" And datediff(day,bg.CRDate,getdate())<7");
                }
                else if (time == "2")
                {  //近一月
                    strWhere += string.Format(" And datediff(day,bg.CRDate,getdate())<30");
                }
                else if (time == "3")  //自定义时间
                {
                    string strTime = context.Request["starTime"] ?? "";
                    string endTime = context.Request["endTime"] ?? "";
                    if (strTime != "")
                    {
                        strWhere += string.Format(" And convert(varchar(10),bg.CRDate,120) >='{0}'", strTime);
                    }
                    if (endTime != "")
                    {
                        strWhere += string.Format(" And convert(varchar(10),bg.CRDate,120) <='{0}'", endTime);
                    }
                }
            }
            int DataID = -1;
            int.TryParse(context.Request["ID"] ?? "-1", out DataID);//记录Id
            if (DataID != -1)
            {
                string strIsHasDataQX = new JH_Auth_QY_ModelB().ISHASDATAREADQX("RWGL", DataID, UserInfo);
                if (strIsHasDataQX == "Y")
                {
                    strWhere += string.Format(" And bg.ID = '{0}'", DataID);
                }
                //更新消息为已读状态
                new JH_Auth_User_CenterB().ReadMsg(UserInfo, DataID, "RWGL");
            }
            DataTable dtList = new DataTable();
            int page = 0;
            int pagecount = 8;
            int.TryParse(context.Request["p"] ?? "1", out page);//页码
            int.TryParse(context.Request["pagecount"] ?? "8", out pagecount);//页数
            page = page == 0 ? 1 : page;
            int recordCount = 0;
            dtList = new SZHL_GZBGB().GetDataPager(" SZHL_RWGL  bg LEFT JOIN JH_Auth_ZiDian zd on LeiBie= zd.ID and Class=7  LEFT JOIN JH_Auth_User jau ON bg.RWFZR = jau.UserName AND jau.ComId=bg.ComId ", @" [CRUserName],
	jau.UserRealName,
	zd.TypeName,
	[RWJZDate],
	[RWTitle],
	[RWContent],
	[LeiBie],
	[RWFZR],
	[RWCYR],
	[RWStatus],
	[BiaoQian],
	[BeiZhu],
	[IsSend],
	[IsComPlete],
	[IsCancel],
	[CancelDate],
	bg.[CRDate],
	bg.[CRUser],
	bg.[Files],
	bg.[Remark],
	bg.[ID],
	[intProcessStanceid],
	[KHFXRS],
	[TopID],
	bg.[ComId],
	[TaskJD],
	[IsTX],CASE WHEN RWJZDate<CONVERT(char(20), GETDATE(), 23) THEN 1 else 0 END AS jzstatus", pagecount, page, "bg.RWJZDate desc", strWhere, ref recordCount);
            string Ids = "";
            string fileIDs = "";
            foreach (DataRow row in dtList.Rows)
            {
                Ids += row["ID"].ToString() + ",";
                if (!string.IsNullOrEmpty(row["Files"].ToString()))
                {
                    fileIDs += row["Files"].ToString() + ",";
                }
            }
            Ids = Ids.TrimEnd(',');
            fileIDs = fileIDs.TrimEnd(',');
            if (Ids != "")
            {
                List<FT_File> FileList = new List<FT_File>();
                DataTable dtPL = new JH_Auth_TLB().GetDTByCommand(string.Format("SELECT tl.* FROM JH_Auth_TL tl WHERE tl.MSGType='RWGL' AND  tl.MSGTLYID in ({0})", Ids));
                dtPL.Columns.Add("FileList", Type.GetType("System.Object"));
                foreach (DataRow dr in dtPL.Rows)
                {
                    if (dr["MSGisHasFiles"] != null && dr["MSGisHasFiles"].ToString() != "")
                    {
                        int[] fileIds = dr["MSGisHasFiles"].ToString().SplitTOInt(',');
                        dr["FileList"] = new FT_FileB().GetEntities(d => fileIds.Contains(d.ID));
                    }
                }


                if (!string.IsNullOrEmpty(fileIDs))
                {
                    int[] fileId = fileIDs.SplitTOInt(',');
                    FileList = new FT_FileB().GetEntities(d => fileId.Contains(d.ID)).ToList();
                }
                dtList.Columns.Add("PLList", Type.GetType("System.Object"));
                dtList.Columns.Add("FileList", Type.GetType("System.Object"));
                foreach (DataRow row in dtList.Rows)
                {
                    row["PLList"] = dtPL.FilterTable("MSGTLYID='" + row["ID"] + "'");
                    if (FileList.Count > 0)
                    {

                        string[] fileIds = row["Files"].ToString().Split(',');
                        row["FileList"] = FileList.Where(d => fileIds.Contains(d.ID.ToString()));
                    }
                }
            }
            msg.Result = dtList;
            msg.Result1 = recordCount;
        }
        //首页获取任务数据
        public void GETRWGLINDEXLIST(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            string userName = UserInfo.User.UserName;
            string strWhere = string.Format(" (bg.CRUser='{0}' or  ','+RWFZR+','  like '%,{0},%'  or ','+RWCYR+','  like '%,{0},%' ) ", userName);
            int recordCount = 0;
            DataTable dtList = new SZHL_GZBGB().GetDataPager(" SZHL_RWGL  bg inner join JH_Auth_ZiDian zd on LeiBie= zd.ID and Class=7  ", "bg.*,zd.TypeName  ", 8, 1, "bg.CRDate desc", strWhere + " And RWStatus=0", ref recordCount);
            DataTable dtList1 = new SZHL_GZBGB().GetDataPager(" SZHL_RWGL  bg inner join JH_Auth_ZiDian zd on LeiBie= zd.ID and Class=7  ", "bg.*,zd.TypeName  ", 8, 1, "bg.CRDate desc", strWhere + " And RWStatus=1", ref recordCount);
            msg.Result = dtList;
            msg.Result1 = dtList1;
        }
        //获取任务信息
        public void GETRWGLMODEL(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            int Id = int.Parse(P1);
            //SZHL_RWGL sr = new SZHL_RWGLB().GetEntity(d => d.ID == Id && d.ComId == UserInfo.User.ComId);
            DataTable dt = new SZHL_RWGLB().GetDTByCommand(string.Format("select bg.*,zd.TypeName,sx.XMMC from SZHL_RWGL bg left join JH_Auth_ZiDian zd on bg.LeiBie=zd.ID LEFT JOIN SZHL_XMGL sx ON sx.ID=bg.XMID where bg.ID='{0}' and bg.ComID='{1}' ", Id, UserInfo.User.ComId));
            msg.Result = dt;
            if (dt.Rows.Count > 0)
            {
                if (!string.IsNullOrEmpty(dt.Rows[0]["Files"].ToString()))
                {
                    int[] fileIds = dt.Rows[0]["Files"].ToString().SplitTOInt(',');
                    msg.Result2 = new FT_FileB().GetEntities(d => fileIds.Contains(d.ID));
                }
                msg.Result1 = new JH_Auth_TLB().GetTL("RWGL", dt.Rows[0]["ID"].ToString().ToString());
            }
            new JH_Auth_User_CenterB().ReadMsg(UserInfo, Id, "RWGL");
        }

        /// <summary>
        /// 导出任务
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void EXPORTRW(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            GETRWGLLIST(context, msg, P1, P2, UserInfo);

            DataTable dt = msg.Result;
            string sqlCol = "CRUserName|负责人,CRUserName|创建人,TypeName|任务类型,RWJZDate|任务截止日期,RWTitle|任务标题,RWContent|任务内容";

            CommonHelp ch = new CommonHelp();

            DataTable dt2 = dt.DelTableCol(sqlCol);
            foreach (DataRow dr in dt2.Rows)
            {
                try
                {
                    dr["任务内容"] = CommonHelp.RemoveHtml(dr["任务内容"].ToString());
                }
                catch (Exception) { }
            }

            msg.ErrorMsg = ch.ExportToExcel("任务管理", dt2);
        }

        //获取任务信息
        public void DELRWGLMODEL(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            int Id = int.Parse(P1);
            new SZHL_RWGLB().Delete(d => d.ID == Id && d.ComId == UserInfo.User.ComId);
        }
        public void COMPLETERWGL(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            //如果提醒状态的评论带结束标志，更新待办的状态
            new SZHL_RWGLB().ExsSql(string.Format("UPDATE  SZHL_RWGL SET RWStatus='{0}' WHERE ID={1} and ComId={2}", P2, P1, UserInfo.User.ComId));
        }
        #region 任务发送消息的接口
        public void RWGLMSG(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            SZHL_TXSX TX = JsonConvert.DeserializeObject<SZHL_TXSX>(P1);
            int rwid = 0;
            int.TryParse(TX.MsgID, out rwid);
            SZHL_RWGL rw = new SZHL_RWGLB().GetEntity(d => d.ID == rwid);

            Article ar0 = new Article();
            ar0.Title = TX.TXContent;
            ar0.Description = rw == null ? "" : rw.RWTitle;
            ar0.Url = TX.MsgID;
            List<Article> al = new List<Article>();
            al.Add(ar0);
            if (!string.IsNullOrEmpty(TX.TXUser))

                try
                {
                    //发送PC消息
                    UserInfo = new JH_Auth_UserB().GetUserInfo(TX.ComId.Value, TX.CRUser);
                    new JH_Auth_User_CenterB().SendMsg(UserInfo, TX.TXMode, TX.TXContent, TX.MsgID, TX.TXUser, "A", 0, TX.ISCS);
                }
                catch (Exception)
                {
                }

            //发送微信消息
            WXHelp wx = new WXHelp(UserInfo.QYinfo);
            wx.SendTH(al, TX.TXMode, "A", TX.TXUser);
        }
        #endregion

    }
}
