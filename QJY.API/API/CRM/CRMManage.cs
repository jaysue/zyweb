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
using Newtonsoft.Json.Linq;
using QJY.Common;

namespace QJY.API
{
    public class CRMManage : IWsService
    {
        public void ProcessRequest(HttpContext context, ref Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            MethodInfo methodInfo = typeof(CRMManage).GetMethod(msg.Action.ToUpper());
            CRMManage model = new CRMManage();
            methodInfo.FastInvoke(model, new object[] { context, msg, P1, P2, UserInfo });
        }

        #region 客户管理

        #region 客户列表
        /// <summary>
        /// 客户列表
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void GETKHGLLIST(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            string userName = UserInfo.User.UserName;
            string strWhere = " 1=1 and kh.ComId=" + UserInfo.User.ComId;

            string leibie = context.Request["lb"] ?? "";
            if (leibie != "")
            {
                strWhere += string.Format(" And kh.KHType='{0}' ", leibie);
            }
            string strContent = context.Request["Content"] ?? "";
            strContent = strContent.TrimEnd();
            if (strContent != "")
            {
                string extwhere = string.Empty;
                string extids = new JH_Auth_ExtendDataB().GetExtIds(Int32.Parse(UserInfo.User.ComId.ToString()), "KHGL", strContent);
                if (extids != "")
                {
                    extwhere = " or kh.ID in (" + extids + ")";
                }
                strWhere += string.Format(" And ( kh.KHName like '%{0}%' " + extwhere + ")", strContent);
            }

            if (P2 != "")
            {
                string[] strs = P2.Split('_');

                if (!string.IsNullOrEmpty(strs[0]))
                {
                    strWhere += string.Format(" And kh.Status='{0}' ", strs[0]);
                }
                if (!string.IsNullOrEmpty(strs[1]))
                {
                    strWhere += string.Format(" And kh.Source='{0}' ", strs[1]);
                }
                if (!string.IsNullOrEmpty(strs[2]))
                {
                    strWhere += string.Format(" And kh.Industry='{0}' ", strs[2]);
                }
                if (!string.IsNullOrEmpty(strs[3]))
                {
                    strWhere += string.Format(" And kh.Scale='{0}' ", strs[3]);
                }
            }

            int DataID = -1;
            int.TryParse(context.Request["ID"] ?? "-1", out DataID);//记录Id
            if (DataID != -1)
            {
                string strIsHasDataQX = new JH_Auth_QY_ModelB().ISHASDATAREADQX("CRM", DataID, UserInfo);
                if (strIsHasDataQX == "Y")
                {
                    strWhere += string.Format(" And kh.ID = '{0}'", DataID);
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
                            new JH_Auth_User_CenterB().ReadMsg(UserInfo, DataID, "CRM");
                        }
                        break;
                    case "1": //创建的
                        {
                            strWhere += " And kh.CRUser ='" + userName + "'";
                        }
                        break;
                    case "2": //负责的
                        {
                            strWhere += " And kh.FZUser ='" + userName + "'";
                        }
                        break;
                    case "3": //下属的
                        {
                            //获取当前登录人负责的下属人员 
                            string Users = new JH_Auth_UserB().GetUserBranchUsers(UserInfo.User.ComId.Value, UserInfo.User.UserName).Select(d => d.UserName).ToList().ListTOString(',');
                            strWhere += string.Format(" and (kh.CRUser in ('{0}') or kh.FZUser in ('{0}')) ", Users.ToFormatLike());
                        }
                        break;
                }
                dt = new SZHL_CRM_KHGLB().GetDataPager("SZHL_CRM_KHGL kh left join JH_Auth_ZiDian zd on kh.KHType=zd.ID", "kh.*,zd.TypeName ", pagecount, page, " kh.CRDate desc", strWhere, ref total);

                if (dt.Rows.Count > 0)
                {
                    dt.Columns.Add("FileList", Type.GetType("System.Object"));
                    dt.Columns.Add("GJZT", Type.GetType("System.String"));
                    dt.Columns.Add("KHLY", Type.GetType("System.String"));
                    dt.Columns.Add("SSHY", Type.GetType("System.String"));
                    dt.Columns.Add("RYGM", Type.GetType("System.String"));

                    dt.Columns.Add("KHLXR", Type.GetType("System.Object"));
                    dt.Columns.Add("GJJL", Type.GetType("System.Object"));
                    dt.Columns.Add("KHHT", Type.GetType("System.Object"));
                    //dt.Columns.Add("KHCP", Type.GetType("System.Object"));
                    dt.Columns.Add("SubExt", Type.GetType("System.Object"));//扩展字段

                    DataTable dtExt = new JH_Auth_ExtendModeB().GetExtColumnAll(UserInfo.User.ComId, "KHGL");
                    foreach (DataRow drExt in dtExt.Rows)
                    {
                        dt.Columns.Add(drExt["TableFiledName"].ToString(), Type.GetType("System.String"));
                    }

                    foreach (DataRow dr in dt.Rows)
                    {
                        #region 扩展字段
                        DataTable dtExtData = new JH_Auth_ExtendModeB().GetExtData(UserInfo.User.ComId, "KHGL", dr["ID"].ToString());
                        dr["SubExt"] = dtExtData;
                        foreach (DataRow drExtData in dtExtData.Rows)
                        {
                            dr[drExtData["TableFiledName"].ToString()] = drExtData["ExtendDataValue"].ToString();
                        }
                        #endregion

                        #region 附件
                        if (dr["Files"] != null && dr["Files"].ToString() != "")
                        {
                            dr["FileList"] = new FT_FileB().GetEntities(" ID in (" + dr["Files"].ToString() + ")");
                        }
                        #endregion

                        #region 跟进状态
                        if (dr["Status"] != null && dr["Status"].ToString() != "")
                        {
                            int SId = int.Parse(dr["Status"].ToString());

                            var SS = new JH_Auth_ZiDianB().GetEntity(p => p.ID == SId);
                            if (SS != null)
                            {
                                dr["GJZT"] = SS.TypeName;
                            }
                        }
                        #endregion

                        #region 客户来源
                        if (dr["Source"] != null && dr["Source"].ToString() != "")
                        {
                            int SoId = int.Parse(dr["Source"].ToString());

                            var S0 = new JH_Auth_ZiDianB().GetEntity(p => p.ID == SoId);
                            if (S0 != null)
                            {
                                dr["KHLY"] = S0.TypeName;
                            }
                        }
                        #endregion

                        #region 所属行业
                        if (dr["Industry"] != null && dr["Industry"].ToString() != "")
                        {
                            int IId = int.Parse(dr["Industry"].ToString());

                            var IY = new JH_Auth_ZiDianB().GetEntity(p => p.ID == IId);
                            if (IY != null)
                            {
                                dr["SSHY"] = IY.TypeName;
                            }
                        }
                        #endregion

                        #region 人员规模
                        if (dr["Scale"] != null && dr["Scale"].ToString() != "")
                        {
                            int ScId = int.Parse(dr["Scale"].ToString());

                            var SC = new JH_Auth_ZiDianB().GetEntity(p => p.ID == ScId);
                            if (SC != null)
                            {
                                dr["RYGM"] = SC.TypeName;
                            }
                        }
                        #endregion

                        #region 跟进记录列表
                        DataTable dtGJJL = new SZHL_CRM_KHGLB().GetDTByCommand("select * from SZHL_CRM_GJJL where ComId='" + UserInfo.User.ComId + "' and typecode='KHGL' and KHID='" + dr["ID"].ToString() + "' order by CRDate desc");
                        dtGJJL.Columns.Add("FileList", Type.GetType("System.Object"));
                        dtGJJL.Columns.Add("GJZT", Type.GetType("System.String"));
                        foreach (DataRow drgjjl in dtGJJL.Rows)
                        {
                            if (drgjjl["Files"] != null && drgjjl["Files"].ToString() != "")
                            {
                                drgjjl["FileList"] = new FT_FileB().GetEntities(" ID in (" + drgjjl["Files"].ToString() + ")");
                            }
                            if (drgjjl["Status"] != null && drgjjl["Status"].ToString() != "")
                            {
                                int SId = int.Parse(drgjjl["Status"].ToString());

                                var SS = new JH_Auth_ZiDianB().GetEntity(p => p.ID == SId);
                                if (SS != null)
                                {
                                    drgjjl["GJZT"] = SS.TypeName;
                                }
                            }
                        }
                        dr["GJJL"] = dtGJJL;
                        #endregion

                        #region 联系人列表
                        DataTable dtLXR = new SZHL_CRM_KHGLB().GetDTByCommand("select * from SZHL_CRM_CONTACT where ComId='" + UserInfo.User.ComId + "' and KHID='" + dr["ID"].ToString() + "' order by CRDate desc ");
                        dtLXR.Columns.Add("FileList", Type.GetType("System.Object"));
                        dtLXR.Columns.Add("SubExt", Type.GetType("System.Object"));//扩展字段
                        foreach (DataRow drlxr in dtLXR.Rows)
                        {
                            if (drlxr["Files"] != null && drlxr["Files"].ToString() != "")
                            {
                                drlxr["FileList"] = new FT_FileB().GetEntities(" ID in (" + drlxr["Files"].ToString() + ")");
                            }
                            drlxr["SubExt"] = new JH_Auth_ExtendModeB().GetExtData(UserInfo.User.ComId, "KHLXR", drlxr["ID"].ToString());
                        }
                        dr["KHLXR"] = dtLXR;
                        #endregion

                        #region 合同管理
                        DataTable dtkhht = new SZHL_CRM_KHGLB().GetDTByCommand(@"select ht.*,zd.TypeName,kh.KHName,zde.TypeName AS FSName,cp.Name AS CPName from SZHL_CRM_HTGL ht 
LEFT join JH_Auth_ZiDian zd on HTType= zd.ID and Class=16 
LEFT join JH_Auth_ZiDian zde on FKFS= zde.ID and zde.Class=17 
LEFT join SZHL_CRM_KHGL kh on ht.KHID=kh.ID 
LEFT JOIN SZHL_CRM_CPGL cp on PID=cp.ID 
where ht.ComId='" + UserInfo.User.ComId + "' and ht.KHID='" + dr["ID"].ToString() + "' order by CRDate desc ");
                        dtkhht.Columns.Add("FileList", Type.GetType("System.Object"));
                        dtkhht.Columns.Add("SubExt", Type.GetType("System.Object"));//扩展字段
                        foreach (DataRow drkhht in dtkhht.Rows)
                        {
                            if (drkhht["Files"] != null && drkhht["Files"].ToString() != "")
                            {
                                drkhht["FileList"] = new FT_FileB().GetEntities(" ID in (" + drkhht["Files"].ToString() + ")");
                            }
                            if (drkhht["HTStatus"] != null && drkhht["HTStatus"].ToString() != "")
                            {
                                string statusName = "";
                                switch (drkhht["HTStatus"].ToString())
                                {
                                    case "0":
                                        statusName = "未开始";
                                        break;
                                    case "1":
                                        statusName = "执行中";
                                        break;
                                    case "2":
                                        statusName = "成功结束";
                                        break;
                                    case "3":
                                        statusName = "意外终止";
                                        break;
                                }
                                drkhht["HTStatus"] = statusName;
                            }

                            drkhht["SubExt"] = new JH_Auth_ExtendModeB().GetExtData(UserInfo.User.ComId, "HTGL", drkhht["ID"].ToString());
                        }
                        dr["KHHT"] = dtkhht;
                        #endregion
                    }
                }
                msg.Result = dt;
                msg.Result1 = total;
            }
        }
        #endregion

