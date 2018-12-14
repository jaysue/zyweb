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
using System.Collections;
using QJY.Common;


namespace QJY.API
{
    public class HYGLManage : IWsService
    {
        public void ProcessRequest(HttpContext context, ref Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            MethodInfo methodInfo = typeof(HYGLManage).GetMethod(msg.Action.ToUpper());
            HYGLManage model = new HYGLManage();
            methodInfo.FastInvoke(model, new object[] { context, msg, P1, P2, UserInfo });
        }

        #region 会议室管理

        #region 查看会议室列表
        /// <summary>
        /// 查看会议室列表
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void GETHYSLIST_PAGE(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            string strWhere = " cc.ComId=" + UserInfo.User.ComId;
            //strWhere += string.Format(" And cc.CRUser='{0}' ", UserInfo.User.UserName);
            if (P1 != "")
            {
                strWhere += string.Format(" And cc.Name like '%{0}%'", P1);
            }
            if (P2 != "")
            {
                strWhere += string.Format(" And  cc.IsDMT ='{0}'", P2);
            }
            string TYY = context.Request["lb"] ?? "";
            if (TYY != "")
            {
                strWhere += string.Format(" And  cc.IsTYY ='{0}'", TYY);
            }
            int page = 0;
            int pagecount = 8;
            int.TryParse(context.Request["p"] ?? "1", out page);
            int.TryParse(context.Request["pagecount"] ?? "8", out pagecount);//页数
            page = page == 0 ? 1 : page;
            int total = 0;
            DataTable dt = new SZHL_CCXJB().GetDataPager(" SZHL_HYGL_ROOM cc", "cc.*", pagecount, page, " cc.CRDate desc", strWhere, ref total);

            dt.Columns.Add("ZT", Type.GetType("System.String"));
            dt.Columns.Add("ZYSJ", Type.GetType("System.String"));

            foreach (DataRow dr in dt.Rows)
            {
                int rid = Int32.Parse(dr["ID"].ToString());
                var st = DateTime.Now;
                var et = DateTime.Now.AddHours(3);
                var list = new SZHL_HYGLB().GetEntities(p => p.RoomID == rid && p.IsDel == 0 && ((st > p.StartTime && st < p.EndTime) || (et > p.StartTime && et < p.EndTime))).OrderBy(p => p.StartTime);

                if (list.Count() == 0)
                {
                    dr["ZT"] = "0";
                    dr["ZYSJ"] = "";
                }
                else
                {
                    dr["ZT"] = "1";
                    dr["ZYSJ"] = list.First().StartTime.Value.ToString("yyyy-MM-dd HH:mm") + "~" + list.First().EndTime.Value.ToString("yyyy-MM-dd HH:mm");
                }
            }
            msg.Result = dt;
            msg.Result1 = total;
        }
        #endregion

        #region 查看可用会议室列表
        /// <summary>
        /// 查看可用会议室列表
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void GETKYHYSLIST(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            //var list = new SZHL_HYGL_ROOMB().GetEntities(p => p.Status == "1" && p.IsDel == 0);
            DataTable dt = new SZHL_HYGL_ROOMB().GetDTByCommand("select * from dbo.SZHL_HYGL_ROOM where IsDel=0 and Status='1' and ComId=" + UserInfo.User.ComId);

            dt.Columns.Add("ZT", Type.GetType("System.String"));
            dt.Columns.Add("ZYSJ", Type.GetType("System.String"));

            foreach (DataRow dr in dt.Rows)
            {
                int rid = Int32.Parse(dr["ID"].ToString());
                var st = DateTime.Now;
                var et = DateTime.Now.AddHours(3);
                var list = new SZHL_HYGLB().GetEntities(p => p.RoomID == rid && p.IsDel == 0 && ((st >= p.StartTime && st < p.EndTime) || (et > p.StartTime && et <= p.EndTime) || (et > p.StartTime && st <= p.StartTime) || (et >= p.EndTime && st < p.EndTime)) && p.ComId == UserInfo.User.ComId).OrderBy(p => p.StartTime);

                List<int> li = new List<int>();

                foreach (var l in list)
                {
                    var pi = new Yan_WF_PIB().GetEntity(p => p.ComId == UserInfo.User.ComId && p.ID == l.intProcessStanceid);
                    if (pi != null)
                    {
                        if (pi.IsCanceled == "Y")
                        {
                            li.Add(l.ID);
                        }
                    }
                }

                var list1 = list.Where(p => !li.Contains(p.ID));

                if (list1.Count() == 0)
                {
                    dr["ZT"] = "0";
                    dr["ZYSJ"] = "";
                }
                else
                {
                    dr["ZT"] = "1";
                    dr["ZYSJ"] = list1.First().StartTime.Value.ToString("yyyy-MM-dd HH:mm") + "~" + list1.First().EndTime.Value.ToString("yyyy-MM-dd HH:mm");
                }
            }

            msg.Result = dt;
        }
        #endregion


