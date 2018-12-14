using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;
using FastReflectionLib;
using QJY.Data;
using Newtonsoft.Json;
using System.Data;
using QJY.Common;
using Senparc.Weixin.QY.Entities;

namespace QJY.API
{
    public class YCGLManage : IWsService
    {
        public void ProcessRequest(HttpContext context, ref Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            MethodInfo methodInfo = typeof(YCGLManage).GetMethod(msg.Action.ToUpper());
            YCGLManage model = new YCGLManage();
            methodInfo.FastInvoke(model, new object[] { context, msg, P1, P2, UserInfo });
        }
        #region 车辆管理

        #region 获取车辆列表
        /// <summary>
        /// 获取车辆列表
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void GETCLLIST_PAGE(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            string strWhere = string.Format(" car.ComId=" + UserInfo.User.ComId);
            if (P1 != "") //车牌号
            {
                strWhere += string.Format("and car.CarNum like '%{0}%'", P1);
            }
            if (P2 != "")//车辆类型
            {
                strWhere += string.Format(" And car.CarType='{0}'", P2); ;
            }
            int recordCount = 0;
            int page = 0;
            int pagecount = 8;
            int.TryParse(context.Request["p"] ?? "1", out page);
            int.TryParse(context.Request["pagecount"] ?? "8", out pagecount);//页数
            DataTable dt = new SZHL_XXFBB().GetDataPager("SZHL_YCGL_CAR car  left join  JH_Auth_ZiDian zd on car.CarType=zd.ID and zd.Class=5 ", "car.*,zd.TypeName", pagecount, page, "car.CRDate desc", strWhere, ref recordCount);
            msg.Result = dt;
            msg.Result1 = recordCount;
        }
        #endregion

        #region 添加车辆信息
        /// <summary>
        /// 添加车辆信息
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void ADDCLINFO(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            SZHL_YCGL_CAR carInfo = JsonConvert.DeserializeObject<SZHL_YCGL_CAR>(P1);
            if (string.IsNullOrEmpty(carInfo.CarType))
            {
                msg.ErrorMsg = "请选择车类型";
                return;
            }
            if (string.IsNullOrEmpty(carInfo.CarNum))
            {
                msg.ErrorMsg = "请填写车牌号";
                return;
            }
            if (carInfo.ID == 0)
            {
                SZHL_YCGL_CAR carInfo1 = new SZHL_YCGL_CARB().GetEntity(d => d.CarNum == carInfo.CarNum && d.ComId == UserInfo.User.ComId);
                if (carInfo1 != null)
                {
                    msg.ErrorMsg = "已有此车牌号的车辆";
                }
                else
                {
                    carInfo.CRDate = DateTime.Now;
                    carInfo.CRUser = UserInfo.User.UserName;
                    carInfo.ComId = UserInfo.User.ComId;
                    carInfo.IsDel = 0;
                    new SZHL_YCGL_CARB().Insert(carInfo);
                }
            }
            else
            {
                new SZHL_YCGL_CARB().Update(carInfo);
            }
        }
        #endregion

        #region 获取车辆信息
        /// <summary>
        /// 获取车辆信息
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void GETCLINFO(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            int Id = int.Parse(P1);
            var car = new SZHL_YCGL_CARB().GetEntity(d => d.ID == Id && d.ComId == UserInfo.User.ComId);
            msg.Result = car;
            //string strSql = string.Format("SELECT  ycgl.*  from SZHL_YCGL  ycgl inner join SZHL_YCGL_CAR car on ycgl.CarID=car.ID inner join  Yan_WF_PI wfpi on ycgl.intProcessStanceid=wfpi.ID where  wfpi.isComplete='Y' and ycgl.ComId={0} and ycgl.CarID={1}", UserInfo.User.ComId, Id);
            //msg.Result1 = new SZHL_YCGL_CARB().GetDTByCommand(strSql);
            int tid = Int32.Parse(car.CarType);
            msg.Result3 = new JH_Auth_ZiDianB().GetEntity(p => p.ID == tid);
            #region 微信端
            var st = DateTime.Now;

            var list = new SZHL_YCGLB().GetEntities(p => p.CarID == Id && p.IsDel == 0 && st < p.EndTime).OrderBy(p => p.StartTime);

            List<int> li = new List<int>();

            foreach (var l in list)
            {
                var pi = new Yan_WF_PIB().GetEntity(p => p.ID == l.intProcessStanceid);
                if (pi != null && pi.IsCanceled == "Y")
                {
                    li.Add(l.ID);
                }
            }

            var list1 = list.Where(p => !li.Contains(p.ID));

            msg.Result2 = list1;
            #endregion
            if (!string.IsNullOrEmpty(car.Files))
            {
                msg.Result4 = new FT_FileB().GetEntities(" ID in (" + car.Files + ")");
            }
        }
        #endregion

