using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FastReflectionLib;
using System.Web;
using QJY.Data;
using Newtonsoft.Json;
using System.Data;
using Senparc.Weixin.QY.Entities;
using QJY.Common;

namespace QJY.API
{
    public class LCSPManage : BaseManage, IWsService
    {

        public void ProcessRequest(HttpContext context, ref Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            MethodInfo methodInfo = typeof(LCSPManage).GetMethod(msg.Action.ToUpper());
            LCSPManage model = new LCSPManage();
            methodInfo.FastInvoke(model, new object[] { context, msg, P1, P2, UserInfo });
        }

        #region 流程审批列表
        /// <summary>
        /// 流程审批列表
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void GETLCSPLIST(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            string userName = UserInfo.User.UserName;
            string strWhere = " 1=1 and lc.ComId=" + UserInfo.User.ComId;

            string leibie = context.Request["lb"] ?? "";
            if (leibie != "")
            {
                strWhere += string.Format(" And lc. LeiBie='{0}' ", leibie);
            }

            string leibie1 = context.Request["lb1"] ?? "";
            if (leibie1 != "")
            {
                strWhere += string.Format(" And pd.RelatedTable='{0}' ", leibie1);
            }
            string strContent = context.Request["Content"] ?? "";
            strContent = strContent.Trim();//去除空格
            if (strContent != "")
            {
                //strWhere += string.Format(" And ( lc.Content like '%{0}%' )", strContent);
                strWhere += string.Format(" And ( pd.ProcessName like '%{0}%' or EXISTS( select * from JH_Auth_ExtendMode T1 join JH_Auth_ExtendData T2 on T1.ID = T2.ExtendModeID where T1.TableName = 'LCSP' and T1.PDID = pd.ID and T2.DataID = lc.ID and T2.ExtendDataValue like '%{1}%' ))", strContent, strContent);

                //strWhere += " or ()";
            }
            int DataID = -1;
            int.TryParse(context.Request["ID"] ?? "-1", out DataID);//记录Id
            if (DataID != -1)
            {
                string strIsHasDataQX = new JH_Auth_QY_ModelB().ISHASDATAREADQX("LCSP", DataID, UserInfo);
                if (strIsHasDataQX == "Y")
                {
                    strWhere += string.Format(" And lc.ID = '{0}'", DataID);
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
                    case "0": //单条信息
                        {
                            //设置usercenter已读
                            new JH_Auth_User_CenterB().ReadMsg(UserInfo, DataID, "LCSP");
                        }

                        break;
                    case "1": //创建
                        {
                            strWhere += " And lc.CRUser ='" + userName + "'";
                        }

                        break;
                    case "2": //待审核
                        {
                            List<string> intProD = new Yan_WF_PIB().GetDSH(UserInfo.User).Select(d => d.PIID.ToString()).ToList();
                            if (intProD.Count > 0)
                            {
                                strWhere += " And lc.intProcessStanceid in (" + (intProD.ListTOString(',') == "" ? "0" : intProD.ListTOString(',')) + ")";
                            }
                            else
                            {
                                strWhere += " and 1=0 ";
                            }
                        }
                        break;
                    case "3": //已审核
                        {
                            var intProD = new Yan_WF_PIB().GetYSH(UserInfo.User).Select(d => d.PIID.ToString()).ToList();
                            if (intProD.Count > 0)
                            {
                                strWhere += " And lc.intProcessStanceid in (" + (intProD.ListTOString(',') == "" ? "0" : intProD.ListTOString(',')) + ")";
                            }
                            else
                            {
                                strWhere += " and 1=0 ";
                            }
                        }
                        break;

                    case "4": //抄送我的
                        {
                            strWhere += " AND dbo.fn_PDStatus(lc.intProcessStanceid) ='已审批'  And   exists (select I.ID from Yan_WF_PI I where I.PDID=pd.ID  AND  ',' + I.ChaoSongUser  + ',' like '%," + userName + ",%' )";

                        }
                        break;
                }
                dt = new SZHL_LCSPB().GetDataPager("SZHL_LCSP lc inner join Yan_WF_PD pd on pd.ID=lc.LeiBie ", "lc.*,dbo.fn_PDStatus(lc.intProcessStanceid) AS StateName,pd.ID as PDID,pd.RelatedTable, pd.ProcessType,pd.ProcessName,'LCSP' as ModelCode", pagecount, page, " lc.CRDate desc", strWhere, ref total);
                if (dt.Rows.Count > 0)
                {
                    dt.Columns.Add("FileList", Type.GetType("System.Object"));
                    dt.Columns.Add("SubExt", typeof(DataTable));
                    foreach (DataRow dr in dt.Rows)
                    {
                        if (dr["Files"] != null && dr["Files"].ToString() != "")
                        {
                            dr["FileList"] = new FT_FileB().GetEntities(" ID in (" + dr["Files"].ToString() + ")");
                        }

                        DataTable dtExtData = new JH_Auth_ExtendModeB().GetExtData(UserInfo.User.ComId, "LCSP", dr["ID"].ToString());
                        dr["SubExt"] = dtExtData;
                    }
                }
                msg.Result = dt;
                msg.Result1 = total;
            }
        }
        #endregion

        #region 流程审批添加
        /// <summary>
        /// 流程审批添加
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void ADDLCSP(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            SZHL_LCSP lcsp = JsonConvert.DeserializeObject<SZHL_LCSP>(P1);
            if (P2 != "") // 处理微信上传的图片
            {
                string fids = new FT_FileB().ProcessWxIMG(P2, "LCSP", UserInfo);

                if (!string.IsNullOrEmpty(lcsp.Files))
                {
                    lcsp.Files += "," + fids;
                }
                else
                {
                    lcsp.Files = fids;
                }
            }

            if (lcsp.ID == 0)
            {
                lcsp.CRDate = DateTime.Now;
                lcsp.CRUser = UserInfo.User.UserName;
                lcsp.BranchName = UserInfo.BranchInfo.DeptName;
                lcsp.ShenQingRen = string.IsNullOrEmpty(lcsp.ShenQingRen) ? UserInfo.User.UserRealName : lcsp.ShenQingRen;
                lcsp.BranchNo = UserInfo.User.BranchCode;
                lcsp.ComId = UserInfo.User.ComId;
                new SZHL_LCSPB().Insert(lcsp);
                //SendStartMsg(UserInfo, lcsp.intProcessStanceid);
            }
            else
            {
                new SZHL_LCSPB().Update(lcsp);
            }
            msg.Result = lcsp;
        }
        #endregion