        #region 添加会议室
        /// <summary>
        /// 添加会议室
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1">客户信息</param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void ADDHYS(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            SZHL_HYGL_ROOM hys = JsonConvert.DeserializeObject<SZHL_HYGL_ROOM>(P1);

            if (string.IsNullOrEmpty(hys.Name))
            {
                msg.ErrorMsg = "会议室名称不能为空!";
            }
            //if (string.IsNullOrEmpty(hys.Location ))
            //{
            //    msg.ErrorMsg =msg.ErrorMsg+ "位置不能为空!";
            //}
            //if (string.IsNullOrEmpty(hys.AdminUser ))
            //{
            //    msg.ErrorMsg = msg.ErrorMsg + "管理员不能为空!";
            //}

            if (string.IsNullOrEmpty(msg.ErrorMsg))
            {
                if (hys.ID == 0)
                {
                    var hys1 = new SZHL_HYGL_ROOMB().GetEntity(p => p.ComId == UserInfo.User.ComId && p.Name == hys.Name);
                    if (hys1 != null)
                    {
                        msg.ErrorMsg = "系统已经存在此会议室名称!";
                    }
                    else
                    {
                        hys.CRDate = DateTime.Now;
                        hys.CRUser = UserInfo.User.UserName;
                        hys.ComId = UserInfo.User.ComId;
                        hys.Status = "1";
                        hys.IsDel = 0;
                        new SZHL_HYGL_ROOMB().Insert(hys);
                        msg.Result = hys;
                    }
                }
                else
                {
                    var hys1 = new SZHL_HYGL_ROOMB().GetEntity(p => p.ComId == UserInfo.User.ComId && p.Name == hys.Name && p.ID != hys.ID);
                    if (hys1 != null)
                    {
                        msg.ErrorMsg = "系统已经存在此会议室名称";
                    }
                    else
                    {
                        new SZHL_HYGL_ROOMB().Update(hys);
                        msg.Result = hys;
                    }
                }

            }
        }
        #endregion

        #region 删除会议室
        /// <summary>
        /// 删除会议室
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void DELHYS(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            int Id = int.Parse(P1);
            int ss = int.Parse(P2);
            SZHL_HYGL_ROOM model = new SZHL_HYGL_ROOMB().GetEntity(d => d.ID == Id && d.ComId == UserInfo.User.ComId);
            model.IsDel = ss;
            model.DelUser = UserInfo.User.UserName;
            model.DelDate = DateTime.Now;
            new SZHL_HYGL_ROOMB().Update(model);

        }
        #endregion

        #region 会议室详细信息
        /// <summary>
        /// 会议室详细信息
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void GETHYSMODEL(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            int Id = int.Parse(P1);
            SZHL_HYGL_ROOM model = new SZHL_HYGL_ROOMB().GetEntity(d => d.ID == Id && d.ComId == UserInfo.User.ComId);

            var st = DateTime.Now;

            var list = new SZHL_HYGLB().GetEntities(p => p.ComId == UserInfo.User.ComId && p.RoomID == model.ID && p.IsDel == 0 && st < p.EndTime).OrderBy(p => p.StartTime);

            List<int> li = new List<int>();

            foreach (var l in list)
            {
                var pi = new Yan_WF_PIB().GetEntity(p => p.ComId == UserInfo.User.ComId && p.ID == l.intProcessStanceid);
                if (pi != null)
                {
                    if (pi.IsCanceled == "Y")
                    {
                        li.Add(l.ID);
                    }
                }
            }

            var list1 = list.Where(p => !li.Contains(p.ID));


            msg.Result = model;
            msg.Result1 = list1;
        }
        #endregion

        #endregion

        #region 会议管理

