using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FastReflectionLib;
using System.Web;
using QJY.Data;
using Newtonsoft.Json;
using System.Data;
using QJY.Common;


namespace QJY.API
{
    public class KQGLManage : IWsService
    {
        public void ProcessRequest(HttpContext context, ref Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            MethodInfo methodInfo = typeof(KQGLManage).GetMethod(msg.Action.ToUpper());
            KQGLManage model = new KQGLManage();
            methodInfo.FastInvoke(model, new object[] { context, msg, P1, P2, UserInfo });
        }
        public void GETKQINSTALL(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            int page = 0;
            int pagecount = 8;
            int.TryParse(context.Request["p"] ?? "1", out page);//页码
            int.TryParse(context.Request["pagecount"] ?? "8", out pagecount);//页数
            page = page == 0 ? 1 : page;
            int recordCount = 0;


            string searchContent = context.Request["search"] ?? "";
            string strWhere = " ComID=" + UserInfo.User.ComId;
            if (!string.IsNullOrEmpty(searchContent))
            {
                strWhere = string.Format(" And KQTitle like '%{0}%'", searchContent);
            }
            DataTable dt = new SZHL_GZBGB().GetDataPager(" SZHL_KQBC ", "*", pagecount, page, " CRDate desc ", strWhere, ref recordCount);
            msg.Result = dt;
            msg.Result1 = recordCount;
        }
        public void ADDKQBC(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            SZHL_KQBC KQBC = JsonConvert.DeserializeObject<SZHL_KQBC>(P1);

            if (string.IsNullOrWhiteSpace(KQBC.KQTitle))
            {
                msg.ErrorMsg = "考勤班次标题不能为空";
                return;
            }


            if (KQBC.ID == 0)
            {
                List<SZHL_KQBC> kqbcList = new SZHL_KQBCB().GetEntities(d => d.KQTitle == KQBC.KQTitle && d.ComID == KQBC.ComID).ToList();
                if (kqbcList.Count > 0)
                {

                    msg.ErrorMsg = "考勤班次标题不能重复";
                    return;
                }
                KQBC.CRDate = DateTime.Now;
                KQBC.CRUser = UserInfo.User.UserName;
                KQBC.ComID = UserInfo.User.ComId;
                new SZHL_KQBCB().Insert(KQBC);
            }
            else
            {
                new SZHL_KQBCB().Update(KQBC);
            }


        }