        #region 获取客户信息
        /// <summary>
        /// 获取客户信息
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void GETKHGLMODEL(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            //    new JH_Auth_User_CenterB().ReadMsg(UserInfo, khgl.ID, "CRM");

            DataTable dt = new SZHL_CRM_KHGLB().GetDTByCommand("select * from SZHL_CRM_KHGL where ID='" + P1 + "'");
            if (dt.Rows.Count > 0)
            {
                dt.Columns.Add("TypeName", Type.GetType("System.String"));
                dt.Columns.Add("GJZT", Type.GetType("System.String"));
                dt.Columns.Add("KHLY", Type.GetType("System.String"));
                dt.Columns.Add("SSHY", Type.GetType("System.String"));
                dt.Columns.Add("RYGM", Type.GetType("System.String"));

                dt.Columns.Add("KHLXR", Type.GetType("System.Object"));
                dt.Columns.Add("GJJL", Type.GetType("System.Object"));
                dt.Columns.Add("KHHT", Type.GetType("System.Object"));
                dt.Columns.Add("SubExt", Type.GetType("System.Object"));//扩展字段

                dt.Rows[0]["SubExt"] = new JH_Auth_ExtendModeB().GetExtData(UserInfo.User.ComId, "KHGL", dt.Rows[0]["ID"].ToString());

                #region 类型
                if (dt.Rows[0]["KHType"] != null && dt.Rows[0]["KHType"].ToString() != "")
                {
                    int TypeId = int.Parse(dt.Rows[0]["KHType"].ToString());

                    var zd = new JH_Auth_ZiDianB().GetEntity(p => p.ID == TypeId);
                    if (zd != null)
                    {
                        dt.Rows[0]["TypeName"] = zd.TypeName;
                    }
                }
                #endregion

                #region 跟进状态
                if (dt.Rows[0]["Status"] != null && dt.Rows[0]["Status"].ToString() != "")
                {
                    int SId = int.Parse(dt.Rows[0]["Status"].ToString());

                    var SS = new JH_Auth_ZiDianB().GetEntity(p => p.ID == SId);
                    if (SS != null)
                    {
                        dt.Rows[0]["GJZT"] = SS.TypeName;
                    }
                }
                #endregion

                #region 客户来源
                if (dt.Rows[0]["Source"] != null && dt.Rows[0]["Source"].ToString() != "")
                {
                    int SoId = int.Parse(dt.Rows[0]["Source"].ToString());

                    var S0 = new JH_Auth_ZiDianB().GetEntity(p => p.ID == SoId);
                    if (S0 != null)
                    {
                        dt.Rows[0]["KHLY"] = S0.TypeName;
                    }
                }
                #endregion

                #region 所属行业
                if (dt.Rows[0]["Industry"] != null && dt.Rows[0]["Industry"].ToString() != "")
                {
                    int IId = int.Parse(dt.Rows[0]["Industry"].ToString());

                    var IY = new JH_Auth_ZiDianB().GetEntity(p => p.ID == IId);
                    if (IY != null)
                    {
                        dt.Rows[0]["SSHY"] = IY.TypeName;
                    }
                }
                #endregion

                #region 人员规模
                if (dt.Rows[0]["Scale"] != null && dt.Rows[0]["Scale"].ToString() != "")
                {
                    int ScId = int.Parse(dt.Rows[0]["Scale"].ToString());

                    var SC = new JH_Auth_ZiDianB().GetEntity(p => p.ID == ScId);
                    if (SC != null)
                    {
                        dt.Rows[0]["RYGM"] = SC.TypeName;
                    }
                }
                #endregion

                #region 附件
                if (dt.Rows[0]["Files"] != null && dt.Rows[0]["Files"].ToString() != "")
                {
                    msg.Result1 = new FT_FileB().GetEntities(" ID in (" + dt.Rows[0]["Files"].ToString() + ")");
                }
                #endregion

                #region 跟进记录列表
                DataTable dtGJJL = new SZHL_CRM_KHGLB().GetDTByCommand("select * from SZHL_CRM_GJJL where ComId='" + UserInfo.User.ComId + "' AND typecode='KHGL' and KHID='" + P1 + "' order by CRDate desc");
                dtGJJL.Columns.Add("FileList", Type.GetType("System.Object"));
                dtGJJL.Columns.Add("GJZT", Type.GetType("System.String"));
                foreach (DataRow drgjjl in dtGJJL.Rows)
                {
                    if (drgjjl["Files"] != null && drgjjl["Files"].ToString() != "")
                    {
                        drgjjl["FileList"] = new FT_FileB().GetEntities(" ID in (" + drgjjl["Files"].ToString() + ")");
                    }
                    if (drgjjl["Status"] != null && drgjjl["Status"].ToString() != "")
                    {
                        int SId = int.Parse(drgjjl["Status"].ToString());

                        var SS = new JH_Auth_ZiDianB().GetEntity(p => p.ID == SId);
                        if (SS != null)
                        {
                            drgjjl["GJZT"] = SS.TypeName;
                        }
                    }
                }
                dt.Rows[0]["GJJL"] = dtGJJL;
                #endregion

                #region 联系人列表
                DataTable dtLXR = new SZHL_CRM_KHGLB().GetDTByCommand("select * from SZHL_CRM_CONTACT where ComId='" + UserInfo.User.ComId + "' and KHID='" + P1 + "' order by CRDate desc ");
                dtLXR.Columns.Add("FileList", Type.GetType("System.Object"));
                dtLXR.Columns.Add("SubExt", typeof(DataTable));
                foreach (DataRow drlxr in dtLXR.Rows)
                {
                    if (drlxr["Files"] != null && drlxr["Files"].ToString() != "")
                    {
                        drlxr["FileList"] = new FT_FileB().GetEntities(" ID in (" + drlxr["Files"].ToString() + ")");
                    }
                    //扩展字段
                    drlxr["SubExt"] = new JH_Auth_ExtendModeB().GetExtData(UserInfo.User.ComId, "KHLXR", drlxr["ID"].ToString());
                }
                dt.Rows[0]["KHLXR"] = dtLXR;
                #endregion

                #region 客户合同
                DataTable dtkhht = new SZHL_CRM_KHGLB().GetDTByCommand(@"select ht.*,zd.TypeName,kh.KHName,zde.TypeName AS FSName,cp.Name AS CPName from SZHL_CRM_HTGL ht 
LEFT join JH_Auth_ZiDian zd on HTType= zd.ID and Class=16 
LEFT join JH_Auth_ZiDian zde on FKFS= zde.ID and zde.Class=17 
LEFT join SZHL_CRM_KHGL kh on KHID=kh.ID 
LEFT JOIN SZHL_CRM_CPGL cp on PID=cp.ID 
where ht.ComId='" + UserInfo.User.ComId + "' and KHID='" + P1 + "' order by CRDate desc ");
                dtkhht.Columns.Add("FileList", Type.GetType("System.Object"));
                dtkhht.Columns.Add("SubExt", typeof(DataTable));
                foreach (DataRow drkhht in dtkhht.Rows)
                {
                    if (drkhht["Files"] != null && drkhht["Files"].ToString() != "")
                    {
                        drkhht["FileList"] = new FT_FileB().GetEntities(" ID in (" + drkhht["Files"].ToString() + ")");
                    }
                    if (drkhht["HTStatus"] != null && drkhht["HTStatus"].ToString() != "")
                    {
                        string statusName = "";
                        switch (drkhht["HTStatus"].ToString())
                        {
                            case "0":
                                statusName = "未开始";
                                break;
                            case "1":
                                statusName = "执行中";
                                break;
                            case "2":
                                statusName = "成功结束";
                                break;
                            case "3":
                                statusName = "意外终止";
                                break;
                        }
                        drkhht["HTStatus"] = statusName;
                    }
                    drkhht["SubExt"] = new JH_Auth_ExtendModeB().GetExtData(UserInfo.User.ComId, "HTGL", drkhht["ID"].ToString());
                }
                dt.Rows[0]["KHHT"] = dtkhht;
                #endregion


                msg.Result = dt;

                int Id = int.Parse(dt.Rows[0]["ID"].ToString());
                new JH_Auth_User_CenterB().ReadMsg(UserInfo, Id, "CRM");

                msg.Result2 = new SZHL_CRM_CONTACTB().GetEntities(p => p.KHID == Id);
            }

        }
        #endregion

        #region 添加客户
        /// <summary>
        /// 添加客户
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1">客户信息</param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void ADDKHGL(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            SZHL_CRM_KHGL khgl = JsonConvert.DeserializeObject<SZHL_CRM_KHGL>(P1);
            if (khgl.KHType == null || khgl.KHType == 0)
            {
                msg.ErrorMsg = "客户类型不能为空";
                return;
            }
            if (string.IsNullOrEmpty(khgl.KHName))
            {
                msg.ErrorMsg = "客户名称不能为空";
                return;
            }
            else
            {
                if (khgl.ID == 0)
                {
                    SZHL_CRM_KHGL sch = new SZHL_CRM_KHGLB().GetEntity(p => p.KHName == khgl.KHName && p.ComId == UserInfo.User.ComId);
                    if (sch != null)
                    {
                        msg.ErrorMsg = "客户名称已经存在";
                        return;
                    }
                }
                else
                {
                    SZHL_CRM_KHGL sch = new SZHL_CRM_KHGLB().GetEntity(p => p.KHName == khgl.KHName && p.ComId == UserInfo.User.ComId && p.ID != khgl.ID);
                    if (sch != null)
                    {
                        msg.ErrorMsg = "客户名称已经存在";
                        return;
                    }
                }
            }
            //if (string.IsNullOrEmpty(khgl.TelePhone))
            //{
            //    msg.ErrorMsg = "客户手机号不能为空";
            //    return;
            //}
            if (string.IsNullOrEmpty(khgl.FZUser))
            {
                msg.ErrorMsg = "负责人不能为空";
                return;
            }
            if (P2 != "") // 处理微信上传的图片
            {
                string fids = new FT_FileB().ProcessWxIMG(P2, "CRM", UserInfo);

                if (!string.IsNullOrEmpty(khgl.Files))
                {
                    khgl.Files += "," + fids;
                }
                else
                {
                    khgl.Files = fids;
                }
            }

            if (khgl.ID == 0)
            {
                khgl.CRDate = DateTime.Now;
                khgl.CRUser = UserInfo.User.UserName;
                khgl.ComId = UserInfo.User.ComId;
                new SZHL_CRM_KHGLB().Insert(khgl);

                string strLXR = context.Request["lxr"] ?? "";
                SZHL_CRM_CONTACT khlxr = JsonConvert.DeserializeObject<SZHL_CRM_CONTACT>(strLXR);
                if (!string.IsNullOrEmpty(khlxr.UserXM) && !string.IsNullOrEmpty(khlxr.TelePhone))
                {
                    khlxr.KHID = khgl.ID;
                    khlxr.CRDate = DateTime.Now;
                    khlxr.CRUser = UserInfo.User.UserName;
                    khlxr.ComId = UserInfo.User.ComId;
                    new SZHL_CRM_CONTACTB().Insert(khlxr);
                }
            }
            else
            {
                new SZHL_CRM_KHGLB().Update(khgl);
            }
            msg.Result = khgl;
        }
        #endregion

