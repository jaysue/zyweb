
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;
using FastReflectionLib;
using System.Data;
using QJY.Data;
using QJY.API;
using Newtonsoft.Json;
using Senparc.Weixin.QY.Entities;
using QJY.Common;

namespace QJY.API
{
    public class WQQDManage : IWsService
    {
        public void ProcessRequest(HttpContext context, ref Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            MethodInfo methodInfo = typeof(WQQDManage).GetMethod(msg.Action.ToUpper());
            WQQDManage model = new WQQDManage();
            methodInfo.FastInvoke(model, new object[] { context, msg, P1, P2, UserInfo });
        }

        #region 外勤签到列表
        /// <summary>
        /// 外勤签到列表
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void GETWQQDLIST(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            string userName = UserInfo.User.UserName;
            string strWhere = " 1=1 and ComId=" + UserInfo.User.ComId;

            string strContent = context.Request["Content"] ?? "";
            strContent = strContent.TrimEnd();
            if (strContent != "")
            {
                strWhere += string.Format(" And ( QDContent like '%{0}%' or CRUserName like '%{0}%' or BranchName like '%{0}%')", strContent);
            }


            //根据创建时间查询
            string time = context.Request["time"] ?? "";
            if (time != "")
            {
                if (time == "1")   //近一周
                {
                    strWhere += string.Format(" And datediff(day,CRDate,getdate())<7");
                }
                else if (time == "2")
                {  //近一月
                    strWhere += string.Format(" And datediff(day,CRDate,getdate())<30");
                }
                else if (time == "3")  //自定义时间
                {
                    string strTime = context.Request["starTime"] ?? "";
                    string endTime = context.Request["endTime"] ?? "";
                    if (strTime != "")
                    {
                        strWhere += string.Format(" And convert(varchar(10),CRDate,120) >='{0}'", strTime);
                    }
                    if (endTime != "")
                    {
                        strWhere += string.Format(" And convert(varchar(10),CRDate,120) <='{0}'", endTime);
                    }
                }
            }


            int DataID = -1;
            int.TryParse(context.Request["ID"] ?? "-1", out DataID);//记录Id
            if (DataID != -1)
            {
                string strIsHasDataQX = new JH_Auth_QY_ModelB().ISHASDATAREADQX("WQQD", DataID, UserInfo);
                if (strIsHasDataQX == "Y")
                {
                    strWhere += string.Format(" And ID = '{0}'", DataID);
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
                            new JH_Auth_User_CenterB().ReadMsg(UserInfo, DataID, "WQQD");
                        }
                        break;
                    case "1": //创建的
                        {
                            strWhere += " And CRUser ='" + userName + "'";
                        }
                        break;
                    case "2": //汇报我的
                        {
                            strWhere += string.Format(" And ','+HBUser+','  like '%,{0},%'", userName);
                        }
                        break;
                    case "3"://下属签到
                        {
                            //获取当前登录人负责的下属人员 
                            string Users = new JH_Auth_UserB().GetUserBranchUsers(UserInfo.User.ComId.Value, UserInfo.User.UserName).Select(d => d.UserName).ToList().ListTOString(',');
                            strWhere += string.Format("and   CRUser in ('{0}')", Users.ToFormatLike());
                        }
                        break;
                }
                dt = new SZHL_WQQDB().GetDataPager("SZHL_WQQD", "*", pagecount, page, " CRDate desc", strWhere, ref total);

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

        #region 获取外勤签到
        /// <summary>
        /// 获取外勤签到
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void GETWQQDMODEL(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            int Id = int.Parse(P1);
            SZHL_WQQD ccxj = new SZHL_WQQDB().GetEntity(d => d.ID == Id);
            msg.Result = ccxj;
            if (ccxj != null)
            {
                if (!string.IsNullOrEmpty(ccxj.Files))
                {
                    msg.Result1 = new FT_FileB().GetEntities(" ID in (" + ccxj.Files + ")");
                }
                new JH_Auth_User_CenterB().ReadMsg(UserInfo, ccxj.ID, "WQQD");
            }

        }
        #endregion

        #region 添加外勤签到
        /// <summary>
        /// 添加外勤签到
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1">客户信息</param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void ADDWQQD(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            SZHL_WQQD wqqd = JsonConvert.DeserializeObject<SZHL_WQQD>(P1);

            if (string.IsNullOrEmpty(wqqd.Position))
            {
                msg.ErrorMsg = "当前位置不能为空";
                return;
            }
            if (P2 != "") // 处理微信上传的图片
            {
                string fids = new FT_FileB().ProcessWxIMG(P2, "RLZY", UserInfo);

                if (!string.IsNullOrEmpty(wqqd.Files))
                {
                    wqqd.Files += "," + fids;
                }
                else
                {
                    wqqd.Files = fids;
                }
            }
            if (wqqd.ID == 0)
            {
                wqqd.CRDate = DateTime.Now;
                wqqd.CRUser = UserInfo.User.UserName;
                wqqd.BranchName = UserInfo.BranchInfo.DeptName;
                wqqd.BranchNo = UserInfo.User.BranchCode;
                wqqd.ComId = UserInfo.User.ComId;
                wqqd.CRUserName = UserInfo.User.UserRealName;
                new SZHL_WQQDB().Insert(wqqd);

                if (!string.IsNullOrEmpty(wqqd.HBUser))
                {
                    SZHL_TXSX CSTX = new SZHL_TXSX();
                    CSTX.Date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    CSTX.APIName = "WQQD";
                    CSTX.ComId = UserInfo.User.ComId;
                    CSTX.FunName = "SENDWQMSG";
                    CSTX.CRUserRealName = UserInfo.User.UserRealName;
                    CSTX.MsgID = wqqd.ID.ToString();
                    CSTX.ISCS = "N";
                    CSTX.TXUser = wqqd.HBUser;
                    CSTX.TXMode = "WQQD";
                    CSTX.CRUser = UserInfo.User.UserName;

                    TXSX.TXSXAPI.AddALERT(CSTX); //时间为发送时间 
                }

            }
            else
            {
                new SZHL_WQQDB().Update(wqqd);
            }
            msg.Result = wqqd;
        }