        public void DELKQBC(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            int id = 0;
            int.TryParse(P1, out id);
            new SZHL_KQBCB().Delete(d => d.ComID == UserInfo.User.ComId && d.ID == id);
        }
        //添加考勤记录
        public void ADDKQJL(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            int type = 0;
            int.TryParse(P1, out type);
            string strSql = string.Format("SELECT  top 1 * from SZHL_KQBC where ComID={0} and (','+KQUsers+',' LIKE '%{1}%' or KQRange=0) ORDER by CRDate DESC", UserInfo.User.ComId, UserInfo.User.UserName);
            DataTable dt = new SZHL_KQBCB().GetDTByCommand(strSql);
            SZHL_KQJL kqjl = new SZHL_KQJL();
            DateTime BCDate;
            kqjl.Status = 0;
            if (dt.Rows.Count > 0)
            {
                string strHour = "";
                if (type == 0)
                {
                    strHour = dt.Rows[0]["KQStart"].ToString();

                }
                else if (type == 1)
                {

                    strHour = dt.Rows[0]["KQEnd"].ToString();
                }

                BCDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, strHour.SplitTOInt(':')[0], strHour.SplitTOInt(':')[1] + 1, 0);
                if (type == 0 && BCDate < DateTime.Now)
                {
                    kqjl.Status = 1;
                }
                if (type == 1 && BCDate > DateTime.Now)
                {
                    kqjl.Status = 2;
                }
            }
            kqjl.KQDate = DateTime.Now;
            kqjl.Type = type;
            kqjl.ComId = UserInfo.User.ComId;
            kqjl.WeekDay = DateTime.Now.DayOfWeek.ToInt32();
            kqjl.KQUser = UserInfo.User.UserName;
            kqjl.KQUserName = UserInfo.User.UserRealName;
            kqjl.KQBranch = UserInfo.BranchInfo.DeptName;
            kqjl.Longitude = context.Request["long"];
            kqjl.Latitude = context.Request["lat"];
            kqjl.Position = P2;
            new SZHL_KQJLB().Insert(kqjl);
            msg.Result = kqjl;

        }
        public void GETKQGZ(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            string strSql = string.Format("SELECT  top 1 * from SZHL_KQBC where ComID={0} and (','+KQUsers+',' LIKE '%{1}%' or KQRange=0) ORDER by CRDate DESC", UserInfo.User.ComId, UserInfo.User.UserName);
            DataTable dt = new SZHL_KQBCB().GetDTByCommand(strSql);
            msg.Result = dt;
            string strSql1 = string.Format("SELECT top 1 * from SZHL_KQJL where ComId={0} and KQUser='{1}' AND DATEDIFF(DAY,KQDate,GETDATE())=0 and Type=0 ORDER by KQDate asc", UserInfo.User.ComId, UserInfo.User.UserName);
            string strSql2 = string.Format("SELECT top 1 * from SZHL_KQJL where ComId={0} and KQUser='{1}' AND DATEDIFF(DAY,KQDate,GETDATE())=0 and Type=1  ORDER by KQDate DESC", UserInfo.User.ComId, UserInfo.User.UserName);
            msg.Result1 = new SZHL_KQBCB().GetDTByCommand(strSql1);
            msg.Result2 = new SZHL_KQBCB().GetDTByCommand(strSql2);
        }
        public void GETKQBCMODEL(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            int ID = 0;
            int.TryParse(P1, out ID);
            msg.Result = new SZHL_KQBCB().GetEntity(d => d.ID == ID);
        }
        //个人考勤日历
        public void GETKQRLLIST(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            string start = context.Request["start"];
            string end = context.Request["end"];
            DateTime sd = DateTime.Parse(start + " 00:00:00");
            DateTime ed = DateTime.Parse(end + " 23:59:59");
            string strWhere = "where ComID=" + UserInfo.QYinfo.ComId + "  and KQUser='" + UserInfo.User.UserName + "' and KQDate between '" + sd + "' and '" + ed + "'";

            List<TXSXManage.RLView> list = new List<TXSXManage.RLView>();
            DataTable dt = new SZHL_KQJLB().GetDTByCommand(" SELECT  *  FROM SZHL_KQJL  " + strWhere);
            foreach (DataRow row in dt.Rows)
            {
                TXSXManage.RLView rlView = new TXSXManage.RLView();
                rlView.title = (row["type"].ToString() == "0" ? "签到   " : "签退   ") + DateTime.Parse(row["KQDate"].ToString()).ToString("HH:mm");
                if (row["Status"].ToString() == "1")
                {
                    rlView.title += "   迟到";
                }
                else if (row["Status"].ToString() == "2")
                {
                    rlView.title += "   早退";
                }
                else
                {
                    rlView.title += "   正常";
                }
                rlView.start = row["KQDate"].ToString();
                list.Add(rlView);
            }
            msg.Result = list;
        }
        public void GETKQJLUSERLIST(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            string strWhere = string.Format(" And  ComId={0}", UserInfo.User.ComId);

            int month = 0;
            int.TryParse(context.Request["month"] ?? "0", out month);
            if (month == 0)
            {
                month = DateTime.Now.Month;
            }
            string strTime = new DateTime(DateTime.Now.Year, month, 1).ToShortDateString();
            string endTime = new DateTime(DateTime.Now.Year, month, 1).AddMonths(1).ToShortDateString();
            strWhere += string.Format("And  KQDate BETWEEN '{0}' and '{1}'", strTime, endTime);
            if (P1 != "")
            {
                int type = 0;
                int.TryParse(P1, out type);
                strWhere += string.Format("And  Type={0} and Status={1}", P1, type + 1);

            }
            string strSql = string.Format("SELECT * from SZHL_KQJL where KQUser='{0}' {1}", UserInfo.User.UserName, strWhere);
            msg.Result = new SZHL_KQJLB().GetDTByCommand(strSql);
        }
        public void GETKQJLLIST(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            int page = 0;
            int pagecount = 8;
            int.TryParse(context.Request["p"] ?? "1", out page);//页码
            int.TryParse(context.Request["pagecount"] ?? "8", out pagecount);//页数
            page = page == 0 ? 1 : page;
            int recordCount = 0;
            string strWhere = string.Format(" ComId={0}", UserInfo.User.ComId);
            if (P2 != "")//内容查询
            {
                strWhere += string.Format(" And (KQUserName like '%{0}%' OR KQBranch like '%{0}%' )", P2);
            }
            //根据创建时间查询
            string time = context.Request["time"] ?? "";
            if (time != "")
            {
                if (time == "1")   //近一周
                {
                    strWhere += string.Format(" And datediff(day,KQDate,getdate())<7");
                }
                else if (time == "2")
                {  //近一月
                    strWhere += string.Format(" And datediff(day,KQDate,getdate())<30");
                }
                else if (time == "3")  //自定义时间
                {
                    string strTime = context.Request["starTime"] ?? "";
                    string endTime = context.Request["endTime"] ?? "";
                    if (strTime != "")
                    {
                        strWhere += string.Format(" And KQDate >='{0}'", strTime + "-01");
                    }
                    if (endTime != "")
                    {
                        DateTime endDate = DateTime.Parse(endTime + "-01");
                        strWhere += string.Format(" And KQDate <='{0}'", endDate.AddMonths(1));
                    }
                }
            }


            if (P1 != "")
            {
                switch (P1)
                {
                    case "1": //创建的
                        {
                            strWhere += " And KQUser ='" + UserInfo.User.UserName + "'";
                        }
                        break;
                    case "2": //下属签到
                        {
                            //获取当前登录人负责的下属人员 
                            string Users = new JH_Auth_UserB().GetUserBranchUsers(UserInfo.User.ComId.Value, UserInfo.User.UserName).Select(d => d.UserName).ToList().ListTOString(',');
                            strWhere += string.Format(" and KQUser in ('{0}') ", Users.ToFormatLike());
                        }
                        break;
                }
            }

            string strsql = string.Format("SELECT   isnull(sum(case when Status=1 then 1 else 0 end),0) CDCount, isnull(sum(case when Status=2 then 1 else 0 end),0) ZTCount FROM SZHL_KQJL where    DATEPART(MONTH,KQDate)= DATEPART(MONTH,getdate()) and ComId={0} and KQUser='{1}'", UserInfo.User.ComId, UserInfo.User.UserName);
            DataTable dtTJ = new SZHL_KQJLB().GetDTByCommand(strsql);
            dtTJ.Columns.Add("QJCount");
            var intProD = new Yan_WF_PIB().GetYSHUserPI(UserInfo.User.UserName, UserInfo.User.ComId.Value, "CCXJ").Select(d => d.ID.ToString()).ToList();
            string strSql1 = string.Format("SELECT  isnull(SUM(Daycount),0) from SZHL_CCXJ where intProcessStanceid in ({0})  and  DATEPART(MONTH,CRDate)= DATEPART(MONTH,getdate()) and  CRUser='{1}'", intProD.ListTOString(',') == "" ? "0" : intProD.ListTOString(','), UserInfo.User.UserName);
            object obj = new SZHL_KQJLB().ExsSclarSql(strSql1);
            dtTJ.Rows[0]["QJCount"] = obj.ToString();
            DataTable dt = new SZHL_KQBCB().GetDataPager(" SZHL_KQJL", "* ", pagecount, page, "KQDate desc", strWhere + "  and Type=0", ref recordCount);
            dt.Columns.Add("QTDate");
            dt.Columns.Add("QTStatus");
            dt.Columns.Add("QTPosition");
            foreach (DataRow row in dt.Rows)
            {
                string strSql2 = string.Format("SELECT top 1 * from SZHL_KQJL where ComId={0} and KQUser='{1}' AND DATEDIFF(DAY,KQDate,'{2}')=0 and Type=1  ORDER by KQDate DESC", UserInfo.User.ComId, row["KQUser"], row["KQDate"]);

                DataTable dtQT = new SZHL_KQBCB().GetDTByCommand(strSql2);
                if (dtQT.Rows.Count > 0)
                {
                    row["QTDate"] = dtQT.Rows[0]["KQDate"];
                    row["QTStatus"] = dtQT.Rows[0]["Status"];
                    row["QTPosition"] = dtQT.Rows[0]["Position"];
                }
            }
            msg.Result = dt;
            msg.Result1 = recordCount;
            msg.Result2 = dtTJ;

        }
    }
}