        #region 更新车辆状态
        /// <summary>
        /// 更新车辆状态
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void MODIFYCLSTATUS(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            int Id = int.Parse(P1);
            string strSql = string.Format(" update SZHL_YCGL_CAR set status={0}  where Id={1} and ComId={2}", P2, P1, UserInfo.User.ComId);
            new SZHL_YCGL_CARB().ExsSql(strSql);
        }
        #endregion

        #region 获取所有车辆
        /// <summary>
        /// 获取所有车辆
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void GETALLCLLIST(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            string strSql = string.Format("SELECT car.*,zd.TypeName from SZHL_YCGL_CAR car left join  JH_Auth_ZiDian zd on car.CarType=zd.ID and Class=5 Where car.ComId={0} and car.Status=0 and car.CarBrand!=''", UserInfo.User.ComId);
            msg.Result = new SZHL_YCGL_CARB().GetDTByCommand(strSql);
        }
        #endregion

        #region 删除车辆
        public void DELCL(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            int id = int.Parse(P1);
            new SZHL_YCGL_CARB().Delete(d => d.ID == id);
        }
        #endregion

        #endregion

        #region 用车管理

        #region 用车列表
        /// <summary>
        /// 用车列表
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void GETYCGLLIST(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            string userName = UserInfo.User.UserName;
            string strWhere = " 1=1 and yc.ComId=" + UserInfo.User.ComId;

            string leibie = context.Request["lb"] ?? "";
            if (leibie != "")
            {
                strWhere += string.Format(" And yc.CarID='{0}' ", leibie);
            }
            string strContent = context.Request["Content"] ?? "";
            strContent = strContent.TrimEnd();
            if (strContent != "")
            {
                strWhere += string.Format(" And ( yc.Remark like '%{0}%' or yc.StartAddress like '%{0}%' or yc.EndAddress like '%{0}%')", strContent);
            }
            int DataID = -1;
            int.TryParse(context.Request["ID"] ?? "-1", out DataID);//记录Id
            if (DataID != -1)
            {
                string strIsHasDataQX = new JH_Auth_QY_ModelB().ISHASDATAREADQX("YCGL", DataID, UserInfo);
                if (strIsHasDataQX == "Y")
                {
                    strWhere += string.Format(" And yc.ID = '{0}'", DataID);
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
                //                        string colNme1 = @"ycgl.*,car.CarBrand,car.CarType,car.CarNum ,    case WHEN wfpi.isComplete is null and wfpi.IsCanceled is null  THEN '正在审批' 
                //                                            when wfpi.isComplete='Y' then '已审批'  WHEN wfpi.IsCanceled='Y' then '已退回' END StateName";
                //                        //strWhere += " And cc.CRUser ='" + userName + "'";
                //                        dt = new SZHL_CCXJB().GetDataPager("SZHL_YCGL ycgl left outer join SZHL_YCGL_CAR  car on ycgl.CarID=car.ID  inner join  Yan_WF_PI wfpi  on ycgl.intProcessStanceid=wfpi.ID", colNme1, 8, page, " ycgl.CRDate desc", strWhere, ref total);
                //                        break;
                //                    case "1":
                //                        string colNme = @"ycgl.*,car.CarBrand,car.CarType,car.CarNum ,    case WHEN wfpi.isComplete is null and wfpi.IsCanceled is null  THEN '正在审批' 
                //                                            when wfpi.isComplete='Y' then '已审批'  WHEN wfpi.IsCanceled='Y' then '已退回' END StateName";
                //                        strWhere += " And ycgl.CRUser ='" + userName + "'";
                //                        dt = new SZHL_CCXJB().GetDataPager("SZHL_YCGL ycgl left outer join SZHL_YCGL_CAR  car on ycgl.CarID=car.ID  inner join  Yan_WF_PI wfpi  on ycgl.intProcessStanceid=wfpi.ID", colNme, 8, page, " ycgl.CRDate desc", strWhere, ref total);

                //                        break;
                //                    case "2":
                //                        List<string> intProD = new Yan_WF_PIB().GetDSH(UserInfo.User).Select(d => d.PIID.ToString()).ToList();
                //                        if (intProD.Count > 0)
                //                        {
                //                            string tableNameD = string.Format(@" SZHL_YCGL ycgl left outer join SZHL_YCGL_CAR  car on ycgl.CarID=car.ID");
                //                            string tableColumnD = "ycgl.* ,car.CarBrand,car.CarType,car.CarNum , '正在审批' StateName";
                //                            strWhere += " And ycgl.intProcessStanceid in (" + (intProD.ListTOString(',') == "" ? "0" : intProD.ListTOString(',')) + ")";
                //                            //string strSql = string.Format("Select {0}  From {1} where {2} order by cc.CRDate desc", tableColumnD, tableNameD, strWhere);
                //                            dt = new SZHL_CCXJB().GetDataPager(tableNameD, tableColumnD, 8, page, " ycgl.CRDate desc", strWhere, ref total);
                //                        }
                //                        break;
                //                    case "3":
                //                        List<Yan_WF_TI> ListData = new Yan_WF_TIB().GetEntities("TaskUserID ='" + UserInfo.User.UserName + "' AND EndTime IS NOT NULL AND TaskUserView!='发起表单'").ToList();
                //                        List<string> intPro = ListData.Select(d => d.PIID.ToString()).ToList();
                //                        string tableName = string.Format(@" SZHL_YCGL ycgl left outer join SZHL_YCGL_CAR  car on ycgl.CarID=car.ID inner join  Yan_WF_PI wfpi  on ycgl.intProcessStanceid=wfpi.ID");
                //                        string tableColumn = "ycgl.* ,car.CarBrand,car.CarType,car.CarNum , case when wfpi.IsCanceled is null then '已审批'  WHEN wfpi.IsCanceled='Y' then '已退回' END StateName ";
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
                            new JH_Auth_User_CenterB().ReadMsg(UserInfo, DataID, "YCGL");
                        }
                        break;
                    case "1": //创建的
                        {
                            strWhere += " And yc.CRUser ='" + userName + "'";
                        }
                        break;
                    case "2": //待审核
                        {
                            var intProD = new Yan_WF_PIB().GetDSH(UserInfo.User).Select(d => d.PIID.ToString()).ToList();
                            if (intProD.Count > 0)
                            {
                                strWhere += " And yc.intProcessStanceid in (" + (intProD.ListTOString(',') == "" ? "0" : intProD.ListTOString(',')) + ")";
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
                                strWhere += " And yc.intProcessStanceid in (" + (intProD.ListTOString(',') == "" ? "0" : intProD.ListTOString(',')) + ")";

                            }
                            else
                            {
                                strWhere += " And 1=0";
                            }
                        }
                        break;
                }

                dt = new SZHL_CCXJB().GetDataPager("SZHL_YCGL yc left join SZHL_YCGL_CAR car on yc.CarID=car.ID", "yc.*,car.CarBrand,car.CarType,car.CarNum ,dbo.fn_PDStatus(yc.intProcessStanceid) AS StateName", pagecount, page, " yc.CRDate desc", strWhere, ref total);

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
        #endregion

        #region 用车管理日历视图
        /// <summary>
        /// 用车管理日历视图
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void GETYCGLVIEW(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            string strSql = string.Format("SELECT  ycgl.ID,ycgl.intProcessStanceid,car.CarBrand+'-'+car.CarType+'-'+car.CarNum+'  '+CONVERT(VARCHAR(5),ycgl.StartTime,8)+'~'+CONVERT(VARCHAR(5),ycgl.EndTime,8) title,ycgl.StartTime start,ycgl.EndTime [end]  from SZHL_YCGL  ycgl left outer join SZHL_YCGL_CAR car on ycgl.CarID=car.ID   where ( dbo.fn_PDStatus(ycgl.intProcessStanceid)='已审批' or dbo.fn_PDStatus(ycgl.intProcessStanceid)='正在审批' or dbo.fn_PDStatus(ycgl.intProcessStanceid)='-1' ) and ycgl.ComId=" + UserInfo.User.ComId + " and isnull(car.CarType,'')!=''");
            if (P1 != "0")
            {
                strSql += string.Format(" and ycgl.CarID={0} ", P1);
            }
            msg.Result = new SZHL_CCXJB().GetDTByCommand(strSql);
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
        public void GETYCGLLIST_PAGE(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            string strWhere = " ycgl.ComId=" + UserInfo.User.ComId;

            if (P1 != "")
            {

                strWhere += string.Format(" And  ycgl.XCType='{0}'", P1);
            }
            if (P2 != "")
            {
                strWhere += string.Format(" And  ycgl.Remark like '%{0}%'", P2);
            }

            int page = 0;
            int.TryParse(context.Request["p"] ?? "1", out page);
            page = page == 0 ? 1 : page;
            int total = 0;
            string colNme = @"ycgl.*,car.CarBrand,car.CarType,car.CarNum ,    case WHEN wfpi.isComplete is null and wfpi.IsCanceled is null  THEN '正在审批' 
                                            when wfpi.isComplete='Y' then '已审批'  WHEN wfpi.IsCanceled='Y' then '已退回' END StateName";
            DataTable dt = new SZHL_CCXJB().GetDataPager("SZHL_YCGL ycgl left outer join SZHL_YCGL_CAR  car on ycgl.CarID=car.ID  inner join  Yan_WF_PI wfpi  on ycgl.intProcessStanceid=wfpi.ID", colNme, 8, page, " ycgl.CRDate desc", strWhere, ref total);
            msg.Result = dt;
            msg.Result1 = total;
        }
        #endregion

        #region 获取可用车辆
        /// <summary>
        /// 获取可用车辆
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void GETCLIST(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            // string strSql = string.Format("SELECT  car.* from  SZHL_YCGL_CAR car LEFT JOIN SZHL_YCGL ycgl on car.ID=ycgl.CarID where car.Status=0 and (ycgl.Status=0 or ycgl.Status is NULL) and car.ComId={0}", UserInfo.User.ComId);
            string strSql = string.Format("SELECT  car.* from  SZHL_YCGL_CAR car  where car.Status=0  and car.ComId={0}", UserInfo.User.ComId);
            //if (P1 != "")
            //{
            //    strSql += string.Format(" and ycgl.EndTime<'{0}' ", P1);
            //}
            msg.Result = new SZHL_YCGL_CARB().GetDTByCommand(strSql);
        }
        #endregion

        #region 查看可用车辆列表（微信端）
        /// <summary>
        /// 查看可用车辆列表（微信端）
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void GETKYCLLIST(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            //var list = new SZHL_HYGL_ROOMB().GetEntities(p => p.Status == "1" && p.IsDel == 0);
            string strwhere = string.Empty;
            if (P1 != "all")
            {
                strwhere = " and isnull(CarBrand,'')!='' ";
            }
            DataTable dt = new SZHL_YCGL_CARB().GetDTByCommand("select * from dbo.SZHL_YCGL_CAR where IsDel=0 and Status='0'  and comid=" + UserInfo.QYinfo.ComId + strwhere);

            dt.Columns.Add("CarTypeName", Type.GetType("System.String"));
            dt.Columns.Add("ZT", Type.GetType("System.String"));
            dt.Columns.Add("ZYSJ", Type.GetType("System.String"));

            foreach (DataRow dr in dt.Rows)
            {
                int rid = Int32.Parse(dr["ID"].ToString());
                int tid = Int32.Parse(dr["CarType"].ToString());

                var st = DateTime.Now;
                var et = DateTime.Now.AddDays(1);

                var jaz = new JH_Auth_ZiDianB().GetEntity(p => p.ID == tid);
                if (jaz != null)
                {
                    dr["CarTypeName"] = jaz.TypeName;
                }

                var list = new SZHL_YCGLB().GetEntities(p => p.ComId == UserInfo.QYinfo.ComId && p.CarID == rid && p.IsDel == 0 && ((st > p.StartTime && st < p.EndTime) || (et > p.StartTime && et < p.EndTime))).OrderBy(p => p.StartTime);

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
        }
        #endregion

        #region 添加用车管理
        /// <summary>
        /// 添加用车管理
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void ADDYCGL(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            SZHL_YCGL ycgl = JsonConvert.DeserializeObject<SZHL_YCGL>(P1);
            if (ycgl == null)
            {
                msg.ErrorMsg = "操作失败";
                return;
            }
            if (ycgl.SYRS == null)
            {
                msg.ErrorMsg = "请填写使用人数";
                return;
            }
            if (string.IsNullOrWhiteSpace(ycgl.StartAddress) || string.IsNullOrWhiteSpace(ycgl.EndAddress))
            {
                msg.ErrorMsg = "请填写地点";
                return;
            }
            if (ycgl.ID == 0)
            {
                if (P2 != "") // 处理微信上传的图片
                {

                    string fids = new FT_FileB().ProcessWxIMG(P2, "YCGL", UserInfo);

                    if (!string.IsNullOrEmpty(ycgl.Files))
                    {
                        ycgl.Files += "," + fids;
                    }
                    else
                    {
                        ycgl.Files = fids;
                    }
                }
                ycgl.CRDate = DateTime.Now;
                ycgl.CRUser = UserInfo.User.UserName;
                ycgl.ComId = UserInfo.User.ComId;
                ycgl.Status = "1";
                ycgl.IsDel = 0;
                new SZHL_YCGLB().Insert(ycgl);
            }
            else
            {
                new SZHL_YCGLB().Update(ycgl);
            }
            msg.Result = ycgl;
        }
        #endregion

        #region 获取用车信息
        /// <summary>
        /// 获取用车信息
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void GETYCGLMODEL(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            int Id = int.Parse(P1);
            var model = new SZHL_YCGLB().GetEntity(d => d.ID == Id && d.ComId == UserInfo.User.ComId);
            if (model != null)
            {
                msg.Result = model;
                if (!string.IsNullOrEmpty(model.Files))
                {
                    msg.Result1 = new FT_FileB().GetEntities(" ID in (" + model.Files + ")");
                }
                if (model.CarID != null)
                {
                    msg.Result2 = new SZHL_YCGL_CARB().GetEntity(p => p.ID == model.CarID);
                }

                new JH_Auth_User_CenterB().ReadMsg(UserInfo, model.ID, "YCGL");
            }
        }
        #endregion

        #region 更新归还车辆记录
        /// <summary>
        /// 更新归还车辆记录
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void BACKCLJL(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            int ID = int.Parse(P1);
            SZHL_YCGL ycgl = new SZHL_YCGLB().GetEntity(d => d.ID == ID && d.ComId == UserInfo.User.ComId);
            ycgl.Status = "0";//0 归还  1正在使用
            ycgl.BackDate = DateTime.Now;
            new SZHL_YCGLB().Update(ycgl);
            msg.Result = ycgl;
        }
        #endregion

        /// <summary>
        /// 取消用车预约
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void DELYC(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            int Id = int.Parse(P1);
            new SZHL_YCGLB().Delete(d => d.ID == Id && d.ComId == UserInfo.User.ComId);

        }
        public void SENDMSG(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            int Id = int.Parse(P1);
            var model = new SZHL_YCGLB().GetEntity(d => d.ID == Id && d.ComId == UserInfo.User.ComId);
            string strContent = "发起人：" + new JH_Auth_UserB().GetUserRealName(UserInfo.User.ComId.Value, model.CRUser) + "\r\n驾驶人：" + new JH_Auth_UserB().GetUserRealName(UserInfo.User.ComId.Value, model.JSR) + "\r\n您有新的用车通知,请尽快查看";
            Article ar0 = new Article();
            ar0.Title = "用车通知";
            ar0.Description = strContent;
            ar0.Url = model.ID.ToString();
            List<Article> al = new List<Article>();
            al.Add(ar0);
            //new JH_Auth_User_CenterB().SendMsg(UserInfo, "HYGL", strContent, model.ID.ToString(), model.JSR, "B", model.intProcessStanceid);

            WXHelp wx = new WXHelp(UserInfo.QYinfo);
            wx.SendTH(al, "YCGL", "A", model.JSR);

        }

        public void EXPORTYCGL(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            int total = 0;
            DataTable dt = new SZHL_YCGLB().GetDataPager("SZHL_YCGL yc left join SZHL_YCGL_CAR car on yc.CarID=car.ID", "yc.*,car.CarBrand,car.CarType,car.CarNum ,dbo.fn_PDStatus(yc.intProcessStanceid) AS StateName", 2000, 1, " yc.CRDate desc", "", ref total);

            string sqlCol = "SYUser|使用人,XCType|行程类别,CarNum|使用车辆,SYRS|用车人数,JSR|驾驶员,Remark|使用说明,StartTime|开始时间,EndTime|结束时间,StartAddress|出发地点,EndAddress|到达地点";
            CommonHelp ch = new CommonHelp();
            DataTable dt2 = dt.DelTableCol(sqlCol);
            foreach (DataRow dr in dt2.Rows)
            {
                try
                {
                    dr["使用人"] = new JH_Auth_UserB().GetUserRealName(UserInfo.QYinfo.ComId, dr["使用人"].ToString());
                    if (!string.IsNullOrEmpty(dr["驾驶员"].ToString()))
                    {
                        dr["驾驶员"] = new JH_Auth_UserB().GetUserRealName(UserInfo.QYinfo.ComId, dr["驾驶员"].ToString());

                    }

                }
                catch (Exception)
                {

                }
            }

            msg.ErrorMsg = ch.ExportToExcel("用车记录", dt2);
        }
        #endregion
    }
}