        #region 获取流程数据信息
        /// <summary>
        /// 获取流程数据信息
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void GETLCSPMODEL(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            if (!string.IsNullOrEmpty(P1))
            {
                int ID = int.Parse(P1);
                SZHL_LCSP lcsp = new SZHL_LCSPB().GetEntity(d => d.ID == ID);
                msg.Result = lcsp;//返回表单数据
                //msg.Result1 = new Yan_WF_PIB().GETPDMODELBYID(lcsp.intProcessStanceid);//返回初始化数据 

                if (lcsp != null)
                {
                    if (!string.IsNullOrEmpty(lcsp.Files))
                    {
                        msg.Result2 = new FT_FileB().GetEntities(" ID in (" + lcsp.Files + ")");
                    }
                    if (lcsp.LeiBie != null)
                    {
                        var pd = new Yan_WF_PDB().GetEntity(p => p.ID == lcsp.LeiBie);
                        if (pd != null)
                        {
                            msg.Result1 = pd;
                        }
                    }
                    if (lcsp.intProcessStanceid != 0)
                    {
                        msg.Result3 = new JH_Auth_User_CenterB().GetDTByCommand("select dbo.fn_PDStatus(" + lcsp.intProcessStanceid + ") AS StateName");
                    }

                    new JH_Auth_User_CenterB().ReadMsg(UserInfo, lcsp.ID, "LCSP");
                }
            }
            else
            {
                int ID = int.Parse(P2);
                msg.Result1 = new Yan_WF_PDB().GetEntity(d => d.ID == ID);//返回初始化数据 
            }
        }
        #endregion

        #region 删除流程审批
        /// <summary>
        /// 删除流程审批
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void DELLCSPBYID(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            try
            {
                int ID = int.Parse(P1);

                if (new SZHL_LCSPB().Delete(d => d.ID == ID && d.ComId == UserInfo.User.ComId))
                {
                    msg.ErrorMsg = "";
                }
            }
            catch (Exception ex)
            {
                msg.ErrorMsg = ex.Message;
            }
        }
        #endregion

        #region 流程审批统计列表
        /// <summary>
        /// 流程审批统计列表
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void GETLCSPTJ(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            string userName = UserInfo.User.UserName;
            string strWhere = " and lc.ComId=" + UserInfo.User.ComId;

            if (P1 != "")
            {
                strWhere += string.Format(" And lc. LeiBie='{0}' ", P1);
            }
            //string strContent = context.Request["Content"] ?? "";
            //if (strContent != "")
            //{
            //    strWhere += string.Format(" And ( lc.Content like '%{0}%' )", strContent);
            //}

            string year = context.Request["year"] ?? "";
            string month = context.Request["month"] ?? "";

            string starDate = (year == "" ? DateTime.Now.Year.ToString() : year) + "-";
            string EndDate = "";
            if (month == "0" || month == "")
            {
                starDate = starDate + "01" + "-01";
                EndDate = (year == "" ? DateTime.Now.Year.ToString() : year) + "-12" + "-31";
            }
            else
            {
                starDate = starDate + month + "-01";
                EndDate = DateTime.Parse(starDate).AddMonths(1).AddDays(-1).ToString("yyyy-MM-dd");
            }

            strWhere += string.Format(" And lc.CRDate>='{0}' and  lc.CRDate<='{1}' ", starDate, EndDate);

            DataTable dt = new SZHL_CCXJB().GetDTByCommand("select lc.CRUser,DATEPART(YEAR,lc.CRDate) lcYear,DATEPART(MONTH,lc.CRDate) lcMonth ,pd.ProcessName,lc.ComId, COUNT(lc.ID) lcCount from SZHL_LCSP lc inner join Yan_WF_PD pd on pd.ID=lc.LeiBie where (dbo.fn_PDStatus(lc.intProcessStanceid)='已审批' or dbo.fn_PDStatus(lc.intProcessStanceid)='-1') " + strWhere + " GROUP by  lc.CRUser, DATEPART(YEAR,lc.CRDate),DATEPART(MONTH,lc.CRDate),pd.ProcessName,lc.ComId");

            msg.Result = dt;

        }

        /// <summary>
        /// 流程审批统计列表
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void GETLCSPTJLIST(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            string userName = UserInfo.User.UserName;
            string strWhere = " 1=1 and (dbo.fn_PDStatus(lc.intProcessStanceid)='已审批' or dbo.fn_PDStatus(lc.intProcessStanceid)='-1')  and lc.ComId=" + UserInfo.User.ComId;

            if (P1 != "")
            {
                strWhere += string.Format(" And lc. LeiBie='{0}' ", P1);
            }
            else
            {
                strWhere += string.Format(" And lc. LeiBie='{0}' ", 0);
            }
            //string strContent = context.Request["Content"] ?? "";
            //if (strContent != "")
            //{
            //    strWhere += string.Format(" And ( lc.Content like '%{0}%' )", strContent);
            //}
            string user = context.Request["yhm"] ?? "";
            string year = context.Request["year"] ?? "";
            string month = context.Request["month"] ?? "";

            if (user != "")
            {
                strWhere += string.Format(" And lc.CRUser='{0}' ", user);
            }

            string starDate = (year == "" ? DateTime.Now.Year.ToString() : year) + "-";
            string EndDate = "";
            if (month == "0" || month == "")
            {
                starDate = starDate + "01" + "-01";
                EndDate = (year == "" ? DateTime.Now.Year.ToString() : year) + "-12" + "-31";
            }
            else
            {
                starDate = starDate + month + "-01";
                EndDate = DateTime.Parse(starDate).AddMonths(1).AddDays(-1).ToString("yyyy-MM-dd");
            }

            strWhere += string.Format(" And lc.CRDate>='{0}' and  lc.CRDate<='{1}' ", starDate, EndDate);

            int page = 0;
            int pagecount = 10;
            int.TryParse(context.Request["p"] ?? "1", out page);
            int.TryParse(context.Request["pagecount"] ?? "10", out pagecount);//页数
            page = page == 0 ? 1 : page;
            int total = 0;

            DataTable dt = new SZHL_CCXJB().GetDataPager("SZHL_LCSP lc left join Yan_WF_PD pd on pd.ID=lc.LeiBie ", "lc.*,dbo.fn_PDStatus(lc.intProcessStanceid) AS StateName,pd.ID as PDID,pd.ProcessType,pd.ProcessName", pagecount, page, " lc.CRDate desc", strWhere, ref total);

            if (dt.Rows.Count > 0)
            {
                dt.Columns.Add("FileList", Type.GetType("System.Object"));
                dt.Columns.Add("SubExt", typeof(DataTable));

                DataTable dtExt = new JH_Auth_ExtendModeB().GetExtColumnAll(UserInfo.User.ComId, "LCSP", P1);
                foreach (DataRow drExt in dtExt.Rows)
                {
                    dt.Columns.Add(drExt["TableFiledName"].ToString(), Type.GetType("System.String"));
                }

                foreach (DataRow dr in dt.Rows)
                {
                    if (dr["Files"] != null && dr["Files"].ToString() != "")
                    {
                        dr["FileList"] = new FT_FileB().GetEntities(" ID in (" + dr["Files"].ToString() + ")");
                    }

                    #region 扩展字段
                    DataTable dtExtData = new JH_Auth_ExtendModeB().GetExtData(UserInfo.User.ComId, "LCSP", dr["ID"].ToString(), P1);
                    dr["SubExt"] = dtExtData;
                    foreach (DataRow drExtData in dtExtData.Rows)
                    {
                        dr[drExtData["TableFiledName"].ToString()] = drExtData["ExtendDataValue"].ToString();
                    }
                    #endregion
                }

                msg.Result2 = dtExt;
            }

            msg.Result = dt;
            msg.Result1 = total;


        }