        #region 会议列表
        /// <summary>
        /// 会议列表
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void GETHYGLLIST(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            string userName = UserInfo.User.UserName;
            string strWhere = " 1=1 and hy.ComId=" + UserInfo.User.ComId;

            string leibie = context.Request["lb"] ?? "";
            if (leibie != "")
            {
                strWhere += string.Format(" And hy.RoomID='{0}' ", leibie);
            }
            string strContent = context.Request["Content"] ?? "";
            strContent = strContent.TrimEnd();
            if (strContent != "")
            {
                strWhere += string.Format(" And ( hy.Title like '%{0}%' )", strContent);
            }
            int DataID = -1;
            int.TryParse(context.Request["ID"] ?? "-1", out DataID);//记录Id
            if (DataID != -1)
            {
                string strIsHasDataQX = new JH_Auth_QY_ModelB().ISHASDATAREADQX("HYGL", DataID, UserInfo);
                if (strIsHasDataQX == "Y")
                {
                    strWhere += string.Format(" And hy.ID = '{0}'", DataID);
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

                #region no use
                //                switch (P1)
                //                {
                //                    case "0":
                //                        string colNme1 = @"ycgl.*,case when ycgl.StartTime>getdate() then '即将开始' when ycgl.StartTime<=getdate() and ycgl.EndTime>=getdate() then '正在进行' 
                //                                            when ycgl.EndTime<getdate() then '已结束' end as HLStatus,car.Name ,case WHEN wfpi.isComplete is null and wfpi.IsCanceled is null  THEN '正在审批' 
                //                                            when wfpi.isComplete='Y' then '已审批'  WHEN wfpi.IsCanceled='Y' then '已退回' END as StateName";
                //                        //strWhere += " And cc.CRUser ='" + userName + "'";
                //                        dt = new SZHL_CCXJB().GetDataPager("SZHL_HYGL ycgl inner join SZHL_HYGL_ROOM  car on ycgl.RoomID=car.ID  inner join  Yan_WF_PI wfpi  on ycgl.intProcessStanceid=wfpi.ID", colNme1, 8, page, " ycgl.CRDate desc", strWhere, ref total);
                //                        break;
                //                    case "1":
                //                        string colNme = @"ycgl.*,case when ycgl.StartTime>getdate() then '即将开始' when ycgl.StartTime<=getdate() and ycgl.EndTime>=getdate() then '正在进行' 
                //                                            when ycgl.EndTime<getdate() then '已结束' end as HLStatus,car.Name ,case WHEN wfpi.isComplete is null and wfpi.IsCanceled is null  THEN '正在审批' 
                //                                            when wfpi.isComplete='Y' then '已审批'  WHEN wfpi.IsCanceled='Y' then '已退回' END as StateName";
                //                        strWhere += " And ycgl.CRUser ='" + userName + "'";
                //                        dt = new SZHL_CCXJB().GetDataPager("SZHL_HYGL ycgl inner join SZHL_HYGL_ROOM  car on ycgl.RoomID=car.ID  inner join  Yan_WF_PI wfpi  on ycgl.intProcessStanceid=wfpi.ID", colNme, 8, page, " ycgl.CRDate desc", strWhere, ref total);

                //                        break;
                //                    case "2":
                //                        string colNme2 = @"ycgl.*,case when ycgl.StartTime>getdate() then '即将开始' when ycgl.StartTime<=getdate() and ycgl.EndTime>=getdate() then '正在进行' 
                //                                            when ycgl.EndTime<getdate() then '已结束' end as HLStatus,car.Name ,'已审批' StateName";
                //                        strWhere += string.Format(" And (','+ycgl.CYUser+','  like '%,{0},%' or ','+ycgl.JLUser+','  like '%,{0},%' or ','+ycgl.ZCUser+','  like '%,{0},%' or ','+ycgl.SXUser+','  like '%,{0},%'  ) and wfpi.isComplete='Y'", userName);
                //                        dt = new SZHL_CCXJB().GetDataPager("SZHL_HYGL ycgl inner join SZHL_HYGL_ROOM  car on ycgl.RoomID=car.ID  inner join  Yan_WF_PI wfpi  on ycgl.intProcessStanceid=wfpi.ID", colNme2, 8, page, " ycgl.StartTime desc", strWhere, ref total);

                //                        break;
                //                    case "3":
                //                        List<string> intProD = new Yan_WF_PIB().GetDSH(UserInfo.User).Select(d => d.PIID.ToString()).ToList();
                //                        if (intProD.Count > 0)
                //                        {
                //                            string tableNameD = string.Format(@" SZHL_HYGL ycgl inner join SZHL_HYGL_ROOM  car on ycgl.RoomID=car.ID");
                //                            string tableColumnD = "ycgl.* ,car.Name , '正在审批' StateName,case when ycgl.StartTime>getdate() then '即将开始' when ycgl.StartTime<=getdate() and ycgl.EndTime>=getdate() then '正在进行' when ycgl.EndTime<getdate() then '已结束' end as HLStatus";
                //                            strWhere += " And ycgl.intProcessStanceid in (" + (intProD.ListTOString(',') == "" ? "0" : intProD.ListTOString(',')) + ")";
                //                            dt = new SZHL_CCXJB().GetDataPager(tableNameD, tableColumnD, 8, page, " ycgl.CRDate desc", strWhere, ref total);
                //                        }
                //                        break;
                //                    case "4":
                //                        List<Yan_WF_TI> ListData = new Yan_WF_TIB().GetEntities("TaskUserID ='" + UserInfo.User.UserName + "' AND EndTime IS NOT NULL AND TaskUserView!='发起表单'").ToList();
                //                        List<string> intPro = ListData.Select(d => d.PIID.ToString()).ToList();
                //                        string tableName = string.Format(@" SZHL_HYGL ycgl inner join SZHL_HYGL_ROOM  car on ycgl.RoomID=car.ID inner join  Yan_WF_PI wfpi  on ycgl.intProcessStanceid=wfpi.ID");
                //                        string tableColumn = "ycgl.* ,car.Name ,  case when wfpi.IsCanceled is null then '已审批'   WHEN wfpi.IsCanceled='Y' then '已退回' END StateName,case when ycgl.StartTime>getdate() then '即将开始' when ycgl.StartTime<=getdate() and ycgl.EndTime>=getdate() then '正在进行' when ycgl.EndTime<getdate() then '已结束' end as HLStatus ";
                //                        strWhere += "  And ycgl.intProcessStanceid in (" + (intPro.ListTOString(',') == "" ? "0" : intPro.ListTOString(',')) + ")";

                //                        dt = new SZHL_CCXJB().GetDataPager(tableName, tableColumn, 8, page, " ycgl.CRDate desc", strWhere, ref total);
                //                        break;
                //                } 
                #endregion

                switch (P1)
                {
                    case "0": //手机单条数据
                        {
                            //设置usercenter已读
                            new JH_Auth_User_CenterB().ReadMsg(UserInfo, DataID, "HYGL");
                        }
                        break;
                    case "1": //创建的
                        {
                            strWhere += " And hy.CRUser ='" + userName + "'";
                        }
                        break;
                    case "2": //参与的
                        {
                            strWhere += string.Format(" And (','+hy.CYUser+','  like '%,{0},%' or ','+hy.JLUser+','  like '%,{0},%' or ','+hy.ZCUser+','  like '%,{0},%' or ','+hy.SXUser+','  like '%,{0},%'  ) and ( dbo.fn_PDStatus(hy.intProcessStanceid)='已审批' or dbo.fn_PDStatus(hy.intProcessStanceid)='-1')", userName);
                        }
                        break;
                    case "3": //待审核
                        {
                            var intProD = new Yan_WF_PIB().GetDSH(UserInfo.User).Select(d => d.PIID.ToString()).ToList();
                            if (intProD.Count > 0)
                            {
                                strWhere += " And hy.intProcessStanceid in (" + (intProD.ListTOString(',') == "" ? "0" : intProD.ListTOString(',')) + ")";
                            }
                            else
                            {
                                strWhere += " And 1=0";
                            }
                        }
                        break;
                    case "4":  //已审核
                        {
                            var intProD = new Yan_WF_PIB().GetYSH(UserInfo.User).Select(d => d.PIID.ToString()).ToList();
                            if (intProD.Count > 0)
                            {
                                strWhere += " And hy.intProcessStanceid in (" + (intProD.ListTOString(',') == "" ? "0" : intProD.ListTOString(',')) + ")";

                            }
                            else
                            {
                                strWhere += " And 1=0";
                            }
                        }
                        break;
                }

                dt = new SZHL_CCXJB().GetDataPager("SZHL_HYGL hy left join SZHL_HYGL_ROOM hys on hy.RoomID=hys.ID", "hy.*,hys.Name ,dbo.fn_PDStatus(hy.intProcessStanceid) AS StateName,case when hy.StartTime>getdate() then '即将开始' when hy.StartTime<=getdate() and hy.EndTime>=getdate() then '正在进行' when hy.EndTime<getdate() then '已结束' end as HLStatus ", pagecount, page, " hy.CRDate desc", strWhere, ref total);

                if (dt.Rows.Count > 0)
                {
                    dt.Columns.Add("RYStatus", Type.GetType("System.String"));
                    dt.Columns.Add("FileList", Type.GetType("System.Object"));
                    dt.Columns.Add("PLList", Type.GetType("System.Object"));
                    foreach (DataRow dr in dt.Rows)
                    {
                        if (dr["Files"] != null && dr["Files"].ToString() != "")
                        {
                            dr["FileList"] = new FT_FileB().GetEntities(" ID in (" + dr["Files"].ToString() + ")");
                        }
                        string strid = dr["ID"].ToString();
                        var hysat = new JH_Auth_TLB().GetEntities(p => p.ComId == UserInfo.User.ComId && p.MSGType == "HYGL" && p.MSGTLYID == strid && p.CRUser == UserInfo.User.UserName && p.MsgISShow != null).OrderByDescending(p => p.CRDate).ToList();
                        if (hysat.Count() > 0)
                        {
                            string strs = string.Empty;
                            foreach (var l in hysat)
                            {
                                if (string.IsNullOrEmpty(strs))
                                {
                                    strs = l.MsgISShow;
                                }
                                else
                                {
                                    strs = strs + "," + l.MsgISShow;
                                }
                            }
                            dr["RYStatus"] = strs;
                        }

                        dr["PLList"] = new JH_Auth_TLB().GetEntities(p => p.ComId == UserInfo.User.ComId && p.MSGType == "HYGL" && p.MSGTLYID == strid);
                    }
                }
                msg.Result = dt;
                msg.Result1 = total;
            }
        }
        #endregion

        #region 添加会议
        /// <summary>
        /// 添加会议
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void ADDHYGL(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            SZHL_HYGL HY = JsonConvert.DeserializeObject<SZHL_HYGL>(P1);

            if (HY == null)
            {
                msg.ErrorMsg = "添加失败";
                return;
            }
            if (string.IsNullOrWhiteSpace(HY.Title))
            {
                msg.ErrorMsg = "会议主题不能为空";
                return;
            }
            if (string.IsNullOrWhiteSpace(HY.CYUser))
            {
                msg.ErrorMsg = "参会人不能为空";
                return;
            }
            if (HY.StartTime >= HY.EndTime)
            {
                msg.ErrorMsg = "开始时间必须大于结束时间";
                return;
            }
            if (HY.ID == 0)
            {
                if (HY.RoomID == 0 || HY.RoomID == null)
                {
                    msg.ErrorMsg = "请选择会议室！";
                }
                else
                {
                    var list = new SZHL_HYGLB().GetEntities(p => p.ComId == UserInfo.User.ComId && p.RoomID == HY.RoomID && p.IsDel == 0 && ((HY.StartTime >= p.StartTime && HY.StartTime < p.EndTime) || (HY.EndTime > p.StartTime && HY.EndTime <= p.EndTime) || (HY.EndTime > p.StartTime && HY.StartTime <= p.StartTime) || (HY.EndTime >= p.EndTime && HY.StartTime < p.EndTime))).OrderBy(p => p.StartTime);
                    List<int> li = new List<int>();

                    foreach (var l in list)
                    {

                        var pi = new Yan_WF_PIB().GetEntity(p => p.ComId == UserInfo.User.ComId && p.ID == l.intProcessStanceid);
                        if (pi != null && pi.IsCanceled == "Y")
                        {
                            li.Add(l.ID);
                        }
                    }

                    var list1 = list.Where(p => !li.Contains(p.ID));

                    if (list1.Count() == 0)
                    {
                        if (P2 != "") // 处理微信上传的图片
                        {
                            string fids = new FT_FileB().ProcessWxIMG(P2, "HYGL", UserInfo);

                            if (!string.IsNullOrEmpty(HY.Files))
                            {
                                HY.Files += "," + fids;
                            }
                            else
                            {
                                HY.Files = fids;
                            }
                        }

                        HY.CRDate = DateTime.Now;
                        HY.CRUser = UserInfo.User.UserName;
                        HY.ComId = UserInfo.User.ComId;
                        HY.IsDel = 0;
                        new SZHL_HYGLB().Insert(HY);
                    }
                    else
                    {
                        msg.ErrorMsg = "选择的时间段中，此会议室已经被占用！";
                    }
                    //可用会议室需要优化，根据时间段来判断会议室是否可用
                    //List<SZHL_HYGL_ROOM> car1 = new SZHL_HYGL_ROOMB().GetEntities(d => d.ComId == UserInfo.User.ComId && d.ID == HY.RoomID && d.Status == "1" && d.IsDel == 0).ToList();
                }
            }
            else
            {
                new SZHL_HYGLB().Update(HY);
            }
            msg.Result = HY;
        }
        #endregion

        #region 会议详细信息
        /// <summary>
        /// 会议详细信息
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void GETHYGLMODEL(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            int Id = int.Parse(P1);
            string strWhere = " 1=1 and hy.ComId=" + UserInfo.User.ComId + " and hy.ID=" + Id;
            string colNme = @"hy.*,hys.Name ,dbo.fn_PDStatus(hy.intProcessStanceid) AS StateName,case when hy.StartTime>getdate() then '即将开始' when hy.StartTime<=getdate() and hy.EndTime>=getdate() then '正在进行' when hy.EndTime<getdate() then '已结束' end as HLStatus ";
            string tableName = string.Format(@" SZHL_HYGL hy left join SZHL_HYGL_ROOM hys on hy.RoomID=hys.ID");

            string strSql = string.Format("Select {0}  From {1} where {2} order by hy.CRDate desc", colNme, tableName, strWhere);
            DataTable dt = new SZHL_HYGLB().GetDTByCommand(strSql);


            msg.Result = dt;
            if (dt.Rows.Count > 0)
            {
                if (dt.Rows[0]["Files"] != null)
                {
                    string strfiles = dt.Rows[0]["Files"].ToString();
                    if (!string.IsNullOrEmpty(strfiles))
                    {
                        msg.Result1 = new FT_FileB().GetEntities(" ID in (" + strfiles + ")");
                    }
                }
                //msg.Result2 = new SZHL_HYGL_QRB().GetEntities(p => p.Status == "0" && p.HYID == model.ID && p.IsDel == 0);
                //msg.Result3 = new SZHL_HYGL_QDB().GetEntities(p => p.IsDel == 0 && p.HYID == model.ID);
                var strid = dt.Rows[0]["ID"].ToString();


                //打开会议即已阅
                //Msg_Result msg2 = new Msg_Result();
                //UPDATEHYQK(context, msg2, strid, "1", UserInfo);

                msg.Result2 = new JH_Auth_TLB().GetEntities(p => p.ComId == UserInfo.User.ComId && p.MSGType == "HYGL" && p.MSGTLYID == strid);

                var hysat = new JH_Auth_TLB().GetEntities(p => p.ComId == UserInfo.User.ComId && p.MSGType == "HYGL" && p.MSGTLYID == strid && p.CRUser == UserInfo.User.UserName && p.MsgISShow != null).OrderByDescending(p => p.CRDate).ToList();
                if (hysat.Count() > 0)
                {
                    string strs = string.Empty;
                    foreach (var l in hysat)
                    {
                        if (string.IsNullOrEmpty(strs))
                        {
                            strs = l.MsgISShow;
                        }
                        else
                        {
                            strs = strs + "," + l.MsgISShow;
                        }
                    }
                    msg.Result3 = strs;
                }
                if (dt.Rows[0]["JLFiles"] != null)
                {
                    string strjlfiles = dt.Rows[0]["JLFiles"].ToString();
                    if (!string.IsNullOrEmpty(strjlfiles))
                    {
                        msg.Result4 = new FT_FileB().GetEntities(" ID in (" + strjlfiles + ")");
                    }
                }

                //更新消息为已读状态
                new JH_Auth_User_CenterB().ReadMsg(UserInfo, Int32.Parse(strid), "HYGL");
            }
        }
        #endregion

        #region 更新会议情况
        /// <summary>
        /// 更新会议情况
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void UPDATEHYQK(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            int Id = int.Parse(P1);

            var hy = new SZHL_HYGLB().GetEntity(p => p.ID == Id);

            if ((hy.FQUser != null && hy.FQUser.Split(',').Contains(UserInfo.User.UserName)) || (hy.CYUser != null && hy.CYUser.Split(',').Contains(UserInfo.User.UserName)) || (hy.JLUser != null && hy.JLUser.Split(',').Contains(UserInfo.User.UserName)) || (hy.ZCUser != null && hy.ZCUser.Split(',').Contains(UserInfo.User.UserName)) || (hy.SXUser != null && hy.SXUser.Split(',').Contains(UserInfo.User.UserName)))
            {
                var tl = new JH_Auth_TLB().GetEntity(p => p.ComId == UserInfo.User.ComId && p.MSGTLYID == P1 && p.CRUser == UserInfo.User.UserName && p.MsgISShow == P2 && p.MSGType == "HYGL");
                if (tl == null)
                {
                    JH_Auth_TL jat = new JH_Auth_TL();
                    jat.ComId = UserInfo.User.ComId;
                    jat.MSGType = "HYGL";
                    jat.MSGTLYID = P1;
                    jat.MsgISShow = P2;

                    //已阅，确认，签到，请假就1,2,3,4
                    if (P2 == "1")
                    {
                        jat.MSGContent = "已阅";
                    }
                    else if (P2 == "2")
                    {
                        jat.MSGContent = "确认参加";
                    }
                    else if (P2 == "3")
                    {
                        jat.MSGContent = "签到";
                    }
                    else if (P2 == "4")
                    {
                        jat.MSGContent = "请假";
                    }
                    var qjsy = context.Request["QJSY"] ?? "";
                    if (qjsy != "") { jat.Remark = qjsy; }
                    jat.CRUser = UserInfo.User.UserName;
                    jat.CRUserName = UserInfo.User.UserRealName;
                    jat.CRDate = DateTime.Now;

                    new JH_Auth_TLB().Insert(jat);

                    msg.Result = jat;
                }
            }
        }
        #endregion



        /// <summary>
        /// 取消会议预约
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void DELHY(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            int Id = int.Parse(P1);
            new SZHL_HYGLB().Delete(d => d.ID == Id && d.ComId == UserInfo.User.ComId);
          

        }


        #region 更新会议记录
        /// <summary>
        /// 更新会议记录
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void UPDATEHYJL(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            int Id = int.Parse(P1);
            string jlfiles = context.Request.Params["JLFiles"];
            var hy = new SZHL_HYGLB().GetEntity(p => p.ComId == UserInfo.User.ComId && p.ID == Id);

            if (hy != null)
            {
                hy.HYJL = P2;
                hy.JLFiles = jlfiles;
                new SZHL_HYGLB().Update(hy);
            }

        }
        #endregion

        #region 会议日历视图
        /// <summary>
        /// 会议日历视图
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void GETHYGLVIEW(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            string strSql = string.Format("SELECT  hygl.ID,hygl.intProcessStanceid,hygl.FQUser,'['+room.Name+']'+hygl.Title+'  '+CONVERT(VARCHAR(5),hygl.StartTime,8)+'~'+CONVERT(VARCHAR(5),hygl.EndTime,8) title,hygl.StartTime start,hygl.EndTime [end],CASE when dbo.fn_PDStatus(hygl.intProcessStanceid)='已审批' then 1 when dbo.fn_PDStatus(hygl.intProcessStanceid)='-1' then 1  when dbo.fn_PDStatus(hygl.intProcessStanceid)='正在审批' then 0 end SHStatus from SZHL_HYGL hygl INNER join SZHL_HYGL_ROOM  room on hygl.RoomID=room.ID  where ( dbo.fn_PDStatus(hygl.intProcessStanceid)='已审批' or dbo.fn_PDStatus(hygl.intProcessStanceid)='正在审批' or dbo.fn_PDStatus(hygl.intProcessStanceid)='-1' ) and hygl.ComId=" + UserInfo.User.ComId);

            if (P1 != "0")
            {
                strSql += string.Format(" and hygl.RoomID={0} ", P1);
            }
            msg.Result = new SZHL_YCGLB().GetDTByCommand(strSql);
        }
        #endregion

        #region 获取会议人员情况
        /// <summary>
        /// 获取会议人员情况
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void GETRYLIST_PAGE(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            int page = 0;
            int.TryParse(context.Request["p"] ?? "1", out page);
            page = page == 0 ? 1 : page;


            int total = 0;

            DataTable dt = new SZHL_YCGLB().GetDataPager("JH_Auth_TL", "*", 8, page, " CRDate desc", "MSGType='HYGL' and MSGTLYID='" + P1 + "' and MsgISShow='" + P2 + "' and  ComId=" + UserInfo.User.ComId, ref total);
            msg.Result = dt;
            msg.Result1 = total;
        }
        #endregion

        #endregion

        #region 会议信息管理

        #region 会议管理发送消息
        /// <summary>
        /// 会议管理发送消息
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void SENDWXMSG(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            var tx = JsonConvert.DeserializeObject<SZHL_TXSX>(P1);
            int msgid = Int32.Parse(tx.MsgID);
            UserInfo = new JH_Auth_UserB().GetUserInfo(tx.ComId.Value, tx.CRUser);
            var model = new SZHL_HYGLB().GetEntity(p => p.ID == msgid && p.ComId == UserInfo.User.ComId);
            if (model != null)
            {
                var user = new JH_Auth_UserB().GetUserByUserName(model.ComId.Value, model.CRUser);
                var rm = new SZHL_HYGL_ROOMB().GetEntity(p => p.ID == model.RoomID && p.ComId == UserInfo.User.ComId);
                Article ar0 = new Article();
                ar0.Title = "会议通知";
                ar0.Description = "发起人：" + user.UserRealName + "\r\n您有新的会议[" + model.Title + "],会议室[" + rm.Name + "],请尽快查看吧";
                ar0.Url = model.ID.ToString();

                List<Article> al = new List<Article>();
                al.Add(ar0);

                string jsr = string.Empty;
                if (!string.IsNullOrEmpty(model.FQUser))
                {
                    jsr = model.FQUser;
                }
                if (!string.IsNullOrEmpty(model.CYUser))
                {
                    if (!string.IsNullOrEmpty(jsr))
                    {
                        jsr = jsr + "," + model.CYUser;
                    }
                    else
                    {
                        jsr = model.CYUser;
                    }
                }
                if (!string.IsNullOrEmpty(model.ZCUser))
                {
                    if (!string.IsNullOrEmpty(jsr))
                    {
                        jsr = jsr + "," + model.ZCUser;
                    }
                    else
                    {
                        jsr = model.ZCUser;
                    }
                }
                if (!string.IsNullOrEmpty(model.JLUser))
                {
                    if (!string.IsNullOrEmpty(jsr))
                    {
                        jsr = jsr + "," + model.JLUser;
                    }
                    else
                    {
                        jsr = model.JLUser;
                    }
                }
                if (!string.IsNullOrEmpty(model.SXUser))
                {
                    if (!string.IsNullOrEmpty(jsr))
                    {
                        jsr = jsr + "," + model.SXUser;
                    }
                    else
                    {
                        jsr = model.SXUser;
                    }
                }

                UserInfo = new JH_Auth_UserB().GetUserInfo(model.ComId.Value, model.CRUser);
                if (!string.IsNullOrEmpty(jsr))
                {
                    //发送消息
                    string content = user.UserRealName + "邀请您参加会议：" + model.Title;
                    new JH_Auth_User_CenterB().SendMsg(UserInfo, "HYGL", content, model.ID.ToString(), jsr, "B", model.intProcessStanceid, tx.ISCS);

                    WXHelp wx = new WXHelp(UserInfo.QYinfo);
                    wx.SendTH(al, "HYGL", "A", jsr);
                }

                if (model.TXSJ > 0)
                {
                    DateTime dt = model.StartTime.ToDateTime().AddMinutes(-model.TXSJ.Value);
                    SZHL_TXSX tx1 = new SZHL_TXSX();
                    tx1.ComId = model.ComId;
                    tx1.APIName = "HYGL";
                    tx1.MsgID = model.ID.ToString();
                    tx1.FunName = "SENDWXMSG_TX";
                    tx1.Date = dt.ToString("yyyy-MM-dd HH:mm:ss");
                    tx1.CRUser = model.CRUser;
                    tx1.CRDate = DateTime.Now;
                    TXSX.TXSXAPI.AddALERT(tx1); //时间为发送时间

                    //TXSX.TXSXAPI.AddALERT(UserInfo.User.ComId.Value, "HYGL", "SENDWXMSG_TX", model.ID.ToString(), dt); //时间为发送时间
                }
            }
        }
        #endregion

        #region 会议发送提醒信息
        /// <summary>
        /// 会议发送提醒信息
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void SENDWXMSG_TX(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            var tx = JsonConvert.DeserializeObject<SZHL_TXSX>(P1);
            int msgid = Int32.Parse(tx.MsgID);

            UserInfo = new JH_Auth_UserB().GetUserInfo(tx.ComId.Value, tx.CRUser);

            var model = new SZHL_HYGLB().GetEntity(p => p.ID == msgid && p.ComId == UserInfo.User.ComId);
            if (model != null)
            {
                var rm = new SZHL_HYGL_ROOMB().GetEntity(p => p.ID == model.RoomID && p.ComId == UserInfo.User.ComId);
                Article ar0 = new Article();
                ar0.Title = "会议提醒";
                ar0.Description = "发起人：" + UserInfo.User.UserRealName + "\r\n您有会议[" + model.Title + "],会议室[" + rm.Name + "],将于" + model.StartTime.Value.ToString("yyyy-MM-dd HH:mm") + "开始，请及时参加";
                ar0.Url = model.ID.ToString();

                List<Article> al = new List<Article>();
                al.Add(ar0);

                string jsr = string.Empty;
                if (!string.IsNullOrEmpty(model.FQUser))
                {
                    jsr = model.FQUser;
                }
                if (!string.IsNullOrEmpty(model.CYUser))
                {
                    if (!string.IsNullOrEmpty(jsr))
                    {
                        jsr = jsr + "," + model.CYUser;
                    }
                    else
                    {
                        jsr = model.CYUser;
                    }
                }
                if (!string.IsNullOrEmpty(model.ZCUser))
                {
                    if (!string.IsNullOrEmpty(jsr))
                    {
                        jsr = jsr + "," + model.ZCUser;
                    }
                    else
                    {
                        jsr = model.ZCUser;
                    }
                }
                if (!string.IsNullOrEmpty(model.JLUser))
                {
                    if (!string.IsNullOrEmpty(jsr))
                    {
                        jsr = jsr + "," + model.JLUser;
                    }
                    else
                    {
                        jsr = model.JLUser;
                    }
                }
                if (!string.IsNullOrEmpty(model.SXUser))
                {
                    if (!string.IsNullOrEmpty(jsr))
                    {
                        jsr = jsr + "," + model.SXUser;
                    }
                    else
                    {
                        jsr = model.SXUser;
                    }
                }

                ////发送消息
                string content = ar0.Description;
                new JH_Auth_User_CenterB().SendMsg(UserInfo, "HYGL", content, model.ID.ToString(), jsr, "B", model.intProcessStanceid, tx.ISCS);
                if (!string.IsNullOrEmpty(jsr))
                {
                    WXHelp wx = new WXHelp(UserInfo.QYinfo);
                    wx.SendTH(al, "HYGL", "A", jsr);
                }
            }
        }
        #endregion

        #region 添加会议消息
        /// <summary>
        /// 添加会议消息
        /// </summary>
        /// <param name="P1"></param>
        public void COMPLETEWFHYGL(string P1)
        {
            int msgid = Int32.Parse(P1);

            var model = new SZHL_HYGLB().GetEntity(p => p.ID == msgid);
            if (model != null)
            {
                SZHL_TXSX tx1 = new SZHL_TXSX();
                tx1.ComId = model.ComId;
                tx1.APIName = "HYGL";
                tx1.MsgID = model.ID.ToString();
                tx1.FunName = "SENDWXMSG";
                tx1.Date = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
                tx1.CRUser = model.CRUser;
                tx1.CRDate = DateTime.Now;
                TXSX.TXSXAPI.AddALERT(tx1); //时间为发送时间
            }
        }


        public void SENDMSG(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {

           
            int Id = int.Parse(P1);
            var model = new SZHL_HYGLB().GetEntity(d => d.ID == Id && d.ComId == UserInfo.User.ComId);
            var rm = new SZHL_HYGL_ROOMB().GetEntity(p => p.ID == model.RoomID && p.ComId == UserInfo.User.ComId);

            Article ar0 = new Article();
            ar0.Title = "会议通知";
            ar0.Description = "发起人：" + new JH_Auth_UserB().GetUserRealName(UserInfo.User.ComId.Value, model.CRUser) + "\r\n您有新的会议[" + model.Title + "],会议室[" + rm.Name + "],请尽快查看吧";
            ar0.Url = model.ID.ToString();
            List<Article> al = new List<Article>();
            al.Add(ar0);
            //new JH_Auth_User_CenterB().SendMsg(UserInfo, "HYGL", strContent, model.ID.ToString(), model.JSR, "B", model.intProcessStanceid);

            WXHelp wx = new WXHelp(UserInfo.QYinfo);
            wx.SendTH(al, "HYGL", "A", model.CYUser);

        }


        #endregion

        #endregion
    }
}