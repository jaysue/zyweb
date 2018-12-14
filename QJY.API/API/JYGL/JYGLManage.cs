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
    public class JYGLManage : IWsService
    {
        public void ProcessRequest(HttpContext context, ref Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            MethodInfo methodInfo = typeof(JYGLManage).GetMethod(msg.Action.ToUpper());
            JYGLManage model = new JYGLManage();
            methodInfo.FastInvoke(model, new object[] { context, msg, P1, P2, UserInfo });
        }
        #region 图书管理

        #region 获取图书列表
        /// <summary>
        /// 获取图书列表
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void GETTSLIST_PAGE(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            string strWhere = string.Format(" SZHL_TSGL_TS.ComId=" + UserInfo.User.ComId);
            if (P1 != "") //图书码
            {
                strWhere += string.Format("and SZHL_TSGL_TS.TSName like '%{0}%'", P1);
            }
            if (P2 != "")//图书类型
            {
                strWhere += string.Format(" And SZHL_TSGL_TS.TSType='{0}'", P2); ;
            }
            string kystatus = context.Request["kystatus"] ?? "";
            if (kystatus != "")//图书类型
            {
                strWhere += string.Format(" And SZHL_TSGL_TS.Status='{0}'", kystatus);
            }

            string jystatus = context.Request["jystatus"] ?? "";
            if (jystatus != "")//图书类型
            {
                strWhere += string.Format(" And SZHL_TSGL_TS.jystatus='{0}'", jystatus);
            }
            int recordCount = 0;
            int page = 0;
            int pagecount = 8;
            int.TryParse(context.Request["p"] ?? "1", out page);
            int.TryParse(context.Request["pagecount"] ?? "8", out pagecount);//页数
            DataTable dt = new SZHL_TSGL_TSB().GetDataPager("SZHL_TSGL_TS   left join  JH_Auth_ZiDian zd on SZHL_TSGL_TS.tsType=zd.ID and zd.Class=24 ", "SZHL_TSGL_TS.*,zd.TypeName,'' dghsj", pagecount, page, "SZHL_TSGL_TS.CRDate desc", strWhere, ref recordCount);

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                if (dt.Rows[i]["jystatus"].ToString() != "0")
                {
                    dt.Rows[i]["dghsj"] = new SZHL_TSGLB().getTSGHDATA(dt.Rows[i]["ID"].ToString());
                }
            }
            msg.Result = dt;
            msg.Result1 = recordCount;
        }
        #endregion

        #region 添加图书信息
        /// <summary>
        /// 添加图书信息
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void ADDTSINFO(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            SZHL_TSGL_TS Info = JsonConvert.DeserializeObject<SZHL_TSGL_TS>(P1);
            if (string.IsNullOrEmpty(Info.TSType))
            {
                msg.ErrorMsg = "请选择图书类型";
                return;
            }
            if (string.IsNullOrEmpty(Info.TSNum))
            {
                msg.ErrorMsg = "请填写图书编码";
                return;
            }
            if (Info.ID == 0)
            {
                SZHL_TSGL_TS MODEL = new SZHL_TSGL_TSB().GetEntity(d => d.TSNum == Info.TSNum && d.ComId == UserInfo.User.ComId);
                if (MODEL != null)
                {
                    msg.ErrorMsg = "已有此编码的图书";
                }
                else
                {
                    Info.CRDate = DateTime.Now;
                    Info.CRUser = UserInfo.User.UserName;
                    Info.ComId = UserInfo.User.ComId;
                    Info.IsDel = 0;
                    new SZHL_TSGL_TSB().Insert(Info);
                }
            }
            else
            {
                new SZHL_TSGL_TSB().Update(Info);
            }
        }
        #endregion

        #region 获取图书信息
        /// <summary>
        /// 获取图书信息
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void GETTSINFO(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            int Id = int.Parse(P1);
            var info = new SZHL_TSGL_TSB().GetEntity(d => d.ID == Id && d.ComId == UserInfo.User.ComId);
            msg.Result = info;
            if (!string.IsNullOrEmpty(info.Files))
            {
                msg.Result4 = new FT_FileB().GetEntities(" ID in (" + info.Files + ")");
            }

            msg.Result2 = new SZHL_TSGLB().GetEntities(" ','+TSID+','  like '%," + info.ID + ",%'");

        }



        /// <summary>
        /// 获取借阅记录
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void GETTSJYINFO(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            int Id = int.Parse(P1);
            msg.Result = new SZHL_TSGLB().GetEntities(" ','+TSID+','  like '%," + Id + ",%'").OrderByDescending(d => d.ID);

        }
        #endregion

        #region 更新图书状态
        /// <summary>
        /// 更新图书状态
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void MODIFYTSSTATUS(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {

            new SZHL_TSGL_TSB().UPSTATUS(P1, P2, UserInfo.User.ComId.ToString());
        }
        public void REBACKTS(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            int JYId = int.Parse(P2);

            new SZHL_TSGL_TSB().UPSTATUS(P1, "0", UserInfo.User.ComId.ToString());
            SZHL_TSGL jygl = new SZHL_TSGLB().GetEntity(d => d.ID == JYId && d.ComId == UserInfo.User.ComId);
            jygl.BackDate = DateTime.Now;
            jygl.BackBZ = (jygl.BackBZ + "," + P1).TrimStart(',');
            new SZHL_TSGLB().Update(jygl);


            //发送归还提醒
            SZHL_TXSX TX = new SZHL_TXSX();
            TX.Date = DateTime.Now.ToString();
            TX.APIName = "JYGL";
            TX.ComId = UserInfo.User.ComId;
            TX.FunName = "TSGLMSG";
            TX.TXMode = "JYGL";
            TX.CRUserRealName = UserInfo.User.UserRealName;
            TX.MsgID = JYId.ToString();
            TX.TXContent = "您好：" + UserInfo.User.UserRealName + ",您借阅的图书(" + jygl.TSName + ")已归还";
            TX.TXUser = jygl.JYR;
            TX.CRUser = UserInfo.User.UserName;
            TXSX.TXSXAPI.AddALERT(TX); //时间为发送时间


        }

        public void SENDTXMSG(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            int Id = 0;
            int.TryParse(P1, out Id);
            SZHL_TSGL jygl = new SZHL_TSGLB().GetEntity(d => d.ID == Id);
            if (jygl != null)
            {
                SZHL_TXSX TX = new SZHL_TXSX();
                TX.Date = DateTime.Now.ToString();
                TX.APIName = "JYGL";
                TX.ComId = UserInfo.User.ComId;
                TX.FunName = "TSGLMSG";
                TX.TXMode = "JYGL";
                TX.CRUserRealName = UserInfo.User.UserRealName;
                TX.MsgID = P1;
                TX.TXContent = "您好：" + UserInfo.User.UserRealName + ",请尽快归还图书(" + jygl.TSName + ")";
                TX.TXUser = jygl.JYR;
                TX.CRUser = UserInfo.User.UserName;
                TXSX.TXSXAPI.AddALERT(TX); //时间为发送时间
            }
        }

        #endregion

        #region 获取所有图书
        /// <summary>
        /// 获取所有图书
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void GETALLTSLIST(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            string strSql = string.Format("SELECT ts.*,zd.TypeName from SZHL_TSGL_TS ts left join  JH_Auth_ZiDian zd on ts.tsType=zd.ID and Class=24 Where ts.ComId={0} and ts.Status=0", UserInfo.User.ComId);
            msg.Result = new SZHL_TSGL_TSB().GetDTByCommand(strSql);
        }
        #endregion

        #region 删除图书
        public void DELTS(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            int id = int.Parse(P1);
            new SZHL_TSGL_TSB().Delete(d => d.ID == id && d.ComId == UserInfo.User.ComId.Value);
        }
        #endregion

        #endregion


        #region 借阅列表
        /// <summary>
        /// 借阅列表
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void GETJYGLLIST(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            string userName = UserInfo.User.UserName;
            string strWhere = " 1=1 and jy.ComId=" + UserInfo.User.ComId;


            string strContent = context.Request["Content"] ?? "";
            strContent = strContent.TrimEnd();
            if (strContent != "")
            {
                strWhere += string.Format(" And ( jy.TSName like '%{0}%')", strContent);
            }
            int DataID = -1;
            int.TryParse(context.Request["ID"] ?? "-1", out DataID);//记录Id
            if (DataID != -1)
            {
                string strIsHasDataQX = new JH_Auth_QY_ModelB().ISHASDATAREADQX("JYGL", DataID, UserInfo);
                if (strIsHasDataQX == "Y")
                {
                    strWhere += string.Format(" And jy.ID = '{0}'", DataID);
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
                            new JH_Auth_User_CenterB().ReadMsg(UserInfo, DataID, "JYGL");
                        }
                        break;
                    case "1": //创建的
                        {
                            strWhere += " And jy.CRUser ='" + userName + "'";
                        }
                        break;
                    case "2": //待审核
                        {
                            var intProD = new Yan_WF_PIB().GetDSH(UserInfo.User).Select(d => d.PIID.ToString()).ToList();
                            if (intProD.Count > 0)
                            {
                                strWhere += " And jy.intProcessStanceid in (" + (intProD.ListTOString(',') == "" ? "0" : intProD.ListTOString(',')) + ")";
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
                                strWhere += " And jy.intProcessStanceid in (" + (intProD.ListTOString(',') == "" ? "0" : intProD.ListTOString(',')) + ")";

                            }
                            else
                            {
                                strWhere += " And 1=0";
                            }
                        }
                        break;
                }

                dt = new SZHL_TSGLB().GetDataPager("SZHL_TSGL jy ", "jy.*,dbo.fn_PDStatus(jy.intProcessStanceid) AS StateName", pagecount, page, " jy.CRDate desc", strWhere, ref total);

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



        #region 菜品列表
        /// <summary>
        /// 图书列表//手机端
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void GETTSLIST(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            DataTable dt = new JH_Auth_ZiDianB().GetDTByCommand("select TypeNO,TypeName,ID from JH_Auth_ZiDian where class=24 and (comid=" + UserInfo.User.ComId + " or comid=0)");
            dt.Columns.Add("Item", Type.GetType("System.Object"));
            dt.Columns.Add("Qty", Type.GetType("System.String"));
            dt.Columns.Add("xsQty", Type.GetType("System.String"));

            List<SZHL_TSGL_TS> LISTTS = new SZHL_TSGL_TSB().GetEntities(D => D.Status != "1" && D.ComId == UserInfo.User.ComId).ToList();
            foreach (DataRow dr in dt.Rows)
            {
                String rid = dr["ID"].ToString();
                var list = LISTTS.Where(D => D.TSType == rid).OrderByDescending(p => p.CRDate).Select(p => new
                {
                    p.ID,
                    p.CRDate,
                    p.auther,
                    p.SL,
                    p.TSName,
                    p.TSTypeName,
                    p.jystatus,
                    p.Files,
                    dghsj = p.jystatus == "0" ? "" : new SZHL_TSGLB().getTSGHDATA(p.ID.ToString()),
                    Qty = 0
                });
                dr["Item"] = list;
                dr["Qty"] = 0;
                dr["xsQty"] = list.Count();
            }

            msg.Result = dt;
        }
        #endregion

        #region 借阅管理日历视图
        /// <summary>
        /// 借阅管理日历视图
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void GETYCGLVIEW(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            string strSql = string.Format("SELECT  ycgl.ID,ycgl.intProcessStanceid,ts.tsBrand+'-'+ts.tsType+'-'+ts.tsNum+'  '+CONVERT(VARCHAR(5),ycgl.StartTime,8)+'~'+CONVERT(VARCHAR(5),ycgl.EndTime,8) title,ycgl.StartTime start,ycgl.EndTime [end]  from SZHL_TSGL  ycgl left outer join SZHL_TSGL_TS ts on ycgl.tsID=ts.ID   where ( dbo.fn_PDStatus(ycgl.intProcessStanceid)='已审批' or dbo.fn_PDStatus(ycgl.intProcessStanceid)='正在审批' or dbo.fn_PDStatus(ycgl.intProcessStanceid)='-1' ) and ycgl.ComId=" + UserInfo.User.ComId + " and isnull(ts.tsType,'')!=''");
            if (P1 != "0")
            {
                strSql += string.Format(" and ycgl.tsID={0} ", P1);
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
        public void GETTSGLLIST_PAGE(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            string strWhere = " jygl.ComId=" + UserInfo.User.ComId;

            if (P1 != "")
            {

                strWhere += string.Format(" And  jygl.XCType='{0}'", P1);
            }
            if (P2 != "")
            {
                strWhere += string.Format(" And  jygl.Remark like '%{0}%'", P2);
            }

            int page = 0;
            int.TryParse(context.Request["p"] ?? "1", out page);
            page = page == 0 ? 1 : page;
            int total = 0;
            string colNme = @"jygl.*,ts.tsBrand,ts.tsType,ts.tsNum ,    case WHEN wfpi.isComplete is null and wfpi.IsCanceled is null  THEN '正在审批' 
                                            when wfpi.isComplete='Y' then '已审批'  WHEN wfpi.IsCanceled='Y' then '已退回' END StateName";
            DataTable dt = new SZHL_CCXJB().GetDataPager("SZHL_TSGL jygl left outer join SZHL_TSGL_TS  ts on jygl.tsID=ts.ID  inner join  Yan_WF_PI wfpi  on jygl.intProcessStanceid=wfpi.ID", colNme, 8, page, " ycgl.CRDate desc", strWhere, ref total);
            msg.Result = dt;
            msg.Result1 = total;
        }
        #endregion

        #region 获取可用图书
        /// <summary>
        /// 获取可用图书
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void GETTSIST(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            string strSql = string.Format("SELECT  ts.* from  SZHL_TSGL_TS ts  where ts.Status=0  and ts.ComId={0}", UserInfo.User.ComId);

            strSql += string.Format("  and ts.id in ({0})", P1 == "" ? "0" : P1.TrimEnd(','));
            msg.Result = new SZHL_TSGL_TSB().GetDTByCommand(strSql);
        }
        #endregion

        #region 查看可用图书列表（微信端）
        /// <summary>
        /// 查看可用图书列表（微信端）
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void GETKYTSLIST(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            string strwhere = string.Empty;

            DataTable dt = new SZHL_TSGL_TSB().GetDTByCommand("select * from dbo.SZHL_TSGL_TS where IsDel=0 and Status='0'  and comid=" + UserInfo.QYinfo.ComId + strwhere);

            dt.Columns.Add("tsTypeName", Type.GetType("System.String"));
            dt.Columns.Add("ZT", Type.GetType("System.String"));
            dt.Columns.Add("ZYSJ", Type.GetType("System.String"));

            foreach (DataRow dr in dt.Rows)
            {

            }

            msg.Result = dt;
        }
        #endregion

        #region 添加借阅管理
        /// <summary>
        /// 添加借阅管理
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void ADDJYGL(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            SZHL_TSGL jygl = JsonConvert.DeserializeObject<SZHL_TSGL>(P1);
            JH_Auth_ZiDian ZD = new JH_Auth_ZiDianB().GetEntities(d => d.Class == 25).FirstOrDefault();
            if (jygl == null)
            {
                msg.ErrorMsg = "操作失败";
                return;
            }
            if (new SZHL_TSGLB().getYHYJTS(UserInfo.User.UserName, UserInfo.User.ComId.Value) + jygl.TSID.Split(',').Count() > int.Parse(ZD.Remark1))
            {
                msg.ErrorMsg = "超出借书数量限制,数量限制为" + ZD.Remark1 + "本";
                return;
            }
            double hours = (jygl.EndTime.Value.AddDays(1) - jygl.StartTime.Value).TotalDays;
            if (hours > double.Parse(ZD.Remark2))
            {
                msg.ErrorMsg = "超出借书时间限制,时间限制为" + ZD.Remark2 + "天";
                return;
            }
            if (jygl.ID == 0)
            {
                jygl.CRDate = DateTime.Now;
                jygl.CRUser = UserInfo.User.UserName;
                jygl.ComId = UserInfo.User.ComId;
                jygl.Status = "0";
                jygl.BackBZ = "";
                jygl.IsDel = 0;
                new SZHL_TSGLB().Insert(jygl);
                new SZHL_TSGL_TSB().UPSTATUS(jygl.TSID, "1", UserInfo.User.ComId.ToString());//更新为借阅状态

            }
            else
            {
                new SZHL_TSGLB().Update(jygl);
            }
            msg.Result = jygl;
        }
        #endregion

        #region 获取借阅信息
        /// <summary>
        /// 获取借阅信息
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void GETJYGLMODEL(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            int Id = int.Parse(P1);
            var model = new SZHL_TSGLB().GetEntity(d => d.ID == Id && d.ComId == UserInfo.User.ComId);
            if (model != null)
            {
                msg.Result = model;
                if (!string.IsNullOrEmpty(model.Files))
                {
                    msg.Result1 = new FT_FileB().GetEntities(" ID in (" + model.Files + ")");
                }
                if (model.TSID != null)
                {
                    msg.Result2 = new SZHL_TSGL_TSB().GetEntities(" ID IN (" + model.TSID + ") ");
                }

                new JH_Auth_User_CenterB().ReadMsg(UserInfo, model.ID, "TSGL");
            }
        }
        #endregion


        /// <summary>
        /// 取消图书借阅
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void DELJY(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            int Id = int.Parse(P1);
            new SZHL_TSGLB().Delete(d => d.ID == Id && d.ComId == UserInfo.User.ComId);

        }




        #region 借阅消息的接口
        public void TSGLMSG(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            SZHL_TXSX TX = JsonConvert.DeserializeObject<SZHL_TXSX>(P1);
            int rwid = 0;
            int.TryParse(TX.MsgID, out rwid);
            SZHL_TSGL jy = new SZHL_TSGLB().GetEntity(d => d.ID == rwid);

            Article ar0 = new Article();
            ar0.Title = TX.TXContent;
            ar0.Description = jy == null ? "" : jy.TSName;
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




        #endregion
    }
}