        /// <summary>
        /// 流程审批导出
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void EXPORTLC(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            string userName = UserInfo.User.UserName;
            string strWhere = " 1=1 and (dbo.fn_PDStatus(lc.intProcessStanceid)='已审批' or dbo.fn_PDStatus(lc.intProcessStanceid)='-1')  and lc.ComId=" + UserInfo.User.ComId;

            if (P1 != "")
            {
                strWhere += string.Format(" And lc. LeiBie='{0}' ", P1);
            }

            string user = context.Request["yhm"] ?? "";
            string year = context.Request["year"] ?? "";
            string month = context.Request["month"] ?? "";

            if (user != "")
            {
                strWhere += string.Format(" And lc.CRUser='{0}' ", user);
            }

            string starDate = (year == "" ? DateTime.Now.Year.ToString() : year) + "-";
            string EndDate = "";
            if (month == "0" || month == "")
            {
                starDate = starDate + "01" + "-01";
                EndDate = (year == "" ? DateTime.Now.Year.ToString() : year) + "-12" + "-31";
            }
            else
            {
                starDate = starDate + month + "-01";
                EndDate = DateTime.Parse(starDate).AddMonths(1).AddDays(-1).ToString("yyyy-MM-dd");
            }

            strWhere += string.Format(" And lc.CRDate>='{0}' and  lc.CRDate<='{1}' ", starDate, EndDate);

            string strCls = string.Empty;
            strCls = "lc.ID,pd.ProcessName '流程',lc.ShenQingRen '申请人',CONVERT(varchar(100), lc.CRDate, 120) '申请时间'";
            DataTable dt = new SZHL_LCSPB().GetDTByCommand("select " + strCls + " from SZHL_LCSP lc left join Yan_WF_PD pd on pd.ID=lc.LeiBie where " + strWhere + " order by lc.CRDate desc");

            if (dt.Rows.Count > 0)
            {
                DataTable dtExt = new JH_Auth_ExtendModeB().GetExtColumnAll(UserInfo.User.ComId, "LCSP", P1);
                foreach (DataRow drExt in dtExt.Rows)
                {
                    dt.Columns.Add(drExt["TableFiledName"].ToString(), Type.GetType("System.String"));
                }

                foreach (DataRow dr in dt.Rows)
                {
                    #region 扩展字段
                    DataTable dtExtData = new JH_Auth_ExtendModeB().GetExtData(UserInfo.User.ComId, "LCSP", dr["ID"].ToString(), P1);
                    foreach (DataRow drExtData in dtExtData.Rows)
                    {
                        dr[drExtData["TableFiledName"].ToString()] = drExtData["ExtendDataValue"].ToString();
                    }
                    #endregion
                }
            }
            dt.Columns.Remove("ID");

            CommonHelp ch = new CommonHelp();
            msg.ErrorMsg = ch.ExportToExcel("流程", dt);
        }

        #endregion

