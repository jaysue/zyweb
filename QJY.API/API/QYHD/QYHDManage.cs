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
    public class QYHDManage : IWsService
    {
        public void ProcessRequest(HttpContext context, ref Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            MethodInfo methodInfo = typeof(QYHDManage).GetMethod(msg.Action.ToUpper());
            QYHDManage model = new QYHDManage();
            methodInfo.FastInvoke(model, new object[] { context, msg, P1, P2, UserInfo });
        }

        /// <summary>
        /// 活动列表
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void GETQYHDLIST_PAGE(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            int DataID = -1;
            int.TryParse(context.Request["ID"] ?? "-1", out DataID);//获取单个数据的ID



            int listType = 0;
            int.TryParse(context.Request["listType"] ?? "0", out listType);
            string strWhere = "ComId=" + UserInfo.User.ComId + " AND (CYR='' OR CYR like '%" + UserInfo.User.UserName + "%' OR  CRUser='" + UserInfo.User.UserName + "')";

            if (listType == 1)
            {
                strWhere = "ComId=" + UserInfo.User.ComId + " And CRUser='" + UserInfo.User.UserName + "'";
            }

            if (P1 == "0")
            {
                strWhere += string.Format(" And Type = 0 ");
            }
            else if (P1 == "1")
            {
                strWhere += string.Format(" And Type=1 ");

            }

            if (P2 != "")
            {
                strWhere += string.Format(" And Title like '%{0}%' ", P2);
            }

            if (DataID != -1)
            {
                string strIsHasDataQX = new JH_Auth_QY_ModelB().ISHASDATAREADQX("QYHD", DataID, UserInfo);
                if (strIsHasDataQX == "Y")
                {
                    strWhere += string.Format(" And ID = '{0}'", DataID);
                }
                //更新消息为已读状态
                new JH_Auth_User_CenterB().ReadMsg(UserInfo, DataID, "QYHD");

            }
            int page = 0;
            int pagecount = 8;
            int.TryParse(context.Request["p"] ?? "1", out page);
            int.TryParse(context.Request["pagecount"] ?? "8", out pagecount);//页数
            int recordCount = 0;
            //HDStatus 0已结束  2 正在进行  1 未开始
            string strSql = @"SELECT qyhd.ID,qyhd.Type,qyhd.FQF,qyhd.Title,qyhd.JZDate,qyhd.HDDate,qyhd.Status,qyhd.CRDate,qyhd.Files,qyhd.CRUser,qyhd.ComId,qyhd.CYR,qyhd.StartTime,qyhd.EndTime,qyhd.HD_Content,qyhd.HD_Adress,qyhd.TP_IsNM,TP_Type,TP_IsPublic,qyhd.IsPublic,COUNT(result.ID) BMCount ,SUM(case when result.CRUser='" + UserInfo.User.UserName + @"' then 1 else 0 end ) UserBMCount,
                                case when StartTime>GETDATE() then 1 when EndTime>GETDATE() and StartTime<GETDATE() then 2  when EndTime<GETDATE() then 0 End HDStatus
                                from SZHL_QYHDN qyhd left join SZHL_QYHD_Result result on  qyhd.Id=result.HDID 
                                group by qyhd.ID,qyhd.Type,qyhd.FQF,qyhd.Title,qyhd.JZDate,qyhd.HDDate,qyhd.Status,qyhd.Files,qyhd.CRDate,qyhd.CRUser,qyhd.ComId,qyhd.CYR,qyhd.StartTime,qyhd.EndTime,qyhd.HD_Content,qyhd.HD_Adress,qyhd.TP_IsNM,TP_Type,TP_IsPublic,qyhd.IsPublic";
            DataTable dt = new SZHL_QYHDNB().GetDataPager("(" + strSql + ") newTab  ", "*", pagecount, page, "HDStatus desc,CRDate desc", strWhere, ref recordCount);

            string Ids = "";
            string fileIDs = "";
            string BMIDs = "";//报名列表
            string TPItemIds = ""; //投票选项
            #region 附件，评论列表
            foreach (DataRow row in dt.Rows)
            {
                Ids += row["ID"].ToString() + ",";
                if (!string.IsNullOrEmpty(row["Files"].ToString()))
                {
                    fileIDs += row["Files"].ToString() + ",";
                }
                if (row["Type"].ToString() == "0")
                {
                    BMIDs += row["ID"].ToString() + ",";
                }
                else
                {

                    TPItemIds += row["ID"].ToString() + ",";
                }
            }
            Ids = Ids.TrimEnd(',');
            fileIDs = fileIDs.TrimEnd(',');
            BMIDs = BMIDs.TrimEnd(',');
            TPItemIds = TPItemIds.TrimEnd(',');
            if (Ids != "")
            {
                List<FT_File> FileList = new List<FT_File>();
                DataTable dtPL = new JH_Auth_TLB().GetDTByCommand(string.Format("SELECT * FROM JH_Auth_TL tl WHERE tl.MSGType='QYHD' AND  tl.MSGTLYID in ({0})", Ids));
                if (!string.IsNullOrEmpty(fileIDs))
                {
                    int[] fileId = fileIDs.SplitTOInt(',');
                    FileList = new FT_FileB().GetEntities(d => fileId.Contains(d.ID)).ToList();
                }
                //报名列表
                List<SZHL_QYHD_Result> resultList = new List<SZHL_QYHD_Result>();
                if (BMIDs != "")
                {
                    int[] HDIds = BMIDs.SplitTOInt(',');
                    resultList = new SZHL_QYHD_ResultB().GetEntities(d => HDIds.Contains(d.HDID.Value) && d.ComId == UserInfo.User.ComId).ToList();
                }
                //投票选项
                DataTable dtOption = new DataTable();
                if (TPItemIds != "")
                {
                    string strOpSql = string.Format(@"SELECT op.ID,op.OptionText,op.HDId,COUNT(result.ID) TPCount from SZHL_QYHD_Option op left join SZHL_QYHD_Result result on op.ID=result.OptionID  and op.HDId in ({0})
                                                GROUP by op.ID,op.OptionText,op.HDId", TPItemIds);
                    dtOption = new SZHL_QYHD_OptionB().GetDTByCommand(strOpSql);
                }
                dt.Columns.Add("PLList", Type.GetType("System.Object"));
                dt.Columns.Add("FileList", Type.GetType("System.Object"));
                dt.Columns.Add("BMList", Type.GetType("System.Object"));
                dt.Columns.Add("OptionList", Type.GetType("System.Object"));
                foreach (DataRow row in dt.Rows)
                {
                    int HDId = int.Parse(row["ID"].ToString());
                    row["BMList"] = resultList.Where(d => d.HDID == HDId);
                    //row["PLList"] = dtPL.FilterTable("MSGTLYID='" + row["ID"] + "'");
                    DataTable dtPLs = dtPL.FilterTable("MSGTLYID='" + row["ID"] + "'");
                    dtPLs.Columns.Add("FileList", Type.GetType("System.Object"));
                    foreach (DataRow dr in dtPLs.Rows)
                    {
                        if (dr["MSGisHasFiles"] != null && dr["MSGisHasFiles"].ToString() != "")
                        {
                            int[] fileIds = dr["MSGisHasFiles"].ToString().SplitTOInt(',');
                            dr["FileList"] = new FT_FileB().GetEntities(d => fileIds.Contains(d.ID));
                        }
                    }
                    row["PLList"] = dtPLs;
                    if (dtOption.Rows.Count > 0)
                    {
                        row["OptionList"] = dtOption.Where("HDId=" + row["ID"]);
                    }
                    if (FileList.Count > 0)
                    {
                        string[] fileIds = row["Files"].ToString().Split(',');
                        row["FileList"] = FileList.Where(d => fileIds.Contains(d.ID.ToString()));
                    }
                }
            }
            #endregion
            msg.Result = dt;
            msg.Result1 = recordCount;
        }

        /// <summary>
        /// 添加
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1">活动报名实体json</param>
        /// <param name="P2"></param>
        /// <param name="strUserName"></param>
        public void ADDQYHD(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            SZHL_QYHDN qyhd = JsonConvert.DeserializeObject<SZHL_QYHDN>(P1);
            if (qyhd.Title.Trim() == "")
            {
                msg.ErrorMsg = "标题不能为空";
                return;
            }
            if (qyhd.FQF.Trim() == "")
            {
                msg.ErrorMsg = "发起方不能为空";
                return;
            }
            if (qyhd.CYR.Trim() == "")
            {
                msg.ErrorMsg = "请选择参与人";
                return;
            }
            if (qyhd.Type == 0)
            {
                if (qyhd.HD_Content.Trim() == "")
                {
                    msg.ErrorMsg = "活动内容不能为空";
                    return;
                }
            }
            List<SZHL_QYHD_Option> option = JsonConvert.DeserializeObject<List<SZHL_QYHD_Option>>(P2);
            if (qyhd.ID == 0 && qyhd.Type == 1)
            {
                if (option.Count < 2)
                {
                    msg.ErrorMsg = "选项最少为两项";
                    return;
                }
                else
                {
                    for (int i = 0; i < option.Count; i++)
                    {
                        if (string.IsNullOrWhiteSpace(option[i].OptionText))
                        {
                            msg.ErrorMsg = "选项不能为空";
                            return;
                        }
                    }
                }
            }
            if (qyhd.StartTime >= qyhd.EndTime)
            {
                msg.ErrorMsg = "开始时间不能大于等于结束时间";
                return;
            }
            string wximg = context.Request["wximg"];
            #region
            if (wximg != null && wximg != "") // 处理微信上传的图片
            {
                string fids = new FT_FileB().ProcessWxIMG(wximg, "QYHD", UserInfo);

                if (!string.IsNullOrEmpty(qyhd.Files))
                {
                    qyhd.Files += "," + fids;
                }
                else
                {
                    qyhd.Files = fids;
                }
            }
            #endregion

            if (qyhd.ID == 0)
            {
                qyhd.CRDate = DateTime.Now;
                qyhd.CRUser = UserInfo.User.UserName;
                qyhd.ComId = UserInfo.User.ComId;
                new SZHL_QYHDNB().Insert(qyhd);


                if (qyhd.Type == 1)
                {
                    for (int i = 0; i < option.Count; i++)
                    {
                        option[i].CRDate = DateTime.Now;
                        option[i].CRUser = UserInfo.User.UserName;
                        option[i].ComId = UserInfo.User.ComId;
                        option[i].HDId = qyhd.ID;
                    }

                    new SZHL_QYHD_OptionB().Insert(option);
                }
                #region 注释
                SZHL_TXSX TX = new SZHL_TXSX();
                TX.Date = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
                TX.APIName = "QYHD";
                TX.ComId = UserInfo.User.ComId;
                TX.FunName = "SENDWXMSG";
                TX.TXMode = "QYHD";
                TX.CRUserRealName = UserInfo.User.UserRealName;
                TX.MsgID = qyhd.ID.ToString();
                TX.TXUser = qyhd.CYR;
                TX.CRUser = UserInfo.User.UserName;
                TXSX.TXSXAPI.AddALERT(TX); //时间为发送时间
                #endregion
            }
            else
            {
                new SZHL_QYHDNB().Update(qyhd);
            }
            msg.Result = qyhd;
        }

        /// <summary>
        /// 获取活动信息
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1">活动报名信息ID</param>
        /// <param name="P2"></param>
        /// <param name="strUserName"></param>
        public void GETQYHDMODEL(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            int Id = int.Parse(P1);
            SZHL_QYHDN qyhd = new SZHL_QYHDNB().GetEntity(d => d.ID == Id);
            msg.Result = qyhd;

            DataTable dtPL = new SZHL_GZBGB().GetDTByCommand("  SELECT *  FROM JH_Auth_TL WHERE MSGType='QYHD' AND  MSGTLYID='" + P1 + "'");
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
            if (!string.IsNullOrEmpty(qyhd.Files))
            {
                int[] fileIds = qyhd.Files.SplitTOInt(',');
                msg.Result2 = new FT_FileB().GetEntities(d => fileIds.Contains(d.ID));
            }
            msg.Result3 = new SZHL_QYHD_ResultB().GetEntities(d => d.HDID == qyhd.ID);
            msg.Result4 = new SZHL_QYHD_OptionB().GetEntities(d => d.HDId == qyhd.ID);
        }

        /// <summary>
        /// 添加报名信息
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1">报名信息json</param>
        /// <param name="P2"></param>
        /// <param name="strUserName"></param>
        public void ADDQYHDITEM(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            SZHL_QYHD_Result item = JsonConvert.DeserializeObject<SZHL_QYHD_Result>(P1);

            SZHL_QYHDN qyhd = new SZHL_QYHDNB().GetEntity(d => d.ID == item.HDID);
            if (qyhd == null)
            {
                msg.ErrorMsg = "活动已截止";
                return;
            }
            if (qyhd.StartTime > DateTime.Now)
            {
                msg.ErrorMsg = "活动报名未开始";
                return;
            }
            if (qyhd.EndTime < DateTime.Now)
            {
                msg.ErrorMsg = "活动报名已截止";
                return;
            }
            if (item.ID != 0)
            {
                new SZHL_QYHD_ResultB().Update(item);
            }
            else
            {
                item.CRUser = UserInfo.User.UserName;
                item.CRDate = DateTime.Now;
                item.ComId = UserInfo.User.ComId;
                new SZHL_QYHD_ResultB().Insert(item);
            }
            msg.Result = item;
        }

        /// <summary>
        /// 获取投票信息
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1">ID</param>
        /// <param name="P2"></param>
        /// <param name="strUserName"></param>
        public void GETQYTPMODEL(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            int Id = 0;
            int.TryParse(P1, out Id);
            SZHL_QYHDN qyhd = new SZHL_QYHDNB().GetEntity(d => d.ID == Id);
            string UserISTP = "N";

            //判断当前人是否已投票
            UserISTP = new SZHL_QYHD_ResultB().GetEntities(d => d.HDID == qyhd.ID && d.OptionUser == UserInfo.User.UserName && d.ComId == UserInfo.User.ComId).Count() > 0 ? "Y" : "N";

            if (P2 == "isuser")
                UserISTP = "";

            //选项详细
            DataTable dt = new SZHL_QYHD_ResultB().GetDTByCommand(@"SELECT ID,OptionText FROM SZHL_QYHD_Option  WHERE HDId='" + P1 + @"'");
            msg.Result = qyhd;
            msg.Result2 = new SZHL_QYHD_ResultB().GetDTByCommand("SELECT COUNT(0),OptionUser FROM SZHL_QYHD_Result WHERE HDId='" + P1 + "' GROUP BY OptionUser,ComId").Rows.Count;
            if (UserISTP == "N")//未投票返回企业投票信息
            {

                msg.Result1 = dt;

            }
            else
            {  //已投票返回投票信息 
                dt.Columns.Add("tpr", Type.GetType("System.Object"));
                dt.Columns.Add("num", Type.GetType("System.Object"));
                List<SZHL_QYHD_Result> resultList = new SZHL_QYHD_ResultB().GetEntities(d => d.HDID == Id).ToList();
                foreach (DataRow row in dt.Rows)
                {
                    int opId = 0;
                    int.TryParse(row["ID"].ToString(), out opId);
                    List<string> optionUser = resultList.Where(d => d.OptionID == opId).Select(d => d.OptionUser).ToList();
                    row["tpr"] = optionUser;
                    row["num"] = optionUser.Count;
                }
                msg.Result1 = dt;
                msg.Result3 = resultList.Count;
            }
            msg.Result4 = UserISTP;
            if (qyhd != null && qyhd.EndTime < DateTime.Now)
            {
                qyhd.Status = 1;
            }
        }

        /// <summary>
        /// 删除信息
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1">活动Id</param>
        /// <param name="P2"></param>
        /// <param name="strUserName"></param>
        public void DELMODEL(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            try
            {
                int hdid = 0;
                if (int.TryParse(P1, out hdid))
                {
                    new SZHL_QYHDNB().Delete(d => d.ID == hdid);
                    new SZHL_QYHD_OptionB().Delete(d => d.HDId == hdid);
                    new SZHL_QYHD_ResultB().Delete(d => d.HDID == hdid);
                }
            }
            catch (Exception) { msg.ErrorMsg = "操作失败"; }
        }

        /// <summary>
        /// 投票
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="strUserName"></param>
        public void ADDTPITEM(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            int Id = 0;
            int.TryParse(P1, out Id);
            SZHL_QYHDN qyhd = new SZHL_QYHDNB().GetEntity(d => d.ID == Id);
            if (qyhd == null)
            {
                msg.ErrorMsg = "活动不存在或已被删除";
                return;
            }
            if (qyhd.StartTime > DateTime.Now)
            {
                msg.ErrorMsg = "投票未开始";
                return;
            }
            if (qyhd.EndTime < DateTime.Now)
            {
                msg.ErrorMsg = "投票已截止";
                return;
            }
            DataTable dt = new SZHL_QYHD_ResultB().GetDTByCommand("SELECT OptionUser FROM SZHL_QYHD_Result WHERE HDId='" + P1 + "' AND CRUser='" + UserInfo.User.UserName + "' AND ComId='" + UserInfo.User.ComId + "'  GROUP BY OptionUser");
            if (dt.Rows.Count > 0)
            {
                msg.ErrorMsg = "您已投票";
                return;
            }
            if (P2 == "")
            {
                msg.ErrorMsg = "请选择";
                return;
            }

            List<SZHL_QYHD_Result> results = new List<SZHL_QYHD_Result>();
            string[] xxitem = P2.Split(',');
            for (int i = 0; i < xxitem.Length; i++)
            {
                SZHL_QYHD_Result result = new SZHL_QYHD_Result();
                result.CRDate = DateTime.Now;
                result.CRUser = UserInfo.User.UserName;
                result.ComId = UserInfo.User.ComId;
                result.OptionUser = UserInfo.User.UserName;
                result.OptionID = Convert.ToInt32(xxitem[i]);
                result.HDID = Id;
                results.Add(result);
            }

            new SZHL_QYHD_ResultB().Insert(results);
            msg.Result = qyhd;
        }

        /// <summary>
        /// 获取投票信息
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="strUserName"></param>
        public void TPCOUNTLIST(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            int Id = 0;
            int.TryParse(P1, out Id);
            DataTable dt = new SZHL_QYHD_ResultB().GetDTByCommand(@"SELECT ID,OptionText FROM SZHL_QYHD_Option  WHERE HDId='" + P1 + @"'");
            dt.Columns.Add("num", Type.GetType("System.Object"));
            dt.Columns.Add("tpr", Type.GetType("System.Object"));
            List<SZHL_QYHD_Result> resultList = new SZHL_QYHD_ResultB().GetEntities(d => d.HDID == Id).ToList();
            foreach (DataRow row in dt.Rows)
            {
                int opId = 0;
                int.TryParse(row["ID"].ToString(), out opId);
                List<string> optionUser = resultList.Where(d => d.OptionID == opId).Select(d => d.OptionUser).ToList();
                row["num"] = optionUser.Count;
                row["tpr"] = optionUser;
            }
            msg.Result = dt;
            msg.Result1 = resultList.Count;
        }

        /// <summary>
        /// 获取投票人
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="strUserName"></param>
        public void GETTPR(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            int Id = 0, opid = 0;
            int.TryParse(P1, out Id);
            int.TryParse(P2, out opid);
            DataTable dttpr = new SZHL_QYHD_ResultB().GetDTByCommand("SELECT CRUser FROM SZHL_QYHD_Result WHERE HDId=" + P1 + " AND OptionID=" + opid + " GROUP BY CRUser");

            msg.Result = dttpr;
        }
        /// <summary>
        /// 已报名点击报名加载用户报名详细
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1">活动报名的ID</param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void GETQYHDITEMBYUSER(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            int ID = int.Parse(P1);
            msg.Result = new SZHL_QYHD_ResultB().GetEntity(d => d.HDID == ID && d.ComId == UserInfo.User.ComId && d.CRUser == UserInfo.User.UserName);
        }

        public void SENDWXMSG(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            var tx = JsonConvert.DeserializeObject<SZHL_TXSX>(P1);

            int msgid = Int32.Parse(tx.MsgID);

            var qyhd = new SZHL_QYHDNB().GetEntity(p => p.ID == msgid);
            UserInfo = new JH_Auth_UserB().GetUserInfo(tx.ComId.Value, qyhd.CRUser);
            if (qyhd != null)
            {
                ////发送消息
                string content = qyhd.FQF + "发起了以（" + qyhd.Title + "）为主题的" + (qyhd.Type == 0 ? "活动" : "投票");
                new JH_Auth_User_CenterB().SendMsg(UserInfo, "QYHD", content, qyhd.ID.ToString(), qyhd.CYR, qyhd.Type == 1 ? "A" : "B");

                ////发送微信消息
                Article ar = new Article();
                ar.Title = content;
                ar.Description = "";
                //ar.PicUrl = qyhd.
                ar.Url = qyhd.ID.ToString();

                List<Article> Msgs = new List<Article>();
                Msgs.Add(ar);
                WXHelp wx = new WXHelp(UserInfo.QYinfo);
                wx.SendTH(Msgs, "QYHD", "B", qyhd.CYR);
            }
        }
    }
}