        #region 删除客户
        /// <summary>
        /// 删除客户
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1">ID</param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void DELKHGL(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            try
            {
                int id = Int32.Parse(P1);
                if (new SZHL_CRM_KHGLB().Delete(d => d.ID == id))
                {
                    if (new SZHL_CRM_CONTACTB().Delete(d => d.KHID == id) && new SZHL_CRM_GJJLB().Delete(d => d.KHID == id && d.TypeCode == "KHGL") && new SZHL_CRM_HTGLB().Delete(d => d.KHID == id))
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
        #endregion

        #region 统计数量
        /// <summary>
        /// 统计数量
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void GETCOUNT(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            msg.Result = new SZHL_CRM_KHGLB().GetEntities(" ComId='" + UserInfo.User.ComId + "' and FZUser='" + UserInfo.User.UserName + "' ").Count();
            msg.Result1 = new SZHL_CRM_CONTACTB().GetEntities(" ComId='" + UserInfo.User.ComId + "' and CRUser='" + UserInfo.User.UserName + "' ").Count();

            string Users = new JH_Auth_UserB().GetUserBranchUsers(UserInfo.User.ComId.Value, UserInfo.User.UserName).Select(d => d.UserName).ToList().ListTOString(',');
            msg.Result2 = new SZHL_CRM_KHGLB().GetEntities(" ComId='" + UserInfo.User.ComId + "' and (CRUser in ('" + Users.ToFormatLike() + "') or FZUser in ('" + Users.ToFormatLike() + "')) ").Count();
        }
        #endregion

        #region 导入客户
        /// <summary>
        /// 导入客户
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1">客户信息</param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void IMPORTKH(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            try
            {
                string strmsg = string.Empty;
                int n = 0;
                List<JObject> ss = JsonConvert.DeserializeObject<List<JObject>>(P1);
                foreach (var s in ss)
                {
                    if (s["status"] != null && s["status"].ToString() == "1")
                    {
                        SZHL_CRM_KHGL khgl = new SZHL_CRM_KHGL();

                      
                        if (s["客户名称"] != null)
                        {
                            khgl.KHName = s["客户名称"].ToString();

                            var kh = new SZHL_CRM_KHGLB().GetEntities("ComId='" + UserInfo.QYinfo.ComId + "' and KHName='" + khgl.KHName + "'").FirstOrDefault();
                            if (kh != null)
                            {
                                strmsg = strmsg + khgl.KHName + "已经存在，未导入此客户！";
                                continue;
                            }
                        }

                        #region 客户类型
                        if (s["客户类型"] != null)
                        {
                            string strtype = s["客户类型"].ToString();
                            if (strtype != "")
                            {
                                var type = new JH_Auth_ZiDianB().GetEntities(" ComId='" + UserInfo.QYinfo.ComId + "' and Class=10 and TypeName='" + strtype + "' ").FirstOrDefault();
                                if (type != null)
                                {
                                    khgl.KHType = type.ID;
                                }
                                else
                                {
                                    JH_Auth_ZiDian jaz = new JH_Auth_ZiDian();
                                    jaz.ComId = UserInfo.QYinfo.ComId;
                                    jaz.Class = 10;
                                    jaz.TypeName = strtype;
                                    jaz.Remark = "0";
                                    jaz.CRUser = UserInfo.User.UserName;
                                    jaz.CRDate = DateTime.Now;

                                    new JH_Auth_ZiDianB().Insert(jaz);

                                    khgl.KHType = jaz.ID;
                                }
                            }
                        }
                        #endregion
                        #region 跟进状态
                        if (s["跟进状态"] != null)
                        {
                            string strgjzt = s["跟进状态"].ToString();
                            if (strgjzt != "")
                            {
                                var type = new JH_Auth_ZiDianB().GetEntities(" ComId='" + UserInfo.QYinfo.ComId + "' and Class=11 and TypeName='" + strgjzt + "' ").FirstOrDefault();
                                if (type != null)
                                {
                                    khgl.Status = type.ID;
                                }
                                else
                                {
                                    JH_Auth_ZiDian jaz = new JH_Auth_ZiDian();
                                    jaz.ComId = UserInfo.QYinfo.ComId;
                                    jaz.Class = 11;
                                    jaz.TypeName = strgjzt;
                                    jaz.Remark = "0";
                                    jaz.CRUser = UserInfo.User.UserName;
                                    jaz.CRDate = DateTime.Now;

                                    new JH_Auth_ZiDianB().Insert(jaz);

                                    khgl.Status = jaz.ID;
                                }
                            }
                        }
                        #endregion
                        #region 客户来源
                        if (s["客户来源"] != null)
                        {
                            string strkhly = s["客户来源"].ToString();
                            if (strkhly != "")
                            {
                                var type = new JH_Auth_ZiDianB().GetEntities(" ComId='" + UserInfo.QYinfo.ComId + "' and Class=12 and TypeName='" + strkhly + "' ").FirstOrDefault();
                                if (type != null)
                                {
                                    khgl.Source = type.ID;
                                }
                                else
                                {
                                    JH_Auth_ZiDian jaz = new JH_Auth_ZiDian();
                                    jaz.ComId = UserInfo.QYinfo.ComId;
                                    jaz.Class = 12;
                                    jaz.TypeName = strkhly;
                                    jaz.Remark = "0";
                                    jaz.CRUser = UserInfo.User.UserName;
                                    jaz.CRDate = DateTime.Now;

                                    new JH_Auth_ZiDianB().Insert(jaz);

                                    khgl.Source = jaz.ID;
                                }
                            }
                        }
                        #endregion
                        #region 所属行业
                        if (s["所属行业"] != null)
                        {
                            string strsshy = s["所属行业"].ToString();
                            if (strsshy != "")
                            {
                                var type = new JH_Auth_ZiDianB().GetEntities(" ComId='" + UserInfo.QYinfo.ComId + "' and Class=13 and TypeName='" + strsshy + "' ").FirstOrDefault();
                                if (type != null)
                                {
                                    khgl.Industry = type.ID;
                                }
                                else
                                {
                                    JH_Auth_ZiDian jaz = new JH_Auth_ZiDian();
                                    jaz.ComId = UserInfo.QYinfo.ComId;
                                    jaz.Class = 13;
                                    jaz.TypeName = strsshy;
                                    jaz.Remark = "0";
                                    jaz.CRUser = UserInfo.User.UserName;
                                    jaz.CRDate = DateTime.Now;

                                    new JH_Auth_ZiDianB().Insert(jaz);

                                    khgl.Industry = jaz.ID;
                                }
                            }
                        }
                        #endregion
                        #region 人员规模
                        if (s["人员规模"] != null)
                        {
                            string strrygm = s["人员规模"].ToString();
                            if (strrygm != "")
                            {
                                var type = new JH_Auth_ZiDianB().GetEntities(" ComId='" + UserInfo.QYinfo.ComId + "' and Class=14 and TypeName='" + strrygm + "' ").FirstOrDefault();
                                if (type != null)
                                {
                                    khgl.Scale = type.ID;
                                }
                                else
                                {
                                    JH_Auth_ZiDian jaz = new JH_Auth_ZiDian();
                                    jaz.ComId = UserInfo.QYinfo.ComId;
                                    jaz.Class = 14;
                                    jaz.TypeName = strrygm;
                                    jaz.Remark = "0";
                                    jaz.CRUser = UserInfo.User.UserName;
                                    jaz.CRDate = DateTime.Now;

                                    new JH_Auth_ZiDianB().Insert(jaz);

                                    khgl.Scale = jaz.ID;
                                }
                            }
                        }
                        #endregion
                        #region 负责人
                        if (s["负责人"] != null)
                        {
                            string strfzr = s["负责人"].ToString();
                            if (strfzr != "")
                            {
                                var fzr = new JH_Auth_UserB().GetEntities("ComId='" + UserInfo.QYinfo.ComId + "' and ( UserName='" + strfzr + "' or mobphone='" + strfzr + "')").FirstOrDefault();
                                if (fzr != null)
                                {
                                    khgl.FZUser = fzr.UserName;
                                }
                            }
                        }
                        #endregion


                        //if (s["电话（必填）"] != null)
                        //{
                        //    khgl.TelePhone = s["电话（必填）"].ToString();
                        //}
                        if (s["电话"] != null)
                        {
                            khgl.TelePhone = s["电话"].ToString();
                        }
                        if (s["传真"] != null)
                        {
                            khgl.FixNo = s["传真"].ToString();
                        }
                        if (s["网址"] != null)
                        {
                            khgl.WebSite = s["网址"].ToString();
                        }
                        if (s["邮箱"] != null)
                        {
                            khgl.Email = s["邮箱"].ToString();
                        }
                        //if (s["省"] != null)
                        //{
                        //    khgl.Province = s["省"].ToString();
                        //}
                        //if (s["市"] != null)
                        //{
                        //    khgl.City = s["市"].ToString();
                        //}
                        //if (s["区"] != null)
                        //{
                        //    khgl.District = s["区"].ToString();
                        //}
                        if (s["地址"] != null)
                        {
                            khgl.Address = s["地址"].ToString();
                        }
                        if (s["备注"] != null)
                        {
                            khgl.Remark = s["备注"].ToString();
                        }
                        if (s["邮编"] != null)
                        {
                            khgl.PostCode = s["邮编"].ToString();
                        }

                        khgl.CRDate = DateTime.Now;
                        khgl.CRUser = UserInfo.User.UserName;
                        khgl.ComId = UserInfo.User.ComId;

                        new SZHL_CRM_KHGLB().Insert(khgl);

                        DataTable dtExtColumn = new JH_Auth_ExtendModeB().GetExtColumnAll(UserInfo.QYinfo.ComId, "KHGL");
                        foreach (DataRow drExt in dtExtColumn.Rows)
                        {
                            string strValue = string.Empty;
                            if (s[drExt["TableFiledName"].ToString()] != null)
                            {
                                strValue = s[drExt["TableFiledName"].ToString()].ToString();
                            }

                            JH_Auth_ExtendData jext = new JH_Auth_ExtendData();
                            jext.ComId = UserInfo.QYinfo.ComId;
                            jext.TableName = "KHGL";
                            jext.DataID = khgl.ID;
                            jext.ExtendModeID = Int32.Parse(drExt["ID"].ToString());
                            jext.ExtendDataValue = strValue;
                            jext.CRUser = UserInfo.User.UserName;
                            jext.CRDate = DateTime.Now;

                            new JH_Auth_ExtendDataB().Insert(jext);
                        }
                        n = n + 1;
                    }
                }
                msg.Result = strmsg;
                msg.Result1 = n;
            }
            catch
            {
                msg.ErrorMsg = "导入失败！";
            }
        }
        #endregion

        #region 导出客户
        /// <summary>
        /// 导出客户
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1">客户信息</param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void EXPORTKH(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            try
            {
                string userName = UserInfo.User.UserName;
                string strWhere = " 1=1 and ComId=" + UserInfo.User.ComId;

                string leibie = context.Request["lb"] ?? "";
                if (leibie != "")
                {
                    strWhere += string.Format(" And KHType='{0}' ", leibie);
                }
                string strContent = context.Request["Content"] ?? "";
                if (strContent != "")
                {
                    string extwhere = string.Empty;
                    string extids = new JH_Auth_ExtendDataB().GetExtIds(Int32.Parse(UserInfo.User.ComId.ToString()), "KHGL", strContent);
                    if (extids != "")
                    {
                        extwhere = " or ID in (" + extids + ")";
                    }
                    strWhere += string.Format(" And ( KHName like '%{0}%' " + extwhere + ")", strContent);
                }

                if (P2 != "")
                {
                    string[] strs = P2.Split('_');

                    if (!string.IsNullOrEmpty(strs[0]))
                    {
                        strWhere += string.Format(" And Status='{0}' ", strs[0]);
                    }
                    if (!string.IsNullOrEmpty(strs[1]))
                    {
                        strWhere += string.Format(" And Source='{0}' ", strs[1]);
                    }
                    if (!string.IsNullOrEmpty(strs[2]))
                    {
                        strWhere += string.Format(" And Industry='{0}' ", strs[2]);
                    }
                    if (!string.IsNullOrEmpty(strs[3]))
                    {
                        strWhere += string.Format(" And Scale='{0}' ", strs[3]);
                    }
                }

                int DataID = -1;
                int.TryParse(context.Request["ID"] ?? "-1", out DataID);//记录Id
                if (DataID != -1)
                {
                    string strIsHasDataQX = new JH_Auth_QY_ModelB().ISHASDATAREADQX("CRM", DataID, UserInfo);
                    if (strIsHasDataQX == "Y")
                    {
                        strWhere += string.Format(" And ID = '{0}'", DataID);
                    }

                }

                if (P1 != "")
                {
                    DataTable dt = new DataTable();
                    switch (P1)
                    {
                        case "0": //手机单条数据
                            {
                                //设置usercenter已读
                                new JH_Auth_User_CenterB().ReadMsg(UserInfo, DataID, "CRM");
                            }
                            break;
                        case "1": //创建的
                            {
                                strWhere += " And CRUser ='" + userName + "'";
                            }
                            break;
                        case "2": //负责的
                            {
                                strWhere += " And FZUser ='" + userName + "'";
                            }
                            break;
                        case "3": //下属的
                            {
                                //获取当前登录人负责的下属人员 
                                string Users = new JH_Auth_UserB().GetUserBranchUsers(UserInfo.User.ComId.Value, UserInfo.User.UserName).Select(d => d.UserName).ToList().ListTOString(',');
                                strWhere += string.Format(" and (CRUser in ('{0}') or FZUser in ('{0}')) ", Users.ToFormatLike());
                            }
                            break;
                    }
                    string strCls = string.Empty;
                    strCls = "ID,KHName '客户名称',dbo.fn_ZDName(KHType) '客户类型',TelePhone '电话',Email '邮箱',FixNo '传真',WebSite '网址',"
                        //+ "Province '省', City '市', District '区',Address '地址',PostCode '邮编',dbo.fn_ZDName(Status) '跟进状态',dbo.fn_ZDName(Source) '客户来源',"
                        + "Address '地址',PostCode '邮编',dbo.fn_ZDName(Status) '跟进状态',dbo.fn_ZDName(Source) '客户来源',"
                        + "dbo.fn_ZDName(Industry) '所属行业',dbo.fn_ZDName(Scale) '人员规模',dbo.fn_YHName(ComId,FZUser) '负责人',dbo.clearhtml(Remark) '备注'";
                    dt = new SZHL_CRM_KHGLB().GetDTByCommand("select " + strCls + " from SZHL_CRM_KHGL where " + strWhere + " order by CRDate desc");

                    //msg.Result = dt;

                    DataTable dtExtColumn = new JH_Auth_ExtendModeB().GetExtColumnAll(UserInfo.QYinfo.ComId, "KHGL");
                    foreach (DataRow drExt in dtExtColumn.Rows)
                    {
                        dt.Columns.Add(drExt["TableFiledName"].ToString(), Type.GetType("System.String"));
                    }

                    if (dtExtColumn.Rows.Count > 0)
                    {
                        foreach (DataRow dr in dt.Rows)
                        {
                            DataTable dtExtData = new JH_Auth_ExtendModeB().GetExtDataAll(UserInfo.QYinfo.ComId, "KHGL", dr["ID"].ToString());
                            foreach (DataRow drExtData in dtExtData.Rows)
                            {
                                dr[drExtData["TableFiledName"].ToString()] = drExtData["ExtendDataValue"].ToString();
                            }
                        }
                    }
                    dt.Columns.Remove("ID");

                    CommonHelp ch = new CommonHelp();
                    msg.ErrorMsg = ch.ExportToExcel("客户", dt);
                }
            }
            catch (Exception ex)
            {
                msg.ErrorMsg = ex.Message.ToString();
            }
        }
        #endregion