        #region 部门数据列表
        /// <summary>
        /// 部门数据列表
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1">类别</param>
        /// <param name="P2">审批内容</param>
        /// <param name="UserInfo"></param>
        public void GETBMLCSPLIST_PAGE(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            string strWhere = string.Format(" lc.ComId={0} ", UserInfo.User.ComId);
            if (P1 != "")
            {
                strWhere += string.Format(" And LeiBie='{0}'", P1);
            }
            int page = 0;
            int.TryParse(context.Request["p"] ?? "1", out page);//页码
            page = page == 0 ? 1 : page;
            int totalCount = 0;
            DataTable dt = new Yan_WF_TDB().GetDataPager("SZHL_LCSP lc inner join  Yan_WF_PI wfpi  on lc.intProcessStanceid=wfpi.ID inner join Yan_WF_PD pd on pd.ID=lc.LeiBie ", @" lc.*, case WHEN wfpi.isComplete is null and wfpi.IsCanceled is null  THEN '正在审批' 
                                            when wfpi.isComplete='Y' then '已审批'  WHEN wfpi.IsCanceled='Y' then '已退回' END StateName,pd.ProcessName", 8, page, "lc.CRDate desc", strWhere, ref totalCount);

            msg.Result = dt;
            msg.Result1 = totalCount;
        }
        #endregion

        #region 流程管理

        #region 流程设置相关

        /// <summary>
        /// 获取流程列表 P1==""流程设置列表，P1!="" 自定义流程添加选择列表
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void GETWFPDLIST(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            if (P1 != "")
            {
                int lcs = int.Parse(P1);
                string strSql = string.Format(@"SELECT * from Yan_WF_PD  where   lcstatus='{0}' and ComId={1} and  IsSuspended= 'Y'  and ','+ManageUser+',' like '%,{2},%'", lcs, UserInfo.User.ComId, UserInfo.User.UserName);

                string strRoleSQL = "";
                foreach (var item in UserInfo.UserRoleCode.Split(','))
                {
                    strRoleSQL = strRoleSQL + string.Format(@"SELECT * from Yan_WF_PD  where   lcstatus='{0}' and ComId={1} and  IsSuspended= 'Y'  and ','+ManageRole+',' like '%,{2},%'", lcs, UserInfo.User.ComId, item);
                    strRoleSQL = strRoleSQL + "  UNION  ";
                }
                if (strRoleSQL.Length > 5)
                {
                    strRoleSQL = strRoleSQL.TrimEnd();
                    strRoleSQL = strRoleSQL.Substring(0, strRoleSQL.Length - 5);
                    strSql = strSql + " UNION " + strRoleSQL;
                }
                DataTable dtData = new Yan_WF_PDB().GetDTByCommand(strSql);
                msg.Result = dtData;

            }
            else
            {
                string strWhere = " 1=1 and wfpd.ComId=" + UserInfo.User.ComId;
                string strContent = context.Request["Content"] ?? "";
                strContent = strContent.TrimEnd();
                if (strContent != "")
                {
                    strWhere += string.Format(" And ( wfpd.ProcessName like '%{0}%' )", strContent);
                }
                string strLB = context.Request["LB"] ?? "";
                strLB = strLB.TrimEnd();
                if (strLB != "")
                {
                    strWhere += string.Format(" And ( wfpd.RelatedTable like '%{0}%' )", strLB);
                }
                string strSql = string.Format(@"SELECT DISTINCT wfpd.RelatedTable, wfpd.ProcessName,wfpd.ManageUser,wfpd.ID,count(wfpi.ID) formCount,wfpd.lcstatus,wfpd.IsSuspended from Yan_WF_PD wfpd LEFT join Yan_WF_PI wfpi on wfpd.ID=wfpi.PDID where   wfpd.isTemp='1' and  {0} group by  wfpd.RelatedTable, wfpd.ProcessName,wfpd.ID,wfpd.lcstatus,wfpd.IsSuspended,wfpd.ManageUser", strWhere, UserInfo.User.UserName);
                msg.Result = new Yan_WF_PDB().GetDTByCommand(strSql);
            }
        }

        /// <summary>
        /// 流程审批添加
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void ADDPROCESS(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            Yan_WF_PD lcsp = JsonConvert.DeserializeObject<Yan_WF_PD>(P1);
            if (lcsp.ProcessName.Trim() == "")
            {
                msg.ErrorMsg = "流程名称不能为空";
                return;
            }
            if (lcsp.ID == 0)//如果Id为0，为添加操作
            {
                if (new Yan_WF_PDB().GetEntities(d => d.ComId == UserInfo.User.ComId && d.ProcessName == lcsp.ProcessName).Count() > 0)
                {
                    msg.ErrorMsg = "已存在此流程";
                    return;
                }

                lcsp.lcstatus = 1;
                lcsp.ComId = UserInfo.User.ComId;
                if (lcsp.isTemp == "1")//如果是模版流程,加上初始模版
                {
                    lcsp.Tempcontent = @"<div class='form-group data-control' datatype='textarea' dataname='表单内容'><label class='col-sm-2 control-label'>表单内容</label><div class='col-sm-9'><textarea class='form-control szhl_UEEDIT' id='ueedit1'></textarea></div></div>";
                }
                if (lcsp.ManageRole != null)
                {
                    lcsp.ManageRole.Trim(',');
                }
                new Yan_WF_PDB().Insert(lcsp); //添加流程表数据

                string qymodelId = context.Request["qymodelId"] ?? ""; //JH_Auth_QY_Model表Id
                if (!string.IsNullOrEmpty(qymodelId))
                {
                    string modelCode = context.Request["modelCode"] ?? "";
                    int lcPDID = lcsp.ID;
                    if (modelCode.ToUpper() == "LCSP")
                    {
                        lcPDID = -1;
                    }
                    //更新JH_Auth_QY_Model表的PDID字段
                    string strSql = string.Format("UPDATE JH_Auth_QY_Model set PDID={0} where ComId={1} and ID={2}", lcPDID, UserInfo.User.ComId, qymodelId);
                    new JH_Auth_QY_ModelB().ExsSql(strSql);
                }
            }
            else
            {
                //修改流程表数据
                new Yan_WF_PDB().Update(lcsp);
            }
            //如果流程类型为固定流程并且固定流程内容不为空，添加固定流程数据
            if (lcsp.ProcessType == "1" && !string.IsNullOrEmpty(P2))
            {
                List<Yan_WF_TD> tdList = JsonConvert.DeserializeObject<List<Yan_WF_TD>>(P2);
                tdList.ForEach(d => d.ProcessDefinitionID = lcsp.ID);
                tdList.ForEach(d => d.ComId = UserInfo.User.ComId);
                tdList.ForEach(d => d.CRDate = DateTime.Now);
                tdList.ForEach(d => d.CRUser = UserInfo.User.UserName);
                tdList.ForEach(d => d.TDCODE = d.ProcessDefinitionID + "-" + d.Taskorder);
                tdList.ForEach(d => d.AssignedRole = d.AssignedRole.Trim(','));
                //tdList.ForEach(d => d.TDCODE = (d.ID == 0 ? (lcsp.ID + d.TDCODE) : d.TDCODE));
                new Yan_WF_TDB().Delete(d => d.ProcessDefinitionID == tdList[0].ProcessDefinitionID);
                new Yan_WF_TDB().Insert(tdList);

            }
            msg.Result = lcsp;
        }
        /// <summary>
        /// 获取流程信息
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void GETPROCESSBYID(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            int Id = int.Parse(P1);
            msg.Result = new Yan_WF_PDB().GetEntity(d => d.ID == Id && d.ComId == UserInfo.User.ComId);
        }
        /// <summary>
        /// 删除流程信息
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void DELPROCESSBYID(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            try
            {

                if (new Yan_WF_PDB().Delete(d => d.ID.ToString() == P1))
                {
                    msg.ErrorMsg = "";
                }
            }
            catch (Exception ex)
            {
                msg.ErrorMsg = ex.Message;
            }
        }
        public void SETMANAGEUSER(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            int Id = 0;
            int.TryParse(P1, out Id);
            string strSql = string.Format(" update Yan_WF_PD set ManageUser='{0}' where Id={1} and ComId={2}", P2, Id, UserInfo.User.ComId);
            new Yan_WF_PDB().ExsSql(strSql);
        }

        /// <summary>
        /// 获取流程类别数据
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void GETLCBDLB(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            string strSql = string.Format(" SELECT DISTINCT RelatedTable FROM  Yan_WF_PD WHERE ComId={0} and RelatedTable!='' and RelatedTable is not null  ", UserInfo.User.ComId);
            msg.Result = new Yan_WF_PDB().GetDTByCommand(strSql);
            msg.Result1 = new JH_Auth_RoleB().GetALLEntities();

        }


        #endregion





        /// <summary>
        /// 开始流程
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1">启动流程的应用Code</param>
        /// <param name="P2">审核人信息</param>
        /// <param name="UserInfo"></param>
        public void STARTWF(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            try
            {
                string strModelCode = P1;
                string LCTYPE = context.Request["LCTYPE"] ?? "";
                string strCSR = context.Request["csr"] ?? "";


                int PDID = 0;
                int.TryParse(context.Request["PDID"] ?? "0", out PDID);

                //数据ID
                int DataID = 0;
                int.TryParse(context.Request["DataID"] ?? "0", out DataID);

                if (LCTYPE == "-1" || LCTYPE == "") //没有流程
                {
                    WFComplete(strModelCode, DataID.ToString(), "Y", UserInfo);
                    return;
                }


                Yan_WF_PIB PIB = new Yan_WF_PIB();
                if (PDID == 0)
                {
                    //如果找不到PDID,就通过FORMCODE找PDID
                    string strSql = string.Format("SELECT PDID from JH_Auth_QY_Model inner join JH_Auth_Model model on ModelID=model.ID where JH_Auth_QY_Model.ComId={0} and model.ModelCode='{1}'", UserInfo.User.ComId, strModelCode);
                    object obj = PIB.ExsSclarSql(strSql);
                    PDID = obj != null ? int.Parse(obj.ToString()) : 0;
                }
                Yan_WF_PD PD = new Yan_WF_PDB().GetEntity(d => d.ID == PDID && d.ComId == UserInfo.User.ComId);
                string strTZR = P2;//审核人

                List<string> ListNextUser = new List<string>();//获取下一任务的处理人
                Yan_WF_TI TI = PIB.StartWF(PD, strModelCode, UserInfo.User.UserName, P2, strCSR, ref ListNextUser);





                //更新关联表的流程ID
                PIB.UpdateDataIdByCode(strModelCode, DataID, TI.PIID);
                msg.Result = TI;

                //发送消息给审核人员

                string jsr = ListNextUser.ListTOString(',');
                if (!string.IsNullOrEmpty(jsr))
                {
                    SZHL_TXSX TX = new SZHL_TXSX();
                    TX.Date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    TX.APIName = "LCSP";
                    TX.ComId = UserInfo.User.ComId;
                    TX.FunName = "LCSP_CHECK";
                    TX.intProcessStanceid = TI.PIID;
                    TX.CRUserRealName = UserInfo.User.UserRealName;
                    TX.MsgID = DataID.ToString();
                    TX.TXContent = UserInfo.User.UserRealName + "发起了一个" + PD.ProcessName + "，请您查阅审核";
                    TX.TXUser = jsr;
                    TX.TXMode = strModelCode;
                    TX.CRUser = UserInfo.User.UserName;
                    TXSX.TXSXAPI.AddALERT(TX); //时间为发送时间
                }
                //发送消息
            }
            catch (Exception ex)
            {
                msg.ErrorMsg = ex.Message;
            }
        }



        /// <summary>
        /// 获取流程的具体步骤
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void GETTDLIST(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            int Id = int.Parse(P1);
            msg.Result = new Yan_WF_TDB().GetEntities(d => d.ProcessDefinitionID == Id).OrderBy(d => d.Taskorder);
        }


        /// <summary>
        /// 对流程待处理人员发送提醒
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void SENDLCCB(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            int Id = int.Parse(P1);
            var CBResult = new Yan_WF_TIB().GetEntities(d => d.PIID == Id && d.TaskState == 0);
            foreach (Yan_WF_TI item in CBResult)
            {
                SZHL_TXSX MODEL = new SZHL_TXSXB().GetEntity(d => d.TXUser == item.TaskUserID && d.intProcessStanceid == item.PIID);
                if (MODEL != null)
                {
                    MODEL.Status = "0";
                    new SZHL_TXSXB().Update(MODEL);
                }
            }
        }



        public void MANAGEWF(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            try
            {
                string strShUser = context.Request["SHUser"] ?? "";
                string strCSUser = context.Request["csr"] ?? "";
                string modelcode = context.Request["formcode"] ?? "";

                int PID = int.Parse(P1);

                int DATAID = 0;
                int.TryParse(context.Request["ID"] ?? "0", out DATAID);


                Yan_WF_PIB PIB = new Yan_WF_PIB();
                if (PIB.isCanSP(UserInfo.User.UserName, PID) == "Y")//先判断用户能不能处理此流程
                {



                    List<string> ListNextUser = new List<string>();
                    PIB.MANAGEWF(UserInfo.User.UserName, PID, P2, ref ListNextUser, strShUser);//处理任务
                    Yan_WF_PI PI = PIB.GetEntity(d => d.ID == PID);

                    //更新抄送人
                    PI.ChaoSongUser = strCSUser;
                    PIB.Update(PI);
                    //更新抄送人

                    Yan_WF_PD PD = new Yan_WF_PDB().GetEntity(d => d.ID == PI.PDID.Value);

                    string content = new JH_Auth_UserB().GetUserRealName(UserInfo.QYinfo.ComId, PI.CRUser) + "发起了" + PD.ProcessName + "表单,等待您审阅";
                    string strTXUser = ListNextUser.ListTOString(',');
                    string funName = "LCSP_CHECK";
                    //添加消息提醒
                    string strIsComplete = ListNextUser.Count() == 0 ? "Y" : "N";//结束流程,找不到人了
                    if (strIsComplete == "Y")//找不到下家就结束流程,并且给流程发起人发送消息
                    {
                        PIB.ENDWF(PID);
                        msg.Result = "Y";//已结束
                        content = UserInfo.User.UserRealName + "审批完成了您发起的" + PD.ProcessName + "表单";
                        strTXUser = PI.CRUser;
                        funName = "LCSP_CHECK";
                        //发送消息给抄送人 
                        if (!string.IsNullOrEmpty(PI.ChaoSongUser))
                        {
                            SZHL_TXSX CSTX = new SZHL_TXSX();
                            CSTX.Date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                            CSTX.APIName = "LCSP";
                            CSTX.ComId = UserInfo.User.ComId;
                            CSTX.FunName = "LCSP_CHECK";
                            CSTX.intProcessStanceid = PID;
                            CSTX.CRUserRealName = UserInfo.User.UserRealName;
                            CSTX.MsgID = DATAID.ToString();
                            CSTX.TXContent = new JH_Auth_UserB().GetEntity(p => p.ComId == PI.ComId && p.UserName == PI.CRUser).UserRealName + "抄送一个" + PD.ProcessName + "，请您查阅接收";
                            CSTX.ISCS = "Y";
                            CSTX.TXUser = PI.ChaoSongUser;
                            CSTX.TXMode = modelcode;
                            CSTX.CRUser = UserInfo.User.UserName;
                            TXSX.TXSXAPI.AddALERT(CSTX); //时间为发送时间
                        }
                    }
                    SZHL_TXSX TX = new SZHL_TXSX();
                    TX.Date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    TX.APIName = "LCSP";
                    TX.ComId = UserInfo.User.ComId;
                    TX.FunName = funName;
                    TX.intProcessStanceid = PID;
                    TX.CRUser = PI.CRUser;
                    TX.CRUserRealName = UserInfo.User.UserRealName;
                    TX.MsgID = DATAID.ToString();
                    TX.TXContent = content;
                    TX.TXUser = strTXUser;
                    TX.TXMode = modelcode;
                    TXSX.TXSXAPI.AddALERT(TX); //时间为发送时间

                    WFComplete(modelcode, DATAID.ToString(), strIsComplete, UserInfo);


                }
                else
                {
                    msg.ErrorMsg = "该流程已被处理,您已无法处理此流程";
                }

            }
            catch (Exception ex)
            {
                msg.ErrorMsg = ex.Message;
            }
        }



        /// <summary>
        /// 流程处理完毕时调用的方法
        /// </summary>
        /// <param name="strModelCode"></param>
        /// <param name="strDataID"></param>
        /// <param name="isComplete">是否最后一步Y:最后一步</param>
        public void WFComplete(string strModelCode, string strDataID, string isComplete, JH_Auth_UserB.UserInfo UserInfo)
        {
          
        }


        /// <summary>
        /// 退回当前流程
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void REBACKWF(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            try
            {
                int PID = int.Parse(P1);
                Yan_WF_PIB PIB = new Yan_WF_PIB();
                if (PIB.isCanSP(UserInfo.User.UserName, PID) == "Y")//先判断用户能不能处理此流程
                {
                    new Yan_WF_PIB().REBACKLC(UserInfo.User.UserName, PID, P2);//结束任务
                    string ModeCode = context.Request["formcode"] ?? "LCSP";
                    int ID = 0;
                    int.TryParse(context.Request["ID"] ?? "0", out ID);
                    if (ID > 0 && !string.IsNullOrEmpty(ModeCode))
                    {
                        Yan_WF_PI PI = new Yan_WF_PIB().GetEntity(d => d.ID == PID);


                        //消息提醒
                        SZHL_TXSX TX = new SZHL_TXSX();
                        TX.Date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                        TX.APIName = "LCSP";
                        TX.ComId = UserInfo.User.ComId;
                        TX.FunName = "LCSP_CHECK";
                        TX.intProcessStanceid = PID;
                        TX.CRUserRealName = UserInfo.User.UserRealName;
                        TX.MsgID = ID.ToString();
                        TX.TXContent = UserInfo.User.UserRealName + "退回了" + new Yan_WF_PDB().GetEntity(d => d.ID == PI.PDID.Value).ProcessName + "，请您查阅";
                        TX.TXUser = PI.CRUser;
                        TX.TXMode = ModeCode;
                        TX.CRUser = UserInfo.User.UserName;
                        TXSX.TXSXAPI.AddALERT(TX); //时间为发送时间
                    }
                }
                else
                {
                    msg.ErrorMsg = "该流程已被处理,您已无法处理此流程";
                }





            }
            catch (Exception ex)
            {
                msg.ErrorMsg = ex.Message;
            }
        }


        /// <summary>
        /// 获取流程数据(返回流程数据以及该流程的任务数据)
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void GETWFDATA(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            try
            {
                msg.Result2 = "{ \"ISCANSP\":\"N\",\"ISCANCEL\":\"N\",\"ISCANEDIT\":\"Y\"}";
                msg.Result3 = "-1";
                msg.Result4 = "Y";


                string ModelCode = context.Request["ModelCode"];
                int DataID = 0;
                int PDID = -1;
                string strDataID = context.Request["DataID"] ?? "0";
                int.TryParse(strDataID, out DataID);
                if (DataID == 0) //添加页面
                {
                    if (ModelCode == "LCSP")
                    {
                        if (P2 != "")
                        {
                            PDID = int.Parse(P2);
                        }
                    }
                    else
                    {
                        //1.获取PDID
                        string strSql = string.Format("SELECT qymodel.PDID from JH_Auth_QY_Model qymodel inner join JH_Auth_Model model on ModelID=model.ID where qymodel.ComId={0} and model.ModelCode='{1}'", UserInfo.User.ComId, ModelCode);
                        object obj = new Yan_WF_PIB().ExsSclarSql(strSql);
                        if (obj != null && obj.ToString() != "")
                        {
                            PDID = Int32.Parse(obj.ToString());
                        }
                    }
                }


                int PIID = 0;
                int.TryParse(P1, out PIID);
                DataTable dtList = new DataTable();
                DataTable dt = new DataTable();
                if (PIID > 0)
                {
                    Yan_WF_PI PIMODEL = new Yan_WF_PIB().GetEntity(d => d.ID == PIID);

                    //固定流程获取流程处理数据
                    if (PIMODEL == null)
                    {
                        msg.ErrorMsg = "流程数据已清除";
                        return;
                    }
                    else
                    {
                        //todo 需要处理PITYPE=-1的情况（关闭流程）
                        if (PIMODEL.PITYPE == "0") //0自由流程,1固定流程
                        { //获取流程处理数据
                            dtList = new Yan_WF_TIB().GetEntities(d => d.PIID == PIID && d.TDCODE == "-1").ToDataTable();
                            dtList.Columns.Add("userrealname");
                            dtList.Columns.Add("state");
                            for (int i = 0; i < dtList.Rows.Count; i++)
                            {
                                dtList.Rows[i]["userrealname"] = new JH_Auth_UserB().GetUserRealName(UserInfo.QYinfo.ComId, dtList.Rows[i]["TaskUserID"].ToString());
                                dtList.Rows[i]["state"] = dtList.Rows[i]["TaskState"];
                            }
                        }
                        else
                        {
                            if (string.IsNullOrEmpty(PIMODEL.isComplete) && string.IsNullOrEmpty(PIMODEL.IsCanceled))
                            {
                                dtList = new Yan_WF_TDB().GetEntities(d => d.ProcessDefinitionID == PIMODEL.PDID.Value).OrderBy(d => d.Taskorder).ToDataTable();
                                dtList.Columns.Add("userrealname");
                                dtList.Columns.Add("EndTime");
                                dtList.Columns.Add("TaskUserView");
                                dtList.Columns.Add("state");

                                foreach (DataRow dr in dtList.Rows)
                                {
                                    string tdCode = dr["TDCODE"].ToString();
                                    Yan_WF_TI tiModel = new Yan_WF_TIB().GetEntity(d => d.PIID == PIID && d.TDCODE == tdCode && d.EndTime != null);//
                                    if (tiModel != null)
                                    {
                                        dr["userrealname"] = new JH_Auth_UserB().GetUserRealName(UserInfo.QYinfo.ComId, tiModel.TaskUserID);
                                        dr["EndTime"] = tiModel.EndTime;
                                        dr["TaskUserView"] = tiModel.TaskUserView;
                                        dr["state"] = tiModel.TaskState;
                                    }
                                    else
                                    {
                                        dr["EndTime"] = "";
                                        dr["TaskUserView"] = "";
                                        dr["userrealname"] = "";
                                        dr["state"] = "";
                                        if (PIMODEL.IsCanceled != "Y")
                                        { dr["state"] = "0"; }
                                    }
                                }
                            }
                            else
                            {
                                dtList = new Yan_WF_TIB().GetEntities(d => d.PIID == PIID && d.EndTime != null).OrderBy(d => d.TDCODE).ToDataTable();//
                                dtList.Columns.Add("userrealname");
                                dtList.Columns.Add("state");
                                foreach (DataRow dr in dtList.Rows)
                                {
                                    dr["userrealname"] = new JH_Auth_UserB().GetUserRealName(UserInfo.QYinfo.ComId, dr["TaskUserID"].ToString());
                                    dr["state"] = dr["TaskState"].ToString();
                                }
                            }
                        }
                    }

                    //获取流程处理数据
                    msg.Result = PIMODEL;//PIMODEL
                    msg.Result1 = dtList;//审批数据
                    msg.Result2 = "{ \"ISCANSP\":\"" + new Yan_WF_PIB().isCanSP(UserInfo.User.UserName, int.Parse(P1)) + "\",\"ISCANCEL\":\"" + new Yan_WF_PIB().isCanCancel(UserInfo.User.UserName, int.Parse(P1)) + "\"}";
                    msg.Result3 = PIMODEL.PITYPE;//
                    msg.Result4 = new Yan_WF_PIB().isCanEdit(UserInfo.User.UserName, int.Parse(P1));

                    msg.Result6 = new JH_Auth_User_CenterB().GetEntities("Remark = " + PIID.ToString() + " AND isCS='Y'");
                }
                else    //新增流程
                {
                    if (PDID > 0)
                    {
                        Yan_WF_PD pdmodel = new Yan_WF_PDB().GetEntity(d => d.ID == PDID);
                        if (pdmodel != null && pdmodel.ProcessType == "1")//0自由流程,1固定流程 
                        {
                            dtList = new Yan_WF_TDB().GetEntities(d => d.ProcessDefinitionID == pdmodel.ID && d.TDCODE != "-1").OrderBy(d => d.Taskorder).ToDataTable();
                            dtList.Columns.Add("userrealname");
                            dtList.Columns.Add("EndTime");
                            dtList.Columns.Add("TaskUserView");
                            dtList.Columns.Add("state");
                            dtList.Rows[0]["userrealname"] = UserInfo.User.UserRealName;
                            dtList.Rows[0]["TaskUserView"] = "发起表单";
                            dtList.Rows[0]["EndTime"] = DateTime.Now.ToString("yyyy-MM-dd");
                            dtList.Rows[0]["state"] = "1";
                            msg.Result = null;
                            msg.Result1 = dtList;

                        }
                        msg.Result3 = pdmodel.ProcessType;
                        msg.Result5 = pdmodel;
                    }
                    else
                    {
                        if (DataID != 0)
                        {
                            msg.Result4 = new JH_Auth_QY_ModelB().isHasDataQX(ModelCode, DataID, UserInfo);
                        }

                    }

                }

            }
            catch (Exception ex)
            {
                msg.ErrorMsg = ex.Message;
            }
        }


        /// <summary>
        /// 禁用或启用流程审批类别
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void MODIFYLCSTATE(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            try
            {
                string strSQL = string.Format("Update Yan_WF_PD set IsSuspended='{0}' where Id={1}", P1, P2);
                new Yan_WF_PDB().ExsSql(strSQL);
            }
            catch (Exception ex)
            {
                msg.ErrorMsg = ex.Message;
            }
        }


        /// <summary>
        /// 根据MODELCODE获取流程定义ID
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        /// <returns></returns>
        public void GETPDIDBYMODECODE(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            try
            {
                string strSql = string.Format("SELECT qymodel.PDID from JH_Auth_QY_Model qymodel inner join JH_Auth_Model model on ModelID=model.ID where qymodel.ComId={0} and model.ModelCode='{1}'", UserInfo.User.ComId, P1);
                object obj = new Yan_WF_PIB().ExsSclarSql(strSql);
                if (obj == null || obj.ToString() == "")
                {
                    msg.Result = "-1";
                }
                else
                {
                    msg.Result = obj.ToString();
                }
            }
            catch (Exception ex)
            {
                msg.ErrorMsg = ex.Message;
            }

        }



        /// <summary>
        /// 获取审核人列表
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void GETSPUSERLIST(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            int PDID = 0;
            int.TryParse(context.Request["PDID"] ?? "0", out PDID);
            if (PDID == 0)
            {
                int PIID = 0;
                int.TryParse(context.Request["PIID"] ?? "0", out PIID);
                PDID = new Yan_WF_PIB().GETPDID(PIID);
            }
            if (PDID == 0)
            {
                string strFormCode = P1;
                string strSql = string.Format("SELECT qymodel.PDID from JH_Auth_QY_Model qymodel inner join JH_Auth_Model model on ModelID=model.ID where qymodel.ComId={0} and model.ModelCode='{1}'", UserInfo.User.ComId, P1);
                object obj = new Yan_WF_PIB().ExsSclarSql(strSql);
                PDID = obj != null && obj.ToString() != "" ? int.Parse(obj.ToString()) : 0;
            }
            if (PDID > 0)
            {
                string[] users = new Yan_WF_PDB().GetEntity(d => d.ID == PDID).Alias.Split(',');
                List<JH_Auth_User> userList = new JH_Auth_UserB().GetEntities(d => d.ComId == UserInfo.User.ComId && users.Contains(d.UserName)).ToList();
                msg.Result = userList;
            }
        }


        /// <summary>
        /// 删除流程信息
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void DELPIINFO(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            if (P1 != "")//PIID
            {
                new Yan_WF_PIB().Delete(d => d.ID.ToString() == P1);
                new Yan_WF_TIB().Delete(d => d.PIID.ToString() == P1);
            }
        }



        public void CANCELWF(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            string strMode = context.Request["ModelCode"].ToString();

            int DataID = 0;
            int.TryParse(context.Request["DataID"] ?? "0", out DataID);

            int PIID = 0;
            if (!int.TryParse(P1, out PIID))
            {
                msg.ErrorMsg = "数据错误";
                return;
            }

            string strISCanCel = new Yan_WF_PIB().isCanCancel(UserInfo.User.UserName, PIID);
            if (strISCanCel == "N")
            {
                msg.ErrorMsg = "该表单已处理完毕,您无法再进行撤回操作";
                return;
            }

            int PDID = 0;
            int.TryParse(P2, out PDID);
            //添加草稿数据


            SZHL_DRAFT MODEL = new SZHL_DRAFTB().GetEntities(d => d.DataID == DataID && d.FormCode == strMode).FirstOrDefault();
            if (MODEL != null)
            {
                MODEL.DataID = null;
                MODEL.CRTime = DateTime.Now;
                new SZHL_DRAFTB().Update(MODEL);
            }


            //删除流程相关数据
            new Yan_WF_PIB().Delete(d => d.ID == PIID);
            new Yan_WF_TIB().Delete(d => d.PIID == PIID);

            //删除表单数据
        }



        #endregion

        #region 流程中的消息处理方法
        public void LCSP_CHECK(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            SendCommonMsg(P1, "A");

        }
        public void SendCommonMsg(string P1, string type)
        {
            SZHL_TXSX TX = JsonConvert.DeserializeObject<SZHL_TXSX>(P1);
            Article ar0 = new Article();
            ar0.Title = TX.TXContent;
            ar0.Description = "";
            ar0.Url = TX.MsgID;
            List<Article> al = new List<Article>();
            al.Add(ar0);
            JH_Auth_UserB.UserInfo UserInfo = new JH_Auth_UserB().GetUserInfo(TX.ComId.Value, TX.CRUser);
            if (!string.IsNullOrEmpty(TX.TXUser))
            {
                try
                {
                    //发送PC消息
                    new JH_Auth_User_CenterB().SendMsg(UserInfo, TX.TXMode, TX.TXContent, TX.MsgID, TX.TXUser, type, TX.intProcessStanceid, TX.ISCS);
                }
                catch (Exception)
                {
                }

                //发送微信消息
                WXHelp wx = new WXHelp(UserInfo.QYinfo);
                wx.SendTH(al, TX.TXMode, "A", TX.TXUser);



            }
        }
        #endregion



        //首页流程数据
        public void GETLCSPLIST_INDEX(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            DataTable dt = new Yan_WF_PIB().GetDSH_SY(UserInfo.User);
            //用车，出差，会议待处理
            if (dt.Rows.Count > 0)
            {
                dt.Columns.Add("DATAID");
                foreach (DataRow dr in dt.Rows)
                {
                    dr["DATAID"] = new Yan_WF_PIB().GetFormIDbyPID(dr["ModelCode"].ToString(), Int32.Parse(dr["PIID"].ToString()));

                }
            }
            msg.Result = dt;
            //流程审批待处理
            string PIIDs = new Yan_WF_TIB().GetEntities(d => d.TaskState == 0 && d.TaskUserID == UserInfo.User.UserName && d.ComId == UserInfo.User.ComId).Select(d => d.PIID).ToList().ListTOString(',');
            if (PIIDs.Length > 0)
            {
                DataTable dtLcsp = new SZHL_LCSPB().GetDTByCommand(string.Format("SELECT lcsp.*,wfpd.ID as PDID,wfpd.ProcessName from SZHL_LCSP lcsp join Yan_WF_PD  wfpd  on lcsp.LeiBie=wfpd.ID where lcsp.intProcessStanceid in ({0})", PIIDs));
                msg.Result1 = dtLcsp;
            }
            //已处理
            string strSql = string.Format(@"SELECT top 8 wfpd.ProcessName,wfpi.CRUser,wfpi.CRDate,wfti.TaskState,wfti.PIID intProcessStanceid,isnull(model.ModelCode,'LCSP') ModelCode,wfpd.ID as PDID from Yan_WF_TI wfti inner join   
                                                    Yan_WF_PI  wfpi on wfpi.ID=wfti.PIID 
                                                    inner join Yan_WF_PD wfpd on wfpi.PDID=wfpd.ID
                                                    left join  JH_Auth_QY_Model qymodel on wfpd.ID=qymodel.PDID and qymodel.ComId=wfpi.ComId
                                                    left join JH_Auth_Model model on qymodel.ModelID=model.ID
                                                    where  EndTime IS NOT NULL and TaskUserID='{0}' and wfti.ComId={1} and TDCODE not like '%-1' order by wfti.EndTime  DESC", UserInfo.User.UserName, UserInfo.User.ComId);
            DataTable dtYCL = new SZHL_LCSPB().GetDTByCommand(strSql);
            if (dtYCL.Rows.Count > 0)
            {
                dtYCL.Columns.Add("ID");
                foreach (DataRow dr in dtYCL.Rows)
                {
                    dr["ID"] = new Yan_WF_PIB().GetFormIDbyPID(dr["ModelCode"].ToString(), Int32.Parse(dr["intProcessStanceid"].ToString()));

                }
            }
            msg.Result2 = dtYCL;
        }

        //待审核统计
        public void GETMODELDSHQTY(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            string ModelCode = P1;
            msg.Result = new Yan_WF_PIB().GetDTByCommand("SELECT P.WFFormNum AS ModelCode,COUNT(T.PIID) AS QTY FROM Yan_WF_PI P JOIN Yan_WF_TI T ON P.ID=T.PIID WHERE P.ComId='" + UserInfo.User.ComId + "' AND P.WFFormNum='" + ModelCode + "' AND P.WFFormNum<>'' AND P.WFFormNum IS NOT NULL AND T.TaskState='0' AND T.TaskUserID='" + UserInfo.User.UserName + "' GROUP BY P.WFFormNum");

        }


        #region 删除流程
        /// <summary>
        /// 删除流程(包括使用该流程的表单和相关数据)
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void DELLC(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            int lcID = 0;
            if (!int.TryParse(P1, out lcID))
            {
                msg.ErrorMsg = "数据错误";
                return;
            }
            List<Yan_WF_PI> piList = new Yan_WF_PIB().GetEntities(d => d.PDID == lcID).ToList();
            if (lcID != 0)
            {
                new SZHL_LCSPB().Delete(d => d.LeiBie == lcID);
                new Yan_WF_TDB().Delete(d => d.ProcessDefinitionID == lcID);
                new Yan_WF_PDB().Delete(d => d.ID == lcID);
                if (piList != null && piList.Count > 0)
                {
                    for (int i = 0; i < piList.Count; i++)
                    {
                        new Yan_WF_TIB().Delete(d => d.PIID == piList[i].ID);
                    }
                }
                new Yan_WF_PIB().Delete(d => d.PDID == lcID);
            }
        }
        #endregion




        #region 获取企业微信流程数据
        public void GETWXSHLIST(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            string year = P1;
            string month = P2;

            string starDate = (year == "" ? DateTime.Now.Year.ToString() : year) + "-";
            string EndDate = "";
            if (month == "0" || month == "")
            {
                starDate = starDate + "01" + "-01";
                EndDate = (year == "" ? DateTime.Now.Year.ToString() : year) + "-12" + "-31";
            }
            else
            {
                starDate = starDate + month + "-01";
                EndDate = DateTime.Parse(starDate).AddMonths(1).AddDays(-1).ToString("yyyy-MM-dd");
            }
            string num = context.Request["num"] ?? "";

            WXHelp wx = new WXHelp(UserInfo.QYinfo);
            ////string strSDate =((DateTime.Parse(starDate).Ticks-621355968000000000) / 10000000).ToString();
            ////string strEDate = ((DateTime.Parse(EndDate).Ticks - 621355968000000000) / 10000000).ToString();
            msg.Result = wx.GetWXSHData(starDate, EndDate, num);


        }
        #endregion

    }
}