        public void SENDWQMSG(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            var tx = JsonConvert.DeserializeObject<SZHL_TXSX>(P1);
            int msgid = Int32.Parse(tx.MsgID);

            UserInfo = new JH_Auth_UserB().GetUserInfo(tx.ComId.Value, tx.CRUser);

            var model = new SZHL_WQQDB().GetEntity(p => p.ID == msgid && p.ComId == UserInfo.User.ComId);
            if (model != null)
            {
                Article ar0 = new Article();

                ar0.Title = "外勤签到提醒";
                ar0.Description = "签到人：" + UserInfo.User.UserRealName + "\r\n签到位置：" + model.Position + "\r\n签到备注：" + CommonHelp.RemoveHtml(model.QDContent) + "\r\n签到时间：" + model.CRDate;
                ar0.Url = model.ID.ToString();
                if (!string.IsNullOrEmpty(model.Files))
                {
                    ar0.PicUrl = model.Files.Split(',')[0];
                }
                List<Article> al = new List<Article>();
                al.Add(ar0);

                string jsr = string.Empty;
                if (!string.IsNullOrEmpty(model.HBUser))
                {
                    jsr = model.HBUser;

                    //发送消息
                    string content = ar0.Description;
                    new JH_Auth_User_CenterB().SendMsg(UserInfo, "WQQD", content, model.ID.ToString(), jsr, "A");
                    if (!string.IsNullOrEmpty(jsr))
                    {
                        WXHelp wx = new WXHelp(UserInfo.QYinfo);
                        wx.SendTH(al, "WQQD", "A", jsr);
                    }
                }
            }
        }

        #endregion

        #region 删除外勤签到
        /// <summary>
        /// 删除外勤签到
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1">ID</param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void DELWQQD(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            try
            {
                int id = Int32.Parse(P1);
                if (new SZHL_WQQDB().Delete(d => d.ID == id))
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
    }
}