        #region 添加客户只有客户名称
        /// <summary>
        /// 添加客户只有客户名称
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1">ID</param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void ADDKHBYNAME(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            try
            {
                SZHL_CRM_KHGL sch = new SZHL_CRM_KHGLB().GetEntity(p => p.KHName == P1 && p.ComId == UserInfo.User.ComId);
                if (sch == null)
                {
                    SZHL_CRM_KHGL kh = new SZHL_CRM_KHGL();
                    kh.KHName = P1;
                    kh.FZUser = UserInfo.User.UserName;
                    kh.CRDate = DateTime.Now;
                    kh.CRUser = UserInfo.User.UserName;
                    kh.ComId = UserInfo.User.ComId;
                    new SZHL_CRM_KHGLB().Insert(kh);
                    msg.Result = kh;
                }
                else
                {
                    msg.ErrorMsg = "客户名称已经存在";
                    return;
                }
            }
            catch (Exception)
            {
                msg.ErrorMsg = "添加失败";
            }
        }
        #endregion

        #region 获取微信客户
        /// <summary>
        /// 获取微信客户
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void GETWXKH(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {

            List<WXKHLX> list = new List<WXKHLX>();
            //获取所有类型
            var listALL = new JH_Auth_ZiDianB().GetEntities(p => p.ComId == UserInfo.User.ComId && p.Class == 10 && p.Remark == "0").ToList();

            string userName = UserInfo.User.UserName;
            string strWhere = " 1=1 and ComId=" + UserInfo.User.ComId;
            string Users = new JH_Auth_UserB().GetUserBranchUsers(UserInfo.User.ComId.Value, UserInfo.User.UserName).Select(d => d.UserName).ToList().ListTOString(',');

            strWhere += " And (CRUser ='" + userName + "' or FZUser ='" + userName + "' or CRUser in ('" + Users.ToFormatLike() + "') or FZUser in ('" + Users.ToFormatLike() + "'))";

            foreach (var v in listALL)
            {
                WXKHLX wx = new WXKHLX();
                wx.ID = v.ID;
                wx.LXName = v.TypeName;
                var users = new SZHL_CRM_KHGLB().GetEntities(strWhere + " and KHType='" + v.ID + "'");
                wx.LXKH = users;
                wx.LXKHNum = users.Count();
                list.Add(wx);
            }

            msg.Result = list;

            msg.Result1 = new SZHL_CRM_KHGLB().GetEntities(strWhere + " and isnull(KHType,'')=''");

        }
        public class WXKHLX
        {
            public int ID { get; set; }
            public string LXName { get; set; }
            public dynamic LXKH { get; set; }
            public int LXKHNum { get; set; }
        }
        #endregion

        #endregion

        #region 客户联系人管理

        #region 所有客户
        /// <summary>
        /// 所有客户
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void GETALLKH(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            string userName = UserInfo.User.UserName;
            string strWhere = " 1=1 and ComId=" + UserInfo.User.ComId;
            string Users = new JH_Auth_UserB().GetUserBranchUsers(UserInfo.User.ComId.Value, UserInfo.User.UserName).Select(d => d.UserName).ToList().ListTOString(',');

            strWhere += " And (CRUser ='" + userName + "' or FZUser ='" + userName + "' or CRUser in ('" + Users.ToFormatLike() + "') or FZUser in ('" + Users.ToFormatLike() + "'))";


            msg.Result = new SZHL_CRM_KHGLB().GetEntities(strWhere);
        }
        #endregion

        #region 所有客户联系人
        /// <summary>
        /// 所有客户联系人
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void GETALLKHLXR(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            string userName = UserInfo.User.UserName;
            string strWhere = " 1=1 and lxr.ComId=" + UserInfo.User.ComId;
            string Users = new JH_Auth_UserB().GetUserBranchUsers(UserInfo.User.ComId.Value, UserInfo.User.UserName).Select(d => d.UserName).ToList().ListTOString(',');

            strWhere += " And (kh.CRUser ='" + userName + "' or kh.FZUser ='" + userName + "' or kh.CRUser in ('" + Users.ToFormatLike() + "') or kh.FZUser in ('" + Users.ToFormatLike() + "'))";


            msg.Result = new SZHL_CRM_KHGLB().GetDTByCommand("select lxr.*,kh.KHName from SZHL_CRM_CONTACT lxr left join SZHL_CRM_KHGL kh on lxr.KHID = kh.ID where " + strWhere);
        }
        #endregion

        #region 客户联系人列表
        /// <summary>
        /// 客户联系人列表
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void GETKHLXRLIST(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            string userName = UserInfo.User.UserName;
            string strWhere = " 1=1 and lxr.ComId=" + UserInfo.User.ComId;

            string leibie = context.Request["lb"] ?? "";
            if (leibie != "")
            {
                strWhere += string.Format(" And lxr.KHID='{0}' ", leibie);
            }
            string strContent = context.Request["Content"] ?? "";
            strContent = strContent.TrimEnd();
            if (strContent != "")
            {
                string extwhere = string.Empty;
                string extids = new JH_Auth_ExtendDataB().GetExtIds(Int32.Parse(UserInfo.User.ComId.ToString()), "KHLXR", strContent);
                if (extids != "")
                {
                    extwhere = " or lxr.ID in (" + extids + ")";
                }

                strWhere += string.Format(" And ( lxr.UserXM like '%{0}%' " + extwhere + " )", strContent);
            }
            int DataID = -1;
            int.TryParse(context.Request["ID"] ?? "-1", out DataID);//记录Id
            if (DataID != -1)
            {
                string strIsHasDataQX = new JH_Auth_QY_ModelB().ISHASDATAREADQX("CRM", DataID, UserInfo);
                if (strIsHasDataQX == "Y")
                {
                    strWhere += string.Format(" And lxr.ID = '{0}'", DataID);
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
                            new JH_Auth_User_CenterB().ReadMsg(UserInfo, DataID, "KHLXR");
                        }
                        break;
                    case "1": //我的
                        {
                            strWhere += " And lxr.CRUser='" + userName + "'";
                        }
                        break;
                    case "2": //下属
                        {
                            //获取当前登录人负责的下属人员 
                            string Users = new JH_Auth_UserB().GetUserBranchUsers(UserInfo.User.ComId.Value, UserInfo.User.UserName).Select(d => d.UserName).ToList().ListTOString(',');
                            strWhere += string.Format(" and lxr.CRUser in ('{0}') ", Users.ToFormatLike());
                        }
                        break;
                }
                dt = new SZHL_CRM_CONTACTB().GetDataPager("SZHL_CRM_CONTACT lxr left join SZHL_CRM_KHGL kh on lxr.KHID=kh.ID", "lxr.*,kh.KHName ", pagecount, page, " lxr.CRDate desc", strWhere, ref total);


                if (dt.Rows.Count > 0)
                {
                    dt.Columns.Add("FileList", Type.GetType("System.Object"));
                    dt.Columns.Add("SubExt", typeof(DataTable));

                    DataTable dtExt = new JH_Auth_ExtendModeB().GetExtColumnAll(UserInfo.User.ComId, "KHLXR");
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
                        DataTable dtExtData = new JH_Auth_ExtendModeB().GetExtData(UserInfo.User.ComId, "KHLXR", dr["ID"].ToString());
                        dr["SubExt"] = dtExtData;
                        foreach (DataRow drExtData in dtExtData.Rows)
                        {
                            dr[drExtData["TableFiledName"].ToString()] = drExtData["ExtendDataValue"].ToString();
                        }
                        #endregion
                    }
                }
                msg.Result = dt;
                msg.Result1 = total;
            }
        }
        #endregion

        #region 获取客户联系人信息
        /// <summary>
        /// 获取客户联系人信息
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void GETKHLXRMODEL(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            int Id = int.Parse(P1);
            SZHL_CRM_CONTACT khgl = new SZHL_CRM_CONTACTB().GetEntity(d => d.ID == Id);
            msg.Result = khgl;
            if (khgl != null)
            {
                var zd = new SZHL_CRM_KHGLB().GetEntity(p => p.ID == khgl.KHID);
                if (zd != null)
                {
                    msg.Result1 = zd.KHName;
                }
                if (!string.IsNullOrEmpty(khgl.Files))
                {
                    msg.Result2 = new FT_FileB().GetEntities(" ID in (" + khgl.Files + ")");
                }
                new JH_Auth_User_CenterB().ReadMsg(UserInfo, khgl.ID, "KHLXR");
            }

        }
        #endregion

        #region 添加客户联系人
        /// <summary>
        /// 添加客户联系人
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1">客户信息</param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void ADDKHLXR(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            SZHL_CRM_CONTACT khlxr = JsonConvert.DeserializeObject<SZHL_CRM_CONTACT>(P1);
            if (string.IsNullOrEmpty(khlxr.UserXM))
            {
                msg.ErrorMsg = "姓名不能为空";
                return;
            }
            if (string.IsNullOrEmpty(khlxr.TelePhone) && string.IsNullOrEmpty(khlxr.MobilePhone))
            {
                msg.ErrorMsg = "手机号不能为空";
                return;
            }
            if (P2 != "") // 处理微信上传的图片
            {
                string fids = new FT_FileB().ProcessWxIMG(P2, "CRM", UserInfo);
                if (!string.IsNullOrEmpty(khlxr.Files))
                {
                    khlxr.Files += "," + fids;
                }
                else
                {
                    khlxr.Files = fids;
                }
            }
            if (khlxr.ID == 0)
            {
                khlxr.CRDate = DateTime.Now;
                khlxr.CRUser = UserInfo.User.UserName;
                khlxr.ComId = UserInfo.User.ComId;
                new SZHL_CRM_CONTACTB().Insert(khlxr);
              
            }
            else
            {
                new SZHL_CRM_CONTACTB().Update(khlxr);
            }
            msg.Result = khlxr;
        }
        #endregion

        #region 删除客户联系人
        /// <summary>
        /// 删除客户联系人
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1">ID</param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void DELKHLXR(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            try
            {
                int id = Int32.Parse(P1);
                new SZHL_CRM_CONTACTB().Delete(d => d.ID == id);
            }
            catch (Exception ex)
            {
                msg.ErrorMsg = ex.Message;
            }
        }
        #endregion

        #region 导入联系人
        /// <summary>
        /// 导入联系人
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1">客户信息</param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void IMPORTLXR(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            try
            {
                int n = 0;
                List<JObject> ss = JsonConvert.DeserializeObject<List<JObject>>(P1);
                foreach (var s in ss)
                {
                    if (s["status"] != null && s["status"].ToString() == "1")
                    {
                        SZHL_CRM_CONTACT khlxr = new SZHL_CRM_CONTACT();

                       
                        if (s["对应客户"] != null)
                        {
                            string strkh = s["对应客户"].ToString();
                            var kh = new SZHL_CRM_KHGLB().GetEntities("ComId='" + UserInfo.QYinfo.ComId + "' and KHName='" + strkh + "'").FirstOrDefault();
                            if (kh != null)
                            {
                                khlxr.KHID = kh.ID;
                            }
                        }

                        if (s["姓名"] != null)
                        {
                            khlxr.UserXM = s["姓名"].ToString();
                        }
                        if (s["性别"] != null)
                        {
                            khlxr.UserSex = s["性别"].ToString();
                        }
                     
                        if (s["手机"] != null)
                        {
                            khlxr.TelePhone = s["手机"].ToString();
                        }
                        if (s["电话"] != null)
                        {
                            khlxr.MobilePhone = s["电话"].ToString();
                        }
                        if (s["分机"] != null)
                        {
                            khlxr.Extension = s["分机"].ToString();
                        }
                        if (s["传真"] != null)
                        {
                            khlxr.FixNo = s["传真"].ToString();
                        }
                        if (s["网址"] != null)
                        {
                            khlxr.WebSite = s["网址"].ToString();
                        }
                        if (s["邮箱"] != null)
                        {
                            khlxr.EMail = s["邮箱"].ToString();
                        }
                        if (s["QQ"] != null)
                        {
                            khlxr.QQ = s["QQ"].ToString();
                        }
                        if (s["微信"] != null)
                        {
                            khlxr.Weixin = s["微信"].ToString();
                        }
                        if (s["邮编"] != null)
                        {
                            khlxr.PostCode = s["邮编"].ToString();
                        }
                        if (s["学历"] != null)
                        {
                            khlxr.Education = s["学历"].ToString();
                        }
                        if (s["公司"] != null)
                        {
                            khlxr.Company = s["公司"].ToString();
                        }
                        if (s["部门"] != null)
                        {
                            khlxr.Department = s["部门"].ToString();
                        }
                        if (s["职位"] != null)
                        {
                            khlxr.Position = s["职位"].ToString();
                        }
               
                        if (s["地址"] != null)
                        {
                            khlxr.Address = s["地址"].ToString();
                        }
                        if (s["备注"] != null)
                        {
                            khlxr.Remark = s["备注"].ToString();
                        }

                        if (s["生日"] != null)
                        {
                            string strb = s["生日"].ToString();
                            if (strb != "")
                            {
                                try
                                {
                                    DateTime ti = DateTime.Parse(s["生日"].ToString() + " 00:00:00");
                                    khlxr.Birthday = ti;
                                }
                                catch { }
                            }
                        }

                        khlxr.CRDate = DateTime.Now;
                        khlxr.CRUser = UserInfo.User.UserName;
                        khlxr.ComId = UserInfo.User.ComId;

                        new SZHL_CRM_CONTACTB().Insert(khlxr);

                        DataTable dtExtColumn = new JH_Auth_ExtendModeB().GetExtColumnAll(UserInfo.QYinfo.ComId, "KHLXR");
                        foreach (DataRow drExt in dtExtColumn.Rows)
                        {
                            string strValue = string.Empty;
                            if (s[drExt["TableFiledName"].ToString()] != null)
                            {
                                strValue = s[drExt["TableFiledName"].ToString()].ToString();
                            }

                            JH_Auth_ExtendData jext = new JH_Auth_ExtendData();
                            jext.ComId = UserInfo.QYinfo.ComId;
                            jext.TableName = "KHLXR";
                            jext.DataID = khlxr.ID;
                            jext.ExtendModeID = Int32.Parse(drExt["ID"].ToString());
                            jext.ExtendDataValue = strValue;
                            jext.CRUser = UserInfo.User.UserName;
                            jext.CRDate = DateTime.Now;

                            new JH_Auth_ExtendDataB().Insert(jext);
                        }
                        n = n + 1;
                    }
                }
                msg.Result1 = n;
            }
            catch
            {
            }
        }
        #endregion

        #region 导出联系人
        /// <summary>
        /// 导出联系人
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1">客户信息</param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void EXPORTKHLXR(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            try
            {
                string userName = UserInfo.User.UserName;
                string strWhere = " 1=1 and lxr.ComId=" + UserInfo.User.ComId;

                string leibie = context.Request["lb"] ?? "";
                if (leibie != "")
                {
                    strWhere += string.Format(" And lxr.KHID='{0}' ", leibie);
                }
                string strContent = context.Request["Content"] ?? "";
                if (strContent != "")
                {
                    strWhere += string.Format(" And ( lxr.UserXM like '%{0}%' )", strContent);
                }
                int DataID = -1;
                int.TryParse(context.Request["ID"] ?? "-1", out DataID);//记录Id
                if (DataID != -1)
                {
                    string strIsHasDataQX = new JH_Auth_QY_ModelB().ISHASDATAREADQX("CRM", DataID, UserInfo);
                    if (strIsHasDataQX == "Y")
                    {
                        strWhere += string.Format(" And lxr.ID = '{0}'", DataID);
                    }

                }

                if (P1 != "")
                {
                    DataTable dt = new DataTable();
                    switch (P1)
                    {
                        case "0": //手机单条数据
                            {
                                //设置usercenter已读
                                new JH_Auth_User_CenterB().ReadMsg(UserInfo, DataID, "KHLXR");
                            }
                            break;
                        case "1": //我的
                            {
                                strWhere += " And lxr.CRUser='" + userName + "'";
                            }
                            break;
                        case "2": //下属
                            {
                                //获取当前登录人负责的下属人员 
                                string Users = new JH_Auth_UserB().GetUserBranchUsers(UserInfo.User.ComId.Value, UserInfo.User.UserName).Select(d => d.UserName).ToList().ListTOString(',');
                                strWhere += string.Format(" and lxr.CRUser in ('{0}') ", Users.ToFormatLike());
                            }
                            break;
                    }
                    string strCls = string.Empty;
                    strCls = "lxr.ID,lxr.UserXM '姓名',kh.KHName '对应客户',lxr.TelePhone '手机',lxr.EMail '邮箱', lxr.FixNo '传真', lxr.WebSite '网址',"
                        + "lxr.MobilePhone '电话',  lxr.Extension '分机', lxr.QQ 'QQ', lxr.Weixin '微信',"// lxr.Education '学历', lxr.Company '公司',"
                        //+ "lxr.Department '部门', lxr.Position '职位',  lxr.Province '省', lxr.City '市', lxr.District '区', lxr.Address '地址', "
                        + "lxr.Department '部门', lxr.Position '职位',   lxr.Address '地址', "
                        + "lxr.PostCode '邮编',lxr.UserSex '性别',case when isnull(lxr.Birthday,'')='' then '' else SUBSTRING(CONVERT(varchar(100), lxr.Birthday, 120), 1,11) end '生日',dbo.clearhtml(lxr.Remark) '备注'";
                    dt = new SZHL_CRM_KHGLB().GetDTByCommand("select " + strCls + " from SZHL_CRM_CONTACT lxr left join SZHL_CRM_KHGL kh on lxr.KHID=kh.ID where " + strWhere + " order by lxr.CRDate desc");

                    //msg.Result = dt;

                    DataTable dtExtColumn = new JH_Auth_ExtendModeB().GetExtColumnAll(UserInfo.QYinfo.ComId, "KHLXR");
                    foreach (DataRow drExt in dtExtColumn.Rows)
                    {
                        dt.Columns.Add(drExt["TableFiledName"].ToString(), Type.GetType("System.String"));
                    }

                    if (dtExtColumn.Rows.Count > 0)
                    {
                        foreach (DataRow dr in dt.Rows)
                        {
                            DataTable dtExtData = new JH_Auth_ExtendModeB().GetExtDataAll(UserInfo.QYinfo.ComId, "KHLXR", dr["ID"].ToString());
                            foreach (DataRow drExtData in dtExtData.Rows)
                            {
                                dr[drExtData["TableFiledName"].ToString()] = drExtData["ExtendDataValue"].ToString();
                            }
                        }
                    }
                    dt.Columns.Remove("ID");

                    CommonHelp ch = new CommonHelp();
                    msg.ErrorMsg = ch.ExportToExcel("客户联系人", dt);
                }
            }
            catch
            {
                msg.ErrorMsg = "导出失败！";
            }
        }
        #endregion

        #endregion

        #region 跟进记录管理

        #region 跟进记录列表
        /// <summary>
        /// 跟进记录列表
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void GETGJJLLIST(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            string userName = UserInfo.User.UserName;
            string strWhere = " 1=1 and typecode='KHGL' and gjjl.ComId=" + UserInfo.User.ComId;

            string leibie = context.Request["lb"] ?? "";
            if (leibie != "")
            {
                strWhere += string.Format(" And gjjl.Status='{0}' ", leibie);
            }
            string strContent = context.Request["Content"] ?? "";
            strContent = strContent.TrimEnd();
            if (strContent != "")
            {
                string extwhere = string.Empty;
                string extids = new JH_Auth_ExtendDataB().GetExtIds(Int32.Parse(UserInfo.User.ComId.ToString()), "GJJL", strContent);
                if (extids != "")
                {
                    extwhere = " or gjjl.ID in (" + extids + ")";
                }

                strWhere += string.Format(" And ( gjjl.Details like '%{0}%' OR kh.KHName like '%{0}%' OR jau.UserRealName  like '%{0}%' " + extwhere + " )", strContent);
            }

            if (P2 != "")
            {
                strWhere += string.Format(" And gjjl.Type='{0}' ", P2);
            }

            int DataID = -1;
            int.TryParse(context.Request["ID"] ?? "-1", out DataID);//记录Id
            if (DataID != -1)
            {
                string strIsHasDataQX = new JH_Auth_QY_ModelB().ISHASDATAREADQX("CRM", DataID, UserInfo);
                if (strIsHasDataQX == "Y")
                {
                    strWhere += string.Format(" And gjjl.ID = '{0}'", DataID);
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
                            new JH_Auth_User_CenterB().ReadMsg(UserInfo, DataID, "GJJL");
                        }
                        break;
                    case "1": //我的
                        {
                            strWhere += " And gjjl.CRUser='" + userName + "'";
                        }
                        break;
                    case "2": //下属
                        {
                            //获取当前登录人负责的下属人员 
                            string Users = new JH_Auth_UserB().GetUserBranchUsers(UserInfo.User.ComId.Value, UserInfo.User.UserName).Select(d => d.UserName).ToList().ListTOString(',');
                            strWhere += string.Format(" and gjjl.CRUser in ('{0}') ", Users.ToFormatLike());
                        }
                        break;
                }
                dt = new SZHL_CRM_CONTACTB().GetDataPager("SZHL_CRM_GJJL gjjl left join SZHL_CRM_KHGL kh on gjjl.KHID=kh.ID left JOIN JH_Auth_User jau ON jau.UserName = gjjl.CRUser and gjjl.ComId = jau.ComId ", "gjjl.*,kh.KHName,jau.UserRealName ", pagecount, page, " gjjl.CRDate desc", strWhere, ref total);

                if (dt.Rows.Count > 0)
                {
                    dt.Columns.Add("FileList", Type.GetType("System.Object"));
                    dt.Columns.Add("GJZT", Type.GetType("System.String"));
                    dt.Columns.Add("SubExt", Type.GetType("System.Object"));//扩展字段

                    DataTable dtExt = new JH_Auth_ExtendModeB().GetExtColumnAll(UserInfo.User.ComId, "GJJL");
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
                        if (dr["Status"] != null && dr["Status"].ToString() != "")
                        {
                            int SId = int.Parse(dr["Status"].ToString());

                            var SS = new JH_Auth_ZiDianB().GetEntity(p => p.ID == SId);
                            if (SS != null)
                            {
                                dr["GJZT"] = SS.TypeName;
                            }
                        }

                        #region 扩展字段
                        DataTable dtExtData = new JH_Auth_ExtendModeB().GetExtData(UserInfo.User.ComId, "GJJL", dr["ID"].ToString());
                        dr["SubExt"] = dtExtData;
                        foreach (DataRow drExtData in dtExtData.Rows)
                        {
                            dr[drExtData["TableFiledName"].ToString()] = drExtData["ExtendDataValue"].ToString();
                        }
                        #endregion
                    }
                }
                msg.Result = dt;
                msg.Result1 = total;
            }
        }
        #endregion

        #region 获取跟进记录信息
        /// <summary>
        /// 获取跟进记录信息
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void GETGJJLMODEL(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            int Id = int.Parse(P1);
            SZHL_CRM_GJJL gjjl = new SZHL_CRM_GJJLB().GetEntity(d => d.ID == Id);
            msg.Result = gjjl;
            if (gjjl != null)
            {
                var zd = new SZHL_CRM_KHGLB().GetEntity(p => p.ID == gjjl.KHID);
                if (zd != null)
                {
                    msg.Result1 = zd.KHName;
                }
                if (!string.IsNullOrEmpty(gjjl.Files))
                {
                    msg.Result2 = new FT_FileB().GetEntities(" ID in (" + gjjl.Files + ")");
                }
                if (gjjl.Status != null)
                {
                    var SS = new JH_Auth_ZiDianB().GetEntity(p => p.ID == gjjl.Status);
                    if (SS != null)
                    {
                        msg.Result3 = SS.TypeName;
                    }
                }
                new JH_Auth_User_CenterB().ReadMsg(UserInfo, gjjl.ID, "GJJL");
            }

        }
        #endregion

        #region 添加跟进记录
        /// <summary>
        /// 添加跟进记录
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1">客户信息</param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void ADDGJJL(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            SZHL_CRM_GJJL gjjl = JsonConvert.DeserializeObject<SZHL_CRM_GJJL>(P1);

            string type = context.Request["gjtype"] ?? "";
            if (gjjl.KHID == null || gjjl.KHID == 0)
            {
                msg.ErrorMsg = "客户不能为空";
                return;
            }
            if (gjjl.Status == null)
            {
                msg.ErrorMsg = "跟进状态不能为空";
                return;
            }
            if (string.IsNullOrEmpty(gjjl.Details))
            {
                msg.ErrorMsg = "跟进描述不能为空";
                return;
            }

            if (P2 != "") // 处理微信上传的图片
            {
                string fids = new FT_FileB().ProcessWxIMG(P2, "CRM", UserInfo);
                if (!string.IsNullOrEmpty(gjjl.Files))
                {
                    gjjl.Files += "," + fids;
                }
                else
                {
                    gjjl.Files = fids;
                }
            }
            if (gjjl.ID == 0)
            {
                if (string.IsNullOrWhiteSpace(type)) type = "KHGL";
                gjjl.TypeCode = type;
                gjjl.CRDate = DateTime.Now;
                gjjl.CRUser = UserInfo.User.UserName;
                gjjl.ComId = UserInfo.User.ComId;
                new SZHL_CRM_GJJLB().Insert(gjjl);

            }
            else
            {
                new SZHL_CRM_GJJLB().Update(gjjl);
            }

            if (gjjl.TypeCode == "HTGL")
            {
                SZHL_CRM_HTGL ht = new SZHL_CRM_HTGLB().GetEntity(d => d.ID == gjjl.KHID);
                ht.HTStatus = gjjl.Status.ToString();
                new SZHL_CRM_HTGLB().Update(ht);
            }
            else if (gjjl.TypeCode == "KHGL")
            {
                SZHL_CRM_KHGL ht = new SZHL_CRM_KHGLB().GetEntity(d => d.ID == gjjl.KHID);
                ht.Status = gjjl.Status;
                new SZHL_CRM_KHGLB().Update(ht);
            }

            msg.Result = gjjl;
        }
        #endregion

        #region 删除跟进记录
        /// <summary>
        /// 删除跟进记录
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1">ID</param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void DELGJJL(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            try
            {
                int id = Int32.Parse(P1);
                new SZHL_CRM_GJJLB().Delete(d => d.ID == id);
            }
            catch (Exception ex)
            {
                msg.ErrorMsg = ex.Message;
            }
        }
        #endregion

        #endregion

        #region 产品管理

        #region 获取产品列表
        /// <summary>
        /// 获取产品列表
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void GETCPLIST(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            int page = 0;
            int pagecount = 8;
            int.TryParse(context.Request["p"] ?? "1", out page);//页码
            int.TryParse(context.Request["pagecount"] ?? "8", out pagecount);//页数
            page = page == 0 ? 1 : page;
            int recordCount = 0;
            string strWhere = string.Format(" cp.ComId={0} ", UserInfo.User.ComId);

            string leibie = context.Request["lb"] ?? "";
            if (leibie != "")
            {
                strWhere += string.Format(" And cp.LeiBie='{0}' ", leibie);
            }
            string strContent = context.Request["Content"] ?? "";
            strContent = strContent.TrimEnd();
            if (strContent != "")
            {
                string extwhere = string.Empty;
                string extids = new JH_Auth_ExtendDataB().GetExtIds(Int32.Parse(UserInfo.User.ComId.ToString()), "CPGL", strContent);
                if (extids != "")
                {
                    extwhere = " or cp.ID in (" + extids + ")";
                }

                strWhere += string.Format(" And ( cp.Name like '%{0}%' " + extwhere + " )", strContent);
            }

            int DataID = -1;
            int.TryParse(context.Request["ID"] ?? "-1", out DataID);//记录Id
            if (DataID != -1)
            {
                string strIsHasDataQX = new JH_Auth_QY_ModelB().ISHASDATAREADQX("CRM", DataID, UserInfo);
                if (strIsHasDataQX == "Y")
                {
                    strWhere += string.Format(" And cp.ID = '{0}'", DataID);
                }

            }
            DataTable dt = new SZHL_CRM_CPGLB().GetDataPager(" SZHL_CRM_CPGL cp inner join JH_Auth_ZiDian zd on LeiBie= zd.ID and Class=15  ",
                " cp.*,zd.TypeName "
                , pagecount, page, "cp.CRDate desc", strWhere, ref recordCount);

            #region 附件
            string Ids = "";
            string fileIDs = "";

            dt.Columns.Add("SubExt", typeof(DataTable));

            DataTable dtExt = new JH_Auth_ExtendModeB().GetExtColumnAll(UserInfo.User.ComId, "CPGL");
            foreach (DataRow drExt in dtExt.Rows)
            {
                dt.Columns.Add(drExt["TableFiledName"].ToString(), Type.GetType("System.String"));
            }
            foreach (DataRow row in dt.Rows)
            {
                Ids += row["ID"].ToString() + ",";
                if (!string.IsNullOrEmpty(row["Files"].ToString()))
                {
                    fileIDs += row["Files"].ToString() + ",";
                }

                #region 扩展字段
                DataTable dtExtData = new JH_Auth_ExtendModeB().GetExtData(UserInfo.User.ComId, "CPGL", row["ID"].ToString());
                row["SubExt"] = dtExtData;
                foreach (DataRow drExtData in dtExtData.Rows)
                {
                    row[drExtData["TableFiledName"].ToString()] = drExtData["ExtendDataValue"].ToString();
                }
                #endregion

            }
            Ids = Ids.TrimEnd(',');
            fileIDs = fileIDs.TrimEnd(',');
            if (Ids != "")
            {
                List<FT_File> FileList = new List<FT_File>();
                if (!string.IsNullOrEmpty(fileIDs))
                {
                    int[] fileId = fileIDs.SplitTOInt(',');
                    FileList = new FT_FileB().GetEntities(d => fileId.Contains(d.ID)).ToList();
                }
                dt.Columns.Add("FileList", Type.GetType("System.Object"));
                foreach (DataRow row in dt.Rows)
                {
                    if (FileList.Count > 0)
                    {

                        string[] fileIds = row["Files"].ToString().Split(',');
                        row["FileList"] = FileList.Where(d => fileIds.Contains(d.ID.ToString()));
                    }
                }
            }
            #endregion

            msg.Result = dt;
            //msg.Result1 = Math.Ceiling(recordCount * 1.0 / 8);
            msg.Result1 = recordCount;
        }
        #endregion

        #region 添加产品
        /// <summary>
        /// 添加产品
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void ADDCP(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            SZHL_CRM_CPGL cp = JsonConvert.DeserializeObject<SZHL_CRM_CPGL>(P1);
            if (P2 != "") // 处理微信上传的图片
            {
                string fids = new FT_FileB().ProcessWxIMG(P2, "CRM", UserInfo);
                if (!string.IsNullOrEmpty(cp.Files))
                {
                    cp.Files += "," + fids;
                }
                else
                {
                    cp.Files = fids;
                }
            }
            decimal price = 0;
            if (!decimal.TryParse(cp.Price, out price))
            {
                msg.ErrorMsg = "单价金额格式不正确";
                return;
            }

            if (string.IsNullOrWhiteSpace(cp.Name))
            {
                msg.ErrorMsg = "产品名称不能为空";
                return;
            }
            if (string.IsNullOrWhiteSpace(cp.BianHao))
            {
                msg.ErrorMsg = "产品编号不能为空";
                return;
            }
            if (string.IsNullOrWhiteSpace(cp.ChengBen))
            {
                msg.ErrorMsg = "单位成本不能为空";
                return;
            }
            if (string.IsNullOrWhiteSpace(cp.DanWei))
            {
                msg.ErrorMsg = "销售单位不能为空";
                return;
            }
            if (cp.ID == 0)
            {
                cp.CRDate = DateTime.Now;
                cp.CRUser = UserInfo.User.UserName;
                cp.ComId = UserInfo.User.ComId;
                new SZHL_CRM_CPGLB().Insert(cp);
            }
            else
            {
                new SZHL_CRM_CPGLB().Update(cp);
            }

            msg.Result = cp;
        }
        #endregion

        #region 删除产品
        /// <summary>
        /// 删除
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void DELCPBYID(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            try
            {
                if (new SZHL_CRM_CPGLB().Delete(d => d.ID.ToString() == P1))
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

        #region 获取产品BYID
        /// <summary>
        /// 获取产品BYID
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void GETCPMODEL(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            int Id = 0;
            int.TryParse(P1, out Id);
            SZHL_CRM_CPGL sg = new SZHL_CRM_CPGLB().GetEntity(d => d.ID == Id && d.ComId == UserInfo.User.ComId);
            msg.Result = sg;
            if (sg.Files != null && sg.Files != "")
            {
                msg.Result1 = new FT_FileB().GetEntities(" ID in (" + sg.Files + ")");
            }

        }
        #endregion

        #region 获取所有产品
        /// <summary>
        /// 获取所有产品
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void GETCPALL(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            int Id = 0;
            int.TryParse(P1, out Id);
            //List<SZHL_CRM_CPGL> cpList = null;
            //cpList = new SZHL_CRM_CPGLB().GetEntities(d => d.ComId == UserInfo.User.ComId).ToList();
            string sql = string.Format("SELECT ID,Name FROM SZHL_CRM_CPGL WHERE ComId = '{0}'", UserInfo.User.ComId);
            DataTable dt = new SZHL_CRM_CPGLB().GetDTByCommand(sql);
            msg.Result = dt;
        }
        #endregion

        #region 导出客户
        /// <summary>
        /// 导出客户
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1">客户信息</param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void EXPORTCP(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            try
            {
                string userName = UserInfo.User.UserName;
                string strWhere = " 1=1 and ComId=" + UserInfo.User.ComId;

                string leibie = context.Request["lb"] ?? "";
                if (leibie != "")
                {
                    strWhere += string.Format(" And LeiBie='{0}' ", leibie);
                }
                string strContent = context.Request["Content"] ?? "";
                if (strContent != "")
                {
                    string extwhere = string.Empty;
                    string extids = new JH_Auth_ExtendDataB().GetExtIds(Int32.Parse(UserInfo.User.ComId.ToString()), "CPGL", strContent);
                    if (extids != "")
                    {
                        extwhere = " or ID in (" + extids + ")";
                    }
                    strWhere += string.Format(" And ( Name like '%{0}%' " + extwhere + " )", strContent);
                }

                DataTable dt = new DataTable();

                string strCls = string.Empty;
                strCls = "ID,Name '产品名称',dbo.fn_ZDName(LeiBie) '产品类型',BianHao '产品编号',Price '标准单价(元)',DanWei '销售单位',ChengBen '单位成本(元)',"
                    //+ "Province '省', City '市', District '区',Address '地址',PostCode '邮编',dbo.fn_ZDName(Status) '跟进状态',dbo.fn_ZDName(Source) '客户来源',"
                    + "dbo.clearhtml(PContent) '产品介绍'";
                dt = new SZHL_CRM_KHGLB().GetDTByCommand("select " + strCls + " from SZHL_CRM_CPGL where " + strWhere + " order by CRDate desc");

                //msg.Result = dt;

                DataTable dtExtColumn = new JH_Auth_ExtendModeB().GetExtColumnAll(UserInfo.QYinfo.ComId, "CPGL");
                foreach (DataRow drExt in dtExtColumn.Rows)
                {
                    dt.Columns.Add(drExt["TableFiledName"].ToString(), Type.GetType("System.String"));
                }

                if (dtExtColumn.Rows.Count > 0)
                {
                    foreach (DataRow dr in dt.Rows)
                    {
                        DataTable dtExtData = new JH_Auth_ExtendModeB().GetExtDataAll(UserInfo.QYinfo.ComId, "CPGL", dr["ID"].ToString());
                        foreach (DataRow drExtData in dtExtData.Rows)
                        {
                            dr[drExtData["TableFiledName"].ToString()] = drExtData["ExtendDataValue"].ToString();
                        }
                    }
                }
                dt.Columns.Remove("ID");

                CommonHelp ch = new CommonHelp();
                msg.ErrorMsg = ch.ExportToExcel("产品", dt);
            }
            catch
            {
                msg.ErrorMsg = "导出失败！";
            }
        }
        #endregion

        #endregion

        #region 合同管理

        #region 获取合同列表
        /// <summary>
        /// 获取合同列表
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void GETHTLIST(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            string userName = UserInfo.User.UserName;

            int page = 0;
            int pagecount = 8;
            int.TryParse(context.Request["p"] ?? "1", out page);//页码
            int.TryParse(context.Request["pagecount"] ?? "8", out pagecount);//页数
            page = page == 0 ? 1 : page;
            int recordCount = 0;
            string strWhere = string.Format(" ht.ComId={0} ", UserInfo.User.ComId);

            string leibie = context.Request["lb"] ?? "";
            if (leibie != "")
            {
                strWhere += string.Format(" And ht.HTType='{0}' ", leibie);
            }
            string strYear = context.Request["year"] ?? "";
            if (strYear != "")
            {
                strWhere += string.Format(" And year(ht.HTStartTime)='{0}' ", strYear);
            }
            string strContent = context.Request["Content"] ?? "";
            strContent = strContent.TrimEnd();
            if (strContent != "")
            {
                string extwhere = string.Empty;
                string extids = new JH_Auth_ExtendDataB().GetExtIds(Int32.Parse(UserInfo.User.ComId.ToString()), "HTGL", strContent);
                if (extids != "")
                {
                    extwhere = " or ht.ID in (" + extids + ")";
                }

                strWhere += string.Format(" And ( ht.Title like '%{0}%' " + extwhere + " )", strContent);
            }
            if (P2 != "")
            {
                string[] strs = P2.Split('_');

                if (!string.IsNullOrEmpty(strs[0]))
                {
                    strWhere += string.Format(" And ht.FKFS='{0}' ", strs[0]);
                }
                if (!string.IsNullOrEmpty(strs[1]))
                {
                    strWhere += string.Format(" And ht.HTStatus='{0}' ", strs[1]);
                }
            }
            //根据创建时间查询
            string time = context.Request["time"] ?? "";
            if (time != "")
            {
                if (time == "1")   //近一周
                {
                    strWhere += string.Format(" And datediff(day,ht.CRDate,getdate())<7");
                }
                else if (time == "2")
                {  //近一月
                    strWhere += string.Format(" And datediff(day,ht.CRDate,getdate())<30");
                }
                else if (time == "3")  //自定义时间
                {
                    string strTime = context.Request["starTime"] ?? "";
                    string endTime = context.Request["endTime"] ?? "";
                    if (strTime != "")
                    {
                        strWhere += string.Format(" And convert(varchar(10),ht.CRDate,120) >='{0}'", strTime);
                    }
                    if (endTime != "")
                    {
                        strWhere += string.Format(" And convert(varchar(10),ht.CRDate,120) <='{0}'", endTime);
                    }
                }
                else if (time == "4")  //今年
                {
                    strWhere += string.Format(" And year(ht.CRDate)=year(getdate())");
                }
                else if (time == "5")   //上一年
                {
                    strWhere += string.Format(" And year(ht.CRDate)=year(getdate())-1");
                }

            }

            int DataID = -1;
            int.TryParse(context.Request["ID"] ?? "-1", out DataID);//记录Id
            if (DataID != -1)
            {
                string strIsHasDataQX = new JH_Auth_QY_ModelB().ISHASDATAREADQX("CRM", DataID, UserInfo);
                if (strIsHasDataQX == "Y")
                {
                    strWhere += string.Format(" And ht.ID = '{0}'", DataID);
                }

            }
            if (P1 != "")
            {
                switch (P1)
                {
                    case "0": //手机单条数据
                        {
                            //设置usercenter已读
                            new JH_Auth_User_CenterB().ReadMsg(UserInfo, DataID, "HTGL");
                        }
                        break;
                    case "1": //我的
                        {
                            strWhere += " And ht.CRUser='" + userName + "'";
                        }
                        break;
                    case "2": //下属
                        {
                            //获取当前登录人负责的下属人员 
                            string Users = new JH_Auth_UserB().GetUserBranchUsers(UserInfo.User.ComId.Value, UserInfo.User.UserName).Select(d => d.UserName).ToList().ListTOString(',');
                            strWhere += string.Format(" and ht.CRUser in ('{0}') ", Users.ToFormatLike());
                        }
                        break;
                }
                DataTable dt = new SZHL_CRM_HTGLB().GetDataPager(@" SZHL_CRM_HTGL ht LEFT join JH_Auth_ZiDian zd on HTType= zd.ID and Class=16 
LEFT join JH_Auth_ZiDian zde on FKFS= zde.ID and zde.Class=17 
LEFT join SZHL_CRM_KHGL kh on KHID=kh.ID 
LEFT JOIN SZHL_CRM_CPGL cp on PID=cp.ID ",
                    " ht.*,zd.TypeName,kh.KHName,zde.TypeName AS FSName,cp.Name AS CPName "
                    , pagecount, page, "ht.CRDate desc", strWhere, ref recordCount);

                string Ids = "";
                string fileIDs = "";

                dt.Columns.Add("SubExt", typeof(DataTable));
                dt.Columns.Add("GJJL", Type.GetType("System.Object"));
                DataTable dtExt = new JH_Auth_ExtendModeB().GetExtColumnAll(UserInfo.User.ComId, "HTGL");
                foreach (DataRow drExt in dtExt.Rows)
                {
                    dt.Columns.Add(drExt["TableFiledName"].ToString(), Type.GetType("System.String"));
                }

                foreach (DataRow row in dt.Rows)
                {
                    Ids += row["ID"].ToString() + ",";
                    if (!string.IsNullOrEmpty(row["Files"].ToString()))
                    {
                        fileIDs += row["Files"].ToString() + ",";
                    }
                    if (row["HTStatus"] != null && row["HTStatus"].ToString() != "")
                    {
                        string statusName = "";
                        switch (row["HTStatus"].ToString())
                        {
                            case "0":
                                statusName = "未开始";
                                break;
                            case "1":
                                statusName = "执行中";
                                break;
                            case "2":
                                statusName = "成功结束";
                                break;
                            case "3":
                                statusName = "意外终止";
                                break;
                        }
                        row["HTStatus"] = statusName;
                    }

                    #region 扩展字段
                    DataTable dtExtData = new JH_Auth_ExtendModeB().GetExtData(UserInfo.User.ComId, "HTGL", row["ID"].ToString());
                    row["SubExt"] = dtExtData;
                    foreach (DataRow drExtData in dtExtData.Rows)
                    {
                        row[drExtData["TableFiledName"].ToString()] = drExtData["ExtendDataValue"].ToString();
                    }
                    #endregion

                    DataTable dtGJJL = new SZHL_CRM_KHGLB().GetDTByCommand("select * from SZHL_CRM_GJJL where ComId='" + UserInfo.User.ComId + "' and typecode='HTGL' and KHID='" + row["ID"].ToString() + "' order by CRDate desc");
                    dtGJJL.Columns.Add("FileList", Type.GetType("System.Object"));
                    foreach (DataRow drgjjl in dtGJJL.Rows)
                    {
                        if (drgjjl["Files"] != null && drgjjl["Files"].ToString() != "")
                        {
                            drgjjl["FileList"] = new FT_FileB().GetEntities(" ID in (" + drgjjl["Files"].ToString() + ")");
                        }
                    }
                    row["GJJL"] = dtGJJL;
                }
                Ids = Ids.TrimEnd(',');
                fileIDs = fileIDs.TrimEnd(',');
                if (Ids != "")
                {
                    List<FT_File> FileList = new List<FT_File>();
                    if (!string.IsNullOrEmpty(fileIDs))
                    {
                        int[] fileId = fileIDs.SplitTOInt(',');
                        FileList = new FT_FileB().GetEntities(d => fileId.Contains(d.ID)).ToList();
                    }
                    dt.Columns.Add("FileList", Type.GetType("System.Object"));
                    foreach (DataRow row in dt.Rows)
                    {
                        if (FileList.Count > 0)
                        {

                            string[] fileIds = row["Files"].ToString().Split(',');
                            row["FileList"] = FileList.Where(d => fileIds.Contains(d.ID.ToString()));
                        }
                    }
                }
                msg.Result = dt;
                //msg.Result1 = Math.Ceiling(recordCount * 1.0 / 8);
                msg.Result1 = recordCount;
            }
        }
        #endregion

        #region 添加合同
        /// <summary>
        /// 添加合同
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void ADDHT(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            SZHL_CRM_HTGL ht = JsonConvert.DeserializeObject<SZHL_CRM_HTGL>(P1);
            if (P2 != "") // 处理微信上传的图片
            {
                string fids = new FT_FileB().ProcessWxIMG(P2, "CRM", UserInfo);
                if (!string.IsNullOrEmpty(ht.Files))
                {
                    ht.Files += "," + fids;
                }
                else
                {
                    ht.Files = fids;
                }
            }
            decimal price = 0;
            if (string.IsNullOrWhiteSpace(ht.Price) || !decimal.TryParse(ht.Price, out price))
            {
                msg.ErrorMsg = "金额格式不正确";
                return;
            }
            if (string.IsNullOrWhiteSpace(ht.Title))
            {
                msg.ErrorMsg = "合同标题不能为空";
                return;
            }
            if (string.IsNullOrWhiteSpace(ht.FZR))
            {
                msg.ErrorMsg = "合同负责人不能为空";
                return;
            }
            if (ht.HTStartTime == null || ht.HTEndTime == null)
            {
                msg.ErrorMsg = "开始时间或结束时间不能为空";
                return;
            }
            if (ht.ID == 0)
            {
                ht.CRDate = DateTime.Now;
                ht.CRUser = UserInfo.User.UserName;
                ht.ComId = UserInfo.User.ComId;
                new SZHL_CRM_HTGLB().Insert(ht);
            }
            else
            {
                new SZHL_CRM_HTGLB().Update(ht);
            }
            msg.Result = ht;
        }
        #endregion

        #region 获取合同BYID
        /// <summary>
        /// 获取合同BYID
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void GETHTMODEL(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            int Id = 0;
            int.TryParse(P1, out Id);
            SZHL_CRM_HTGL sg = new SZHL_CRM_HTGLB().GetEntity(d => d.ID == Id && d.ComId == UserInfo.User.ComId);
            msg.Result = sg;
        }
        #endregion

        #region 删除合同
        /// <summary>
        /// 删除合同
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void DELHTBYID(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            try
            {
                int id = int.Parse(P1);
                if (new SZHL_CRM_HTGLB().Delete(d => d.ID == id))
                {
                    new SZHL_CRM_GJJLB().Delete(d => d.KHID == id && d.TypeCode == "HTGL");
                    msg.ErrorMsg = "";
                }
            }
            catch (Exception ex)
            {
                msg.ErrorMsg = ex.Message;
            }
        }
        #endregion

        #region 导入合同
        /// <summary>
        /// 导入合同
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1">客户信息</param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void IMPORTHT(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            try
            {
                string strmsg = string.Empty;
                int n = 0;
                List<JObject> ss = JsonConvert.DeserializeObject<List<JObject>>(P1);
                foreach (var s in ss)
                {
                    if (s["status"] != null && s["status"].ToString() == "1")
                    {
                        SZHL_CRM_HTGL htgl = new SZHL_CRM_HTGL();

                        if (s["对应客户"] != null)
                        {
                            string strkh = s["对应客户"].ToString();
                            var kh = new SZHL_CRM_KHGLB().GetEntities("ComId='" + UserInfo.QYinfo.ComId + "' and KHName='" + strkh + "'").FirstOrDefault();
                            if (kh != null)
                            {
                                htgl.KHID = kh.ID;
                            }
                        }

                        if (s["关联产品"] != null)
                        {
                            string strkh = s["关联产品"].ToString();
                            var kh = new SZHL_CRM_CPGLB().GetEntities("ComId='" + UserInfo.QYinfo.ComId + "' and Name='" + strkh + "'").FirstOrDefault();
                            if (kh != null)
                            {
                                htgl.PID = kh.ID;
                            }
                        }

                        #region 合同状态
                        if (s["合同状态"] != null)
                        {
                            string strgjzt = s["合同状态"].ToString();
                            if (strgjzt != "")
                            {
                                switch (strgjzt)
                                {
                                    case "未开始": htgl.HTStatus = "0"; break;
                                    case "执行中": htgl.HTStatus = "1"; break;
                                    case "成功结束": htgl.HTStatus = "2"; break;
                                    case "意外终止": htgl.HTStatus = "3"; break;
                                }
                            }
                        }
                        #endregion
                        #region 合同类型
                        if (s["合同类型"] != null)
                        {
                            string strkhly = s["合同类型"].ToString();
                            if (strkhly != "")
                            {
                                var type = new JH_Auth_ZiDianB().GetEntities(" ComId='" + UserInfo.QYinfo.ComId + "' and Class=16 and TypeName='" + strkhly + "' ").FirstOrDefault();
                                if (type != null)
                                {
                                    htgl.HTType = type.ID;
                                }
                                else
                                {
                                    JH_Auth_ZiDian jaz = new JH_Auth_ZiDian();
                                    jaz.ComId = UserInfo.QYinfo.ComId;
                                    jaz.Class = 16;
                                    jaz.TypeName = strkhly;
                                    jaz.Remark = "0";
                                    jaz.CRUser = UserInfo.User.UserName;
                                    jaz.CRDate = DateTime.Now;

                                    new JH_Auth_ZiDianB().Insert(jaz);

                                    htgl.HTType = jaz.ID;
                                }
                            }
                        }
                        #endregion
                        #region 付款方式
                        if (s["付款方式"] != null)
                        {
                            string strsshy = s["付款方式"].ToString();
                            if (strsshy != "")
                            {
                                var type = new JH_Auth_ZiDianB().GetEntities(" ComId='" + UserInfo.QYinfo.ComId + "' and Class=17 and TypeName='" + strsshy + "' ").FirstOrDefault();
                                if (type != null)
                                {
                                    htgl.FKFS = type.ID;
                                }
                                else
                                {
                                    JH_Auth_ZiDian jaz = new JH_Auth_ZiDian();
                                    jaz.ComId = UserInfo.QYinfo.ComId;
                                    jaz.Class = 17;
                                    jaz.TypeName = strsshy;
                                    jaz.Remark = "0";
                                    jaz.CRUser = UserInfo.User.UserName;
                                    jaz.CRDate = DateTime.Now;

                                    new JH_Auth_ZiDianB().Insert(jaz);

                                    htgl.FKFS = jaz.ID;
                                }
                            }
                        }
                        #endregion
                        #region 负责人
                        if (s["负责人"] != null)
                        {
                            string strfzr = s["负责人"].ToString();
                            if (strfzr != "")
                            {
                                var fzr = new JH_Auth_UserB().GetEntities("ComId='" + UserInfo.QYinfo.ComId + "' and ( UserName='" + strfzr + "' or mobphone='" + strfzr + "')").FirstOrDefault();
                                if (fzr != null)
                                {
                                    htgl.FZR = fzr.UserName;
                                }
                            }
                        }
                        #endregion

                        if (s["合同标题"] != null)
                        {
                            htgl.Title = s["合同标题"].ToString();
                        }
                        if (s["合同总金额"] != null)
                        {
                            htgl.Price = s["合同总金额"].ToString();
                        }
                        if (s["签约日期"] != null)
                        {
                            DateTime dt = new DateTime();
                            try
                            {
                                dt = DateTime.Parse(s["签约日期"].ToString());
                            }
                            catch { }
                            htgl.QYDate = dt;
                        }
                        if (s["开始时间"] != null)
                        {
                            DateTime dt = new DateTime();
                            try
                            {
                                dt = DateTime.Parse(s["开始时间"].ToString());
                            }
                            catch { }
                            htgl.HTStartTime = dt;
                        }
                        if (s["结束时间"] != null)
                        {
                            DateTime dt = new DateTime();
                            try
                            {
                                dt = DateTime.Parse(s["结束时间"].ToString());
                            }
                            catch { }
                            htgl.HTEndTime = dt;
                        }
                        if (s["付款说明"] != null)
                        {
                            htgl.FKSM = s["付款说明"].ToString();
                        }
                        if (s["有效期"] != null)
                        {
                            htgl.ExpiryDate = s["有效期"].ToString();
                        }
                        if (s["我方签约人"] != null)
                        {
                            htgl.WFQYR = s["我方签约人"].ToString();
                        }
                        if (s["客户方签约人"] != null)
                        {
                            htgl.KHQYR = s["客户方签约人"].ToString();
                        }
                        if (s["合同编号"] != null)
                        {
                            htgl.HTCode = s["合同编号"].ToString();
                        }
                        if (s["备注"] != null)
                        {
                            htgl.Remark = s["备注"].ToString();
                        }

                        htgl.CRDate = DateTime.Now;
                        htgl.CRUser = UserInfo.User.UserName;
                        htgl.ComId = UserInfo.User.ComId;

                        new SZHL_CRM_HTGLB().Insert(htgl);

                        DataTable dtExtColumn = new JH_Auth_ExtendModeB().GetExtColumnAll(UserInfo.QYinfo.ComId, "HTGL");
                        foreach (DataRow drExt in dtExtColumn.Rows)
                        {
                            string strValue = string.Empty;
                            if (s[drExt["TableFiledName"].ToString()] != null)
                            {
                                strValue = s[drExt["TableFiledName"].ToString()].ToString();
                            }

                            JH_Auth_ExtendData jext = new JH_Auth_ExtendData();
                            jext.ComId = UserInfo.QYinfo.ComId;
                            jext.TableName = "KHGL";
                            jext.DataID = htgl.ID;
                            jext.ExtendModeID = Int32.Parse(drExt["ID"].ToString());
                            jext.ExtendDataValue = strValue;
                            jext.CRUser = UserInfo.User.UserName;
                            jext.CRDate = DateTime.Now;

                            new JH_Auth_ExtendDataB().Insert(jext);
                        }
                        n = n + 1;
                    }
                }
                msg.Result = strmsg;
                msg.Result1 = n;
            }
            catch
            {
                msg.ErrorMsg = "导入失败！";
            }
        }
        #endregion

        #region 导出合同
        /// <summary>
        /// 导出合同
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1">客户信息</param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void EXPORTHT(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            try
            {
                string userName = UserInfo.User.UserName;

                string strWhere = string.Format(" ht.ComId={0} ", UserInfo.User.ComId);

                string leibie = context.Request["lb"] ?? "";
                if (leibie != "")
                {
                    strWhere += string.Format(" And ht.HTType='{0}' ", leibie);
                }
                string strContent = context.Request["Content"] ?? "";
                if (strContent != "")
                {
                    string extwhere = string.Empty;
                    string extids = new JH_Auth_ExtendDataB().GetExtIds(Int32.Parse(UserInfo.User.ComId.ToString()), "HTGL", strContent);
                    if (extids != "")
                    {
                        extwhere = " or ht.ID in (" + extids + ")";
                    }

                    strWhere += string.Format(" And ( ht.Title like '%{0}%' " + extwhere + " )", strContent);
                }
                if (P2 != "")
                {
                    string[] strs = P2.Split('_');

                    if (!string.IsNullOrEmpty(strs[0]))
                    {
                        strWhere += string.Format(" And ht.FKFS='{0}' ", strs[0]);
                    }
                    if (!string.IsNullOrEmpty(strs[1]))
                    {
                        strWhere += string.Format(" And ht.HTStatus='{0}' ", strs[1]);
                    }
                }
                //根据创建时间查询
                string time = context.Request["time"] ?? "";
                if (time != "")
                {
                    if (time == "1")   //近一周
                    {
                        strWhere += string.Format(" And datediff(day,ht.CRDate,getdate())<7");
                    }
                    else if (time == "2")
                    {  //近一月
                        strWhere += string.Format(" And datediff(day,ht.CRDate,getdate())<30");
                    }
                    else if (time == "3")  //自定义时间
                    {
                        string strTime = context.Request["starTime"] ?? "";
                        string endTime = context.Request["endTime"] ?? "";
                        if (strTime != "")
                        {
                            strWhere += string.Format(" And convert(varchar(10),ht.CRDate,120) >='{0}'", strTime);
                        }
                        if (endTime != "")
                        {
                            strWhere += string.Format(" And convert(varchar(10),ht.CRDate,120) <='{0}'", endTime);
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
                        strWhere += string.Format(" And ID = '{0}'", DataID);
                    }

                }

                if (P1 != "")
                {
                    DataTable dt = new DataTable();
                    switch (P1)
                    {
                        case "0": //手机单条数据
                            {
                                //设置usercenter已读
                                new JH_Auth_User_CenterB().ReadMsg(UserInfo, DataID, "HTGL");
                            }
                            break;
                        case "1": //我的
                            {
                                strWhere += " And ht.CRUser='" + userName + "'";
                            }
                            break;
                        case "2": //下属
                            {
                                //获取当前登录人负责的下属人员 
                                string Users = new JH_Auth_UserB().GetUserBranchUsers(UserInfo.User.ComId.Value, UserInfo.User.UserName).Select(d => d.UserName).ToList().ListTOString(',');
                                strWhere += string.Format(" and ht.CRUser in ('{0}') ", Users.ToFormatLike());
                            }
                            break;
                    }
                    string strCls = string.Empty;
                    strCls = "ht.ID,ht.Title '合同标题',dbo.fn_ZDName(ht.HTType) '合同类型',ht.Price '合同总金额',CONVERT(varchar(100), ht.QYDate, 111) '签约日期',kh.KHName '对应客户',CONVERT(varchar(100), ht.HTStartTime, 111) '开始时间',"
                        //+ "Province '省', City '市', District '区',Address '地址',PostCode '邮编',dbo.fn_ZDName(Status) '跟进状态',dbo.fn_ZDName(Source) '客户来源',"
                        + "CONVERT(varchar(100), ht.HTEndTime, 111) '结束时间',case when ht.HTStatus='0' then '未开始' when ht.HTStatus='1' then '执行中' when ht.HTStatus='2' then '成功结束' when ht.HTStatus='3' then '意外终止' else '其他' END '合同状态',dbo.fn_ZDName(ht.FKFS) '付款方式',"
                        + "ht.FKSM '付款说明', cp.Name '关联产品',"
                        + "ht.ExpiryDate '有效期',ht.WFQYR '我方签约人',ht.KHQYR '客户方签约人',ht.HTCode '合同编号',dbo.fn_YHName(ht.ComId,ht.FZR) '负责人',dbo.clearhtml(ht.Remark) '备注'";
                    dt = new SZHL_CRM_KHGLB().GetDTByCommand("select " + strCls + " from SZHL_CRM_HTGL ht left join SZHL_CRM_KHGL kh on ht.KHID=kh.ID left join SZHL_CRM_CPGL cp on ht.PID=cp.ID where " + strWhere + " order by ht.CRDate desc");

                    //msg.Result = dt;

                    DataTable dtExtColumn = new JH_Auth_ExtendModeB().GetExtColumnAll(UserInfo.QYinfo.ComId, "HTGL");
                    foreach (DataRow drExt in dtExtColumn.Rows)
                    {
                        dt.Columns.Add(drExt["TableFiledName"].ToString(), Type.GetType("System.String"));
                    }

                    if (dtExtColumn.Rows.Count > 0)
                    {
                        foreach (DataRow dr in dt.Rows)
                        {
                            DataTable dtExtData = new JH_Auth_ExtendModeB().GetExtDataAll(UserInfo.QYinfo.ComId, "KHGL", dr["ID"].ToString());
                            foreach (DataRow drExtData in dtExtData.Rows)
                            {
                                dr[drExtData["TableFiledName"].ToString()] = drExtData["ExtendDataValue"].ToString();
                            }
                        }
                    }
                    dt.Columns.Remove("ID");

                    CommonHelp ch = new CommonHelp();
                    msg.ErrorMsg = ch.ExportToExcel("合同", dt);
                }
            }
            catch
            {
                msg.ErrorMsg = "导出失败！";
            }
        }
        #endregion

        #endregion

        
    
    }
}