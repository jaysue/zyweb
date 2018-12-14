using QJY.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;
using FastReflectionLib;
using System.Data;
using QJY.Data;
using Newtonsoft.Json;
using Senparc.Weixin.QY.Entities;
using QJY.Common;


namespace QJY.API
{
    public class GZBGManage : IWsService
    {
        public void ProcessRequest(HttpContext context, ref Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            MethodInfo methodInfo = typeof(GZBGManage).GetMethod(msg.Action.ToUpper());
            GZBGManage model = new GZBGManage();
            methodInfo.FastInvoke(model, new object[] { context, msg, P1, P2, UserInfo });
        }
        /// <summary>
        /// 获取日志列表
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1">日志类型</param>
        /// <param name="P2">查询条件</param>
        /// <param name="strUserName"></param>
        public void GETGZBGLIST(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {

            int DataID = -1;
            int.TryParse(context.Request["ID"] ?? "-1", out DataID);//微信获取单个数据的ID


            int page = 0;
            int pagecount = 8;
            int.TryParse(context.Request["p"] ?? "1", out page);//页码
            int.TryParse(context.Request["pagecount"] ?? "8", out pagecount);//页数
            page = page == 0 ? 1 : page;
            int recordCount = 0;
            string strWhere = string.Format(" bg.ComId={0} ", UserInfo.User.ComId);
            string type = context.Request["type"] ?? "1";
            if (type == "2")//下属报告
            {
                //获取当前登录人负责的下属人员 
                string Users = new JH_Auth_UserB().GetUserBranchUsers(UserInfo.User.ComId.Value, UserInfo.User.UserName).Select(d => d.UserName).ToList().ListTOString(',');
                strWhere += string.Format("and   bg.CRUser in ('{0}')", Users.ToFormatLike());
            }
            else if (type == "1") //当前登录人报告
            {
                strWhere += string.Format("and   bg.CRUser='{0}'", UserInfo.User.UserName);
            }
            else if (type == "3")//获取抄送人报告
            {
                strWhere += string.Format("and    ','+bg.ChaoSongUser+',' like '%{0}%'", UserInfo.User.UserName);
            }

            if (P1 != "")//分类
            {
                strWhere += string.Format("And  bg.LeiBie={0}", P1);
            }
            if (P2 != "")//内容查询
            {
                strWhere += string.Format(" And (bg.RBContent like '%{0}%' OR bg.CRUserName like '%{0}%' OR bg.BranchName like '%{0}%' )", P2);
            }
            //根据创建时间查询
            string time = context.Request["time"] ?? "";
            if (time != "")
            {
                if (time == "1")   //近一周
                {
                    strWhere += string.Format(" And datediff(day,bg.RBDate,getdate())<7");
                }
                else if (time == "2")
                {  //近一月
                    strWhere += string.Format(" And datediff(day,bg.RBDate,getdate())<30");
                }
                else if (time == "3")  //自定义时间
                {
                    string strTime = context.Request["starTime"] ?? "";
                    string endTime = context.Request["endTime"] ?? "";
                    if (strTime != "")
                    {
                        strWhere += string.Format(" And convert(varchar(10),bg.RBDate,120) >='{0}'", strTime);
                    }
                    if (endTime != "")
                    {
                        strWhere += string.Format(" And convert(varchar(10),bg.RBDate,120) <='{0}'", endTime);
                    }
                }
            }
            if (DataID != -1)
            {
                string strIsHasDataQX = new JH_Auth_QY_ModelB().ISHASDATAREADQX("GZBG", DataID, UserInfo);
                if (strIsHasDataQX == "Y")
                {
                    strWhere += string.Format(" And bg.ID = '{0}'", DataID);
                }
                //更新消息为已读状态
                new JH_Auth_User_CenterB().ReadMsg(UserInfo, DataID, "GZBG");

            }
            DataTable dt = new SZHL_GZBGB().GetDataPager(" SZHL_GZBG  bg inner join JH_Auth_ZiDian zd on LeiBie= zd.ID and Class=6  ", "bg.BranchName,bg.CRUserName,CONVERT(varchar(100), bg.RBDate, 23) as RBDate,zd.TypeName,bg.CRUser,bg.RBContent,bg.RBJSR,bg.RBWCQK,bg.LeiBie,bg.CRDate,bg.ID,bg.Files ", pagecount, page, "bg.CRDate desc", strWhere, ref recordCount);
            string Ids = "";
            string fileIDs = "";
            foreach (DataRow row in dt.Rows)
            {
                Ids += row["ID"].ToString() + ",";
                if (!string.IsNullOrEmpty(row["Files"].ToString()))
                {
                    fileIDs += row["Files"].ToString() + ",";
                }
            }
            Ids = Ids.TrimEnd(',');
            fileIDs = fileIDs.TrimEnd(',');

            dt.Columns.Add("PLList", Type.GetType("System.Object"));
            dt.Columns.Add("FileList", Type.GetType("System.Object"));
            if (Ids != "")
            {
                List<FT_File> FileList = new List<FT_File>();
                DataTable dtPL = new JH_Auth_TLB().GetDTByCommand(string.Format("SELECT tl.* FROM JH_Auth_TL tl WHERE tl.MSGType='GZBG' AND  tl.MSGTLYID in ({0})", Ids));
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
                foreach (DataRow row in dt.Rows)
                {
                    row["PLList"] = dtPL.FilterTable("MSGTLYID='" + row["ID"] + "'");
                    if (FileList.Count > 0)
                    {

                        string[] fileIds = row["Files"].ToString().Split(',');
                        row["FileList"] = FileList.Where(d => fileIds.Contains(d.ID.ToString()));
                    }
                }
            }
            msg.Result = dt;
            msg.Result1 = recordCount;
        }
        /// <summary>
        /// 获取日志列表
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1">日志类型</param>
        /// <param name="P2">查询条件</param>
        /// <param name="strUserName"></param>
        public void GETGZBGUSERLIST(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {

            string userName = UserInfo.User.UserName;
            string strUserName = context.Request["username"] ?? "";//经费报销统计中的查看列表需要的参数
            if (!string.IsNullOrEmpty(strUserName))
            {
                userName = strUserName;
            }
            string strWhere = string.Format(" bg.ComId={0} ", UserInfo.User.ComId);

            strWhere += string.Format("and   bg.CRUser='{0}'", userName);


            if (P1 != "")//分类
            {
                strWhere += string.Format("And  bg.LeiBie={0}", P1);
            }
            if (P2 != "")//内容查询
            {
                strWhere += string.Format(" And (bg.RBContent like '%{0}%' OR bg.CRUserName like '%{0}%' OR bg.BranchName like '%{0}%' )", P2);
            }

            int month = 0;
            int.TryParse(context.Request["month"] ?? "1", out month);
            string strTime = new DateTime(DateTime.Now.Year, month, 1).ToShortDateString();
            string endTime = new DateTime(DateTime.Now.Year, month, 1).AddMonths(1).ToShortDateString();
            strWhere += string.Format("And bg.RBDate BETWEEN '{0}' and '{1}'", strTime, endTime);

            string strSql = string.Format(" Select bg.BranchName,bg.RBContent,bg.RBJSR,bg.RBWCQK,bg.RBDate,bg.LeiBie,bg.CRUser,bg.CRDate,bg.ID,bg.Files,bg.CRUserName,zd.TypeName  from  SZHL_GZBG  bg inner join JH_Auth_ZiDian zd on LeiBie= zd.ID and Class=6 Where {0} order by bg.CRDate desc", strWhere);
            msg.Result = new SZHL_GZBGB().GetDTByCommand(strSql);
        }

        /// <summary>
        /// 导出工作报告
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void EXPORTGZBG(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            GETGZBGLIST(context, msg, P1, P2, UserInfo);
            DataTable dt = msg.Result;
            string sqlCol = "BranchName|部门,TypeName|类型,RBDate|日期,RBContent|报告内容,TypeName|类型,CRUserName|姓名,RBWCQK|完成情况";
            CommonHelp ch = new CommonHelp();

            DataTable dt2 = dt.DelTableCol(sqlCol);
            foreach (DataRow dr in dt2.Rows)
            {
                try
                {
                    dr["报告内容"] = CommonHelp.RemoveHtml(dr["报告内容"].ToString());
                    dr["完成情况"] = CommonHelp.RemoveHtml(dr["完成情况"].ToString());
                }
                catch (Exception)
                {

                }
            }

            msg.ErrorMsg = ch.ExportToExcel("工作报告", dt2);
        }

        /// <summary>
        /// 添加工作日志
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="strUserName"></param>
        public void ADDGZBG(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            SZHL_GZBG GZBG = JsonConvert.DeserializeObject<SZHL_GZBG>(P1);
            if (GZBG == null)
            {
                msg.ErrorMsg = "添加失败";
                return;
            }
            if (string.IsNullOrWhiteSpace(GZBG.RBContent))
            {
                msg.ErrorMsg = "日报内容不能为空";
                return;
            }
            if (GZBG.LeiBie == null)
            {
                msg.ErrorMsg = "日报类型不能为空";
                return;
            }
            if (P2 != "") // 处理微信上传的图片
            {

                string fids = new FT_FileB().ProcessWxIMG(P2, "GZBG", UserInfo);

                if (!string.IsNullOrEmpty(GZBG.Files))
                {
                    GZBG.Files += "," + fids;
                }
                else
                {
                    GZBG.Files = fids;
                }
            }


            if (GZBG.ID == 0)
            {
                GZBG.CRDate = DateTime.Now;
                GZBG.CRUser = UserInfo.User.UserName;
                GZBG.CRUserName = UserInfo.User.UserRealName;
                GZBG.BranchName = UserInfo.BranchInfo.DeptName;
                GZBG.ComId = UserInfo.User.ComId;
                new SZHL_GZBGB().Insert(GZBG);
                //发送消息给传送人 
                if (!string.IsNullOrEmpty(GZBG.ChaoSongUser))
                {
                    SZHL_TXSX CSTX = new SZHL_TXSX();
                    CSTX.Date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    CSTX.APIName = "GZBG";
                    CSTX.ComId = UserInfo.User.ComId;
                    CSTX.FunName = "SENDGZBG";
                    CSTX.CRUserRealName = UserInfo.User.UserRealName;
                    CSTX.MsgID = GZBG.ID.ToString();
                    CSTX.TXContent = UserInfo.User.UserRealName + "抄送一个工作报告，请您查阅";
                    CSTX.ISCS = "Y";
                    CSTX.TXUser = GZBG.ChaoSongUser;
                    CSTX.TXMode = "GZBG";
                    CSTX.CRUser = UserInfo.User.UserName;
                    TXSX.TXSXAPI.AddALERT(CSTX); //时间为发送时间
                }
            }
            else
            {
                new SZHL_GZBGB().Update(GZBG);
            }


            msg.Result = GZBG;
        }
        /// <summary>
        /// 删除日报
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1">日报ID</param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void DELGZBGBYID(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            try
            {

                if (new SZHL_GZBGB().Delete(d => d.ID.ToString() == P1))
                {
                    if (new JH_Auth_TLB().Delete(d => d.MSGTLYID == P1 && d.MSGType == "GZBG"))
                    {
                        msg.ErrorMsg = "";
                    }
                }
            }
            catch (Exception ex)
            {
                msg.ErrorMsg = ex.Message;
            }
        }
        public void GETGZBGMODEL(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            int Id = 0;
            int.TryParse(P1, out Id);
            SZHL_GZBG sg = new SZHL_GZBGB().GetEntity(d => d.ID == Id && d.ComId == UserInfo.User.ComId);
            msg.Result = sg;
            if (sg != null)
            {
                msg.Result3 = new JH_Auth_ZiDianB().GetEntity(d => d.ID == sg.LeiBie).TypeName;
            }

            DataTable dtPL = new SZHL_GZBGB().GetDTByCommand("  SELECT *  FROM JH_Auth_TL WHERE MSGType='GZBG' AND  MSGTLYID='" + P1 + "'");
            dtPL.Columns.Add("FileList", Type.GetType("System.Object"));
            foreach (DataRow dr in dtPL.Rows)
            {
                if (dr["MSGisHasFiles"] != null && dr["MSGisHasFiles"].ToString() != "")
                {
                    int[] fileIds = dr["MSGisHasFiles"].ToString().SplitTOInt(',');
                    dr["FileList"] = new FT_FileB().GetEntities(d => fileIds.Contains(d.ID));
                }
            }

            msg.Result1 = dtPL;
            if (!string.IsNullOrEmpty(sg.Files))
            {
                int[] fileIds = sg.Files.SplitTOInt(',');
                msg.Result2 = new FT_FileB().GetEntities(d => fileIds.Contains(d.ID));
            }

            //更新消息为已读状态
            if (sg != null)
            {
                new JH_Auth_User_CenterB().ReadMsg(UserInfo, sg.ID, "GZBG");
            }


        }
        #region 工作报告统计
        /// <summary>
        /// 工作报告统计
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void GETGZBGTJ(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            int page = 0;
            int pagecount = 8;
            int.TryParse(context.Request["p"] ?? "1", out page);//页码
            int.TryParse(context.Request["pagecount"] ?? "8", out pagecount);//页数
            page = page == 0 ? 1 : page;
            int recordCount = 0;

            string starDate = (P1 == "" ? DateTime.Now.Year.ToString() : P1) + "-";
            string EndDate = "";
            if (P2 == "0" || P2 == "")
            {
                starDate = starDate + "01" + "-01";
                EndDate = (P1 == "" ? DateTime.Now.Year.ToString() : P1) + "-12" + "-31";

            }
            else
            {
                starDate = starDate + P2 + "-01";
                EndDate = DateTime.Parse(starDate).AddMonths(1).AddDays(-1).ToString("yyyy-MM-dd");
            }
            string searchContent = context.Request["search"] ?? "";
            string strWhere = "";
            if (!string.IsNullOrEmpty(searchContent))
            {
                strWhere = string.Format(" And ( BG.CRUserName like '%{0}%'  OR BG.BranchName like '%{1}%' )", searchContent, searchContent);
            }
            string strSql = string.Format(@"SELECT   BG.BranchName,BG.CRUser, zd.TypeName, DATEPART(MONTH,BG.CRDate) ccMonth , COUNT(BG.ID) daycount from SZHL_GZBG BG inner join JH_Auth_ZiDian zd on BG.LeiBie=zd.ID  and zd.COMID='{0}' WHERE   BG.ComID='{1}'  AND  BG.RBDate BETWEEN  '{2}' and '{3}'" + strWhere + " GROUP by  BG.BranchName,BG.CRUser, zd.TypeName, DATEPART(MONTH,BG.CRDate) ", UserInfo.User.ComId, UserInfo.User.ComId, starDate, EndDate);
            DataTable dt = new SZHL_GZBGB().GetDataPager("(" + strSql + ") as gzbgtjtable ", "*", pagecount, page, " BranchName,CRUser,ccMonth,TypeName ", " 1=1 ", ref recordCount);
            msg.Result = dt;
            msg.Result1 = recordCount;
        }
        #endregion
        public void SENDGZBG(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            SZHL_TXSX TX = JsonConvert.DeserializeObject<SZHL_TXSX>(P1);
            Article ar0 = new Article();
            ar0.Title = TX.TXContent;
            ar0.Description = "";
            ar0.Url = TX.MsgID;
            List<Article> al = new List<Article>();
            al.Add(ar0);
            if (!string.IsNullOrEmpty(TX.TXUser))
            {
                try
                {
                    //发送PC消息
                    UserInfo = new JH_Auth_UserB().GetUserInfo(TX.ComId.Value, TX.CRUser);
                    WXHelp wx = new WXHelp(UserInfo.QYinfo);
                    wx.SendTH(al, TX.TXMode, "A", TX.TXUser);
                    new JH_Auth_User_CenterB().SendMsg(UserInfo, TX.TXMode, TX.TXContent, TX.MsgID, TX.TXUser, "A", 0, TX.ISCS);
                }
                catch (Exception ex)
                {
                    CommonHelp.WriteLOG(ex.Message.ToString());
                }
                //发送微信消息

            }
        }
    }
}