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
using QJY.Common;

namespace QJY.API
{
    public class JFBXManage : IWsService
    {
        public void ProcessRequest(HttpContext context, ref Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            MethodInfo methodInfo = typeof(JFBXManage).GetMethod(msg.Action.ToUpper());
            JFBXManage model = new JFBXManage();
            methodInfo.FastInvoke(model, new object[] { context, msg, P1, P2, UserInfo });
        }
        public void GETJFBXLIST(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            string userName = UserInfo.User.UserName;
            string strUserName = context.Request["username"] ?? "";//经费报销统计中的查看列表需要的参数
            if (!string.IsNullOrEmpty(strUserName))
            {
                userName = strUserName;
            }
            string strWhere = " ComId =" + UserInfo.User.ComId.Value;

            DataTable dtList = new DataTable();
            int page = 0;
            int pagecount = 8;
            int.TryParse(context.Request["p"] ?? "1", out page);//页码
            int.TryParse(context.Request["pagecount"] ?? "8", out pagecount);//页数
            int DataID = -1;
            int.TryParse(context.Request["ID"] ?? "-1", out DataID);//记录Id
            if (DataID != -1)
            {
                string strIsHasDataQX = new JH_Auth_QY_ModelB().ISHASDATAREADQX("JFBX", DataID, UserInfo);
                if (strIsHasDataQX == "Y")
                {
                    strWhere += string.Format(" And ID = '{0}'", DataID);
                }

            }
            switch (P1)
            {
                case "0": //手机单条数据
                    {
                        //设置usercenter已读
                        new JH_Auth_User_CenterB().ReadMsg(UserInfo, DataID, "JFBX");
                    }
                    break;
                case "1": //创建的
                    {
                        strWhere += " And CRUser ='" + userName + "'";
                    }
                    break;
                case "2": //待审核
                    {
                        var intProD = new Yan_WF_PIB().GetDSH(UserInfo.User).Select(d => d.PIID.ToString()).ToList();
                        if (intProD.Count > 0)
                        {
                            strWhere += " And intProcessStanceid in (" + (intProD.ListTOString(',') == "" ? "0" : intProD.ListTOString(',')) + ")";
                        }
                        else
                        {
                            strWhere += " And 1=0";
                        }
                    }
                    break;
                case "3":  //已审核
                    {
                        var intProD = new Yan_WF_PIB().GetYSH(UserInfo.User).Select(d => d.PIID.ToString()).ToList();
                        if (intProD.Count > 0)
                        {
                            strWhere += " And intProcessStanceid in (" + (intProD.ListTOString(',') == "" ? "0" : intProD.ListTOString(',')) + ")";

                        }
                        else
                        {
                            strWhere += " And 1=0";
                        }
                    }
                    break;
                case "4":

                    //获取当前登录人负责的下属人员 
                    string Users = new JH_Auth_UserB().GetUserBranchUsers(UserInfo.User.ComId.Value, UserInfo.User.UserName).Select(d => d.UserName).ToList().ListTOString(',');
                    strWhere += string.Format("and   CRUser in ('{0}')", Users.ToFormatLike());
                    break;
            }
            //根据创建时间查询
            string time = context.Request["time"] ?? "";
            if (time != "")
            {
                if (time == "1")   //近一周
                {
                    strWhere += string.Format(" And datediff(day,BXDate,getdate())<7");
                }
                else if (time == "2")
                {  //近一月
                    strWhere += string.Format(" And datediff(day,BXDate,getdate())<30");
                }
                else if (time == "3")  //自定义时间
                {
                    string strTime = context.Request["starTime"] ?? "";
                    string endTime = context.Request["endTime"] ?? "";
                    if (strTime != "")
                    {
                        strWhere += string.Format(" And BXDate >='{0}'", strTime);
                    }
                    if (endTime != "")
                    {
                        strWhere += string.Format(" And BXDate <='{0}'", endTime);
                    }
                }
            }
            string strContent = context.Request["Content"] ?? "";
            strContent = strContent.TrimEnd();
            if (strContent != "")
            {
                strWhere += string.Format(" And (BranchName like '%{0}%' or ShenQingRen like '%{0}%' or FormCode like '%{0}%' )", strContent);
            }
            page = page == 0 ? 1 : page;
            int recordCount = 0;
            dtList = new SZHL_JFBXB().GetDataPager(" SZHL_JFBX ", "*,dbo.fn_PDStatus(intProcessStanceid) AS StateName ", pagecount, page, "CRDate desc", strWhere, ref recordCount);

            msg.Result = dtList;
            msg.Result1 = recordCount;
        }
        public void GEUSERJFBXLIST(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            string userName = UserInfo.User.UserName;
            string strUserName = context.Request["username"] ?? "";//经费报销统计中的查看列表需要的参数
            if (!string.IsNullOrEmpty(strUserName))
            {
                userName = strUserName;
            }
            string strWhere = " And jfbx.ComId =" + UserInfo.User.ComId.Value;

            DataTable dtList = new DataTable();


            strWhere += " And jfbx.CRUser ='" + userName + "'";
            //已审核的经费报销
            //var intProD = new Yan_WF_PIB().GetYSHUserPI(userName, UserInfo.User.ComId.Value, "JFBX").Select(d => d.ID.ToString()).ToList();
            //if (intProD.Count > 0)
            //{
            //    strWhere += " And intProcessStanceid in (" + (intProD.ListTOString(',') == "" ? "0" : intProD.ListTOString(',')) + ")";

            //}
            //else
            //{
            //    strWhere += " And 1=0";
            //}
            int month = 0;
            int.TryParse(context.Request["month"] ?? "1", out month);
            string strTime = new DateTime(DateTime.Now.Year, month, 1).ToShortDateString();
            string endTime = new DateTime(DateTime.Now.Year, month, 1).AddMonths(1).ToShortDateString();
            strWhere += string.Format(" And BXDate BETWEEN '{0}' and '{1}'", strTime, endTime);

            string strContent = context.Request["Content"] ?? "";
            if (strContent != "")
            {
                strWhere += string.Format(" And (BranchName like '%{0}%' or ShenQingRen like '%{0}%' or FormCode like '%{0}%' )", strContent);
            }
            string strSql = string.Format(@" SELECT  jfbx.*,dbo.fn_PDStatus(intProcessStanceid) AS StateName  FROM SZHL_JFBX jfbx
                                        Left join Yan_WF_PI wfpi on intProcessStanceid=wfpi.ID WHERE   ((wfpi.Id is not null and wfpi.isComplete='Y') or wfpi.Id is null) {0}   order by CRDate desc", strWhere);
            dtList = new SZHL_JFBXB().GetDTByCommand(strSql);
            msg.Result = dtList;
            msg.Result1 = dtList.Compute("sum(BXZJE)", "");
        }
        public void GETJFBXMODEL(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            int id = 0;
            int.TryParse(P1, out id);
            SZHL_JFBX jfbx = new SZHL_JFBXB().GetEntity(d => d.ID == id);
            if (jfbx != null)
            {
                msg.Result = jfbx;
                msg.Result1 = new SZHL_JFBXITEMB().GetDTByCommand("SELECT item.*,zd.TypeName from SZHL_JFBXITEM item inner join JH_Auth_ZiDian  zd on zd.ID=item.LeiBie and zd.Class=23 where JFBXID=" + id);
                if (!string.IsNullOrEmpty(jfbx.Files))
                {
                    int[] fileIds = jfbx.Files.SplitTOInt(',');
                    msg.Result2 = new FT_FileB().GetEntities(d => fileIds.Contains(d.ID));

                }
                if (jfbx.XMID != null)
                {
                    msg.Result3 = new SZHL_XMGLB().GetEntity(d => d.ID == jfbx.XMID);
                }
            }


        }
        public void ADDJFBX(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            SZHL_JFBX jfbxModel = JsonConvert.DeserializeObject<SZHL_JFBX>(P1);
            List<SZHL_JFBXITEM> itemList = JsonConvert.DeserializeObject<List<SZHL_JFBXITEM>>(P2);
            if (itemList == null || itemList.Count() == 0)
            {
                msg.Result = "请添加消费记录";
                return;
            }

            string wximg = context.Request["wximg"] ?? "";
            if (wximg != "") // 处理微信上传的图片
            {

                string fids = new FT_FileB().ProcessWxIMG(wximg, "JFBX", UserInfo);

                if (!string.IsNullOrEmpty(jfbxModel.Files))
                {
                    jfbxModel.Files += "," + fids;
                }
                else
                {
                    jfbxModel.Files = fids;
                }
            }
            if (jfbxModel.ID == 0)
            {
                jfbxModel.CRDate = DateTime.Now;
                jfbxModel.CRUser = UserInfo.User.UserName;
                jfbxModel.ComId = UserInfo.User.ComId;
                jfbxModel.FormCode = new SZHL_JFBXB().GetFormCode();
                jfbxModel.BranchName = UserInfo.BranchInfo.DeptName;
                jfbxModel.BranchNo = UserInfo.BranchInfo.DeptCode;
                new SZHL_JFBXB().Insert(jfbxModel);
                foreach (SZHL_JFBXITEM item in itemList)
                {
                    item.ComID = UserInfo.User.ComId;
                    item.JFBXID = jfbxModel.ID;
                    new SZHL_JFBXITEMB().Insert(item);
                }
            }
            else
            {
                new SZHL_JFBXB().Update(jfbxModel);
                new SZHL_JFBXITEMB().Delete(d => d.JFBXID == jfbxModel.ID && d.ComID == UserInfo.User.ComId);
                foreach (SZHL_JFBXITEM item in itemList)
                {
                    item.ComID = UserInfo.User.ComId;
                    item.JFBXID = jfbxModel.ID;
                    new SZHL_JFBXITEMB().Insert(item);
                }
            }
            msg.Result = jfbxModel;
        }

        public void GETJFBXTJ(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            int page = 0;
            int pagecount = 8;
            int.TryParse(context.Request["p"] ?? "1", out page);
            int.TryParse(context.Request["pagecount"] ?? "8", out pagecount);//页数
            page = page == 0 ? 1 : page;
            int total = 0;


            int year = 0;
            int.TryParse(P1, out year);
            int month = 0;
            int.TryParse(P2, out month);
            string strWhere = "";
            if (month > 0)
            {
                DateTime newData = new DateTime(year, month, 1);
                strWhere = string.Format("  and DATEDIFF(MONTH, jfbx.BXDate,'{0}')=0 ", newData.ToString("yyyy-MM-dd HH:mm:ss"));
            }
            string content = context.Request["search"] ?? "";
            content = content.TrimEnd();
            if (content != "")
            {
                strWhere += string.Format("and (jfbx.ShenQingRen like '%{0}%' or jfbx.BranchName like '%{0}%')", content);
            }
            string strSql = string.Format(@"SELECT  jfbx.ShenQingRen,jfbx.CRUser,jfbx.BranchName,DATEPART(YEAR, jfbx.BXDate) BXYear,DATEPART(month, jfbx.BXDate) BXMonth,SUM(BXZJE) totalMoney FROM SZHL_JFBX jfbx
                                            Left join Yan_WF_PI wfpi on intProcessStanceid=wfpi.ID WHERE jfbx.ComId={0} and  ((wfpi.Id is not null and wfpi.isComplete='Y') or wfpi.Id is null)    {1} group by jfbx.ShenQingRen,jfbx.CRUser,jfbx.BranchName,DATEPART(YEAR, jfbx.BXDate),DATEPART(month, jfbx.BXDate)", UserInfo.User.ComId, strWhere);
            DataTable dtJFBX = new SZHL_JFBXB().GetDataPager("(" + strSql + ") as newjf", "*", pagecount, page, "BranchName,ShenQingRen", "1=1", ref total);
            msg.Result = dtJFBX;
            msg.Result1 = total;
        }

    }
}