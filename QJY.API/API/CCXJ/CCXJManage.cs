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
    public class CCXJManage : IWsService
    {
        public void ProcessRequest(HttpContext context, ref Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            MethodInfo methodInfo = typeof(CCXJManage).GetMethod(msg.Action.ToUpper());
            CCXJManage model = new CCXJManage();
            methodInfo.FastInvoke(model, new object[] { context, msg, P1, P2, UserInfo });
        }
        #region 请假列表
        /// <summary>
        /// 请假列表
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void GETCCXJLIST(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            string userName = UserInfo.User.UserName;
            string strWhere = " 1=1 and cc.ComId=" + UserInfo.User.ComId;

            string leibie = context.Request["lb"] ?? "";
            if (leibie != "")
            {
                strWhere += string.Format(" And cc.LeiBie='{0}' ", leibie);
            }
            string strContent = context.Request["Content"] ?? "";
            strContent = strContent.TrimEnd();
            if (strContent != "")
            {
                strWhere += string.Format(" And ( cc.ZhuYaoShiYou like '%{0}%' )", strContent);
            }
            int DataID = -1;
            int.TryParse(context.Request["ID"] ?? "-1", out DataID);//记录Id
            if (DataID != -1)
            {
                string strIsHasDataQX = new JH_Auth_QY_ModelB().ISHASDATAREADQX("CCXJ", DataID, UserInfo);
                if (strIsHasDataQX == "Y")
                {
                    strWhere += string.Format(" And cc.ID = '{0}'", DataID);
                }

            }

            if (P1 != "")
            {
                int page = 0;
                int pagecount = 8;
                int.TryParse(context.Request["p"] ?? "1", out page);
                int.TryParse(context.Request["pagecount"] ?? "8", out pagecount);//页数
                page = page == 0 ? 1 : page;
                int total = 0;
                DataTable dt = new DataTable();
                switch (P1)
                {
                    case "0": //手机单条数据
                        {
                            //设置usercenter已读
                            new JH_Auth_User_CenterB().ReadMsg(UserInfo, DataID, "CCXJ");
                        }
                        break;
                    case "1": //创建的
                        {
                            strWhere += " And cc.CRUser ='" + userName + "'";
                        }
                        break;
                    case "2": //待审核
                        {
                            var intProD = new Yan_WF_PIB().GetDSH(UserInfo.User).Select(d => d.PIID.ToString()).ToList();
                            if (intProD.Count > 0)
                            {
                                strWhere += " And cc.intProcessStanceid in (" + (intProD.ListTOString(',') == "" ? "0" : intProD.ListTOString(',')) + ")";
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
                                strWhere += " And cc.intProcessStanceid in (" + (intProD.ListTOString(',') == "" ? "0" : intProD.ListTOString(',')) + ")";

                            }
                            else
                            {
                                strWhere += " And 1=0";
                            }
                        }
                        break;
                }
                dt = new SZHL_CCXJB().GetDataPager("SZHL_CCXJ cc left join JH_Auth_ZiDian zd on cc.LeiBie=zd.ID", "cc.*,zd.TypeName ,dbo.fn_PDStatus(cc.intProcessStanceid) AS StateName", pagecount, page, " cc.CRDate desc", strWhere, ref total);

                if (dt.Rows.Count > 0)
                {
                    dt.Columns.Add("FileList", Type.GetType("System.Object"));
                    foreach (DataRow dr in dt.Rows)
                    {
                        if (dr["Files"] != null && dr["Files"].ToString() != "")
                        {
                            dr["FileList"] = new FT_FileB().GetEntities(" ID in (" + dr["Files"].ToString() + ")");
                        }
                    }
                }
                msg.Result = dt;
                msg.Result1 = total;
            }
        }
        /// <summary>
        /// 请假列表
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void GETCCXJUSERLIST(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            string userName = UserInfo.User.UserName;
            string strWhere = " 1=1 and cc.ComId=" + UserInfo.User.ComId;
            string strUserName = context.Request["username"] ?? "";//经费报销统计中的查看列表需要的参数
            if (!string.IsNullOrEmpty(strUserName))
            {
                userName = strUserName;
            }
            strWhere += " And cc.CRUser ='" + userName + "'";
            if (P1 != "")
            {
                strWhere += string.Format(" And cc.LeiBie='{0}' ", P1);
            }
            string strContent = context.Request["Content"] ?? "";
            if (strContent != "")
            {
                strWhere += string.Format(" And ( cc.ZhuYaoShiYou like '%{0}%' )", strContent);
            }
            int month = 0;
            int.TryParse(context.Request["month"] ?? "1", out month);
            if (month==0)
            {
                month = DateTime.Now.Month;
            }
            string strTime = new DateTime(DateTime.Now.Year, month, 1).ToShortDateString();
            string endTime = new DateTime(DateTime.Now.Year, month, 1).AddMonths(1).ToShortDateString();
            strWhere += string.Format("And cc.CRDate BETWEEN '{0}' and '{1}'", strTime, endTime);
            
            //已审核的经费报销
            //var intProD = new Yan_WF_PIB().GetYSHUserPI(userName, UserInfo.User.ComId.Value, "CCXJ").Select(d => d.ID.ToString()).ToList();
            //if (intProD.Count > 0)
            //{
            //    strWhere += " And intProcessStanceid in (" + (intProD.ListTOString(',') == "" ? "0" : intProD.ListTOString(',')) + ")";

            //}
            //else
            //{
            //    strWhere += " And 1=0";
            //}
            string strSql = string.Format("select cc.*,zd.TypeName from  SZHL_CCXJ cc left join JH_Auth_ZiDian zd on cc.LeiBie=zd.ID where {0} and (dbo.fn_PDStatus(cc.intProcessStanceid)='-1' or dbo.fn_PDStatus(cc.intProcessStanceid)='已审批')  order by  cc.CRDate desc", strWhere);
            DataTable dt = new SZHL_CCXJB().GetDTByCommand(strSql);
            msg.Result = dt;
        }
        #endregion

        #region 部门数据
        /// <summary>
        /// 部门数据
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void GETCCXJLIST_PAGE(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            string strWhere = " cc.ComId=" + UserInfo.User.ComId;
            if (P1 != "")
            {
                strWhere += string.Format(" And cc.LeiBie={0}", P1);
            }
            if (P2 != "")
            {
                strWhere += string.Format(" And  cc.ZhuYaoShiYou like '%{0}%'", P2);
            }
            int page = 0;
            int.TryParse(context.Request["p"] ?? "1", out page);
            page = page == 0 ? 1 : page;
            int total = 0;
            string colNme = @"cc.ShenQingRen,cc.LeiBie,cc.StarTime,cc.EndTime,cc.Daycount,cc.ZhuYaoShiYou,cc.ID,cc.BranchName,zd.TypeName  , 
                                            case WHEN wfpi.isComplete is null and wfpi.IsCanceled is null  THEN '正在审批' 
                                            when wfpi.isComplete='Y' then '已审批'  WHEN wfpi.IsCanceled='Y' then '已退回' END StateName,intProcessStanceid";
            DataTable dt = new SZHL_CCXJB().GetDataPager("SZHL_CCXJ cc  inner join JH_Auth_ZiDian zd on cc.LeiBie=zd.ID inner join  Yan_WF_PI wfpi  on cc.intProcessStanceid=wfpi.ID", colNme, 8, page, " cc.CRDate desc", strWhere, ref total);
            msg.Result = dt;
            msg.Result1 = total;
        }
        #endregion

        #region 获取出差请假信息
        /// <summary>
        /// 获取出差请假信息
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void GETCCXJMODEL(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            int Id = int.Parse(P1);
            SZHL_CCXJ ccxj = new SZHL_CCXJB().GetEntity(d => d.ID == Id);
            msg.Result = ccxj;
            if (ccxj != null)
            {
                int id = Int32.Parse(ccxj.LeiBie);
                var zd = new JH_Auth_ZiDianB().GetEntity(p => p.ID == id);
                msg.Result1 = zd.TypeName;
                if (!string.IsNullOrEmpty(ccxj.Files))
                {
                    msg.Result2 = new FT_FileB().GetEntities(" ID in (" + ccxj.Files + ")");
                }

                if (ccxj.intProcessStanceid != 0)
                {
                    msg.Result3 = new JH_Auth_User_CenterB().GetDTByCommand("select dbo.fn_PDStatus(" + ccxj.intProcessStanceid + ") AS StateName");
                }

                new JH_Auth_User_CenterB().ReadMsg(UserInfo, ccxj.ID, "CCXJ");
            }

        }
        #endregion

        #region 添加出差请假
        /// <summary>
        /// 添加出差请假
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1">客户信息</param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void ADDCCXJ(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            SZHL_CCXJ ccxj = JsonConvert.DeserializeObject<SZHL_CCXJ>(P1);

            if (ccxj.Daycount == 0)
            {
                msg.ErrorMsg = "申请天数不能为空";
                return;
            }
            if (ccxj.EndTime < ccxj.StarTime)
            {
                msg.ErrorMsg = "开始时间不能大于结束时间";
                return;
            }
            if (P2 != "") // 处理微信上传的图片
            {
                string fids = new FT_FileB().ProcessWxIMG(P2, "CCXJ", UserInfo);
                if (!string.IsNullOrEmpty(ccxj.Files))
                {
                    ccxj.Files += "," + fids;
                }
                else
                {
                    ccxj.Files = fids;
                }
            }
            if (ccxj.ID == 0)
            {
                ccxj.ShenQingRen = string.IsNullOrEmpty(ccxj.ShenQingRen) ? UserInfo.User.UserRealName : ccxj.ShenQingRen;
                ccxj.CRDate = DateTime.Now;
                ccxj.CRUser = UserInfo.User.UserName;
                ccxj.BranchName = UserInfo.BranchInfo.DeptName;
                ccxj.BranchNo = UserInfo.User.BranchCode;
                ccxj.ComId = UserInfo.User.ComId;
                new SZHL_CCXJB().Insert(ccxj);

            }
            else
            {
                new SZHL_CCXJB().Update(ccxj);
            }
            msg.Result = ccxj;
        }
        #endregion

        #region 请假天数统计
        /// <summary>
        /// 请假天数统计
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void GETCCXJTJ(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            int page = 0;
            int pagecount = 8;
            int.TryParse(context.Request["p"] ?? "1", out page);
            int.TryParse(context.Request["pagecount"] ?? "8", out pagecount);//页数
            page = page == 0 ? 1 : page;
            int total = 0;
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
                strWhere = string.Format(" And cc.ShenQingRen like '%{0}%'", searchContent);
            }
            string strSql = string.Format(@"SELECT  cc.ShenQingRen,cc.BranchName,sum(cc.Daycount) daycount,zd.TypeName,DATEPART(MONTH,cc.CRDate) ccMonth,cc.CRUser,cc.LeiBie 
                                            from SZHL_CCXJ cc inner join JH_Auth_ZiDian zd on cc.LeiBie=zd.ID  where (dbo.fn_PDStatus(cc.intProcessStanceid)='-1' or dbo.fn_PDStatus(cc.intProcessStanceid)='已审批') And  cc.ComId={0} and cc.CRDate BETWEEN  '{1}' and '{2}' " + strWhere + @"
                                            GROUP by cc.ShenQingRen,zd.TypeName,cc.LeiBie,cc.BranchName,cc.CRUser,DATEPART(MONTH,cc.CRDate)", UserInfo.User.ComId, starDate + " 00:00:00", EndDate + " 23:59:59");
            DataTable dt = new SZHL_CCXJB().GetDataPager("(" + strSql + ") as newcc", "*", pagecount, page, "BranchName,ShenQingRen,ccMonth,TypeName,CRUser", "1=1", ref total);
            msg.Result = dt;
            msg.Result1 = total;
        }
        #endregion

        /// <summary>
        /// 取消出差请假
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void DELCCXJ(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            int Id = int.Parse(P1);
            new SZHL_CCXJB().Delete(d => d.ID == Id && d.ComId == UserInfo.User.ComId);

        }
    }
}